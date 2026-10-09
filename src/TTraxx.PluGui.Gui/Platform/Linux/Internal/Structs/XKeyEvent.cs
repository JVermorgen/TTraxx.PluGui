using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>KeyPress / KeyRelease. keycode is the hardware key; XLookupString turns it into a keysym and text.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XKeyEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public nint root;
    public nint subwindow;
    public nint time;
    public int x;
    public int y;
    public int x_root;
    public int y_root;
    public uint state;
    public uint keycode;
    public int same_screen;
}

/// <summary>FocusIn / FocusOut. mode and detail say why: a grab, or focus moving within the window's own tree.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XFocusChangeEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public int mode;
    public int detail;
}

/// <summary>SelectionRequest: another client asks the selection's owner (us) for its contents as target, into property.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionRequestEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint owner;
    public nint requestor;
    public nuint selection;
    public nuint target;
    public nuint property;
    public nint time;
}

/// <summary>SelectionNotify: the answer to a conversion - property holds the data, or is None when it failed.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint requestor;
    public nuint selection;
    public nuint target;
    public nuint property;
    public nint time;
}

/// <summary>SelectionClear: another client took the selection we owned.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XSelectionClearEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public nuint selection;
    public nint time;
}
