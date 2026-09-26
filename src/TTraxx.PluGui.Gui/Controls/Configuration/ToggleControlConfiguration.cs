namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares an on/off pill switch bound to one host parameter. The parameter is read as on at
/// normalized 0.5 and above, and a click writes a full 0.0 or 1.0 - so it suits a genuinely
/// two-state parameter rather than a stepped one with several positions.
/// </summary>
public class ToggleControlConfiguration() : ParameterControlConfiguration, IStyledControlConfiguration<ToggleStyle>
{
    /// <summary>The parameter this toggle reads and drives.</summary>
    public required ParameterBinding Parameter { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="ToggleStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<ToggleStyle>? Style { get; init; }
}
