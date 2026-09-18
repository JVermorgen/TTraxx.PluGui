namespace TTraxx.PluGui.Gui;

/// <summary>
/// The minimum every control configuration provides: a stable identity, an enabled-state
/// predicate and a size bucket. A configuration is the declarative description of a control
/// (what it's bound to, how big, how it's styled); the <see cref="PluginControl"/> built from
/// it holds the runtime state (bounds, hover, drag).
///
/// IDENTITY MATTERS: <see cref="Id"/> is what a window's control cache keys on, so a
/// configuration instance is meant to be created ONCE and kept (typically in a section/
/// definitions object the window reads from), not rebuilt inside BuildLayout(). Handing
/// BuildLayout() a fresh configuration each time yields a fresh Id each time, which defeats
/// the cache: the control is recreated and loses its state.
/// </summary>
public interface IControlConfiguration
{
    /// <summary>
    /// Stable, per-configuration identity used by the window's control cache to decide whether a
    /// control already exists. Assigned once on construction - see the note on this interface
    /// about keeping configuration instances alive.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Evaluated on every draw and interaction rather than stored, so a control greys out and stops
    /// responding the moment whatever it depends on changes - no explicit invalidation needed.
    /// Defaults to always-enabled.
    /// </summary>
    Func<bool> IsEnabled { get; init; }

    /// <summary>
    /// Size bucket this control renders at. Resolved to actual pixel bounds through the
    /// configuration's style (see <see cref="ISizedControlConfiguration"/>), not used as a
    /// measurement itself.
    /// </summary>
    ControlSizes ControlSize { get; init; }
}
