using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>Implement this in PluginWindow — receives callbacks from the platform layer, without PluginWindow needing to know anything about Win32/Cocoa/X11.</summary>
internal interface IPlatformWindowHost
{
    void OnPaint(SKCanvas canvas, int width, int height);
    void OnResize(int width, int height);
    void OnPointerDown(int x, int y, KeyModifiers modifiers = KeyModifiers.None);
    void OnPointerMove(int x, int y, KeyModifiers modifiers = KeyModifiers.None);
    void OnPointerUp(int x, int y);
    void OnWheel(int x, int y, int ticks, KeyModifiers modifiers = KeyModifiers.None);
    void OnDoubleClick(int x, int y);

    /// <summary>Right-click: show a context menu for whatever control is at (x, y), if it offers one.</summary>
    void OnContextMenu(int x, int y);

    /// <summary>Whether files dragged over (x, y) could be dropped there - the drag cursor shows it.</summary>
    bool CanDropFilesAt(int x, int y);

    /// <summary>Files dropped onto the window from the OS (Explorer, Finder) at (x, y): offered to the control there.</summary>
    void OnFilesDropped(int x, int y, IReadOnlyList<string> paths);

    /// <summary>A key pressed while the window has the keyboard; true when a control took it (the platform then keeps it from the host).</summary>
    bool OnKeyDown(KeyEventArgs e);

    /// <summary>Text typed while the window has the keyboard, through the keyboard layout; true when a control took it.</summary>
    bool OnTextInput(string text);

    /// <summary>The window lost the keyboard to something else (the host, another window): whichever control had it no longer does.</summary>
    void OnKeyboardFocusLost();
}
