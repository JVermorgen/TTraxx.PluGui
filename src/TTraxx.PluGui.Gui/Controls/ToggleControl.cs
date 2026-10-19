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

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems()
    {
        ContextMenuItem reset = new("Reset to Default", () =>
        {
            Parameter.Edit(Parameter.Quantize(Parameter.Info.DefaultNormalizedValue));
            Refresh();
        }, IsEnabled);

        // Held idle: the reason first, greyed - something to read, not to pick.
        return IdleReason is { } reason ? [new(reason, () => { }, IsEnabled: false), reset] : [reset];
    }

    /// <summary>Why the toggle is on but can't act (see <see cref="ToggleControlConfiguration.IdleReason"/>); null while it can, or is off.</summary>
    private string? IdleReason => IsOn ? config.IdleReason?.Invoke() : null;

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

        // Lit from above: the track is recessed (shaded from its top, its lower lip catching the
        // light), the thumb raised in it (a shadow below, lighter at its top). On, the track fills
        // with the accent, deepening towards the top as the groove does.
        var pillRect = new SKRect(pillX, pillY, pillX + pillW, pillY + pillH);
        var pillRadius = pillH / 2f;
        if (enabled && isOn)
        {
            using var shader = SKShader.CreateLinearGradient(new SKPoint(pillX, pillRect.Top), new SKPoint(pillX, pillRect.Bottom),
                [Theme.Accent.Towards(Theme.AccentDim, 0.6f), Theme.Accent], null, SKShaderTileMode.Clamp);
            using var pillPaint = new SKPaint { Shader = shader, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(pillRect, pillRadius, pillRadius, pillPaint);
        }
        else
        {
            canvas.FillRecessed(pillRect, pillRadius, enabled ? Theme.TrackBackground : Theme.TrackBackground.WithAlpha(120));
        }
        if (enabled) canvas.DrawBottomLip(pillRect, pillRadius, RescaleExact(0.6f), 30);

        var knobRadius = (pillH / 2) - Rescale(1);
        var knobCx = isOn ? pillX + pillW - (pillH / 2) : pillX + (pillH / 2);
        var knobCy = pillY + (pillH / 2);
        var thumbAlpha = enabled ? (byte)255 : (byte)70;

        using (var shadow = SKShader.CreateRadialGradient(new SKPoint(knobCx, knobCy + RescaleExact(1.2f)), knobRadius * 1.35f,
                   [SKColors.Black.WithAlpha(enabled ? (byte)160 : (byte)50), SKColors.Black.WithAlpha(0)], [0.7f, 1], SKShaderTileMode.Clamp))
        using (var shadowPaint = new SKPaint { Shader = shadow, IsAntialias = true })
        {
            canvas.DrawCircle(knobCx, knobCy + RescaleExact(1.2f), knobRadius * 1.35f, shadowPaint);
        }

        using (var face = SKShader.CreateLinearGradient(new SKPoint(knobCx, knobCy - knobRadius), new SKPoint(knobCx, knobCy + knobRadius),
                   [Theme.TextPrimary.Lighter(0.4f).WithAlpha(thumbAlpha), Theme.TextPrimary.Darker(0.22f).WithAlpha(thumbAlpha)], null, SKShaderTileMode.Clamp))
        using (var knobPaint = new SKPaint { Shader = face, IsAntialias = true, Style = SKPaintStyle.Fill })
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
    /// any) in front of the label. On but idle (<see cref="ToggleControlConfiguration.IdleReason"/>):
    /// the groove with a dashed accent outline, and <see cref="Icons.Alert"/> in front of the label.
    /// </summary>
    private void DrawChip(SKCanvas canvas, ToggleStyle style)
    {
        var isOn = IsOn;
        var enabled = IsEnabled;
        var idle = IdleReason is not null;
        var radius = RescaleExact(style.ChipCornerRadius);
        var box = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        // A raised key, lit from above (see SkiaDepthExtensions); while on, the accent tints it.
        var surface = Theme.TrackBackground.WithAlpha(enabled ? (byte)200 : (byte)100);
        if (enabled)
        {
            using SKRoundRect shape = new(box, radius, radius);
            canvas.DrawDropShadow(shape, RescaleExact(1.5f), RescaleExact(2), 140);
        }
        canvas.FillRaised(box, radius, surface);
        if (isOn && !idle)
        {
            using SKPaint tint = new() { Color = Theme.Accent.WithAlpha(enabled ? (byte)36 : (byte)18), IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRoundRect(box, radius, radius, tint);
        }
        if (enabled) canvas.DrawTopHighlight(box, radius, RescaleExact(0.7f), 50);

        if (enabled && idle)
        {
            using var dash = SKPathEffect.CreateDash([RescaleExact(3f), RescaleExact(2f)], 0);
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(IsHovered ? (byte)230 : (byte)170), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f), PathEffect = dash };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }
        else if (enabled && (isOn || IsHovered))
        {
            var outlineAlpha = !isOn ? (byte)150 : IsHovered ? (byte)200 : (byte)90;
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(outlineAlpha), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }

        var textColor = !enabled ? Theme.TextDisabled : isOn && !idle ? Theme.TextPrimary : Theme.TextDim;
        using SKFont font = new() { Size = RescaleExact(style.ChipFontSize), Typeface = Fonts.Bold };
        using SKPaint textPaint = new() { Color = textColor, IsAntialias = true };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        var label = Parameter.Info.Label;

        // The icon and the label are centred as one group, so the label shifts over to make room.
        var icon = idle ? Icons.Alert : isOn ? null : style.ChipOffIcon;
        var iconSize = icon is null ? 0f : RescaleExact(style.ChipFontSize);
        var gap = icon is null ? 0f : RescaleExact(4f);
        var groupLeft = (_w - (iconSize + gap + font.MeasureText(label))) / 2f;

        if (icon is not null)
            canvas.DrawIconFill(icon, groupLeft + (iconSize / 2f), _h / 2f, iconSize, idle && enabled ? Theme.Accent : textColor);

        canvas.DrawTextTopAligned(label, groupLeft + iconSize + gap, textTop, SKTextAlign.Left, font, textPaint);
    }
}
