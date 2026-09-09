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
/// A square 2D pad with a glowing dot showing the current (X, Y) position,
/// waveform-style icons in each corner, and an optional secondary
/// "ghost" indicator for a modulated/offset target position. Dragging
/// anywhere in the pad moves the dot (and value) directly under the
/// cursor - absolute positioning, unlike knobs' relative drag.
/// </summary>
public sealed class XYPadControl(XYPadControlConfiguration config) : AbstractControlBase(config)
{
    private double _xValue = config.GetNormalizedXValue();
    private double _yValue = config.GetNormalizedYValue();

    private bool _isDragging;

    public override void OnPointerDown(PointerEventArgs e)
    {
        _isDragging = true;
        config.BeginGroupAction();
        if (SetFromPointer(e.X, e.Y))
        {
            config.BeginXAction();
            config.PerformXAction(_xValue);
            config.EndXAction();
            config.BeginYAction();
            config.PerformYAction(_yValue);
            config.EndYAction();
        }
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        if (_isDragging)
        {
            if (SetFromPointer(e.X, e.Y))
            {
                config.BeginXAction();
                config.PerformXAction(_xValue);
                config.EndXAction();
                config.BeginYAction();
                config.PerformYAction(_yValue);
                config.EndYAction();
            }
            Refresh();
        }
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            config.EndGroupAction();
        }
    }

    public override bool HitTest(int x, int y) => x >= _x && x <= _x + _w && y >= _y && y <= _y + _h;

    public override IParameterControlInfo GetParameterInfo(int xPos, int yPos)
    {
        var halfWidth = _w / 2;
        var relativeXPos = xPos - _x;
        var halfHeight = _h / 2;
        var relativeYPos = yPos - _y;
        var isDiagonalRightBottomHalf = (halfWidth - relativeXPos) + (halfHeight - relativeYPos) > 0;
        return isDiagonalRightBottomHalf
            ? config.ParameterYInfo
            : config.ParameterXInfo;
    }

    private bool SetFromPointer(int x, int y)
    {
        var newX = Math.Clamp(x / (double)_w, 0.0, 1.0);
        var newY = Math.Clamp(y / (double)_h, 0.0, 1.0);
        var changed = Math.Abs(newX - _xValue) > 1e-6 || Math.Abs(newY - _yValue) > 1e-6;
        _xValue = newX;
        _yValue = newY;
        return changed;
    }

    public override void Draw(SKCanvas canvas)
    {
        if (!_isDragging)
        {
            _xValue = Math.Clamp(config.GetNormalizedXValue(), 0.0, 1.0);
            _yValue = Math.Clamp(config.GetNormalizedYValue(), 0.0, 1.0);
        }

        using (SKPaint borderPaint = new()
        {
            Color = Theme.Current.XYPanelBorder,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        })
        {
            var bgColorCenter = Theme.Current.XYPanelBackgroundShadow;
            var bgColorSide = Theme.Current.XYPanelBackgroundHighlight;
            const int x = 1;
            const int y = 1;
            var w = _w - 1;
            var h = _h - 1;
            var radius = Math.Max(w, h) * 0.75f;
            canvas.FillRoundRectRadialGradient(x, y, w, h, radius, 12, [bgColorCenter, bgColorCenter, bgColorSide], [0, 0.3f, 1f]);
            canvas.DrawRoundRect(x, y, w, h, 12, 12, borderPaint);
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

        var dotX = (int)(_xValue * _w);
        var dotY = (int)(_yValue * _h);
        var dimColor = Theme.Current.TextDim;

        float iconW = Globals.Rescale(28);
        float iconH = Globals.Rescale(12);
        var strokeW = Globals.RescaleExact(1.5f);

        var topY = Globals.Rescale(10) + (iconH / 2f);
        var bottomY = _h - Globals.Rescale(24) + (iconH / 2f);
        var leftX = Globals.Rescale(10) + (iconW / 2f);
        var rightX = _w - Globals.Rescale(38) + (iconW / 2f);

        canvas.DrawIconStroke(Icons.Sine, leftX, topY, iconW, iconH, strokeW, dimColor);
        canvas.DrawIconStroke(Icons.Saw, rightX, topY, iconW, iconH, strokeW, dimColor);
        canvas.DrawIconStroke(Icons.Pulse, leftX, bottomY, iconW, iconH, strokeW, dimColor);
        canvas.DrawIconStroke(Icons.Triangle, rightX, bottomY, iconW, iconH, strokeW, dimColor);

        using SKPaint glowOuter = new() { Color = Theme.Current.GlowOuter, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowMid = new() { Color = Theme.Current.GlowMid, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowCore = new() { Color = Theme.Current.GlowCore, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(11), glowOuter);
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(7), glowMid);
        canvas.DrawCircle(dotX, dotY, Globals.Rescale(3), glowCore);

        if (config.MorphEnvelopeIndicator is { } indicator)
        {
            var offsetX = Math.Clamp(indicator.GetOffsetX(), -1.0, 1.0);
            var offsetY = Math.Clamp(indicator.GetOffsetY(), -1.0, 1.0);

            if (Math.Abs(offsetX) > 0.001 || Math.Abs(offsetY) > 0.001)
            {
                var targetX = Math.Clamp(_xValue + offsetX, 0.0, 1.0);
                var targetY = Math.Clamp(_yValue + offsetY, 0.0, 1.0);
                var targetPxX = (int)(targetX * _w);
                var targetPxY = (int)(targetY * _h);

                using SKPaint linePaint = new()
                {
                    Color = Theme.Current.Accent2.WithAlpha(140),
                    IsAntialias = true,
                    StrokeWidth = Globals.RescaleExact(1.2f),
                    PathEffect = SKPathEffect.CreateDash(
                        [Globals.RescaleExact(3f), Globals.RescaleExact(3f)], 0)
                };
                canvas.DrawLine(dotX, dotY, targetPxX, targetPxY, linePaint);

                using SKPaint targetRingPaint = new()
                {
                    Color = Theme.Current.Accent2,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = Globals.RescaleExact(1.5f)
                };
                canvas.DrawCircle(targetPxX, targetPxY, Globals.Rescale(5), targetRingPaint);
            }
        }
    }
}