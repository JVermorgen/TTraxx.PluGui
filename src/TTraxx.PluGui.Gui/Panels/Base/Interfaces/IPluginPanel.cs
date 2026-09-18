using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Minimal drawing contract for reusable panel chrome - the framed, titled boxes a window groups
/// controls into. Unlike PluginControl, a panel has no parameter binding, no
/// pointer events, and no coordinate transformation of its own: the canvas
/// that comes in here is the same canvas as in DrawBackground — so
/// absolute, window-relative coordinates, not the per-control local
/// coordinates that ControlManager.Draw applies.
///
/// A panel is CHROME, not a container: it draws behind its controls and offers them a coordinate
/// origin, but it doesn't own, clip or dispose them - the window's control manager does all of
/// that. Which is why a panel's children still appear in the window's own layout (see
/// <see cref="AddControl"/>).
/// </summary>
public interface IPluginPanel
{
    /// <summary>
    /// Draws the panel's chrome in absolute, window-relative coordinates. Called from the window's
    /// DrawBackground (normally via PluginWindow.DrawRegisteredPanels), so it lands underneath the
    /// controls drawn afterwards.
    /// </summary>
    void Draw(SKCanvas canvas);

    /// <summary>
    /// Registers a control as a child of this panel and returns its placement with
    /// ABSOLUTE (window-relative) coordinates - directly yield-returnable from
    /// BuildLayout(), the same way as a control placed with no panel at all.
    /// <paramref name="relativeX"/>/<paramref name="relativeY"/> are relative to the top-left corner
    /// of the panel body (Left/Top from the configuration); the panel converts that itself.
    ///
    /// You normally never call this: pass <c>panel:</c> to PluginWindow.Place() and it calls this for
    /// you, once per control per layout build. Because of that per-build call, CONSTRUCT A PANEL
    /// INSIDE BuildLayout() and register it there (PluginWindow.RegisterPanel) rather than keeping one
    /// in a field across builds - a panel that outlives a build keeps accumulating registrations in
    /// <see cref="Controls"/>, one full set per rebuild.
    /// </summary>
    ControlPlacement AddControl(PluginControl control, int relativeX, int relativeY, int width, int height);

    /// <summary>
    /// Every child registered on this panel instance, with ABSOLUTE (window-relative) coordinates -
    /// directly yield-returnable from a window's BuildLayout(). Nothing in the framework reads this:
    /// PluginWindow.Place() folds each control into the layout from <see cref="AddControl"/>'s return
    /// value instead, so this is here for a window that wants to yield a panel's children as a group.
    /// </summary>
    IReadOnlyList<ControlPlacement> Controls { get; }
}
