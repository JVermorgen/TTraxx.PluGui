namespace TTraxx.PluGui.Gui;

/// <summary>
/// Base for configurations of controls that drive host parameters - the pairing of
/// <see cref="ControlConfiguration"/>'s identity/enabled/size defaults with the
/// <see cref="IParameterControlConfiguration"/> marker, so a concrete configuration gets both
/// by deriving once. Declare the actual <see cref="ParameterBinding"/>(s) on the derived type.
/// </summary>
public abstract class ParameterControlConfiguration : ControlConfiguration, IParameterControlConfiguration;
