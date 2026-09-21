using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Shared geometry and theming for panel chrome. The configuration is in unscaled design units;
/// every accessor here returns pixels at <paramref name="context"/>'s scale.
///
/// The base draws the whole titled card - header band, outline, title - and a concrete style only
/// fills the body (<see cref="DrawBody"/>). A style builds its geometry from the scaled
/// <see cref="Left"/>/<see cref="Top"/>/<see cref="Width"/>/<see cref="Height"/> accessors and the
/// <see cref="FromLeft"/>/<see cref="FromRight"/>/<see cref="FromTop"/> helpers - never from the raw
/// configuration, which is unscaled. See <see cref="MetallicPluginPanel"/> for a worked example.
/// </summary>
/// <param name="config">Position and size, in design units.</param>
/// <param name="context">The owning window's context - pass the window's <c>Context</c>.</param>
public abstract class PluginPanel(RenderContext context, PluginPanelConfiguration config) : IPluginPanel
{
    /// <summary>
    /// This panel's unscaled configuration. Read <see cref="PluginPanelConfiguration.Title"/> and the
    /// like from here, but use the scaled accessors below for geometry.
    /// </summary>
    protected readonly PluginPanelConfiguration _config = config;

    /// <summary>Scale, theme and fonts of the window this panel belongs to.</summary>
    protected RenderContext Context { get; } = context;

    /// <summary>Body height in pixels.</summary>
    public int Height => Context.Rescale(_config.Height);

    /// <summary>Body left edge in pixels, window-relative.</summary>
    public int Left => Context.Rescale(_config.Left);

    /// <summary>Body top edge in pixels, window-relative. A header, if the style draws one, sits above this.</summary>
    public int Top => Context.Rescale(_config.Top);

    /// <summary>Body width in pixels.</summary>
    public int Width => Context.Rescale(_config.Width);

    /// <summary>Corner rounding in pixels.</summary>
    public int CornerRadius => Context.Rescale(_config.CornerRadius);

    private readonly List<ControlPlacement> _controls = [];

    /// <inheritdoc/>
    public IReadOnlyList<ControlPlacement> Controls => _controls;

    /// <inheritdoc/>
    /// <remarks>
    /// The offset is applied in UNSCALED design units - the placement returned here is rescaled later,
    /// when the window calls PluginControl.SetBounds on it. So this must not use the scaled
    /// <see cref="Left"/>/<see cref="Top"/> accessors, or the offset would be scaled twice.
    /// </remarks>
    public ControlPlacement AddControl(PluginControl control, int relativeX, int relativeY, int width, int height)
    {
        ControlPlacement placed = new(control, _config.Left + relativeX, _config.Top + relativeY, width, height);
        _controls.Add(placed);
        return placed;
    }
    /// <summary>
    /// Draws the panel's chrome: the titled header band, the style's body surface
    /// (<see cref="DrawBody"/>), and the outline around both.
    ///
    /// How the two meet is decided by <see cref="RoundedBodyTop"/>, and the drawing ORDER follows from
    /// it. With a rounded body top the band is drawn first and reaches past the body's top edge, so the
    /// body's fill hides its lower half; with a square one the band stops at that edge and is stroked
    /// alongside the body, sides lining up into one unbroken outline.
    ///
    /// A style normally overrides <see cref="DrawBody"/> and leaves this alone; override this only for
    /// chrome that isn't a titled card at all.
    /// </summary>
    public virtual void Draw(SKCanvas canvas)
    {
        var roundedTop = RoundedBodyTop;

        using var band = BandShape(roundedTop);
        using var body = BodyShape(roundedTop);
        using SKPaint borderPaint = new()
        {
            Color = MetallicTheme.PanelMetalBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = RescaleExact(0.3f),
            IsAntialias = true
        };

        DrawHeaderBand(canvas, band);

        // A tucked band is outlined BEFORE the body, which then paints over the part of that outline
        // that reaches into it - the band keeps its rounded top and loses its bottom, with no clipping
        // and no stroke ending in mid-air.
        if (roundedTop) canvas.DrawRoundRect(band, borderPaint);

        // The body surface is drawn square and gets its corners from this clip, so a style can't round
        // a corner the outline doesn't, or leave a sliver of background inside one.
        canvas.Save();
        canvas.ClipRoundRect(body, SKClipOperation.Intersect, antialias: true);
        DrawBody(canvas);
        canvas.Restore();

        if (!roundedTop) canvas.DrawRoundRect(band, borderPaint);
        canvas.DrawRoundRect(body, borderPaint);

        DrawTitle(canvas);
    }

