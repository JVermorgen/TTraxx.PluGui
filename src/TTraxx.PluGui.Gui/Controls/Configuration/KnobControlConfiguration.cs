using TTraxx.PluGui.Gui;

namespace TTraxx.PluGui.Gui;

public class KnobControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
    public Func<KnobStyle>? Style { get; init; }

    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? KnobStyle.Default).Bounds[ControlSize];
}
