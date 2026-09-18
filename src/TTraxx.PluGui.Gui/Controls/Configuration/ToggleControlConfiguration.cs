namespace TTraxx.PluGui.Gui;

public class ToggleControlConfiguration() : ParameterControlConfiguration, ISizedControlConfiguration
{
    public required ParameterBinding Parameter { get; init; }
    public Func<ToggleStyle>? Style { get; init; }

    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? ToggleStyle.Default).Bounds[ControlSize];
}
