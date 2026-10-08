using SkiaSharp;

namespace TTraxx.PluGui.Gui;

/// <summary>
/// <para>
/// A single-line text field. A click takes the keyboard (see PluginControl.TakeKeyboardFocus) with
/// the caret where the click was - a double-click, or a focus from
/// <see cref="TextFieldControlConfiguration.FocusRequest"/>, selects it all - and a drag selects.
/// While it has the keyboard: typing replaces the selection; Left/Right/Home/End move the caret
/// (Shift extends the selection); Backspace/Delete; Ctrl+A, Ctrl+C, Ctrl+X, Ctrl+V; Enter commits
/// and Escape cancels, each giving the keyboard back. A press elsewhere ends the editing too,
/// keeping the text.
/// </para>
/// <para>
/// Keys only reach it where the platform can take the keyboard - Windows so far.
/// </para>
/// </summary>
public sealed class TextFieldControl : PluginControl
{
    private const float PaddingX = 6f;

    private readonly TextFieldControlConfiguration _config;
    private string _text = "";
    private int _caret;
    private int _anchor;
    private float _scroll;
    private bool _selecting;

    public TextFieldControl(TextFieldControlConfiguration config) : base(config)
    {
        _config = config;
        if (config.FocusRequest is { } request) request.Requested += FocusAndSelectAll;
    }

    /// <summary>The text shown: the copy being edited, or the owner's while not editing.</summary>
    private string Text => HasKeyboardFocus ? _text : _config.GetText();

    public override void OnKeyboardFocusChanged(bool focused)
    {
        if (focused)
        {
            _text = _config.GetText();
            _caret = _anchor = _text.Length;
        }

        _selecting = false;
        _scroll = 0;
    }

    public override void OnPointerDown(PointerEventArgs e)
    {
        if (!IsEnabled) return;

        var wasFocused = HasKeyboardFocus;
        TakeKeyboardFocus();
        var index = IndexAt(e.X);
        _caret = index;
        if (!wasFocused || (e.Modifiers & KeyModifiers.Shift) == 0) _anchor = index;
        _selecting = true;
        Refresh();
    }

    public override void OnPointerMove(PointerEventArgs e)
    {
        if (!_selecting) return;

        _caret = IndexAt(e.X);
        Refresh();
    }

    public override void OnPointerUp(PointerEventArgs e) => _selecting = false;

    public override void OnDoubleClick(PointerEventArgs e)
    {
        if (!HasKeyboardFocus) return;

        _anchor = 0;
        _caret = _text.Length;
        Refresh();
    }

    public override bool OnTextInput(string text)
    {
        if (!HasKeyboardFocus) return false;

        Insert(text);
        return true;
    }

    public override bool OnKeyDown(KeyEventArgs e)
    {
        if (!HasKeyboardFocus) return false;

        var shift = (e.Modifiers & KeyModifiers.Shift) != 0;
        var control = (e.Modifiers & KeyModifiers.Control) != 0;
        switch (e.Key)
        {
            case Key.Left:
                MoveCaret(HasSelection && !shift ? SelectionStart : _caret - 1, shift);
                return true;
            case Key.Right:
                MoveCaret(HasSelection && !shift ? SelectionEnd : _caret + 1, shift);
                return true;
            case Key.Home:
                MoveCaret(0, shift);
                return true;
            case Key.End:
                MoveCaret(_text.Length, shift);
                return true;
            case Key.Backspace:
                if (!HasSelection && _caret > 0) _anchor = _caret - 1;
                Insert("");
                return true;
            case Key.Delete:
                if (!HasSelection && _caret < _text.Length) _anchor = _caret + 1;
                Insert("");
                return true;
            case Key.A when control:
                _anchor = 0;
                _caret = _text.Length;
                return true;
            case Key.C when control:
                if (HasSelection) SetClipboardText(Selected);
                return true;
            case Key.X when control:
                if (HasSelection)
                {
                    SetClipboardText(Selected);
                    Insert("");
                }

                return true;
            case Key.V when control:
                if (GetClipboardText() is { } pasted) Insert(pasted.ReplaceLineEndings(" "));
                return true;
            case Key.Enter:
                var committed = _text;
                ReleaseKeyboardFocus();
                _config.OnCommit?.Invoke(committed);
                return true;
            case Key.Escape:
                ReleaseKeyboardFocus();
                _config.OnCancel?.Invoke();
                return true;
            default:
                return e.Key != Key.Tab && e.Key != Key.None; // let Tab and keys it doesn't use go to the host
        }
    }

