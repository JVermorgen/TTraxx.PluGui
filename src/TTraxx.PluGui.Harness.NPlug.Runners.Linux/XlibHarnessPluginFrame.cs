using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Linux;

/// <summary>
/// The harness's stand-in for the host side of the VST3 view relationship on X11 - the Xlib
/// counterpart of Win32HarnessPluginFrame, letting a plugin that wants to resize its own editor
/// have something to ask.
///
/// Takes a callback rather than a window handle because resizing an X11 window means talking to the
/// display connection the runner owns, so the runner keeps that logic and passes in just the part
/// this frame needs.
/// </summary>
internal sealed class XlibHarnessPluginFrame(Action<int, int> resizeContent) : IAudioPluginFrame
{
    /// <summary>
    /// Asks the runner to resize its window to the requested view rectangle. Only the rectangle's
    /// width and height are meaningful here; the position belongs to the runner.
    /// </summary>
    public void ResizeView(IAudioPluginView view, ViewRectangle newSize)
        => resizeContent(newSize.Right - newSize.Left, newSize.Bottom - newSize.Top);
}
