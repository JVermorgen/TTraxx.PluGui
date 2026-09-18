namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;

/// <summary>
/// NSEvent flag values needed to translate Cocoa modifiers into KeyModifiers. Only the two the
/// library models are declared - see that enum for why Command and Option are left out.
/// </summary>
internal static class NSEventConstants
{
    // NSEvent.ModifierFlags bits.

    /// <summary>NSEventModifierFlagShift.</summary>
    internal const nuint ModifierFlagShift = 1 << 17;

    /// <summary>NSEventModifierFlagControl.</summary>
    internal const nuint ModifierFlagControl = 1 << 18;
}
