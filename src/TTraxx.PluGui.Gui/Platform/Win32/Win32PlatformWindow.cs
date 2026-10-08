using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Helpers;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Win32;

/// <summary>
/// Windows backend, and the reference the other two platform layers are modelled on (see
/// MacOsPlatformWindow and LinuxPlatformWindow): a WS_CHILD window created inside the HWND the VST3
/// host supplies as parent, painted by blitting a reused DIB-backed Skia surface in WM_PAINT.
///
/// It creates a plain STATIC control and SUBCLASSES it (swapping in its own WndProc) rather than
/// registering a window class of its own, so nothing process-wide is registered from inside a host.
/// (The harness's own top-level window does register a class - it's a standalone app window, not one
/// embedded in a host process.)
///
/// The WndProc must be a static, blittable function pointer for NativeAOT, so the owning instance is
/// recovered inside it from a GCHandle parked in the window's user data. That GCHandle is also what
/// keeps this object alive while native code holds a pointer to it; <see cref="Destroy"/> restores the
/// original WndProc and frees it.
///
/// Neither pump source applies here: Win32 already has a message loop for events and SetTimer for the
/// repaint clock, which is why both properties are null.
/// </summary>
internal sealed class Win32PlatformWindow : IPlatformWindow
{
    private const nint ContinuousRepaintTimerId = 1;
    private const uint ContinuousRepaintIntervalMs = 33; // ~30 Hz

    private nint _hwnd;
    private bool _continuousRepaintActive;
    private nint _origWndProc = nint.Zero;
    private GCHandle _selfHandle;
    private IPlatformWindowHost? _host;
    private nint _dropTarget;

    // Whatever had the keyboard before a control took it (see SetKeyboardFocus), and the first half
    // of a character typed as a surrogate pair.
    private nint _focusBefore;
    private char _highSurrogate;

    // DIB-backed surface, Skia (controls).
    // Reused between paints, only rebuilt on resize.
    private nint _dibMemDc;
    private nint _dibSection;
    private nint _dibOldBitmap;
    private nint _dibBits;
    private SKSurface? _skSurface;
    private int _dibW;
    private int _dibH;

    static Win32PlatformWindow() => Kernel32.RegisterUIEngineResolver();

    public IEventPumpSource? EventPumpSource => null; // Win32 has its own message loop (WndProc) — no external pump needed

    public ITimerPumpSource? TimerPumpSource => null; // Win32 drives its own repaint timer via SetTimer/WM_TIMER — no external pump needed

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

