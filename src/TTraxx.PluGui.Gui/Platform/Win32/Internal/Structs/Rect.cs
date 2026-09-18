using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Structs;

/// <summary>
/// Win32 RECT. Note this is edge coordinates, not position-plus-size: width is Right - Left. Used by
/// GetClientRect and as part of <see cref="PaintStruct"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    /// <summary>Left edge, inclusive.</summary>
    internal int Left;

    /// <summary>Top edge, inclusive.</summary>
    internal int Top;

    /// <summary>Right edge, exclusive.</summary>
    internal int Right;

    /// <summary>Bottom edge, exclusive.</summary>
    internal int Bottom;
}
