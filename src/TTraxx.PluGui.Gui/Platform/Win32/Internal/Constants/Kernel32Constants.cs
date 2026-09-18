namespace TTraxx.PluGui.Gui.Platform.Win32.Internal.Constants;

/// <summary>kernel32.dll flag values used by the module-path lookup in Kernel32.</summary>
internal static class Kernel32Constants
{
    /// <summary>
    /// Tells GetModuleHandleEx to treat its argument as an ADDRESS inside a module rather than a module
    /// name, which is how the plugin locates its own .vst3 without knowing what it was named.
    /// Note the flag does not increment the module's reference count.
    /// </summary>
    public const uint GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x00000004;
}
