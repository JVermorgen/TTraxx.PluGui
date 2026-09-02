using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>ButtonPress / ButtonRelease. button: 1=left, 2=middle, 3=right, 4/5=scrollwheel up/down.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XButtonEvent
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
    public uint button;
    public int same_screen;
}