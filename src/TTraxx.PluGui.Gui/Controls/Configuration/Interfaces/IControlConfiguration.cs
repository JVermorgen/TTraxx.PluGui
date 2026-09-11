namespace TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;

public interface IControlConfiguration
{
    Guid Id { get; }
    Func<bool> IsEnabled { get; init; }
}
