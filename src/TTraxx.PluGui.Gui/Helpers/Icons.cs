using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Built-in vector icons, chiefly the waveform glyphs a synth UI needs.
///
/// COORDINATE SPACE: every path is built in a normalized box from -0.5 to +0.5 on both axes,
/// centered on the origin, with Y pointing DOWN as in Skia. That's what makes them scale-free -
/// the caller multiplies by whatever size it needs (see <see cref="SkiaIconExtensions"/>) instead
/// of the icon carrying a baked-in size. Draw one through those extensions rather than passing the
/// path to Skia directly, or it renders as a sub-pixel speck at the origin.
///
/// Paths are built once on first use and cached. Treat them as READ-ONLY: they're shared across
/// every window in the process, so transform a copy (or transform the canvas) rather than the path
/// itself.
/// </summary>
public static class Icons
{
    private static volatile SKPath? _arrowRight;
    private static volatile SKPath? _sine;
    private static volatile SKPath? _saw;
    private static volatile SKPath? _pulse;
    private static volatile SKPath? _triangle;

    /// <summary>Right-pointing triangle with a notched back edge - a play/next marker. Filled.</summary>
    public static SKPath ArrowRight => _arrowRight ??= BuildArrowRight();

    /// <summary>One full sine cycle, polyline-approximated. Meant to be STROKED, not filled.</summary>
    public static SKPath Sine => _sine ??= BuildSine();

    /// <summary>Two sawtooth ramps. Meant to be STROKED.</summary>
    public static SKPath Saw => _saw ??= BuildSaw();

    /// <summary>A square/pulse edge profile. Meant to be STROKED.</summary>
    public static SKPath Pulse => _pulse ??= BuildPulse();

    /// <summary>A triangle wave profile. Meant to be STROKED.</summary>
    public static SKPath Triangle => _triangle ??= BuildTriangle();

    internal static void InvalidateCache()
    {
        // Deliberately not disposing: a draw on the UI thread may still
        // hold a reference, and Hot Reload callbacks don't arrive on it.
        _arrowRight = null;
        _sine = null;
        _saw = null;
        _pulse = null;
        _triangle = null;
    }

    #region Private Methods
    private static SKPath BuildArrowRight()
    {
        using SKPathBuilder builder = new();
        builder.MoveTo(-0.6f, -0.5f);
        builder.LineTo(0.6f, 0f);
        builder.LineTo(-0.6f, 0.5f);
        builder.LineTo(-0.3f, 0f);
        builder.Close();
        return builder.Detach();
    }

    private static SKPath BuildSine()
    {
        using SKPathBuilder builder = new();
        const int segments = 20;
        for (var i = 0; i <= segments; i++)
        {
            var t = (float)i / segments;
            var nx = t - 0.5f;
            var ny = -(float)Math.Sin(t * 2 * Math.PI) / 2f;
            if (i == 0) builder.MoveTo(nx, ny);
            else builder.LineTo(nx, ny);
        }
        return builder.Detach();
    }

    private static SKPath BuildSaw()
    {
        using SKPathBuilder builder = new();
        builder.MoveTo(-0.5f, 0.5f);
        builder.LineTo(-0.05f, -0.5f);
        builder.LineTo(0f, 0.5f);
        builder.LineTo(0.45f, -0.5f);
        builder.LineTo(0.5f, 0.5f);
        return builder.Detach();
    }

    private static SKPath BuildPulse()
    {
        using SKPathBuilder builder = new();
        builder.MoveTo(-0.5f, 0.5f);
        builder.LineTo(-0.5f, -0.5f);
        builder.LineTo(-0.1f, -0.5f);
        builder.LineTo(-0.1f, 0.5f);
        builder.LineTo(0.5f, 0.5f);
        return builder.Detach();
    }

    private static SKPath BuildTriangle()
    {
        using SKPathBuilder builder = new();
        builder.MoveTo(-0.5f, 0.5f);
        builder.LineTo(-0.25f, -0.5f);
        builder.LineTo(0.25f, 0.5f);
        builder.LineTo(0.5f, -0.5f);
        return builder.Detach();
    }
    #endregion
}
