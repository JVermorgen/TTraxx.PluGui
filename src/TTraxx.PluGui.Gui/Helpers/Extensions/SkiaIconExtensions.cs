using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers.Extensions;

public static class SkiaIconExtensions
{
    public static void DrawIconFill(this SKCanvas canvas, SKPath icon, float x, float y, float size, SKColor color)
    {
        using SKPaint paint = new() { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.Save();
        canvas.Translate(x, y);
        canvas.Scale(size, size);
        canvas.DrawPath(icon, paint);
        canvas.Restore();
    }

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