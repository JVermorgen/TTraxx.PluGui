using TTraxx.PluGui.Gui.Controls.Configuration.Base;

namespace TTraxx.PluGui.Gui.Controls.Configuration;

public class ToggleControlConfiguration() : ParameterControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
}
