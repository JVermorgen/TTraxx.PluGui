using SkiaSharp;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Linux.Internal;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux;

internal sealed unsafe class LinuxPlatformWindow : IPlatformWindow, IEventPumpSource, ITimerPumpSource
{
    // Matches Win32PlatformWindow's own SetTimer rate, so a continuously
    // repainting control (e.g. the level meter) animates at the same speed
    // regardless of platform.
    private const int ContinuousRepaintIntervalMs = 33;

    private bool _continuousRepaintEnabled;

    private nint _display;
    private nint _window;
    private nint _gc;
    private IPlatformWindowHost? _host;
    private X11Keyboard? _keyboard;
    private X11FileDrag? _fileDrag;
    private X11FileDrop? _fileDrop;

    // The pointer and the time of the last event that had them: where a drag starts, and its timestamp.
    private int _pointerX;
    private int _pointerY;
    private nint _lastEventTime;

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

    static LinuxPlatformWindow() => Libc.RegisterUIEngineResolver();

    public IEventPumpSource EventPumpSource => this; // X11 has no native message loop of its own - the host must poll our fd

    public ITimerPumpSource? TimerPumpSource => _continuousRepaintEnabled ? this : null;

    /// <summary>
    /// <para>
    /// X11, unlike Win32's Shcore, has no per-monitor/per-HWND DPI API —
    /// DPI is a session-wide X resource. parentHandle is thus not used;
    /// we read the "Xft.dpi" resource (which GTK/Qt/most desktop
    /// environments also do) and fall back to physical screen size in mm.
    /// </para>
    /// <para>
    /// Reuse _display if Attach() has already run before GetInitialScaleFactor());
    /// otherwise we briefly open a connection here just for this query.
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
                | XlibConstants.StructureNotifyMask
                | XlibConstants.KeyPressMask
                | XlibConstants.FocusChangeMask);

        _keyboard = new X11Keyboard(_display, _window, parentHandle);
        _fileDrag = new X11FileDrag(_display, _window);
        _fileDrop = new X11FileDrop(_display, _window);
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

    /// <summary>
    /// Unlike Win32/Cocoa, X11 gives a plain client window no timer of its own
    /// to drive a self-repaint. Since this platform-agnostic layer can't see
    /// the VST3-level IAudioPluginRunLoop.RegisterTimer (Steinberg::Linux::IRunLoop)
    /// itself, it just records the request and exposes itself via
    /// TimerPumpSource - the view-level code that DOES know about NPlug is
    /// responsible for registering/unregistering that with the host.
    /// </summary>
    public void SetContinuousRepaint(bool enabled) => _continuousRepaintEnabled = enabled;

    /// <summary>
    /// Drags files out over XDND (see X11FileDrag). Unlike Win32's this returns at once - true when
    /// the drag started - and the drag goes on from the event loop, since the target (the host, as a
    /// rule) answers from its own. The press ends here, as on Win32: the host gets OnPointerUp now.
    /// </summary>
    public bool StartFileDrag(IReadOnlyList<string> paths)
    {
        if (!_isAttached || _isDestroyed || paths.Count == 0 || _fileDrag is null) return false;
        if (!_fileDrag.Start(paths, _lastEventTime)) return false;

        _host?.OnPointerUp(_pointerX, _pointerY);
        return true;
    }

    /// <summary>
    /// Takes the X input focus while a control edits text, so the keys come here rather than to the
    /// host, and gives it back when the editing ends (see X11Keyboard). A click alone never takes it.
    /// </summary>
    public void SetKeyboardFocus(bool focused)
    {
        if (!_isAttached || _isDestroyed) return;
        _keyboard?.SetFocus(focused);
    }

    public string? GetClipboardText() => _isAttached && !_isDestroyed ? _keyboard?.GetClipboardText() : null;

    public void SetClipboardText(string text)
    {
        if (_isAttached && !_isDestroyed) _keyboard?.SetClipboardText(text);
    }

    // For a host that passes keys on through VST3's onKeyDown instead: X11 can't tell what a host's
    // plain character would make, so Shift just makes a letter upper case.
    public string TranslateTypedCharacter(char character, KeyModifiers modifiers)
        => ((modifiers & KeyModifiers.Shift) != 0 ? char.ToUpperInvariant(character) : character).ToString();

    /// <summary>
    /// X11 has no dialog of its own: this runs the desktop's - zenity (GNOME and most others), else
    /// kdialog (KDE) - and reads the chosen path from its output. Null if neither is installed or the
    /// user cancels. (Files dropped from a file manager come through XDND - see X11FileDrop - from the
    /// sources that look inside the host's window for this one.)
    /// </summary>
    public string? ShowOpenFileDialog(string title, IReadOnlyList<FileDialogFilter> filters)
    {
        var patterns = filters.Select(filter => (filter.Name, Patterns: string.Join(' ', filter.Extensions.Select(extension => "*." + extension)))).ToList();

        var zenity = new List<string> { "--file-selection", "--title", title };
        foreach (var (name, filterPatterns) in patterns) zenity.AddRange(["--file-filter", $"{name} | {filterPatterns}"]);
        if (RunDialog("zenity", zenity, out var path)) return path;

        var kdialogFilter = string.Join('\n', patterns.Select(pattern => $"{pattern.Patterns}|{pattern.Name}"));
        return RunDialog("kdialog", ["--title", title, "--getopenfilename", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), kdialogFilter], out path)
            ? path
            : null;
    }

    /// <summary>Runs a dialog program; false if it couldn't be started. The path is null when it was cancelled.</summary>
    private static bool RunDialog(string program, IEnumerable<string> arguments, out string? path)
    {
        path = null;
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo(program) { RedirectStandardOutput = true, UseShellExecute = false };
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = System.Diagnostics.Process.Start(start);
            if (process is null) return false;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            if (process.ExitCode == 0 && output.Length > 0) path = output;
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false; // not installed
        }
    }

    int ITimerPumpSource.PreferredIntervalMilliseconds => ContinuousRepaintIntervalMs;

    void ITimerPumpSource.OnTimerTick() => Invalidate();

    public void Destroy()
    {
        if (_isDestroyed) return;
        // Hand the keyboard back while the window still exists to give it from.
        if (_isAttached) _keyboard?.SetFocus(false);
        if (_isAttached) _fileDrag?.Dispose();

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
                _lastEventTime = buttonDown.time;
                (_pointerX, _pointerY) = (buttonDown.x, buttonDown.y);
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
                _lastEventTime = buttonUp.time;
                if (buttonUp.button != 1) break;

                // A drag out ended the press when it started; the release is the drop.
                if (_fileDrag is { IsDragging: true }) _fileDrag.OnRelease(buttonUp.time);
                else _host?.OnPointerUp(buttonUp.x, buttonUp.y);
                break;
            case XEventType.MotionNotify:
                var motion = Marshal.PtrToStructure<XMotionEvent>(rawEventPtr);
                _lastEventTime = motion.time;
                if (_fileDrag is { IsDragging: true })
                {
                    _fileDrag.OnMotion(motion.x_root, motion.y_root, motion.time);
                    break;
                }

                (_pointerX, _pointerY) = (motion.x, motion.y);
                _host?.OnPointerMove(motion.x, motion.y, ToModifiers(motion.state));
                break;
            case XEventType.ClientMessage:
                var message = (XClientMessageEvent*)rawEventPtr;
                if (_fileDrag?.OnClientMessage(message) != true) _ = _fileDrop?.OnClientMessage(message, _host);
                break;
            case XEventType.SelectionNotify:
                _ = _fileDrop?.OnSelectionNotify(*(XSelectionEvent*)rawEventPtr, _host);
                break;
            case XEventType.ConfigureNotify:
                var configure = Marshal.PtrToStructure<XConfigureEvent>(rawEventPtr);
                if (configure.width > 0 && configure.height > 0)
                {
                    EnsureSurface(configure.width, configure.height);
                    _host?.OnResize(configure.width, configure.height);
                }
                break;
            case XEventType.KeyPress:
                // As on Win32: the key (WM_KEYDOWN), then its text (WM_CHAR) whatever became of the key -
                // a text field takes a plain A as a key too (so the host doesn't get it), and still
                // wants the "a". A key that types nothing (an arrow, Ctrl+C) has no text.
                var keyEvent = (XKeyEvent*)rawEventPtr;
                var (key, text) = X11Keyboard.Read(keyEvent);
                if (key != Key.None) _host?.OnKeyDown(new KeyEventArgs(key, ToModifiers(keyEvent->state)));
                if (text is not null) _host?.OnTextInput(text);
                break;
            case XEventType.FocusOut:
                if (X11Keyboard.IsFocusLost(*(XFocusChangeEvent*)rawEventPtr)) _host?.OnKeyboardFocusLost();
                break;
            case XEventType.SelectionRequest:
                var request = *(XSelectionRequestEvent*)rawEventPtr;
                if (_fileDrag?.OnSelectionRequest(request) != true) _keyboard?.OnSelectionRequest(request);
                break;
            case XEventType.SelectionClear:
                _keyboard?.OnSelectionClear(*(XSelectionClearEvent*)rawEventPtr);
                break;
        }
    }

    private static KeyModifiers ToModifiers(uint state) => X11Keyboard.ModifiersOf(state);

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
        // Without this guard, a late/nested paint call (e.g. just after
        // Destroy(), or before Attach() has fully completed) can call
        // XPutImage with _display/_window set to nint.Zero -> SIGSEGV.
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
