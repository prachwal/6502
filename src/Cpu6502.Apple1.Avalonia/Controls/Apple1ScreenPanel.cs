using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Cpu6502.Apple1.Avalonia.Terminal;
using Cpu6502.System.Apple1;

namespace Cpu6502.Apple1.Avalonia.Controls;

public sealed class Apple1ScreenPanel : Control
{
    private const double CellAspect = 0.62;
    private const double ScreenInset = 14.0;

    private AvaloniaTerminalLink? _terminal;
    private IScreenSource? _screenSource;

    private static readonly Typeface Typeface = new("Cascadia Mono, Consolas, Menlo, Courier New");

    public Apple1ScreenPanel()
    {
        Focusable = true;
    }

    public string ScreenSourceName => _screenSource?.Name ?? "NONE";

    public void Attach(AvaloniaTerminalLink terminal, IScreenSource screenSource)
    {
        if (_screenSource != null)
            _screenSource.Changed -= OnScreenSourceChanged;

        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _screenSource = screenSource ?? throw new ArgumentNullException(nameof(screenSource));
        _screenSource.Changed += OnScreenSourceChanged;
        InvalidateVisual();
    }

    public void Clear()
    {
        _screenSource?.Clear();
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(new SolidColorBrush(Color.Parse("#020402")), Bounds);

        Rect screen = CalculateScreenRect(Bounds).Deflate(ScreenInset);
        context.FillRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Color.Parse("#071007"), 0),
                new GradientStop(Color.Parse("#010301"), 1)
            }
        }, screen);

        double contentLeft = screen.Left + screen.Width * 0.04;
        double contentTop = screen.Top + screen.Height * 0.04;
        double contentWidth = screen.Width * 0.92;
        double contentHeight = screen.Height * 0.90;
        int rows = _screenSource?.Rows ?? 24;
        int columns = _screenSource?.Columns ?? 40;
        double cellHeight = Math.Min(contentHeight / rows, contentWidth / (columns * CellAspect));
        double cellWidth = cellHeight * CellAspect;
        double gridWidth = cellWidth * columns;
        double gridHeight = cellHeight * rows;
        double originX = contentLeft + Math.Max(0, (contentWidth - gridWidth) / 2);
        double originY = contentTop;
        double fontSize = Math.Floor(cellHeight * 0.76);

        var textBrush = new SolidColorBrush(Color.Parse("#7cff7a"));
        var cursorBrush = new SolidColorBrush(Color.Parse("#b8ffb2"));
        var glowBrush = new SolidColorBrush(Color.Parse("#2a8f35"));

        DrawScanlines(context, screen, cellHeight);

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                char ch = _screenSource?.GetCell(row, column) ?? ' ';
                if (ch == ' ')
                    continue;

                var x = originX + column * cellWidth;
                var y = originY + row * cellHeight;
                DrawGlyph(context, ch, x, y, cellWidth, cellHeight, fontSize, glowBrush, textBrush);
            }
        }

        DrawCursor(context, originX, originY, cellWidth, cellHeight, fontSize, cursorBrush);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_terminal == null)
            return;

        byte? value = e.Key switch
        {
            Key.Enter when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Enter, out byte mapped) => mapped,
            Key.Escape when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Escape, out byte mapped) => mapped,
            Key.Back when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Backspace, out byte mapped) => mapped,
            Key.Delete when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Delete, out byte mapped) => mapped,
            Key.Left when Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.LeftArrow, out byte mapped) => mapped,
            _ => null
        };

        if (value.HasValue)
        {
            _terminal.EnqueueApple1Key(value.Value);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (_terminal == null || string.IsNullOrEmpty(e.Text))
            return;

        foreach (char c in e.Text)
        {
            if (Apple1KeyMapper.TryMapPrintableCharacter(c, out byte value))
                _terminal.EnqueueApple1Key(value);
        }

        e.Handled = true;
        base.OnTextInput(e);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
    }

    private void OnScreenSourceChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(InvalidateVisual);
    }

    private static Rect CalculateScreenRect(Rect bounds)
    {
        double preferredAspect = 4.0 / 3.0;
        double width = bounds.Width;
        double height = bounds.Height;

        if (width <= 0 || height <= 0)
            return bounds;

        if (width / height > preferredAspect)
            width = height * preferredAspect;
        else
            height = width / preferredAspect;

        return new Rect(
            bounds.Left + (bounds.Width - width) / 2,
            bounds.Top + (bounds.Height - height) / 2,
            width,
            height);
    }

    private static void DrawScanlines(DrawingContext context, Rect screen, double cellHeight)
    {
        var scanlineBrush = new SolidColorBrush(Color.FromArgb(18, 0, 0, 0));
        for (double y = screen.Top; y < screen.Bottom; y += cellHeight * 2)
            context.FillRectangle(scanlineBrush, new Rect(screen.Left, y, screen.Width, 1));
    }

    private static void DrawGlyph(
        DrawingContext context,
        char ch,
        double x,
        double y,
        double cellWidth,
        double cellHeight,
        double fontSize,
        IBrush glowBrush,
        IBrush textBrush)
    {
        var origin = new Point(x + cellWidth * 0.08, y + cellHeight * 0.02);
        var formatted = new FormattedText(
            ch.ToString(),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface,
            fontSize,
            glowBrush);

        context.DrawText(formatted, new Point(origin.X + 1, origin.Y + 1));
        context.DrawText(formatted, origin);

        var finalText = new FormattedText(
            ch.ToString(),
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface,
            fontSize,
            textBrush);
        context.DrawText(finalText, origin);
    }

    private void DrawCursor(
        DrawingContext context,
        double originX,
        double originY,
        double cellWidth,
        double cellHeight,
        double fontSize,
        IBrush cursorBrush)
    {
        var x = originX + (_screenSource?.CursorColumn ?? 0) * cellWidth;
        var y = originY + (_screenSource?.CursorRow ?? 0) * cellHeight;
        var cursorText = new FormattedText(
            "@",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            Typeface,
            fontSize,
            cursorBrush);

        context.DrawText(cursorText, new Point(x + cellWidth * 0.08, y + cellHeight * 0.02));
    }
}
