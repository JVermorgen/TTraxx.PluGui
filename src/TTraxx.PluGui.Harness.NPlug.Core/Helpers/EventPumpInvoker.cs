using NPlug;
using System.Reflection;

namespace TTraxx.PluGui.Harness.NPlug.Core.Helpers;

/// <summary>
/// Searches via reflection for an optional "EventPumpSource" property on
/// an IAudioPluginView implementation (as a bridge to the underlying platform window).
/// If the property doesn't exist, or returns null (such as on Windows - no external pump needed), then there is
/// simply no event pump to control; nothing is required.
/// </summary>
public static class EventPumpInvoker
{
    public static Action? TryCreateEventPumpDelegate(this IAudioPluginView view)
    {
        PropertyInfo? property = view.GetType().GetProperty("EventPumpSource");
        object? pumpSource = property?.GetValue(view);
        if (pumpSource is null) return null;

        MethodInfo? method = pumpSource.GetType().GetMethod("ProcessPendingEvents", Type.EmptyTypes);
        return method is null ? null : (Action)Delegate.CreateDelegate(typeof(Action), pumpSource, method);
    }
}