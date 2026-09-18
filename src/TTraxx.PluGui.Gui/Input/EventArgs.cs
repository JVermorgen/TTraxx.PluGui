namespace TTraxx.PluGui.Gui;

/// <summary>
/// Keyboard modifiers reported with a pointer or wheel event. A deliberately minimal set rather
/// than a mirror of every platform's full modifier state: Alt/Option and Command are left out
/// because hosts routinely claim them for their own gestures, so a plugin can't rely on receiving
/// them. Every platform backend maps its native flags onto these.
/// </summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier held.</summary>
    None = 0,

    /// <summary>Shift - used by the built-in knob for fine-tuning a drag (see KnobStyle.FineTuneDivisor).</summary>
    Shift = 1 << 0,

    /// <summary>
    /// Control. Reported by every platform backend, but no built-in control acts on it yet - it's
    /// here for custom controls to use.
    /// </summary>
    Control = 1 << 1,
}

/// <summary>
/// A pointer event in the receiving control's LOCAL coordinates, with (0,0) at its top-left.
/// While a control holds the pointer capture from a press, X/Y can fall outside its bounds and go
/// negative - see PluginControl.OnPointerMove.
/// </summary>
/// <param name="X">Local horizontal position in pixels.</param>
/// <param name="Y">Local vertical position in pixels.</param>
/// <param name="Modifiers">Modifiers held at the time of the event.</param>
public readonly record struct PointerEventArgs(int X, int Y, KeyModifiers Modifiers = KeyModifiers.None);

/// <summary>
/// A wheel event in the receiving control's LOCAL coordinates. <paramref name="Delta"/> is
/// normalized by the platform layer into wheel notches, so a control gets the same numbers on
/// Windows, macOS and X11 instead of each platform's raw units.
/// </summary>
/// <param name="X">Local horizontal position in pixels.</param>
/// <param name="Y">Local vertical position in pixels.</param>
/// <param name="Delta">Notches scrolled: positive away from the user, negative toward them. Usually +/-1, but a fast or high-resolution wheel can report more per event.</param>
/// <param name="Modifiers">Modifiers held at the time of the event.</param>
public readonly record struct WheelEventArgs(int X, int Y, int Delta, KeyModifiers Modifiers = KeyModifiers.None);
