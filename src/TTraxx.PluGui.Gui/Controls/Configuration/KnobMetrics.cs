namespace TTraxx.PluGui.Gui;

/// <summary>
/// Everything about a knob that changes with its <see cref="ControlSizes"/> tier - one entry of
/// <see cref="KnobStyle.Sizes"/>. Kept together so a tier can't end up with a layout box from one
/// table and a knob size from another. Lengths are unscaled design units.
/// </summary>
/// <param name="Width">Full layout box width - wider than the arc, so the range labels fit.</param>
/// <param name="Height">Full layout box height - taller than the arc, so the title fits underneath.</param>
/// <param name="Diameter">Size of the knob itself - the arc, not the box. The arc is drawn just inside it.</param>
/// <param name="TrackStrokeWidth">Stroke width of the unfilled background arc.</param>
/// <param name="ValueStrokeWidth">Stroke width of the filled value arc - heavier than the track so the value reads first.</param>
public readonly record struct KnobMetrics(int Width, int Height, int Diameter, float TrackStrokeWidth, float ValueStrokeWidth);
