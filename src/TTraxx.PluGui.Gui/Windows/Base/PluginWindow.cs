using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Manager;
using TTraxx.PluGui.Gui.Controls.Overlay;
using TTraxx.PluGui.Gui.Windows.Factories;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Base class for a plugin's top-level GUI window. Owns the platform window (created via
/// <see cref="PlatformWindowFactory"/>), the control cache/lifecycle, layout, and input/paint
/// dispatch - so a concrete window only has to describe its content, not how it gets on screen.
///
/// A derived window implements two things:
/// <list type="bullet">
/// <item><see cref="DrawBackground"/> - paints whatever isn't a control (gradients, dividers, logo, ...).</item>
/// <item><see cref="BuildLayout"/> - yields every control the window shows, via <see cref="Place"/>/
/// <see cref="PlaceGrid"/>, each already resolved to an absolute (window-relative) position.</item>
/// </list>
/// Optionally, <see cref="ConfigureSizing"/> can register window-specific sizing overrides via
/// <see cref="RegisterSizing{TConfig}"/> before the first <see cref="BuildLayout"/> call.
///
/// This type serves two audiences, and they're deliberately kept apart:
/// <list type="bullet">
/// <item>A DERIVED WINDOW uses the protected surface below (BuildLayout/DrawBackground/Place/
/// PlaceGrid/RegisterPanel/RegisterSizing) - that's what deriving from this class is for.</item>
/// <item>A HOST (VST3 editor view, harness, ...) drives the window through <see cref="IPluginWindow"/>:
/// <see cref="AttachToParent"/> creates the native window and runs the first build, then
/// <see cref="SetBounds"/> on resize and <see cref="Destroy"/> on teardown. Host-side code should
/// depend on that interface rather than on this class.</item>
/// </list>
///
/// LIFECYCLE: attaching twice throws (it would leak the first native window), <see cref="Destroy"/>
/// is idempotent, and SetBounds/RefreshUI/TryFindParameter no-op once the window is destroyed - so a
/// host refresh timer or a deferred resize that races teardown can't reach a dead native window.
/// Everything else (pointer/wheel/paint routing, control caching, panel drawing) is handled
/// internally - see the private <c>Build</c>/<c>ApplyLayout</c> methods and the explicit
/// <see cref="IPlatformWindowHost"/> implementation below.
/// </summary>
public abstract class PluginWindow : IPluginWindow, IHotReloadTarget, IPlatformWindowHost
{
    private int _windowWidth;
    private int _windowHeight;

    // Controls survive across rebuilds, keyed by their configuration's Id, so BuildLayout()
    // can be re-run (e.g. on hot reload) without losing per-control state (drag position, focus, ...).
    private readonly Dictionary<Guid, PluginControl> _controlCache = [];
    // Window-specific overrides registered via RegisterSizing(), keyed by config type.
    private readonly Dictionary<Type, Func<ControlSizes, (int Width, int Height)>> _sizingStrategies = [];
    private readonly ControlManager _controlManager = new();

    // Non-null only while a Build() is in progress; tracks which cached control ids this
    // build actually used, so Build() can evict the ones that weren't (removed controls).
    private HashSet<Guid>? _buildIdsInProgress;
    private List<ControlPlacement> _layout = [];
    private List<IPluginPanel> _registeredPanels = [];
    private ContextMenuOverlay? _contextMenu;
    // Set once ConfigureSizing() has run for this window instance, so it's called exactly once
    // regardless of how many times Build() (via RebuildControls()) runs afterward.
    private bool _sizingConfigured;

    // Lifecycle guards. Attaching twice would leak the first native window and orphan its
    // controls, so that's treated as a programming error and throws. Everything else is lenient
    // on purpose: a host can legitimately call RefreshUI()/SetBounds() from a timer or a deferred
    // resize that races Destroy(), so those no-op once the native window is gone.
    private bool _isAttached;
    private bool _isDestroyed;

    private IPlatformWindow PlatformWindow { get; } = PlatformWindowFactory.Create();

    /// <inheritdoc/>
    public RenderContext Context { get; } = new();

    /// <summary>Scales a design-unit length to whole pixels, at this window's scale.</summary>
    protected int Rescale(float designUnits) => Context.Rescale(designUnits);

