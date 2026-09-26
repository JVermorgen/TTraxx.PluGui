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

    private XYPadStyle Style => config.ResolveStyle();

    /// <summary>Height of the pad area, in pixels: the whole control, less the footer band if there is one.</summary>
    private int PadHeight => config.Footer is null ? _h : _h - Rescale(Style.FooterHeight);

    private bool IsInFooter(int localY) => config.Footer is not null && localY >= PadHeight;

    private int FooterIndexAt(int localX)
    {
        var count = config.Footer?.Items.Count ?? 0;
        return count == 0 ? -1 : Math.Clamp(localX * count / Math.Max(1, _w), 0, count - 1);
    }

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;

        if (IsInFooter(e.Y))
        {
            if (config.Footer is { } footer && FooterIndexAt(e.X) is var index and >= 0) footer.SetSelectedIndex(index);
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

        var hovered = HitTest(e.X, e.Y) && IsInFooter(e.Y) ? FooterIndexAt(e.X) : -1;
        if (hovered == _hoveredFooterIndex) return;
        _hoveredFooterIndex = hovered;
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

        var count = footer.Items.Count;
        if (count == 0) return;

        var selected = Math.Clamp(footer.GetSelectedIndex(), 0, count - 1);
        var inset = RescaleExact(3f);
        var segmentWidth = (band.Width - (2 * inset)) / count;
        var pillRadius = RescaleExact(3f);

        var pill = new SKRect(band.Left + inset + (selected * segmentWidth), band.Top + inset,
            band.Left + inset + ((selected + 1) * segmentWidth), band.Bottom - inset);
        using (SKPaint highlight = new() { Color = Theme.Accent.WithAlpha(enabled ? (byte)150 : (byte)50), IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(pill, pillRadius, pillRadius, highlight);

        using SKFont font = new() { Size = RescaleExact(style.FooterFontSize), Typeface = Fonts.Bold };
        var textTop = band.Top + ((band.Height - (font.Metrics.Descent - font.Metrics.Ascent)) / 2f);
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
