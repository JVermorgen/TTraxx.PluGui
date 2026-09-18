namespace TTraxx.PluGui.Gui;

/// <summary>
/// Everything a control needs to read and write one host parameter.
/// Build one per parameter and hand the same instance to any control
/// that should drive it.
///
/// Values cross this boundary NORMALIZED (0..1), never in musical units: the host stores
/// normalized values, and mapping to Hz/dB/ms is the plugin's own business. The binding carries
/// <see cref="MinValue"/>/<see cref="MaxValue"/> purely so it can format a value for display -
/// see <see cref="ToPlain"/>.
///
/// Edits are bracketed by <see cref="BeginEdit"/>/<see cref="EndEdit"/> because a host records a
/// drag as ONE automation gesture: without the brackets, a drag writes a stream of unrelated
/// value changes and automation recording/undo come out wrong. Use <see cref="Edit"/> for a
/// one-shot change and the explicit triplet for a drag (begin on pointer-down, set per move, end
/// on pointer-up).
/// </summary>
public sealed class ParameterBinding
{
    /// <summary>The parameter's static facts (id, label, unit, default, step count).</summary>
    public required IParameterControlInfo Info { get; init; }

    /// <summary>Reads the parameter's current normalized (0..1) value from the model.</summary>
    public required Func<double> GetNormalizedValue { get; init; }

    /// <summary>Opens an edit gesture - call once before the first <see cref="SetNormalizedValue"/> of a drag.</summary>
    public required Action BeginEdit { get; init; }

    /// <summary>Writes a normalized (0..1) value. Only valid between <see cref="BeginEdit"/> and <see cref="EndEdit"/>.</summary>
    public required Action<double> SetNormalizedValue { get; init; }

    /// <summary>Closes the edit gesture opened by <see cref="BeginEdit"/>.</summary>
    public required Action EndEdit { get; init; }

    /// <summary>Plain-unit value that normalized 0.0 maps to, used for display formatting only.</summary>
    public double MinValue { get; init; } = 0.0;

    /// <summary>Plain-unit value that normalized 1.0 maps to, used for display formatting only.</summary>
    public double MaxValue { get; init; } = 1.0;

    /// <summary>Overrides the unit-based default formatting when set.</summary>
    public Func<double, string>? ValueFormatter { get; init; }

    /// <summary>Current value, clamped to 0..1.</summary>
    public double Normalized => Math.Clamp(GetNormalizedValue(), 0.0, 1.0);

    /// <summary>Convenience for the very common begin/set/end triplet - for discrete edits (click, wheel, reset).</summary>
    public void Edit(double normalized)
    {
        BeginEdit();
        SetNormalizedValue(normalized);
        EndEdit();
    }

    /// <summary>
    /// Maps a normalized (0..1) value onto the <see cref="MinValue"/>..<see cref="MaxValue"/> range
    /// for display. Linear on purpose: a parameter with a musical curve (a frequency, say) applies
    /// that curve in the DSP layer, and a readout derived from the raw range would disagree with it -
    /// supply a <see cref="ValueFormatter"/> in that case.
    /// </summary>
    public double ToPlain(double normalized) => MinValue + ((MaxValue - MinValue) * normalized);

    /// <summary>Snaps to the nearest discrete position, or returns the value unchanged when continuous.</summary>
    public double Quantize(double normalized)
        => Info.PositionCount is int positions && positions > 1
            ? Math.Round(normalized * (positions - 1)) / (positions - 1)
            : normalized;
}
