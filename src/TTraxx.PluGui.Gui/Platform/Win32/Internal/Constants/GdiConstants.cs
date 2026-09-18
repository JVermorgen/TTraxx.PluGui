namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

/// <summary>wingdi.h values needed by the one BitBlt that copies the finished Skia surface onto the window.</summary>
internal static class GdiConstants
{
    /// <summary>BitBlt raster op: copy the source rectangle verbatim.</summary>
    internal const int SRCCOPY = 0x00CC0020;
}
