using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class XYPadControlConfiguration() : ParameterControlConfiguration
{
    public required IParameterControlInfo ParameterXInfo { get; init; }
    public required IParameterControlInfo ParameterYInfo { get; init; }
    public required Func<double> GetNormalizedXValue { get; init; }
    public required Func<double> GetNormalizedYValue { get; init; }
    public double MinXValue { get; init; } = 0f;
    public double MaxXValue { get; init; } = 1f;
    public double MinYValue { get; init; } = 0f;
    public double MaxYValue { get; init; } = 1f;
    public required Action BeginGroupAction { get; init; }
    public required Action BeginXAction { get; init; }
    public required Action<double> PerformXAction { get; init; }
    public required Action EndXAction { get; init; }
    public required Action BeginYAction { get; init; }
    public required Action<double> PerformYAction { get; init; }
    public required Action EndYAction { get; init; }
    public required Action EndGroupAction { get; init; }
    public XYPadModulationIndicator? MorphEnvelopeIndicator { get; init; }
}

/// <summary>
/// Optional, read-only visualization of where a modulation source would
/// push the morph position at full deflection - no interaction of its
/// own, just a dot + connecting line drawn on top of the main dot.
/// </summary>
/// <param name="GetOffsetX">Bipolar value (-1..1) - how far the indicator deviates from the main dot on the X axis at full modulation.</param>
/// <param name="GetOffsetY">Same for the Y axis.</param>
public sealed record XYPadModulationIndicator(Func<double> GetOffsetX, Func<double> GetOffsetY);