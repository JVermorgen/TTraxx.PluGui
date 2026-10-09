using System.Text;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

/// <summary>
/// <para>
/// The keyboard and the clipboard for LinuxPlatformWindow, X11's counterparts of Win32's Keyboard.
/// </para>
/// <para>
/// Keys: the window takes the X input focus while a control edits text, so the key presses come to
/// it rather than to the host. XLookupString turns one into a keysym through the layout, Shift, Caps
/// Lock and AltGr included; the keysym gives both the key (arrows, Backspace) and the text. There's no
/// input method (XIM): it would need the process's locale, which is the host's to set. So dead keys
/// don't compose - "^" then "e" makes "^e", not "ê"; keys that type an accented letter directly do work.
/// </para>
/// <para>
/// Clipboard: X11's CLIPBOARD selection. Reading asks its owner to convert it to UTF-8 text and waits
/// briefly for the answer. Copying makes the window the owner and answers other clients' requests from
/// the event loop; the text goes when the window does, unless a clipboard manager took a copy.
/// </para>
/// </summary>
internal sealed unsafe class X11Keyboard(nint display, nint window, nint parent)
{
    private const int RevertToParent = 2;
    private const int PropModeReplace = 0;
    private const nuint AtomType = 4;    // XA_ATOM
    private const nuint StringType = 31; // XA_STRING: Latin-1 text

    // How long a paste waits for the clipboard's owner to answer.
    private const int ClipboardTimeoutMilliseconds = 500;

    private readonly nuint _clipboard = Xlib.XInternAtom(display, "CLIPBOARD", false);
    private readonly nuint _utf8 = Xlib.XInternAtom(display, "UTF8_STRING", false);
    private readonly nuint _text = Xlib.XInternAtom(display, "TEXT", false);
    private readonly nuint _targets = Xlib.XInternAtom(display, "TARGETS", false);
    private readonly nuint _transfer = Xlib.XInternAtom(display, "PLUGUI_CLIPBOARD", false);

    private nint _focusBefore;
    private string? _ownedText;

    // ---------------------------------------------------------------- focus

    /// <summary>
    /// Takes the X input focus (true), or gives it back (false) - to what had it, if that's one of the
    /// window's ancestors (the host's frame), else to the parent. Only to a window known to exist:
    /// focusing a destroyed one is an X error, which by default ends the whole process.
    /// </summary>
    public void SetFocus(bool focused)
    {
        _ = Xlib.XGetInputFocus(display, out var current, out _);
        if (focused)
        {
            if (current == window) return;

            _focusBefore = current;
            _ = Xlib.XSetInputFocus(display, window, RevertToParent, 0);
        }
        else
        {
            if (current != window) return;

            var back = IsAncestor(_focusBefore) ? _focusBefore : parent;
            _focusBefore = 0;
            _ = Xlib.XSetInputFocus(display, back, RevertToParent, 0);
        }

        _ = Xlib.XFlush(display);
    }

    /// <summary>
    /// Whether a FocusOut means the window really lost the keyboard: not one sent while a menu or
    /// drag grabs it for a moment (mode NotifyGrab/NotifyUngrab), nor focus moving between our own
    /// windows (detail NotifyInferior), nor one about the pointer's window (NotifyPointer).
    /// </summary>
    public static bool IsFocusLost(in XFocusChangeEvent focusOut) => focusOut.mode is 0 or 3 && focusOut.detail is not (2 or 5);

    private bool IsAncestor(nint candidate)
    {
        if (candidate is 0 or 1) return false; // None, PointerRoot

        var current = window;
        for (var depth = 0; depth < 64; depth++)
        {
            if (Xlib.XQueryTree(display, current, out var root, out var up, out var children, out _) == 0) return false;
            if (children != null) _ = Xlib.XFree(children);
            if (up == candidate) return true;
            if (up == 0 || up == root) return false;
            current = up;
        }

        return false;
    }

    // ---------------------------------------------------------------- keys

    /// <summary>A key press as a key (Key.None for one that's only text) and the text it types (null for none).</summary>
    public static (Key Key, string? Text) Read(XKeyEvent* keyEvent)
    {
        var buffer = stackalloc byte[32];
        var length = Xlib.XLookupString(keyEvent, buffer, 32, out var keysym, 0);

        var key = KeyOf(keysym);
        if (key != Key.None && key is not (Key.A or Key.C or Key.V or Key.X)) return (key, null);

        // Ctrl+letter is a shortcut, not text.
        if ((keyEvent->state & XlibConstants.ControlMask) != 0) return (key, null);

        var text = TextOf(keysym);
        if (text is null && length == 1 && buffer[0] is >= 0x20 and < 0x7F) text = ((char)buffer[0]).ToString();
        return (key, text);
    }

