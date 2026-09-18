using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Interfaces;

/// <summary>
/// A per-platform host stand-in: opens a native top-level window, embeds a plugin's editor view in
/// it and pumps messages until it closes. This is what lets the GUI be developed and iterated
/// WITHOUT a DAW - the runner plays the part a host normally would.
///
/// One implementation per windowing system (Win32, X11). Obtain the right one from
/// PlatformHarnessRunnerFactory rather than constructing one directly.
/// </summary>
public interface IHarnessRunner
{
    /// <summary>The VST3 view platform type this runner provides a parent handle for (HWND, X11 window id, ...).</summary>
    public AudioPluginViewPlatform Platform { get; }

    /// <summary>
    /// Opens the window, attaches <paramref name="plugin"/>'s editor view and runs the message loop.
    /// BLOCKS until the window closes, so it's effectively the harness's Main.
    /// </summary>
    void Run(IHarnessPlugin plugin);
}
