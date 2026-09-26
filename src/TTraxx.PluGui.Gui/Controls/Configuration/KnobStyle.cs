namespace TTraxx.PluGui.Gui;

/// <summary>
/// Look, feel and drag behaviour of a knob - geometry (arc angles, tick placement, the per-size
/// <see cref="Sizes"/> table) together with the interaction constants (drag distance, fine-tune,
/// wheel resolution). Colors are NOT here: those come from the window's theme, so one style works
/// against any theme.
///
/// A record with init-only members, so a variation is a <c>with</c> expression off
/// <see cref="Default"/> rather than a new type. Lengths are unscaled design units; the control
/// rescales them as it draws.
/// </summary>
public sealed record KnobStyle : IControlStyle<KnobStyle>
{
    /// <summary>The built-in style. A fresh instance per call, so mutating a copy can't affect anything else.</summary>
    public static KnobStyle Default => new();

    /// <summary>
    /// Everything that varies with the knob's size tier: its layout box, the knob's own diameter and its
    /// stroke widths. The box is hand-tuned per size so the labels have room around the arc; XL gets
    /// proportionally heavier strokes. ControlSizes.L isn't assigned to any control yet; its box is
    /// interpolated between M and XL.
    /// </summary>
    public SizeTable<KnobMetrics> Sizes { get; init; } = new()
    {
        //       Width  Height  Diam.   Track  Value
        S  = new(58,    94,     42,     2.0f,  3.0f),
        M  = new(64,    100,    46,     2.0f,  3.0f),
        L  = new(67,    105,    52,     2.0f,  3.0f),
        XL = new(74,    116,    64,     2.8f,  4.2f)
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

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => (Sizes[size].Width, Sizes[size].Height);
}
