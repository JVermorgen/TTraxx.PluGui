using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;
using TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls.Base;

public abstract class AbstractControlBase(IControlConfiguration config) : IDisposable
{
    protected int _x;
    protected int _y;
    protected int _w;
    protected int _h;

    protected int _containerW = 1;
    protected int _containerH = 1;

    private Action? _invalidateRequest;

    protected IControlConfiguration Config { get; } = config;

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

    /// <summary>Bounds are given in unscaled design units and rescaled here.</summary>
    public void SetBounds(int x, int y, int w, int h)
    {
        _x = Globals.Rescale(x);
        _y = Globals.Rescale(y);
        _w = Globals.Rescale(w);
        _h = Globals.Rescale(h);
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