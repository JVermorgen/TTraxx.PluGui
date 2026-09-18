namespace TTraxx.PluGui.Gui;

public abstract class ControlConfiguration : IControlConfiguration
{
    public Guid Id { get; } = Guid.NewGuid();
    public Func<bool> IsEnabled { get; init; } = () => true;

    public ControlSizes ControlSize { get; init; } = ControlSizes.S;
}
