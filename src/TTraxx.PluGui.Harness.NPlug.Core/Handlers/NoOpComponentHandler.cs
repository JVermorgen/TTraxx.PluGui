using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Handlers;

/// <summary>
/// Minimal no-op host handler for the NPlug GUI harness: does nothing with
/// parameter edits (there is no real host/automation to write back to),
/// but prevents AudioController.GetHandler() from failing in the same way
/// a real VST3 host would always set via setComponentHandler.
/// </summary>
public sealed class NoOpComponentHandler : IAudioControllerHandler
{
    void IAudioControllerHandler.BeginEdit(AudioParameterId id) { /* dummy */ }
    void IAudioControllerHandler.PerformEdit(AudioParameterId id, double valueNormalized) { /* dummy */ }
    void IAudioControllerHandler.EndEdit(AudioParameterId id) { /* dummy */ }
    void IAudioControllerHandler.RestartComponent(AudioRestartFlags flags) { /* dummy */ }
    void IAudioControllerHandler.StartGroupEdit() { /* dummy */ }
    void IAudioControllerHandler.FinishGroupEdit() { /* dummy */ }
    bool IAudioControllerHandler.IsAdvancedEditSupported => true;
    bool IAudioControllerHandler.IsCreateContextMenuSupported => false;
    bool IAudioControllerHandler.IsRequestBusActivationSupported => false;
    bool IAudioControllerHandler.IsProgressSupported => false;
    bool IAudioControllerHandler.IsUnitAndProgramListSupported => false;

    void IAudioControllerHandler.SetDirty(bool state) { }
    void IAudioControllerHandler.RequestOpenEditor(string name) { }
    IAudioContextMenu IAudioControllerHandler.CreateContextMenu(IAudioPluginView plugView, AudioParameterId paramID)
        => throw new NotSupportedException("Context-menu's worden niet ondersteund in de GUI-harness.");
    void IAudioControllerHandler.RequestBusActivation(BusMediaType type, BusDirection dir, int index, bool state) { }
    AudioProgressId IAudioControllerHandler.StartProgress(AudioProgressType type, string? optionalDescription)
        => throw new NotSupportedException("Progress-rapportage wordt niet ondersteund in de GUI-harness.");
    void IAudioControllerHandler.UpdateProgress(AudioProgressId id, double normValue) { }
    void IAudioControllerHandler.FinishProgress(AudioProgressId id) { }
    void IAudioControllerHandler.NotifyUnitSelection(AudioUnitId unitId) { }
    void IAudioControllerHandler.NotifyProgramListChange(AudioProgramListId listId, int programIndex) { }
}
