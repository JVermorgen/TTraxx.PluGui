using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// The full color palette the library's controls draw from. A plugin implements this once to
/// restyle every control at a stroke - controls never hardcode a color, they ask their window's
/// <see cref="RenderContext"/> for the theme.
///
/// The slots are named by ROLE, not by appearance ("TextDim", not "grey"), so a light theme is
/// just as implementable as a dark one. Implementing every member is required: there's no partial
/// theme, because a control silently falling back to a default color it wasn't designed for looks
/// worse than a palette that is obviously wrong. Start from
/// <see cref="DefaultPluginTheme"/> and adjust.
///
/// Panel metal shading is a separate, optional concern - see <see cref="IMetallicPanelTheme"/>,
/// which a theme may additionally implement.
/// </summary>
public interface IPluginTheme
{
    /// <summary>Window background base color.</summary>
    SKColor Background { get; }

    /// <summary>Lighter end of the window background gradient - conventionally the top.</summary>
    SKColor BackgroundHighlight { get; }

    /// <summary>Darker end of the window background gradient - conventionally the bottom.</summary>
    SKColor BackgroundShadow { get; }

    /// <summary>Primary accent: a knob's filled value arc, an active toggle.</summary>
    SKColor Accent { get; }

    /// <summary>Muted accent, for an accented element that should recede.</summary>
    SKColor AccentDim { get; }

    /// <summary>Secondary accent, to distinguish a second kind of active element from <see cref="Accent"/>.</summary>
    SKColor Accent2 { get; }

    /// <summary>Main label and readout text.</summary>
    SKColor TextPrimary { get; }

    /// <summary>Secondary text - a control's own label, where the value matters more than its name.</summary>
    SKColor TextDim { get; }

    /// <summary>Text of a disabled control.</summary>
    SKColor TextDisabled { get; }

    /// <summary>Unfilled part of a track or arc - the groove a value is drawn over.</summary>
    SKColor TrackBackground { get; }

    /// <summary>Border around an XY pad's field.</summary>
    SKColor XYPanelBorder { get; }

    /// <summary>Lighter end of an XY pad field's gradient.</summary>
    SKColor XYPanelBackgroundHighlight { get; }

    /// <summary>Darker end of an XY pad field's gradient.</summary>
    SKColor XYPanelBackgroundShadow { get; }

    /// <summary>Center of a glow - the brightest, most opaque stop. Used for an XY pad's dot.</summary>
    SKColor GlowCore { get; }

    /// <summary>Middle glow stop. Normally the core color at reduced alpha.</summary>
    SKColor GlowMid { get; }

    /// <summary>Outermost glow stop, fading toward transparent.</summary>
    SKColor GlowOuter { get; }

    /// <summary>Context menu background fill.</summary>
    SKColor MenuBackground { get; }

    /// <summary>Context menu border.</summary>
    SKColor MenuBorder { get; }

    /// <summary>Highlight behind the context menu item under the pointer.</summary>
    SKColor MenuItemHoverBackground { get; }

    /// <summary>Context menu text for an enabled item.</summary>
    SKColor MenuTextPrimary { get; }

    /// <summary>Context menu text for a disabled item.</summary>
    SKColor MenuTextDisabled { get; }
}
