using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// A push button: a rounded box with a label, outlined in the accent on hover and filled with it
/// while held. Fires on release over the button (see <see cref="ButtonControlConfiguration.OnClick"/>),
/// and can also run a momentary action for as long as it is held (OnPress/OnRelease).
/// </summary>
public sealed class ButtonControl(ButtonControlConfiguration config) : PluginControl(config)
{
    private bool _isPressed;
    private bool _isHovered;

    private ButtonStyle Style => config.ResolveStyle();

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isPressed = true;
        config.OnPress?.Invoke();
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        var hovered = HitTest(e.X, e.Y);
        if (hovered == _isHovered) return;
        _isHovered = hovered;
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (!_isPressed) return;
        _isPressed = false;
        config.OnRelease?.Invoke();
        if (IsEnabled && HitTest(e.X, e.Y)) config.OnClick();
        Refresh();
    }

    public override void OnPointerLeave()
    {
        _isHovered = false;
        base.OnPointerLeave();
    }

    public override void Draw(SKCanvas canvas)
    {
        var style = Style;
        var enabled = IsEnabled;
        var radius = RescaleExact(style.CornerRadius);
        var box = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        // Held down (and still over the button - or anywhere, for a momentary one whose action is still
        // running): filled with the accent, like a selected tab. A primary button carries a tint of it
        // at rest, so it stands out before it is touched.
        var held = _isPressed && ((enabled && _isHovered) || config.OnRelease is not null);
        var fill = held ? Theme.Accent.WithAlpha(150)
            : style.IsPrimary && enabled ? Theme.Accent.WithAlpha(_isHovered ? (byte)45 : (byte)22)
            : Theme.TrackBackground.WithAlpha(enabled ? (byte)200 : (byte)100);
        using (SKPaint background = new() { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(box, radius, radius, background);

        if (enabled && !held && (_isHovered || style.IsPrimary))
        {
            var outlineAlpha = !style.IsPrimary ? (byte)150 : _isHovered ? (byte)200 : (byte)90;
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(outlineAlpha), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }

        using SKFont font = new() { Size = RescaleExact(style.FontSize), Typeface = Fonts.Bold };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        var textColor = !enabled ? Theme.TextDisabled
            : held ? Theme.TextPrimary
            : style.IsPrimary ? Theme.Accent
            : _isHovered ? Theme.TextPrimary : Theme.TextDim;
        using SKPaint textPaint = new() { Color = textColor, IsAntialias = true };

        // The icon and the label are centred as one group, so the label shifts over to make room.
        var iconSize = style.Icon is null ? 0f : RescaleExact(style.FontSize * style.IconScale);
        var gap = style.Icon is null ? 0f : RescaleExact(style.FontSize * 0.6f);
        var groupLeft = box.MidX - ((iconSize + gap + font.MeasureText(config.Label)) / 2f);

        if (style.Icon is not null)
            canvas.DrawIconFill(style.Icon, groupLeft + (iconSize / 2f), _h / 2f, iconSize, textColor);

        canvas.DrawTextTopAligned(config.Label, groupLeft + iconSize + gap, textTop, SKTextAlign.Left, font, textPaint);
    }
}
