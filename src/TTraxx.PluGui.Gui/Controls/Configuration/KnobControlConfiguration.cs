namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a rotary knob bound to one host parameter - the workhorse control for continuous and
/// stepped values alike (a stepped parameter snaps via <see cref="ParameterBinding.Quantize"/>).
/// </summary>
public class KnobControlConfiguration() : ParameterControlConfiguration, IStyledControlConfiguration<KnobStyle>
{
    /// <summary>The parameter this knob reads and drives.</summary>
    public required ParameterBinding Parameter { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="KnobStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use: that keeps it live under Hot Reload and lets a
    /// style be derived from the current theme.
    /// </summary>
    public Func<KnobStyle>? Style { get; init; }

    /// <summary>
    /// A value centred on the middle (a pan, a bipolar amount): the arc fills from the top of the
    /// knob towards the value, rather than from the bottom-left end.
    /// </summary>
    public bool IsBipolar { get; init; }
}
