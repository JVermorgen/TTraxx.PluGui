namespace TTraxx.PluGui.Gui;

/// <summary>One row in a control's right-click context menu. See PluginControl.GetContextMenuItems.</summary>
public sealed record ContextMenuItem(string Label, Action Execute, bool IsEnabled = true);
