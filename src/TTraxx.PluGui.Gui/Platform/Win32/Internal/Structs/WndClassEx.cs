using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WndClassEx
{
    internal uint cbSize;
    internal uint style;

    internal delegate* unmanaged<nint, uint, nuint, nint, nint> lpfnWndProc;

    internal int cbClsExtra;
    internal int cbWndExtra;

    internal nint hInstance;
    internal nint hIcon;
    internal nint hCursor;
    internal nint hbrBackground;

    internal char* lpszMenuName;
    internal char* lpszClassName;

    internal nint hIconSm;
}