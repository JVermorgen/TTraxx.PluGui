namespace TTraxx.PluGui.Gui.Input;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Control = 1 << 1,
}

public readonly record struct PointerEventArgs(int X, int Y, KeyModifiers Modifiers = KeyModifiers.None);
public readonly record struct WheelEventArgs(int X, int Y, int Delta, KeyModifiers Modifiers = KeyModifiers.None); // Delta normalised
