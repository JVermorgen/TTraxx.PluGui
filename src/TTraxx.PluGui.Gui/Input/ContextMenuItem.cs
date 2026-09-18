namespace TTraxx.PluGui.Gui;

/// <summary>
/// One row in a control's right-click context menu. See PluginControl.GetContextMenuItems, which
/// returns these; the window builds and owns the actual menu, so a control only describes the rows
/// and never draws or dismisses anything itself.
///
/// There are no submenus, separators or checkmarks - a plugin's per-control menu is a short list of
/// verbs ("Reset to Default"), and the flat shape keeps it that way.
/// </summary>
/// <param name="Label">Text shown on the row.</param>
/// <param name="Execute">
/// Runs when the row is clicked, on the UI thread. A disabled row is never invoked. The menu closes
/// either way, so this doesn't need to dismiss anything - but it should request a repaint if it
/// changed something visible.
/// </param>
/// <param name="IsEnabled">
/// When false the row is drawn greyed out, doesn't highlight on hover and can't be invoked. Evaluated
/// once, when the control builds the item - unlike a control's own enabled state, this isn't a live
/// predicate, which is fine because the list is rebuilt on every right-click.
/// </param>
public sealed record ContextMenuItem(string Label, Action Execute, bool IsEnabled = true);
