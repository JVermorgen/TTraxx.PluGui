namespace TTraxx.PluGui.Gui;

public interface IControlConfiguration
{
    Guid Id { get; }
    Func<bool> IsEnabled { get; init; }

    ControlSizes ControlSize { get; init; }
}
