using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>MotionNotify (mouse movement, also during drag).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XMotionEvent
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
    public byte is_hint;
    public int same_screen;
}