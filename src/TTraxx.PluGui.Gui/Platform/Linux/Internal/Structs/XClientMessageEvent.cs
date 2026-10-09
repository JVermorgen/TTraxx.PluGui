using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>ClientMessage: a message one client sends another - XDND's are all these, five longs of data.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct XClientMessageEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public nuint message_type;
    public int format;
    public fixed long l[5]; // C's long: 64 bits on 64-bit Linux
}
