using TTraxx.PluGui.Harness.NPlug.Core.Interfaces;
using TTraxx.PluGui.Harness.NPlug.Runners.Linux;
using TTraxx.PluGui.Harness.NPlug.Runners.Win32;

namespace TTraxx.PluGui.Harness.NPlug.Runners;

/// <summary>
/// Picks the <see cref="IHarnessRunner"/> for the running OS. Exists so a harness entry point can
/// be a single cross-platform <c>Create().Run(plugin)</c> without referencing the per-platform
/// runner types itself.
/// </summary>
public static class PlatformHarnessRunnerFactory
{
    /// <summary>Creates the harness runner for the current OS.</summary>
    /// <exception cref="PlatformNotSupportedException">No runner exists for this OS - macOS is not implemented yet.</exception>
    public static IHarnessRunner Create()
        => true switch
        {
            _ when OperatingSystem.IsWindows() => new Win32HarnessRunner(),
            _ when OperatingSystem.IsLinux() => new XlibHarnessRunner(),
            _ => throw new PlatformNotSupportedException()
        };
}
