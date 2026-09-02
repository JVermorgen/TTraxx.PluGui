using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Helpers;

public static class PlatformHelper
{
    public static AudioPluginViewPlatform GetCurrentAudioPluginViewPlatform()
        => true switch
        {
            _ when OperatingSystem.IsWindows() => AudioPluginViewPlatform.Hwnd,
            _ when OperatingSystem.IsLinux() => AudioPluginViewPlatform.X11EmbedWindowID,
            _ => throw new PlatformNotSupportedException("Enkel Windows en Linux worden momenteel ondersteund.")
        };
}
