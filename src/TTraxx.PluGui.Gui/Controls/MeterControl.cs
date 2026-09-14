using SkiaSharp;
using TTraxx.PluGui.Gui.Controls.Base;
using TTraxx.PluGui.Gui.Controls.Configuration;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Helpers.Typography;

namespace TTraxx.PluGui.Gui.Controls;

/// <summary>
/// Read-only vertical LED/segment level meter with a falling peak-hold
/// marker. Not bound to a host parameter - GetLevel is polled every repaint,
/// so the caller is free to feed it live audio-thread state (e.g. via a
/// volatile field written from ProcessMain).
/// </summary>
public sealed class MeterControl(MeterControlConfiguration config) : AbstractControlBase(config)
{
    private double _peakHoldValue;
    private double _peakHoldRemainingSeconds;
    private long _lastDrawTicks = Environment.TickCount64;

    private MeterStyle Style => config.Style?.Invoke() ?? MeterStyle.Default;

    /// <summary>Meters animate independently of user interaction, so the window needs to keep repainting on its own.</summary>
    public override bool NeedsContinuousRepaint => true;

    public override void Draw(SKCanvas canvas)
    {
        var style = Style;
        var level = Math.Max(0.0, config.GetLevel());
        if (level < style.SilenceThreshold) level = 0.0;
        UpdatePeakHold(level, style);

        var labelReserve = config.Label is null ? 0 : Globals.RescaleExact(24);
        var horizontalSpace = Globals.RescaleExact(6);
        var barWidth = Math.Max(0, _w - (horizontalSpace * 2));
        var barXStart = horizontalSpace;
        var barYStart = Globals.RescaleExact(8);
        var barHeight = _h - labelReserve - barYStart;
        if (barHeight <= 0) return;

        var gap = Globals.RescaleExact(style.SegmentGapPx);
        var segmentCount = Math.Max(1, style.SegmentCount);
        var segmentHeight = (barHeight - (gap * (segmentCount - 1))) / segmentCount;
        if (segmentHeight <= 0) return;

        var litSegments = Math.Min(segmentCount, (int)Math.Ceiling(Math.Min(level, 1.0) * segmentCount));

        for (var i = 0; i < segmentCount; i++)
        {
            var segmentTop = barYStart + barHeight - ((i + 1) * segmentHeight) - (i * gap);
            SKRect rect = new(barXStart, segmentTop, barXStart + barWidth, segmentTop + segmentHeight);

            var segmentPosition = (i + 1) / (double)segmentCount;
            var litColor = segmentPosition >= style.ClipThreshold ? style.ClipColor
                : segmentPosition >= style.WarningThreshold ? style.WarningColor
                : style.NormalColor;

            using SKPaint segmentPaint = new()
            {
                Color = i < litSegments ? litColor : Theme.Current.TrackBackground,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };
            canvas.DrawRect(rect, segmentPaint);
        }

        if (_peakHoldValue > 0)
        {
            var markerHeight = Globals.RescaleExact(2f);
            var markerY = Math.Clamp(barHeight - (float)(Math.Min(_peakHoldValue, 1.0) * barHeight), 0, barHeight - markerHeight);
            using SKPaint markerPaint = new() { Color = Theme.Current.TextPrimary, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawRect(new SKRect(barXStart, markerY, barXStart + barWidth, markerY + markerHeight), markerPaint);
        }

        if (config.Label is not null)
        {
            using SKPaint textPaint = new() { Color = Theme.Current.TextDim, IsAntialias = true };
            using SKFont font = new() { Size = Globals.Rescale(11), Typeface = Fonts.Current.Bold };
            canvas.DrawTextTopAligned(config.Label, _w / 2f, barYStart + barHeight + Globals.Rescale(4), SKTextAlign.Center, font, textPaint);
        }
    }

    private void UpdatePeakHold(double level, MeterStyle style)
    {
        var now = Environment.TickCount64;
        var dt = Math.Max(0, (now - _lastDrawTicks) / 1000.0);
        _lastDrawTicks = now;

        if (level >= _peakHoldValue)
        {
            _peakHoldValue = level;
            _peakHoldRemainingSeconds = style.PeakHoldSeconds;
        }
        else if (_peakHoldRemainingSeconds > 0)
        {
            _peakHoldRemainingSeconds -= dt;
        }
        else
        {
            _peakHoldValue = Math.Max(level, _peakHoldValue - (style.PeakHoldDecayPerSecond * dt));
        }
    }
}
