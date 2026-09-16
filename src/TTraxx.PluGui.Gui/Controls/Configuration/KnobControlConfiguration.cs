using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class KnobControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
    public Func<KnobStyle>? Style { get; init; }

    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? KnobStyle.Default).Bounds[ControlSize];
}