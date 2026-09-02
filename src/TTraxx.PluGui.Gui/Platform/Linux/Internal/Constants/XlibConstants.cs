namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;

internal static class XlibConstants
{
    internal const nint ExposureMask = 1 << 15;
    internal const nint ButtonPressMask = 1 << 2;
    internal const nint ButtonReleaseMask = 1 << 3;
    internal const nint PointerMotionMask = 1 << 6;
    internal const nint StructureNotifyMask = 1 << 17;

    internal const int ZPixmap = 2;
}