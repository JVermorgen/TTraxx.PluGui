using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

/// <summary>Win32 POINT. Used with ScreenToClient, to bring a wheel event's screen position into client coordinates.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    /// <summary>Horizontal coordinate.</summary>
    internal int X;

    /// <summary>Vertical coordinate.</summary>
    internal int Y;
}
