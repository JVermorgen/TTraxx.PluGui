using SkiaSharp;
using TTraxx.PluGui.Gui;
using TTraxx.PluGui.Harness.NPlug.Core.Controls;

namespace TTraxx.PluGui.Harness.NPlug.Core;

/// <summary>
/// Fixed-height dev-tool bar shown above the plugin view in every harness
/// window: a switch to pin the harness window above others, and a switch for
/// TTraxx.PluGui.Gui.Helpers.Globals.ShowControlBounds (outlines every
/// control's bounds - see that flag's own doc comment). Built from the exact
/// same window/control framework a plugin's own view draws with, so it
/// automatically matches the loaded plugin's theme, and is never part of a
/// shipped plugin - only the harness runners construct one.
/// </summary>
public sealed class HarnessSettingsPanel(bool initialAlwaysOnTop, Action<bool> onAlwaysOnTopChanged) : PluginWindow
{
    /// <summary>Logical (unscaled) height - rescaled the same way as everything else this window lays out, so it stays proportional at high DPI.</summary>
    public const int Height = 50;

    private bool _isAlwaysOnTop = initialAlwaysOnTop;

    /// <summary>The "show control bounds" switch outlines the plugin's own controls, not the switch bar itself.</summary>
    protected override bool ParticipatesInDebugBoundsOverlay => false;

    protected override IEnumerable<ControlPlacement> BuildLayout()
    {
        yield return (new HarnessToggleControl(new ToggleControlConfiguration
        {
            Parameter = new ParameterBinding
            {
                Info = new ParameterControlInfo {
                    ParameterId = 0,
                    Label = "Always on top",
                    Unit = string.Empty,
                    DefaultNormalizedValue = 0.0
                },
                GetNormalizedValue = () => _isAlwaysOnTop ? 1.0 : 0.0,
                BeginEdit = () => { },
                SetNormalizedValue = value =>
                {
                    _isAlwaysOnTop = value >= 0.5;
                    onAlwaysOnTopChanged(_isAlwaysOnTop);
                },
                EndEdit = () => { }
            }
        }), 16, 0, 170, Height);

        yield return (new HarnessToggleControl(new ToggleControlConfiguration
        {
            Parameter = new ParameterBinding
            {
                Info = new ParameterControlInfo {
                    ParameterId = 1,
                    Label = "Show control outlines",
                    Unit = string.Empty,
                    DefaultNormalizedValue = 0.0
                },
                GetNormalizedValue = () => Globals.ShowControlBounds ? 1.0 : 0.0,
                BeginEdit = () => { },
                SetNormalizedValue = value => Globals.ShowControlBounds = value >= 0.5,
                EndEdit = () => { }
            }
        }), 196, 0, 220, Height);
    }

    protected override void DrawBackground(SKCanvas canvas, int width, int height)
    {
        using SKPaint bgPaint = new()
        {
            Color = Theme.MenuBackground,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, width, height, bgPaint);

        using SKPaint borderPaint = new()
        {
            Color = Theme.MenuBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        canvas.DrawLine(0, height - 1, width, height - 1, borderPaint);
    }
}
