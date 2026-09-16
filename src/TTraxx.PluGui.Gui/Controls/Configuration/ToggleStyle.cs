namespace TTraxx.PluGui.Gui;

public sealed record ToggleStyle
{
    public static ToggleStyle Default => new();

    /// <summary>A toggle's full layout box per ControlSizes tier. Only S is defined so far - no control uses another tier yet.</summary>
    private static IReadOnlyDictionary<ControlSizes, (int Width, int Height)> DefaultBounds
        => new Dictionary<ControlSizes, (int Width, int Height)>
        {
            { ControlSizes.S, (58, 94) }
        };

    public IReadOnlyDictionary<ControlSizes, (int Width, int Height)> Bounds { get; init; } = DefaultBounds;
}
