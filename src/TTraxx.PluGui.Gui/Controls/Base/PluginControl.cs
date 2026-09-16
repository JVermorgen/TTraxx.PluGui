using SkiaSharp;

namespace TTraxx.PluGui.Gui;

public abstract class PluginControl(IControlConfiguration config) : IDisposable
{
    protected int _x;
    protected int _y;
    protected int _w;
    protected int _h;

    protected int _containerW = 1;
    protected int _containerH = 1;

    private Action? _invalidateRequest;

    // Replaced by the owning window's context in SetBounds(). The stand-in keeps an unplaced
    // control drawable at scale 1 with the default theme, rather than throwing mid-paint.
    private RenderContext _context = new();

    protected IControlConfiguration Config { get; } = config;

    /// <summary>
    /// Scale, theme and fonts of the window this control belongs to. Assigned when the window lays
    /// the control out; see <see cref="RenderContext"/> for why this isn't global state.
    /// </summary>
    protected RenderContext Context => _context;

    /// <summary>Scales a design-unit length to whole pixels, at this control's window scale.</summary>
    protected int Rescale(float designUnits) => _context.Rescale(designUnits);

    /// <summary>Scales a design-unit length without rounding, at this control's window scale.</summary>
    protected float RescaleExact(float designUnits) => _context.RescaleExact(designUnits);

    /// <summary>This control's window theme.</summary>
    protected IPluginTheme Theme => _context.Theme;

    /// <summary>The metallic-panel colors of this control's window theme.</summary>
    protected IMetallicPanelTheme MetallicTheme => _context.MetallicTheme;

    /// <summary>This control's window fonts.</summary>
    protected IPluginFonts Fonts => _context.Fonts;

    protected Guid Id => Config.Id;
    public int X => _x;
    public int Y => _y;
    public int Width => _w;
    public int Height => _h;
    public SKRect Bounds => new(_x, _y, _x + _w, _y + _h);

    protected bool IsHovered { get; private set; }

    protected bool IsEnabled => Config.IsEnabled();

    /// <summary>
    /// Bound by the window when the control is added, so a control can ask
    /// for a redraw without knowing anything about Win32/Cocoa/X11.
    /// </summary>
    public void BindInvalidate(Action invalidateRequest) => _invalidateRequest = invalidateRequest;

    public void Refresh() => _invalidateRequest?.Invoke();

    /// <summary>
    /// Bounds are given in unscaled design units and rescaled here. <paramref name="context"/> is the
    /// owning window's - it's adopted by this control and used for every later scale/theme/font
    /// lookup, including from HitTest and pointer handlers, so a window re-applies its layout after
    /// the host changes scale.
    /// </summary>
    public void SetBounds(int x, int y, int w, int h, RenderContext context)
    {
        _context = context;

        _x = context.Rescale(x);
        _y = context.Rescale(y);
        _w = context.Rescale(w);
        _h = context.Rescale(h);
    }

    public void SetContainerBackgroundReference(int containerW, int containerH)
    {
        _containerW = Math.Max(1, containerW);
        _containerH = Math.Max(1, containerH);
        Refresh();
    }

    /// <summary>
    /// Draws in local coordinates (0,0 = top-left corner of this control).
    /// The canvas is already translated by ControlManager.Draw
    /// </summary>
    public abstract void Draw(SKCanvas canvas);

    public virtual bool HitTest(int localX, int localY)
        => localX >= 0 && localX < _w && localY >= 0 && localY < _h;

    public virtual IParameterControlInfo? GetParameterInfo(int localX, int localY) => null;

    /// <summary>Override to offer right-click menu items (e.g. "Reset to Default"). Empty by default - no menu shown.</summary>
    public virtual IReadOnlyList<ContextMenuItem> GetContextMenuItems() => [];

    /// <summary>
    /// True for controls that animate on their own (e.g. a level meter) and
    /// therefore need the window to keep repainting even when nothing else
    /// changed. False by default - most controls only need a redraw in
    /// response to an actual interaction or parameter change.
    /// </summary>
    public virtual bool NeedsContinuousRepaint => false;

    public virtual void OnPointerEnter()
    {
        IsHovered = true;
        Refresh();
    }
    public virtual void OnPointerLeave()
    {
        IsHovered = false;
        Refresh();
    }

    public virtual void OnPointerDown(PointerEventArgs e) { }
    public virtual void OnPointerMove(PointerEventArgs e) { }
    public virtual void OnPointerUp(PointerEventArgs e) { }
    public virtual void OnWheel(WheelEventArgs e) { }
    public virtual void OnDoubleClick(PointerEventArgs e) { }

    /// <summary>Override to release cached Skia resources (SKShader, SKPicture, ...).</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
