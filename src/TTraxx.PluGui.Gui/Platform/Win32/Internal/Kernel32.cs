using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetDllDirectoryW([MarshalAs(UnmanagedType.LPWStr)] string lpPathName);

    /// <summary>
    /// Returns the directory of the native module (the .vst3 file), not
    /// of the host process.
    /// </summary>
    private static unsafe string GetOwnModuleDirectory()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AnchorMethod()
        { }

        delegate*<void> anchor = &AnchorMethod;
        nint anchorAddress = (nint)anchor;

        if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, anchorAddress, out nint hModule))
            throw new InvalidOperationException("GetModuleHandleExW failed.");

        const int bufferSize = 260;
        char* buffer = stackalloc char[bufferSize];
        uint len = GetModuleFileNameW(hModule, buffer, bufferSize);
        if (len == 0) throw new InvalidOperationException("GetModuleFileNameW failed.");

        string path = new(buffer, 0, (int)len);
        return Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Could not determine directory.");
    }

    public static void EnsureOwnDirectoryLoaded(string libraryFileName)
    {
        libraryFileName = $"{libraryFileName}.dll";
        string ownDir;
        try { ownDir = GetOwnModuleDirectory(); } //AOT (plugin)
        catch (InvalidOperationException) { return; } //JIT (e.g. harness)
        SetDllDirectoryW(ownDir);

        string fullPath = Path.Combine(ownDir, libraryFileName);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"{libraryFileName} not found at expected path: {fullPath}");

        NativeLibrary.Load(fullPath);
    }
}