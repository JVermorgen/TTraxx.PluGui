namespace TTraxx.PluGui.Gui.Controls.Base;

/// <summary>
/// One control placed in a window's layout: the control itself plus its ABSOLUTE
/// (window-relative) bounds. This is what a window's BuildLayout() yields, and what
/// a panel's AddControl() hands back.
///
/// It exists as a named type rather than a bare tuple because it appears in every
/// derived window's BuildLayout() signature: a record struct can grow a member
/// (z-order, visibility, ...) without breaking every derived class the way widening
/// a tuple would. Deconstruction (<c>var (control, x, y, w, h) = placement;</c>) and
/// an implicit conversion from the equivalent tuple both work, so existing
/// tuple-shaped layout code keeps compiling.
/// </summary>
/// <param name="Control">The control to place.</param>
/// <param name="X">Absolute, window-relative left edge.</param>
/// <param name="Y">Absolute, window-relative top edge.</param>
/// <param name="W">Width in the same (already scaled) units as X/Y.</param>
/// <param name="H">Height in the same (already scaled) units as X/Y.</param>
public readonly record struct ControlPlacement(AbstractControlBase Control, int X, int Y, int W, int H)
{
    /// <summary>Lets a layout still be written as a plain <c>(control, x, y, w, h)</c> tuple.</summary>
    public static implicit operator ControlPlacement((AbstractControlBase Control, int X, int Y, int W, int H) placement)
        => new(placement.Control, placement.X, placement.Y, placement.W, placement.H);
}
