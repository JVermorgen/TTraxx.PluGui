namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look of an on/off toggle - currently only its per-size layout box; colors come from the
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
}
