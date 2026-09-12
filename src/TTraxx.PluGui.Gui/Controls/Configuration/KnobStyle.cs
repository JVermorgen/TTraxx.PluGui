namespace TTraxx.PluGui.Gui.Controls.Configuration;

public sealed record KnobStyle
{
    public static KnobStyle Default => new();

    private static IReadOnlyDictionary<KnobSizes, int> DefaultRadii
        => new Dictionary<KnobSizes, int>
        {
            { KnobSizes.S, 42 },
            { KnobSizes.M, 46 },
            { KnobSizes.L, 52 },
            { KnobSizes.XL, 64 }
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

    public IReadOnlyDictionary<KnobSizes, int> Radii { get; init; } = DefaultRadii;
}

public enum KnobSizes { S, M, L, XL }