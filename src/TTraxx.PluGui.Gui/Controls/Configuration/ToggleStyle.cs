using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look of an on/off toggle - its appearance and per-size layout box; colors come from the
/// window's theme. A record with init-only members, so a variation is a <c>with</c> expression off
/// <see cref="Default"/>. Lengths are unscaled design units.
/// </summary>
public sealed record ToggleStyle : IControlStyle<ToggleStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static ToggleStyle Default => new();

    /// <summary>
    /// Everything that varies with the toggle's size tier. The layout boxes are the same as a knob's,
    /// so a toggle lines up in a row of knobs.
    /// </summary>
    public SizeTable<ToggleMetrics> Sizes { get; init; } = new()
    {
        //      Width Height Pill CenterY
        S = new(58, 94, 14, 44),
        M = new(64, 100, 14, 44),
        L = new(67, 105, 18, 54),
        XL = new(74, 116, 18, 54)
    };

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => (Sizes[size].Width, Sizes[size].Height);

    /// <summary>
    /// A pill with the label underneath (the default), or a <see cref="ToggleAppearance.Chip"/>: a
    /// labelled box that lights up while on. A chip fills whatever box it is placed in - give it an
    /// explicit width and height rather than relying on <see cref="Sizes"/>, which are pill-shaped.
    /// </summary>
    public ToggleAppearance Appearance { get; init; } = ToggleAppearance.Pill;

    /// <summary>Corner rounding of a chip.</summary>
    public float ChipCornerRadius { get; init; } = 3f;

    /// <summary>Text size of a chip's label.</summary>
    public float ChipFontSize { get; init; } = 10f;

    /// <summary>
    /// A filled glyph drawn before a chip's label while it is OFF - a padlock (<see cref="Icons.Padlock"/>)
    /// for a toggle whose off means "locked", say. Null for none. In the normalized space described on
    /// <see cref="Icons"/>.
    /// </summary>
    public SKPath? ChipOffIcon { get; init; }
}

/// <summary>How a <see cref="ToggleControl"/> draws itself - see <see cref="ToggleStyle.Appearance"/>.</summary>
public enum ToggleAppearance
{
    /// <summary>A sliding pill switch with the label underneath, sized to line up with a row of knobs.</summary>
    Pill,

    /// <summary>A labelled box filling its placement, lit with the accent while on - for rows of related switches.</summary>
    Chip
}
