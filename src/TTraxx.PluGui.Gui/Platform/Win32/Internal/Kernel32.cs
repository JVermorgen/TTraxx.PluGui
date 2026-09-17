using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TTraxx.PluGui.Gui.Platform.Internal;
using static TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants.Kernel32Constants;

namespace TTraxx.PluGui.Gui.Platform.Win32.Internal;

internal static partial class Kernel32
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetModuleHandleEx(uint dwFlags, nint lpModuleName, out nint phModule);

    [LibraryImport("kernel32.dll")]
    internal static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetModuleHandleExW(uint dwFlags, nint lpModuleName, out nint phModule);

    // char* instead of StringBuilder — LibraryImport supports blittable
    // pointer parameters directly, no marshaller needed.
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleFileNameW", SetLastError = true)]
    private static unsafe partial uint GetModuleFileNameW(nint hModule, char* lpFilename, uint nSize);

    /// <summary>
    /// Returns the full path of the native module (the .vst3 file), not of the host
    /// process, or <c>null</c> when there is no such module — which is the normal case
    /// under JIT (e.g. the harness), where our code lives in dynamically emitted memory
    /// that belongs to no loaded module at all.
    /// </summary>
    private static unsafe string? GetOwnModulePath()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AnchorMethod()
        { }

        delegate*<void> anchor = &AnchorMethod;
        nint anchorAddress = (nint)anchor;

        if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, anchorAddress, out nint hModule))
            return null;

        const int bufferSize = 260;
        char* buffer = stackalloc char[bufferSize];
        uint len = GetModuleFileNameW(hModule, buffer, bufferSize);
        if (len == 0) return null;

        return new string(buffer, 0, (int)len);
    }

    /// <summary>
    /// Binds SkiaSharp to the Skia DLL shipped next to our own .vst3.
    /// See <see cref="UIEngineLibrary"/> for why this is not left to default probing.
    /// </summary>
    public static void RegisterUIEngineResolver() => UIEngineLibrary.Register(GetOwnModulePath(), ".dll");
}