using SkiaSharp;

namespace TTraxx.PluGui.Gui;

public interface IPluginTheme
{
    SKColor Background { get; }
    SKColor BackgroundHighlight { get; }
    SKColor BackgroundShadow { get; }

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

    SKColor MenuBackground { get; }
    SKColor MenuBorder { get; }
    SKColor MenuItemHoverBackground { get; }
    SKColor MenuTextPrimary { get; }
    SKColor MenuTextDisabled { get; }
}
