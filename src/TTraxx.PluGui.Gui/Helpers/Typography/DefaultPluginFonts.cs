using SkiaSharp;
using TTraxx.PluGui.Gui.Helpers.Typography.Interfaces;

namespace TTraxx.PluGui.Gui.Helpers.Typography;

/// <summary>
/// Resolves a neutral UI sans-serif per platform, trying each candidate
/// family in order and falling back to Skia's default. Typefaces are
/// resolved once and cached: SKTypeface wraps a native resource, and
/// controls request fonts inside Draw (i.e. every frame).
/// </summary>
public sealed class DefaultPluginFonts : IPluginFonts
{
    private static readonly string[] _familyCandidates = GetFamilyCandidates();

    private readonly Lazy<SKTypeface> _bold = new(() => Resolve(SKFontStyleWeight.Bold));
    private readonly Lazy<SKTypeface> _regular = new(() => Resolve(SKFontStyleWeight.Normal));

    public SKTypeface Bold => _bold.Value;
    public SKTypeface Regular => _regular.Value;

    private static string[] GetFamilyCandidates()
    {
        if (OperatingSystem.IsWindows())
            return ["Segoe UI", "Tahoma", "Arial"];

        if (OperatingSystem.IsMacOS())
            return ["SF Pro Text", "Helvetica Neue", "Helvetica"];

        // Linux and anything else: cover the common desktop distributions.
        return ["DejaVu Sans", "Liberation Sans", "Cantarell", "Ubuntu", "Noto Sans"];
    }

    private static SKTypeface Resolve(SKFontStyleWeight weight)
    {
        foreach (var family in _familyCandidates)
        {
            var typeface = SKTypeface.FromFamilyName(family, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            // FromFamilyName can return null, or silently substitute a
            // different family - only accept an actual match so the
            // candidate list is meaningful rather than short-circuiting
            // on the first call.
            if (typeface is not null &&
                string.Equals(typeface.FamilyName, family, StringComparison.OrdinalIgnoreCase))
            {
                return typeface;
            }

            typeface?.Dispose();
        }

        return SKTypeface.Default;
    }
}
