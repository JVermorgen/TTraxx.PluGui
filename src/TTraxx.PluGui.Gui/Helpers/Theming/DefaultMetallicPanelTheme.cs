using SkiaSharp;
using TTraxx.PluGui.Gui.Helpers.Theming.Interfaces;

namespace TTraxx.PluGui.Gui.Helpers.Theming;

public sealed class DefaultMetallicPanelTheme : IMetallicPanelTheme
{
    public static readonly DefaultMetallicPanelTheme Instance = new();

    public SKColor PanelMetalTop => new(80, 80, 80);
    public SKColor PanelMetalBottom => new(45, 45, 45);
    public SKColor PanelMetalBorder => new(120, 120, 120);
    public SKColor PanelMetalSheen => new(255, 255, 255, 90);
    public SKColor PanelMetalHeaderHighlight => new(35, 35, 35);
    public SKColor PanelMetalHeaderShadow => new(12, 12, 12);
}