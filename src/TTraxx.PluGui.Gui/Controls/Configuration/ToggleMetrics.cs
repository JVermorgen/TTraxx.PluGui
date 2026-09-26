namespace TTraxx.PluGui.Gui;

/// <summary>
/// Everything about a toggle that changes with its <see cref="ControlSizes"/> tier - one entry of
/// <see cref="ToggleStyle.Sizes"/>. Lengths are unscaled design units.
/// </summary>
/// <param name="Width">Full layout box width.</param>
/// <param name="Height">Full layout box height - taller than the pill, so the label fits underneath.</param>
/// <param name="PillHeight">Height of the pill; its width is twice this, and the knob inside it scales along.</param>
/// <param name="PillCenterY">Vertical position of the pill's centre, measured from the top of the layout box.</param>
public readonly record struct ToggleMetrics(int Width, int Height, int PillHeight, int PillCenterY);
