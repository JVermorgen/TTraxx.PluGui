using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Horizontal bar slider: the filled part shows the value (from the left edge, or from the centre
/// when bipolar) and the formatted value is printed on the bar. Horizontal drag changes it -
/// relative, like a knob, so grabbing it anywhere never makes it jump - with Shift for fine
/// adjustment; wheel and double-click-to-default work as on a knob.
/// </summary>
public sealed class SliderControl(SliderControlConfiguration config) : PluginControl(config)
{
    private bool _isDragging;
    private double _dragValue;
    private int _lastPointerX;

    private ParameterBinding Parameter => config.Parameter;
    private SliderStyle Style => config.ResolveStyle();

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isDragging = true;
        Parameter.BeginEdit();
        _lastPointerX = e.X;
        _dragValue = Parameter.Normalized;
        Refresh();
    }

    /// <summary>Incremental, like KnobControl: toggling Shift mid-drag changes the rate without jumping the value.</summary>
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

        var dx = e.X - _lastPointerX;
        _lastPointerX = e.X;

        var sweepPixels = Rescale((float)Style.DragPixelsForFullSweep)
                          * (e.Modifiers.HasFlag(KeyModifiers.Shift) ? Style.FineTuneDivisor : 1.0);

        _dragValue = Math.Clamp(_dragValue + (dx / sweepPixels), 0.0, 1.0);
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
        Parameter.Edit(Math.Clamp(Parameter.Normalized + (e.Delta * (1.0 / Style.WheelSteps)), 0.0, 1.0));
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

    public override IParameterControlInfo? GetParameterInfo(int localX, int localY) => Parameter.Info;

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", ResetToDefault, IsEnabled)
    ];

    public override void Draw(SKCanvas canvas)
    {
        var style = Style;
        var enabled = IsEnabled;
        var value = Parameter.Normalized;
        var radius = RescaleExact(style.CornerRadius);
        var bar = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        using (SKPaint track = new() { Color = enabled ? Theme.TrackBackground : Theme.TrackBackground.WithAlpha(120), IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(bar, radius, radius, track);

        // The fill, clipped to the bar's rounded outline so its ends follow the corners.
        var valueX = bar.Left + (float)(value * bar.Width);
        var originX = config.IsBipolar ? bar.MidX : bar.Left;
        var fillColor = !enabled ? Theme.Accent.WithAlpha(60) : _isDragging || IsHovered ? Theme.Accent2 : Theme.Accent;
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(bar, radius, radius), SKClipOperation.Intersect, antialias: true);
        using (SKPaint fill = new() { Color = fillColor.WithAlpha((byte)(fillColor.Alpha * 0.55f)), IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRect(new SKRect(Math.Min(originX, valueX), bar.Top, Math.Max(originX, valueX), bar.Bottom), fill);
        canvas.Restore();

        // A bipolar bar marks its zero, so a small amount either way still reads as a direction. Two
        // short ticks on the edges rather than one line across: the value readout sits in the middle
        // of the bar, exactly where a full-height line would cut through it.
        if (config.IsBipolar)
        {
            using SKPaint zero = new() { Color = enabled ? Theme.TextDim : Theme.TextDisabled, IsAntialias = true, StrokeWidth = RescaleExact(1f) };
            var tick = RescaleExact(4f);
            canvas.DrawLine(bar.MidX, bar.Top, bar.MidX, bar.Top + tick, zero);
            canvas.DrawLine(bar.MidX, bar.Bottom - tick, bar.MidX, bar.Bottom, zero);
        }

        using SKFont font = new() { Size = RescaleExact(style.FontSize), Typeface = Fonts.Bold };
        using SKPaint textPaint = new() { Color = enabled ? Theme.TextPrimary : Theme.TextDisabled, IsAntialias = true };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        canvas.DrawTextTopAligned(FormatValue(value), _w / 2f, textTop, SKTextAlign.Center, font, textPaint);
    }

    private string FormatValue(double normalized)
    {
        var plain = Parameter.ToPlain(normalized);
        return Parameter.ValueFormatter?.Invoke(plain) ?? plain.Format(Parameter.Info.Unit);
    }
}
