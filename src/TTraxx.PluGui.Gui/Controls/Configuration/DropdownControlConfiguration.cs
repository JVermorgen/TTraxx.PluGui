namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a dropdown bound to one STEPPED host parameter whose positions have names - a choice
/// of source or waveform, say. It shows the current choice in a box and opens the full list on a
/// click. Better than a stepped knob once there are more than a handful of positions, or when the
/// names matter more than their order.
/// </summary>
public class DropdownControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    /// <summary>
    /// The parameter this dropdown reads and drives. Its <see cref="ParameterControlInfo.StepCount"/>
    /// gives the number of positions; a stored value is mapped by that count, not by the number of
    /// <see cref="Items"/>.
    /// </summary>
    public required ParameterBinding Parameter { get; init; }

    /// <summary>
    /// Display names for the first positions, in position order - usually one per position. A list
    /// that will grow can have fewer names than the parameter has positions: the rest are reserved,
    /// never offered, and appending a name later leaves every stored value meaning what it did.
    /// </summary>
    public required IReadOnlyList<string> Items { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="DropdownStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<DropdownStyle>? Style { get; init; }

    /// <inheritdoc/>
    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? DropdownStyle.Default).Bounds[ControlSize];
}
