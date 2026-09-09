using SkiaSharp;
using TTraxx.PluGui.Gui.Helpers;
using TTraxx.PluGui.Gui.Helpers.Extensions;
using TTraxx.PluGui.Gui.Helpers.Theming;
using TTraxx.PluGui.Gui.Helpers.Typography;
using TTraxx.PluGui.Gui.Panels.Base;
using TTraxx.PluGui.Gui.Panels.Configuration;

namespace TTraxx.PluGui.Gui.Panels;

/// <summary>
/// A "brushed metal" panel with a title and arrow icon. Instantiate once
/// per panel and call Draw every frame.
/// </summary>
public sealed class MetallicPluginPanel(PluginPanelConfiguration config) : AbstractPluginPanelBase(config)
{
    public override void Draw(SKCanvas canvas)
    {
        canvas.FillRoundRectVerticalGradient(Left, FromTop(-24), Width, Globals.RescaleExact(34), CornerRadius, Theme.CurrentMetallic.PanelMetalHeaderHighlight, Theme.CurrentMetallic.PanelMetalHeaderShadow);

        using SKPaint headerBorderPaint = new()
        {
            Color = Theme.CurrentMetallic.PanelMetalBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Globals.RescaleExact(0.3f),
            IsAntialias = true
        };
        canvas.DrawRoundRect(Left, FromTop(-24), Width, Globals.RescaleExact(34), CornerRadius, CornerRadius, headerBorderPaint);

        canvas.FillRoundRectVerticalGradient(Left, Top, Width, Height, CornerRadius, Theme.CurrentMetallic.PanelMetalTop, Theme.CurrentMetallic.PanelMetalBottom);

        using SKPaint borderPaint = new()
        {
            Color = Theme.CurrentMetallic.PanelMetalBorder,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = Globals.RescaleExact(0.3f),
            IsAntialias = true
        };
        canvas.DrawRoundRect(Left, Top, Width, Height, CornerRadius, CornerRadius, borderPaint);

        using SKPaint sheenPaint = new()
        {
            Color = Theme.CurrentMetallic.PanelMetalSheen,
            StrokeWidth = Globals.RescaleExact(0.2f),
            IsAntialias = true
        };
        canvas.DrawLine(FromLeft(8), FromTop(3), FromRight(8), FromTop(3), sheenPaint);

        canvas.DrawIconFill(Icons.ArrowRight, FromLeft(27), FromTop(-12), Globals.RescaleExact(8), Theme.Current.TextPrimary);

        using SKPaint titlePaint = new()
        {
            Color = Theme.Current.TextPrimary,
            IsAntialias = true
        };
        using SKFont titleFont = new()
        {
            Size = Globals.RescaleExact(11),
            Typeface = Fonts.Current.Bold
        };
        canvas.DrawTextTopAligned(_config.Title, FromLeft(35), FromTop(-20), SKTextAlign.Left, titleFont, titlePaint);
    }

    /// <summary>
    /// "──── LABEL ────"-style section divider within this panel. y is
    /// logical, like the configuration dimensions, and gets rescaled here.
    /// </summary>
    public void DrawSectionDivider(SKCanvas canvas, int relativeY, string text)
    {
        var left = FromLeft(16);
        var right = FromRight(16);
        var lineY = FromTop(relativeY);

        using SKPaint textPaint = new()
        {
            Color = Theme.Current.TextDim,
            IsAntialias = true
        };
        using SKFont textFont = new()
        {
            Size = Globals.RescaleExact(9),
            Typeface = Fonts.Current.Bold
        };

        var textWidth = textFont.MeasureText(text, out _);
        var textPad = Globals.RescaleExact(8);
        var centerX = Left + (Width / 2f);
        var textLeft = centerX - (textWidth / 2f);
        var textRight = centerX + (textWidth / 2f);

        using SKPaint linePaint = new()
        {
            Color = Theme.Current.TextDim,
            StrokeWidth = Globals.RescaleExact(0.5f),
            IsAntialias = true
        };
        canvas.DrawLine(left, lineY, textLeft - textPad, lineY, linePaint);
        canvas.DrawLine(textRight + textPad, lineY, right, lineY, linePaint);

        canvas.DrawTextTopAligned(text, centerX, lineY - (textFont.Size / 2f), SKTextAlign.Center, textFont, textPaint);
    }
}