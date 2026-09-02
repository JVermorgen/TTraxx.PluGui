using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Interfaces;

public interface IHarnessPlugin
{
    string DisplayName { get; }

    AudioPluginViewPlatform Platform { get; }
    IAudioPluginView Create();
}