using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls.Manager;

internal sealed class ControlManager : IDisposable
{
    private readonly List<AbstractControlBase> _controls = [];
    private AbstractControlBase? _capturedControl;
    private AbstractControlBase? _hoveredControl;

    public void Add(AbstractControlBase control) => _controls.Add(control);

    public void Sync(IReadOnlyList<AbstractControlBase> desired)
    {
        if (_capturedControl != null && !desired.Contains(_capturedControl))
            _capturedControl = null;

        if (_hoveredControl is not null && !desired.Contains(_hoveredControl))
            _hoveredControl = null;

        // Dispose controls that are being dropped, but never one that is
        // also in the new set - rebuilds commonly reuse instances.
        foreach (var control in _controls)
        {
            if (!desired.Contains(control)) control.Dispose();
        }

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
        var info = control?.GetParameterInfo(x - control.X, y - control.Y);
        if (info is not null)
        {
            parameterId = info.ParameterId;
            return true;
        }
        parameterId = default;
        return false;
    }

    /// <summary>Used by the window layer to find the right-click target for a context menu, outside the normal pointer-event flow.</summary>
    public AbstractControlBase? FindControlAt(int x, int y) => HitTestControls(x, y);

    /// <summary>Back-to-front (reversed drawing order), so when overlapping the topmost (last drawn) control gets a hit first.</summary>
    private AbstractControlBase? HitTestControls(int x, int y)
    {
        for (var i = _controls.Count - 1; i >= 0; i--)
        {
            var control = _controls[i];
            if (control.HitTest(x - control.X, y - control.Y)) return control;
        }
        return null;
    }

    private static PointerEventArgs ToLocal(AbstractControlBase control, int x, int y, KeyModifiers modifiers = KeyModifiers.None)
    => new(x - control.X, y - control.Y, modifiers);

    public void OnPointerDown(int x, int y, KeyModifiers modifiers = KeyModifiers.None)
    {
        var control = HitTestControls(x, y);
        if (control is null) return;
        _capturedControl = control;
        control.OnPointerDown(ToLocal(control, x, y, modifiers));
    }

    public void OnPointerMove(int x, int y, KeyModifiers modifiers = KeyModifiers.None)
    {
        // As long as there is a capture (dragging), everything goes to that control,
        // even outside its own HitTest boundaries — matches the behavior
        // of the old per-control SetCapture.
        var target = _capturedControl ?? HitTestControls(x, y);
        UpdateHover(_capturedControl is null ? target : null);
        if (target is null) return;
        target.OnPointerMove(ToLocal(target, x, y, modifiers));
    }

    public void OnPointerUp(int x, int y)
    {
        var target = _capturedControl;
        _capturedControl = null;
        if (target is null) return;
        target.OnPointerUp(ToLocal(target, x, y));

        // Re-evaluate hover: the pointer may have been released elsewhere.
        UpdateHover(HitTestControls(x, y));
    }

    public void OnWheel(int x, int y, int delta, KeyModifiers modifiers = KeyModifiers.None)
    {
        var control = _capturedControl ?? HitTestControls(x, y);
        if (control is null) return;
        control.OnWheel(new WheelEventArgs(x - control.X, y - control.Y, delta, modifiers));
    }

    public void OnDoubleClick(int x, int y)
    {
        var control = HitTestControls(x, y);
        if (control is null) return;
        control.OnDoubleClick(ToLocal(control, x, y));
    }

    /// <summary>Called by the window when the pointer leaves it entirely.</summary>
    public void OnPointerLeaveWindow() => UpdateHover(null);

    private void UpdateHover(AbstractControlBase? target)
    {
        if (ReferenceEquals(_hoveredControl, target)) return;

        _hoveredControl?.OnPointerLeave();
        _hoveredControl = target;
        _hoveredControl?.OnPointerEnter();
    }

    public void Dispose()
    {
        foreach (var control in _controls) control.Dispose();
        _controls.Clear();
        _capturedControl = null;
        _hoveredControl = null;
    }
}