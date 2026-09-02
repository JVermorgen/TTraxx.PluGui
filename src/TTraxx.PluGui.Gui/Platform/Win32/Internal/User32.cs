using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;
using static TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants.WindowMessageConstants;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

internal static partial class User32
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern nint CreateWindowEx(int dwExStyle, string lpClassName, string? lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ScreenToClient(nint hWnd, ref Point lpPoint);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static partial nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hWnd, int nIndex);

    internal static nint SetWindowUserData(nint hWnd, nint value) => SetWindowLongPtr(hWnd, GWLP_USERDATA, value);

    internal static nint GetWindowUserData(nint hWnd) => GetWindowLongPtr(hWnd, GWLP_USERDATA);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint Msg, nint wParam, nint lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetClientRect(nint hWnd, out Rect lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool InvalidateRect(nint hWnd, nint lpRect, [MarshalAs(UnmanagedType.Bool)] bool bErase);

    [DllImport("user32.dll")]
    internal static extern nint BeginPaint(nint hWnd, out PaintStruct lpPaint);

    [DllImport("user32.dll")]
    internal static extern bool EndPaint(nint hWnd, ref PaintStruct lpPaint);

    [LibraryImport("user32.dll")]
    internal static partial nint SetCapture(nint hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ReleaseCapture();

    [LibraryImport("user32.dll")]
    private static partial int GetDpiForWindow(nint hWnd);

    [LibraryImport("user32.dll")]
    private static partial int GetDpiForSystem(); // Win10+

    internal static int SafeGetDpiForWindow(nint hWnd)
    {
        try
        {
            int dpi = GetDpiForWindow(hWnd);
            return dpi > 0 ? dpi : 96;
        }
        catch { return 96; }
    }

    internal static int SafeGetDpiForSystem()
    {
        try
        {
            int dpi = GetDpiForSystem();
            return dpi > 0 ? dpi : 96;
        }
        catch { return 96; }
    }
}