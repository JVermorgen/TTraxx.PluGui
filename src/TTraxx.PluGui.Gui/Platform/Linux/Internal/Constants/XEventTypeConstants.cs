namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;

/// <summary>
/// Xlib event types that we handle. Full list is in X.h; this is
/// the subset that XSelectInput in LinuxPlatformWindow requests.
/// </summary>
internal static class XEventType
{
    internal const int ButtonPress = 4;
    internal const int ButtonRelease = 5;
    internal const int MotionNotify = 6;
    internal const int Expose = 12;
    internal const int ConfigureNotify = 22;
}
