
namespace TTraxx.PluGui.Gui;

/// <summary>
/// Everything needed to turn a window's design units into pixels and to pick its colors and fonts:
/// the content scale, the theme and the font set. One instance per window, owned by the window and
/// handed to every control and panel it lays out.
///
/// WHY THIS IS PER WINDOW: a DAW routinely opens two editors for the same plugin, and the plugin
/// binary is loaded once per process - so a process-wide scale factor is simply wrong as soon as
/// those two editors sit on monitors with different DPI. The last one to attach would win, for
/// both. Scale therefore belongs to the window, not to a static.
///
/// <see cref="Theme"/> and <see cref="Fonts"/> fall back to the process-wide defaults
/// (<see cref="PluginDefaults"/>, normally set once
/// at startup) unless a window sets its own. That keeps the usual "one theme per plugin" setup a
/// one-liner, while leaving room for a per-window theme (e.g. a host-provided light/dark switch).
/// </summary>
public sealed class RenderContext
{
    private IPluginTheme? _theme;
    private IPluginFonts? _fonts;

    /// <summary>
    /// Content scale for this window: the host's DPI/content scale times whatever extra multiplier
    /// the plugin applies. 1.0 means "design units are pixels". Set by the editor view on attach and
    /// whenever the host reports a new scale; re-apply the window's layout afterwards.
    /// </summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>This window's theme. Defaults to <see cref="PluginDefaults.Theme"/>.</summary>
    public IPluginTheme Theme
    {
        get => _theme ?? PluginDefaults.Theme;
        set => _theme = value;
    }

    /// <summary>This window's fonts. Defaults to <see cref="PluginDefaults.Fonts"/>.</summary>
    public IPluginFonts Fonts
    {
        get => _fonts ?? PluginDefaults.Fonts;
        set => _fonts = value;
    }

    /// <summary>
    /// The metallic-panel colors for <see cref="Theme"/>, or the built-in default set when this
    /// theme doesn't provide any.
    /// </summary>
    public IMetallicPanelTheme MetallicTheme => Theme as IMetallicPanelTheme ?? DefaultMetallicPanelTheme.Instance;

    /// <summary>Scales a design-unit length to whole pixels.</summary>
    public int Rescale(float designUnits) => (int)Math.Round(Scale * designUnits);

    /// <summary>Scales a design-unit length without rounding - for geometry that should stay sub-pixel exact.</summary>
    public float RescaleExact(float designUnits) => Scale * designUnits;
}
