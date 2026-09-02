using NPlug;
using TTraxx.PluGui.Gui.Windows.Interfaces;

namespace TTraxx.PluGui.NPlug.Interfaces;

public interface IPluGuiPluginView : IAudioPluginView
{
    IEventPumpSource? EventPumpSource { get; }

    void RefreshUI();

    void RebuildControls();
}
