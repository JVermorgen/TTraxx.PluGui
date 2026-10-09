using System.Text;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

/// <summary>
/// <para>
/// Dragging files out of the window on X11, as an XDND source (version 5): the pointer is grabbed,
/// and while it moves the window under it that takes XDND (XdndAware) is told the drag entered, where
/// it is, and that it left or was dropped; the files are offered as text/uri-list through the
/// XdndSelection selection, which the target asks for once it takes the drop.
/// </para>
/// <para>
/// Unlike Win32's, this drag doesn't block: the target is usually the host, in this very process
/// and on this very thread, and it can only answer once its event loop runs again. So the drag
/// starts, the call returns, and the rest happens in the window's event loop - the pointer's moves
/// and release, the target's XdndStatus and XdndFinished, its request for the files.
/// </para>
/// <para>
/// Windows can vanish while the pointer crosses them: the calls that name them run under X11Errors.
/// </para>
/// </summary>
internal sealed unsafe class X11FileDrag(nint display, nint window)
{
    private const int Version = 5;
    private const int GrabModeAsync = 1;
    private const int GrabSuccess = 0;
    private const uint DragEvents = (uint)(XlibConstants.ButtonReleaseMask | XlibConstants.PointerMotionMask);
    private const nuint AtomType = 4;   // XA_ATOM
    private const nuint WindowType = 33; // XA_WINDOW
    private const int PropModeReplace = 0;

    // From X's cursor font: a hand where the drop would be taken, a circle where it wouldn't.
    private const uint HandCursor = 60;   // XC_hand2
    private const uint RefuseCursor = 24; // XC_circle

    private readonly nuint _aware = Xlib.XInternAtom(display, "XdndAware", false);
    private readonly nuint _proxy = Xlib.XInternAtom(display, "XdndProxy", false);
    private readonly nuint _enter = Xlib.XInternAtom(display, "XdndEnter", false);
    private readonly nuint _position = Xlib.XInternAtom(display, "XdndPosition", false);
    private readonly nuint _status = Xlib.XInternAtom(display, "XdndStatus", false);
    private readonly nuint _leave = Xlib.XInternAtom(display, "XdndLeave", false);
    private readonly nuint _drop = Xlib.XInternAtom(display, "XdndDrop", false);
    private readonly nuint _finished = Xlib.XInternAtom(display, "XdndFinished", false);
    private readonly nuint _selection = Xlib.XInternAtom(display, "XdndSelection", false);
    private readonly nuint _copy = Xlib.XInternAtom(display, "XdndActionCopy", false);
    private readonly nuint _uriList = Xlib.XInternAtom(display, "text/uri-list", false);
    private readonly nuint _targets = Xlib.XInternAtom(display, "TARGETS", false);

    private byte[]? _files;   // the uri list on offer, kept until the next drag
    private bool _dragging;   // the pointer is grabbed
    private nint _target;     // the XDND window under the pointer, 0 for none
    private nint _sendTo;     // where its messages go: it, or its proxy
    private int _version;
    private bool _accepted;   // its last status
    private bool _awaitingStatus;
    private bool _positionPending;
    private int _pendingX, _pendingY;
    private nint _pendingTime;
    private nint _handCursor, _refuseCursor;

    public bool IsDragging => _dragging;

    /// <summary>Starts the drag: offers the files and grabs the pointer. False if it couldn't be grabbed (another grab is on).</summary>
    public bool Start(IReadOnlyList<string> paths, nint time)
    {
        if (_dragging) return false;

        var uris = new StringBuilder();
        foreach (var path in paths) uris.Append(new Uri(Path.GetFullPath(path)).AbsoluteUri).Append("\r\n");
        _files = Encoding.UTF8.GetBytes(uris.ToString());

        if (_handCursor == 0) _handCursor = Xlib.XCreateFontCursor(display, HandCursor);
        if (_refuseCursor == 0) _refuseCursor = Xlib.XCreateFontCursor(display, RefuseCursor);

        if (Xlib.XGrabPointer(display, window, false, DragEvents, GrabModeAsync, GrabModeAsync, 0, _refuseCursor, time) != GrabSuccess) return false;

        _ = Xlib.XSetSelectionOwner(display, _selection, window, time);
        _dragging = true;
        _target = 0;
        _accepted = false;
        _awaitingStatus = false;
        _positionPending = false;
        _ = Xlib.XFlush(display);
        return true;
    }

    /// <summary>The pointer moved to (<paramref name="x"/>, <paramref name="y"/>) on the screen.</summary>
    public void OnMotion(int x, int y, nint time)
    {
        if (!_dragging) return;

        var target = FindTarget(x, y, out var sendTo, out var version);
        if (target != _target)
        {
            if (_target != 0) Send(_sendTo, _target, _leave, (long)window, 0, 0, 0, 0);

            _target = target;
            _sendTo = sendTo;
            _version = Math.Min(version, Version);
            _accepted = false;
            _awaitingStatus = false;
            _positionPending = false;
            SetCursor();

            if (_target != 0) Send(_sendTo, _target, _enter, (long)window, (long)_version << 24, (long)_uriList, 0, 0);
        }

        if (_target == 0) return;

        if (_awaitingStatus)
        {
            // One position at a time: the newest waits for the target's answer to the last.
            (_pendingX, _pendingY, _pendingTime, _positionPending) = (x, y, time, true);
            return;
        }

        SendPosition(x, y, time);
    }

