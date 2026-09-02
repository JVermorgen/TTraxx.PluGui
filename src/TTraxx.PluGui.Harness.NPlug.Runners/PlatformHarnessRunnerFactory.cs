using TTraxx.PluGui.Harness.NPlug.Core.Interfaces;
using TTraxx.PluGui.Harness.NPlug.Runners.Linux;
using TTraxx.PluGui.Harness.NPlug.Runners.Win32;

namespace TTraxx.PluGui.Harness.NPlug.Runners;

public static class PlatformHarnessRunnerFactory
{
    public static IHarnessRunner Create()
        => true switch
        {
            _ when OperatingSystem.IsWindows() => new Win32HarnessRunner(),
            _ when OperatingSystem.IsLinux() => new XlibHarnessRunner(),
            _ => throw new PlatformNotSupportedException()
        };
}
