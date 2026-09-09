namespace TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;

public interface IParameterControlInfo
{
    int ParameterId { get; }
    string Unit { get; }
    double DefaultNormalizedValue { get; }
    string Label { get; }

    /// <summary>
    /// Raw VST3-style step count as defined by the parameter itself:
    /// the number of steps between min and max, not the number of
    /// discrete positions. A value of 0 means continuous.
    /// Setting this converts it into the stored position count -
    /// see ParameterControlInfo for the exact rule.
    /// Read PositionCount instead if you need the actual number of
    /// discrete positions for layout/quantization math.
    /// </summary>
    int StepCount { get; init; }

    /// <summary>
    /// The number of discrete positions this parameter renders as, or
    /// null if continuous. This is StepCount already converted (VST3's
    /// StepCount + 1, since StepCount counts the gaps between positions,
    /// not the positions themselves) - use this one in drawing/hit-testing
    /// code.
    /// </summary>
    int? PositionCount { get; }
}