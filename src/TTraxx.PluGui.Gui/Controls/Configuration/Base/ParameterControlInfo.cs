using TTraxx.PluGui.Gui;

namespace TTraxx.PluGui.Gui;

public class ParameterControlInfo() : IParameterControlInfo
{
    public required int ParameterId { get; init; }
    public required string Label { get; init; }
    public required double DefaultNormalizedValue { get; init; }
    public required string Unit { get; init; }

    public int StepCount { get; init; }

    public int? PositionCount => StepCount is int steps && steps != 0 ? steps + 1 : null;
}
