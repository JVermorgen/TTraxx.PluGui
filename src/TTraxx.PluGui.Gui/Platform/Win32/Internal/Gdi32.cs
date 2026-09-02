using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

internal static partial class Gdi32
{
    [LibraryImport("gdi32.dll")]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    public static partial nint SelectObject(nint hdc, nint hgdiobj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint hObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BitBlt(nint hdcDest, int xDest, int yDest, int w, int h, nint hdcSrc, int xSrc, int ySrc, int rop);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    private static partial nint CreateDIBSection(nint hdc, ref BitmapInfoHeader pbmi, uint usage, out nint ppvBits, nint hSection, uint offset);

    public static nint CreateDIBSection32(nint hdc, int width, int height, out nint bits)
    {
        BitmapInfoHeader bmi = new()
        {
            biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
            biWidth = width,
            biHeight = -height, // top-down: row 0 = top, matches SKSurface expectation
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0 // BI_RGB
        };
        return CreateDIBSection(hdc, ref bmi, 0 /* DIB_RGB_COLORS */, out bits, nint.Zero, 0);
    }
}