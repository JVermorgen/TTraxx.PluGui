using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// A box showing a stepped parameter's current choice by name, with a chevron. A click opens the
/// whole list in the window's popup menu, the current choice marked; picking a row writes that
/// position in one bracketed edit. The wheel steps through the choices without opening the list,
/// and right-click offers "Reset to Default".
/// <para>
/// No double-click reset, unlike a knob: the first click of a double-click has already opened the
/// list and the second has closed it again, so a reset on top would fire on a gesture the user
/// meant as "open, then change my mind".
/// </para>
/// </summary>
public sealed class DropdownControl(DropdownControlConfiguration config) : PluginControl(config)
{
    private ParameterBinding Parameter => config.Parameter;
    private DropdownStyle Style => config.ResolveStyle();

    /// <summary>Choices on offer - the named ones. Can be fewer than <see cref="Positions"/>.</summary>
    private int Count => Math.Min(config.Items.Count, Positions);

    /// <summary>
    /// Positions the parameter has. Normally the same as the number of names, but a list that is
    /// expected to grow can reserve spare positions up front: values are mapped by position count,
    /// not name count, so appending a name later doesn't change what an existing stored value means.
    /// </summary>
    private int Positions => Parameter.Info.PositionCount ?? config.Items.Count;

    /// <summary>The current position. May be a reserved, unnamed one if the host set it there.</summary>
    private int SelectedIndex => Positions <= 1 ? 0 : Math.Clamp((int)Math.Round(Parameter.Normalized * (Positions - 1)), 0, Positions - 1);

    private double ToNormalized(int index) => Positions <= 1 ? 0.0 : (double)index / (Positions - 1);

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled || Count == 0) return;

        var selected = SelectedIndex;
        var items = new List<ContextMenuItem>(Count);
        for (var i = 0; i < Count; i++)
        {
            var index = i;
            items.Add(new ContextMenuItem(config.Items[i], () =>
            {
                Parameter.Edit(ToNormalized(index));
                Refresh();
            }, IsEnabled: true, IsChecked: i == selected));
        }

        // Directly below the box, left edges aligned, the way a native dropdown opens.
        ShowMenu(0, _h, items);
    }

    public override void OnWheel(WheelEventArgs e)
    {
        if (!IsEnabled || Count <= 1) return;

        // Stays within the named choices: the wheel never lands on a reserved position.
        var next = Math.Clamp(SelectedIndex + e.Delta, 0, Count - 1);
        Parameter.Edit(ToNormalized(next));
        Refresh();
    }

    public override IParameterControlInfo? GetParameterInfo(int localX, int localY) => Parameter.Info;

    public override IReadOnlyList<ContextMenuItem> GetContextMenuItems() =>
    [
        new("Reset to Default", () =>
        {
            Parameter.Edit(Parameter.Quantize(Parameter.Info.DefaultNormalizedValue));
            Refresh();
        }, IsEnabled)
    ];

    public override void Draw(SKCanvas canvas)
    {
        var style = Style;
        var enabled = IsEnabled;
        var radius = RescaleExact(style.CornerRadius);
        var box = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        using (SKPaint fill = new() { Color = enabled ? Theme.TrackBackground : Theme.TrackBackground.WithAlpha(120), IsAntialias = true, Style = SKPaintStyle.Fill })
            canvas.DrawRoundRect(box, radius, radius, fill);

        var borderColor = !enabled ? Theme.AccentDim.WithAlpha(60) : IsHovered ? Theme.Accent : Theme.AccentDim;
        using (SKPaint border = new() { Color = borderColor, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) })
            canvas.DrawRoundRect(box, radius, radius, border);

        var padding = Rescale(style.Padding);
        var chevronW = RescaleExact(style.ChevronWidth);
        var chevronRight = _w - padding;
        var centerY = _h / 2f;

        using (SKPathBuilder builder = new())
        {
            builder.MoveTo(chevronRight - chevronW, centerY - (chevronW / 4f));
            builder.LineTo(chevronRight, centerY - (chevronW / 4f));
            builder.LineTo(chevronRight - (chevronW / 2f), centerY + (chevronW / 4f));
            builder.Close();
            using var chevron = builder.Detach();
            using SKPaint chevronPaint = new() { Color = enabled ? Theme.TextDim : Theme.TextDisabled, IsAntialias = true, Style = SKPaintStyle.Fill };
            canvas.DrawPath(chevron, chevronPaint);
        }

        if (Count == 0) return;

        // A reserved position has no name yet; show it as a dash rather than fall over.
        var selected = SelectedIndex;
        var label = selected < Count ? config.Items[selected] : "-";

        using SKFont font = new() { Size = RescaleExact(style.FontSize), Typeface = Fonts.Regular };
        using SKPaint textPaint = new() { Color = enabled ? Theme.TextPrimary : Theme.TextDisabled, IsAntialias = true };

        // Centred on the glyph box (ascent + descent), not the nominal font size - see ContextMenuOverlay.
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;

        // A name too long for the box is cut off at the chevron rather than running under it.
        canvas.Save();
        canvas.ClipRect(new SKRect(padding, 0, chevronRight - chevronW - Rescale(3), _h));
        canvas.DrawTextTopAligned(label, padding, textTop, SKTextAlign.Left, font, textPaint);
        canvas.Restore();
    }
}
