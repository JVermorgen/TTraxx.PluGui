using SkiaSharp;

namespace TTraxx.PluGui.Gui.Controls.Overlay;

/// <summary>
/// A small floating right-click menu. Unlike a normal control it isn't part
/// of ControlManager's layout/hit-test list: PluginWindow owns a single
/// instance directly so it can always sit on top and swallow the very next
/// click regardless of what control is underneath.
///
/// It is IMMUTABLE once constructed apart from its hover row: size and position are measured from
/// the items in the constructor. The window therefore creates a new one per right-click and drops it
/// on dismissal (and on resize, whose old position would no longer be valid) rather than reusing and
/// re-measuring one.
///
/// Lifecycle is entirely the window's: this type draws, hit-tests and invokes, but never decides
/// when it disappears - see PluginWindow's IPlatformWindowHost input handlers, which close it on the
/// next click, double-click or resize and ignore the wheel while it's open.
/// </summary>
internal sealed class ContextMenuOverlay
{
    private const int RowHeight = 22;
    private const int HorizontalPadding = 14;
    private const int MinWidth = 120;

    private readonly IReadOnlyList<ContextMenuItem> _items;
    private readonly RenderContext _context;
    private readonly SKRect _bounds;
    private int _hoveredIndex = -1;

    /// <summary>
    /// Measures the menu against <paramref name="items"/> and places it at the click point, nudged back
    /// inside the window when it would otherwise overflow the right or bottom edge.
    ///
    /// x, y and containerW/H are already-scaled pixel coordinates, matching everything else the
    /// platform layer reports.
    /// </summary>
    public ContextMenuOverlay(int x, int y, IReadOnlyList<ContextMenuItem> items, int containerW, int containerH, RenderContext context)
    {
        _items = items;
        _context = context;

        using SKFont font = new() { Size = _context.Rescale(11), Typeface = _context.Fonts.Regular };
        var paddingPx = _context.Rescale(HorizontalPadding);
        var rowHeightPx = _context.Rescale(RowHeight);

        var width = _context.Rescale(MinWidth);
        foreach (var item in items)
        {
            var textWidth = font.MeasureText(item.Label, out _);
            width = Math.Max(width, (int)textWidth + (paddingPx * 2));
        }
        var height = rowHeightPx * items.Count;

        // Keep the menu fully on-screen, anchored at the click point when there's room.
        var left = Math.Min(x, Math.Max(0, containerW - width));
        var top = Math.Min(y, Math.Max(0, containerH - height));

        _bounds = new SKRect(left, top, left + width, top + height);
    }

    private float RowHeightPx => _bounds.Height / _items.Count;

    /// <summary>
    /// Moves the hover highlight to the row at this window-relative point, or clears it when the
    /// pointer is outside the menu. The caller repaints.
    /// </summary>
    public void UpdateHover(int x, int y) => _hoveredIndex = IndexAt(x, y);

    /// <summary>Executes the hit item if any and enabled. The caller dismisses the overlay regardless of the outcome.</summary>
    public void HandleClick(int x, int y)
    {
        var index = IndexAt(x, y);
        if (index < 0) return;

        var item = _items[index];
        if (item.IsEnabled) item.Execute();
    }

    private int IndexAt(int x, int y)
    {
        if (!_bounds.Contains(x, y)) return -1;
        var index = (int)((y - _bounds.Top) / RowHeightPx);
        return index >= 0 && index < _items.Count ? index : -1;
    }

    /// <summary>
    /// Draws the menu in absolute, window-relative coordinates - the canvas is NOT translated for it,
    /// unlike a control's. Called last in the window's paint, so it covers everything else.
    /// </summary>
    public void Draw(SKCanvas canvas)
    {
        var radius = _context.RescaleExact(4f);

        using (SKPaint bgPaint = new() { Color = _context.Theme.MenuBackground, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(_bounds, radius, radius, bgPaint);

        using (SKPaint borderPaint = new() { Color = _context.Theme.MenuBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            canvas.DrawRoundRect(_bounds, radius, radius, borderPaint);

        using SKFont font = new() { Size = _context.Rescale(11), Typeface = _context.Fonts.Regular };
        var rowH = RowHeightPx;
        var textX = _bounds.Left + _context.Rescale(HorizontalPadding);

        // Center on the font's actual glyph-box height (ascent + descent), not the
        // nominal font.Size - those two aren't equal, and using Size visibly throws
        // the text off-center within the row.
        SKFontMetrics metrics = font.Metrics;
        var textHeight = metrics.Descent - metrics.Ascent;

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var rowTop = _bounds.Top + (i * rowH);

            if (i == _hoveredIndex && item.IsEnabled)
            {
                using SKPaint hoverPaint = new() { Color = _context.Theme.MenuItemHoverBackground, IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawRoundRect(new SKRect(_bounds.Left, rowTop, _bounds.Left + _bounds.Width, rowTop + rowH), radius, radius, hoverPaint);
            }

            using SKPaint textPaint = new()
            {
                Color = item.IsEnabled ? _context.Theme.MenuTextPrimary : _context.Theme.MenuTextDisabled,
                IsAntialias = true
            };
            canvas.DrawTextTopAligned(item.Label, textX, rowTop + ((rowH - textHeight) / 2f), SKTextAlign.Left, font, textPaint);

            // The current choice: a dot in the left padding, drawn rather than a glyph so it doesn't
            // depend on the font having a check-mark character.
            if (item.IsChecked)
            {
                using SKPaint markPaint = new() { Color = _context.Theme.Accent, IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawCircle(_bounds.Left + (_context.Rescale(HorizontalPadding) / 2f), rowTop + (rowH / 2f), _context.RescaleExact(2.5f), markPaint);
            }
        }
    }
}
