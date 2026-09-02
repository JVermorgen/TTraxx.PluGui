using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;

namespace TTraxx.PluGui.Gui.Panels.Base.Interfaces;

/// <summary>
/// Minimal drawing contract for reusable panel chrome. Unlike
/// AbstractControlBase, a panel has no parameter binding, no
/// pointer events, and no coordinate transformation of its own: the canvas
/// that comes in here is the same canvas as in DrawBackground — so
/// absolute, window-relative coordinates, not the per-control local
/// coordinates that ControlManager.Draw applies.
/// </summary>
public interface IPluginPanel
{
    void Draw(SKCanvas canvas);

    /// <summary>
    /// Registers a control as a child of this panel. x/y are relative
    /// to the top-left corner of the panel (Left/Top from the configuration),
    /// not absolute window-relative — the panel converts that itself.
    /// Call this ONCE (e.g., in the window constructor), never from
    /// GetLayout itself: that's called multiple times per session (on
    /// attach and on every resize) and would grow the list on re-registration.
    /// </summary>
    void AddControl(AbstractControlBase control, int relativeX, int relativeY, int width, int height);

    /// <summary>All registered children, with ABSOLUTE (window-relative) coordinates — directly usable in GetLayout.</summary>
    IReadOnlyList<(AbstractControlBase Control, int X, int Y, int W, int H)> Controls { get; }
}