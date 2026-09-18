using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Extra colors for the brushed-metal panel look (see MetallicPluginPanel). Kept separate from
/// <see cref="IPluginTheme"/> because it belongs to ONE panel style: a theme for a plugin that
/// doesn't use metal panels shouldn't have to invent six colors it never draws.
///
/// A theme opts in by additionally implementing this interface;
/// <see cref="RenderContext.MetallicTheme"/> picks it up automatically and otherwise falls back to
/// <see cref="DefaultMetallicPanelTheme"/>.
/// </summary>
public interface IMetallicPanelTheme
{
    /// <summary>Lighter end of the panel's metal gradient - conventionally the top.</summary>
    SKColor PanelMetalTop { get; }

    /// <summary>Darker end of the panel's metal gradient.</summary>
    SKColor PanelMetalBottom { get; }

    /// <summary>Panel outline, brighter than the fill so the panel reads as raised.</summary>
    SKColor PanelMetalBorder { get; }

    /// <summary>Specular sheen across the metal. Normally near-white at low alpha.</summary>
    SKColor PanelMetalSheen { get; }

    /// <summary>Upper edge of the engraved panel title area.</summary>
    SKColor PanelMetalHeaderHighlight { get; }

    /// <summary>Lower edge of the engraved panel title area, completing the cut-in look.</summary>
    SKColor PanelMetalHeaderShadow { get; }
}
