namespace TTraxx.PluGui.Gui;

/// <summary>Implemented per platform (Win32PlatformWindow, MacOsPlatformWindow, LinuxPlatformWindow). Manages the native window + the shared Skia surface, and routes all events to the host.</summary>
internal interface IPlatformWindow
{
    bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host);
    void SetBounds(int x, int y, int width, int height);
    float GetInitialScaleFactor(nint parentHandle);
    void Invalidate();
    void Destroy();

    /// <summary>
    /// Starts (true) or stops (false) a self-owned periodic repaint, for
    /// controls that animate on their own (e.g. a level meter) and can't
    /// rely on Invalidate() being called by anything else. Idempotent.
    /// Platforms without a way to drive this themselves (see
    /// LinuxPlatformWindow) may no-op.
    /// </summary>
    void SetContinuousRepaint(bool enabled);

    /// <summary>
    /// Non-null when this platform requires an external event pump
    /// (see <see cref="IEventPumpSource"/>). Null on platforms that
    /// already have their own message loop (Win32, macOS).
    /// </summary>
    IEventPumpSource? EventPumpSource { get; }

    /// <summary>
    /// Non-null only while this platform has no timer of its own (currently
    /// only X11/Linux) AND SetContinuousRepaint(true) is currently in effect -
    /// see <see cref="ITimerPumpSource"/>. Null on platforms with a self-driven
    /// timer (Win32), platforms without one implemented yet (macOS), and
    /// whenever no continuously-repainting control is present.
    /// </summary>
    ITimerPumpSource? TimerPumpSource { get; }
}
