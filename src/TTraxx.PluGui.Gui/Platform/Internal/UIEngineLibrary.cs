using SkiaSharp;
using System.Runtime.InteropServices;

namespace TTraxx.PluGui.Gui.Platform.Internal;

/// <summary>
/// <para>
/// Binds SkiaSharp's P/Invokes to the Skia binary that sits next to <em>our own</em> plugin
/// binary, rather than letting the runtime (or the OS loader) pick one for us.
/// </para>
/// <para>
/// Two separate problems, one fix:
/// </para>
/// <para>
/// 1. As an embedded plugin we are not the main executable, so
/// <see cref="AppContext.BaseDirectory"/> points at the host DAW, not at our .vst3 — the
/// runtime's default probing simply doesn't look where our Skia lives. Each platform layer
/// therefore resolves its own module path (GetModuleHandleEx on Win32, dladdr elsewhere) and
/// hands it to <see cref="Register"/>.
/// </para>
/// <para>
/// 2. Windows' loader keys its loaded-module table on <em>base name</em>. If another
/// Skia-based plugin in the same host process got there first, a load of "libSkiaSharp.dll"
/// returns <em>their</em> module — whatever Skia version that happens to be — even when we
/// ask by full path. Since SkiaSharp's C ABI shifts between Skia milestones, that is a crash
/// in someone else's binary, on a user's machine, that we cannot reproduce.
/// </para>
/// <para>
/// So a plugin may ship Skia under a private file name: <c>libSkiaSharp_&lt;PluginName&gt;.dll</c>
/// next to the plugin binary, where &lt;PluginName&gt; is that binary's own file name without
/// extension (OriGen8.vst3 → libSkiaSharp_OriGen8.dll). We prefer that file when it exists and
/// fall back to the stock name otherwise, so a plugin opts in purely by renaming the file it
/// publishes — no API call, and therefore no way to get the ordering wrong relative to the
/// static ctor that runs this.
/// </para>
/// </summary>
internal static class UIEngineLibrary
{
    /// <summary>
    /// The module name SkiaSharp's own [DllImport]s ask for. Baked into the SkiaSharp
    /// assembly, so it stays "libSkiaSharp" no matter what the file on disk is called —
    /// which is exactly why a resolver, not just a preload, is needed to redirect it.
    /// </summary>
    public const string StockName = "libSkiaSharp";

    /// <summary>
    /// Points SkiaSharp at the Skia binary next to <paramref name="ownModulePath"/>.
    /// Does nothing if the module path is unknown (JIT builds such as the harness, where
    /// there is no native module to ask about) or if no Skia binary is found there — in
    /// both cases the runtime's default probing is left to do its normal job.
    /// </summary>
    /// <param name="ownModulePath">Full path to our own native binary.</param>
    /// <param name="extension">Platform library extension, including the dot.</param>
    public static void Register(string? ownModulePath, string extension)
    {
        if (string.IsNullOrEmpty(ownModulePath)) return;

        string? ownDir = Path.GetDirectoryName(ownModulePath);
        if (string.IsNullOrEmpty(ownDir)) return;

        string pluginName = Path.GetFileNameWithoutExtension(ownModulePath);
        string fullPath = Path.Combine(ownDir, $"{StockName}_{pluginName}{extension}");

        if (!File.Exists(fullPath))
        {
            fullPath = Path.Combine(ownDir, $"{StockName}{extension}");
            if (!File.Exists(fullPath)) return;
        }

        // Resolve once, here, rather than per callback: NativeLibrary.Load caches by
        // path anyway, but this also surfaces a missing/broken Skia during bootstrap
        // instead of on the first paint.
        nint handle = NativeLibrary.Load(fullPath);

        NativeLibrary.SetDllImportResolver(typeof(SKImageInfo).Assembly,
            (name, _, _) => name == StockName ? handle : nint.Zero);
    }
}
