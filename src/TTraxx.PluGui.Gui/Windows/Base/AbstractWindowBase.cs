using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Controls.Manager;
using TTraxx.PluGui.Gui.Windows.Factories;
using TTraxx.PluGui.Gui.Windows.Interfaces;

namespace TTraxx.PluGui.Gui.Windows.Base;

public abstract class AbstractWindowBase : IPlatformWindowHost
{
    protected int _windowWidth;
    protected int _windowHeight;

    private readonly Dictionary<Guid, AbstractControlBase> _controlCache = [];

    private HashSet<Guid>? _buildIdsInProgress;
    private List<(AbstractControlBase Control, int X, int Y, int W, int H)> _layout = [];

    internal IPlatformWindow PlatformWindow { get; } = PlatformWindowFactory.Create();
    private readonly ControlManager _controlManager = new();

    public bool AttachToParent(nint parentHandle, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        if (!PlatformWindow.Attach(parentHandle, width, height, this)) return false;

        Build();
        return true;
    }

    public void SetBounds(int x, int y, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        PlatformWindow.SetBounds(x, y, width, height);
        ApplyLayout();
        PlatformWindow.Invalidate();
    }

    void IPlatformWindowHost.OnPaint(SKCanvas canvas, int width, int height)
    {
        DrawBackground(canvas, width, height);
        _controlManager.Draw(canvas);
    }

    void IPlatformWindowHost.OnResize(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        ApplyLayout();
        PlatformWindow.Invalidate();
    }

    void IPlatformWindowHost.OnPointerDown(int x, int y) => _controlManager.OnPointerDown(x, y);
    void IPlatformWindowHost.OnPointerMove(int x, int y) => _controlManager.OnPointerMove(x, y);
    void IPlatformWindowHost.OnPointerUp(int x, int y) => _controlManager.OnPointerUp(x, y);
    void IPlatformWindowHost.OnWheel(int x, int y, int ticks) => _controlManager.OnWheel(x, y, ticks);
    void IPlatformWindowHost.OnDoubleClick(int x, int y) => _controlManager.OnDoubleClick(x, y);

    protected abstract void DrawBackground(SKCanvas canvas, int width, int height);
    protected abstract IEnumerable<(AbstractControlBase Control, int X, int Y, int W, int H)> BuildLayout();

    public void RefreshUI() => PlatformWindow.Invalidate();
    public bool TryFindParameter(int x, int y, out int parameterId) => _controlManager.TryFindParameter(x, y, out parameterId);
    public virtual void Destroy() => PlatformWindow.Destroy();

    /// <summary>
    /// Harness-only: calls BuildLayout() again (Hot Reload of
    /// positions, panels, or new/removed controls) and synchronizes
    /// the control-manager. A production host never calls this.
    /// </summary>
    public void RebuildControls() => Build();

    private void Build()
    {
        _buildIdsInProgress = [];
        _layout = [.. BuildLayout()];

        // Clean up cache entries that this build no longer used (e.g.
        // a control that was removed from BuildLayout()) - prevents an
        // unbounded growing cache with repeated RebuildControls().
        foreach (var staleId in _controlCache.Keys.Where(id => !_buildIdsInProgress.Contains(id)).ToList())
            _controlCache.Remove(staleId);
        _buildIdsInProgress = null;

        ApplyLayout();
        _controlManager.Sync(_layout.ConvertAll(e => e.Control));
    }

    private void ApplyLayout()
    {
        foreach (var (control, x, y, w, h) in _layout)
        {
            control.BindInvalidate(PlatformWindow.Invalidate);
            control.SetContainerBackgroundReference(_windowWidth, _windowHeight);
            control.SetBounds(x, y, w, h);
        }
    }

    protected TControl GetOrCreate<TControl>(Guid configId, Func<TControl> factory) where TControl : AbstractControlBase
    {
        _buildIdsInProgress?.Add(configId);

        if (_controlCache.TryGetValue(configId, out var existing) && existing is TControl typed)
            return typed;

        var created = factory();
        _controlCache[configId] = created;
        return created;
    }

    /// <summary>Initial DPI/content scale for the current platform (see IPlatformWindow.GetInitialScaleFactor).</summary>
    public float GetInitialScaleFactor(nint parentHandle) => PlatformWindow.GetInitialScaleFactor(parentHandle);

    /// <summary>Non-null when this platform requires an external event pump (see IEventPumpSource).</summary>
    public IEventPumpSource? EventPumpSource => PlatformWindow.EventPumpSource;
}