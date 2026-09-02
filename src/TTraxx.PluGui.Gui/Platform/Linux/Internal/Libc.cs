using SkiaSharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Linux.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.Linux.Internal;

internal static partial class Libc
{
    [LibraryImport("libc.so.6", EntryPoint = "dladdr")]
    private static partial int dladdr(nint addr, out DlInfo info);

    private static unsafe string GetOwnModuleDirectory()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AnchorMethod()
        { }

        delegate*<void> anchor = &AnchorMethod;
        var anchorAddress = (nint)anchor;

        if (dladdr(anchorAddress, out DlInfo info) == 0 || info.dli_fname == nint.Zero)
            throw new InvalidOperationException("dladdr failed to resolve own module path.");

        var path = Marshal.PtrToStringUTF8(info.dli_fname);
        return Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Could not determine directory.");
    }

    public static void EnsureOwnDirectoryLoaded(string libraryFileName)
    {
        string ownDir;
        try { ownDir = GetOwnModuleDirectory(); } //AOT (plugin)
        catch (InvalidOperationException) { return; } //JIT (e.g. harness)
        var fullPath = Path.Combine(ownDir, $"{libraryFileName}.so");

        NativeLibrary.SetDllImportResolver(typeof(SKImageInfo).Assembly, (name, _, _) =>
            name == libraryFileName ? NativeLibrary.Load(fullPath) : nint.Zero);
    }
}