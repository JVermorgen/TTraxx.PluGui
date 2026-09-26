namespace TTraxx.PluGui.Gui;

/// <summary>
/// What every built-in style provides, so the style plumbing (resolving the configured style or the
/// default, sizing a control from it) is written once in
/// <see cref="StyledControlConfigurationExtensions"/> instead of per control.
/// </summary>
public interface IControlStyle<TSelf> where TSelf : class, IControlStyle<TSelf>
{
    /// <summary>The style a control uses when its configuration doesn't set one.</summary>
    static abstract TSelf Default { get; }

    /// <summary>
    /// A control's full layout box at <paramref name="size"/>, in unscaled design units - typically
    /// read from the style's <see cref="SizeTable{T}"/>.
    /// </summary>
    (int Width, int Height) BoundsFor(ControlSizes size);
}
