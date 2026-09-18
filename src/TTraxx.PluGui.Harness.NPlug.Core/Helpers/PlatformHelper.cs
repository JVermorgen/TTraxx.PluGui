using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Helpers;

/// <summary>
/// Maps the running OS onto the VST3 view platform tag a host would pass when attaching an editor,
/// so a harness plugin can report the right one without an OS check of its own.
/// </summary>
public static class PlatformHelper
{
    /// <summary>The VST3 view platform type for the current OS.</summary>
    /// <exception cref="PlatformNotSupportedException">Only Windows and Linux are supported so far.</exception>
    public static AudioPluginViewPlatform GetCurrentAudioPluginViewPlatform()
        => true switch
        {
            _ when OperatingSystem.IsWindows() => AudioPluginViewPlatform.Hwnd,
            _ when OperatingSystem.IsLinux() => AudioPluginViewPlatform.X11EmbedWindowID,
            _ => throw new PlatformNotSupportedException("Enkel Windows en Linux worden momenteel ondersteund.")
        };
}
