namespace TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;

public interface IParameterControlConfiguration
{
    Guid Id { get; }
    Func<bool> IsEnabled { get; init; }
}
