using System.Text;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

/// <summary>
/// <para>
/// Files dropped onto the window on X11, as an XDND target (version 5): the window says it takes
/// XDND (XdndAware), answers each position with whether the control there takes files, and on the
/// drop asks the source for its text/uri-list, then hands the local paths to the control.
/// </para>
/// <para>
/// Whether a drag ever gets here is the source's choice: XDND looks for its target among the
/// toplevel windows, and many sources (GTK's, so GNOME Files) stop at the host's window and never
/// look inside it for ours. Qt's look for the deepest window that takes XDND, and find it.
/// </para>
/// </summary>
internal sealed unsafe class X11FileDrop
{
    private const int Version = 5;
    private const nuint AtomType = 4; // XA_ATOM
    private const int PropModeReplace = 0;

    private readonly nint _display;
    private readonly nint _window;
    private readonly nuint _enter, _position, _status, _leave, _drop, _finished, _selection, _copy, _typeList, _uriList, _transfer;

    private nint _source;      // the window dragging over us, 0 for none
    private bool _offersFiles; // it offers text/uri-list
    private bool _accepted;    // our last answer
    private int _x, _y;        // where it is, in our coordinates
    private bool _awaitingFiles;

    public X11FileDrop(nint display, nint window)
    {
        _display = display;
        _window = window;
        _enter = Atom("XdndEnter");
        _position = Atom("XdndPosition");
        _status = Atom("XdndStatus");
        _leave = Atom("XdndLeave");
        _drop = Atom("XdndDrop");
        _finished = Atom("XdndFinished");
        _selection = Atom("XdndSelection");
        _copy = Atom("XdndActionCopy");
        _typeList = Atom("XdndTypeList");
        _uriList = Atom("text/uri-list");
        _transfer = Atom("PLUGUI_XDND");

        nuint version = Version;
        _ = Xlib.XChangeProperty(display, window, Atom("XdndAware"), AtomType, 32, PropModeReplace, &version, 1);
    }

    /// <summary>A source's XdndEnter, XdndPosition, XdndLeave or XdndDrop; false for any other client message.</summary>
    public bool OnClientMessage(XClientMessageEvent* message, IPlatformWindowHost? host)
    {
        var type = message->message_type;
        if (type == _enter)
        {
            _source = (nint)message->l[0];
            _offersFiles = (message->l[1] & 1) != 0 ? TypeListOffersFiles(_source) : (nuint)message->l[2] == _uriList || (nuint)message->l[3] == _uriList || (nuint)message->l[4] == _uriList;
            _accepted = false;
            return true;
        }

        if (type == _position)
        {
            _source = (nint)message->l[0];
            var rootX = (int)((message->l[2] >> 16) & 0xFFFF);
            var rootY = (int)(message->l[2] & 0xFFFF);
            var root = Xlib.XRootWindow(_display, Xlib.XDefaultScreen(_display));
            _ = Xlib.XTranslateCoordinates(_display, root, _window, rootX, rootY, out _x, out _y, out _);

            _accepted = _offersFiles && host is not null && host.CanDropFilesAt(_x, _y);

            // Bit 1: send every position, even where the answer stays the same - it's per control.
            Send(_source, _status, (long)_window, (_accepted ? 1 : 0) | 2, 0, 0, _accepted ? (long)_copy : 0);
            return true;
        }

        if (type == _leave)
        {
            _source = 0;
            _accepted = false;
            return true;
        }

        if (type == _drop)
        {
            _source = (nint)message->l[0];
            if (_accepted)
            {
                _ = Xlib.XConvertSelection(_display, _selection, _uriList, _transfer, _window, (nint)message->l[2]);
                _ = Xlib.XFlush(_display);
                _awaitingFiles = true;
            }
            else
            {
                Finish(false);
            }

            return true;
        }

        return false;
    }

    /// <summary>The source's answer with the files; false if it's an answer to something else.</summary>
    public bool OnSelectionNotify(in XSelectionEvent notify, IPlatformWindowHost? host)
    {
        if (notify.selection != _selection || !_awaitingFiles) return false;

        _awaitingFiles = false;
        var paths = notify.property != 0 ? ReadPaths() : [];
        if (paths.Count > 0) host?.OnFilesDropped(_x, _y, paths);
        Finish(paths.Count > 0);
        return true;
    }

    private List<string> ReadPaths()
    {
        var paths = new List<string>();
        if (Xlib.XGetWindowProperty(_display, _window, _transfer, 0, 1 << 20, true, 0, out _, out var format, out var count, out _, out var data) != 0 || data == null)
        {
            return paths;
        }

        try
        {
            if (format != 8) return paths;

            // One URI a line; a "#" line is a comment. Only local files (file://) make sense here.
            var text = Encoding.UTF8.GetString(data, (int)count);
            foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (line.StartsWith('#')) continue;
                if (Uri.TryCreate(line, UriKind.Absolute, out var uri) && uri.IsFile) paths.Add(uri.LocalPath);
            }
        }
        finally
        {
            _ = Xlib.XFree(data);
        }

        return paths;
    }

    /// <summary>Whether a source offering more than three types has text/uri-list among them (its XdndTypeList).</summary>
    private bool TypeListOffersFiles(nint source)
    {
        var previous = X11Errors.Begin(); // the source may have gone
        var failed = Xlib.XGetWindowProperty(_display, source, _typeList, 0, 64, false, AtomType, out _, out var format, out var count, out _, out var data) != 0;
        if (X11Errors.End(_display, previous) || failed || data == null)
        {
            if (data != null) _ = Xlib.XFree(data);
            return false;
        }

        try
        {
            if (format != 32) return false;
            for (var index = 0; index < (int)count; index++)
            {
                if (((nuint*)data)[index] == _uriList) return true;
            }

            return false;
        }
        finally
        {
            _ = Xlib.XFree(data);
        }
    }

    private void Finish(bool taken)
    {
        if (_source != 0) Send(_source, _finished, (long)_window, taken ? 1 : 0, taken ? (long)_copy : 0, 0, 0);
        _source = 0;
        _accepted = false;
    }

    private void Send(nint to, nuint type, long l0, long l1, long l2, long l3, long l4)
    {
        XEvent send = default;
        var message = (XClientMessageEvent*)&send;
        message->type = XEventType.ClientMessage;
        message->display = _display;
        message->window = to;
        message->message_type = type;
        message->format = 32;
        message->l[0] = l0;
        message->l[1] = l1;
        message->l[2] = l2;
        message->l[3] = l3;
        message->l[4] = l4;
        var previous = X11Errors.Begin(); // the source may have gone
        _ = Xlib.XSendEvent(_display, to, false, 0, &send);
        _ = X11Errors.End(_display, previous);
    }

    private nuint Atom(string name) => Xlib.XInternAtom(_display, name, false);
}
