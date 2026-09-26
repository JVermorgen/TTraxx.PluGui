using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// Square 2D pad with a glowing dot at the current (X, Y) position, optional
/// corner glyphs, and an optional read-only modulation-target indicator.
/// Dragging positions the dot absolutely under the cursor.
/// <para>
/// Optionally it carries its own footer - a segmented selector inside the same frame, under the pad
/// area (see <see cref="XYPadControlConfiguration.Footer"/>) - and marks where its snap can land
/// (<see cref="XYPadControlConfiguration.SnapPoints"/>), so a pad and its snap mode read as one control.
/// </para>
/// </summary>
public sealed class XYPadControl(XYPadControlConfiguration config) : PluginControl(config)
{
    private double _xValue = config.XParameter.Normalized;
    private double _yValue = config.YParameter.Normalized;
    private bool _isDragging;
    private int _hoveredFooterIndex = -1;
    private bool _isToggleHovered;

    // The recent path of every live point, by its id: (x, y) in normalized pad space, with the
    // Stopwatch timestamp it was seen at. Pruned to XYPadStyle.LiveTrailSeconds on every repaint.
    private readonly Dictionary<long, List<(double X, double Y, long Time)>> _trails = [];
    private readonly List<long> _staleTrails = [];

    private XYPadStyle Style => config.ResolveStyle();

    /// <summary>Live points change by themselves, so the pad has to keep repainting to show them.</summary>
    public override bool NeedsContinuousRepaint => config.LivePoints is not null;

    /// <summary>Height of the pad area, in pixels: the whole control, less the footer band if there is one.</summary>
    private int PadHeight => config.Footer is null ? _h : _h - Rescale(Style.FooterHeight);

    private bool IsInFooter(int localY) => config.Footer is not null && localY >= PadHeight;

    /// <summary>Where the footer's choices end and its toggle, if any, begins - in pixels.</summary>
    private int FooterChoicesRight => config.Footer?.Toggle is null ? _w : _w - Rescale(Style.FooterToggleWidth);

    private bool IsOnFooterToggle(int localX) => config.Footer?.Toggle is not null && localX >= FooterChoicesRight;

    private int FooterIndexAt(int localX)
    {
        var count = config.Footer?.Items.Count ?? 0;
        if (count == 0 || IsOnFooterToggle(localX)) return -1;
        return Math.Clamp(localX * count / Math.Max(1, FooterChoicesRight), 0, count - 1);
    }

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;

        if (IsInFooter(e.Y))
        {
            if (config.Footer is { } footer)
            {
                if (IsOnFooterToggle(e.X) && footer.Toggle is { } toggle) toggle.SetOn(!toggle.IsOn());
                else if (FooterIndexAt(e.X) is var index and >= 0) footer.SetSelectedIndex(index);
            }
            Refresh();
            return;
        }

        _isDragging = true;
        config.BeginGroupEdit?.Invoke();
        ApplyFromPointer(e.X, e.Y);
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        if (_isDragging)
        {
            ApplyFromPointer(e.X, e.Y);
            Refresh();
            return;
        }

