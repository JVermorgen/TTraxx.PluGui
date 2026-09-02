using System.Globalization;
using System.Runtime.InteropServices;

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