    /// <summary>Scales a design-unit length without rounding, at this window's scale.</summary>
    protected float RescaleExact(float designUnits) => Context.RescaleExact(designUnits);

    /// <summary>This window's theme.</summary>
    protected IPluginTheme Theme => Context.Theme;

    /// <summary>The metallic-panel colors of this window's theme.</summary>
    protected IMetallicPanelTheme MetallicTheme => Context.MetallicTheme;

    /// <summary>This window's fonts.</summary>
    protected IPluginFonts Fonts => Context.Fonts;

    /// <summary>
    /// True between a successful <see cref="AttachToParent"/> and <see cref="Destroy"/> - i.e. while
    /// a native window actually exists. Check this before touching platform resources from a derived
    /// window's own timers or callbacks.
    /// </summary>
    protected bool IsLive => _isAttached && !_isDestroyed;

    /// <inheritdoc/>
    public bool AttachToParent(nint parentHandle, int width, int height)
    {
        ObjectDisposedException.ThrowIf(_isDestroyed, this);
        if (_isAttached)
            throw new InvalidOperationException(
                $"{GetType().Name} is already attached. Use one window instance per host view - " +
                "re-attaching would leak the native window the first call created.");

        _windowWidth = width;
        _windowHeight = height;

        if (!PlatformWindow.Attach(parentHandle, width, height, this)) return false;

        _isAttached = true;
        Build();
        return true;
    }

    /// <inheritdoc/>
    public void SetBounds(int x, int y, int width, int height)
    {
        if (!IsLive) return;

        _windowWidth = width;
        _windowHeight = height;

        PlatformWindow.SetBounds(x, y, width, height);
        ApplyLayout();
        PlatformWindow.Invalidate();
    }

    /// <inheritdoc/>
    public void RefreshUI()
    {
        if (!IsLive) return;
        PlatformWindow.Invalidate();
    }

    /// <inheritdoc/>
    public bool TryFindParameter(int x, int y, out int parameterId)
    {
        if (!IsLive)
        {
            parameterId = 0;
            return false;
        }

        return _controlManager.TryFindParameter(x, y, out parameterId);
    }

    /// <inheritdoc/>
    public void Destroy()
    {
        if (_isDestroyed) return;

        _isDestroyed = true;
        _isAttached = false;

        OnDestroying();
        PlatformWindow.Destroy();
    }

    /// <inheritdoc/>
    public float GetInitialScaleFactor(nint parentHandle) => PlatformWindow.GetInitialScaleFactor(parentHandle);

    /// <inheritdoc/>
    public IEventPumpSource? EventPumpSource => PlatformWindow.EventPumpSource;

    /// <inheritdoc/>
    public ITimerPumpSource? TimerPumpSource => PlatformWindow.TimerPumpSource;

    /// <summary>
    /// Harness-only: calls BuildLayout() again (Hot Reload of positions, panels, or
    /// new/removed controls) and synchronizes the control-manager. A production host never
    /// calls this - which is why it's an explicit <see cref="IHotReloadTarget"/> implementation,
    /// kept off the window's everyday surface. Cast to that interface to reach it.
    /// </summary>
    void IHotReloadTarget.RebuildControls()
    {
        if (!IsLive) return;
        Build();
    }

    /// <summary>
    /// Called once, from <see cref="Destroy"/>, just before the native window goes away - for a
    /// derived window to release resources of its own. No-op by default.
    ///
    /// This is a hook rather than an overridable Destroy() on purpose: teardown order and the
    /// "exactly once" guarantee stay with the base class, so a derived window can't break them by
    /// forgetting a base call.
    /// </summary>
    protected virtual void OnDestroying() { }

    /// <summary>
    /// Whether this window honors Globals.ShowControlBounds (the harness's "show
    /// control bounds" dev-tool switch). True for a plugin's own content - the
    /// intended target - and overridden to false by the harness's own chrome (e.g.
    /// HarnessSettingsPanel), so the dev-tool bar doesn't outline its own controls.
    /// </summary>
    protected virtual bool ParticipatesInDebugBoundsOverlay => true;

