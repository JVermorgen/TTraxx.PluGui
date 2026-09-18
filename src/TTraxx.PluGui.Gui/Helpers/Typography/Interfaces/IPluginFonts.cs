using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Typefaces used by the library's controls. A consuming plugin can
/// supply its own implementation via <see cref="PluginDefaults.Fonts"/>; otherwise
/// <see cref="DefaultPluginFonts"/> resolves a sensible per-platform system font.
///
/// Two weights, no sizes: a control picks its own size from its style and rescales it, so a font
/// set only has to answer "which typeface", not "how big".
/// </summary>
public interface IPluginFonts
{
    /// <summary>Used for labels and anything that must stay legible at small sizes.</summary>
    SKTypeface Bold { get; }

    /// <summary>Used for longer or secondary text, where weight would be too loud.</summary>
    SKTypeface Regular { get; }
}
