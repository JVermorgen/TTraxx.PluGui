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
    /// abandoned by dragging off. The button repaints the window itself afterwards. Does nothing by
    /// default, for a button that only uses <see cref="OnPress"/>/<see cref="OnRelease"/>.
    /// </summary>
    public Action OnClick { get; init; } = static () => { };

    /// <summary>
    /// Runs the moment the button goes down - with <see cref="OnRelease"/>, a momentary action that
    /// lasts exactly as long as the button is held (audition something, then go back). Null for none.
    /// </summary>
    public Action? OnPress { get; init; }

    /// <summary>
    /// Runs when a press that ran <see cref="OnPress"/> ends - wherever the pointer is by then, and
    /// even if the button was disabled in the meantime, so the two always come in pairs. Null for none.
    /// While it is set, a button stays drawn as held when the pointer drags off it: the action is
    /// still going on.
    /// </summary>
    public Action? OnRelease { get; init; }

    /// <summary>
    /// Look and feel override, or null for <see cref="ButtonStyle.Default"/>. A factory rather than an
    /// instance so the style is resolved on each use - see <see cref="KnobControlConfiguration.Style"/>.
    /// </summary>
    public Func<ButtonStyle>? Style { get; init; }
}
