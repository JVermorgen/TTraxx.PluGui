namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Constants;

/// <summary>
/// Xlib bit flags used by LinuxPlatformWindow. The Mask values are OR'd together into the single
/// event mask passed to XSelectInput: X11 delivers ONLY the event classes selected there, so a
/// missing bit shows up as an input type that silently never arrives.
/// </summary>
internal static class XlibConstants
{
    /// <summary>Selects Expose events - X11's repaint request.</summary>
    internal const nint ExposureMask = 1 << 15;

    /// <summary>Selects ButtonPress. Also how scroll arrives, as buttons 4 and 5.</summary>
    internal const nint ButtonPressMask = 1 << 2;

    /// <summary>Selects ButtonRelease.</summary>
    internal const nint ButtonReleaseMask = 1 << 3;

    /// <summary>Selects MotionNotify for every pointer move, not just dragged ones.</summary>
    internal const nint PointerMotionMask = 1 << 6;

    /// <summary>Selects ConfigureNotify, which is how a resize is reported.</summary>
    internal const nint StructureNotifyMask = 1 << 17;

    /// <summary>
    /// XImage format: packed pixels, one plane, scanline-ordered - the layout matching the buffer Skia
    /// renders into, so the image can be handed to XPutImage without repacking.
    /// </summary>
    internal const int ZPixmap = 2;

    // XButtonEvent/XMotionEvent.state modifier bits.

    /// <summary>Shift held, per the event's state field.</summary>
    internal const uint ShiftMask = 1 << 0;

    /// <summary>Control held, per the event's state field.</summary>
    internal const uint ControlMask = 1 << 2;
}
