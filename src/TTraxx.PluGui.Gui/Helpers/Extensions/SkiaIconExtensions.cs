using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Draws the normalized icon paths from <see cref="Icons"/> at a real size and position - the
/// scaling step those paths deliberately leave out. Two methods because a filled glyph and a
/// stroked waveform need different treatment: stroking a scaled canvas would scale the stroke
/// width with it, so <see cref="DrawIconStroke"/> transforms the path and leaves the canvas alone.
///
/// Coordinates are pixels, so rescale design units before calling.
/// </summary>
public static class SkiaIconExtensions
{
    /// <summary>
    /// Fills an <paramref name="icon"/> (a path in the normalized space described on
    /// <see cref="Icons"/>), scaling the canvas so its -0.5..0.5 box becomes
    /// <paramref name="size"/> pixels across - icons are square in their own space, so one value
    /// covers both axes. Because the path is centered on its own origin,
    /// <paramref name="x"/>/<paramref name="y"/> position the icon's CENTER, not its top-left.
    /// </summary>
    public static void DrawIconFill(this SKCanvas canvas, SKPath icon, float x, float y, float size, SKColor color)
    {
        using SKPaint paint = new() { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(size, size);
        canvas.DrawPath(icon, paint);
        canvas.Restore();
    }

    /// <summary>
    /// Strokes an <paramref name="icon"/> (a path in the normalized space described on
    /// <see cref="Icons"/>) at an independent <paramref name="width"/> and
    /// <paramref name="height"/> - for the waveform glyphs, which usually want to be wider than they
    /// are tall - centered on <paramref name="centerX"/>/<paramref name="centerY"/>. All in pixels.
    ///
    /// The path itself is transformed rather than the canvas, so <paramref name="strokeWidth"/> stays
    /// exact instead of being stretched by the scale. Caps and joins are rounded.
    /// </summary>
    public static void DrawIconStroke(this SKCanvas canvas, SKPath icon, float centerX, float centerY, float width, float height, float strokeWidth, SKColor color)
    {
        SKMatrix matrix = SKMatrix.Concat(SKMatrix.CreateTranslation(centerX, centerY), SKMatrix.CreateScale(width, height));
        using SKPath transformed = new();
        icon.Transform(matrix, transformed);
        using SKPaint paint = new()
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        canvas.DrawPath(transformed, paint);
    }
}
