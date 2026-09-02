using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    internal int Left, Top, Right, Bottom;
}