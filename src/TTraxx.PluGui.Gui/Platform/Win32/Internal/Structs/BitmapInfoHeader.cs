using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

/// <summary>
/// Win32 BITMAPINFOHEADER, describing the DIB section that backs the Skia surface. Field names and
/// order match the native struct exactly, since it's passed to CreateDIBSection by layout.
/// Populated by Gdi32.CreateDIBSection32 rather than by hand.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct BitmapInfoHeader
{
    /// <summary>Size of this struct in bytes. GDI uses it to tell header versions apart, so it must be set.</summary>
    public uint biSize;

    /// <summary>Bitmap width in pixels.</summary>
    public int biWidth;

    /// <summary>
    /// Bitmap height in pixels. NEGATIVE means a top-down DIB (row 0 is the top), which is what Skia
    /// expects; a positive value gives the DIB default of bottom-up and renders everything flipped.
    /// </summary>
    public int biHeight;      // negative = top-down (matches Skia's origin)

    /// <summary>Colour planes. Always 1 - the field predates packed-pixel formats.</summary>
    public ushort biPlanes;

    /// <summary>Bits per pixel; 32 here, to match the surface's BGRA layout.</summary>
    public ushort biBitCount;

    /// <summary>Compression mode. 0 (BI_RGB) means uncompressed.</summary>
    public uint biCompression;

    /// <summary>Image byte size. May be left 0 for an uncompressed bitmap.</summary>
    public uint biSizeImage;

    /// <summary>Horizontal resolution in pixels per metre. Unused for an off-screen surface.</summary>
    public int biXPelsPerMeter;

    /// <summary>Vertical resolution in pixels per metre. Unused for an off-screen surface.</summary>
    public int biYPelsPerMeter;

    /// <summary>Palette entries used. Irrelevant at 32bpp.</summary>
    public uint biClrUsed;

    /// <summary>Palette entries that are required. Irrelevant at 32bpp.</summary>
    public uint biClrImportant;
}
