using SkiaSharp;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Input;
using TTraxx.PluGui.Gui.Platform.Linux.Internal;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;
using TTraxx.PluGui.Gui.Windows.Interfaces;

namespace TTraxx.PluGui.Gui.Platform.Linux;

internal sealed unsafe class LinuxPlatformWindow : IPlatformWindow, IEventPumpSource
{
    private const string UIEngineLibraryName = "libSkiaSharp";

    private nint _display;
    private nint _window;
    private nint _gc;
    private IPlatformWindowHost? _host;

    private SKSurface? _skSurface;
    private byte[]? _pixelBuffer;
    private int _w;
    private int _h;

    private ulong _lastClickTimeMs;
    private int _lastClickX;
    private int _lastClickY;

    private nint _visual;
    private int _depth;

    // Guard-state: the native window only exists between a successful
    // Attach() and Destroy(). SetBounds()/Invalidate()/Repaint() can enter
    // nested (host-reentrancy during setFrame/resizeView, or a late callback
    // after Destroy) before/after that window exists — see also
    // TryRegisterLinuxRunLoop() for the same pattern elsewhere.
    private bool _isAttached;
    private bool _isDestroyed;

    // Save a resize request that arrives before Attach() completes,
    // so we don't lose it but still apply it once the window exists.
    private int? _pendingX;
    private int? _pendingY;
    private int? _pendingWidth;
    private int? _pendingHeight;

    private const ulong DoubleClickThresholdMs = 400; // X11 has no system setting like GetDoubleClickTime() on Win32; fixed threshold
    private const int DoubleClickMaxDistance = 4; // pixels — prevents slight jitter between two separate clicks from counting as a double-click

    static LinuxPlatformWindow() => Libc.EnsureOwnDirectoryLoaded(UIEngineLibraryName);

    public IEventPumpSource EventPumpSource => this; // X11 heeft geen eigen message loop - de host moet onze fd pollen

    /// <summary>
    /// <para>
    /// X11, unlike Win32's Shcore, has no per-monitor/per-HWND DPI API —
    /// DPI is a session-wide X resource. parentHandle is thus not used;
    /// we read the "Xft.dpi" resource (which GTK/Qt/most desktop
    /// environments also do) and fall back to physical screen size in mm.
    /// </para>
    /// <para>
    /// Reuse _display if Attach() has already run (the normal case —
    /// Attached() in SpectralMorphView calls AttachToParent() before
    /// GetInitialScaleFactor()); otherwise we briefly open a connection
    /// here just for this query.
    /// </para>
    /// </summary>
    public float GetInitialScaleFactor(nint parentHandle) => Xlib.DetermineInitialScale(_display);

