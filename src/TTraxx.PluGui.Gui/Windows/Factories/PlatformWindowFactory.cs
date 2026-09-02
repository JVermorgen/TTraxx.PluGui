using TTraxx.PluGui.Gui.Windows.Interfaces;

namespace TTraxx.PluGui.Gui.Windows.Factories;

internal static class PlatformWindowFactory
{
    public static IPlatformWindow Create()
    {
        if (OperatingSystem.IsWindows()) return new Platform.Win32.Win32PlatformWindow();
        if (OperatingSystem.IsMacOS()) return new Platform.MacOS.MacOsPlatformWindow();
        if (OperatingSystem.IsLinux()) return new Platform.Linux.LinuxPlatformWindow();
        throw new PlatformNotSupportedException();
    }
}