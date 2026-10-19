using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Rotary knob: background arc, filled value arc, pointer line. Vertical
/// drag changes the value; wheel and double-click-to-default are supported.
/// Geometry and feel are configurable through KnobStyle.
/// </summary>
public sealed class KnobControl(KnobControlConfiguration config) : PluginControl(config)
{
    private const double SkiaArcStartAngleDeg = 135;

    private bool _isDragging;
    private double _dragValue;
    private int _lastPointerY;

    private ParameterBinding Parameter => config.Parameter;
    private KnobStyle Style => config.ResolveStyle();

    private KnobMetrics Metrics => Style.Sizes[config.ControlSize];

    /// <summary>The cap's radius, a share of the arc's.</summary>
    private const float CapRadiusFactor = 0.68f;

    /// <summary>The value arc's glow: how much wider than the arc each stroke is (design units), and its alpha.</summary>
    private static readonly (float Extra, byte Alpha)[] GlowStrokes = [(4f, 28), (2f, 48)];

    private float CenterX => _w / 2f;
    private float CenterY(KnobMetrics metrics) => (RescaleExact(metrics.Diameter) / 2f) + RescaleExact(21);
    private float Radius(KnobMetrics metrics) => Math.Max(2, (RescaleExact(metrics.Diameter) / 2f) - Rescale(2));

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isDragging = true;
        Parameter.BeginEdit();
        _lastPointerY = e.Y;
        _dragValue = Parameter.Normalized;
        Refresh();
    }

    /// <summary>
    /// Applies the incremental (not absolute-from-drag-start) mouse delta to
    /// the running value, so toggling Shift (fine-tune) mid-drag changes the
    /// rate for subsequent movement without jumping the current value.
    /// </summary>
    public override void OnPointerMove(PointerEventArgs e)
    {
        if (!_isDragging) return;

        if (!IsEnabled)
        {
            _isDragging = false;
            Parameter.EndEdit();
            Refresh();
            return;
        }

        var dy = e.Y - _lastPointerY;
        _lastPointerY = e.Y;

        var sweepPixels = e.Modifiers.HasFlag(KeyModifiers.Shift)
            ? Style.DragPixelsForFullSweep * Style.FineTuneDivisor
            : Style.DragPixelsForFullSweep;

        _dragValue = Math.Clamp(_dragValue - (dy / sweepPixels), 0.0, 1.0);
        Parameter.SetNormalizedValue(Parameter.Quantize(_dragValue));
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        Parameter.EndEdit();
        Refresh();
    }

    public override void OnWheel(WheelEventArgs e)
    {
        if (!IsEnabled) return;

        double n;
        if (Parameter.Info.PositionCount is int positions && positions > 1)
        {
            var currentStep = (int)Math.Round(Parameter.Normalized * (positions - 1));
            var newStep = Math.Clamp(currentStep + e.Delta, 0, positions - 1);
            n = (double)newStep / (positions - 1);
        }
        else
        {
            n = Math.Clamp(Parameter.Normalized + (e.Delta * (1.0 / Style.WheelSteps)), 0.0, 1.0);
        }

        Parameter.Edit(n);
        Refresh();
    }

    public override void OnDoubleClick(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        ResetToDefault();
    }

    private void ResetToDefault()
    {
        Parameter.Edit(Parameter.Quantize(Parameter.Info.DefaultNormalizedValue));
        Refresh();
    }

    /// <summary>
    /// The raised cap: a soft shadow below it (a radial fade, not a blur - see the glow), a face from
    /// a little lighter than the metal's top to a little darker than its bottom, and a rim that
    /// catches the light at the top and fades out by the middle. Dimmed when the knob is disabled.
    /// </summary>
    private void DrawCap(SKCanvas canvas, float cx, float cy, float radius, bool enabled)
    {
        var faceAlpha = enabled ? (byte)255 : (byte)110;
        var shadowCenter = new SKPoint(cx, cy + RescaleExact(2));
        var shadowRadius = radius * 1.3f;
        using (var shadow = SKShader.CreateRadialGradient(shadowCenter, shadowRadius,
                   [SKColors.Black.WithAlpha(enabled ? (byte)170 : (byte)70), SKColors.Black.WithAlpha(0)], [radius * 0.85f / shadowRadius, 1], SKShaderTileMode.Clamp))
        using (SKPaint shadowPaint = new() { Shader = shadow, IsAntialias = true })
            canvas.DrawCircle(shadowCenter, shadowRadius, shadowPaint);

        var top = MetallicTheme.PanelMetalTop.Lighter(0.06f).WithAlpha(faceAlpha);
        var bottom = MetallicTheme.PanelMetalBottom.Darker(0.15f).WithAlpha(faceAlpha);
        using (var face = SKShader.CreateLinearGradient(new SKPoint(cx, cy - radius), new SKPoint(cx, cy + radius), [top, bottom], null, SKShaderTileMode.Clamp))
        using (SKPaint facePaint = new() { Shader = face, IsAntialias = true })
            canvas.DrawCircle(cx, cy, radius, facePaint);

        var rimWidth = RescaleExact(0.8f);
        using var rim = SKShader.CreateLinearGradient(new SKPoint(cx, cy - radius), new SKPoint(cx, cy),
            [SKColors.White.WithAlpha(enabled ? (byte)80 : (byte)30), SKColors.White.WithAlpha(0)], null, SKShaderTileMode.Clamp);
        using SKPaint rimPaint = new() { Shader = rim, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = rimWidth };
        canvas.DrawCircle(cx, cy, radius - (rimWidth / 2), rimPaint);
    }

    public override bool HitTest(int localX, int localY)
    {
        var metrics = Metrics;
        var radius = Radius(metrics);
        var dx = localX - CenterX;
        var dy = localY - CenterY(metrics);
        return (dx * dx) + (dy * dy) <= radius * radius;
    }

    public override IParameterControlInfo GetParameterInfo(int localX, int localY) => Parameter.Info;

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", ResetToDefault, IsEnabled)
    ];

    public override void Draw(SKCanvas canvas)
    {
        // Resolved once per paint: Style re-runs the configuration's style factory on every read.
        var style = Style;
        var metrics = style.Sizes[config.ControlSize];
        var value = Parameter.Normalized;
        var enabled = IsEnabled;

        var cx = CenterX;
        var cy = CenterY(metrics);
        var drawRadius = Radius(metrics);
        var filledSweep = style.SweepDeg * value;

        // Lit from above: the arc is a groove the value runs in, glowing faintly, and inside it a
        // raised cap - a shadow below, lighter at its top, its rim catching the light - carries the
        // pointer.
        var tickColor = enabled
                            ? Theme.AccentDim
                            : Theme.AccentDim.WithAlpha(40);
        var grooveColor = enabled
                            ? Theme.TrackBackground
                            : Theme.TrackBackground.WithAlpha(90);
        var valueColor = !enabled
                            ? Theme.Accent.WithAlpha(60)
                            : _isDragging || IsHovered ? Theme.Accent2 : Theme.Accent;
        var pointerColor = enabled
                            ? Theme.TextPrimary
                            : Theme.TextPrimary.WithAlpha(70);

        if (Parameter.Info.PositionCount is int positionCount && positionCount > 1)
        {
            var tickInnerR = drawRadius * style.TickInnerRadiusFactor;
            var tickOuterR = drawRadius * style.TickOuterRadiusFactor;

            using SKPaint tickPaint = new()
            {
                Color = tickColor,
                IsAntialias = true,
                StrokeWidth = RescaleExact(1.0f),
                StrokeCap = SKStrokeCap.Butt
            };

            for (var s = 0; s < positionCount; s++)
            {
                var stepNorm = (double)s / (positionCount - 1);
                var tickAngle = (style.StartAngleDeg - (style.SweepDeg * stepNorm)).DegToRad();

                canvas.DrawLine(
                    cx + (float)(Math.Cos(tickAngle) * tickInnerR), cy - (float)(Math.Sin(tickAngle) * tickInnerR),
                    cx + (float)(Math.Cos(tickAngle) * tickOuterR), cy - (float)(Math.Sin(tickAngle) * tickOuterR),
                    tickPaint);
            }
        }

        // The groove is as wide as the value arc, so the value fills it.
        var valueWidth = RescaleExact(metrics.ValueStrokeWidth);

        SKRect arcRect = new(cx - drawRadius, cy - drawRadius, cx + drawRadius, cy + drawRadius);

        using (SKPaint groovePaint = new()
        {
            Color = grooveColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = valueWidth,
            StrokeCap = SKStrokeCap.Round
        })
        {
            canvas.DrawArc(arcRect, (float)SkiaArcStartAngleDeg, (float)style.SweepDeg, false, groovePaint);
        }

        var fillStart = config.IsBipolar ? style.SweepDeg / 2 : 0;
        var fillSweep = (float)(filledSweep - fillStart);
        if (enabled)
        {
            // The glow: two wider, fainter strokes under the value - cheaper than a blur, which the
            // meters' continuous repaint would run for every knob on every frame.
            foreach (var (extra, alpha) in GlowStrokes)
            {
                using SKPaint glowPaint = new()
                {
                    Color = valueColor.WithAlpha(alpha),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = valueWidth + RescaleExact(extra),
                    StrokeCap = SKStrokeCap.Round
                };
                canvas.DrawArc(arcRect, (float)(SkiaArcStartAngleDeg + fillStart), fillSweep, false, glowPaint);
            }
        }

        using (SKPaint valuePaint = new()
        {
            Color = valueColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = valueWidth,
            StrokeCap = SKStrokeCap.Round
        })
        {
            canvas.DrawArc(arcRect, (float)(SkiaArcStartAngleDeg + fillStart), fillSweep, false, valuePaint);
        }

        var capRadius = drawRadius * CapRadiusFactor;
        DrawCap(canvas, cx, cy, capRadius, enabled);

        var pointerAngle = (style.StartAngleDeg - (style.SweepDeg * value)).DegToRad();
        var innerR = capRadius * 0.35;
        var outerR = capRadius * 0.85;
        using (SKPaint pointerPaint = new()
        {
            Color = pointerColor,
            IsAntialias = true,
            StrokeWidth = RescaleExact(metrics.ValueStrokeWidth / 2f),
            StrokeCap = SKStrokeCap.Round
        })
        {
            canvas.DrawLine(
                cx + (float)(Math.Cos(pointerAngle) * innerR), cy - (float)(Math.Sin(pointerAngle) * innerR),
                cx + (float)(Math.Cos(pointerAngle) * outerR), cy - (float)(Math.Sin(pointerAngle) * outerR),
                pointerPaint);
        }

        using SKPaint fontPaint = new() { IsAntialias = true };
        using SKFont font = new() { Size = Rescale(11), Typeface = Fonts.Bold };
        using SKFont smallFont = new() { Size = Rescale(8), Typeface = Fonts.Regular };

        var knobBottom = cy + drawRadius;
        var titleY = knobBottom + Rescale(12);
        if (titleY + Rescale(11) <= _h)
        {
            fontPaint.Color = enabled ? Theme.TextDim : Theme.TextDisabled;
            canvas.DrawTextTopAligned(Parameter.Info.Label, cx, titleY, SKTextAlign.Center, font, fontPaint);
        }

        var rangeY = titleY - Rescale(15);
        if (rangeY + Rescale(9) <= _h)
        {
            if (Parameter.Info.PositionCount is int)
            {
                fontPaint.Color = enabled ? Theme.Accent : Theme.TextDisabled;
                canvas.DrawTextTopAligned(FormatValue(value), cx, rangeY, SKTextAlign.Center, smallFont, fontPaint);
            }
            else
            {
                // Through FormatValue like every other readout, so a ValueFormatter shapes the range
                // ends too - otherwise a knob with one reads its ends in one format and its value in
                // another. Without a formatter this is the same MinValue/MaxValue unit formatting.
                fontPaint.Color = enabled ? Theme.TextDim : Theme.TextDisabled;
                var minimum = FormatValue(0.0);
                var maximum = FormatValue(1.0);

                // Two long ends ("0.02 Hz", "20 Hz") on a small knob would run into each other: drawn smaller, to fit
                // with a gap between them.
                var room = _w - Rescale(10) - Rescale(4);
                var needed = smallFont.MeasureText(minimum) + smallFont.MeasureText(maximum);
                using SKFont endFont = new() { Size = needed > room ? smallFont.Size * room / needed : smallFont.Size, Typeface = smallFont.Typeface };
                canvas.DrawTextTopAligned(minimum, Rescale(5), rangeY, SKTextAlign.Left, endFont, fontPaint);
                canvas.DrawTextTopAligned(maximum, _w - Rescale(5), rangeY, SKTextAlign.Right, endFont, fontPaint);
            }
        }

        if (_isDragging && Parameter.Info.PositionCount is null)
        {
            var liveY = Math.Max(0, cy - drawRadius - RescaleExact(18));
            fontPaint.Color = Theme.Accent2;
            canvas.DrawTextTopAligned(FormatValue(value), cx, liveY, SKTextAlign.Center, font, fontPaint);
        }
    }

    private string FormatValue(double normalized)
    {
        var plain = Parameter.ToPlain(normalized);
        return Parameter.ValueFormatter?.Invoke(plain) ?? plain.Format(Parameter.Info.Unit);
    }
}
