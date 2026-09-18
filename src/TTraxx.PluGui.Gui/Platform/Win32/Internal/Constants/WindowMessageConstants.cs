namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

/// <summary>
/// The subset of winuser.h values Win32PlatformWindow's WndProc needs. Hand-declared rather than
/// taken from a dependency: only a dozen of the hundreds of WM_* messages are ever handled, and
/// each is a documented, permanently fixed value.
///
/// Despite the name this also holds the two GWL* indices, which aren't messages but window-long
/// offsets for SetWindowLongPtr - they're here because they're used in the same place, when
/// installing the WndProc.
/// </summary>
internal static class WindowMessageConstants
{
    /// <summary>Sent when a click would activate the window. Answered with <see cref="MA_NOACTIVATE"/>.</summary>
    public const int WM_MOUSEACTIVATE = 0x0021;

    /// <summary>
    /// WM_MOUSEACTIVATE reply meaning "handle the click but do not activate". Keeps a click on a
    /// control from stealing focus away from the host window.
    /// </summary>
    public const nint MA_NOACTIVATE = 3;

    /// <summary>Repaint request - where the Skia surface is blitted to the window.</summary>
    public const int WM_PAINT = 0x000F;

    /// <summary>Window is being destroyed.</summary>
    public const int WM_DESTROY = 0x0002;

    /// <summary>Left button pressed.</summary>
    public const int WM_LBUTTONDOWN = 0x0201;

    /// <summary>Left button released.</summary>
    public const int WM_LBUTTONUP = 0x0202;

    /// <summary>Pointer moved.</summary>
    public const int WM_MOUSEMOVE = 0x0200;

    /// <summary>Wheel turned. The notch count is the high word of wParam, in units of <see cref="WHEEL_DELTA"/>.</summary>
    public const int WM_MOUSEWHEEL = 0x020A;

    /// <summary>Left button double-clicked. Only delivered because the window is created with SS_NOTIFY.</summary>
    public const int WM_LBUTTONDBLCLK = 0x0203;

    /// <summary>Right button pressed - raised as a context-menu request.</summary>
    public const int WM_RBUTTONDOWN = 0x0204;

    /// <summary>
    /// Wheel units in one notch. WM_MOUSEWHEEL reports accumulated distance, not notches, so the
    /// platform layer divides by this to get the normalized count a control receives.
    /// </summary>
    public const int WHEEL_DELTA = 120;

    /// <summary>Timer tick - drives the continuous-repaint clock set up by SetTimer.</summary>
    public const int WM_TIMER = 0x0113;

    /// <summary>
    /// Background-erase request. Answered as handled without drawing, since WM_PAINT paints every
    /// pixel - letting Windows erase first would flicker.
    /// </summary>
    public const int WM_ERASEBKGND = 0x0014;

    /// <summary>Window resized. New client width/height arrive packed in lParam.</summary>
    public const int WM_SIZE = 0x0005;

    // wParam key-state flags carried by WM_LBUTTONDOWN/WM_MOUSEMOVE/WM_MOUSEWHEEL.

    /// <summary>Shift held, per the MK_* flags in the low word of wParam.</summary>
    public const int MK_SHIFT = 0x0004;

    /// <summary>Control held, per the MK_* flags in the low word of wParam.</summary>
    public const int MK_CONTROL = 0x0008;

    /// <summary>
    /// SetWindowLongPtr index for the window's user data slot, where the owning managed instance's
    /// GCHandle is parked so the static WndProc can find it again.
    /// </summary>
    public const int GWLP_USERDATA = -21;

    /// <summary>SetWindowLongPtr index for the window procedure - writing it is what subclasses the window.</summary>
    public const int GWL_WNDPROC = -4;
}
