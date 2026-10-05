namespace TTraxx.PluGui.Gui;

/// <summary>
/// One entry in an open-file dialog's type list - see PluginControl.ShowOpenFileDialog.
/// </summary>
/// <param name="Name">What the dialog shows, e.g. "Audio files".</param>
/// <param name="Extensions">The extensions it lets through, without the dot, e.g. ["wav", "aif"].</param>
public sealed record FileDialogFilter(string Name, IReadOnlyList<string> Extensions);
