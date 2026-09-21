using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// The secondary panel style: the same chrome as <see cref="MetallicPluginPanel"/> - same titled
/// header band, same hairline outline, same corner radius - with NO body fill, so whatever the
/// window painted behind it shows through the frame.
///
/// Use it for a group that has to read as subordinate to the metal panels around it: an outline
/// weighs less than a raised metal face, so a window full of metal boxes keeps a clear hierarchy
/// instead of every group shouting equally loudly. It's also the style to reach for when the panel
/// sits over something worth seeing - a background gradient, a logo, another panel's body.
///
/// There is no body to draw: a panel with no surface of its own IS the plain card that
/// PluginPanel.Draw already draws, and that shared card is what keeps this style's outline exactly
/// as tight as the metal one's.
///
/// Positioning, sizing and children work exactly as for the metal panel: the configured rectangle is
/// the BODY, the header band lives in the ~24 design units above it, and children are placed relative
/// to the body's top-left via <c>panel:</c> on PluginWindow.Place(). Construct one inside
/// BuildLayout() and register it with PluginWindow.RegisterPanel - never keep one in a field across
/// builds (see the note on IPluginPanel.AddControl).
/// </summary>
public sealed class TransparentPluginPanel(RenderContext context, PluginPanelConfiguration config) : PluginPanel(context, config)
{
    /// <summary>
    /// Square top corners, so the band lands flush on the body and the two share one outline. With no
    /// fill to hide an overlap, a tucked band would only leave a notch either side; aligning them is
    /// the cleaner fit.
    /// </summary>
    protected override bool RoundedBodyTop => false;
}
