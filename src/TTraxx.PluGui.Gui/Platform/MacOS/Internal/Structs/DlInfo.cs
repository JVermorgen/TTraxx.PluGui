using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

/// <summary>
/// Darwin's <c>Dl_info</c>, filled in by <c>dladdr</c>. Same POSIX layout as the Linux counterpart,
/// declared separately per platform so each stays with its own interop surface; the fields carry
/// descriptive names here rather than the native <c>dli_</c> prefixes.
///
/// Only <see cref="FileName"/> is actually read - see Darwin, which uses it to locate the mach-o
/// image the plugin's own code lives in. All four pointers are owned by the loader and must not be
/// freed.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DlInfo
{
    /// <summary>const char* - path to the shared object containing the queried address.</summary>
    public nint FileName;   // const char* - path to the shared object

    /// <summary>Base address at which that image is loaded.</summary>
    public nint FileBase;

    /// <summary>const char* - name of the nearest symbol at or before the address, if any.</summary>
    public nint SymbolName;

    /// <summary>Exact address of that symbol.</summary>
    public nint SymbolAddr;
}
