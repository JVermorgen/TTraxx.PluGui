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

    /// <summary>Height of the footer band, when the pad has one (see <see cref="XYPadControlConfiguration.Footer"/>).</summary>
    public int FooterHeight { get; init; } = 18;

    /// <summary>Text size of the footer's labels.</summary>
    public float FooterFontSize { get; init; } = 10f;

    /// <summary>Width of the footer's toggle, when it has one - the choices share what is left.</summary>
    public int FooterToggleWidth { get; init; } = 52;

    /// <summary>Radius of a live point, and how many seconds of its path its trail shows.</summary>
    public float LivePointRadius { get; init; } = 3.5f;
    public float LiveTrailSeconds { get; init; } = 0.35f;

    /// <summary>Radius of a snap-point dot, and length of a snap-point tick in from the frame.</summary>
    public float SnapMarkerRadius { get; init; } = 2f;
    public float SnapTickLength { get; init; } = 6f;

    /// <summary>Layout box per size tier - the pad area alone, without a footer. The same at every tier until a plugin needs a second pad size.</summary>
    public SizeTable<(int Width, int Height)> Sizes { get; init; } = SizeTable<(int Width, int Height)>.Uniform((220, 220));

    /// <inheritdoc/>
    public (int Width, int Height) BoundsFor(ControlSizes size) => Sizes[size];
}
