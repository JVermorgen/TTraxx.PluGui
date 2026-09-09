using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers.Theming.Interfaces;

public interface IPluginTheme
{
    SKColor Accent { get; }
    SKColor AccentDim { get; }
    SKColor Accent2 { get; }

    SKColor TextPrimary { get; }
    SKColor TextDim { get; }
    SKColor TextDisabled { get; }

    SKColor TrackBackground { get; }

    SKColor XYPanelBorder { get; }
    SKColor XYPanelBackgroundHighlight { get; }
    SKColor XYPanelBackgroundShadow { get; }

    SKColor GlowCore { get; }
    SKColor GlowMid { get; }
    SKColor GlowOuter { get; }
}