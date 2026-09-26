namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look and feel of a push button - geometry only; colors come from the window's theme. A record with
/// init-only members, so a variation is a <c>with</c> expression off <see cref="Default"/>.
/// Lengths are unscaled design units.
/// </summary>
public sealed record ButtonStyle : IControlStyle<ButtonStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static ButtonStyle Default => new();

    /// <summary>
    /// Layout box per size tier. Usually overridden per placement with an explicit width, sized to the
    /// label; the height matches a dropdown's, so the two line up in a row.
    /// </summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((80, 22));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];

    /// <summary>Corner rounding.</summary>
    public float CornerRadius { get; init; } = 3f;

    /// <summary>Text size of the label.</summary>
    public float FontSize { get; init; } = 10f;
}
