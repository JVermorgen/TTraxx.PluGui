using SkiaSharp;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Windows.Interfaces;

/// <summary>Implement this in AbstractWindowBase — receives callbacks from the platform layer, without AbstractWindowBase needing to know anything about Win32/Cocoa/X11.</summary>
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
}