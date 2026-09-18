using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

/// <summary>
/// POSIX <c>Dl_info</c>, filled in by <c>dladdr</c>. Field names keep the native <c>dli_</c> prefix,
/// and the order matches the header exactly since the struct is marshalled by layout.
///
/// Only <see cref="dli_fname"/> is actually read - see Libc, which uses it to find the path of the
/// shared object the plugin's own code lives in so Skia can be loaded from beside it.
/// All four pointers are owned by the loader and must not be freed.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct DlInfo
{
    /// <summary>const char* - path of the shared object containing the queried address.</summary>
    public nint dli_fname;

    /// <summary>Base address at which that shared object is loaded.</summary>
    public nint dli_fbase;

    /// <summary>const char* - name of the nearest symbol at or before the address, if any.</summary>
    public nint dli_sname;

    /// <summary>Exact address of that symbol.</summary>
    public nint dli_saddr;
}
