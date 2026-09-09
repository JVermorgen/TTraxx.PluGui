using System.Reflection.Metadata;
using TTraxx.PluGui.Gui.Helpers.HotReload;
using TTraxx.PluGui.Gui.Helpers.Typography;

[assembly: MetadataUpdateHandler(typeof(GuiCacheUpdateHandler))]

namespace TTraxx.PluGui.Gui.Helpers.HotReload;

internal static class GuiCacheUpdateHandler
{
    /// <summary>
    /// Invoked after a Hot Reload edit is applied, before UpdateApplication.
    /// Drops cached resources whose builders may have just been rewritten.
    /// </summary>
    /// <param name="updatedTypes">
    /// Types affected by the update, or null when the runtime can't narrow
    /// it down (in which case anything may have changed). The parameter is
    /// part of the signature the runtime matches on - it must stay even
    /// where the body ignores it.
    /// </param>
    internal static void ClearCache(Type[]? updatedTypes)
    {
        // null means "unknown scope" - clear everything to be safe.
        if (updatedTypes is null)
        {
            Icons.InvalidateCache();
            Fonts.InvalidateCache();
            return;
        }

        foreach (var type in updatedTypes)
        {
            if (type == typeof(Icons)) Icons.InvalidateCache();
            if (type == typeof(DefaultPluginFonts) || type == typeof(Fonts)) Fonts.InvalidateCache();
        }
    }

    /// <summary>
    /// Invoked after every handler's ClearCache has run. Used here purely
    /// to force a repaint so the edit is visible without touching the UI.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "Any edit can affect rendering - a Draw body, a theme colour, a layout constant - so repaint unconditionally rather than trying to guess from updatedTypes which changes are visual.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Any edit can affect rendering - a Draw body, a theme colour, a layout constant - so repaint unconditionally rather than trying to guess from updatedTypes which changes are visual.")]
    internal static void UpdateApplication(Type[]? updatedTypes)
        => GuiHotReload.RaiseReloaded();
}