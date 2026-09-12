using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Controls.Configuration;
using TTraxx.PluGui.Gui.Controls.Configuration.Base.Interfaces;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls;

/// <summary>
/// Square 2D pad with a glowing dot at the current (X, Y) position, optional
/// corner glyphs, and an optional read-only modulation-target indicator.
/// Dragging positions the dot absolutely under the cursor.
/// </summary>
public sealed class XYPadControl(XYPadControlConfiguration config) : AbstractControlBase(config)
{
    private double _xValue = config.XParameter.Normalized;
    private double _yValue = config.YParameter.Normalized;
    private bool _isDragging;

    private XYPadStyle Style => config.Style?.Invoke() ?? XYPadStyle.Default;

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;
        _isDragging = true;
        config.BeginGroupEdit?.Invoke();
        ApplyFromPointer(e.X, e.Y);
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        if (!_isDragging) return;
        ApplyFromPointer(e.X, e.Y);
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        config.EndGroupEdit?.Invoke();
    }

    public override IParameterControlInfo? GetParameterInfo(int localX, int localY)
    {
        // Split along the anti-diagonal: bottom-right half reports Y, the rest X.
        var isBottomRightHalf = ((_w / 2) - localX) + ((_h / 2) - localY) > 0;
        return isBottomRightHalf ? config.YParameter.Info : config.XParameter.Info;
    }

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", () =>
        {
            config.BeginGroupEdit?.Invoke();
            config.XParameter.Edit(config.XParameter.Quantize(config.XParameter.Info.DefaultNormalizedValue));
            config.YParameter.Edit(config.YParameter.Quantize(config.YParameter.Info.DefaultNormalizedValue));
            config.EndGroupEdit?.Invoke();
            Refresh();
        }, IsEnabled)
    ];

    private void ApplyFromPointer(int localX, int localY)
    {
        var newX = Math.Clamp(localX / (double)_w, 0.0, 1.0);
        var newY = Math.Clamp(localY / (double)_h, 0.0, 1.0);

        if (Math.Abs(newX - _xValue) <= 1e-6 && Math.Abs(newY - _yValue) <= 1e-6) return;

        _xValue = newX;
        _yValue = newY;
        config.XParameter.Edit(_xValue);
        config.YParameter.Edit(_yValue);
    }

    public override void Draw(SKCanvas canvas)
    {
        if (!_isDragging)
        {
            _xValue = config.XParameter.Normalized;
            _yValue = config.YParameter.Normalized;
        }

        var radius = Globals.Rescale(Style.CornerRadius);

        using (SKPaint borderPaint = new()
        {
            Color = Theme.Current.XYPanelBorder,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        })
        {
            var bgCenter = Theme.Current.XYPanelBackgroundShadow;
            var bgSide = Theme.Current.XYPanelBackgroundHighlight;
            var w = _w - 1;
            var h = _h - 1;
            var gradientRadius = Math.Max(w, h) * 0.75f;
            canvas.FillRoundRectRadialGradient(1, 1, w, h, gradientRadius, radius,
                [bgCenter, bgCenter, bgSide], [0, 0.3f, 1f]);
            canvas.DrawRoundRect(1, 1, w, h, radius, radius, borderPaint);
        }

        using (SKPaint gridPaint = new()
        {
            Color = Theme.Current.TrackBackground,
            IsAntialias = false,
            StrokeWidth = 1
        })
        {
            canvas.DrawLine(_w / 2f, 8, _w / 2f, _h - 8, gridPaint);
            canvas.DrawLine(8, _h / 2f, _w - 8, _h / 2f, gridPaint);
        }

        DrawCornerIcons(canvas);

        var dotX = (int)(_xValue * _w);
        var dotY = (int)(_yValue * _h);

        using SKPaint glowOuter = new() { Color = Theme.Current.GlowOuter, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowMid = new() { Color = Theme.Current.GlowMid, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowCore = new() { Color = Theme.Current.GlowCore, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(Style.GlowOuterRadius), glowOuter);
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(Style.GlowMidRadius), glowMid);
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(Style.GlowCoreRadius), glowCore);

        DrawModulationIndicator(canvas, dotX, dotY);
    }

    private void DrawCornerIcons(SKCanvas canvas)
    {
        if (Style.CornerIcons is not { Count: > 0 } icons) return;

        var dimColor = Theme.Current.TextDim;
        float iconW = Globals.Rescale(Style.IconWidth);
        float iconH = Globals.Rescale(Style.IconHeight);
        var strokeW = Globals.RescaleExact(Style.IconStrokeWidth);

        var topY = Globals.Rescale(10) + (iconH / 2f);
        var bottomY = _h - Globals.Rescale(24) + (iconH / 2f);
        var leftX = Globals.Rescale(10) + (iconW / 2f);
        var rightX = _w - Globals.Rescale(38) + (iconW / 2f);

        // Clockwise from top-left: TL, TR, BL, BR.
        ReadOnlySpan<(float X, float Y)> positions =
            [(leftX, topY), (rightX, topY), (leftX, bottomY), (rightX, bottomY)];

        for (var i = 0; i < icons.Count && i < positions.Length; i++)
        {
            canvas.DrawIconStroke(icons[i], positions[i].X, positions[i].Y, iconW, iconH, strokeW, dimColor);
        }
    }

    private void DrawModulationIndicator(SKCanvas canvas, int dotX, int dotY)
    {
        if (config.ModulationIndicator is not { } indicator) return;

        var offsetX = Math.Clamp(indicator.GetOffsetX(), -1.0, 1.0);
        var offsetY = Math.Clamp(indicator.GetOffsetY(), -1.0, 1.0);

        if (Math.Abs(offsetX) <= 0.001 && Math.Abs(offsetY) <= 0.001) return;

        var targetPxX = (int)(Math.Clamp(_xValue + offsetX, 0.0, 1.0) * _w);
        var targetPxY = (int)(Math.Clamp(_yValue + offsetY, 0.0, 1.0) * _h);

        using SKPaint linePaint = new()
        {
            Color = Theme.Current.Accent2.WithAlpha(140),
            IsAntialias = true,
            StrokeWidth = Globals.RescaleExact(1.2f),
            PathEffect = SKPathEffect.CreateDash([Globals.RescaleExact(3f), Globals.RescaleExact(3f)], 0)
        };
        canvas.DrawLine(dotX, dotY, targetPxX, targetPxY, linePaint);

        using SKPaint ringPaint = new()
        {
            Color = Theme.Current.Accent2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Globals.RescaleExact(1.5f)
        };
        canvas.DrawCircle(targetPxX, targetPxY, Globals.Rescale(Style.IndicatorRingRadius), ringPaint);
    }
}