    /// <summary>
    /// Whether the body's top corners are rounded - and with them, how the header band meets the body.
    /// One knob decides both because the two only work in pairs.
    ///
    /// ROUNDED (the default, and the metal look): the corners curve away from the band's sides, which
    /// have nothing to land on, so the band tucks under the body instead and the body's fill covers
    /// what overlaps. The corners read as a card sitting under a title plate, at the cost of a small
    /// notch either side of the band.
    ///
    /// SQUARE: band and body share one straight edge, their sides line up, and the outline runs
    /// unbroken from the band's rounded top around the whole body - the tidier fit when there's no
    /// body fill to cover an overlap anyway.
    /// </summary>
    protected virtual bool RoundedBodyTop => true;


    /// <summary>
    /// Fills the panel's body - the style's actual surface, and the one thing a panel style has to
    /// say for itself. No-op by default, which is exactly a transparent panel.
    ///
    /// Draw with SQUARE corners over <see cref="Left"/>/<see cref="Top"/>/<see cref="Width"/>/
    /// <see cref="Height"/>: <see cref="Draw"/> has already clipped the canvas to the body's rounded
    /// outline, so the corners come out right without every style having to round them itself.
    /// </summary>
    protected virtual void DrawBody(SKCanvas canvas) { }

    /// <summary>Scales a design-unit length to whole pixels, at this panel's window scale.</summary>
    protected int Rescale(float designUnits) => Context.Rescale(designUnits);

    /// <summary>Scales a design-unit length without rounding, at this panel's window scale.</summary>
    protected float RescaleExact(float designUnits) => Context.RescaleExact(designUnits);

    /// <summary>This panel's window theme.</summary>
    protected IPluginTheme Theme => Context.Theme;

    /// <summary>The metallic-panel colors of this panel's window theme.</summary>
    protected IMetallicPanelTheme MetallicTheme => Context.MetallicTheme;

    /// <summary>This panel's window fonts.</summary>
    protected IPluginFonts Fonts => Context.Fonts;

    /// <summary>
    /// Absolute X, <paramref name="pixels"/> design units in from the panel's left edge. Inset helpers
    /// like this one keep a panel's drawing code readable at any scale: the numbers written down stay
    /// the design-unit insets, with the scaling and the panel's own origin folded in here.
    /// </summary>
    protected float FromLeft(int pixels) => Left + Context.RescaleExact(pixels);

    /// <summary>Absolute X, <paramref name="pixels"/> design units in from the panel's RIGHT edge.</summary>
    protected float FromRight(int pixels) => Left + Width - Context.RescaleExact(pixels);

    /// <summary>
    /// Absolute Y, <paramref name="pixels"/> design units down from the panel's top edge. Pass a
    /// NEGATIVE value to reach into the header band.
    /// </summary>
    protected float FromTop(int pixels) => Top + Context.RescaleExact(pixels);

    /// <summary>
    /// Design-unit length of the header band's gradient ramp. Longer than the band itself on purpose:
    /// the band shows the TOP of the ramp, which is what makes it read as a lit edge rather than an
    /// evenly shaded strip.
    /// </summary>
    private const int HeaderGradientHeight = 34;

    /// <summary>How far, in design units, the title header band reaches ABOVE the body's top edge.</summary>
    protected const int HeaderOverhang = 24;

    /// <summary>
    /// The header band, rounded on top and shaped at the bottom to match the body it meets (see
    /// <see cref="RoundedBodyTop"/>): it either reaches PAST the body's top edge, to be tucked under
    /// the body's fill, or stops square on that edge and continues the body's sides.
    /// </summary>
    private SKRoundRect BandShape(bool tuckedUnderBody)
    {
        float radius = CornerRadius;
        var bandTop = FromTop(-HeaderOverhang);

        if (tuckedUnderBody)
            return new SKRoundRect(new SKRect(Left, bandTop, Left + Width, bandTop + RescaleExact(HeaderGradientHeight)), radius, radius);

        SKRoundRect shape = new();
        shape.SetRectRadii(new SKRect(Left, bandTop, Left + Width, Top),
            [new SKPoint(radius, radius), new SKPoint(radius, radius), SKPoint.Empty, SKPoint.Empty]);
        return shape;
    }

