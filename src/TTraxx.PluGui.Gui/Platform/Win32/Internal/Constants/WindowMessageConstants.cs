namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

internal static class WindowMessageConstants
{
    public const int WM_MOUSEACTIVATE = 0x0021;
    public const nint MA_NOACTIVATE = 3;

    public const int WM_PAINT = 0x000F;
    public const int WM_DESTROY = 0x0002;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WHEEL_DELTA = 120;
    public const int WM_TIMER = 0x0113;
    public const int WM_ERASEBKGND = 0x0014;
    public const int WM_SIZE = 0x0005;

    // wParam key-state flags carried by WM_LBUTTONDOWN/WM_MOUSEMOVE/WM_MOUSEWHEEL.
    public const int MK_SHIFT = 0x0004;
    public const int MK_CONTROL = 0x0008;

    public const int GWLP_USERDATA = -21;

    public const int GWL_WNDPROC = -4;
}