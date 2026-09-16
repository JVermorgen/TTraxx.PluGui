namespace TTraxx.PluGui.Gui;

/// <summary>
/// Process-wide development switches. Deliberately tiny: anything that varies per window (scale,
/// theme, fonts) lives on that window's <see cref="RenderContext"/> instead - see the note there
/// about why a static scale factor is a bug waiting to happen.
/// </summary>
public static class Globals
{
    /// <summary>
    /// Dev-tool switch: when true, every control is drawn with a debug outline around its bounds.
    /// Defaults to false, and nothing in a shipped plugin ever sets it - only the harness does, so
    /// this stays inert in production builds. Genuinely process-wide: it's a developer's view
    /// preference, not a property of any one window.
    /// </summary>
    public static bool ShowControlBounds { get; set; }
}
