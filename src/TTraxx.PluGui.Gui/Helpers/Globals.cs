namespace TTraxx.PluGui.Gui.Helpers;

public static class Globals
{
    public static float ScaleFactor { get; set; } = 1.0f; // final content scale = hostScale * UserUiScale

    public static int Rescale(float basevalue) => (int)Math.Round(ScaleFactor * basevalue);

    public static float RescaleExact(float basevalue) => ScaleFactor * basevalue;

    /// <summary>
    /// Dev-tool switch: when true, ControlManager draws a debug outline around
    /// every control's bounds. Defaults to false, and nothing in the shipped
    /// plugin ever sets it - only the harness does, so this stays inert in
    /// production builds.
    /// </summary>
    public static bool ShowControlBounds { get; set; }
}
