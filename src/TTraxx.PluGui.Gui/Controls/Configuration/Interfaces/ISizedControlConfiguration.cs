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
    (int Width, int Height) ResolveBounds();
}
