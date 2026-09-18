namespace TTraxx.PluGui.Gui;

/// <summary>
/// Marks a configuration whose control drives one or more host parameters (a knob, a toggle, an
/// XY pad) as opposed to a display-only one (a meter). It adds no members of its own: the
/// parameter bindings themselves stay on the concrete configuration, because their SHAPE differs
/// - one binding for a knob, two for an XY pad - and forcing that into a common member would
/// only fit the single-parameter case.
///
/// Its use is letting host-side and tooling code ask "is this control automatable?" without
/// naming every concrete configuration type.
/// </summary>
public interface IParameterControlConfiguration : IControlConfiguration;
