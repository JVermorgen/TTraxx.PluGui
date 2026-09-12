namespace TTraxx.PluGui.Gui.Input;

/// <summary>One row in a control's right-click context menu. See AbstractControlBase.GetContextMenuItems.</summary>
public sealed record ContextMenuItem(string Label, Action Execute, bool IsEnabled = true);
