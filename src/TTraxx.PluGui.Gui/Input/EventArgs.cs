namespace TTraxx.PluGui.Gui.Input;

public readonly record struct PointerEventArgs(int X, int Y);
public readonly record struct WheelEventArgs(int X, int Y, int Delta); // Delta normalised
