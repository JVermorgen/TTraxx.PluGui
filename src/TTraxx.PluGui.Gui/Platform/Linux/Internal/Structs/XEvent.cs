using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>
/// Xlib's XEvent union, declared only far enough to read <see cref="type"/> and dispatch on it.
///
/// Xlib defines XEvent as a union of every event struct, so the real thing is as large as its biggest
/// member. Rather than model that union, this reserves a padding block big enough to receive any of
/// them and then re-reads the same memory as the specific struct the type calls for (XButtonEvent,
/// XMotionEvent, XExposeEvent, XConfigureEvent). The padding must therefore stay at least as large as
/// the native union, or XNextEvent writes past the end of it - which is why it is deliberately
/// generous rather than exact.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct XEvent
{
    /// <summary>Event type tag - the only field read through this struct. See XEventTypeConstants.</summary>
    public int type;

    private unsafe fixed byte pad[188]; // rough enough for type-dispatch; specific event structs are overlaid as needed
}
