using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Interfaces;

public interface IHarnessRunner
{
    public AudioPluginViewPlatform Platform { get; }
    void Run(IHarnessPlugin plugin);
}