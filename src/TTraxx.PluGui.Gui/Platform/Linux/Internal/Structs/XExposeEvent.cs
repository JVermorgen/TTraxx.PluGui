using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>Expose — part of the window needs to be redrawn.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XExposeEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint window;
    public int x;
    public int y;
    public int width;
    public int height;
    public int count;
}