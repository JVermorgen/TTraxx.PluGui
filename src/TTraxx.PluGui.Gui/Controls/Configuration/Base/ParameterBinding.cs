using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

namespace TTraxx.PluGui.Gui.Controls.Configuration.Base;

/// <summary>
/// Everything a control needs to read and write one host parameter.
/// Build one per parameter and hand the same instance to any control
/// that should drive it.
/// </summary>
public sealed class ParameterBinding
{
    public required IParameterControlInfo Info { get; init; }
    public required Func<double> GetNormalizedValue { get; init; }
    public required Action BeginEdit { get; init; }
    public required Action<double> SetNormalizedValue { get; init; }
    public required Action EndEdit { get; init; }

    public double MinValue { get; init; } = 0.0;
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

    public double ToPlain(double normalized) => MinValue + ((MaxValue - MinValue) * normalized);

    /// <summary>Snaps to the nearest discrete position, or returns the value unchanged when continuous.</summary>
    public double Quantize(double normalized)
        => Info.PositionCount is int positions && positions > 1
            ? Math.Round(normalized * (positions - 1)) / (positions - 1)
            : normalized;
}