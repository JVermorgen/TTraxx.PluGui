namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a two-dimensional pad that drives one parameter per axis from a single dragged dot -
/// for values a user thinks of as one gesture (a morph position) rather than two numbers.
/// </summary>
public class XYPadControlConfiguration() : ParameterControlConfiguration, IStyledControlConfiguration<XYPadStyle>
{
    /// <summary>Parameter driven by the dot's horizontal position (0 = left edge).</summary>
    public required ParameterBinding XParameter { get; init; }

    /// <summary>Parameter driven by the dot's vertical position (0 = bottom edge).</summary>
    public required ParameterBinding YParameter { get; init; }

    /// <summary>
    /// Opens a host-side edit group around a drag, so the two parameters this pad moves are recorded
    /// as ONE user gesture instead of two interleaved ones. Optional: without it the individual
    /// parameter begin/end brackets still fire and automation is still correct, just not grouped.
    /// </summary>
    public Action? BeginGroupEdit { get; init; }

    /// <summary>Closes the group opened by <see cref="BeginGroupEdit"/>.</summary>
    public Action? EndGroupEdit { get; init; }

    /// <summary>Optional read-only modulation overlay - see <see cref="XYPadModulationIndicator"/>.</summary>
    public XYPadModulationIndicator? ModulationIndicator { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="XYPadStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<XYPadStyle>? Style { get; init; }
}

/// <summary>
/// Optional, read-only visualization of where a modulation source would
/// push the morph position at full deflection - no interaction of its
/// own, just a dot + connecting line drawn on top of the main dot.
/// </summary>
/// <param name="GetOffsetX">Bipolar value (-1..1) - how far the indicator deviates from the main dot on the X axis at full modulation.</param>
/// <param name="GetOffsetY">Same for the Y axis.</param>
public sealed record XYPadModulationIndicator(Func<double> GetOffsetX, Func<double> GetOffsetY);
