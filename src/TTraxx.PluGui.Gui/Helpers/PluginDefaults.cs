
namespace TTraxx.PluGui.Gui;

/// <summary>
/// The theme and font set a new <see cref="RenderContext"/> starts from - i.e. what every window of
/// this plugin looks like unless it says otherwise. Set these once at startup, before the first
/// window is built.
///
/// These are DEFAULTS, not the values controls read while drawing: a control always goes through its
/// window's <see cref="RenderContext"/>, which may carry a theme of its own. That indirection is what
/// makes a per-window theme (a host-driven light/dark switch, say) possible at all.
/// </summary>
public static class PluginDefaults
{
    /// <summary>Default theme for new render contexts. Assign your plugin's theme here at startup.</summary>
    public static IPluginTheme Theme { get; set; } = new DefaultPluginTheme();

    /// <summary>Default font set for new render contexts. Leave as-is to use the built-in fonts.</summary>
    public static IPluginFonts Fonts { get; set; } = new DefaultPluginFonts();

    internal static void InvalidateFontCache()
    {
        // Only reset the built-in implementation - a plugin-supplied
        // IPluginFonts owns its own caching policy.
        if (Fonts is DefaultPluginFonts)
            Fonts = new DefaultPluginFonts();
    }
}
