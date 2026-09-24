namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look and feel of a tab strip - geometry only; colors come from the window's theme. A record with
/// init-only members, so a variation is a <c>with</c> expression off <see cref="Default"/>.
/// Lengths are unscaled design units.
/// </summary>
public sealed record TabStripStyle
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static TabStripStyle Default => new();

    /// <summary>Sized to sit inside a panel's title band, which is 24 units high.</summary>
    private static IReadOnlyDictionary<ControlSizes, (int Width, int Height)> DefaultBounds
        => new Dictionary<ControlSizes, (int Width, int Height)>
        {
            { ControlSizes.S, (150, 17) }
        };

    /// <summary>Layout box per size tier. Usually overridden per placement with an explicit width, sized to the labels.</summary>
    public IReadOnlyDictionary<ControlSizes, (int Width, int Height)> Bounds { get; init; } = DefaultBounds;

    /// <summary>Corner rounding of the strip and of the selected tab's highlight.</summary>
    public float CornerRadius { get; init; } = 3f;

    /// <summary>Text size of the tab labels.</summary>
    public float FontSize { get; init; } = 10f;
}
