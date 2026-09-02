namespace TTraxx.PluGui.Gui.Helpers;

public static class ValueHelpers
{
    // Auto-converts large Hz values to kHz for a shorter, more compact
    // display (3000 Hz -> "3.0 kHz") instead of always keeping the raw unit.
    public static string Format(this double plainValue, string unit)
    {
        double displayValue = plainValue;

        if (unit == "Hz" && Math.Abs(plainValue) >= 1000)
        {
            unit = "kHz";
            displayValue = plainValue / 1000.0;
        }

        string formatter = unit switch
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

        string text = displayValue.ToString($"{formatter}");
        return unit != "" ? $"{text} {unit}" : text;
    }

    // Converts degrees to radians.
    public static double DegToRad(this double deg) => deg * Math.PI / 180.0;
}
