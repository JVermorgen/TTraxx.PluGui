using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Linux;

internal sealed class XlibHarnessPluginFrame(Action<int, int> resizeContent) : IAudioPluginFrame
{
    public void ResizeView(IAudioPluginView view, ViewRectangle newSize)
        => resizeContent(newSize.Right - newSize.Left, newSize.Bottom - newSize.Top);
}
