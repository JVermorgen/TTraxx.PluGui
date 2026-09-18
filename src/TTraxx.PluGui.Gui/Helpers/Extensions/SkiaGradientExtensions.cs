using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// One-call gradient fills, so a control doesn't have to build a shader plus a paint and dispose
/// both every frame. Each call creates and disposes its own shader: convenient, but that makes
/// these unsuitable for a hot path repainted continuously - cache an SKShader yourself there.
///
/// Antialiasing follows the shape: square fills are drawn WITHOUT it so a full-bleed background
/// doesn't get a blurred half-pixel seam at the edges, rounded ones WITH it so the corners stay
/// smooth. Coordinates are pixels, so rescale design units before calling.
/// </summary>
public static class SkiaGradientExtensions
{
    /// <summary>Fills a rectangle with a top-to-bottom two-stop gradient.</summary>
    public static void FillVerticalGradient(this SKCanvas canvas, float x, float y, float w, float h, SKColor colorTop, SKColor colorBottom)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x, y), new SKPoint(x, y + h),
            [colorTop, colorBottom], null, SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };
        canvas.DrawRect(x, y, w, h, paint);
    }

    /// <summary>
    /// Fills a rectangle with a top-to-bottom multi-stop gradient - for a background that needs a
    /// sharper highlight or shadow band than an even two-stop ramp gives.
    /// <paramref name="colors"/> runs top to bottom, and <paramref name="positions"/> places each one
    /// at 0..1 with one entry per color.
    /// </summary>
    public static void FillVerticalGradient(this SKCanvas canvas, float x, float y, float w, float h, SKColor[] colors, float[] positions)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x, y), new SKPoint(x, y + h),
            colors, positions, SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };
        canvas.DrawRect(x, y, w, h, paint);
    }

    /// <summary>
    /// Fills a rounded rectangle with a top-to-bottom two-stop gradient.
    /// <paramref name="cornerRadius"/> is in pixels and applies to both axes.
    /// </summary>
    public static void FillRoundRectVerticalGradient(this SKCanvas canvas, float x, float y, float w, float h, float cornerRadius, SKColor colorTop, SKColor colorBottom)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(x, y), new SKPoint(x, y + h),
            [colorTop, colorBottom], null, SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRoundRect(x, y, w, h, cornerRadius, cornerRadius, paint);
    }

    /// <summary>
    /// Fills a rounded rectangle with a radial gradient - a glow or a domed highlight.
    /// <paramref name="radius"/> is where the outermost stop lands and <paramref name="cornerRadius"/>
    /// applies to both axes, both in pixels; <paramref name="colors"/> runs from the center outward,
    /// with <paramref name="positions"/> placing each one at 0..1.
    ///
    /// NOTE: the gradient is centered on (w/2, h/2) in CANVAS coordinates, not on the rectangle's own
    /// center, so it only lines up when the rectangle is drawn at the canvas origin. That's the normal
    /// case for a control, whose canvas is already translated to its top-left before it draws
    /// (<paramref name="x"/> and <paramref name="y"/> are then 0). Pass a nonzero x/y and the
    /// highlight shifts off-center.
    /// </summary>
    public static void FillRoundRectRadialGradient(this SKCanvas canvas, float x, float y, float w, float h, float radius, float cornerRadius, SKColor[] colors, float[] positions)
    {
        using var shader = SKShader.CreateRadialGradient(new SKPoint(w / 2, h / 2), radius,
            colors, positions, SKShaderTileMode.Clamp);
        using SKPaint paint = new()
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };
        canvas.DrawRoundRect(x, y, w, h, cornerRadius, cornerRadius, paint);
    }
}
