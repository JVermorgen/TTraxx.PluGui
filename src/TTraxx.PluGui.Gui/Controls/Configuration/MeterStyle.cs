using SkiaSharp;

namespace TTraxx.PluGui.Gui;

public sealed record MeterStyle : IControlStyle<MeterStyle>
{
    public static MeterStyle Default => new();

    public int SegmentCount { get; init; } = 14;
    public int SegmentGapPx { get; init; } = 2;

    /// <summary>Normalized level (0..1) where the warning (yellow) zone begins.</summary>
    public double WarningThreshold { get; init; } = 0.75;

    /// <summary>Normalized level (0..1) where the clip (red) zone begins.</summary>
    public double ClipThreshold { get; init; } = 0.95;

    /// <summary>
    /// Levels below this snap to exact 0 before lighting segments. Filter/resonator
    /// feedback tails asymptotically approach zero without ever hitting it exactly,
    /// so without a floor the bottom segment would stay lit forever on a decaying tail.
    /// </summary>
    public double SilenceThreshold { get; init; } = 0.001;

    /// <summary>How long the peak-hold marker stays put before it starts falling back down.</summary>
    public double PeakHoldSeconds { get; init; } = 0.8;

    /// <summary>Fall-back speed of the peak-hold marker, in normalized level per second, once it starts decaying.</summary>
    public double PeakHoldDecayPerSecond { get; init; } = 1.2;

    // Deliberately fixed, theme-independent hues: audio meters use the same
    // green/amber/red convention regardless of the surrounding plugin's look.
    public SKColor NormalColor { get; init; } = new(80, 220, 120);
    public SKColor WarningColor { get; init; } = new(240, 200, 60);
    public SKColor ClipColor { get; init; } = new(230, 70, 70);

    /// <summary>Layout box per size tier - as tall as a knob of the same tier, so a meter lines up in a row of knobs.</summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = new()
    {
        S = (24, 94),
        M = (28, 100),
        L = (30, 105),
        XL = (30, 116)
    };

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];
}
