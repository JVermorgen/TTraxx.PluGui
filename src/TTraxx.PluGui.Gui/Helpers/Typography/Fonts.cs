using TTraxx.PluGui.Gui.Helpers.Typography.Interfaces;

namespace TTraxx.PluGui.Gui.Helpers.Typography;

public static class Fonts
{
    public static IPluginFonts Current { get; set; } = new DefaultPluginFonts();

    internal static void InvalidateCache()
    {
        // Only reset the built-in implementation - a plugin-supplied
        // IPluginFonts owns its own caching policy.
        if (Current is DefaultPluginFonts)
            Current = new DefaultPluginFonts();
    }
}