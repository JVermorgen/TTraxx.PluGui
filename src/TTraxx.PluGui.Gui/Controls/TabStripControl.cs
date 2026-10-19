using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// A segmented row of tabs, equal widths, the selected one filled. A click selects; the page switch
/// itself is the configuration's business (see <see cref="TabStripControlConfiguration"/>).
/// </summary>
public sealed class TabStripControl(TabStripControlConfiguration config) : PluginControl(config)
{
    private int _hoveredIndex = -1;

    private TabStripStyle Style => config.ResolveStyle();

    private int Count => config.Tabs.Count;

    private int IndexAt(int localX) => Count == 0 ? -1 : Math.Clamp(localX * Count / Math.Max(1, _w), 0, Count - 1);

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled || Count == 0) return;
        config.SetSelectedIndex(IndexAt(e.X));
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        var index = HitTest(e.X, e.Y) ? IndexAt(e.X) : -1;
        if (index == _hoveredIndex) return;
        _hoveredIndex = index;
        Refresh();
    }

    public override void OnPointerLeave()
    {
        _hoveredIndex = -1;
        base.OnPointerLeave();
    }

    public override void Draw(SKCanvas canvas)
    {
        if (Count == 0) return;

        var style = Style;
        var enabled = IsEnabled;
        var radius = RescaleExact(style.CornerRadius);
        var strip = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);
        var selected = Math.Clamp(config.GetSelectedIndex(), 0, Count - 1);
        var tabWidth = strip.Width / Count;

        // Lit from above (see SkiaDepthExtensions): the strip is a recessed track, the selected tab a
        // key raised in it, inset all round, with a shadow below and the light along its top edge.
        canvas.FillRecessed(strip, radius, Theme.TrackBackground.WithAlpha(enabled ? (byte)200 : (byte)100));
        if (enabled) canvas.DrawBottomLip(strip, radius, RescaleExact(0.6f), 24);

        var inset = RescaleExact(2);
        var selectedRect = new SKRect(strip.Left + (selected * tabWidth) + inset, strip.Top + inset,
            strip.Left + ((selected + 1) * tabWidth) - inset, strip.Bottom - inset);
        var selectedRadius = Math.Max(0, radius - inset);
        if (enabled)
        {
            using SKRoundRect shape = new(selectedRect, selectedRadius, selectedRadius);
            canvas.DrawDropShadow(shape, RescaleExact(1), RescaleExact(1.5f), 150);
        }
        var accent = Theme.Accent.WithAlpha(enabled ? (byte)170 : (byte)50);
        using (var shader = SKShader.CreateLinearGradient(new SKPoint(selectedRect.Left, selectedRect.Top), new SKPoint(selectedRect.Left, selectedRect.Bottom),
                   [accent, accent.WithAlpha(enabled ? (byte)120 : (byte)40)], null, SKShaderTileMode.Clamp))
        using (SKPaint highlight = new() { Shader = shader, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(selectedRect, selectedRadius, selectedRadius, highlight);
        if (enabled) canvas.DrawTopHighlight(selectedRect, selectedRadius, RescaleExact(0.7f), 60);

        using SKFont font = new() { Size = RescaleExact(style.FontSize), Typeface = Fonts.Bold };
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;

        for (var i = 0; i < Count; i++)
        {
            var color = !enabled ? Theme.TextDisabled
                      : i == selected || i == _hoveredIndex ? Theme.TextPrimary
                      : Theme.TextDim;
            using SKPaint textPaint = new() { Color = color, IsAntialias = true };
            canvas.DrawTextTopAligned(config.Tabs[i], strip.Left + ((i + 0.5f) * tabWidth), textTop, SKTextAlign.Center, font, textPaint);
        }
    }
}