    public override void Draw(SKCanvas canvas)
    {
        var focused = HasKeyboardFocus;
        var enabled = IsEnabled;
        var radius = RescaleExact(3f);
        var box = new SKRect(0.5f, 0.5f, _w - 0.5f, _h - 0.5f);

        using (SKPaint background = new() { Color = Theme.TrackBackground.WithAlpha(enabled ? (byte)220 : (byte)110), IsAntialias = true })
            canvas.DrawRoundRect(box, radius, radius, background);

        if (focused || (IsHovered && enabled))
        {
            using SKPaint outline = new() { Color = Theme.Accent.WithAlpha(focused ? (byte)200 : (byte)110), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = RescaleExact(1f) };
            canvas.DrawRoundRect(box, radius, radius, outline);
        }

        using SKFont font = Font();
        var metrics = font.Metrics;
        var textTop = (_h - (metrics.Descent - metrics.Ascent)) / 2f;
        var left = RescaleExact(PaddingX);
        var text = Text;

        canvas.Save();
        canvas.ClipRect(new SKRect(left - 1, 0, _w - left + 1, _h));

        if (text.Length == 0)
        {
            using SKPaint placeholder = new() { Color = Theme.TextDisabled, IsAntialias = true };
            canvas.DrawTextTopAligned(_config.Placeholder, left, textTop, SKTextAlign.Left, font, placeholder);
        }

        if (focused)
        {
            KeepCaretInView(font);
            var origin = left - _scroll;
            if (HasSelection)
            {
                var from = origin + font.MeasureText(text.AsSpan(0, SelectionStart));
                var to = origin + font.MeasureText(text.AsSpan(0, SelectionEnd));
                using SKPaint selection = new() { Color = Theme.Accent.WithAlpha(90), IsAntialias = true };
                canvas.DrawRect(new SKRect(from, textTop - 1, to, textTop + (metrics.Descent - metrics.Ascent) + 1), selection);
            }

            using SKPaint textPaint = new() { Color = Theme.TextPrimary, IsAntialias = true };
            canvas.DrawTextTopAligned(text, origin, textTop, SKTextAlign.Left, font, textPaint);

            var caretX = MathF.Round(origin + font.MeasureText(text.AsSpan(0, _caret))) + 0.5f;
            using SKPaint caret = new() { Color = Theme.Accent, StrokeWidth = RescaleExact(1f) };
            canvas.DrawLine(caretX, textTop - 1, caretX, textTop + (metrics.Descent - metrics.Ascent) + 1, caret);
        }
        else if (text.Length > 0)
        {
            using SKPaint textPaint = new() { Color = enabled ? Theme.TextPrimary : Theme.TextDisabled, IsAntialias = true };
            canvas.DrawTextTopAligned(text, left, textTop, SKTextAlign.Left, font, textPaint);
        }

        canvas.Restore();
    }

    private bool HasSelection => _anchor != _caret;

    private int SelectionStart => Math.Min(_anchor, _caret);

    private int SelectionEnd => Math.Max(_anchor, _caret);

    private string Selected => _text[SelectionStart..SelectionEnd];

    private SKFont Font() => new() { Size = RescaleExact(_config.FontSize), Typeface = Fonts.Regular };

    private void FocusAndSelectAll()
    {
        if (!IsEnabled || !IsVisible) return;

        TakeKeyboardFocus();
        _anchor = 0;
        _caret = _text.Length;
        Refresh();
    }

    // Replaces the selection (or inserts at the caret) - typing, pasting, deleting.
    private void Insert(string text)
    {
        var room = _config.MaxLength - (_text.Length - (SelectionEnd - SelectionStart));
        if (text.Length > room) text = text[..Math.Max(0, room)];

        var start = SelectionStart;
        _text = string.Concat(_text.AsSpan(0, start), text, _text.AsSpan(SelectionEnd));
        _caret = _anchor = start + text.Length;
        _config.OnTextChanged?.Invoke(_text);
    }

    private void MoveCaret(int index, bool extend)
    {
        _caret = Math.Clamp(index, 0, _text.Length);
        if (!extend) _anchor = _caret;
    }

    // The character boundary nearest a local x.
    private int IndexAt(float x)
    {
        using var font = Font();
        var offset = x - RescaleExact(PaddingX) + _scroll;
        var text = _text;
        for (var index = 0; index < text.Length; index++)
        {
            var before = font.MeasureText(text.AsSpan(0, index));
            var after = font.MeasureText(text.AsSpan(0, index + 1));
            if (offset < (before + after) / 2f) return index;
        }

        return text.Length;
    }

    // Scrolls the text sideways so the caret stays inside the box.
    private void KeepCaretInView(SKFont font)
    {
        var width = _w - (2 * RescaleExact(PaddingX));
        var caret = font.MeasureText(_text.AsSpan(0, _caret));
        if (caret - _scroll > width) _scroll = caret - width;
        if (caret - _scroll < 0) _scroll = caret;
        _scroll = Math.Max(0, Math.Min(_scroll, Math.Max(0, font.MeasureText(_text) - width)));
    }
}