    /// <summary>
    /// Paints everything in the window that isn't a control: background, gradients, dividers, static
    /// text/logo, etc. Called every frame before controls are drawn on top. Call <see cref="DrawRegisteredPanels"/>
    /// from here if the window has any panels, so they draw beneath their own controls.
    /// </summary>
    protected abstract void DrawBackground(SKCanvas canvas, int width, int height);

    /// <summary>
    /// Declares every control the window shows, as a sequence of <see cref="ControlPlacement"/>
    /// (control + absolute X/Y, width, height). Called once on <see cref="AttachToParent"/> and
    /// again on a hot-reload rebuild (see <see cref="IHotReloadTarget"/> - a production host never
    /// triggers one). Implement by yielding <see cref="Place"/>/<see cref="PlaceGrid"/> calls;
    /// controls are cached across rebuilds by their configuration's Id, so re-running this doesn't
    /// recreate controls that are still present.
    /// </summary>
    protected abstract IEnumerable<ControlPlacement> BuildLayout();

    /// <summary>
    /// Returns the cached control for <paramref name="configId"/>, creating it via <paramref name="factory"/>
    /// on first use. Marks the id as used by the current build (see <see cref="Build"/>) so it survives
    /// cache eviction. Prefer <see cref="Create{TConfig,TControl}"/> when the control's own configuration
    /// already carries its Id - it's the same thing without repeating the config.
    /// </summary>
    protected TControl GetOrCreate<TControl>(Guid configId, Func<TControl> factory) where TControl : PluginControl
    {
        _buildIdsInProgress?.Add(configId);

        if (_controlCache.TryGetValue(configId, out var existing) && existing is TControl typed)
            return typed;

        var created = factory();
        _controlCache[configId] = created;
        return created;
    }

    /// <summary>
    /// Shorthand for the common <c>GetOrCreate(config.Id, () => new TControl(config))</c>
    /// pattern - caches/creates a control keyed by its own configuration's Id, without
    /// having to repeat the config for both the key and the constructor call.
    /// </summary>
    protected TControl Create<TConfig, TControl>(TConfig config, Func<TConfig, TControl> factory)
        where TConfig : IControlConfiguration
        where TControl : PluginControl
        => GetOrCreate(config.Id, () => factory(config));

    /// <summary>
    /// Creates (or reuses) a control from its configuration and places it, always returning its
    /// ABSOLUTE (window-relative) position - the same shape whether or not <paramref name="panel"/>
    /// is given, so every call is uniformly <c>yield return Place(...)</c> from BuildLayout(),
    /// regardless of whether the control lives in a panel or not. When <paramref name="panel"/> is
    /// given, x/y are panel-relative (the panel converts them to absolute internally); otherwise
    /// they're already absolute.
    ///
    /// Width/height resolve in order: the explicit <paramref name="width"/>/<paramref name="height"/>
    /// if given, then a window-specific override registered for TConfig via RegisterSizing(), then
    /// TConfig's own ISizedControlConfiguration default (if it implements it); extraWidth/extraHeight
    /// are then added on top regardless (e.g. for a panel whose labels need more room than the
    /// control's own natural size).
    ///
    /// <paramref name="visibleWhen"/> shows the control only while it returns true - the building
    /// block for paged panels, where several controls share one area and a tab decides which set is
    /// on screen. Evaluated on every draw and hit-test; null (the default) means always visible.
    /// </summary>
    protected ControlPlacement Place<TConfig, TControl>(
        TConfig config,
        Func<TConfig, TControl> factory,
        int x, int y,
        int? width = null,
        int? height = null,
        int extraWidth = 0,
        int extraHeight = 0,
        IPluginPanel? panel = null,
        Func<bool>? visibleWhen = null)
        where TConfig : IControlConfiguration
        where TControl : PluginControl
    {
        var control = Create(config, factory);
        control.SetVisibleWhen(visibleWhen);
        var (w, h) = ResolveBounds(config, width, height);
        w += extraWidth;
        h += extraHeight;

        return panel is null
            ? new ControlPlacement(control, x, y, w, h)
            : panel.AddControl(control, x, y, w, h);
    }