    /// <summary>
    /// The body: the configured rectangle, always rounded at the bottom, rounded on top only when
    /// <paramref name="roundTop"/> - square there is what lets the band land flush on it.
    /// </summary>
    private SKRoundRect BodyShape(bool roundTop)
    {
        float radius = CornerRadius;
        var top = roundTop ? new SKPoint(radius, radius) : SKPoint.Empty;

        SKRoundRect shape = new();
        shape.SetRectRadii(new SKRect(Left, Top, Left + Width, Top + Height),
            [top, top, new SKPoint(radius, radius), new SKPoint(radius, radius)]);
        return shape;
    }

    /// <summary>
    /// Fills the engraved title band. The gradient runs its full <see cref="HeaderGradientHeight"/>
    /// while the band shows only the top of it, so the shading the look is built on survives the band
    /// being shorter than the ramp. Filled for a see-through style too: it's what keeps the title
    /// legible against whatever the window paints behind the panel.
    /// </summary>
    private void DrawHeaderBand(SKCanvas canvas, SKRoundRect band)
    {
        var bandTop = band.Rect.Top;
        var bandLeft = band.Rect.Left;

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(bandLeft, bandTop), new SKPoint(bandLeft, bandTop + RescaleExact(HeaderGradientHeight)),
            [MetallicTheme.PanelMetalHeaderHighlight, MetallicTheme.PanelMetalHeaderShadow], SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(band, paint);
    }

    /// <summary>Draws the header band's arrow icon and the configuration's title.</summary>
    private void DrawTitle(SKCanvas canvas)
    {
        canvas.DrawIconFill(Icons.ArrowRight, FromLeft(27), FromTop(-12), RescaleExact(8), Theme.TextPrimary);

        using SKPaint titlePaint = new()
        {
            Color = Theme.TextPrimary,
            IsAntialias = true
        };
        using SKFont titleFont = new()
        {
            Size = RescaleExact(11),
            Typeface = Fonts.Bold
        };
        canvas.DrawTextTopAligned(_config.Title, FromLeft(35), FromTop(-20), SKTextAlign.Left, titleFont, titlePaint);
    }

    /// <summary>
    /// "──── LABEL ────"-style section divider within this panel, for grouping rows of controls under
    /// a heading. <paramref name="relativeY"/> is logical, like the configuration dimensions, measured
    /// down from the panel's top edge, and gets rescaled here.
    ///
    /// Call this from the WINDOW's DrawBackground, after DrawRegisteredPanels - it draws straight onto
    /// the canvas rather than being part of <see cref="Draw"/>, so a window can place as many dividers
    /// as its layout needs without the panel having to know about them.
    /// </summary>
    public void DrawSectionDivider(SKCanvas canvas, int relativeY, string text)
    {
        var left = FromLeft(16);
        var right = FromRight(16);
        var lineY = FromTop(relativeY);

        using SKPaint textPaint = new()
        {
            Color = Theme.TextDim,
            IsAntialias = true
        };
        using SKFont textFont = new()
        {
            Size = RescaleExact(9),
            Typeface = Fonts.Bold
        };

        var textWidth = textFont.MeasureText(text, out _);
        var textPad = RescaleExact(8);
        var centerX = Left + (Width / 2f);
        var textLeft = centerX - (textWidth / 2f);
        var textRight = centerX + (textWidth / 2f);

        using SKPaint linePaint = new()
        {
            Color = Theme.TextDim,
            StrokeWidth = RescaleExact(0.5f),
            IsAntialias = true
        };
        canvas.DrawLine(left, lineY, textLeft - textPad, lineY, linePaint);
        canvas.DrawLine(textRight + textPad, lineY, right, lineY, linePaint);

        canvas.DrawTextTopAligned(text, centerX, lineY - (textFont.Size / 2f), SKTextAlign.Center, textFont, textPaint);
    }
}
