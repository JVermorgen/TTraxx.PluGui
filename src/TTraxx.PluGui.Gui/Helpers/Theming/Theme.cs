using TTraxx.PluGui.Gui.Helpers.Theming.Interfaces;

namespace TTraxx.PluGui.Gui.Helpers.Theming;

public static class Theme
{
    public static IPluginTheme Current { get; set; } = new DefaultPluginTheme();

    public static IMetallicPanelTheme CurrentMetallic =>
        Current as IMetallicPanelTheme ?? DefaultMetallicPanelTheme.Instance;
}