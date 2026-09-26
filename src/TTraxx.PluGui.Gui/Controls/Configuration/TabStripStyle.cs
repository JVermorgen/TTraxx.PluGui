namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look and feel of a tab strip - geometry only; colors come from the window's theme. A record with
/// init-only members, so a variation is a <c>with</c> expression off <see cref="Default"/>.
/// Lengths are unscaled design units.
/// </summary>
public sealed record TabStripStyle : IControlStyle<TabStripStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static TabStripStyle Default => new();

    /// <summary>
    /// Layout box per size tier. Usually overridden per placement with an explicit width, sized to the
    /// labels; the height is sized to sit inside a panel's title band, which is 24 units high.
    /// </summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((150, 17));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];

    /// <summary>Corner rounding of the strip and of the selected tab's highlight.</summary>
    public float CornerRadius { get; init; } = 3f;

    /// <summary>Text size of the tab labels.</summary>
    public float FontSize { get; init; } = 10f;
}
