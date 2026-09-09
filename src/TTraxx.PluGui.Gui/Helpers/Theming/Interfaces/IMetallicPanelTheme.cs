using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers.Theming.Interfaces;

public interface IMetallicPanelTheme
{
    SKColor PanelMetalTop { get; }
    SKColor PanelMetalBottom { get; }
    SKColor PanelMetalBorder { get; }
    SKColor PanelMetalSheen { get; }
    SKColor PanelMetalHeaderHighlight { get; }
    SKColor PanelMetalHeaderShadow { get; }
}