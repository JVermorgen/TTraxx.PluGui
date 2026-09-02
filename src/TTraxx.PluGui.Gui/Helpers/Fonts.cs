using SkiaSharp;

namespace TTraxx.PluGui.Gui.Helpers;

public static class Fonts
{
    public static SKTypeface SegoeBold => SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
    public static SKTypeface SegoeRegular => SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
}