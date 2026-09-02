using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers.Extensions;

public static class SkiaGradientExtensions
{
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