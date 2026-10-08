namespace TTraxx.PluGui.Gui;

/// <summary>
/// Declares a single-line text field - a name to save under, a search. Not bound to a host
/// parameter: text isn't automatable. The field edits a copy while it has the keyboard and reports
/// each change (<see cref="OnTextChanged"/>); Enter commits, Escape cancels.
/// </summary>
public class TextFieldControlConfiguration() : ControlConfiguration
{
    /// <summary>The text to show, and to start editing from. Read whenever the field isn't being edited.</summary>
    public Func<string> GetText { get; init; } = static () => "";

    /// <summary>Runs on every edit with the text as it now is - a search filtering as it's typed. Null for none.</summary>
    public Action<string>? OnTextChanged { get; init; }

    /// <summary>Enter pressed: the text as typed. The field gives the keyboard back afterwards. Null for none.</summary>
    public Action<string>? OnCommit { get; init; }

    /// <summary>Escape pressed. The field gives the keyboard back afterwards. Null for none.</summary>
    public Action? OnCancel { get; init; }

    /// <summary>Shown dimmed while the field is empty.</summary>
    public string Placeholder { get; init; } = "";

    /// <summary>The longest text the field takes, in characters.</summary>
    public int MaxLength { get; init; } = 64;

    /// <summary>Text size in design units.</summary>
    public float FontSize { get; init; } = 11f;

    /// <summary>
    /// Lets the owner give the field the keyboard without a click - a dialog opening with its name
    /// field ready to type in. Null for none.
    /// </summary>
    public TextFieldFocusRequest? FocusRequest { get; init; }
}

/// <summary>
/// A way to give a text field the keyboard from outside it (see
/// <see cref="TextFieldControlConfiguration.FocusRequest"/>): the owner keeps this, passes it in the
/// field's configuration, and calls <see cref="Request"/>.
/// </summary>
public sealed class TextFieldFocusRequest
{
    internal event Action? Requested;

    /// <summary>The field takes the keyboard, its text all selected - if it has been laid out.</summary>
    public void Request() => Requested?.Invoke();
}
