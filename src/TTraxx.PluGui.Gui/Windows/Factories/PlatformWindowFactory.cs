namespace TTraxx.PluGui.Gui.Windows.Factories;

/// <summary>
/// Picks the <see cref="IPlatformWindow"/> implementation for the running OS. This is the single
/// place in the library that names the per-platform window types, which is what keeps
/// <see cref="PluginWindow"/> free of any Win32/Cocoa/X11 knowledge.
/// </summary>
internal static class PlatformWindowFactory
{
    /// <summary>Creates a native window backend for the current OS.</summary>
    /// <exception cref="PlatformNotSupportedException">Running on an OS with no backend.</exception>
    public static IPlatformWindow Create()
    {
        if (OperatingSystem.IsWindows()) return new Platform.Win32.Win32PlatformWindow();
        if (OperatingSystem.IsMacOS()) return new Platform.MacOS.MacOsPlatformWindow();
        if (OperatingSystem.IsLinux()) return new Platform.Linux.LinuxPlatformWindow();
        throw new PlatformNotSupportedException();
    }
}
