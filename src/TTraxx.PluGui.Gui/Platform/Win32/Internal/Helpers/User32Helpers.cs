namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Helpers;

internal static class User32Helpers
{
    public static int GetX(nint lParam) => (short)(lParam.ToInt64() & 0xFFFF);
    public static int GetY(nint lParam) => (short)((lParam.ToInt64() >> 16) & 0xFFFF);

    public static int GetWidth(nint lParam) => (int)(lParam.ToInt64() & 0xFFFF);
    public static int GetHeight(nint lParam) => (int)((lParam.ToInt64() >> 16) & 0xFFFF);

    public static short GetWheel(nint wParam) => (short)((wParam.ToInt64() >> 16) & 0xFFFF);
}