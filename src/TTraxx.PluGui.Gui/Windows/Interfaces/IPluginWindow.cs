
namespace TTraxx.PluGui.Gui;

/// <summary>
/// Everything a HOST needs from a plugin window - a VST3 editor view, the GUI harness,
/// or any other embedder. This is the "drive it" half of a window; the "describe its
/// content" half (BuildLayout/DrawBackground/Place/...) lives on
/// <see cref="PluginWindow"/> and is not part of this contract.
///
/// Derive a window from <see cref="PluginWindow"/> (which implements this) -
/// implementing this interface directly means reimplementing the control cache, layout
/// and input routing by hand. Depend on THIS type in host-side code, so an adapter can
/// be exercised without a native window.
/// </summary>
public interface IPluginWindow
{
    /// <summary>
    /// This window's scale, theme and fonts. A host sets <see cref="RenderContext.Scale"/> here from
    /// the DPI/content scale it reports, then calls <see cref="SetBounds"/> so the new scale is
    /// applied to the layout. Per window rather than per process - see <see cref="RenderContext"/>.
    /// </summary>
    RenderContext Context { get; }

    /// <summary>
    /// Creates the native platform window as a child of <paramref name="parentHandle"/> and runs the
    /// first layout build. Call once, when the host (VST3 editor, harness, ...) attaches
    /// the plugin's view to its own window.
    /// </summary>
    /// <param name="parentHandle">Native handle (HWND, NSView, X11 Window) of the host's parent window.</param>
    /// <param name="width">Initial window width, in the same (unscaled) units passed to <see cref="SetBounds"/>.</param>
    /// <param name="height">Initial window height.</param>
    /// <returns><c>false</c> if the platform layer failed to attach (e.g. invalid handle); the window is unusable in that case.</returns>
    /// <exception cref="InvalidOperationException">This window is already attached - use one window instance per host view.</exception>
    /// <exception cref="ObjectDisposedException">This window has already been destroyed.</exception>
    bool AttachToParent(nint parentHandle, int width, int height);

    /// <summary>
    /// Resizes/repositions the native window and re-applies the current layout against the new size
    /// (controls don't get rebuilt - only re-bounded), then repaints. Call whenever the host resizes
    /// the plugin view (e.g. the user dragging the editor's corner). No-op once the window has been
    /// destroyed, so a deferred resize that races teardown is safe.
    /// </summary>
    void SetBounds(int x, int y, int width, int height);

    /// <summary>
    /// Requests a repaint without changing layout or control state - e.g. after a model value changed
    /// outside of user input. No-op once the window has been destroyed, so a host refresh timer that
    /// outlives the view is safe.
    /// </summary>
    void RefreshUI();

    /// <summary>
    /// Tears down the native platform window. Call when the host detaches/closes the plugin view.
    /// Idempotent: a second call is a no-op, so overlapping teardown paths are safe.
    /// </summary>
    void Destroy();

    /// <summary>
    /// Hit-tests (<paramref name="x"/>, <paramref name="y"/>) against the current layout and, if a
    /// control bound to a parameter is found there, returns that parameter's id. Used by hosts that
    /// need to resolve "what parameter is under this point" outside of normal pointer dispatch (e.g.
    /// tooltips, host-driven automation gestures).
    /// </summary>
    /// <returns><c>true</c> if a parameter-bound control was found at that position.</returns>
    bool TryFindParameter(int x, int y, out int parameterId);

    /// <summary>Initial DPI/content scale for the current platform (see <see cref="IPlatformWindow.GetInitialScaleFactor"/>).</summary>
    float GetInitialScaleFactor(nint parentHandle);

    /// <summary>Non-null when this platform requires an external event pump (see <see cref="IEventPumpSource"/>).</summary>
    IEventPumpSource? EventPumpSource { get; }

    /// <summary>Non-null when this platform needs an externally-driven repaint timer (see <see cref="ITimerPumpSource"/>).</summary>
    ITimerPumpSource? TimerPumpSource { get; }
}
