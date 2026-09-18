namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look, feel and drag behaviour of a knob - geometry (arc angles, tick and stroke sizing, the
/// per-size tables) together with the interaction constants (drag distance, fine-tune, wheel
/// resolution). Colors are NOT here: those come from the window's theme, so one style works
/// against any theme.
///
/// A record with init-only members, so a variation is a <c>with</c> expression off
/// <see cref="Default"/> rather than a new type. Lengths are unscaled design units; the control
/// rescales them as it draws.
/// </summary>
public sealed record KnobStyle
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static KnobStyle Default => new();

    private static IReadOnlyDictionary<ControlSizes, int> DefaultRadii
        => new Dictionary<ControlSizes, int>
        {
            { ControlSizes.S, 42 },
            { ControlSizes.M, 46 },
            { ControlSizes.L, 52 },
            { ControlSizes.XL, 64 }
        };

    /// <summary>Angle of the 0.0 end of the arc, in degrees, measured clockwise from straight up.</summary>
    public double StartAngleDeg { get; init; } = -135;

    /// <summary>How far the arc travels from <see cref="StartAngleDeg"/> to reach 1.0, in degrees clockwise.</summary>
    public double SweepDeg { get; init; } = 270;

    /// <summary>Vertical drag distance, in unscaled pixels, for a full 0..1 sweep.</summary>
    public double DragPixelsForFullSweep { get; init; } = 200.0;

    /// <summary>Multiplier applied to DragPixelsForFullSweep while Shift is held, for finer adjustments.</summary>
    public double FineTuneDivisor { get; init; } = 4.0;

    /// <summary>Wheel resolution for continuous parameters: 1/N per tick.</summary>
    public int WheelSteps { get; init; } = 72;

    /// <summary>Where a step tick starts, as a multiple of the knob radius - just outside the arc by default.</summary>
    public float TickInnerRadiusFactor { get; init; } = 1.02f;

    /// <summary>Where a step tick ends, as a multiple of the knob radius.</summary>
    public float TickOuterRadiusFactor { get; init; } = 1.35f;

    /// <summary>Stroke width of the unfilled background arc.</summary>
    public float TrackStrokeWidth { get; init; } = 2.0f;

    /// <summary>Stroke width of the filled value arc - heavier than the track so the value reads first.</summary>
    public float ValueStrokeWidth { get; init; } = 3.0f;

    /// <summary>Track stroke width at <see cref="ControlSizes.XL"/>, which needs proportionally heavier lines.</summary>
    public float TrackStrokeWidthXL { get; init; } = 2.8f;

    /// <summary>Value stroke width at <see cref="ControlSizes.XL"/>.</summary>
    public float ValueStrokeWidthXL { get; init; } = 4.2f;

    /// <summary>
    /// The knob's own drawing radius per size tier - the arc, not the layout box. See
    /// <see cref="Bounds"/> for why the two are separate.
    /// </summary>
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

    /// <summary>
    /// Full layout box per size tier, as consumed by KnobControlConfiguration.ResolveBounds() -
    /// wider and taller than the arc itself because the label sits inside it.
    /// </summary>
    public IReadOnlyDictionary<ControlSizes, (int Width, int Height)> Bounds { get; init; } = DefaultBounds;
}
