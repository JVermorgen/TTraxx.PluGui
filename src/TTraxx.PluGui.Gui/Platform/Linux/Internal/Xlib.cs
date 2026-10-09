using System.Globalization;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

internal static partial class Xlib
{
    [LibraryImport("libX11.so.6")] internal static partial nint XOpenDisplay(nint displayName);
    [LibraryImport("libX11.so.6")] internal static partial int XConnectionNumber(nint display);
    [LibraryImport("libX11.so.6")] internal static partial int XDefaultScreen(nint display);
    [LibraryImport("libX11.so.6")] internal static partial nint XRootWindow(nint display, int screenNumber);

    [LibraryImport("libX11.so.6")]
    internal static partial nint XCreateSimpleWindow(nint display, nint parent, int x, int y, uint width, uint height,
        uint borderWidth, nuint border, nuint background);

    [LibraryImport("libX11.so.6")] internal static partial int XMapWindow(nint display, nint window);
    [LibraryImport("libX11.so.6")] internal static partial int XDestroyWindow(nint display, nint window);
    [LibraryImport("libX11.so.6")] internal static partial int XResizeWindow(nint display, nint window, uint width, uint height);
    [LibraryImport("libX11.so.6")] internal static partial int XMoveResizeWindow(nint display, nint window, int x, int y, uint width, uint height);
    [LibraryImport("libX11.so.6")] internal static partial int XSelectInput(nint display, nint window, nint eventMask);
    [LibraryImport("libX11.so.6")] internal static partial int XFlush(nint display);
    [LibraryImport("libX11.so.6")] internal static partial int XPending(nint display);
    [LibraryImport("libX11.so.6")] internal static partial int XNextEvent(nint display, nint eventReturn);

    [LibraryImport("libX11.so.6")] internal static partial nint XCreateGC(nint display, nint window, nuint valueMask, nint values);
    [LibraryImport("libX11.so.6")] internal static partial int XFreeGC(nint display, nint gc);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial int XPutImage(nint display, nint window, nint gc, nint image,
        int srcX, int srcY, int destX, int destY, uint width, uint height);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial nint XCreateImage(nint display, nint visual, uint depth, int format, int offset,
        byte* data, uint width, uint height, int bitmapPad, int bytesPerLine);

    [LibraryImport("libX11.so.6")] internal static partial nint XDefaultVisual(nint display, int screenNumber);
    [LibraryImport("libX11.so.6")] internal static partial int XDefaultDepth(nint display, int screenNumber);

    // ---- Keyboard: focus, and turning a key press into a key and its text ----

    [LibraryImport("libX11.so.6")] internal static partial int XGetInputFocus(nint display, out nint focus, out int revertTo);
    [LibraryImport("libX11.so.6")] internal static partial int XSetInputFocus(nint display, nint focus, int revertTo, nint time);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial int XQueryTree(nint display, nint window, out nint root, out nint parent, out nint* children, out uint childCount);

    [LibraryImport("libX11.so.6")] internal static unsafe partial int XLookupString(XKeyEvent* keyEvent, byte* buffer, int bytes, out nuint keysym, nint composeStatus);
    [LibraryImport("libX11.so.6")] internal static unsafe partial nuint XLookupKeysym(XKeyEvent* keyEvent, int index);

