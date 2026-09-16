namespace TTraxx.PluGui.Gui;

/// <summary>
/// Logical (non-scaled) dimensions and title for a plugin panel —
/// same convention as the control configurations: numbers here are
/// the same design units as in BuildLayout(). The panel rescales them
/// itself, at its window's RenderContext scale, when drawing.
/// </summary>
public sealed class PluginPanelConfiguration
{
    public required int Left { get; init; }
    public required int Top { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public int CornerRadius { get; init; } = 8;
    public required string Title { get; init; }
}
