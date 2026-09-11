using TTraxx.PluGui.Gui.Controls.Configuration.Base;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class XYPadControlConfiguration() : ParameterControlConfiguration
{
    public required ParameterBinding XParameter { get; init; }
    public required ParameterBinding YParameter { get; init; }

    public Action? BeginGroupEdit { get; init; }
    public Action? EndGroupEdit { get; init; }

    public XYPadModulationIndicator? ModulationIndicator { get; init; }
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