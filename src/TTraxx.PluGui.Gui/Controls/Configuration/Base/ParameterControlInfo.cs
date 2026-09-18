namespace TTraxx.PluGui.Gui;

/// <summary>
/// The static facts about one host parameter - what it's called, what unit it reads in, where its
/// default sits and whether it's continuous or stepped. Paired with the accessor delegates in
/// <see cref="ParameterBinding"/>, which supplies the moving part (the current value).
///
/// This deliberately mirrors the VST3 parameter info the host already has, so an adapter can
/// populate it straight from the plugin's own parameter declaration rather than duplicating
/// those facts in the GUI layer.
/// </summary>
public class ParameterControlInfo() : IParameterControlInfo
{
    /// <summary>Host-side parameter id, used to route automation and to answer "what parameter is under this point".</summary>
    public required int ParameterId { get; init; }

    /// <summary>Short display name drawn under or beside the control.</summary>
    public required string Label { get; init; }

    /// <summary>Normalized (0..1) value a "Reset to Default" gesture returns to.</summary>
    public required double DefaultNormalizedValue { get; init; }

    /// <summary>
    /// Unit suffix for formatted values ("Hz", "dB", "ms", "%", ...), or an empty string for a bare
    /// number. Drives the default formatting - including the automatic Hz-to-kHz switch.
    /// </summary>
    public required string Unit { get; init; }

    /// <inheritdoc/>
    public int StepCount { get; init; }

    /// <inheritdoc/>
    public int? PositionCount => StepCount is int steps && steps != 0 ? steps + 1 : null;
}
