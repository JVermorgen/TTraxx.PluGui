using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct DlInfo
{
    public nint dli_fname;
    public nint dli_fbase;
    public nint dli_sname;
    public nint dli_saddr;
}