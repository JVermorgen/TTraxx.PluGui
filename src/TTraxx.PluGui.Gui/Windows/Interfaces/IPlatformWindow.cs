namespace TTraxx.PluGui.Gui;

/// <summary>Implemented per platform (Win32PlatformWindow, MacOsPlatformWindow, LinuxPlatformWindow). Manages the native window + the shared Skia surface, and routes all events to the host.</summary>
internal interface IPlatformWindow
{
    bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host);
    void SetBounds(int x, int y, int width, int height);
    float GetInitialScaleFactor(nint parentHandle);
    void Invalidate();
    void Destroy();

    /// <summary>
    /// Starts (true) or stops (false) a self-owned periodic repaint, for
    /// controls that animate on their own (e.g. a level meter) and can't
    /// rely on Invalidate() being called by anything else. Idempotent.
    /// Platforms without a way to drive this themselves (see
    /// LinuxPlatformWindow) may no-op.
    /// </summary>
    void SetContinuousRepaint(bool enabled);

    /// <summary>
    /// Non-null when this platform requires an external event pump
    /// (see <see cref="IEventPumpSource"/>). Null on platforms that
    /// already have their own message loop (Win32, macOS).
    /// </summary>
    IEventPumpSource? EventPumpSource { get; }

    /// <summary>
    /// Non-null only while this platform has no timer of its own (currently
    /// only X11/Linux) AND SetContinuousRepaint(true) is currently in effect -
    /// see <see cref="ITimerPumpSource"/>. Null on platforms with a self-driven
    /// timer (Win32), platforms without one implemented yet (macOS), and
    /// whenever no continuously-repainting control is present.
    /// </summary>
    ITimerPumpSource? TimerPumpSource { get; }

    /// <summary>
    /// Shows the platform's modal open-file dialog, owned by the host's window, and returns the chosen
    /// file's full path, or null if the user cancelled (or the platform has no dialog to show). Blocks
    /// the UI thread while the dialog is open, as a modal dialog does.
    /// </summary>
    string? ShowOpenFileDialog(string title, IReadOnlyList<FileDialogFilter> filters);

    /// <summary>
    /// Starts dragging files out of the window - to a DAW's track, say - while the left button is
    /// down, and blocks until the drag ends; true when they were dropped somewhere that took them.
    /// The press ends with it: the host gets OnPointerUp before this returns. False at once where the
    /// platform can't drag out yet.
    /// </summary>
    bool StartFileDrag(IReadOnlyList<string> paths);

    /// <summary>
    /// Takes the keyboard for the window (true) - a control is editing text - or gives it back to
    /// whatever had it before (false). Key presses and typed text then reach the host (OnKeyDown,
    /// OnTextInput). A no-op where the platform can't take the keyboard yet.
    /// </summary>
    void SetKeyboardFocus(bool focused);

    /// <summary>The clipboard's text, or null when it holds none (or the platform can't read it yet).</summary>
    string? GetClipboardText();

    /// <summary>Puts text on the clipboard, where the platform can.</summary>
    void SetClipboardText(string text);

    /// <summary>
    /// The text a key's character makes as the keyboard stands now - Shift, Caps Lock, the layout -
    /// for a host that passes on the key's plain character (FL Studio sends "a" for Shift+A). Where
    /// the platform can't tell, Shift makes a letter upper case.
    /// </summary>
    string TranslateTypedCharacter(char character, KeyModifiers modifiers);
}
