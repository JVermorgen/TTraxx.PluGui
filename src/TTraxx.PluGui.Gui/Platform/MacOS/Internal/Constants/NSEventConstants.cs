namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;

/// <summary>
/// NSEvent flag values needed to translate Cocoa modifiers into KeyModifiers: Shift and Control, which
/// the library models, and Command, which a key event reads as Control (see CocoaKeyboard). Option
/// is left out - see KeyModifiers.
/// </summary>
internal static class NSEventConstants
{
    // NSEvent.ModifierFlags bits.

    /// <summary>NSEventModifierFlagShift.</summary>
    internal const nuint ModifierFlagShift = 1 << 17;

    /// <summary>NSEventModifierFlagControl.</summary>
    internal const nuint ModifierFlagControl = 1 << 18;

    /// <summary>NSEventModifierFlagCommand - the Mac's shortcut key, which a text field reads as Control (Cmd+C copies).</summary>
    internal const nuint ModifierFlagCommand = 1 << 20;
}
