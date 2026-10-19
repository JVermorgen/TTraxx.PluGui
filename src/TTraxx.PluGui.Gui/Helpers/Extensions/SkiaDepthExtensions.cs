using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// The pieces of the controls' depth, all lit from above: a raised thing casts a soft shadow below
/// itself, is lighter at the top than at the bottom and catches the light along its top edge; a
/// recessed thing (a groove, a well) is shaded from the top. Coordinates are pixels.
/// </summary>
public static class SkiaDepthExtensions
{
    /// <summary><paramref name="color"/> moved <paramref name="amount"/> (0..1) of the way to white, its alpha kept.</summary>
    public static SKColor Lighter(this SKColor color, float amount) => color.Towards(SKColors.White, amount);

    /// <summary><paramref name="color"/> moved <paramref name="amount"/> (0..1) of the way to black, its alpha kept.</summary>
    public static SKColor Darker(this SKColor color, float amount) => color.Towards(SKColors.Black, amount);

    /// <summary><paramref name="color"/> moved <paramref name="amount"/> (0..1) of the way to <paramref name="target"/>'s red, green and blue, its alpha kept.</summary>
    public static SKColor Towards(this SKColor color, SKColor target, float amount)
    {
        static byte Mix(byte from, byte to, float t) => (byte)Math.Round(from + ((to - from) * t));
        return new(Mix(color.Red, target.Red, amount), Mix(color.Green, target.Green, amount), Mix(color.Blue, target.Blue, amount), color.Alpha);
    }

    /// <summary>
    /// A soft shadow under <paramref name="shape"/>, moved down by <paramref name="offsetY"/>, drawn only
    /// outside the shape - so a translucent fill drawn over it doesn't show it through.
    /// </summary>
    public static void DrawDropShadow(this SKCanvas canvas, SKRoundRect shape, float offsetY, float blur, byte alpha)
    {
        canvas.Save();
        canvas.ClipRoundRect(shape, SKClipOperation.Difference, antialias: true);
        canvas.Translate(0, offsetY);
        using SKPaint paint = new()
        {
            Color = SKColors.Black.WithAlpha(alpha),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur)
        };
        canvas.DrawRoundRect(shape, paint);
        canvas.Restore();
    }

    /// <summary>Fills a raised rounded box: <paramref name="fill"/> a little lighter at its top than at its bottom.</summary>
    public static void FillRaised(this SKCanvas canvas, SKRect box, float radius, SKColor fill)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(box.Left, box.Top), new SKPoint(box.Left, box.Bottom),
            [fill.Lighter(0.08f), fill.Darker(0.18f)], null, SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(box, radius, radius, paint);
    }

    /// <summary>Fills a recessed rounded box: <paramref name="fill"/>, shaded darker towards its top.</summary>
    public static void FillRecessed(this SKCanvas canvas, SKRect box, float radius, SKColor fill)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(box.Left, box.Top), new SKPoint(box.Left, box.Bottom),
            [fill.Darker(0.55f), fill], [0, 0.7f], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(box, radius, radius, paint);
    }

    /// <summary>The light a raised box catches along its top edge, fading out by its middle.</summary>
    public static void DrawTopHighlight(this SKCanvas canvas, SKRect box, float radius, float strokeWidth, byte alpha)
    {
        var inset = strokeWidth / 2f;
        using var shader = SKShader.CreateLinearGradient(new SKPoint(box.Left, box.Top), new SKPoint(box.Left, box.MidY),
            [SKColors.White.WithAlpha(alpha), SKColors.White.WithAlpha(0)], null, SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth };
        canvas.DrawRoundRect(new SKRect(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset),
            Math.Max(0, radius - inset), Math.Max(0, radius - inset), paint);
    }

    /// <summary>
    /// The shadow a recessed shape's top edge casts into it: black at <paramref name="alpha"/> along
    /// its top, gone <paramref name="depth"/> pixels down - fixed, so a tall screen and a short one
    /// look alike at their tops.
    /// </summary>
    public static void DrawInsetShadow(this SKCanvas canvas, SKRoundRect shape, float depth, byte alpha)
    {
        var rect = shape.Rect;
        canvas.Save();
        canvas.ClipRoundRect(shape, SKClipOperation.Intersect, antialias: true);
        using var shader = SKShader.CreateLinearGradient(new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Left, rect.Top + depth),
            [SKColors.Black.WithAlpha(alpha), SKColors.Black.WithAlpha(0)], null, SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader };
        canvas.DrawRect(rect.Left, rect.Top, rect.Width, Math.Min(depth, rect.Height), paint);
        canvas.Restore();
    }

    /// <summary>The light a recessed box's lower lip catches: a faint line just under its bottom edge.</summary>
    public static void DrawBottomLip(this SKCanvas canvas, SKRect box, float radius, float strokeWidth, byte alpha)
    {
        canvas.Save();
        canvas.ClipRect(new SKRect(box.Left - strokeWidth, box.MidY, box.Right + strokeWidth, box.Bottom + (2 * strokeWidth)));
        using SKPaint paint = new() { Color = SKColors.White.WithAlpha(alpha), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth };
        canvas.DrawRoundRect(new SKRect(box.Left, box.Top + strokeWidth, box.Right, box.Bottom + strokeWidth), radius, radius, paint);
        canvas.Restore();
    }
}