    // ---- Selections (the clipboard) ----

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nuint XInternAtom(nint display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [LibraryImport("libX11.so.6")] internal static partial nint XGetSelectionOwner(nint display, nuint selection);
    [LibraryImport("libX11.so.6")] internal static partial int XSetSelectionOwner(nint display, nuint selection, nint owner, nint time);
    [LibraryImport("libX11.so.6")] internal static partial int XConvertSelection(nint display, nuint selection, nuint target, nuint property, nint requestor, nint time);
    [LibraryImport("libX11.so.6")] internal static unsafe partial int XCheckTypedWindowEvent(nint display, nint window, int eventType, XEvent* eventReturn);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial int XGetWindowProperty(nint display, nint window, nuint property, nint longOffset, nint longLength,
        [MarshalAs(UnmanagedType.Bool)] bool delete, nuint requestedType, out nuint actualType, out int actualFormat,
        out nuint itemCount, out nuint bytesAfter, out byte* data);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial int XChangeProperty(nint display, nint window, nuint property, nuint type, int format, int mode,
        void* data, int elementCount);

    [LibraryImport("libX11.so.6")]
    internal static unsafe partial int XSendEvent(nint display, nint window, [MarshalAs(UnmanagedType.Bool)] bool propagate, nint eventMask, void* eventSend);

    [LibraryImport("libX11.so.6")] internal static unsafe partial int XFree(void* data);

    // ---- Dragging out (XDND): the pointer, the windows under it, and trapping errors on windows that vanish ----

    [LibraryImport("libX11.so.6")]
    internal static partial int XGrabPointer(nint display, nint grabWindow, [MarshalAs(UnmanagedType.Bool)] bool ownerEvents, uint eventMask,
        int pointerMode, int keyboardMode, nint confineTo, nint cursor, nint time);

    [LibraryImport("libX11.so.6")] internal static partial int XUngrabPointer(nint display, nint time);
    [LibraryImport("libX11.so.6")] internal static partial int XChangeActivePointerGrab(nint display, uint eventMask, nint cursor, nint time);
    [LibraryImport("libX11.so.6")] internal static partial nint XCreateFontCursor(nint display, uint shape);
    [LibraryImport("libX11.so.6")] internal static partial int XFreeCursor(nint display, nint cursor);

    [LibraryImport("libX11.so.6")]
    internal static partial int XTranslateCoordinates(nint display, nint sourceWindow, nint destinationWindow, int sourceX, int sourceY,
        out int destinationX, out int destinationY, out nint child);

    [LibraryImport("libX11.so.6")] internal static partial int XSync(nint display, [MarshalAs(UnmanagedType.Bool)] bool discard);
    // The handler is a C function pointer (int (*)(Display*, XErrorEvent*)); the previous one comes back to be restored.
    [LibraryImport("libX11.so.6")] internal static partial nint XSetErrorHandler(nint handler);

    [LibraryImport("libX11.so.6")] internal static partial nint XResourceManagerString(nint display);
    [LibraryImport("libX11.so.6")] internal static partial int XDisplayWidth(nint display, int screenNumber);
    [LibraryImport("libX11.so.6")] internal static partial int XDisplayWidthMM(nint display, int screenNumber);
    [LibraryImport("libX11.so.6")] internal static partial int XCloseDisplay(nint display);

    /// <summary>
    /// Determines the initial host/DPI scale.
    /// </summary>
    internal static float DetermineInitialScale(nint display)
    {
        var ownsDisplay = display == nint.Zero;
        if (ownsDisplay)
        {
            display = XOpenDisplay(nint.Zero);
            if (display == nint.Zero) return 1.0f;
        }

        try
        {
            var dpi = TryReadXftDpi(display) ?? ComputePhysicalDpi(display);
            return Math.Clamp(dpi / 96.0f, 0.5f, 4.0f); // 96.0f: X11-convention for scale 1.0 (same reference as GTK/Qt)
        }
        finally
        {
            if (ownsDisplay) _ = XCloseDisplay(display);
        }
    }

    private static float? TryReadXftDpi(nint display)
    {
        var resourcesPtr = XResourceManagerString(display);
        if (resourcesPtr == nint.Zero) return null;

        // Owned by Xlib itself — Marshal.PtrToStringAnsi copies the
        // contents, we don't/shouldn't free the pointer.
        var resources = Marshal.PtrToStringAnsi(resourcesPtr);
        if (string.IsNullOrEmpty(resources)) return null;

        // Format is lines like "Xft.dpi:\t96" (RESOURCE_MANAGER syntax).
        foreach (var rawLine in resources.Split('\n'))
        {
            var line = rawLine.Trim();
            if (!line.StartsWith("Xft.dpi:", StringComparison.Ordinal)) continue;

            var value = line["Xft.dpi:".Length..].Trim();
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dpi) && dpi > 0)
                return dpi;

            break; // line found but unparseable — no point searching further
        }

        return null;
    }

    private static float ComputePhysicalDpi(nint display)
    {
        var screen = XDefaultScreen(display);
        var widthPx = XDisplayWidth(display, screen);
        var widthMm = XDisplayWidthMM(display, screen);

        // widthMm can be 0 on some virtual/headless X servers (Xvfb,
        // VNC, ...) — fall back to the default DPI instead of dividing by 0.
        if (widthPx <= 0 || widthMm <= 0) return 96.0f;

        return widthPx * 25.4f / widthMm;
    }
}
