
namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a level meter: display-only, so it derives from <see cref="ControlConfiguration"/>
/// rather than <see cref="ParameterControlConfiguration"/> - there's no parameter to edit and
/// nothing for the host to automate. It polls <see cref="GetLevel"/> while the window repaints
/// continuously (see PluginControl.NeedsContinuousRepaint) instead of being pushed new values.
/// </summary>
public sealed class MeterControlConfiguration() : ControlConfiguration, ISizedControlConfiguration
{
    /// <summary>Returns the current level, normalized so that 1.0 is the meter's nominal "0 dB"/full-scale mark. May momentarily exceed 1.0 to show clipping.</summary>
    public required Func<double> GetLevel { get; init; }

    /// <summary>Optional label drawn under the meter (e.g. "Output").</summary>
    public string? Label { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="MeterStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<MeterStyle>? Style { get; init; }

    /// <inheritdoc/>
    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? MeterStyle.Default).Bounds[ControlSize];
}
