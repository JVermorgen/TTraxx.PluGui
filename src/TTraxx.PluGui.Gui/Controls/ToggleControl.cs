using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// On/off pill switch with a sliding knob and a label underneath. A click flips the parameter to a
/// full 0.0 or 1.0 in one bracketed edit, and right-click offers "Reset to Default"; there's no
/// drag behaviour, since a two-state value has nothing to drag through.
/// </summary>
public sealed class ToggleControl(ToggleControlConfiguration config) : PluginControl(config)
{
    private ParameterBinding Parameter => config.Parameter;
    private ToggleStyle Style => config.ResolveStyle();

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
        var style = Style;
        if (style.Appearance == ToggleAppearance.Chip)
        {
            DrawChip(canvas, style);
            return;
        }

        var isOn = IsOn;
        var enabled = IsEnabled;
        var metrics = style.Sizes[config.ControlSize];

        var pillH = Math.Min(_h - Rescale(12), RescaleExact(metrics.PillHeight));
        var pillW = pillH * 2;
        var pillX = (_w - pillW) / 2;
        var pillY = RescaleExact(metrics.PillCenterY) - (pillH / 2);

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

    /// <summary>
    /// <see cref="ToggleAppearance.Chip"/>: the whole box, tinted and outlined in the accent while on;
    /// while off a plain groove, outlined only on hover like a button, with the style's off icon (if
    /// any) in front of the label.
    /// </summary>
    private void DrawChip(SKCanvas canvas, ToggleStyle style)
    {
        var isOn = IsOn;
        var enabled = IsEnabled;
        var radius = RescaleExact(style.ChipCornerRadius);
        var box = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        var fill = isOn
            ? Theme.Accent.WithAlpha(enabled ? (byte)36 : (byte)18)
            : Theme.TrackBackground.WithAlpha(enabled ? (byte)200 : (byte)100);
        using (SKPaint background = new() { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(box, radius, radius, background);

        if (enabled && (isOn || IsHovered))
        {
            var outlineAlpha = !isOn ? (byte)150 : IsHovered ? (byte)200 : (byte)90;
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(outlineAlpha), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }

        var textColor = !enabled ? Theme.TextDisabled : isOn ? Theme.TextPrimary : Theme.TextDim;
        using SKFont font = new() { Size = RescaleExact(style.ChipFontSize), Typeface = Fonts.Bold };
        using SKPaint textPaint = new() { Color = textColor, IsAntialias = true };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        var label = Parameter.Info.Label;

        // The icon and the label are centred as one group, so the label shifts over to make room.
        var icon = isOn ? null : style.ChipOffIcon;
        var iconSize = icon is null ? 0f : RescaleExact(style.ChipFontSize);
        var gap = icon is null ? 0f : RescaleExact(4f);
        var groupLeft = (_w - (iconSize + gap + font.MeasureText(label))) / 2f;

        if (icon is not null)
            canvas.DrawIconFill(icon, groupLeft + (iconSize / 2f), _h / 2f, iconSize, textColor);

        canvas.DrawTextTopAligned(label, groupLeft + iconSize + gap, textTop, SKTextAlign.Left, font, textPaint);
    }
}
