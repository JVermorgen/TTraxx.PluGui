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
    /// <summary>The height, in design units, of the metal gradient's top and bottom bands.</summary>
    private const float MetalBandHeight = 48;

    /// <summary>
    /// Fills the body with the metal gradient and lays the sheen line just below the header band.
    /// Both are drawn square: the body clip set up by PluginPanel.Draw rounds the corners.
    ///
    /// The gradient's ends are bands of a fixed height (<see cref="MetalBandHeight"/>): the top colour
    /// fades over one into the colour halfway to the bottom one, which fills the middle, and that fades
    /// over the other into the bottom colour - so a short panel and a tall one look alike at their edges.
    /// </summary>
    protected override void DrawBody(SKCanvas canvas)
    {
        var top = MetallicTheme.PanelMetalTop;
        var bottom = MetallicTheme.PanelMetalBottom;
        SKColor middle = new((byte)((top.Red + bottom.Red) / 2), (byte)((top.Green + bottom.Green) / 2),
            (byte)((top.Blue + bottom.Blue) / 2), (byte)((top.Alpha + bottom.Alpha) / 2));
        var band = RescaleExact(MetalBandHeight);
        canvas.FillVerticalGradient(Left, Top, Width, Height, top, middle, bottom, band, band);

        using SKPaint sheenPaint = new()
        {
            Color = MetallicTheme.PanelMetalSheen,
            StrokeWidth = RescaleExact(0.2f),
            IsAntialias = true
        };
        canvas.DrawLine(FromLeft(8), FromTop(3), FromRight(8), FromTop(3), sheenPaint);
    }
}
