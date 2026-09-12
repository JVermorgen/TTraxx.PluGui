using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Helpers;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;
using TTraxx.PluGui.Gui.Windows.Interfaces;

namespace TTraxx.PluGui.Gui.Platform.Win32;

internal sealed class Win32PlatformWindow : IPlatformWindow
{
    private const string UIEngineLibraryName = "libSkiaSharp";

    private nint _hwnd;
    private nint _origWndProc = nint.Zero;
    private GCHandle _selfHandle;
    private IPlatformWindowHost? _host;

    // DIB-backed surface, Skia (controls).
    // Reused between paints, only rebuilt on resize.
    private nint _dibMemDc;
    private nint _dibSection;
    private nint _dibOldBitmap;
    private nint _dibBits;
    private SKSurface? _skSurface;
    private int _dibW;
    private int _dibH;

    static Win32PlatformWindow() => Kernel32.EnsureOwnDirectoryLoaded(UIEngineLibraryName);

    public IEventPumpSource? EventPumpSource => null; // Win32 has its own message loop (WndProc) — no external pump needed

    public float GetInitialScaleFactor(nint parentHandle) => Shcore.DetermineInitialScale(parentHandle);

    public bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host)
    {
        _host = host;
        if (parentHandle == nint.Zero) return false;

        _hwnd = User32.CreateWindowEx(WindowStyleConstants.WS_EX_NOACTIVATE, WindowStyleConstants.WC_STATIC, null,
            WindowStyleConstants.WS_CHILD | WindowStyleConstants.WS_VISIBLE | WindowStyleConstants.SS_NOTIFY,
            0, 0, width, height, parentHandle, nint.Zero, nint.Zero, nint.Zero);

        if (_hwnd == nint.Zero) return false;

        _selfHandle = GCHandle.Alloc(this);
        User32.SetWindowUserData(_hwnd, GCHandle.ToIntPtr(_selfHandle));
        _origWndProc = User32.SetWindowLongPtr(_hwnd, WindowMessageConstants.GWL_WNDPROC, GetStaticWndProcPointer());
        return true;
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        if (_hwnd != nint.Zero) User32.MoveWindow(_hwnd, x, y, width, height, true);
    }

    public void Invalidate()
    {
        if (_hwnd != nint.Zero) User32.InvalidateRect(_hwnd, nint.Zero, false);
    }

    public void Destroy()
    {
        if (_hwnd != nint.Zero)
        {
            if (_origWndProc != nint.Zero) User32.SetWindowLongPtr(_hwnd, WindowMessageConstants.GWL_WNDPROC, _origWndProc);
            User32.DestroyWindow(_hwnd);
            _hwnd = nint.Zero;
        }
        DisposeDibSurface();
        if (_selfHandle.IsAllocated) _selfHandle.Free();
    }

    #region "private"

    private static unsafe nint GetStaticWndProcPointer()
    {
        delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint> ptr = &StaticWndProc;
        return (nint)ptr;
    }

    // Must stay static + blittable params for NativeAOT. Looks up the
    // owning instance via GWLP_USERDATA (set in AttachToParent before this
    // proc was installed) and dispatches to the real instance logic.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static nint StaticWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        nint userData = User32.GetWindowUserData(hWnd);
        if (userData != 0)
        {
            Win32PlatformWindow? instance = (Win32PlatformWindow?)GCHandle.FromIntPtr(userData).Target;
            if (instance != null) return instance.InstanceWndProc(hWnd, msg, wParam, lParam);
        }

        return User32.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private nint InstanceWndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case WindowMessageConstants.WM_MOUSEACTIVATE: return WindowMessageConstants.MA_NOACTIVATE;
            case WindowMessageConstants.WM_ERASEBKGND: return 1; // everything is drawn in WM_PAINT
            case WindowMessageConstants.WM_PAINT:
                {
                    nint hdc = User32.BeginPaint(_hwnd, out PaintStruct ps);
                    try { PaintViaSkia(hdc); }
                    finally { User32.EndPaint(_hwnd, ref ps); }
                    return 0;
                }
            case WindowMessageConstants.WM_SIZE:
                int w = User32Helpers.GetWidth(lParam), h = User32Helpers.GetHeight(lParam);
                if (w > 0 && h > 0) _host?.OnResize(w, h);
                break;
            case WindowMessageConstants.WM_LBUTTONDOWN:
                User32.SetCapture(_hwnd);
                _host?.OnPointerDown(User32Helpers.GetX(lParam), User32Helpers.GetY(lParam), User32Helpers.GetKeyModifiers(wParam));
                return nint.Zero;
            case WindowMessageConstants.WM_MOUSEMOVE:
                _host?.OnPointerMove(User32Helpers.GetX(lParam), User32Helpers.GetY(lParam), User32Helpers.GetKeyModifiers(wParam));
                return nint.Zero;
            case WindowMessageConstants.WM_LBUTTONUP:
                _host?.OnPointerUp(User32Helpers.GetX(lParam), User32Helpers.GetY(lParam));
                User32.ReleaseCapture();
                return nint.Zero;
            case WindowMessageConstants.WM_MOUSEWHEEL:
                Point pt = new() { X = User32Helpers.GetX(lParam), Y = User32Helpers.GetY(lParam) };
                User32.ScreenToClient(_hwnd, ref pt);
                int ticks = User32Helpers.GetWheel(wParam) / WindowMessageConstants.WHEEL_DELTA;
                _host?.OnWheel(pt.X, pt.Y, ticks, User32Helpers.GetKeyModifiers(wParam));
                return nint.Zero;
            case WindowMessageConstants.WM_LBUTTONDBLCLK:
                _host?.OnDoubleClick(User32Helpers.GetX(lParam), User32Helpers.GetY(lParam));
                return nint.Zero;
            case WindowMessageConstants.WM_RBUTTONDOWN:
                _host?.OnContextMenu(User32Helpers.GetX(lParam), User32Helpers.GetY(lParam));
                return nint.Zero;
        }

        return User32.CallWindowProc(_origWndProc, hWnd, msg, wParam, lParam);
    }

    private void PaintViaSkia(nint hdc)
    {
        User32.GetClientRect(_hwnd, out Rect rect);
        int w = Math.Max(1, rect.Right - rect.Left);
        int h = Math.Max(1, rect.Bottom - rect.Top);

        EnsureDibSurface(hdc, w, h);

        SKCanvas canvas = _skSurface!.Canvas;
        canvas.Save();
        canvas.Clear(SKColors.Transparent); // guarantees a clean start every frame, regardless of what the previous frame left in the reused buffer
        _host?.OnPaint(canvas, w, h);
        canvas.Restore();
        _skSurface.Flush();

        Gdi32.BitBlt(hdc, 0, 0, w, h, _dibMemDc, 0, 0, GdiConstants.SRCCOPY);
    }

    private void EnsureDibSurface(nint hdc, int w, int h)
    {
        if (_skSurface != null && w == _dibW && h == _dibH) return;

        DisposeDibSurface();

        _dibMemDc = Gdi32.CreateCompatibleDC(hdc);
        _dibSection = Gdi32.CreateDIBSection32(_dibMemDc, w, h, out _dibBits); // top-down, 32bpp BGRA
        _dibOldBitmap = Gdi32.SelectObject(_dibMemDc, _dibSection);

        SKImageInfo info = new(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skSurface = SKSurface.Create(info, _dibBits, w * 4);

        _dibW = w;
        _dibH = h;
    }

    private void DisposeDibSurface()
    {
        _skSurface?.Dispose();
        _skSurface = null;

        if (_dibMemDc != nint.Zero)
        {
            if (_dibOldBitmap != nint.Zero) Gdi32.SelectObject(_dibMemDc, _dibOldBitmap);
            if (_dibSection != nint.Zero) Gdi32.DeleteObject(_dibSection);
            Gdi32.DeleteDC(_dibMemDc);
        }

        _dibMemDc = nint.Zero;
        _dibSection = nint.Zero;
        _dibBits = nint.Zero;
    }

    #endregion
}
