using TTraxx.PluGui.Gui.Controls.Configuration.Base;
using TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class ToggleControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
    public Func<ToggleStyle>? Style { get; init; }

    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? ToggleStyle.Default).Bounds[ControlSize];
}
