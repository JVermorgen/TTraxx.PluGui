using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class ToggleControlConfiguration() : ParameterControlConfiguration
{
    public required IParameterControlInfo ParameterInfo { get; init; }
    public required Func<double> GetNormalizedValue { get; init; }
    public required Action BeginAction { get; init; }
    public required Action<double> PerformAction { get; init; }
    public required Action EndAction { get; init; }
}
