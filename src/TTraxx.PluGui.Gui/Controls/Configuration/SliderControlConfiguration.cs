namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a horizontal slider bound to one continuous host parameter, with its value printed on
/// the bar. Takes far less room than a knob, which is what a row of matrix routes or any other
/// dense list of amounts needs.
/// </summary>
public class SliderControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    /// <summary>The parameter this slider reads and drives.</summary>
    public required ParameterBinding Parameter { get; init; }

    /// <summary>
    /// Fill from the centre instead of the left edge - for a parameter whose middle means "none"
    /// (a modulation amount, a pan), so the bar shows direction as well as size.
    /// </summary>
    public bool IsBipolar { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="SliderStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<SliderStyle>? Style { get; init; }

    /// <inheritdoc/>
    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? SliderStyle.Default).Bounds[ControlSize];
}
