using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfoHeader
{
    public uint biSize;
    public int biWidth;
    public int biHeight;      // negative = top-down (matches Skia's origin)
    public ushort biPlanes;
    public ushort biBitCount;
    public uint biCompression;
    public uint biSizeImage;
    public int biXPelsPerMeter;
    public int biYPelsPerMeter;
    public uint biClrUsed;
    public uint biClrImportant;
}
