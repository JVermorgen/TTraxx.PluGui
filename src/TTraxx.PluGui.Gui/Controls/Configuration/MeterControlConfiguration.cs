
namespace TTraxx.PluGui.Gui;

public sealed class MeterControlConfiguration() : ControlConfiguration, ISizedControlConfiguration
{
    /// <summary>Returns the current level, normalized so that 1.0 is the meter's nominal "0 dB"/full-scale mark. May momentarily exceed 1.0 to show clipping.</summary>
    public required Func<double> GetLevel { get; init; }

    /// <summary>Optional label drawn under the meter (e.g. "Output").</summary>
    public string? Label { get; init; }

    public Func<MeterStyle>? Style { get; init; }

    public (int Width, int Height) ResolveBounds() => (Style?.Invoke() ?? MeterStyle.Default).Bounds[ControlSize];
}
