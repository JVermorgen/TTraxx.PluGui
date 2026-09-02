namespace TTraxx.PluGui.Gui.Panels.Configuration;

/// <summary>
/// Logical (non-scaled) dimensions and title for a MetalicPanel —
/// same convention as the Control configurations: numbers here are
/// the same units as in GetLayout. MetalicPanel rescales itself via
/// Globals.Instance.Rescale when drawing.
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