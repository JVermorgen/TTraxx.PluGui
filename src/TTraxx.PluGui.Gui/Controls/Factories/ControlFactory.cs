namespace TTraxx.PluGui.Gui.Controls.Factories;

public static class ControlFactory
{
    /// <summary>
    /// Creates a control of the appropriate type for the given configuration.
    /// </summary>
    public static PluginControl Create(IControlConfiguration config) => config switch
    {
        KnobControlConfiguration knobConfig => new KnobControl(knobConfig),
        ToggleControlConfiguration toggleConfig => new ToggleControl(toggleConfig),
        XYPadControlConfiguration xyPadConfig => new XYPadControl(xyPadConfig),
        MeterControlConfiguration meterConfig => new MeterControl(meterConfig),
        _ => throw new ArgumentException("Unsupported control configuration type", nameof(config)),
    };
}