using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// A push button: a rounded box with a label, outlined in the accent on hover and filled with it
/// while held. Fires on release over the button (see <see cref="ButtonControlConfiguration.OnClick"/>).
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

        // Held down (and still over the button): filled with the accent, like a selected tab.
        var held = enabled && _isPressed && _isHovered;
        var fill = held ? Theme.Accent.WithAlpha(150) : Theme.TrackBackground.WithAlpha(enabled ? (byte)200 : (byte)100);
        using (SKPaint background = new() { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(box, radius, radius, background);

        if (enabled && _isHovered && !held)
        {
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(150), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }

        using SKFont font = new() { Size = RescaleExact(style.FontSize), Typeface = Fonts.Bold };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        var textColor = !enabled ? Theme.TextDisabled : held || _isHovered ? Theme.TextPrimary : Theme.TextDim;
        using SKPaint textPaint = new() { Color = textColor, IsAntialias = true };
        canvas.DrawTextTopAligned(config.Label, box.MidX, textTop, SKTextAlign.Center, font, textPaint);
    }
}
