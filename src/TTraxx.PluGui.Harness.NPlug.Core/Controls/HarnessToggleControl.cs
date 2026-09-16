using SkiaSharp;
using TTraxx.PluGui.Gui;

namespace TTraxx.PluGui.Harness.NPlug.Core.Controls;

/// <summary>
/// Compact horizontal switch with the label to its right, vertically centered -
/// sized to fit a slim settings bar. TTraxx.PluGui.Gui's own ToggleControl lays
/// its label out underneath the pill, tuned for a knob-grid cell, which doesn't
/// fit here. Harness-only dev-tool control, not part of the widget library a
/// plugin's own view draws with.
/// </summary>
public sealed class HarnessToggleControl(ToggleControlConfiguration config) : PluginControl(config)
{
    private ParameterBinding Parameter => config.Parameter;
    private bool IsOn => Parameter.Normalized >= 0.5;

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        Parameter.Edit(IsOn ? 0.0 : 1.0);
        Refresh();
    }

    public override IParameterControlInfo? GetParameterInfo(int localX, int localY) => Parameter.Info;

    public override void Draw(SKCanvas canvas)
    {
        var isOn = IsOn;

        var pillH = Rescale(16);
        var pillW = pillH * 2;
        var pillX = RescaleExact(4);
        var pillY = (_h - pillH) / 2f;

        using (SKPaint pillPaint = new() { Color = isOn ? Theme.Accent : Theme.TrackBackground, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            canvas.DrawRoundRect(pillX, pillY, pillW, pillH, pillH / 2f, pillH / 2f, pillPaint);
        }

        var knobRadius = (pillH / 2f) - RescaleExact(1);
        var knobCx = isOn ? pillX + pillW - (pillH / 2f) : pillX + (pillH / 2f);
        var knobCy = pillY + (pillH / 2f);
        using (SKPaint knobPaint = new() { Color = Theme.MenuTextPrimary, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            canvas.DrawCircle(knobCx, knobCy, knobRadius, knobPaint);
        }

        using SKPaint textPaint = new() { Color = Theme.MenuTextPrimary, IsAntialias = true };
        using SKFont font = new() { Size = Rescale(12), Typeface = Fonts.Bold };
        SKFontMetrics metrics = font.Metrics;
        var textTopY = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        canvas.DrawTextTopAligned(Parameter.Info.Label, pillX + pillW + RescaleExact(8), textTopY, SKTextAlign.Left, font, textPaint);
    }
}
