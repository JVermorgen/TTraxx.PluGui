using NPlug;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Harness.NPlug.Core.Helpers;
using TTraxx.PluGui.Harness.NPlug.Core.Interfaces;
using TTraxx.PluGui.NPlug.Interfaces;

namespace TTraxx.PluGui.Harness.NPlug.Runners.Linux;

public sealed partial class XlibHarnessRunner : IHarnessRunner
{
    private const long StructureNotifyMask = 1L << 17;
    private const int DestroyNotify = 17;

    // EWMH "always on top" hint: no native Xlib window style for this - the
    // window manager honors it via a _NET_WM_STATE ClientMessage sent to the
    // root window after mapping (the standard wmctrl-style mechanism).
    private const int ClientMessage = 33;
    private const long SubstructureRedirectMask = 1L << 20;
    private const long SubstructureNotifyMask = 1L << 19;
    private const long NetWmStateAdd = 1;
    private const int ClientMessageBufferSize = 96;

    public AudioPluginViewPlatform Platform => AudioPluginViewPlatform.X11EmbedWindowID;

    public void Run(IHarnessPlugin plugin)
    {
        var display = XOpenDisplay(nint.Zero);
        if (display == nint.Zero) throw new InvalidOperationException("Kon geen X11-connectie openen.");

        Globals.ScaleFactor = 1.0f;

        var view = plugin.Create();
        var initialSize = view.Size;

        var screen = XDefaultScreen(display);
        var root = XRootWindow(display, screen);
        var window = XCreateSimpleWindow(display, root, 100, 100,
            (uint)(initialSize.Right - initialSize.Left), (uint)(initialSize.Bottom - initialSize.Top), 1, 0, 0);
        _ = XStoreName(display, window, plugin.DisplayName);
        _ = XSelectInput(display, window, StructureNotifyMask);
        _ = XMapWindow(display, window);
        _ = XFlush(display);

        if (plugin.AlwaysOnTop) RequestAlwaysOnTop(display, window, root);

        view.SetFrame(new XlibHarnessPluginFrame(display, window));
        view.Attached(window, plugin.Platform);

        var typedView = view as IPluGuiPluginView;

        Action? refreshAction = null;
        Action? rebuildControlsAction = null;
        Action? eventPumpAction = null;
        if (typedView is null)
        {
            refreshAction = view.TryCreateDelegate("RefreshUI");
            rebuildControlsAction = view.TryCreateDelegate("RebuildControls");
            eventPumpAction = view.TryCreateEventPumpDelegate();
        }
        var lastRefresh = Environment.TickCount64;

        var eventBuffer = Marshal.AllocHGlobal(256);
        try
        {
            var running = true;
            while (running)
            {
                while (XPending(display) > 0)
                {
                    _ = XNextEvent(display, eventBuffer);
                    if (Marshal.ReadInt32(eventBuffer) == DestroyNotify) running = false;
                }

                if (typedView is null)
                {
                    eventPumpAction?.Invoke();
                    if ((rebuildControlsAction != null || refreshAction != null) && Environment.TickCount64 - lastRefresh >= 250)
                    {
                        rebuildControlsAction?.Invoke();
                        if (rebuildControlsAction is null) refreshAction?.Invoke();
                        lastRefresh = Environment.TickCount64;
                    }
                }
                else
                {
                    typedView.EventPumpSource?.ProcessPendingEvents();
                    typedView.RebuildControls();
                }

                Thread.Sleep(8);
            }
        }
        finally { Marshal.FreeHGlobal(eventBuffer); }

        view.Removed();
    }

    [LibraryImport("libX11")] private static partial nint XOpenDisplay(nint display);
    [LibraryImport("libX11")] private static partial int XDefaultScreen(nint display);
    [LibraryImport("libX11")] private static partial nint XRootWindow(nint display, int screenNumber);
    [LibraryImport("libX11")] private static partial nint XCreateSimpleWindow(nint display, nint parent, int x, int y, uint width, uint height, uint borderWidth, nint border, nint background);
    [LibraryImport("libX11", StringMarshalling = StringMarshalling.Utf8)] private static partial int XStoreName(nint display, nint window, string windowName);
    [LibraryImport("libX11")] private static partial int XSelectInput(nint display, nint window, long eventMask);
    [LibraryImport("libX11")] private static partial int XMapWindow(nint display, nint window);
    [LibraryImport("libX11")] private static partial int XFlush(nint display);
    [LibraryImport("libX11")] private static partial int XPending(nint display);
    [LibraryImport("libX11")] private static partial int XNextEvent(nint display, nint eventReturn);
    [LibraryImport("libX11")] internal static partial int XResizeWindow(nint display, nint window, uint width, uint height);

    [LibraryImport("libX11", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint XInternAtom(nint display, string atom_name, [MarshalAs(UnmanagedType.Bool)] bool only_if_exists);

    [LibraryImport("libX11")]
    private static partial int XSendEvent(nint display, nint window, [MarshalAs(UnmanagedType.Bool)] bool propagate, long event_mask, nint event_send);

    /// <summary>Asks the window manager to keep this window above others, via the standard EWMH _NET_WM_STATE/_NET_WM_STATE_ABOVE ClientMessage (same mechanism tools like wmctrl use).</summary>
    private static void RequestAlwaysOnTop(nint display, nint window, nint root)
    {
        var wmState = XInternAtom(display, "_NET_WM_STATE", false);
        var wmStateAbove = XInternAtom(display, "_NET_WM_STATE_ABOVE", false);
        if (wmState == nint.Zero || wmStateAbove == nint.Zero) return;

        var eventBuffer = Marshal.AllocHGlobal(ClientMessageBufferSize);
        try
        {
            Marshal.Copy(new byte[ClientMessageBufferSize], 0, eventBuffer, ClientMessageBufferSize);

            Marshal.WriteInt32(eventBuffer, 0, ClientMessage);
            Marshal.WriteIntPtr(eventBuffer, 32, window);
            Marshal.WriteIntPtr(eventBuffer, 40, wmState);
            Marshal.WriteInt32(eventBuffer, 48, 32); // format: data is 32-bit values
            Marshal.WriteInt64(eventBuffer, 56, NetWmStateAdd); // data.l[0]
            Marshal.WriteIntPtr(eventBuffer, 64, wmStateAbove);  // data.l[1]
            Marshal.WriteInt64(eventBuffer, 80, 1);              // data.l[3]: source indication = normal application

            _ = XSendEvent(display, root, false, SubstructureRedirectMask | SubstructureNotifyMask, eventBuffer);
            _ = XFlush(display);
        }
        finally
        {
            Marshal.FreeHGlobal(eventBuffer);
        }
    }
}