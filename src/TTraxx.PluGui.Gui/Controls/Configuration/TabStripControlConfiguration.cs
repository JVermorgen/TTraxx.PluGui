namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a row of tabs that picks which page of a paged area is on screen - typically placed in
/// a panel's title band, with the panel's controls placed per page via PluginWindow.Place's
/// visibleWhen. Not bound to a host parameter: which page is showing is the window's own state,
/// not part of the sound, so it isn't automatable and isn't saved with a preset.
/// </summary>
public class TabStripControlConfiguration() : ControlConfiguration, IStyledControlConfiguration<TabStripStyle>
{
    /// <summary>One label per page, left to right.</summary>
    public required IReadOnlyList<string> Tabs { get; init; }

    /// <summary>Reads the page currently showing.</summary>
    public required Func<int> GetSelectedIndex { get; init; }

    /// <summary>Switches pages. The strip repaints the window itself afterwards.</summary>
    public required Action<int> SetSelectedIndex { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="TabStripStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<TabStripStyle>? Style { get; init; }
}
