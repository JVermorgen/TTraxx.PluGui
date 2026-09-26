namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look and feel of a dropdown - geometry only; colors come from the window's theme. A record with
/// init-only members, so a variation is a <c>with</c> expression off <see cref="Default"/>.
/// Lengths are unscaled design units.
/// </summary>
public sealed record DropdownStyle : IControlStyle<DropdownStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static DropdownStyle Default => new();

    /// <summary>Layout box per size tier. Usually overridden per placement with an explicit width, since a dropdown sizes to its column.</summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((110, 22));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];

    /// <summary>Corner rounding of the box.</summary>
    public float CornerRadius { get; init; } = 3f;

    /// <summary>Gap between the box edge and the text, and between the chevron and the right edge.</summary>
    public int Padding { get; init; } = 7;

    /// <summary>Width of the down-pointing chevron; its height is half this.</summary>
    public float ChevronWidth { get; init; } = 7f;

    /// <summary>Text size of the current choice.</summary>
    public float FontSize { get; init; } = 11f;
}