        var inFooter = HitTest(e.X, e.Y) && IsInFooter(e.Y);
        var hovered = inFooter ? FooterIndexAt(e.X) : -1;
        var toggleHovered = inFooter && IsOnFooterToggle(e.X);
        if (hovered == _hoveredFooterIndex && toggleHovered == _isToggleHovered) return;
        _hoveredFooterIndex = hovered;
        _isToggleHovered = toggleHovered;
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        config.EndGroupEdit?.Invoke();
    }

    public override void OnPointerLeave()
    {
        _hoveredFooterIndex = -1;
        _isToggleHovered = false;
        base.OnPointerLeave();
    }

    public override IParameterControlInfo? GetParameterInfo(int localX, int localY)
    {
        if (IsInFooter(localY)) return null;

        // Split along the anti-diagonal: bottom-right half reports Y, the rest X.
        var isBottomRightHalf = ((_w / 2) - localX) + ((PadHeight / 2) - localY) > 0;
        return isBottomRightHalf ? config.YParameter.Info : config.XParameter.Info;
    }

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", () =>
        {
            var (x, y) = Snapped(config.XParameter.Info.DefaultNormalizedValue, config.YParameter.Info.DefaultNormalizedValue);
            config.BeginGroupEdit?.Invoke();
            config.XParameter.Edit(config.XParameter.Quantize(x));
            config.YParameter.Edit(config.YParameter.Quantize(y));
            config.EndGroupEdit?.Invoke();
            Refresh();
        }, IsEnabled)
    ];

    private void ApplyFromPointer(int localX, int localY)
    {
        var (newX, newY) = Snapped(Math.Clamp(localX / (double)_w, 0.0, 1.0), Math.Clamp(localY / (double)PadHeight, 0.0, 1.0));

        if (Math.Abs(newX - _xValue) <= 1e-6 && Math.Abs(newY - _yValue) <= 1e-6) return;

        _xValue = newX;
        _yValue = newY;
        config.XParameter.Edit(_xValue);
        config.YParameter.Edit(_yValue);
    }

    /// <summary>The configured snap applied to a normalized position, kept inside 0..1 whatever it returns.</summary>
    private (double X, double Y) Snapped(double x, double y)
    {
        if (config.Snap is not { } snap) return (x, y);

        var (snappedX, snappedY) = snap(x, y);
        return (Math.Clamp(snappedX, 0.0, 1.0), Math.Clamp(snappedY, 0.0, 1.0));
    }

    public override void Draw(SKCanvas canvas)
    {
        if (!_isDragging)
        {
            _xValue = config.XParameter.Normalized;
            _yValue = config.YParameter.Normalized;
        }

        var style = Style;
        var radius = Rescale(style.CornerRadius);
        var padHeight = PadHeight;
        var card = new SKRoundRect(SKRect.Create(1, 1, _w - 1, _h - 1), radius);

        // One card for the pad and its footer: the background first, the border last, so the footer
        // sits inside the same frame rather than beside it.
        canvas.FillRoundRectRadialGradient(1, 1, _w - 1, _h - 1, Math.Max(_w, _h) * 0.75f, radius,
            [Theme.XYPanelBackgroundShadow, Theme.XYPanelBackgroundShadow, Theme.XYPanelBackgroundHighlight], [0, 0.3f, 1f]);

        if (config.Footer is { } footer) DrawFooter(canvas, footer, card, padHeight, style);

        using (SKPaint gridPaint = new()
        {
            Color = Theme.TrackBackground,
            IsAntialias = false,
            StrokeWidth = 1
        })
        {
            canvas.DrawLine(_w / 2f, 8, _w / 2f, padHeight - 8, gridPaint);
            canvas.DrawLine(8, padHeight / 2f, _w - 8, padHeight / 2f, gridPaint);
        }

        var snapPoints = config.SnapPoints?.Invoke() ?? [];
        DrawSnapMarkers(canvas, snapPoints, padHeight, style);
        DrawCornerIcons(canvas, snapPoints, padHeight, style);

        using (SKPaint borderPaint = new()
        {
            Color = Theme.XYPanelBorder,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        })
        {
            canvas.DrawRoundRect(card, borderPaint);
        }

        var dotX = (int)(_xValue * _w);
        var dotY = (int)(_yValue * padHeight);

        using SKPaint glowOuter = new() { Color = Theme.GlowOuter, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowMid = new() { Color = Theme.GlowMid, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint glowCore = new() { Color = Theme.GlowCore, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawCircle(dotX, dotY, Rescale(style.GlowOuterRadius), glowOuter);
        canvas.DrawCircle(dotX, dotY, Rescale(style.GlowMidRadius), glowMid);
        canvas.DrawCircle(dotX, dotY, Rescale(style.GlowCoreRadius), glowCore);

        DrawModulationIndicator(canvas, dotX, dotY, padHeight, style);

        // Over the dot, not under it: where the sound actually is matters more than where it was
        // put, and with nothing modulating it a live point sits right in the dot's glow.
        DrawLivePoints(canvas, padHeight, style);
    }

    /// <summary>
    /// Each live point as a small solid dot faded by its weight, with a trail of where it has been
    /// over the last <see cref="XYPadStyle.LiveTrailSeconds"/> - fading out and thinning with age.
    /// </summary>
    private void DrawLivePoints(SKCanvas canvas, int padHeight, XYPadStyle style)
    {
        if (config.LivePoints is not { } getPoints) return;

        var points = getPoints();
        var now = System.Diagnostics.Stopwatch.GetTimestamp();
        var maxAge = (long)(style.LiveTrailSeconds * System.Diagnostics.Stopwatch.Frequency);

        // Trails of points that have gone - a note that ended - go with them.
        _staleTrails.Clear();
        foreach (var id in _trails.Keys)
        {
            var present = false;
            foreach (var point in points) present |= point.Id == id;
            if (!present) _staleTrails.Add(id);
        }
        foreach (var id in _staleTrails) _trails.Remove(id);

        var color = Theme.XYLivePoint;
        using SKPaint trailPaint = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
        using SKPaint dotPaint = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
        var radius = RescaleExact(style.LivePointRadius);

        foreach (var point in points)
        {
            var weight = (float)Math.Clamp(point.Weight, 0.0, 1.0);
            if (!_trails.TryGetValue(point.Id, out var trail)) _trails[point.Id] = trail = [];

            if (trail.Count == 0 || trail[^1].X != point.X || trail[^1].Y != point.Y) trail.Add((point.X, point.Y, now));
            trail.RemoveAll(entry => now - entry.Time > maxAge);

            for (var i = 1; i < trail.Count; i++)
            {
                var freshness = 1f - ((now - trail[i].Time) / (float)maxAge); // 1 = just now, 0 = about to drop off
                trailPaint.Color = color.WithAlpha((byte)(170 * freshness * weight));
                trailPaint.StrokeWidth = radius * (0.4f + (0.8f * freshness));
                canvas.DrawLine((float)(trail[i - 1].X * _w), (float)(trail[i - 1].Y * padHeight),
                    (float)(trail[i].X * _w), (float)(trail[i].Y * padHeight), trailPaint);
            }

            // Never quite invisible while the point exists: a note in its release still reads as a note.
            dotPaint.Color = color.WithAlpha((byte)(60 + (195 * weight)));
            canvas.DrawCircle((float)(point.X * _w), (float)(point.Y * padHeight), radius, dotPaint);
        }
    }

    /// <summary>
    /// The footer band: a shade darker than the pad, a hairline across the top, and the choices as
    /// equal segments - the selected one a soft accent pill, like a tab strip without its own track.
    /// </summary>
    private void DrawFooter(SKCanvas canvas, XYPadFooter footer, SKRoundRect card, int padHeight, XYPadStyle style)
    {
        var enabled = IsEnabled;
        var band = new SKRect(1, padHeight, _w - 1, _h - 1);

        canvas.Save();
        canvas.ClipRoundRect(card, antialias: true);
        using (SKPaint shade = new() { Color = Theme.XYPanelBackgroundShadow.WithAlpha(140), Style = SKPaintStyle.Fill })
            canvas.DrawRect(band, shade);
        canvas.Restore();

        using (SKPaint hairline = new() { Color = Theme.XYPanelBorder, IsAntialias = false, StrokeWidth = 1 })
            canvas.DrawLine(1, padHeight + 0.5f, _w - 1, padHeight + 0.5f, hairline);

        using SKFont font = new() { Size = RescaleExact(style.FooterFontSize), Typeface = Fonts.Bold };
        var textTop = band.Top + ((band.Height - (font.Metrics.Descent - font.Metrics.Ascent)) / 2f);
        var inset = RescaleExact(3f);
        var pillRadius = RescaleExact(3f);

        if (footer.Toggle is { } toggle) DrawFooterToggle(canvas, toggle, band, font, textTop, inset, enabled);

        var count = footer.Items.Count;
        if (count == 0) return;

        var selected = Math.Clamp(footer.GetSelectedIndex(), 0, count - 1);
        var choicesRight = Math.Min(band.Right, FooterChoicesRight);
        var segmentWidth = (choicesRight - band.Left - (2 * inset)) / count;

        var pill = new SKRect(band.Left + inset + (selected * segmentWidth), band.Top + inset,
            band.Left + inset + ((selected + 1) * segmentWidth), band.Bottom - inset);
        using (SKPaint highlight = new() { Color = Theme.Accent.WithAlpha(enabled ? (byte)150 : (byte)50), IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(pill, pillRadius, pillRadius, highlight);

        for (var i = 0; i < count; i++)
        {
            var color = !enabled ? Theme.TextDisabled
                      : i == selected || i == _hoveredFooterIndex ? Theme.TextPrimary
                      : Theme.TextDim;
            using SKPaint text = new() { Color = color, IsAntialias = true };
            canvas.DrawTextTopAligned(footer.Items[i], band.Left + inset + ((i + 0.5f) * segmentWidth), textTop, SKTextAlign.Center, font, text);
        }
    }

    /// <summary>
    /// The footer's toggle, at its right end behind a hairline: a small dot in the live-point colour
    /// (lit when on, hollow when off) and its label - so it reads as the switch for those points.
    /// </summary>
    private void DrawFooterToggle(SKCanvas canvas, XYPadFooterToggle toggle, SKRect band, SKFont font, float textTop, float inset, bool enabled)
    {
        var left = (float)FooterChoicesRight;
        var isOn = toggle.IsOn();

        using (SKPaint separator = new() { Color = Theme.XYPanelBorder, IsAntialias = false, StrokeWidth = 1 })
            canvas.DrawLine(left + 0.5f, band.Top + inset, left + 0.5f, band.Bottom - inset, separator);

        var dotRadius = RescaleExact(3f);
        var dotX = left + RescaleExact(10f);
        var dotY = band.MidY;
        var dotColor = !enabled ? Theme.TextDisabled : isOn ? Theme.XYLivePoint : Theme.TextDim;
        using (SKPaint dot = new()
        {
            Color = dotColor,
            IsAntialias = true,
            Style = isOn ? SKPaintStyle.Fill : SKPaintStyle.Stroke,
            StrokeWidth = RescaleExact(1.2f)
        })
            canvas.DrawCircle(dotX, dotY, dotRadius, dot);

        var textColor = !enabled ? Theme.TextDisabled : isOn || _isToggleHovered ? Theme.TextPrimary : Theme.TextDim;
        using SKPaint text = new() { Color = textColor, IsAntialias = true };
        canvas.DrawTextTopAligned(toggle.Label, dotX + dotRadius + RescaleExact(5f), textTop, SKTextAlign.Left, font, text);
    }

    /// <summary>
    /// Where the snap can land, by where the point lies: inside the pad a small dot, on an edge a
    /// tick in from the frame. Corners are left to <see cref="DrawCornerIcons"/>: a mark there would
    /// sit outside the rounded frame.
    /// </summary>
    private void DrawSnapMarkers(SKCanvas canvas, IReadOnlyList<(double X, double Y)> points, int padHeight, XYPadStyle style)
    {
        if (points.Count == 0) return;

        using SKPaint marker = new() { Color = Theme.TextDim, IsAntialias = true, Style = SKPaintStyle.Fill };
        using SKPaint tick = new() { Color = Theme.TextDim, IsAntialias = true, StrokeWidth = RescaleExact(1.5f), StrokeCap = SKStrokeCap.Round };
        var tickLength = RescaleExact(style.SnapTickLength);
        var edgeInset = RescaleExact(2f); // clear of the 2 px frame

        foreach (var (x, y) in points)
        {
            var onLeftOrRight = x <= 0.0 || x >= 1.0;
            var onTopOrBottom = y <= 0.0 || y >= 1.0;
            if (onLeftOrRight && onTopOrBottom) continue;

            var px = (float)(x * _w);
            var py = (float)(y * padHeight);
            if (onLeftOrRight)
            {
                var from = x <= 0.0 ? edgeInset : _w - edgeInset;
                canvas.DrawLine(from, py, x <= 0.0 ? from + tickLength : from - tickLength, py, tick);
            }
            else if (onTopOrBottom)
            {
                var from = y <= 0.0 ? edgeInset : padHeight - edgeInset;
                canvas.DrawLine(px, from, px, y <= 0.0 ? from + tickLength : from - tickLength, tick);
            }
            else
            {
                canvas.DrawCircle(px, py, RescaleExact(style.SnapMarkerRadius), marker);
            }
        }
    }

    /// <summary>The corner glyphs - lit up where a snap point sits on that corner.</summary>
    private void DrawCornerIcons(SKCanvas canvas, IReadOnlyList<(double X, double Y)> snapPoints, int padHeight, XYPadStyle style)
    {
        if (style.CornerIcons is not { Count: > 0 } icons) return;

        float iconW = Rescale(style.IconWidth);
        float iconH = Rescale(style.IconHeight);
        var strokeW = RescaleExact(style.IconStrokeWidth);

        var topY = Rescale(10) + (iconH / 2f);
        var bottomY = padHeight - Rescale(24) + (iconH / 2f);
        var leftX = Rescale(10) + (iconW / 2f);
        var rightX = _w - Rescale(38) + (iconW / 2f);

        // Clockwise from top-left: TL, TR, BL, BR - the same order as the corners' (x, y) below.
        ReadOnlySpan<(float X, float Y)> positions =
            [(leftX, topY), (rightX, topY), (leftX, bottomY), (rightX, bottomY)];
        ReadOnlySpan<(double X, double Y)> corners = [(0, 0), (1, 0), (0, 1), (1, 1)];

        for (var i = 0; i < icons.Count && i < positions.Length; i++)
        {
            var isSnapTarget = false;
            foreach (var point in snapPoints)
            {
                if (point.X == corners[i].X && point.Y == corners[i].Y) isSnapTarget = true;
            }

            canvas.DrawIconStroke(icons[i], positions[i].X, positions[i].Y, iconW, iconH, strokeW,
                isSnapTarget ? Theme.TextPrimary : Theme.TextDim);
        }
    }

    private void DrawModulationIndicator(SKCanvas canvas, int dotX, int dotY, int padHeight, XYPadStyle style)
    {
        if (config.ModulationIndicator is not { } indicator) return;

        var offsetX = Math.Clamp(indicator.GetOffsetX(), -1.0, 1.0);
        var offsetY = Math.Clamp(indicator.GetOffsetY(), -1.0, 1.0);

        if (Math.Abs(offsetX) <= 0.001 && Math.Abs(offsetY) <= 0.001) return;

        var targetPxX = (int)(Math.Clamp(_xValue + offsetX, 0.0, 1.0) * _w);
        var targetPxY = (int)(Math.Clamp(_yValue + offsetY, 0.0, 1.0) * padHeight);

        using SKPaint linePaint = new()
        {
            Color = Theme.Accent2.WithAlpha(140),
            IsAntialias = true,
            StrokeWidth = RescaleExact(1.2f),
            PathEffect = SKPathEffect.CreateDash([RescaleExact(3f), RescaleExact(3f)], 0)
        };
        canvas.DrawLine(dotX, dotY, targetPxX, targetPxY, linePaint);

        using SKPaint ringPaint = new()
        {
            Color = Theme.Accent2,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = RescaleExact(1.5f)
        };
        canvas.DrawCircle(targetPxX, targetPxY, Rescale(style.IndicatorRingRadius), ringPaint);
    }
}
