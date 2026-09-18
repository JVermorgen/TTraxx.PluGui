namespace TTraxx.PluGui.Gui;

/// <summary>
/// Small geometry helpers for control drawing. Styles express angles in DEGREES because that's how
/// they're easiest to reason about and edit, while the trigonometry that positions a knob's arc and
/// pointer needs radians - this is the conversion between the two.
/// </summary>
internal static class GeometryExtensions
{
    /// <summary>Converts degrees to radians.</summary>
    internal static double DegToRad(this double deg) => deg * Math.PI / 180.0;
}
