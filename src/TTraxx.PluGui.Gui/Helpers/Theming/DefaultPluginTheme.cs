using SkiaSharp;
using TTraxx.PluGui.Gui.Helpers.Theming.Interfaces;

namespace TTraxx.PluGui.Gui.Helpers.Theming;

public sealed class DefaultPluginTheme : IPluginTheme
{
    public SKColor Accent => new(230, 230, 230);
    public SKColor AccentDim => new(100, 100, 100);
    public SKColor Accent2 => new(220, 255, 180);

    public SKColor TextPrimary => new(255, 255, 255);
    public SKColor TextDim => new(120, 120, 120);
    public SKColor TextDisabled => new(75, 75, 75);

    public SKColor TrackBackground => new(44, 44, 44);

    public SKColor XYPanelBorder => new(50, 50, 50);
    public SKColor XYPanelBackgroundHighlight => new(40, 40, 40);
    public SKColor XYPanelBackgroundShadow => new(20, 20, 20);

    public SKColor GlowCore => new(240, 240, 240, 255);
    public SKColor GlowMid => new(240, 240, 240, 110);
    public SKColor GlowOuter => new(240, 240, 240, 45);

    public SKColor MenuBackground => new(30, 30, 30);
    public SKColor MenuBorder => new(60, 60, 60);
    public SKColor MenuItemHoverBackground => new(230, 230, 230, 60);
    public SKColor MenuTextPrimary => new(255, 255, 255);
    public SKColor MenuTextDisabled => new(75, 75, 75);
}