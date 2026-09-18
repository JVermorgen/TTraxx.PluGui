namespace TTraxx.PluGui.Gui;

/// <summary>
/// Base implementation of <see cref="IControlConfiguration"/> - supplies the identity and the
/// two common defaults so a concrete configuration only declares what's specific to its control
/// (its parameter binding, its style, ...). Derive from <see cref="ParameterControlConfiguration"/>
/// instead when the control drives a host parameter.
/// </summary>
public abstract class ControlConfiguration : IControlConfiguration
{
    /// <inheritdoc/>
    /// <remarks>
    /// Generated per instance, which is precisely why a configuration should be constructed once
    /// and reused: see the identity note on <see cref="IControlConfiguration"/>.
    /// </remarks>
    public Guid Id { get; } = Guid.NewGuid();

    /// <inheritdoc/>
    public Func<bool> IsEnabled { get; init; } = () => true;

    /// <inheritdoc/>
    public ControlSizes ControlSize { get; init; } = ControlSizes.S;
}