    public bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host)
    {
        _host = host;
        _display = Xlib.XOpenDisplay(nint.Zero);
        if (_display == nint.Zero) return false;

        var screen = Xlib.XDefaultScreen(_display);
        _visual = Xlib.XDefaultVisual(_display, screen);
        _depth = Xlib.XDefaultDepth(_display, screen);

        _window = Xlib.XCreateSimpleWindow(_display, parentHandle, 0, 0, (uint)width, (uint)height, 0, 0, 0);
        if (_window == nint.Zero) return false;

        _ = Xlib.XSelectInput(_display, _window,
                XlibConstants.ExposureMask
                | XlibConstants.ButtonPressMask
                | XlibConstants.ButtonReleaseMask
                | XlibConstants.PointerMotionMask
                | XlibConstants.StructureNotifyMask);

        _gc = Xlib.XCreateGC(_display, _window, 0, nint.Zero);
        _ = Xlib.XMapWindow(_display, _window);
        _ = Xlib.XFlush(_display);

        EnsureSurface(width, height);

        // Window now truly exists — from here on XMoveResizeWindow/XPutImage
        // calls are safe.
        _isAttached = true;

        // Was there meanwhile (nested, during the above steps or before this
        // Attach call) a SetBounds() request that we had to defer? Apply it now.
        if (_pendingWidth is int pw && _pendingHeight is int ph)
        {
            var px = _pendingX ?? 0;
            var py = _pendingY ?? 0;
            _pendingX = null;
            _pendingY = null;
            _pendingWidth = null;
            _pendingHeight = null;

            _ = Xlib.XMoveResizeWindow(_display, _window, px, py, (uint)pw, (uint)ph);
            EnsureSurface(pw, ph);
        }

        return true;
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        if (!_isAttached || _isDestroyed)
        {
            // Window does not exist yet (host-reentrancy during the first
            // setFrame()/Attach()) or no longer exists (late call after Destroy()).
            // Save the request; Attach() will apply it once the window is ready.
            // After Destroy() there's nothing meaningful left to save — we silently
            // ignore the request.
            if (!_isDestroyed)
            {
                _pendingX = x;
                _pendingY = y;
                _pendingWidth = width;
                _pendingHeight = height;
            }
            return;
        }

        _ = Xlib.XMoveResizeWindow(_display, _window, x, y, (uint)width, (uint)height);
        EnsureSurface(width, height);
    }

    public void Invalidate()
    {
        if (!_isAttached || _isDestroyed) return;
        Repaint(); // no separate dirty-flag needed: X11 has no "InvalidateRect" equivalent that you trigger yourself; direct repainting here is the pragmatic approach until we fully wire up events
    }

    public void Destroy()
    {
        if (_isDestroyed) return;
        _isDestroyed = true;
        _isAttached = false;

        if (_gc != nint.Zero) _ = Xlib.XFreeGC(_display, _gc);
        if (_window != nint.Zero) _ = Xlib.XDestroyWindow(_display, _window);
        _skSurface?.Dispose();
    }

    /// <summary>
    /// Called FROM the VST3 bridge code —
    /// the host reports via IRunLoop.onFDIsSet that there is activity on
    /// GetConnectionFd(), and only then may events be processed. Never
    /// poll blocking by itself.
    /// </summary>
    int IEventPumpSource.GetPumpHandle() => Xlib.XConnectionNumber(_display);

    public void ProcessPendingEvents()
    {
        if (!_isAttached || _isDestroyed) return;

        while (Xlib.XPending(_display) > 0)
        {
            var evt = new XEvent();
            var evtPtr = (nint)(&evt);
            _ = Xlib.XNextEvent(_display, evtPtr);
            DispatchEvent(evt, evtPtr);
        }
    }

    #region "private"

    private void DispatchEvent(XEvent evt, nint rawEventPtr)
    {
        switch (evt.type)
        {
            case XEventType.Expose:
                _ = Marshal.PtrToStructure<XExposeEvent>(rawEventPtr);
                // We deliberately ignore _.x/y/width/height and always repaint
                // the full surface — matches how WM_PAINT/PaintViaSkia also does it
                // on Windows (no partial-redraw logic).
                Repaint();
                break;
            case XEventType.ButtonPress:
                var buttonDown = Marshal.PtrToStructure<XButtonEvent>(rawEventPtr);
                if (buttonDown.button is 4 or 5)
                {
                    // X11 has no separate wheel event — scroll comes in as
                    // ButtonPress with button 4 (up) or 5 (down).
                    var ticks = buttonDown.button == 4 ? 1 : -1;
                    _host?.OnWheel(buttonDown.x, buttonDown.y, ticks, ToModifiers(buttonDown.state));
                }
                else if (buttonDown.button == 3)
                {
                    _host?.OnContextMenu(buttonDown.x, buttonDown.y);
                }
                else if (buttonDown.button == 1)
                {
                    var clickTime = (ulong)buttonDown.time; // XButtonEvent.time is an X11 Timestamp (ms since server start), not wall-clock — only usable relatively
                    var dx = buttonDown.x - _lastClickX;
                    var dy = buttonDown.y - _lastClickY;
                    var withinTime = clickTime - _lastClickTimeMs <= DoubleClickThresholdMs;
                    var withinDistance = (dx * dx) + (dy * dy) <= DoubleClickMaxDistance * DoubleClickMaxDistance;

                    if (withinTime && withinDistance)
                    {
                        _host?.OnDoubleClick(buttonDown.x, buttonDown.y);
                        // Reset so that any potential THIRD quick click doesn't immediately
                        // count as a double-click on the double-click — each double-click
                        // starts a new count from zero.
                        _lastClickTimeMs = 0;
                    }
                    else
                    {
                        _host?.OnPointerDown(buttonDown.x, buttonDown.y, ToModifiers(buttonDown.state));
                        _lastClickTimeMs = clickTime;
                        _lastClickX = buttonDown.x;
                        _lastClickY = buttonDown.y;
                    }
                }
                break;
            case XEventType.ButtonRelease:
                var buttonUp = Marshal.PtrToStructure<XButtonEvent>(rawEventPtr);
                if (buttonUp.button == 1) _host?.OnPointerUp(buttonUp.x, buttonUp.y);
                break;
            case XEventType.MotionNotify:
                var motion = Marshal.PtrToStructure<XMotionEvent>(rawEventPtr);
                _host?.OnPointerMove(motion.x, motion.y, ToModifiers(motion.state));
                break;
            case XEventType.ConfigureNotify:
                var configure = Marshal.PtrToStructure<XConfigureEvent>(rawEventPtr);
                if (configure.width > 0 && configure.height > 0)
                {
                    EnsureSurface(configure.width, configure.height);
                    _host?.OnResize(configure.width, configure.height);
                }
                break;
        }
    }

    private static KeyModifiers ToModifiers(uint state)
    {
        var modifiers = KeyModifiers.None;
        if ((state & XlibConstants.ShiftMask) != 0) modifiers |= KeyModifiers.Shift;
        if ((state & XlibConstants.ControlMask) != 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }

    private void EnsureSurface(int w, int h)
    {
        if (_skSurface != null && w == _w && h == _h) return;

        _w = w;
        _h = h;

        _pixelBuffer = new byte[w * h * 4];
        var pixels = GCHandle.Alloc(_pixelBuffer, GCHandleType.Pinned).AddrOfPinnedObject();

        var info = new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skSurface = SKSurface.Create(info, pixels, w * 4);
    }

    private void Repaint()
    {
        // Zonder deze guard kan een late/geneste paint-aanroep (bv. net na
        // Destroy(), of vóór Attach() volledig is afgerond) XPutImage
        // aanroepen met een _display/_window van nint.Zero -> SIGSEGV.
        if (!_isAttached || _isDestroyed) return;
        if (_skSurface is null || _pixelBuffer is null) return;

        var canvas = _skSurface.Canvas;
        _ = canvas.Save();
        canvas.Clear(SKColors.Transparent);
        _host?.OnPaint(canvas, _w, _h);
        canvas.Restore();
        _skSurface.Flush();

        fixed (byte* ptr = _pixelBuffer)
        {
            var image = Xlib.XCreateImage(_display, _visual, (uint)_depth, XlibConstants.ZPixmap, 0, ptr, (uint)_w, (uint)_h, 32, _w * 4);
            _ = Xlib.XPutImage(_display, _window, _gc, image, 0, 0, 0, 0, (uint)_w, (uint)_h);
        }
    }

    #endregion
}