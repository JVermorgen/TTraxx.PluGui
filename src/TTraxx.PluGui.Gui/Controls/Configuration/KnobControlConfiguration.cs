using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class KnobControlConfiguration() : ParameterControlConfiguration
{
    public required IParameterControlInfo ParameterInfo { get; init; }
    public KnobSizes KnobSize { get; init; } = KnobSizes.S;
    public required Func<double> GetNormalizedValue { get; init; }
    public double MinValue { get; init; } = 0f;
    public double MaxValue { get; init; } = 1f;
    public required Action BeginAction { get; init; }
    public required Action<double> PerformAction { get; init; }
    public required Action EndAction { get; init; }
    public Func<double, string>? CustomValueFormatter { get; init; }
}

public enum KnobSizes
{
    S,
    M,
    L,
    XL
}