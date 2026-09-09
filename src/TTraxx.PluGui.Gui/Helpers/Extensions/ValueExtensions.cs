namespace TTraxx.PluGui.Gui.Helpers.Extensions;

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