    /// <summary>
    /// Places same-type controls on a grid: each entry in <paramref name="cells"/> names its own
    /// (Row, Column), spaced by <paramref name="columnPitch"/>/<paramref name="rowPitch"/> from
    /// (<paramref name="originX"/>, <paramref name="originY"/>). Since placement is keyed by the
    /// column/row each entry names rather than a running index, a cell is skipped by simply not
    /// including it - no need to special-case a ragged row or a hole in the middle of the grid.
    /// Every other parameter behaves exactly as on Place(), which this calls once per entry.
    /// </summary>
    protected IEnumerable<ControlPlacement> PlaceGrid<TConfig, TControl>(
        IEnumerable<(TConfig Config, int Row, int Column)> cells,
        Func<TConfig, TControl> factory,
        int originX, int originY,
        int rowPitch, int columnPitch,
        int? width = null, int? height = null,
        int extraWidth = 0, int extraHeight = 0,
        IPluginPanel? panel = null,
        Func<bool>? visibleWhen = null)
        where TConfig : IControlConfiguration
        where TControl : PluginControl
    {
        foreach (var (config, row, column) in cells)
        {
            var x = originX + (column * columnPitch);
            var y = originY + (row * rowPitch);
            yield return Place(config, factory, x, y, width, height, extraWidth, extraHeight, panel, visibleWhen);
        }
    }

    /// <summary>
    /// Registers, for one TConfig, a window-specific override of the function that derives its
    /// natural (Width, Height) from its ControlSize - so Place() can auto-size a TConfig control
    /// whenever it isn't given explicit width/height. This is only needed to override a
    /// TConfig's own default: any TConfig that implements ISizedControlConfiguration (every
    /// built-in sized config does, via its Style's Bounds table) already auto-sizes with no
    /// registration at all. Call from ConfigureSizing().
    /// </summary>
    protected void RegisterSizing<TConfig>(Func<ControlSizes, (int Width, int Height)> sizing)
        where TConfig : IControlConfiguration
        => _sizingStrategies[typeof(TConfig)] = sizing;

    /// <summary>
    /// Called once, before the first BuildLayout(), for derived windows to register any
    /// window-specific sizing overrides via RegisterSizing(). No-op by default - most windows
    /// won't need this at all, since ISizedControlConfiguration already covers the common case.
    /// </summary>
    protected virtual void ConfigureSizing() { }

    /// <summary>
    /// Registers a panel built this BuildLayout() call so DrawRegisteredPanels() draws it (in
    /// registration order). This is purely about drawing: a panel's controls still need their own
    /// <c>yield return Place(..., panel: thisPanel)</c> in BuildLayout() like any other control -
    /// Place() already resolves the right absolute position via the panel, so nothing further is
    /// needed to fold them into the layout.
    /// </summary>
    protected TPanel RegisterPanel<TPanel>(TPanel panel) where TPanel : IPluginPanel
    {
        _registeredPanels.Add(panel);
        return panel;
    }

    /// <summary>Draws every panel registered this build, in registration order.</summary>
    protected void DrawRegisteredPanels(SKCanvas canvas)
    {
        foreach (var panel in _registeredPanels) panel.Draw(canvas);
    }

    private (int W, int H) ResolveBounds<TConfig>(TConfig config, int? width, int? height) where TConfig : IControlConfiguration
    {
        if (width is int w && height is int h) return (w, h);

        // A window-specific override (RegisterSizing<TConfig>()) wins over the config's own default.
        if (_sizingStrategies.TryGetValue(typeof(TConfig), out var overrideSizing))
        {
            var (ow, oh) = overrideSizing(config.ControlSize);
            return (width ?? ow, height ?? oh);
        }

        // Otherwise fall back to the config's own default, if it knows how to size itself.
        if (config is ISizedControlConfiguration self)
        {
            var (sw, sh) = self.ResolveBounds();
            return (width ?? sw, height ?? sh);
        }

        throw new InvalidOperationException(
            $"Place<{typeof(TConfig).Name}>: no width/height given, {typeof(TConfig).Name} doesn't implement " +
            $"ISizedControlConfiguration, and no RegisterSizing<{typeof(TConfig).Name}>(...) override is registered. " +
            "Pass width/height explicitly instead.");
    }

