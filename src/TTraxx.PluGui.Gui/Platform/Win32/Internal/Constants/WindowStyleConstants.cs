namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

internal static class WindowStyleConstants
{
    public const string WC_STATIC = "STATIC";
    public const int WS_CHILD = 0x40000000;
    public const int WS_VISIBLE = 0x10000000;
    public const int SS_NOTIFY = 0x0100;
    public const int WS_CLIPCHILDREN = 0x02000000;

    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_COMPOSITED = 0x02000000;
}