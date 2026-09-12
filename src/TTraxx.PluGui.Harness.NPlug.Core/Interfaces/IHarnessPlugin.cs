using NPlug;

namespace TTraxx.PluGui.Harness.NPlug.Core.Interfaces;

public interface IHarnessPlugin
{
    string DisplayName { get; }

    /// <summary>When true, the harness window is pinned above other windows. Off by default.</summary>
    bool AlwaysOnTop => false;

    AudioPluginViewPlatform Platform { get; }
    IAudioPluginView Create();
}