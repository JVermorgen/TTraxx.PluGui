namespace TTraxx.PluGui.Gui.Helpers;

public static class Globals
{
    public static float ScaleFactor { get; set; } = 1.0f; // final content scale = hostScale * UserUiScale

    public static int Rescale(float basevalue) => (int)Math.Round(ScaleFactor * basevalue);

    public static float RescaleExact(float basevalue) => ScaleFactor * basevalue;
}
