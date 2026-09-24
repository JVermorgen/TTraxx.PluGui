using SkiaSharp;

namespace TTraxx.PluGui.Gui.Controls.Manager;

/// <summary>
/// Holds a window's current control set and does the work that needs to see ALL of them at once:
/// drawing them in order, hit-testing, tracking which one is hovered, and routing pointer events
/// to the right one. Splitting this out is what keeps <see cref="PluginWindow"/> about layout and
/// lifecycle rather than input bookkeeping.
///
/// POINTER CAPTURE: a press captures the control under the pointer, and every move and the release
/// go to it until then - even outside its bounds - so overshooting a drag doesn't drop it. Hover is
/// suppressed for the duration, since the pointer "belongs" to the dragged control.
///
/// Coordinates arriving here are window-relative; controls are given local ones.
/// </summary>
internal sealed class ControlManager : IDisposable
{
    // Fixed, theme-independent color: outlines must stay visible regardless
    // of whatever the current control/theme happens to be drawing.
    private static readonly SKPaint DebugBoundsPaint = new()
    {
        Color = new SKColor(255, 0, 255),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1,
        IsAntialias = false // crisp 1px outline, no half-pixel blur
    };

    private readonly List<PluginControl> _controls = [];
    private PluginControl? _capturedControl;
    private PluginControl? _hoveredControl;

    public void Add(PluginControl control) => _controls.Add(control);

    public void Sync(IReadOnlyList<PluginControl> desired)
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

    /// <summary>showDebugBounds is decided by the caller (see PluginWindow.ParticipatesInDebugBoundsOverlay) rather than read from Globals here, so a window can opt out regardless of the global flag.</summary>
    public void Draw(SKCanvas canvas, bool showDebugBounds)
    {
        foreach (var control in _controls)
        {
            if (!control.IsVisible) continue;

            canvas.Save();
            canvas.Translate(control.X, control.Y);
            control.Draw(canvas);
            if (showDebugBounds)
                canvas.DrawRect(0, 0, control.Width, control.Height, DebugBoundsPaint);
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
    public PluginControl? FindControlAt(int x, int y) => HitTestControls(x, y);

    /// <summary>True when any current control (e.g. a level meter) needs the window to keep repainting on its own.</summary>
    public bool HasContinuousRepaintControls()
    {
        foreach (var control in _controls)
        {
            if (control.NeedsContinuousRepaint) return true;
        }
        return false;
    }

    /// <summary>
    /// Back-to-front (reversed drawing order), so when overlapping the topmost (last drawn) control gets
    /// a hit first. Hidden controls are skipped: controls on the inactive page of a paged panel share
    /// their space with the visible ones, and must not intercept the pointer from underneath them.
    /// </summary>
    private PluginControl? HitTestControls(int x, int y)
    {
        for (var i = _controls.Count - 1; i >= 0; i--)
        {
            var control = _controls[i];
            if (control.IsVisible && control.HitTest(x - control.X, y - control.Y)) return control;
        }
        return null;
    }

    private static PointerEventArgs ToLocal(PluginControl control, int x, int y, KeyModifiers modifiers = KeyModifiers.None)
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

    private void UpdateHover(PluginControl? target)
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
