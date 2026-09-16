namespace TTraxx.PluGui.Gui.Controls.Configuration;

public sealed record KnobStyle
{
    public static KnobStyle Default => new();

    private static IReadOnlyDictionary<ControlSizes, int> DefaultRadii
        => new Dictionary<ControlSizes, int>
        {
            { ControlSizes.S, 42 },
            { ControlSizes.M, 46 },
            { ControlSizes.L, 52 },
            { ControlSizes.XL, 64 }
        };

    public double StartAngleDeg { get; init; } = -135;
    public double SweepDeg { get; init; } = 270;

    /// <summary>Vertical drag distance, in unscaled pixels, for a full 0..1 sweep.</summary>
    public double DragPixelsForFullSweep { get; init; } = 200.0;

    /// <summary>Multiplier applied to DragPixelsForFullSweep while Shift is held, for finer adjustments.</summary>
    public double FineTuneDivisor { get; init; } = 4.0;

    /// <summary>Wheel resolution for continuous parameters: 1/N per tick.</summary>
    public int WheelSteps { get; init; } = 72;

    public float TickInnerRadiusFactor { get; init; } = 1.02f;
    public float TickOuterRadiusFactor { get; init; } = 1.35f;

    public float TrackStrokeWidth { get; init; } = 2.0f;
    public float ValueStrokeWidth { get; init; } = 3.0f;
    public float TrackStrokeWidthXL { get; init; } = 2.8f;
    public float ValueStrokeWidthXL { get; init; } = 4.2f;

    public IReadOnlyDictionary<ControlSizes, int> Radii { get; init; } = DefaultRadii;

    /// <summary>
    /// A knob's full layout box per ControlSizes tier - hand-tuned per size so its label has
    /// room to sit underneath it. Deliberately separate from Radii (the knob's own drawing
    /// size): a bigger label needs more box than the arc alone would. ControlSizes.L isn't
    /// assigned to any control yet; its box is interpolated between M and XL.
    /// </summary>
    private static IReadOnlyDictionary<ControlSizes, (int Width, int Height)> DefaultBounds
        => new Dictionary<ControlSizes, (int Width, int Height)>
        {
            { ControlSizes.S, (58, 94) },
            { ControlSizes.M, (64, 100) },
            { ControlSizes.L, (67, 105) },
            { ControlSizes.XL, (74, 116) }
        };

    public IReadOnlyDictionary<ControlSizes, (int Width, int Height)> Bounds { get; init; } = DefaultBounds;
}
