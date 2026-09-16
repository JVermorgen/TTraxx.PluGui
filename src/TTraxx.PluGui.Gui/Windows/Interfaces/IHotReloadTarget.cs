namespace TTraxx.PluGui.Gui;

/// <summary>
/// Dev-tooling only: re-runs a window's layout build so Hot Reload can pick up changed
/// positions, panels, or new/removed controls. A production host NEVER calls this -
/// which is why it's kept off <see cref="IPluginWindow"/> and implemented explicitly,
/// so it doesn't show up on a plugin window's everyday surface.
/// </summary>
public interface IHotReloadTarget
{
    /// <summary>Rebuilds the control layout (controls with unchanged ids keep their state).</summary>
    void RebuildControls();
}
