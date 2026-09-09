using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Controls.Configuration;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Helpers.Typography;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls;

/// <summary>
/// A styled rotary knob: a background arc, a filled progress arc, and a
/// pointer line. Sweeps 270 degrees, the conventional audio-plugin knob
/// range. Value is always normalized (0..1); drag behaviour is
/// vertical-drag-to-change.
/// </summary>
public sealed class KnobControl(KnobControlConfiguration config) : AbstractControlBase(config)
{
    private const double StartAngleDeg = -135;
    private const double SkiaArcStartAngleDeg = 135;
    private const double SweepDeg = 270;
    private const double DragPixelsForFullSweep = 200.0;

    private const int PTR_STEPS = 72;

    private bool _isDragging;
    private double _dragStartValue;
    private int _dragStartY;

    private readonly Dictionary<KnobSizes, int> _knobSizeToRadiusMapping = new()
    {
        { KnobSizes.S, 42 },
        { KnobSizes.M, 46 },
        { KnobSizes.L, 52 },
        { KnobSizes.XL, 64 }
    };

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!config.IsEnabled()) return;
        _isDragging = true;
        config.BeginAction();
        _dragStartY = e.Y;
        _dragStartValue = Math.Clamp(config.GetNormalizedValue(), 0.0f, 1.0f);
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        if (!_isDragging) return;

        if (!config.IsEnabled())
        {
            _isDragging = false;
            config.EndAction();
            Refresh();
            return;
        }

        var dy = e.Y - _dragStartY;
        var newNorm = QuantizeIfStepped(Math.Clamp(_dragStartValue - (dy / DragPixelsForFullSweep), 0.0, 1.0));
        config.PerformAction(newNorm);
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (_isDragging && config.IsEnabled())
        {
            _isDragging = false;
            config.EndAction();
            Refresh();
        }
    }

    public override void OnWheel(WheelEventArgs e)
    {
        if (!config.IsEnabled()) return;
        var wheelTicks = e.Delta;
        double n;
        if (config.ParameterInfo.PositionCount is int positions && positions > 1)
        {
            var currentStep = (int)Math.Round(config.GetNormalizedValue() * (positions - 1));
            var newStep = Math.Clamp(currentStep + wheelTicks, 0, positions - 1);
            n = (double)newStep / (positions - 1);
        }
        else
        {
            n = config.GetNormalizedValue() + (wheelTicks * (1.0 / PTR_STEPS));
        }
        config.BeginAction();
        config.PerformAction(n);
        config.EndAction();
        Refresh();
    }

    public override void OnDoubleClick(PointerEventArgs e)
    {
        if (!config.IsEnabled()) return;
        var norm0 = QuantizeIfStepped((0.0 - config.MinValue) / (config.MaxValue - config.MinValue));
        config.BeginAction();
        config.PerformAction(norm0);
        config.EndAction();
        Refresh();
    }

    public override bool HitTest(int x, int y)
    {
        var circleSize = Globals.Rescale(_knobSizeToRadiusMapping[config.KnobSize]);
        var localCx = _w / 2;
        var localCy = (circleSize / 2) + Globals.Rescale(21);
        var globalCx = _x + localCx;
        var globalCy = _y + localCy;
        var drawRadius = Math.Max(2, (circleSize / 2) - Globals.Rescale(2));

        var dx = x - globalCx;
        var dy = y - globalCy;
        return (dx * dx) + (dy * dy) <= drawRadius * drawRadius;
    }

    public override IParameterControlInfo GetParameterInfo(int xPos, int yPos) => config.ParameterInfo;

    public override void Draw(SKCanvas canvas)
    {
        var value = Math.Clamp(config.GetNormalizedValue(), 0.0, 1.0);
        var enabled = config.IsEnabled();

        var circleSize = Globals.Rescale(_knobSizeToRadiusMapping[config.KnobSize]);
        var cx = _w / 2;
        var cy = (circleSize / 2) + Globals.Rescale(21);
        var drawRadius = Math.Max(2, (circleSize / 2) - Globals.Rescale(2));

        var filledSweep = SweepDeg * value;

        var trackColor = enabled
                            ? Theme.Current.AccentDim
                            : Theme.Current.AccentDim.WithAlpha(40);
        var valueColor = !enabled
                            ? Theme.Current.Accent.WithAlpha(60)
                            : _isDragging ? Theme.Current.Accent2 : Theme.Current.Accent;
        var pointerColor = enabled
                            ? Theme.Current.TextPrimary
                            : Theme.Current.TextPrimary.WithAlpha(70);

        if (config.ParameterInfo.PositionCount is int positionCount && positionCount > 1)
        {
            var tickColor = trackColor;
            var tickInnerR = drawRadius * 1.02;
            var tickOuterR = drawRadius * 1.35;

            using SKPaint tickPaint = new()
            {
                Color = tickColor,
                IsAntialias = true,
                StrokeWidth = Globals.RescaleExact(1.0f),
                StrokeCap = SKStrokeCap.Butt
            };

            for (var s = 0; s < positionCount; s++)
            {
                var stepNorm = (double)s / (positionCount - 1);
                var tickAngle = (StartAngleDeg - (SweepDeg * stepNorm)).DegToRad();

                canvas.DrawLine(
                    cx + (float)(Math.Cos(tickAngle) * tickInnerR), cy - (float)(Math.Sin(tickAngle) * tickInnerR),
                    cx + (float)(Math.Cos(tickAngle) * tickOuterR), cy - (float)(Math.Sin(tickAngle) * tickOuterR),
                    tickPaint);
            }
        }

        var dimmedArcWidth = config.KnobSize switch
        {
            KnobSizes.XL => Globals.RescaleExact(2.8f),
            _ => Globals.RescaleExact(2.0f)
        };
        var accentArcWidth = config.KnobSize switch
        {
            KnobSizes.XL => Globals.RescaleExact(4.2f),
            _ => Globals.RescaleExact(3.0f)
        };

        SKRect arcRect = new(cx - drawRadius, cy - drawRadius, cx + drawRadius, cy + drawRadius);

        using (SKPaint trackPaint = new()
        {
            Color = trackColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = dimmedArcWidth,
            StrokeCap = SKStrokeCap.Butt
        })
        {
            canvas.DrawArc(arcRect, (float)(SkiaArcStartAngleDeg + filledSweep), (float)(SweepDeg - filledSweep), false, trackPaint);
        }

        using (SKPaint valuePaint = new()
        {
            Color = valueColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = accentArcWidth,
            StrokeCap = SKStrokeCap.Butt
        })
        {
            canvas.DrawArc(arcRect, (float)SkiaArcStartAngleDeg, (float)filledSweep, false, valuePaint);
        }

        var pointerAngle = (StartAngleDeg - (SweepDeg * value)).DegToRad();
        var innerR = drawRadius * 0.25;
        var outerR = drawRadius * 0.85;
        using (SKPaint pointerPaint = new()
        {
            Color = pointerColor,
            IsAntialias = true,
            StrokeWidth = Globals.RescaleExact(1.3f),
            StrokeCap = SKStrokeCap.Round
        })
        {
            canvas.DrawLine(
                cx + (float)(Math.Cos(pointerAngle) * innerR), cy - (float)(Math.Sin(pointerAngle) * innerR),
                cx + (float)(Math.Cos(pointerAngle) * outerR), cy - (float)(Math.Sin(pointerAngle) * outerR),
                pointerPaint);
        }

        using SKPaint fontPaint = new() { IsAntialias = true };
        using SKFont font = new() { Size = Globals.Rescale(11), Typeface = Fonts.Current.Bold };
        using SKFont smallFont = new() { Size = Globals.Rescale(8), Typeface = Fonts.Current.Regular };

        var knobBottom = cy + drawRadius;
        var titleY = knobBottom + Globals.Rescale(12);
        if (titleY + Globals.Rescale(11) <= _h)
        {
            fontPaint.Color = enabled ? Theme.Current.TextDim : Theme.Current.TextDisabled;
            canvas.DrawTextTopAligned(config.ParameterInfo.Label, cx, titleY, SKTextAlign.Center, font, fontPaint);
        }

        var rangeY = titleY - Globals.Rescale(15);
        if (rangeY + Globals.Rescale(9) <= _h)
        {
            if (config.ParameterInfo.PositionCount is int)
            {
                var plainValue = config.MinValue + ((config.MaxValue - config.MinValue) * value);
                var currentText = config.CustomValueFormatter?.Invoke(plainValue) ?? plainValue.Format(config.ParameterInfo.Unit);
                fontPaint.Color = enabled ? Theme.Current.Accent : Theme.Current.TextDisabled;
                canvas.DrawTextTopAligned(currentText, cx, rangeY, SKTextAlign.Center, smallFont, fontPaint);
            }
            else
            {
                var minText = $"{config.MinValue.Format(config.ParameterInfo.Unit)}";
                fontPaint.Color = enabled ? Theme.Current.TextDim : Theme.Current.TextDisabled;
                canvas.DrawTextTopAligned(minText, Globals.Rescale(5), rangeY, SKTextAlign.Left, smallFont, fontPaint);

                var maxText = $"{config.MaxValue.Format(config.ParameterInfo.Unit)}";
                canvas.DrawTextTopAligned(maxText, _w - Globals.Rescale(5), rangeY, SKTextAlign.Right, smallFont, fontPaint);
            }
        }

        if (_isDragging && config.ParameterInfo.PositionCount is null)
        {
            var plainValue = config.MinValue + ((config.MaxValue - config.MinValue) * value);
            var liveText = config.CustomValueFormatter?.Invoke(plainValue) ?? plainValue.Format(config.ParameterInfo.Unit);
            var liveY = Math.Max(0, cy - drawRadius - Globals.RescaleExact(18));
            fontPaint.Color = Theme.Current.Accent2;
            canvas.DrawTextTopAligned(liveText, cx, liveY, SKTextAlign.Center, font, fontPaint);
        }
    }

    private double QuantizeIfStepped(double normalized)
        => (config.ParameterInfo.PositionCount is int positions && positions > 1)
            ? Math.Round(normalized * (positions - 1)) / (positions - 1)
            : normalized;
}