using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Constants;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal;

/// <summary>
/// <para>
/// The keyboard and the clipboard for MacOsPlatformWindow, Cocoa's counterparts of Win32's Keyboard.
/// Not verified on a Mac yet.
/// </para>
/// <para>
/// Keys: while a control edits text the view is its window's first responder, so AppKit sends it
/// keyDown: (and, for Cmd+key, performKeyEquivalent: first - before the host's menu can take Cmd+C).
/// A key is read from the event's hardware keyCode (arrows, Delete...), a shortcut from its
/// charactersIgnoringModifiers (so Cmd+A follows the layout, as Ctrl+A does on Windows), and the text
/// from its characters - through the layout, Shift and Option, dead keys composed by the system.
/// </para>
/// <para>
/// Clipboard: the general NSPasteboard, as plain text.
/// </para>
/// </summary>
internal static class CocoaKeyboard
{
    private static readonly nint s_selKeyCode = ObjC.Sel("keyCode");
    private static readonly nint s_selCharacters = ObjC.Sel("characters");
    private static readonly nint s_selCharactersIgnoringModifiers = ObjC.Sel("charactersIgnoringModifiers");
    private static readonly nint s_selModifierFlags = ObjC.Sel("modifierFlags");

    // NSPasteboardTypeString's value: the UTI for plain text.
    private const string PasteboardTypeString = "public.utf8-plain-text";

    /// <summary>The event's modifiers, Command read as Control: the Mac's shortcuts are Cmd+key.</summary>
    public static KeyModifiers ModifiersOf(nint keyEvent)
    {
        var flags = ObjC.MsgSendNUInt(keyEvent, s_selModifierFlags);
        var modifiers = KeyModifiers.None;
        if ((flags & NSEventConstants.ModifierFlagShift) != 0) modifiers |= KeyModifiers.Shift;
        if ((flags & (NSEventConstants.ModifierFlagCommand | NSEventConstants.ModifierFlagControl)) != 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }

    /// <summary>A key event as a key (Key.None for one that's only text) and the text it types (null for none).</summary>
    public static (Key Key, string? Text) Read(nint keyEvent)
    {
        var key = KeyOf(ObjC.MsgSendUShort(keyEvent, s_selKeyCode));
        if (key != Key.None) return (key, null);

        var plain = ObjC.ToManagedString(ObjC.MsgSend(keyEvent, s_selCharactersIgnoringModifiers));
        key = plain?.ToUpperInvariant() switch { "A" => Key.A, "C" => Key.C, "V" => Key.V, "X" => Key.X, _ => Key.None };

        // Cmd+letter or Ctrl+letter is a shortcut, not text.
        if ((ModifiersOf(keyEvent) & KeyModifiers.Control) != 0) return (key, null);

        var text = ObjC.ToManagedString(ObjC.MsgSend(keyEvent, s_selCharacters));
        return (key, IsText(text) ? text : null);
    }

    // Text, not a control character or one of the private-use characters (U+F700-U+F8FF) Cocoa
    // gives function keys such as F1 or Page Up.
    private static bool IsText(string? text)
        => !string.IsNullOrEmpty(text) && !text.Any(c => char.IsControl(c) || c is >= '' and <= '');

    // Virtual key codes from Carbon's Events.h (kVK_...): positions on the keyboard, the same in every layout.
    private static Key KeyOf(ushort keyCode) => keyCode switch
    {
        51 => Key.Backspace,     // kVK_Delete
        117 => Key.Delete,       // kVK_ForwardDelete
        48 => Key.Tab,
        36 or 76 => Key.Enter,   // kVK_Return, kVK_ANSI_KeypadEnter
        53 => Key.Escape,
        115 => Key.Home,
        119 => Key.End,
        123 => Key.Left,
        124 => Key.Right,
        125 => Key.Down,
        126 => Key.Up,
        _ => Key.None,
    };

    // ---------------------------------------------------------------- clipboard

    private static nint GeneralPasteboard => ObjC.MsgSend(ObjC.GetClass("NSPasteboard"), ObjC.Sel("generalPasteboard"));

    public static string? GetClipboardText()
    {
        var pasteboard = GeneralPasteboard;
        return pasteboard == nint.Zero
            ? null
            : ObjC.ToManagedString(ObjC.MsgSendIdWithIntPtr(pasteboard, ObjC.Sel("stringForType:"), ObjC.NSString(PasteboardTypeString)));
    }

    public static void SetClipboardText(string text)
    {
        var pasteboard = GeneralPasteboard;
        if (pasteboard == nint.Zero) return;

        _ = ObjC.MsgSendNInt(pasteboard, ObjC.Sel("clearContents"));
        _ = ObjC.MsgSendBoolWithIntPtrIntPtr(pasteboard, ObjC.Sel("setString:forType:"), ObjC.NSString(text), ObjC.NSString(PasteboardTypeString));
    }
}
