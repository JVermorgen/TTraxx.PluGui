using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Shared geometry and theming for panel chrome. The configuration is in unscaled design units;
/// every accessor here returns pixels at <paramref name="context"/>'s scale.
///
/// A concrete panel implements <see cref="Draw"/> and builds its geometry from the scaled
/// <see cref="Left"/>/<see cref="Top"/>/<see cref="Width"/>/<see cref="Height"/> accessors and the
/// <see cref="FromLeft"/>/<see cref="FromRight"/>/<see cref="FromTop"/> helpers - never from the raw
/// configuration, which is unscaled. See <see cref="MetallicPluginPanel"/> for a worked example.
/// </summary>
/// <param name="config">Position and size, in design units.</param>
/// <param name="context">The owning window's context - pass the window's <c>Context</c>.</param>
public abstract class PluginPanel(RenderContext context, PluginPanelConfiguration config) : IPluginPanel
{
    /// <summary>
    /// This panel's unscaled configuration. Read <see cref="PluginPanelConfiguration.Title"/> and the
    /// like from here, but use the scaled accessors below for geometry.
    /// </summary>
    protected readonly PluginPanelConfiguration _config = config;

    /// <summary>Scale, theme and fonts of the window this panel belongs to.</summary>
    protected RenderContext Context { get; } = context;

    /// <summary>Body height in pixels.</summary>
    public int Height => Context.Rescale(_config.Height);

    /// <summary>Body left edge in pixels, window-relative.</summary>
    public int Left => Context.Rescale(_config.Left);

    /// <summary>Body top edge in pixels, window-relative. A header, if the style draws one, sits above this.</summary>
    public int Top => Context.Rescale(_config.Top);

    /// <summary>Body width in pixels.</summary>
    public int Width => Context.Rescale(_config.Width);

    /// <summary>Corner rounding in pixels.</summary>
    public int CornerRadius => Context.Rescale(_config.CornerRadius);

    private readonly List<ControlPlacement> _controls = [];

    /// <inheritdoc/>
    public IReadOnlyList<ControlPlacement> Controls => _controls;

    /// <inheritdoc/>
    /// <remarks>
    /// The offset is applied in UNSCALED design units - the placement returned here is rescaled later,
    /// when the window calls PluginControl.SetBounds on it. So this must not use the scaled
    /// <see cref="Left"/>/<see cref="Top"/> accessors, or the offset would be scaled twice.
    /// </remarks>
    public ControlPlacement AddControl(PluginControl control, int relativeX, int relativeY, int width, int height)
    {
        ControlPlacement placed = new(control, _config.Left + relativeX, _config.Top + relativeY, width, height);
        _controls.Add(placed);
        return placed;
    }

    /// <inheritdoc/>
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

    /// <summary>
    /// Absolute X, <paramref name="pixels"/> design units in from the panel's left edge. Inset helpers
    /// like this one keep a panel's drawing code readable at any scale: the numbers written down stay
    /// the design-unit insets, with the scaling and the panel's own origin folded in here.
    /// </summary>
    protected float FromLeft(int pixels) => Left + Context.RescaleExact(pixels);

    /// <summary>Absolute X, <paramref name="pixels"/> design units in from the panel's RIGHT edge.</summary>
    protected float FromRight(int pixels) => Left + Width - Context.RescaleExact(pixels);

    /// <summary>
    /// Absolute Y, <paramref name="pixels"/> design units down from the panel's top edge. Pass a
    /// NEGATIVE value to reach above the body - that's how a style places a header tab.
    /// </summary>
    protected float FromTop(int pixels) => Top + Context.RescaleExact(pixels);
}