    public static KeyModifiers ModifiersOf(uint state)
    {
        var modifiers = KeyModifiers.None;
        if ((state & XlibConstants.ShiftMask) != 0) modifiers |= KeyModifiers.Shift;
        if ((state & XlibConstants.ControlMask) != 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }

    // Keysyms, from X11's keysymdef.h; the keypad's own, for when Num Lock is off.
    private static Key KeyOf(nuint keysym) => keysym switch
    {
        0xFF08 => Key.Backspace,
        0xFF09 or 0xFE20 => Key.Tab,         // Tab, ISO_Left_Tab (Shift+Tab)
        0xFF0D or 0xFF8D => Key.Enter,       // Return, KP_Enter
        0xFF1B => Key.Escape,
        0xFFFF or 0xFF9F => Key.Delete,
        0xFF50 or 0xFF95 => Key.Home,
        0xFF57 or 0xFF9C => Key.End,
        0xFF51 or 0xFF96 => Key.Left,
        0xFF52 or 0xFF97 => Key.Up,
        0xFF53 or 0xFF98 => Key.Right,
        0xFF54 or 0xFF99 => Key.Down,
        'a' or 'A' => Key.A,
        'c' or 'C' => Key.C,
        'v' or 'V' => Key.V,
        'x' or 'X' => Key.X,
        _ => Key.None,
    };

    // A keysym's character: Latin-1 keysyms are their own code, Unicode ones carry it plus
    // 0x01000000, and the currency signs (the euro among them) sit at their Unicode values too.
    private static string? TextOf(nuint keysym)
    {
        int? code = keysym switch
        {
            >= 0x20 and <= 0x7E or >= 0xA0 and <= 0xFF => (int)keysym,
            >= 0x20A0 and <= 0x20AC => (int)keysym,
            >= 0x01000100 and <= 0x0110FFFF => (int)(keysym - 0x01000000),
            _ => null,
        };

        return code is int value && value is < 0xD800 or > 0xDFFF && !char.IsControl((char)Math.Min(value, char.MaxValue))
            ? char.ConvertFromUtf32(value)
            : null;
    }

    // ---------------------------------------------------------------- clipboard

    /// <summary>The clipboard's text: ours if we own it, else converted by its owner. Null when there's none.</summary>
    public string? GetClipboardText()
    {
        var owner = Xlib.XGetSelectionOwner(display, _clipboard);
        if (owner == 0) return null;
        if (owner == window) return _ownedText;

        _ = Xlib.XConvertSelection(display, _clipboard, _utf8, _transfer, window, 0);
        _ = Xlib.XFlush(display);

        // Waits for the owner's answer only - every other event stays queued for the event loop.
        XEvent answer;
        var waited = 0;
        while (Xlib.XCheckTypedWindowEvent(display, window, XEventType.SelectionNotify, &answer) == 0)
        {
            if (waited >= ClipboardTimeoutMilliseconds) return null;
            Thread.Sleep(5);
            waited += 5;
        }

        var notify = (XSelectionEvent*)&answer;
        if (notify->property == 0) return null; // the owner has no text

        if (Xlib.XGetWindowProperty(display, window, _transfer, 0, 1 << 20, true, 0,
                out var type, out var format, out var count, out _, out var data) != 0 || data == null)
        {
            return null;
        }

        try
        {
            // An INCR answer (data sent in parts, for very large text) isn't followed: not for a name.
            if (format != 8 || (type != _utf8 && type != StringType)) return null;

            var bytes = new ReadOnlySpan<byte>(data, (int)count);
            return type == StringType ? Encoding.Latin1.GetString(bytes) : Encoding.UTF8.GetString(bytes);
        }
        finally
        {
            _ = Xlib.XFree(data);
        }
    }

    /// <summary>Puts text on the clipboard: the window becomes the CLIPBOARD's owner and serves it on request.</summary>
    public void SetClipboardText(string text)
    {
        _ownedText = text;
        _ = Xlib.XSetSelectionOwner(display, _clipboard, window, 0);
        _ = Xlib.XFlush(display);
    }

    /// <summary>Another client took the clipboard: ours is no longer the text on it.</summary>
    public void OnSelectionClear(in XSelectionClearEvent clear)
    {
        if (clear.selection == _clipboard) _ownedText = null;
    }

    /// <summary>
    /// Answers a client asking for the clipboard we own: the list of formats (TARGETS), or the text as
    /// UTF-8 (UTF8_STRING, TEXT) or Latin-1 (STRING) - anything else is refused.
    /// </summary>
    public void OnSelectionRequest(in XSelectionRequestEvent request)
    {
        // An old client may name no property; the target then serves as one.
        var property = request.property != 0 ? request.property : request.target;
        var answered = false;

        if (request.selection == _clipboard && _ownedText is { } text)
        {
            if (request.target == _targets)
            {
                // Format 32 means C longs: on 64-bit Linux an atom per 8 bytes, as nuint is.
                var formats = stackalloc nuint[] { _targets, _utf8, _text, StringType };
                _ = Xlib.XChangeProperty(display, request.requestor, property, AtomType, 32, PropModeReplace, formats, 4);
                answered = true;
            }
            else if (request.target == _utf8 || request.target == _text || request.target == StringType)
            {
                var latin1 = request.target == StringType;
                var bytes = latin1 ? Encoding.Latin1.GetBytes(text) : Encoding.UTF8.GetBytes(text);
                fixed (byte* data = bytes)
                {
                    _ = Xlib.XChangeProperty(display, request.requestor, property, latin1 ? StringType : _utf8, 8, PropModeReplace, data, bytes.Length);
                }

                answered = true;
            }
        }

        var notify = new XSelectionEvent
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

        // Into a full-size event: XSendEvent copies a whole XEvent.
        XEvent send = default;
        *(XSelectionEvent*)&send = notify;
        _ = Xlib.XSendEvent(display, request.requestor, false, 0, &send);
        _ = Xlib.XFlush(display);
    }
}
