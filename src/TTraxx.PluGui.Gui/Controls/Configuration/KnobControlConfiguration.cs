namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a rotary knob bound to one host parameter - the workhorse control for continuous and
/// stepped values alike (a stepped parameter snaps via <see cref="ParameterBinding.Quantize"/>).
/// </summary>
public class KnobControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    /// <summary>The parameter this knob reads and drives.</summary>
    public required ParameterBinding Parameter { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="KnobStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use: that keeps it live under Hot Reload and lets a
    /// style be derived from the current theme.
    /// </summary>
    public Func<KnobStyle>? Style { get; init; }

    /// <inheritdoc/>
    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? KnobStyle.Default).Bounds[ControlSize];
}
