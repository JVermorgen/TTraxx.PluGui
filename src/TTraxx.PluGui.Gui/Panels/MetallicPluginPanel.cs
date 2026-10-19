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

    /// <summary>How dark the bevel's shadow along the body's bottom edge gets.</summary>
    private const byte BevelShadowAlpha = 110;

    /// <summary>
    /// Fills the body with the metal gradient and bevels its edge. The fill is drawn square: the body
    /// clip set up by PluginPanel.Draw rounds the corners.
    ///
    /// The gradient's ends are bands of a fixed height (<see cref="MetalBandHeight"/>): the top colour
    /// fades over one into the colour halfway to the bottom one, which fills the middle, and that fades
    /// over the other into the bottom colour - so a short panel and a tall one look alike at their edges.
    ///
    /// The bevel, lit from above: along the body's edge the sheen colour at the top, fading out by a
    /// quarter of the way down, and a shadow along the bottom - so the face reads as raised.
    /// </summary>
    protected override void DrawBody(SKCanvas canvas)
    {
        var top = MetallicTheme.PanelMetalTop;
        var bottom = MetallicTheme.PanelMetalBottom;
        var middle = top.Towards(bottom, 0.5f);
        var band = RescaleExact(MetalBandHeight);
        canvas.FillVerticalGradient(Left, Top, Width, Height, top, middle, bottom, band, band);

        var stroke = RescaleExact(1);
        var sheen = MetallicTheme.PanelMetalSheen;
        using var bevel = SKShader.CreateLinearGradient(new SKPoint(Left, Top), new SKPoint(Left, Top + Height),
            [sheen, sheen.WithAlpha(0), SKColors.Black.WithAlpha(0), SKColors.Black.WithAlpha(BevelShadowAlpha)], [0, 0.25f, 0.75f, 1], SKShaderTileMode.Clamp);
        using SKPaint bevelPaint = new() { Shader = bevel, Style = SKPaintStyle.Stroke, StrokeWidth = stroke, IsAntialias = true };
        float radius = CornerRadius;
        canvas.DrawRoundRect(new SKRect(Left + (stroke / 2), Top + (stroke / 2), Left + Width - (stroke / 2), Top + Height - (stroke / 2)),
            radius - (stroke / 2), radius - (stroke / 2), bevelPaint);
    }
}
