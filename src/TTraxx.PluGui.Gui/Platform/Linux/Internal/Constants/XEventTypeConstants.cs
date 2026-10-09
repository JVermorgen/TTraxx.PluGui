namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;

/// <summary>
/// Xlib event types that we handle. Full list is in X.h; this is
/// the subset LinuxPlatformWindow handles: what XSelectInput requests, plus the
/// selection events X11 sends a selection owner or requestor unasked.
/// </summary>
internal static class XEventType
{
    internal const int KeyPress = 2;
    internal const int ButtonPress = 4;
    internal const int ButtonRelease = 5;
    internal const int MotionNotify = 6;
    internal const int FocusOut = 10;
    internal const int Expose = 12;
    internal const int ConfigureNotify = 22;
    internal const int SelectionClear = 29;
    internal const int SelectionRequest = 30;
    internal const int SelectionNotify = 31;
}
