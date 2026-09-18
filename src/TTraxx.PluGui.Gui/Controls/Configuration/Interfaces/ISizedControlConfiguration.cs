namespace TTraxx.PluGui.Gui;

/// <summary>
/// A control configuration that can compute its own natural (Width, Height) - typically by
/// looking up its ControlSize in its own effective Style's size table.
/// PluginWindow.Place() uses this as the zero-setup default for auto-sizing: implement it
/// once on a configuration type and every window gets sensible sizes - RegisterSizing() 
/// is available for a window that wants to override it anyway.
/// </summary>
public interface ISizedControlConfiguration : IControlConfiguration
{
    /// <summary>
    /// This control's natural layout box in unscaled design units, for its current
    /// <see cref="IControlConfiguration.ControlSize"/>. Includes room for anything drawn as part of
    /// the control (a label underneath, say), not just its interactive area.
    /// </summary>
    (int Width, int Height) ResolveBounds();
}
