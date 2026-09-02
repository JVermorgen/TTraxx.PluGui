using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>ConfigureNotify — arrives on window resize/move.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XConfigureEvent
{
    public int type;
    public nuint serial;
    public int send_event;
    public nint display;
    public nint eventWindow;
    public nint window;
    public int x;
    public int y;
    public int width;
    public int height;
    public int border_width;
    public nint above;
    public int override_redirect;
}