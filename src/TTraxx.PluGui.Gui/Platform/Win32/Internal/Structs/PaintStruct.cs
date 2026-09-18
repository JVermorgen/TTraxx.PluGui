using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

/// <summary>
/// Win32 PAINTSTRUCT, filled in by BeginPaint and handed back to EndPaint. Only
/// <see cref="hdc"/> is read - the window repaints its whole client area from the Skia surface rather
/// than honouring the update rectangle, so the rest is carried purely to satisfy the API contract.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct PaintStruct
{
    /// <summary>Device context to paint into, valid until EndPaint.</summary>
    internal nint hdc;

    /// <summary>Whether Windows wants the background erased. Ignored, since WM_ERASEBKGND is handled as a no-op.</summary>
    [MarshalAs(UnmanagedType.Bool)]
    internal bool fErase;

    /// <summary>The region needing repaint. Ignored - the full client area is redrawn.</summary>
    internal Rect rcPaint;

    /// <summary>Reserved for Windows' internal use.</summary>
    [MarshalAs(UnmanagedType.Bool)]
    internal bool fRestore;

    /// <summary>Reserved for Windows' internal use.</summary>
    [MarshalAs(UnmanagedType.Bool)]
    internal bool fIncUpdate;

    /// <summary>Reserved for Windows' internal use. Present so the struct's size matches the native one.</summary>
    internal fixed byte rgbReserved[32];
}
