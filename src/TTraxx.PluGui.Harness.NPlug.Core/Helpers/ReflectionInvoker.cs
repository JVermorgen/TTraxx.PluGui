namespace TTraxx.PluGui.Harness.NPlug.Core.Helpers;

/// <summary>
/// Generic reflection helper: searches for a public, parameterless
/// instance method with the given name and caches it as a delegate. Shared
/// by all "optional hook on IAudioPluginView" invokers in this harness
/// (RefreshUI, RebuildControls, ...).
/// </summary>
public static class ReflectionInvoker
{
    public static Action? TryCreateDelegate(this object target, string methodName)
    {
        var method = target.GetType().GetMethod(methodName, Type.EmptyTypes);
        return method is null ? null : (Action)Delegate.CreateDelegate(typeof(Action), target, method);
    }
}