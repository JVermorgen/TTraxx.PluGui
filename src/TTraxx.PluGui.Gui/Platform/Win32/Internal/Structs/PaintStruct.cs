using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PaintStruct
{
    internal nint hdc;

    [MarshalAs(UnmanagedType.Bool)]
    internal bool fErase;

    internal Rect rcPaint;

    [MarshalAs(UnmanagedType.Bool)]
    internal bool fRestore;

    [MarshalAs(UnmanagedType.Bool)]
    internal bool fIncUpdate;

    internal fixed byte rgbReserved[32];
}