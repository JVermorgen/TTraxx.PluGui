namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;

/// <summary>Core Graphics bitmap flags for wrapping the Skia pixel buffer as a CGImage.</summary>
internal static class CGConstants
{
    /// <summary>
    /// kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst — the
    /// standard combination for "BGRA, premultiplied" like Skia's
    /// SKColorType.Bgra8888/SKAlphaType.Premul puts in memory. Describing the buffer with anything else
    /// swaps the channels rather than failing, so this must stay in step with the surface's format.
    /// </summary>
    internal const uint BitmapInfoBgraPremultiplied = 0x2000 | 2;
}
