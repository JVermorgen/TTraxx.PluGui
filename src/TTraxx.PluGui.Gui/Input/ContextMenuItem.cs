namespace TTraxx.PluGui.Gui;

/// <summary>
/// One row in a popup menu: a control's right-click menu (see PluginControl.GetContextMenuItems) or
/// the list a picker opens (see PluginControl.ShowMenu). The window builds and owns the actual menu,
/// so a control only describes the rows and never draws or dismisses anything itself.
///
/// There are no submenus or separators - a plugin's per-control menu is a short list of verbs
/// ("Reset to Default") or choices, and the flat shape keeps it that way.
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
/// predicate, which is fine because the list is rebuilt every time the menu opens.
/// </param>
/// <param name="IsChecked">
/// Marks the row as the current choice - for a picker's list, where the menu should show what is
/// selected now. Plain verb menus leave it false.
/// </param>
public sealed record ContextMenuItem(string Label, Action Execute, bool IsEnabled = true, bool IsChecked = false);
