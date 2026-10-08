using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

/// <summary>
/// The keyboard and the clipboard for Win32PlatformWindow: taking the focus while a control edits
/// text (and giving it back to whatever had it), the window messages keys arrive as, and the
/// clipboard's Unicode text.
/// </summary>
internal static partial class Keyboard
{
    public const int WM_SETFOCUS = 0x0007;
    public const int WM_KILLFOCUS = 0x0008;
    public const int WM_KEYDOWN = 0x0100;
    public const int WM_CHAR = 0x0102;

    /// <summary>Asked by dialog-style hosts which keys the window wants; answered "all of them, and the characters".</summary>
    public const int WM_GETDLGCODE = 0x0087;
    public const nint DLGC_WANTALLKEYS = 0x0004;
    public const nint DLGC_WANTCHARS = 0x0080;

    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [LibraryImport("user32.dll")]
    internal static partial nint SetFocus(nint hWnd);

    [LibraryImport("user32.dll")]
    internal static partial nint GetFocus();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    internal static partial nint GetParent(nint hWnd);

    [LibraryImport("user32.dll")]
    private static partial short GetKeyState(int nVirtKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenClipboard(nint hWndNewOwner);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseClipboard();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyClipboard();

    [LibraryImport("user32.dll")]
    private static partial nint GetClipboardData(uint uFormat);

    [LibraryImport("user32.dll")]
    private static partial nint SetClipboardData(uint uFormat, nint hMem);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalLock(nint hMem);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalUnlock(nint hMem);

    [LibraryImport("kernel32.dll")]
    private static partial nint GlobalFree(nint hMem);

    [LibraryImport("user32.dll")]
    private static partial short VkKeyScanW(ushort ch); // a WCHAR, passed as its code: char isn't blittable to LibraryImport

    [LibraryImport("user32.dll")]
    private static partial uint MapVirtualKeyW(uint uCode, uint uMapType);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool GetKeyboardState(byte* lpKeyState);

    [LibraryImport("user32.dll")]
    private static unsafe partial int ToUnicode(uint wVirtKey, uint wScanCode, byte* lpKeyState, char* pwszBuff, int cchBuff, uint wFlags);

    // ToUnicode: leave the keyboard's state (a pending dead key) as it is (Windows 10 1607 on).
    private const uint DontChangeKeyboardState = 1 << 2;

    /// <summary>
    /// The text <paramref name="character"/>'s key makes with the keyboard as it is now: the key is
    /// found from the character in the current layout (VkKeyScan), then turned back into text with
    /// the live state of Shift, Caps Lock and AltGr (ToUnicode). Null when the layout has no such key.
    /// </summary>
    public static unsafe string? Translate(char character)
    {
        var scan = VkKeyScanW((ushort)character);
        if (scan == -1) return null;

        var virtualKey = (uint)(scan & 0xFF);
        var state = stackalloc byte[256];
        if (!GetKeyboardState(state)) return null;

        // Ctrl doesn't make text; a host passing on a key with it held isn't typing.
        state[VK_CONTROL] = 0;
        var buffer = stackalloc char[8];
        var count = ToUnicode(virtualKey, MapVirtualKeyW(virtualKey, 0), state, buffer, 8, DontChangeKeyboardState);
        return count > 0 ? new string(buffer, 0, count) : null;
    }

    /// <summary>The key a WM_KEYDOWN's virtual-key code stands for.</summary>
    public static Key KeyOf(nint virtualKey) => (int)virtualKey switch
    {
        0x08 => Key.Backspace,
        0x09 => Key.Tab,
        0x0D => Key.Enter,
        0x1B => Key.Escape,
        0x23 => Key.End,
        0x24 => Key.Home,
        0x25 => Key.Left,
        0x26 => Key.Up,
        0x27 => Key.Right,
        0x28 => Key.Down,
        0x2E => Key.Delete,
        0x41 => Key.A,
        0x43 => Key.C,
        0x56 => Key.V,
        0x58 => Key.X,
        _ => Key.None,
    };

    /// <summary>Shift and Control as they are now - a key message doesn't carry them, unlike a mouse message.</summary>
    public static KeyModifiers CurrentModifiers()
    {
        var modifiers = KeyModifiers.None;
        if (GetKeyState(VK_SHIFT) < 0) modifiers |= KeyModifiers.Shift;
        if (GetKeyState(VK_CONTROL) < 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }

    public static unsafe string? GetClipboardText(nint owner)
    {
        if (!OpenClipboard(owner)) return null;
        try
        {
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == nint.Zero) return null;

            var text = GlobalLock(handle);
            if (text == nint.Zero) return null;
            try
            {
                return new string((char*)text);
            }
            finally
            {
                GlobalUnlock(handle);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    public static unsafe void SetClipboardText(nint owner, string text)
    {
        if (!OpenClipboard(owner)) return;
        try
        {
            EmptyClipboard();
            var bytes = (nuint)((text.Length + 1) * sizeof(char));
            var handle = GlobalAlloc(GMEM_MOVEABLE, bytes);
            if (handle == nint.Zero) return;

            var target = GlobalLock(handle);
            if (target == nint.Zero)
            {
                GlobalFree(handle);
                return;
            }

            fixed (char* source = text)
            {
                Buffer.MemoryCopy(source, (void*)target, (long)bytes, text.Length * sizeof(char));
            }

            ((char*)target)[text.Length] = '\0';
            GlobalUnlock(handle);

            // The clipboard owns the memory once it's taken; until then it's ours to free.
            if (SetClipboardData(CF_UNICODETEXT, handle) == nint.Zero) GlobalFree(handle);
        }
        finally
        {
            CloseClipboard();
        }
    }
}
