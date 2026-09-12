using TTraxx.PluGui.Gui.Input;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Helpers;

internal static class User32Helpers
{
    public static int GetX(nint lParam) => (short)(lParam.ToInt64() & 0xFFFF);
    public static int GetY(nint lParam) => (short)((lParam.ToInt64() >> 16) & 0xFFFF);

    public static int GetWidth(nint lParam) => (int)(lParam.ToInt64() & 0xFFFF);
    public static int GetHeight(nint lParam) => (int)((lParam.ToInt64() >> 16) & 0xFFFF);

    public static short GetWheel(nint wParam) => (short)((wParam.ToInt64() >> 16) & 0xFFFF);

    /// <summary>Low word of wParam on WM_LBUTTONDOWN/WM_MOUSEMOVE/WM_MOUSEWHEEL carries MK_* key-state flags.</summary>
    public static KeyModifiers GetKeyModifiers(nint wParam)
    {
        var flags = (int)(wParam.ToInt64() & 0xFFFF);
        var modifiers = KeyModifiers.None;
        if ((flags & WindowMessageConstants.MK_SHIFT) != 0) modifiers |= KeyModifiers.Shift;
        if ((flags & WindowMessageConstants.MK_CONTROL) != 0) modifiers |= KeyModifiers.Control;
        return modifiers;
    }
}