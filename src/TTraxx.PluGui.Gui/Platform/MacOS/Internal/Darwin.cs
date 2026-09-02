using SkiaSharp;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.MacOS.Internal.Structs;

namespace TTraxx.PluGui.Gui.Platform.MacOS.Internal;

/// <summary>
/// <para>
/// dladdr() wrapper to find the directory where our own (NativeAOT-
/// compiled) plugin binary is located. dladdr is POSIX and exists on Darwin too, but the library name
/// it lives in differs: libdl.so.2 on Linux vs libSystem on macOS.
/// </para>
/// <para>
/// Why this is needed: as an embedded VST3 in a host DAW, "our own
/// executable" is not the host executable but our own mach-o binary
/// (MyPlugin.vst3/Contents/MacOS/MyPlugin), and AppContext.BaseDirectory
/// doesn't reliably point to it. dladdr() on an address from our own
/// (natively compiled) code returns the correct path.
/// </para>
/// </summary>
internal static partial class Darwin
{
    private const string LibSystem = "/usr/lib/libSystem.B.dylib";

    [LibraryImport(LibSystem)]
    [return: MarshalAs(UnmanagedType.I4)]
    private static partial int dladdr(nint addr, out DlInfo info);

    /// <summary>
    /// Returns the directory of the current (mach-o) binary, or the
    /// working directory if the lookup fails for any reason
    /// (prefer a wrong-but-existing fallback over a crash during
    /// static-ctor bootstrap).
    /// </summary>
    internal static unsafe string GetOwnModuleDirectory()
    {
        // Any address of code that actually lives in our own compiled
        // binary suffices — dladdr looks up which loaded image contains
        // that memory address, not based on a specific function. We use
        // the address of this method itself via a local function pointer.
        delegate* unmanaged<void> self = &Noop;
        nint address = (nint)self;

        if (dladdr(address, out var info) != 0 && info.FileName != nint.Zero)
        {
            string? path = Marshal.PtrToStringUTF8(info.FileName);
            if (!string.IsNullOrEmpty(path))
            {
                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) return dir;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    [UnmanagedCallersOnly]
    private static void Noop()
    {
        // Does nothing — exists only to provide a valid address via &Noop
        // that is guaranteed to be in our own binary.
    }

    public static void EnsureOwnDirectoryLoaded(string libraryFileName)
    {
        string ownDir = GetOwnModuleDirectory();
        string fullPath = Path.Combine(ownDir, $"{libraryFileName}.dylib");

        NativeLibrary.SetDllImportResolver(typeof(SKImageInfo).Assembly, (name, _, _) =>
            name == libraryFileName ? NativeLibrary.Load(fullPath) : nint.Zero);
    }
}