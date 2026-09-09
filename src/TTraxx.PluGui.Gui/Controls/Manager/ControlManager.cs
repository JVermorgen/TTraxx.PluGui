using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls.Manager;

internal sealed class ControlManager
{
    private readonly List<AbstractControlBase> _controls = [];
    private AbstractControlBase? _capturedControl;

    public void Add(AbstractControlBase control) => _controls.Add(control);

    public void Sync(IReadOnlyList<AbstractControlBase> desired)
    {
        if (_capturedControl != null && !desired.Contains(_capturedControl))
            _capturedControl = null;

        _controls.Clear();
        _controls.AddRange(desired);
    }

    public void Draw(SKCanvas canvas)
    {
        foreach (var control in _controls)
        {
            canvas.Save();
            canvas.Translate(control.X, control.Y);
            control.Draw(canvas);
            canvas.Restore();
        }
    }

    public bool TryFindParameter(int x, int y, out int parameterId)
    {
        var control = HitTestControls(x, y); // reuses the existing private hit-test loop
        if (control is not null)
        {
            parameterId = control.GetParameterInfo(x, y).ParameterId;
            return true;
        }
        parameterId = default;
        return false;
    }

    /// <summary>Back-to-front (reversed drawing order), so when overlapping the topmost (last drawn) control gets a hit first.</summary>
    private AbstractControlBase? HitTestControls(int x, int y)
    {
        for (var i = _controls.Count - 1; i >= 0; i--)
        {
            if (_controls[i].HitTest(x, y)) return _controls[i];
        }
        return null;
    }

    public void OnPointerDown(int x, int y)
    {
        var control = HitTestControls(x, y);
        if (control is null) return;
        _capturedControl = control;
        control.OnPointerDown(new PointerEventArgs(x - control.X, y - control.Y));
    }

    public void OnPointerMove(int x, int y)
    {
        // As long as there is a capture (dragging), everything goes to that control,
        // even outside its own HitTest boundaries — matches the behavior
        // of the old per-control SetCapture.
        var target = _capturedControl ?? HitTestControls(x, y);
        if (target is null) return;
        target.OnPointerMove(new PointerEventArgs(x - target.X, y - target.Y));
    }

    public void OnPointerUp(int x, int y)
    {
        var target = _capturedControl;
        _capturedControl = null;
        if (target is null) return;
        target.OnPointerUp(new PointerEventArgs(x - target.X, y - target.Y));
    }

    public void OnWheel(int x, int y, int delta)
    {
        var control = _capturedControl ?? HitTestControls(x, y);
        if (control is null) return;
        control.OnWheel(new WheelEventArgs(x - control.X, y - control.Y, delta));
    }

    public void OnDoubleClick(int x, int y)
    {
        var control = HitTestControls(x, y);
        if (control is null) return;
        control.OnDoubleClick(new PointerEventArgs(x - control.X, y - control.Y));
    }
}