        // Files dragged from Explorer: through OLE, the way Explorer drags (see OleDropTarget - a
        // host with a drop target of its own would otherwise take the drag). Only if OLE won't have
        // one does the window fall back to WM_DROPFILES: that route's style (WS_EX_ACCEPTFILES)
        // makes OLE accept files anywhere in the window, ahead of our own target, so the cursor
        // couldn't say where a drop lands. Into a host run as administrator Windows refuses a drag
        // from Explorer either way.
        // A drag out of a plugin window (FileDrag) isn't taken back in by one.
        _dropTarget = OleDropTarget.Register(new OleDropTarget.Callbacks(
            (x, y) => !FileDrag.IsDragging && (_host?.CanDropFilesAt(x, y) ?? false),
            (x, y, paths) =>
            {
                if (!FileDrag.IsDragging) _host?.OnFilesDropped(x, y, paths);
            },
            _hwnd));
        if (_dropTarget == nint.Zero) Shell32.DragAcceptFiles(_hwnd, true);
        return true;
    }

    public string? ShowOpenFileDialog(string title, IReadOnlyList<FileDialogFilter> filters)
    {
        if (_hwnd == nint.Zero) return null;

        var owner = User32.GetAncestor(_hwnd, User32.GA_ROOT);
        return Comdlg32.ShowOpen(owner != nint.Zero ? owner : _hwnd, title, filters);
    }

    public bool StartFileDrag(IReadOnlyList<string> paths)
    {
        if (_hwnd == nint.Zero || paths.Count == 0) return false;

        // OLE runs the mouse from here until the button comes up, so the window never sees the
        // release: the press is ended here, where the pointer is now.
        User32.ReleaseCapture();
        var dropped = FileDrag.Start(_hwnd, paths);

        User32.GetCursorPos(out var point);
        User32.ScreenToClient(_hwnd, ref point);
        _host?.OnPointerUp(point.X, point.Y);
        return dropped;
    }

    /// <summary>
    /// The window never takes the focus on a click (WM_MOUSEACTIVATE answers MA_NOACTIVATE, so the
    /// host keeps its shortcuts) - only while a control edits text, and it hands the focus back to
    /// whatever had it when the editing ends.
    /// </summary>
    public void SetKeyboardFocus(bool focused)
    {
        if (_hwnd == nint.Zero) return;

        if (focused)
        {
            var current = Keyboard.GetFocus();
            if (current == _hwnd) return;

            _focusBefore = current;
            Keyboard.SetFocus(_hwnd);
            return;
        }

        if (Keyboard.GetFocus() != _hwnd) return;

        var back = _focusBefore != nint.Zero && Keyboard.IsWindow(_focusBefore) ? _focusBefore : Keyboard.GetParent(_hwnd);
        _focusBefore = nint.Zero;
        Keyboard.SetFocus(back);
    }

    public string? GetClipboardText() => _hwnd == nint.Zero ? null : Keyboard.GetClipboardText(_hwnd);

    public void SetClipboardText(string text)
    {
        if (_hwnd != nint.Zero) Keyboard.SetClipboardText(_hwnd, text);
    }

    public string TranslateTypedCharacter(char character, KeyModifiers modifiers)
        => Keyboard.Translate(character)
            ?? ((modifiers & KeyModifiers.Shift) != 0 ? char.ToUpperInvariant(character) : character).ToString();

    // A WM_CHAR as text: control characters (Enter, Backspace, Ctrl+letter) are keys, not text, and
    // a character beyond the first plane arrives in two halves.
    private string? TakeCharacter(char character)
    {
        if (char.IsHighSurrogate(character))
        {
            _highSurrogate = character;
            return null;
        }

        if (char.IsLowSurrogate(character))
        {
            var high = _highSurrogate;
            _highSurrogate = default;
            return high == default ? null : new string([high, character]);
        }

        _highSurrogate = default;
        return char.IsControl(character) ? null : character.ToString();
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        if (_hwnd != nint.Zero) User32.MoveWindow(_hwnd, x, y, width, height, true);
    }

    public void Invalidate()
    {
        if (_hwnd != nint.Zero) User32.InvalidateRect(_hwnd, nint.Zero, false);
    }

    public void SetContinuousRepaint(bool enabled)
    {
        if (enabled == _continuousRepaintActive) return;
        _continuousRepaintActive = enabled;
        if (_hwnd == nint.Zero) return;

        if (enabled) User32.SetTimer(_hwnd, ContinuousRepaintTimerId, ContinuousRepaintIntervalMs, nint.Zero);
        else User32.KillTimer(_hwnd, ContinuousRepaintTimerId);
    }

    public void Destroy()
    {
        OleDropTarget.Revoke(_dropTarget);
        _dropTarget = nint.Zero;

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
            case WindowMessageConstants.WM_TIMER:
                if (wParam == ContinuousRepaintTimerId) Invalidate();
                return nint.Zero;
            case Keyboard.WM_GETDLGCODE:
                return Keyboard.DLGC_WANTALLKEYS | Keyboard.DLGC_WANTCHARS;
            case Keyboard.WM_KEYDOWN:
                if (_host?.OnKeyDown(new KeyEventArgs(Keyboard.KeyOf(wParam), Keyboard.CurrentModifiers())) == true) return nint.Zero;
                break;
            case Keyboard.WM_CHAR:
                if (TakeCharacter((char)wParam) is { } text && _host?.OnTextInput(text) == true) return nint.Zero;
                break;
            case Keyboard.WM_KILLFOCUS:
                _highSurrogate = default;
                _host?.OnKeyboardFocusLost();
                break;
            case WindowMessageConstants.WM_DROPFILES:
                var (paths, point) = Shell32.TakeDrop(wParam);
                if (paths.Count > 0) _host?.OnFilesDropped(point.X, point.Y, paths);
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
