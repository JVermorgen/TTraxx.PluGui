namespace TTraxx.PluGui.Gui.Windows.Interfaces;

/// <summary>
/// <para>
/// Implemented by platform layers that have no timer of their own to drive a
/// continuously-repainting control (e.g. a level meter) and instead must be
/// actively ticked via a handle that the caller registers in its own run loop
/// (X11 on Linux, via VST3's IRunLoop.RegisterTimer).
/// </para>
/// <para>
/// Platforms that can drive their own repaint timer (Win32) or don't support
/// one yet (macOS) simply leave IPlatformWindow.TimerPumpSource null - the
/// caller then doesn't need to handle anything special.
/// </para>
/// </summary>
public interface ITimerPumpSource
{
    /// <summary>How often, in milliseconds, the caller should call OnTimerTick().</summary>
    int PreferredIntervalMilliseconds { get; }

    /// <summary>Called by the host's own timer/run loop each tick.</summary>
    void OnTimerTick();
}
