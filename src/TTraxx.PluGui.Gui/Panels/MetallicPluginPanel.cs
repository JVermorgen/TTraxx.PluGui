using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// A "brushed metal" panel: a gradient-filled body with a sheen line across its top, under the
/// titled header band every panel style shares (see PluginPanel.Draw). The body keeps its rounded
/// top corners, with the band tucked in behind them.
///
/// The header band is drawn in the ~24 design units ABOVE the configured Top, so the body rectangle
/// you configure is not the full visual footprint - budget space overhead accordingly.
///
/// Construct one inside the window's BuildLayout() and hand it to PluginWindow.RegisterPanel, which
/// draws it each frame via DrawRegisteredPanels; see the note on IPluginPanel.AddControl for why a
/// panel shouldn't be held in a field across layout builds.
///
/// For a subordinate group inside - or next to - a metal panel, use
/// <see cref="TransparentPluginPanel"/>: same chrome, no metal face.
/// </summary>
public sealed class MetallicPluginPanel(RenderContext context, PluginPanelConfiguration config) : PluginPanel(context, config)
{
    /// <summary>
    /// Fills the body with the metal gradient and lays the sheen line just below the header band.
    /// Both are drawn square: the body clip set up by PluginPanel.Draw rounds the corners.
    /// </summary>
    protected override void DrawBody(SKCanvas canvas)
    {
        canvas.FillVerticalGradient(Left, Top, Width, Height, MetallicTheme.PanelMetalTop, MetallicTheme.PanelMetalBottom);

        using SKPaint sheenPaint = new()
        {
            Color = MetallicTheme.PanelMetalSheen,
            StrokeWidth = RescaleExact(0.2f),
            IsAntialias = true
        };
        canvas.DrawLine(FromLeft(8), FromTop(3), FromRight(8), FromTop(3), sheenPaint);
    }
}
