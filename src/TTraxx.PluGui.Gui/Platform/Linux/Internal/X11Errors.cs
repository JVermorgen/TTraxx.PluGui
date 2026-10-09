using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

/// <summary>
/// Traps X errors around calls that name another client's window, which may be gone by now - one
/// being dragged over, or the source of a drop. An X error goes to the process's error handler,
/// and Xlib's default one ends the process (the host with it). Between <see cref="Begin"/> and
/// <see cref="End"/> a handler that only notes the error stands in; the handler is the process's,
/// so the stretch stays short and on the UI thread.
/// </summary>
internal static unsafe class X11Errors
{
    private static bool s_errored;

    /// <summary>Puts the noting handler in; returns the one it replaced, for <see cref="End"/>.</summary>
    public static nint Begin()
    {
        s_errored = false;
        return Xlib.XSetErrorHandler((nint)(delegate* unmanaged<nint, nint, int>)&Note);
    }

    /// <summary>Waits for the server to have handled the calls, puts the old handler back, and says whether any of them failed.</summary>
    public static bool End(nint display, nint previous)
    {
        _ = Xlib.XSync(display, false);
        _ = Xlib.XSetErrorHandler(previous);
        return s_errored;
    }

    /// <summary>Whether a call since <see cref="Begin"/> has failed (so far as the server has answered).</summary>
    public static bool Errored => s_errored;

    [UnmanagedCallersOnly]
    private static int Note(nint display, nint error)
    {
        s_errored = true;
        return 0;
    }
}
