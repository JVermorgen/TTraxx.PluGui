using SkiaSharp;

namespace TTraxx.PluGui.Gui.Windows.Interfaces;

/// <summary>Implement this in AbstractWindowBase — receives callbacks from the platform layer, without AbstractWindowBase needing to know anything about Win32/Cocoa/X11.</summary>
internal interface IPlatformWindowHost
{
    void OnPaint(SKCanvas canvas, int width, int height);
    void OnResize(int width, int height);
    void OnPointerDown(int x, int y);
    void OnPointerMove(int x, int y);
    void OnPointerUp(int x, int y);
    void OnWheel(int x, int y, int ticks);
    void OnDoubleClick(int x, int y);
}