namespace TTraxx.PluGui.Gui;

/// <summary>
/// Default formatting of a plain (already de-normalized) parameter value for on-screen readout.
/// The rules are keyed off the unit string from ParameterControlInfo.Unit, so a parameter gets a
/// sensible readout with no per-parameter formatting code - decimals where they carry information
/// ("0.8 s") and none where they'd be noise ("440 Hz").
///
/// Supply ParameterBinding.ValueFormatter to override this for one parameter.
/// </summary>
public static class ValueExtensions
{
    // Auto-converts large Hz values to kHz for a shorter, more compact
    // display (3000 Hz -> "3.0 kHz") instead of always keeping the raw unit.
    internal static string Format(this double plainValue, string unit)
    {
        var displayValue = plainValue;

        if (unit == "Hz" && Math.Abs(plainValue) >= 1000)
        {
            unit = "kHz";
            displayValue = plainValue / 1000.0;
        }

        var formatter = unit switch
        {
            "Hz" => "F0",
            "kHz" => "0.#",
            "dB" => "F0",
            "ms" => "F0",
            "s" => "0.#",
            "%" => "F0",
            "oct" => "F0",
            _ => "0.##"
        };

        var text = displayValue.ToString($"{formatter}");
        return unit != "" ? $"{text} {unit}" : text;
    }
}
