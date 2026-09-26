namespace TTraxx.PluGui.Gui;

/// <summary>
/// A control configuration with an overridable <typeparamref name="TStyle"/>. Implementing it is all
/// a configuration needs to do to auto-size: <see cref="ISizedControlConfiguration.ResolveBounds"/>
/// is supplied here, from the effective style's <see cref="IControlStyle{TSelf}.BoundsFor"/>.
/// </summary>
public interface IStyledControlConfiguration<TStyle> : ISizedControlConfiguration
    where TStyle : class, IControlStyle<TStyle>
{
    /// <summary>
    /// Look and feel override, or null for <typeparamref name="TStyle"/>'s default. A factory rather
    /// than an instance so the style is resolved on each use: that keeps it live under Hot Reload and
    /// lets a style be derived from the current theme.
    /// </summary>
    Func<TStyle>? Style { get; }

    (int Width, int Height) ISizedControlConfiguration.ResolveBounds()
        => StyledControlConfigurationExtensions.ResolveBounds(this);
}

/// <summary>Style resolution shared by every <see cref="IStyledControlConfiguration{TStyle}"/>.</summary>
public static class StyledControlConfigurationExtensions
{
    /// <summary>The configured style, or <typeparamref name="TStyle"/>'s default when none is set.</summary>
    public static TStyle ResolveStyle<TStyle>(this IStyledControlConfiguration<TStyle> config)
        where TStyle : class, IControlStyle<TStyle>
        => config.Style?.Invoke() ?? TStyle.Default;

    /// <summary>
    /// The control's natural layout box for its <see cref="IControlConfiguration.ControlSize"/>. The
    /// same value as <see cref="ISizedControlConfiguration.ResolveBounds"/>, callable on the concrete
    /// configuration type as well (a window laying its grid out from a knob's height, say).
    /// </summary>
    public static (int Width, int Height) ResolveBounds<TStyle>(this IStyledControlConfiguration<TStyle> config)
        where TStyle : class, IControlStyle<TStyle>
        => config.ResolveStyle().BoundsFor(config.ControlSize);
}
