using SkiaSharp;

namespace TTraxx.PluGui.Gui;

public sealed class ToggleControl(ToggleControlConfiguration config) : PluginControl(config)
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

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", () =>
        {
            Parameter.Edit(Parameter.Quantize(Parameter.Info.DefaultNormalizedValue));
            Refresh();
        }, IsEnabled)
    ];

    public override void Draw(SKCanvas canvas)
    {
        var isOn = IsOn;
        var enabled = IsEnabled;

        var pillH = Math.Min(_h - Rescale(12), Rescale(14));
        var pillW = pillH * 2;
        var pillX = (_w - pillW) / 2;
        var pillY = Rescale(44) - (pillH / 2);

        var pillColor = !enabled
                            ? Theme.TrackBackground.WithAlpha(120)
                            : isOn ? Theme.Accent : Theme.TrackBackground;

        using (var pillPaint = new SKPaint { Color = pillColor, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            var pillRect = new SKRect(pillX, pillY, pillX + pillW, pillY + pillH);
            canvas.DrawRoundRect(pillRect, pillH / 2f, pillH / 2f, pillPaint);
        }

        var knobColor = enabled
                        ? Theme.TextPrimary
                        : Theme.TextPrimary.WithAlpha(70);
        var knobRadius = (pillH / 2) - Rescale(1);
        var knobCx = isOn ? pillX + pillW - (pillH / 2) : pillX + (pillH / 2);
        var knobCy = pillY + (pillH / 2);

        using (var knobPaint = new SKPaint { Color = knobColor, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            canvas.DrawCircle(knobCx, knobCy, knobRadius, knobPaint);
        }

        var labelY = pillY + pillH + Rescale(22);
        if (labelY + Rescale(11) <= _h)
        {
            using var paint = new SKPaint
            {
                Color = enabled ? Theme.TextDim : Theme.TextDisabled,
                IsAntialias = true
            };
            using var font = new SKFont
            {
                Size = Rescale(11),
                Typeface = Fonts.Bold,
            };
            canvas.DrawTextTopAligned(Parameter.Info.Label, _w / 2f, labelY, SKTextAlign.Center, font, paint);
        }
    }
}
