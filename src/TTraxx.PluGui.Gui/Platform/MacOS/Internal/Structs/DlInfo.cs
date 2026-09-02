using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct DlInfo
{
    public nint FileName;   // const char* - path to the shared object
    public nint FileBase;
    public nint SymbolName;
    public nint SymbolAddr;
}