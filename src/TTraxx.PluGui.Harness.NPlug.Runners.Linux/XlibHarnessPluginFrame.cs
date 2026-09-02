using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Linux;

internal sealed class XlibHarnessPluginFrame(nint display, nint window) : IAudioPluginFrame
{
    public void ResizeView(IAudioPluginView view, ViewRectangle newSize)
        => _ = XlibHarnessRunner.XResizeWindow(display, window, (uint)(newSize.Right - newSize.Left), (uint)(newSize.Bottom - newSize.Top));
}