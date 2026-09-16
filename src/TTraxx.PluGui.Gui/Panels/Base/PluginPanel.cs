using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Shared geometry and theming for panel chrome. The configuration is in unscaled design units;
/// every accessor here returns pixels at <paramref name="context"/>'s scale.
/// </summary>
/// <param name="config">Position and size, in design units.</param>
/// <param name="context">The owning window's context - pass the window's <c>Context</c>.</param>
public abstract class PluginPanel(RenderContext context, PluginPanelConfiguration config) : IPluginPanel
{
    protected readonly PluginPanelConfiguration _config = config;

    /// <summary>Scale, theme and fonts of the window this panel belongs to.</summary>
    protected RenderContext Context { get; } = context;

    public int Height => Context.Rescale(_config.Height);
    public int Left => Context.Rescale(_config.Left);
    public int Top => Context.Rescale(_config.Top);
    public int Width => Context.Rescale(_config.Width);

    public int CornerRadius => Context.Rescale(_config.CornerRadius);

    private readonly List<ControlPlacement> _controls = [];
    public IReadOnlyList<ControlPlacement> Controls => _controls;

    public ControlPlacement AddControl(PluginControl control, int relativeX, int relativeY, int width, int height)
    {
        ControlPlacement placed = new(control, _config.Left + relativeX, _config.Top + relativeY, width, height);
        _controls.Add(placed);
        return placed;
    }

    public abstract void Draw(SKCanvas canvas);

    /// <summary>Scales a design-unit length to whole pixels, at this panel's window scale.</summary>
    protected int Rescale(float designUnits) => Context.Rescale(designUnits);

    /// <summary>Scales a design-unit length without rounding, at this panel's window scale.</summary>
    protected float RescaleExact(float designUnits) => Context.RescaleExact(designUnits);

    /// <summary>This panel's window theme.</summary>
    protected IPluginTheme Theme => Context.Theme;

    /// <summary>The metallic-panel colors of this panel's window theme.</summary>
    protected IMetallicPanelTheme MetallicTheme => Context.MetallicTheme;

    /// <summary>This panel's window fonts.</summary>
    protected IPluginFonts Fonts => Context.Fonts;

    protected float FromLeft(int pixels) => Left + Context.RescaleExact(pixels);

    protected float FromRight(int pixels) => Left + Width - Context.RescaleExact(pixels);

    protected float FromTop(int pixels) => Top + Context.RescaleExact(pixels);
}
