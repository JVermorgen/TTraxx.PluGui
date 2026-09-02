using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Win32;

internal sealed class Win32HarnessPluginFrame(nint hwnd) : IAudioPluginFrame
{
    public void ResizeView(IAudioPluginView view, ViewRectangle newSize)
        => Win32HarnessRunner.ResizeWindow(hwnd, newSize.Right - newSize.Left, newSize.Bottom - newSize.Top);
}