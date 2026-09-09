using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration.Base;

public class ParameterControlInfo() : IParameterControlInfo
{
    public required int ParameterId { get; init; }
    public required string Label { get; init; }
    public required double DefaultNormalizedValue { get; init; }
    public required string Unit { get; init; }

    public int StepCount { get; init; }

    public int? PositionCount => StepCount is int steps && steps != 0 ? steps + 1 : null;
}