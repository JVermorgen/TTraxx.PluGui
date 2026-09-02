using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct XEvent { public int type; private unsafe fixed byte pad[188]; } // rough enough for type-dispatch; overlay specific event-structs below via marshalling as needed