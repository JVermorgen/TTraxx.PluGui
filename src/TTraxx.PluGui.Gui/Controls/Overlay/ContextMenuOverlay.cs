using SkiaSharp;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Helpers.Typography;
using TTraxx.PluGui.Gui.Input;

namespace TTraxx.PluGui.Gui.Controls.Overlay;

/// <summary>
/// A small floating right-click menu. Unlike a normal control it isn't part
/// of ControlManager's layout/hit-test list: AbstractWindowBase owns a single
/// instance directly so it can always sit on top and swallow the very next
/// click regardless of what control is underneath.
/// </summary>
internal sealed class ContextMenuOverlay
{
    private const int RowHeight = 22;
    private const int HorizontalPadding = 14;
    private const int MinWidth = 120;

    private readonly IReadOnlyList<ContextMenuItem> _items;
    private readonly SKRect _bounds;
    private int _hoveredIndex = -1;

    /// <summary>x, y and containerW/H are already-scaled pixel coordinates, matching everything else the platform layer reports.</summary>
    public ContextMenuOverlay(int x, int y, IReadOnlyList<ContextMenuItem> items, int containerW, int containerH)
    {
        _items = items;

        using SKFont font = new() { Size = Globals.Rescale(11), Typeface = Fonts.Current.Regular };
        var paddingPx = Globals.Rescale(HorizontalPadding);
        var rowHeightPx = Globals.Rescale(RowHeight);

        var width = Globals.Rescale(MinWidth);
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

    public void Draw(SKCanvas canvas)
    {
        var radius = Globals.RescaleExact(4f);

        using (SKPaint bgPaint = new() { Color = Theme.Current.MenuBackground, IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(_bounds, radius, radius, bgPaint);

        using (SKPaint borderPaint = new() { Color = Theme.Current.MenuBorder, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            canvas.DrawRoundRect(_bounds, radius, radius, borderPaint);

        using SKFont font = new() { Size = Globals.Rescale(11), Typeface = Fonts.Current.Regular };
        var rowH = RowHeightPx;
        var textX = _bounds.Left + Globals.Rescale(HorizontalPadding);

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
                using SKPaint hoverPaint = new() { Color = Theme.Current.MenuItemHoverBackground, IsAntialias = true, Style = SKPaintStyle.Fill };
                canvas.DrawRoundRect(new SKRect(_bounds.Left, rowTop, _bounds.Left + _bounds.Width, rowTop + rowH), radius, radius, hoverPaint);
            }

            using SKPaint textPaint = new()
            {
                Color = item.IsEnabled ? Theme.Current.MenuTextPrimary : Theme.Current.MenuTextDisabled,
                IsAntialias = true
            };
            canvas.DrawTextTopAligned(item.Label, textX, rowTop + ((rowH - textHeight) / 2f), SKTextAlign.Left, font, textPaint);
        }
    }
}
