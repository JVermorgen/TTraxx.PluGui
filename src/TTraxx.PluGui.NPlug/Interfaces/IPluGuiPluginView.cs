using global::NPlug;
using TTraxx.PluGui.Gui;

namespace TTraxx.PluGui.NPlug;

public interface IPluGuiPluginView : IAudioPluginView
{
    IEventPumpSource? EventPumpSource { get; }

    /// <summary>
    /// The content scale this editor is drawn at (host DPI/content scale times the plugin's own
    /// multiplier). A harness that draws its own chrome around the editor needs this to size that
    /// chrome to match; a real host doesn't.
    /// </summary>
    float Scale { get; }

    void RefreshUI();

    void RebuildControls();
}
