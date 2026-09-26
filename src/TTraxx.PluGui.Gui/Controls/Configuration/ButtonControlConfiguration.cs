namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a push button that runs an action - for one-shot commands (randomize, undo, reset)
/// rather than a value. Not bound to a host parameter: a command isn't a state, so there is nothing
/// to automate or save. Whatever the action changes, it changes through the plugin's own parameters.
/// </summary>
public class ButtonControlConfiguration() : ControlConfiguration, IStyledControlConfiguration<ButtonStyle>
{
    /// <summary>The text on the button.</summary>
    public required string Label { get; init; }

    /// <summary>
    /// Runs on a click: pointer released over the button it went down on, so a press can still be
    /// abandoned by dragging off. The button repaints the window itself afterwards.
    /// </summary>
    public required Action OnClick { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="ButtonStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<ButtonStyle>? Style { get; init; }
}
