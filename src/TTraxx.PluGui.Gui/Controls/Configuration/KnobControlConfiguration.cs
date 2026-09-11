using TTraxx.PluGui.Gui.Controls.Configuration.Base;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class KnobControlConfiguration() : ParameterControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
    public KnobSizes KnobSize { get; init; } = KnobSizes.S;
    public Func<KnobStyle>? Style { get; init; }
}