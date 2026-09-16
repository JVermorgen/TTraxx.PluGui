using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Panels.Base.Interfaces;
using TTraxx.PluGui.Gui.Panels.Configuration;

namespace TTraxx.PluGui.Gui.Panels.Base;

public abstract class AbstractPluginPanelBase(PluginPanelConfiguration config) : IPluginPanel
{
    protected readonly PluginPanelConfiguration _config = config;

    public int Height => Globals.Rescale(_config.Height);
    public int Left => Globals.Rescale(_config.Left);
    public int Top => Globals.Rescale(_config.Top);
    public int Width => Globals.Rescale(_config.Width);

    public int CornerRadius => Globals.Rescale(_config.CornerRadius);

    private readonly List<ControlPlacement> _controls = [];
    public IReadOnlyList<ControlPlacement> Controls => _controls;

    public ControlPlacement AddControl(AbstractControlBase control, int relativeX, int relativeY, int width, int height)
    {
        ControlPlacement placed = new(control, _config.Left + relativeX, _config.Top + relativeY, width, height);
        _controls.Add(placed);
        return placed;
    }

    public abstract void Draw(SKCanvas canvas);

    protected float FromLeft(int pixels) => Left + Globals.RescaleExact(pixels);

    protected float FromRight(int pixels) => Left + Width - Globals.RescaleExact(pixels);

    protected float FromTop(int pixels) => Top + Globals.RescaleExact(pixels);
}