using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Base class for every control in the library: a knob, a toggle, a meter, an XY pad. A control
/// owns its runtime state (bounds, hover, whatever it tracks mid-drag) and knows how to draw and
/// respond to input; what it's bound to and how big it is come from its
/// <see cref="IControlConfiguration"/>.
///
/// A control is built once and reused across layout rebuilds - the owning window caches it by its
/// configuration's Id - so it may be re-bounded any number of times over its life, and must not
/// assume it's laid out only once.
///
/// COORDINATES: bounds (<see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>,
/// <see cref="Height"/>) are absolute, window-relative and already scaled to pixels, whereas
/// <see cref="Draw"/> and the pointer handlers work in LOCAL coordinates with (0,0) at this
/// control's top-left - the canvas and event positions are translated before they arrive. Lengths
/// a subclass introduces itself should go through <see cref="Rescale"/> so they track the
/// window's scale.
///
/// To implement one: override <see cref="Draw"/> (required), plus whichever of the input hooks it
/// reacts to. Override <see cref="GetParameterInfo"/> when it's bound to a parameter,
/// <see cref="GetContextMenuItems"/> to offer a right-click menu, <see cref="NeedsContinuousRepaint"/>
/// when it animates on its own, and <see cref="Dispose"/> when it caches Skia resources.
/// </summary>
public abstract class PluginControl(IControlConfiguration config) : IDisposable
{
    /// <summary>Absolute, window-relative left edge in pixels - already scaled. Set by <see cref="SetBounds"/>.</summary>
    protected int _x;

    /// <summary>Absolute, window-relative top edge in pixels - already scaled.</summary>
    protected int _y;

    /// <summary>Width in pixels - already scaled. The usual reference for local-coordinate drawing.</summary>
    protected int _w;

    /// <summary>Height in pixels - already scaled.</summary>
    protected int _h;

    // Size of the surface this control sits on, for controls that sample the window background
    // (a gradient, say) to blend against it. Never 0, so it's safe as a divisor.
    protected int _containerW = 1;
    protected int _containerH = 1;

    private Action? _invalidateRequest;

    // Replaced by the owning window's context in SetBounds(). The stand-in keeps an unplaced
    // control drawable at scale 1 with the default theme, rather than throwing mid-paint.
    private RenderContext _context = new();

    /// <summary>
    /// The configuration this control was built from. A subclass normally works with its own
    /// concrete configuration type instead (captured via its primary constructor) and uses this only
    /// for the shared members.
    /// </summary>
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

    /// <summary>This control's identity - its configuration's Id, which is what the window's cache keys on.</summary>
    protected Guid Id => Config.Id;

    /// <summary>Absolute, window-relative left edge in pixels.</summary>
    public int X => _x;

    /// <summary>Absolute, window-relative top edge in pixels.</summary>
    public int Y => _y;

    /// <summary>Width in pixels.</summary>
    public int Width => _w;

    /// <summary>Height in pixels.</summary>
    public int Height => _h;

    /// <summary>Absolute, window-relative bounds - what the control manager hit-tests and clips against.</summary>
    public SKRect Bounds => new(_x, _y, _x + _w, _y + _h);

    /// <summary>
    /// True while the pointer is inside this control. Maintained by the base class from
    /// <see cref="OnPointerEnter"/>/<see cref="OnPointerLeave"/>; read it from <see cref="Draw"/> for
    /// hover styling.
    /// </summary>
    protected bool IsHovered { get; private set; }

    /// <summary>
    /// Whether this control currently accepts input, re-evaluated from the configuration's predicate
    /// on every call rather than cached. Check it at the top of an input handler AND while drawing -
    /// the base class doesn't filter events on your behalf.
    /// </summary>
    protected bool IsEnabled => Config.IsEnabled();

    /// <summary>
    /// Bound by the window when the control is added, so a control can ask
    /// for a redraw without knowing anything about Win32/Cocoa/X11.
    /// </summary>
    public void BindInvalidate(Action invalidateRequest) => _invalidateRequest = invalidateRequest;

    /// <summary>
    /// Requests a repaint of the window this control belongs to. Call it after changing anything the
    /// control draws from; it's a no-op before the control has been laid out.
    /// </summary>
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

    /// <summary>
    /// Tells this control how big the surface behind it is, for controls that need to line their own
    /// fill up with the window's background (continuing a gradient across the control, say, rather
    /// than restarting it). Both values are clamped to at least 1 so they're safe to divide by.
    /// </summary>
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

    /// <summary>
    /// Whether a point (in local coordinates) counts as inside this control. The default is the full
    /// rectangle; override for a non-rectangular shape so the surrounding area stays clickable by
    /// whatever is underneath.
    /// </summary>
    public virtual bool HitTest(int localX, int localY)
        => localX >= 0 && localX < _w && localY >= 0 && localY < _h;

    /// <summary>
    /// The parameter at a point (in local coordinates), or null when this control isn't
    /// parameter-bound. A point is asked for rather than the control as a whole because one control
    /// can front several parameters - an XY pad reports a different one per axis region.
    /// </summary>
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

    /// <summary>Pointer entered this control. Sets <see cref="IsHovered"/> and repaints - call the base implementation when overriding.</summary>
    public virtual void OnPointerEnter()
    {
        IsHovered = true;
        Refresh();
    }

    /// <summary>Pointer left this control. Clears <see cref="IsHovered"/> and repaints - call the base implementation when overriding.</summary>
    public virtual void OnPointerLeave()
    {
        IsHovered = false;
        Refresh();
    }

    /// <summary>
    /// Pointer pressed inside this control. This also CAPTURES the pointer: every
    /// <see cref="OnPointerMove"/> up to the matching <see cref="OnPointerUp"/> is delivered here even
    /// once it leaves this control's bounds, so a drag isn't cut short by the user overshooting.
    /// Start a drag gesture here (including the parameter's BeginEdit).
    /// </summary>
    public virtual void OnPointerDown(PointerEventArgs e) { }

    /// <summary>
    /// Pointer moved, either over this control or anywhere at all while it holds the capture from
    /// <see cref="OnPointerDown"/>. BECAUSE OF THAT CAPTURE, the coordinates may be negative or past
    /// this control's width/height mid-drag - useful for relative drags, but never assume they're
    /// inside the control.
    /// </summary>
    public virtual void OnPointerMove(PointerEventArgs e) { }

    /// <summary>
    /// Pointer released, ending the capture. Only ever delivered to the control that took it in
    /// <see cref="OnPointerDown"/>, so it's the reliable place to close a gesture (the parameter's
    /// EndEdit) - and it arrives wherever the release happened, in or out of bounds.
    /// </summary>
    public virtual void OnPointerUp(PointerEventArgs e) { }

    /// <summary>
    /// Wheel turned over this control. <see cref="WheelEventArgs.Delta"/> counts notches (positive =
    /// away from the user), already normalized across platforms.
    /// </summary>
    public virtual void OnWheel(WheelEventArgs e) { }

    /// <summary>Double-click inside this control - conventionally "reset to default".</summary>
    public virtual void OnDoubleClick(PointerEventArgs e) { }

    /// <summary>Override to release cached Skia resources (SKShader, SKPicture, ...).</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
