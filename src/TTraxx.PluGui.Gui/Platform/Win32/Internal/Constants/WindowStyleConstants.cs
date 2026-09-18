namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

/// <summary>
/// Window class name and style bits used when creating the plugin's child window. Note that WS_*
/// and WS_EX_* live in SEPARATE flag spaces - the dwStyle and dwExStyle arguments of CreateWindowEx.
/// </summary>
internal static class WindowStyleConstants
{
    /// <summary>
    /// The predefined STATIC control class. The window is created from this and then subclassed, so no
    /// class of our own is registered inside the host process.
    /// </summary>
    public const string WC_STATIC = "STATIC";

    /// <summary>Child window - required to live inside the HWND the host supplies as parent.</summary>
    public const int WS_CHILD = 0x40000000;

    /// <summary>Visible from creation, so no separate ShowWindow call is needed.</summary>
    public const int WS_VISIBLE = 0x10000000;

    /// <summary>
    /// Makes a STATIC control report mouse input instead of swallowing it. Without this the window
    /// receives no clicks at all, so it is not optional here.
    /// </summary>
    public const int SS_NOTIFY = 0x0100;

    /// <summary>
    /// Prevents the window from taking activation when clicked, leaving focus with the host. Paired
    /// with the <see cref="WindowMessageConstants.MA_NOACTIVATE"/> reply.
    /// </summary>
    public const int WS_EX_NOACTIVATE = 0x08000000;
}
