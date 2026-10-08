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

/// <summary>
/// The keys a control can act on while it has the keyboard (see PluginControl.TakeKeyboardFocus):
/// editing and navigation keys, and the letters of the usual clipboard shortcuts. Typed text
/// arrives separately, as characters (PluginControl.OnTextInput), already through the keyboard
/// layout - so a key here is never the way to read what was typed.
/// </summary>
public enum Key
{
    /// <summary>Any key not listed below.</summary>
    None,
    Backspace,
    Delete,
    Left,
    Right,
    Up,
    Down,
    Home,
    End,
    Enter,
    Escape,
    Tab,

    /// <summary>The A key - with Control, select all.</summary>
    A,

    /// <summary>The C key - with Control, copy.</summary>
    C,

    /// <summary>The V key - with Control, paste.</summary>
    V,

    /// <summary>The X key - with Control, cut.</summary>
    X,
}

/// <summary>A key pressed while a control has the keyboard.</summary>
/// <param name="Key">Which key, or <see cref="Key.None"/> for one a control can't act on.</param>
/// <param name="Modifiers">Modifiers held at the time.</param>
public readonly record struct KeyEventArgs(Key Key, KeyModifiers Modifiers = KeyModifiers.None);
