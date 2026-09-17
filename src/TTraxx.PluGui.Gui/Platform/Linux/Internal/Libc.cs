using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Internal;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

internal static partial class Libc
{
    [LibraryImport("libc.so.6", EntryPoint = "dladdr")]
    private static partial int dladdr(nint addr, out DlInfo info);

    /// <summary>
    /// Returns the full path of our own (NativeAOT-compiled) plugin binary, or
    /// <c>null</c> when dladdr cannot name one — the normal case under JIT (e.g. the
    /// harness), where the anchor address lives in emitted code, not in a loaded image.
    /// </summary>
    private static unsafe string? GetOwnModulePath()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AnchorMethod()
        { }

        delegate*<void> anchor = &AnchorMethod;
        var anchorAddress = (nint)anchor;

        if (dladdr(anchorAddress, out DlInfo info) == 0 || info.dli_fname == nint.Zero)
            return null;

        return Marshal.PtrToStringUTF8(info.dli_fname);
    }

    /// <summary>
    /// Binds SkiaSharp to the Skia .so shipped next to our own plugin binary.
    /// See <see cref="UIEngineLibrary"/> for why this is not left to default probing.
    /// </summary>
    public static void RegisterUIEngineResolver() => UIEngineLibrary.Register(GetOwnModulePath(), ".so");
}