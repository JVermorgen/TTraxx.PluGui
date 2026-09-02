namespace TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

public interface IParameterControlInfo
{
    int ParameterId { get; }
    string Unit { get; }
    double DefaultNormalizedValue { get; }
    string Label { get; }
    int? StepCount { get; init; }
}