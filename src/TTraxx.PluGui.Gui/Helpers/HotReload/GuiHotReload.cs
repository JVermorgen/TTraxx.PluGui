namespace TTraxx.PluGui.Gui.Helpers.HotReload;

/// <summary>
/// Signals that Hot Reload applied an edit. The harness subscribes to
/// this to force a repaint; in a DAW nothing ever raises it.
/// </summary>
public static class GuiHotReload
{
    public static event Action? Reloaded;

    internal static void RaiseReloaded() => Reloaded?.Invoke();
}