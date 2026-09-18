namespace TTraxx.PluGui.Gui;

/// <summary>
/// Read-only view of one host parameter's static facts, as controls and context menus consume
/// them. See <see cref="ParameterControlInfo"/> for the implementation a plugin populates.
/// </summary>
public interface IParameterControlInfo
{
    /// <summary>Host-side parameter id, used to route automation and hit-test results.</summary>
    int ParameterId { get; }

    /// <summary>Unit suffix for formatted values ("Hz", "dB", ...), or an empty string for a bare number.</summary>
    string Unit { get; }

    /// <summary>Normalized (0..1) value a "Reset to Default" gesture returns to.</summary>
    double DefaultNormalizedValue { get; }

    /// <summary>Short display name drawn under or beside the control.</summary>
    string Label { get; }

    /// <summary>
    /// Raw VST3-style step count as defined by the parameter itself:
    /// the number of steps between min and max, not the number of
    /// discrete positions. A value of 0 means continuous.
    /// Read <see cref="PositionCount"/> instead if you need the actual
    /// number of discrete positions for layout/quantization math.
    /// </summary>
    int StepCount { get; init; }

    /// <summary>
    /// The number of discrete positions this parameter renders as, or
    /// null if continuous. This is <see cref="StepCount"/> converted on
    /// read (VST3's StepCount + 1, since StepCount counts the gaps between
    /// positions, not the positions themselves) - use this one in
    /// drawing/hit-testing code.
    /// </summary>
    int? PositionCount { get; }
}
