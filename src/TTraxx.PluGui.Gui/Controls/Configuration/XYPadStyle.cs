using SkiaSharp;

namespace TTraxx.PluGui.Gui;

public sealed record XYPadStyle : IControlStyle<XYPadStyle>
{
    public static XYPadStyle Default => new();

    public int CornerRadius { get; init; } = 12;
    public int GlowOuterRadius { get; init; } = 11;
    public int GlowMidRadius { get; init; } = 7;
    public int GlowCoreRadius { get; init; } = 3;
    public int IndicatorRingRadius { get; init; } = 5;

    public int IconWidth { get; init; } = 28;
    public int IconHeight { get; init; } = 12;
    public float IconStrokeWidth { get; init; } = 1.5f;

    /// <summary>
    /// Corner glyphs, clockwise from top-left. Defaults to the waveform set;
    /// pass null to draw none (e.g. a cutoff/resonance pad).
    /// </summary>
    public IReadOnlyList<SKPath>? CornerIcons { get; init; } =
        [Icons.Sine, Icons.Saw, Icons.Pulse, Icons.Triangle];

    /// <summary>Layout box per size tier. The same at every tier until a plugin needs a second pad size.</summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((220, 220));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];
}
