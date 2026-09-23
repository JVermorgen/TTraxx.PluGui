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
    private KnobStyle Style => config.Style?.Invoke() ?? KnobStyle.Default;

    private int RadiusFor(ControlSizes size)
        => Style.Radii.TryGetValue(size, out var r) ? r : KnobStyle.Default.Radii[size];

    private float Radius => Math.Max(2, (Rescale(RadiusFor(config.ControlSize)) / 2f) - Rescale(2));
    private float CenterX => _w / 2f;
    private float CenterY => (Rescale(RadiusFor(config.ControlSize)) / 2f) + Rescale(21);

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

    public override bool HitTest(int localX, int localY)
    {
        var dx = localX - CenterX;
        var dy = localY - CenterY;
        return (dx * dx) + (dy * dy) <= Radius * Radius;
    }

    public override IParameterControlInfo GetParameterInfo(int localX, int localY) => Parameter.Info;

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", ResetToDefault, IsEnabled)
    ];

    public override void Draw(SKCanvas canvas)
    {
        var value = Parameter.Normalized;
        var enabled = IsEnabled;

        var cx = CenterX;
        var cy = CenterY;
        var drawRadius = Radius;
        var filledSweep = Style.SweepDeg * value;

        var trackColor = enabled
                            ? Theme.AccentDim
                            : Theme.AccentDim.WithAlpha(40);
        var valueColor = !enabled
                            ? Theme.Accent.WithAlpha(60)
                            : _isDragging || IsHovered ? Theme.Accent2 : Theme.Accent;
        var pointerColor = enabled
                            ? Theme.TextPrimary
                            : Theme.TextPrimary.WithAlpha(70);

        if (Parameter.Info.PositionCount is int positionCount && positionCount > 1)
        {
            var tickInnerR = drawRadius * Style.TickInnerRadiusFactor;
            var tickOuterR = drawRadius * Style.TickOuterRadiusFactor;

            using SKPaint tickPaint = new()
            {
                Color = trackColor,
                IsAntialias = true,
                StrokeWidth = RescaleExact(1.0f),
                StrokeCap = SKStrokeCap.Butt
            };

            for (var s = 0; s < positionCount; s++)
            {
                var stepNorm = (double)s / (positionCount - 1);
                var tickAngle = (Style.StartAngleDeg - (Style.SweepDeg * stepNorm)).DegToRad();

                canvas.DrawLine(
                    cx + (float)(Math.Cos(tickAngle) * tickInnerR), cy - (float)(Math.Sin(tickAngle) * tickInnerR),
                    cx + (float)(Math.Cos(tickAngle) * tickOuterR), cy - (float)(Math.Sin(tickAngle) * tickOuterR),
                    tickPaint);
            }
        }

        var isXL = config.ControlSize == ControlSizes.XL;
        var trackWidth = RescaleExact(isXL ? Style.TrackStrokeWidthXL : Style.TrackStrokeWidth);
        var valueWidth = RescaleExact(isXL ? Style.ValueStrokeWidthXL : Style.ValueStrokeWidth);

        SKRect arcRect = new(cx - drawRadius, cy - drawRadius, cx + drawRadius, cy + drawRadius);

        using (SKPaint trackPaint = new()
        {
            Color = trackColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = trackWidth,
            StrokeCap = SKStrokeCap.Butt
        })
        {
            canvas.DrawArc(arcRect, (float)(SkiaArcStartAngleDeg + filledSweep), (float)(Style.SweepDeg - filledSweep), false, trackPaint);
        }

        using (SKPaint valuePaint = new()
        {
            Color = valueColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = valueWidth,
            StrokeCap = SKStrokeCap.Butt
        })
        {
            canvas.DrawArc(arcRect, (float)SkiaArcStartAngleDeg, (float)filledSweep, false, valuePaint);
        }

        var pointerAngle = (Style.StartAngleDeg - (Style.SweepDeg * value)).DegToRad();
        var innerR = drawRadius * 0.25;
        var outerR = drawRadius * 0.85;
        using (SKPaint pointerPaint = new()
        {
            Color = pointerColor,
            IsAntialias = true,
            StrokeWidth = RescaleExact(1.3f),
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
                canvas.DrawTextTopAligned(FormatValue(0.0),
                    Rescale(5), rangeY, SKTextAlign.Left, smallFont, fontPaint);
                canvas.DrawTextTopAligned(FormatValue(1.0),
                    _w - Rescale(5), rangeY, SKTextAlign.Right, smallFont, fontPaint);
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
