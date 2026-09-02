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
}