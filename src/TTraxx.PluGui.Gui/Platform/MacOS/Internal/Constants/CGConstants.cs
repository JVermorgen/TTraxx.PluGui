namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;

internal static class CGConstants
{
    // kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst — the
    // standard combination for "BGRA, premultiplied" like Skia's
    // SKColorType.Bgra8888/SKAlphaType.Premul puts in memory.
    internal const uint BitmapInfoBgraPremultiplied = 0x2000 | 2;
}