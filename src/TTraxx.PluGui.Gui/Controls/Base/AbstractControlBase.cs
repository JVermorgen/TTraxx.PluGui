using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;
using TTraxx.PluGui.Gui.Controls.Configuration.Interfaces;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls.Base;

public abstract class AbstractControlBase(IParameterControlConfiguration config)
{
    protected int _x;
    protected int _y;
    protected int _w;
    protected int _h;

    protected int _containerW = 1;
    protected int _containerH = 1;

    protected Guid _id = config.Id;

    public int X => _x;
    public int Y => _y;

    private Action? _invalidateRequest;

    /// <summary>Bound by PluginEditorWindow when adding this control, so the control can request a redraw without knowing anything about Win32/Cocoa/X11 itself.</summary>
    public void BindInvalidate(Action invalidateRequest) => _invalidateRequest = invalidateRequest;

    public void Refresh() => _invalidateRequest?.Invoke();

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

    /// <summary>Draws in local coordinates (0,0 = top-left corner of this control). The canvas is already translated by ControlManager.Draw, so this remains identical to the old DrawSkia content.</summary>
    public abstract void Draw(SKCanvas canvas);

    /// <summary>Takes GLOBAL (window-relative) coordinates — unchanged from how HitTest already worked.</summary>
    public abstract bool HitTest(int x, int y);

    public abstract IParameterControlInfo GetParameterInfo(int xPos, int yPos);

    /// <summary>Takes LOCAL (control-relative) coordinates — same reference frame as the old WM_MOUSEMOVE lParam within its own HWND.</summary>
    public virtual void OnPointerDown(PointerEventArgs e) { }
    public virtual void OnPointerMove(PointerEventArgs e) { }
    public virtual void OnPointerUp(PointerEventArgs e) { }
    public virtual void OnWheel(WheelEventArgs e) { }
    public virtual void OnDoubleClick(PointerEventArgs e) { }
}