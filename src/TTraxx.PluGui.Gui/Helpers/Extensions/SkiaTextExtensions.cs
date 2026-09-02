using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers.Extensions;

public static class SkiaTextExtensions
{
    /// <summary>
    /// Draws text with (x, topY) as the top of the text line, like
    /// GDI's TextOut without TA_BASELINE. SkiaSharp's DrawText uses
    /// the baseline as the anchor by default, so we correct with the ascent
    /// of the font (SKFont.Metrics.Ascent is negative in Skia — hence the
    /// subtraction instead of addition).
    /// </summary>
    public static void DrawTextTopAligned(this SKCanvas canvas, string text, float x, float topY, SKTextAlign textAlign, SKFont font, SKPaint paint)
    {
        SKFontMetrics metrics = font.Metrics;
        var baselineY = topY - metrics.Ascent;
        canvas.DrawText(text, x, baselineY, textAlign, font, paint);
    }
}