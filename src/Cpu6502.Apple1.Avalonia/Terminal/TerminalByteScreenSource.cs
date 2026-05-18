namespace Cpu6502.Apple1.Avalonia.Terminal;

public sealed class TerminalByteScreenSource : IScreenSource, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly AvaloniaTerminalLink _terminal;
    private readonly Apple1ScreenBuffer _buffer;

    public TerminalByteScreenSource(AvaloniaTerminalLink terminal, int columns = 40, int rows = 24)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _buffer = new Apple1ScreenBuffer(columns, rows);
        _terminal.OutputWritten += OnOutputWritten;
    }

    public event EventHandler? Changed;

    public string Name => "PIA";
    public int Columns => _buffer.Columns;
    public int Rows => _buffer.Rows;
    public int CursorColumn
    {
        get
        {
            lock (_syncRoot)
                return _buffer.CursorColumn;
        }
    }

    public int CursorRow
    {
        get
        {
            lock (_syncRoot)
                return _buffer.CursorRow;
        }
    }

    public char GetCell(int row, int column)
    {
        lock (_syncRoot)
            return _buffer.GetCell(row, column);
    }

    public string GetSnapshotText(bool includeCursor = false)
    {
        lock (_syncRoot)
            return _buffer.ToDisplayText(includeCursor);
    }

    public void Clear()
    {
        lock (_syncRoot)
            _buffer.Clear();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Refresh() => Changed?.Invoke(this, EventArgs.Empty);

    public void Dispose() => _terminal.OutputWritten -= OnOutputWritten;

    private void OnOutputWritten(object? sender, byte value)
    {
        lock (_syncRoot)
            _buffer.Write(value);

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
