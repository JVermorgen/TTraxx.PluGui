namespace TTraxx.PluGui.Gui.Helpers.HotReload;

/// <summary>
/// Signals that Hot Reload applied an edit, after the library's own icon and font caches have been
/// dropped. Nothing in the library subscribes to it - the harness picks edits up through its
/// periodic RebuildControls() - so it is purely a hook for plugin code that wants to react to an
/// edit directly. In a DAW nothing ever raises it.
/// </summary>
public static class GuiHotReload
{
    public static event Action? Reloaded;

    internal static void RaiseReloaded() => Reloaded?.Invoke();
}