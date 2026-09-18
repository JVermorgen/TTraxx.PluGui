using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Win32;

/// <summary>
/// The harness's stand-in for the host side of the VST3 view relationship on Windows. A plugin that
/// wants to change its own editor size can't just resize itself - it asks its frame, and the host
/// resizes the container. Without a frame the plugin has nobody to ask, so the harness supplies this
/// one and resizes its top-level window to match.
/// </summary>
internal sealed class Win32HarnessPluginFrame(nint hwnd) : IAudioPluginFrame
{
    /// <summary>
    /// Resizes the harness window to the requested view rectangle. The rectangle is in the host's
    /// coordinate space, so only its width and height are meaningful here - the position is the
    /// harness window's own business.
    /// </summary>
    public void ResizeView(IAudioPluginView view, ViewRectangle newSize)
        => Win32HarnessRunner.ResizeWindow(hwnd, newSize.Right - newSize.Left, newSize.Bottom - newSize.Top);
}
