using TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration.Base
{
    public abstract class ParameterControlConfiguration : IParameterControlConfiguration
    {
        public Guid Id { get; } = Guid.NewGuid();
        public Func<bool> IsEnabled { get; init; } = () => true;
    }
}
