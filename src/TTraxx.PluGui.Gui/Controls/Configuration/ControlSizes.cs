namespace TTraxx.PluGui.Gui;

/// <summary>
/// Size buckets a control can render at. Deliberately a small set of named steps rather than
/// free pixel sizes: each style maps these to hand-tuned bounds (see <see cref="KnobStyle"/>'s
/// size tables), so controls across a plugin stay visually consistent instead of drifting a few
/// pixels apart. Pass explicit width/height to PluginWindow.Place() for the rare one-off that
/// genuinely needs its own size.
/// </summary>
public enum ControlSizes
{
    /// <summary>Small - the default, and the size most styles are tuned around.</summary>
    S,

    /// <summary>Medium.</summary>
    M,

    /// <summary>Large.</summary>
    L,

    /// <summary>Extra large - for a control that should read as the section's primary one.</summary>
    XL
}
