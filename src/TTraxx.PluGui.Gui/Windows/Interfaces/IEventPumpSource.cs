namespace TTraxx.PluGui.Gui.Windows.Interfaces;

/// <summary>
/// <para>
/// Implemented by platform layers that cannot passively wait for OS messages
/// (like Win32's WndProc which automatically arrives via the host message loop)
/// but instead must be actively polled via a handle/fd that the caller registers
/// in its own run loop (X11 on Linux, via GetConnectionFd()).
/// </para>
/// <para>
/// Platforms that don't need this (Win32, macOS) simply leave
/// IPlatformWindow.EventPumpSource null - the caller then doesn't need to
/// handle anything special.
/// </para>
/// </summary>
public interface IEventPumpSource
{
    /// <summary>OS-specific handle (e.g. the X11 connection fd) that the host runloop must monitor.</summary>
    int GetPumpHandle();

    /// <summary>Called as soon as the host runloop reports activity on GetPumpHandle().</summary>
    void ProcessPendingEvents();
}