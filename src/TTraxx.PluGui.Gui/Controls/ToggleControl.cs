using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Controls.Configuration;
using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Helpers.Typography;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls;

public sealed class ToggleControl(ToggleControlConfiguration config) : AbstractControlBase(config)
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

        var pillH = Math.Min(_h - Globals.Rescale(12), Globals.Rescale(14));
        var pillW = pillH * 2;
        var pillX = (_w - pillW) / 2;
        var pillY = Globals.Rescale(44) - (pillH / 2);

        var pillColor = !enabled
                            ? Theme.Current.TrackBackground.WithAlpha(120)
                            : isOn ? Theme.Current.Accent : Theme.Current.TrackBackground;

        using (var pillPaint = new SKPaint { Color = pillColor, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            var pillRect = new SKRect(pillX, pillY, pillX + pillW, pillY + pillH);
            canvas.DrawRoundRect(pillRect, pillH / 2f, pillH / 2f, pillPaint);
        }

        var knobColor = enabled
                        ? Theme.Current.TextPrimary
                        : Theme.Current.TextPrimary.WithAlpha(70);
        var knobRadius = (pillH / 2) - Globals.Rescale(1);
        var knobCx = isOn ? pillX + pillW - (pillH / 2) : pillX + (pillH / 2);
        var knobCy = pillY + (pillH / 2);

        using (var knobPaint = new SKPaint { Color = knobColor, IsAntialias = true, Style = SKPaintStyle.Fill })
        {
            canvas.DrawCircle(knobCx, knobCy, knobRadius, knobPaint);
        }

        var labelY = pillY + pillH + Globals.Rescale(22);
        if (labelY + Globals.Rescale(11) <= _h)
        {
            using var paint = new SKPaint
            {
                Color = enabled ? Theme.Current.TextDim : Theme.Current.TextDisabled,
                IsAntialias = true
            };
            using var font = new SKFont
            {
                Size = Globals.Rescale(11),
                Typeface = Fonts.Current.Bold,
            };
            canvas.DrawTextTopAligned(Parameter.Info.Label, _w / 2f, labelY, SKTextAlign.Center, font, paint);
        }
    }
}