    /// <summary>The button came up: the drop, if the target takes it, else the drag just ends.</summary>
    public void OnRelease(nint time)
    {
        if (!_dragging) return;

        _dragging = false;
        _ = Xlib.XUngrabPointer(display, time);

        if (_target != 0)
        {
            if (_accepted) Send(_sendTo, _target, _drop, (long)window, 0, time, 0, 0);
            else Send(_sendTo, _target, _leave, (long)window, 0, 0, 0, 0);
        }

        _target = 0;
        _ = Xlib.XFlush(display);
    }

    /// <summary>A target's XdndStatus or XdndFinished; false for any other client message.</summary>
    public bool OnClientMessage(XClientMessageEvent* message)
    {
        if (message->message_type == _status)
        {
            if (!_dragging || (nint)message->l[0] != _target) return true;

            _awaitingStatus = false;
            _accepted = (message->l[1] & 1) != 0;
            SetCursor();
            if (_positionPending)
            {
                _positionPending = false;
                SendPosition(_pendingX, _pendingY, _pendingTime);
            }

            return true;
        }

        return message->message_type == _finished; // the target has the files: nothing left to do
    }

    /// <summary>A request for the files being dragged; false if it's for another selection.</summary>
    public bool OnSelectionRequest(in XSelectionRequestEvent request)
    {
        if (request.selection != _selection) return false;

        var property = request.property != 0 ? request.property : request.target;
        var answered = false;
        if (_files is { } files)
        {
            if (request.target == _targets)
            {
                var formats = stackalloc nuint[] { _targets, _uriList };
                _ = Xlib.XChangeProperty(display, request.requestor, property, AtomType, 32, PropModeReplace, formats, 2);
                answered = true;
            }
            else if (request.target == _uriList)
            {
                fixed (byte* data = files)
                {
                    _ = Xlib.XChangeProperty(display, request.requestor, property, _uriList, 8, PropModeReplace, data, files.Length);
                }

                answered = true;
            }
        }

        XEvent send = default;
        *(XSelectionEvent*)&send = new XSelectionEvent
        {
            type = XEventType.SelectionNotify,
            send_event = 1,
            display = display,
            requestor = request.requestor,
            selection = request.selection,
            target = request.target,
            property = answered ? property : 0,
            time = request.time,
        };
        _ = Xlib.XSendEvent(display, request.requestor, false, 0, &send);
        _ = Xlib.XFlush(display);
        return true;
    }

    /// <summary>Lets go of the pointer and the cursors - the window is going.</summary>
    public void Dispose()
    {
        if (_dragging) _ = Xlib.XUngrabPointer(display, 0);
        _dragging = false;
        if (_handCursor != 0) _ = Xlib.XFreeCursor(display, _handCursor);
        if (_refuseCursor != 0) _ = Xlib.XFreeCursor(display, _refuseCursor);
        _handCursor = _refuseCursor = 0;
    }

    private void SendPosition(int x, int y, nint time)
    {
        Send(_sendTo, _target, _position, (long)window, 0, ((long)x << 16) | (uint)(y & 0xFFFF), time, (long)_copy);
        _awaitingStatus = true;
    }

    private void SetCursor()
        => _ = Xlib.XChangeActivePointerGrab(display, DragEvents, _accepted ? _handCursor : _refuseCursor, 0);

    /// <summary>
    /// The XDND window under the screen point: from the root down, the first window that says it
    /// takes XDND - a toplevel, normally, which finds its own widget under the point. Not our own
    /// window, which doesn't. 0 for none.
    /// </summary>
    private nint FindTarget(int x, int y, out nint sendTo, out int version)
    {
        sendTo = 0;
        version = 0;

        var previous = X11Errors.Begin();
        try
        {
            var root = Xlib.XRootWindow(display, Xlib.XDefaultScreen(display));
            var current = root;
            for (var depth = 0; depth < 32; depth++)
            {
                if (Xlib.XTranslateCoordinates(display, root, current, x, y, out _, out _, out var child) == 0 || child == 0 || X11Errors.Errored) return 0;
                current = child;
                if (current == window) return 0;

                if (AwareVersion(current, out version, out var proxy))
                {
                    sendTo = proxy != 0 ? proxy : current;
                    return current;
                }
            }

            return 0;
        }
        finally
        {
            _ = X11Errors.End(display, previous);
        }
    }

    /// <summary>Whether a window takes XDND, and in which version; a proxy, if it names one, receives its messages.</summary>
    private bool AwareVersion(nint candidate, out int version, out nint proxy)
    {
        version = 0;
        proxy = 0;

        if (Xlib.XGetWindowProperty(display, candidate, _aware, 0, 1, false, AtomType, out _, out var format, out var count, out _, out var data) != 0 || data == null)
        {
            return false;
        }

        try
        {
            if (format != 32 || count < 1) return false;
            version = (int)*(nuint*)data;
        }
        finally
        {
            _ = Xlib.XFree(data);
        }

        if (version < 3) return false; // older than any target still around

        if (Xlib.XGetWindowProperty(display, candidate, _proxy, 0, 1, false, WindowType, out _, out format, out count, out _, out data) == 0 && data != null)
        {
            if (format == 32 && count >= 1) proxy = *(nint*)data;
            _ = Xlib.XFree(data);
        }

        return true;
    }

    private void Send(nint to, nint target, nuint type, long l0, long l1, long l2, long l3, long l4)
    {
        XEvent send = default;
        var message = (XClientMessageEvent*)&send;
        message->type = XEventType.ClientMessage;
        message->display = display;
        message->window = target;
        message->message_type = type;
        message->format = 32;
        message->l[0] = l0;
        message->l[1] = l1;
        message->l[2] = l2;
        message->l[3] = l3;
        message->l[4] = l4;
        var previous = X11Errors.Begin(); // the target may have gone
        _ = Xlib.XSendEvent(display, to, false, 0, &send);
        _ = X11Errors.End(display, previous);
    }
}
