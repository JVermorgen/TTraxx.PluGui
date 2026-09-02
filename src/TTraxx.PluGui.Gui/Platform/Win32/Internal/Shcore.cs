using System.Runtime.InteropServices;
using static TTraxx.PluGui.Gui.Platform.Win32.Internal.Kernel32;
using static TTraxx.PluGui.Gui.Platform.Win32.Internal.User32;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

internal static partial class Shcore
{
    [LibraryImport("shcore.dll")]
    private static partial int GetProcessDpiAwareness(nint hprocess, out int value);

    /// <summary>
    /// Determines the initial host/DPI scale. If the process is DPI-unaware, compensate
    /// for Windows DPI virtualization; if it's DPI-aware, use per-window DPI.
    /// </summary>
    internal static float DetermineInitialScale(nint parent)
    {
        try
        {
            GetProcessDpiAwareness(GetCurrentProcess(), out int awareness);

            if (awareness == 0) // DPI_AWARENESS_UNAWARE → compensate virtualization
            {
                int sys = SafeGetDpiForSystem();
                return 96f / sys; // e.g., 96/144 = 0.666.. (draw smaller, Windows scales up)
            }
            else
            {
                int dpi = SafeGetDpiForWindow(parent);
                return dpi / 96f; // e.g., 144/96 = 1.5
            }
        }
        catch
        {
            return 1.0f;
        }
    }
}