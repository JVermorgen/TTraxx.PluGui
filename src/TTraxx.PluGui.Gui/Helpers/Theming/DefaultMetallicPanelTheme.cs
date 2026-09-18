using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Built-in brushed-metal panel colors, used for any theme that doesn't implement
/// <see cref="IMetallicPanelTheme"/> itself. Tuned against <see cref="DefaultPluginTheme"/>'s dark
/// background, so a markedly lighter theme will want its own.
/// </summary>
public sealed class DefaultMetallicPanelTheme : IMetallicPanelTheme
{
    /// <summary>
    /// Shared instance. Safe as a singleton because every member is a computed constant - there's no
    /// per-use state to collide over, so <see cref="RenderContext"/> hands the same one to every window.
    /// </summary>
    public static readonly DefaultMetallicPanelTheme Instance = new();

    public SKColor PanelMetalTop => new(80, 80, 80);
    public SKColor PanelMetalBottom => new(45, 45, 45);
    public SKColor PanelMetalBorder => new(120, 120, 120);
    public SKColor PanelMetalSheen => new(255, 255, 255, 90);
    public SKColor PanelMetalHeaderHighlight => new(35, 35, 35);
    public SKColor PanelMetalHeaderShadow => new(12, 12, 12);
}
