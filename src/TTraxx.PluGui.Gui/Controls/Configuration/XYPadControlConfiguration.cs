namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a two-dimensional pad that drives one parameter per axis from a single dragged dot -
/// for values a user thinks of as one gesture (a morph position) rather than two numbers.
/// </summary>
public class XYPadControlConfiguration() : ParameterControlConfiguration, IStyledControlConfiguration<XYPadStyle>
{
    /// <summary>Parameter driven by the dot's horizontal position (0 = left edge).</summary>
    public required ParameterBinding XParameter { get; init; }

    /// <summary>Parameter driven by the dot's vertical position (0 = bottom edge).</summary>
    public required ParameterBinding YParameter { get; init; }

    /// <summary>
    /// Opens a host-side edit group around a drag, so the two parameters this pad moves are recorded
    /// as ONE user gesture instead of two interleaved ones. Optional: without it the individual
    /// parameter begin/end brackets still fire and automation is still correct, just not grouped.
    /// </summary>
    public Action? BeginGroupEdit { get; init; }

    /// <summary>Closes the group opened by <see cref="BeginGroupEdit"/>.</summary>
    public Action? EndGroupEdit { get; init; }

    /// <summary>
    /// Optional quantizer for dragged positions: takes the normalized (X, Y) under the pointer and
    /// returns the position to store instead - to snap the dot to a grid, say. Applied before the
    /// parameters are written, so the dot always shows the value the plugin actually has. Also
    /// applied to "Reset to Default". Read on every use, so it can follow a mode parameter; null
    /// leaves the pad continuous.
    /// </summary>
    public Func<double, double, (double X, double Y)>? Snap { get; init; }

    /// <summary>
    /// Optional markers for where <see cref="Snap"/> can land, in normalized (X, Y) - read on every
    /// repaint, so they can follow a snap mode. A point inside the pad draws as a small dot, one on
    /// an edge as a tick in from the frame, and one on a corner lights that corner's icon.
    /// </summary>
    public Func<IReadOnlyList<(double X, double Y)>>? SnapPoints { get; init; }

    /// <summary>
    /// Optional segmented selector drawn inside the pad's own frame, below the pad area - for a
    /// setting that belongs to the pad (its snap mode, say). Adds <see cref="XYPadStyle.FooterHeight"/>
    /// to the control's height; the pad area itself keeps its size.
    /// </summary>
    public XYPadFooter? Footer { get; init; }

    /// <summary>Optional read-only modulation overlay - see <see cref="XYPadModulationIndicator"/>.</summary>
    public XYPadModulationIndicator? ModulationIndicator { get; init; }

    /// <summary>
    /// Optional read-only points showing where the values actually are right now - for a plugin
    /// whose engine moves them away from the dragged position (per-voice modulation, say). Read on
    /// every repaint; while it is set the pad repaints continuously. Each point leaves a short
    /// fading trail (<see cref="XYPadStyle.LiveTrailSeconds"/>), kept per <see cref="XYPadLivePoint.Id"/>.
    /// Return an empty list to show none.
    /// </summary>
    public Func<IReadOnlyList<XYPadLivePoint>>? LivePoints { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="XYPadStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<XYPadStyle>? Style { get; init; }

    /// <summary>The pad's layout box: the style's, plus the footer's band when there is one.</summary>
    public (int Width, int Height) ResolveBounds()
    {
        var style = this.ResolveStyle();
        var (width, height) = style.BoundsFor(ControlSize);
        return Footer is null ? (width, height) : (width, height + style.FooterHeight);
    }

    (int Width, int Height) ISizedControlConfiguration.ResolveBounds() => ResolveBounds();
}

/// <summary>
/// A row of choices in an XY pad's footer - see <see cref="XYPadControlConfiguration.Footer"/>. Like a
/// tab strip, not bound to a host parameter by itself: <paramref name="SetSelectedIndex"/> decides
/// what a choice does.
/// </summary>
/// <param name="Items">One label per choice, left to right.</param>
/// <param name="GetSelectedIndex">Reads the current choice.</param>
/// <param name="SetSelectedIndex">Makes a choice. The pad repaints the window itself afterwards.</param>
public sealed record XYPadFooter(IReadOnlyList<string> Items, Func<int> GetSelectedIndex, Action<int> SetSelectedIndex)
{
    /// <summary>Optional on/off switch at the footer's right end, beside the choices.</summary>
    public XYPadFooterToggle? Toggle { get; init; }
}

/// <summary>An on/off switch in an XY pad's footer - see <see cref="XYPadFooter.Toggle"/>.</summary>
/// <param name="Label">Its text.</param>
/// <param name="IsOn">Reads its state.</param>
/// <param name="SetOn">Changes its state. The pad repaints the window itself afterwards.</param>
public sealed record XYPadFooterToggle(string Label, Func<bool> IsOn, Action<bool> SetOn);

/// <summary>
/// One live point on an XY pad - see <see cref="XYPadControlConfiguration.LivePoints"/>.
/// </summary>
/// <param name="Id">
/// Identifies the point from one repaint to the next, so its trail follows it. A new id starts a
/// new trail - give a point a new one when it stands for something new (a new note, say).
/// </param>
/// <param name="X">Normalized horizontal position, 0..1.</param>
/// <param name="Y">Normalized vertical position, 0..1.</param>
/// <param name="Weight">How strongly to draw it, 0..1 - an envelope level, for instance, so it fades out with the sound.</param>
public readonly record struct XYPadLivePoint(long Id, double X, double Y, double Weight);

/// <summary>
/// Optional, read-only visualization of where a modulation source would
/// push the morph position at full deflection - no interaction of its
/// own, just a dot + connecting line drawn on top of the main dot.
/// </summary>
/// <param name="GetOffsetX">Bipolar value (-1..1) - how far the indicator deviates from the main dot on the X axis at full modulation.</param>
/// <param name="GetOffsetY">Same for the Y axis.</param>
public sealed record XYPadModulationIndicator(Func<double> GetOffsetX, Func<double> GetOffsetY);
