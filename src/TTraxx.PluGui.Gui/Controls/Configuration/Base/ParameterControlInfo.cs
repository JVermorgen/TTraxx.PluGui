using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration.Base;

public class ParameterControlInfo() : IParameterControlInfo
{
    public required int ParameterId { get; init; }
    public required string Label { get; init; }
    public required string Unit { get; init; }
    public int? StepCount { get; init => field = value == 0 ? null : value + 1; }  // number of positions, null = continuous
    public required double DefaultNormalizedValue { get; init; }
}