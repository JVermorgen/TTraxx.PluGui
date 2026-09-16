using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Typefaces used by the library's controls. A consuming plugin can
/// supply its own implementation via PluginDefaults.Fonts; otherwise
/// DefaultPluginFonts resolves a sensible per-platform system font.
/// </summary>
public interface IPluginFonts
{
    SKTypeface Bold { get; }
    SKTypeface Regular { get; }
}
