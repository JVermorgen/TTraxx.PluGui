namespace TTraxx.PluGui.Gui;

/// <summary>
/// Logical (non-scaled) dimensions and title for a plugin panel —
/// same convention as the control configurations: numbers here are
/// the same design units as in BuildLayout(). The panel rescales them
/// itself, at its window's RenderContext scale, when drawing.
///
/// The rectangle described here is the panel BODY. A panel style may draw its title outside that
/// rectangle - <see cref="MetallicPluginPanel"/> puts its header tab in the ~24 design units ABOVE
/// <see cref="Top"/> - so leave room above the body rather than butting it against whatever sits
/// overhead. Child coordinates are relative to the body's top-left, not the header's.
/// </summary>
public sealed class PluginPanelConfiguration
{
    /// <summary>Left edge of the panel body, relative to the window.</summary>
    public required int Left { get; init; }

    /// <summary>
    /// Top edge of the panel BODY, relative to the window - not the top of the title header, which a
    /// style may draw above this. See the note on this type.
    /// </summary>
    public required int Top { get; init; }

    /// <summary>Width of the panel body.</summary>
    public required int Width { get; init; }

    /// <summary>Height of the panel body, excluding any header drawn above <see cref="Top"/>.</summary>
    public required int Height { get; init; }

    /// <summary>Corner rounding of the panel body.</summary>
    public int CornerRadius { get; init; } = 8;

    /// <summary>
    /// Text drawn in the panel's header. Required, so a panel is always labelled - pass an empty
    /// string for a deliberately untitled frame.
    /// </summary>
    public required string Title { get; init; }
}
