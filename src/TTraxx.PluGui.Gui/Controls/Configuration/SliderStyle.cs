namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look and feel and drag behaviour of a horizontal slider - geometry and interaction constants
/// only; colors come from the window's theme. A record with init-only members, so a variation is a
/// <c>with</c> expression off <see cref="Default"/>. Lengths are unscaled design units.
/// </summary>
public sealed record SliderStyle : IControlStyle<SliderStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static SliderStyle Default => new();

    /// <summary>Layout box per size tier. Usually overridden per placement with an explicit width.</summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((100, 22));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];

    /// <summary>Corner rounding of the bar.</summary>
    public float CornerRadius { get; init; } = 3f;

    /// <summary>
    /// Horizontal drag distance, in unscaled pixels, for a full 0..1 sweep. Deliberately not tied to
    /// the bar's own width, so a short slider isn't twitchier than a long one.
    /// </summary>
    public double DragPixelsForFullSweep { get; init; } = 200.0;

    /// <summary>Multiplier applied to DragPixelsForFullSweep while Shift is held, for finer adjustments.</summary>
    public double FineTuneDivisor { get; init; } = 4.0;

    /// <summary>Wheel resolution: 1/N of the range per notch.</summary>
    public int WheelSteps { get; init; } = 50;

    /// <summary>Text size of the value readout drawn on the bar.</summary>
    public float FontSize { get; init; } = 10f;
}