    #region Private

    // Runs ConfigureSizing() once, then (re-)runs BuildLayout(), evicts cache entries the build no
    // longer referenced, applies the resulting layout, and syncs the control manager. Called from
    // AttachToParent() and IHotReloadTarget.RebuildControls().
    private void Build()
    {
        if (!_sizingConfigured)
        {
            ConfigureSizing();
            _sizingConfigured = true;
        }

        _buildIdsInProgress = [];
        _registeredPanels = [];
        _layout = [.. BuildLayout()];

        // Clean up cache entries that this build no longer used (e.g.
        // a control that was removed from BuildLayout()) - prevents an
        // unbounded growing cache with repeated RebuildControls().
        foreach (var staleId in _controlCache.Keys.Where(id => !_buildIdsInProgress.Contains(id)).ToList())
            _controlCache.Remove(staleId);
        _buildIdsInProgress = null;

        ApplyLayout();
        _controlManager.Sync(_layout.ConvertAll(e => e.Control));
        PlatformWindow.SetContinuousRepaint(_controlManager.HasContinuousRepaintControls());
    }

    // Re-binds every control in the current layout to the (possibly new) window size and bounds,
    // without recreating anything - the cheap path used by SetBounds()/OnResize() where the control
    // set itself hasn't changed.
    private void ApplyLayout()
    {
        foreach (var (control, x, y, w, h) in _layout)
        {
            control.BindInvalidate(PlatformWindow.Invalidate);
            control.BindShowMenu(OpenMenu);
            control.SetContainerBackgroundReference(_windowWidth, _windowHeight);
            control.SetBounds(x, y, w, h, Context);
        }
    }

    void IPlatformWindowHost.OnPaint(SKCanvas canvas, int width, int height)
    {
        DrawBackground(canvas, width, height);
        _controlManager.Draw(canvas, ParticipatesInDebugBoundsOverlay && Globals.ShowControlBounds);
        _contextMenu?.Draw(canvas);
    }

    void IPlatformWindowHost.OnResize(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        _contextMenu = null; // its position was computed against the old size
        ApplyLayout();
        PlatformWindow.Invalidate();
    }

    void IPlatformWindowHost.OnPointerDown(int x, int y, KeyModifiers modifiers)
    {
        if (_contextMenu is { } menu)
        {
            menu.HandleClick(x, y);
            _contextMenu = null;
            PlatformWindow.Invalidate();
            return;
        }
        _controlManager.OnPointerDown(x, y, modifiers);
    }

    void IPlatformWindowHost.OnPointerMove(int x, int y, KeyModifiers modifiers)
    {
        if (_contextMenu is { } menu)
        {
            menu.UpdateHover(x, y);
            PlatformWindow.Invalidate();
            return;
        }
        _controlManager.OnPointerMove(x, y, modifiers);
    }

    void IPlatformWindowHost.OnPointerUp(int x, int y) => _controlManager.OnPointerUp(x, y);

    void IPlatformWindowHost.OnWheel(int x, int y, int ticks, KeyModifiers modifiers)
    {
        if (_contextMenu != null) return; // ignore scroll while a menu is open
        _controlManager.OnWheel(x, y, ticks, modifiers);
    }

    void IPlatformWindowHost.OnDoubleClick(int x, int y)
    {
        if (_contextMenu != null)
        {
            _contextMenu = null;
            PlatformWindow.Invalidate();
            return;
        }
        _controlManager.OnDoubleClick(x, y);
    }

    void IPlatformWindowHost.OnContextMenu(int x, int y)
        => OpenMenu(x, y, _controlManager.FindControlAt(x, y)?.GetContextMenuItems() ?? []);

    // The one way a menu opens, whether from a right-click or from a control asking for one
    // (PluginControl.ShowMenu - a dropdown, say). An empty list closes any open menu instead.
    private void OpenMenu(int x, int y, IReadOnlyList<ContextMenuItem> items)
    {
        _contextMenu = items.Count > 0 ? new ContextMenuOverlay(x, y, items, _windowWidth, _windowHeight, Context) : null;
        PlatformWindow.Invalidate();
    }

    #endregion
}
