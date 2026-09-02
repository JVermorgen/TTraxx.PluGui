namespace TTraxx.PluGui.Gui.Windows.Interfaces;

/// <summary>Implemented per platform (Win32PlatformWindow, MacOsPlatformWindow, LinuxPlatformWindow). Manages the native window + the shared Skia surface, and routes all events to the host.</summary>
internal interface IPlatformWindow
{
    bool Attach(nint parentHandle, int width, int height, IPlatformWindowHost host);
    void SetBounds(int x, int y, int width, int height);
    float GetInitialScaleFactor(nint parentHandle);
    void Invalidate();
    void Destroy();

    /// <summary>
    /// Non-null when this platform requires an external event pump
    /// (see <see cref="IEventPumpSource"/>). Null on platforms that
    /// already have their own message loop (Win32, macOS).
    /// </summary>
    IEventPumpSource? EventPumpSource { get; }
}
