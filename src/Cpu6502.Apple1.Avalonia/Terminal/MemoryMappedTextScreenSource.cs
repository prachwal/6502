using System.Text;

namespace Cpu6502.Apple1.Avalonia.Terminal;

public sealed class MemoryMappedTextScreenSource : IScreenSource
{
    private readonly Func<uint, byte> _readMemory;
    private readonly uint? _startAddress;
    private readonly MemoryScreenEncoding _encoding;
    private readonly char[,] _cells;

    public MemoryMappedTextScreenSource(
        Func<uint, byte> readMemory,
        uint? startAddress,
        int columns,
        int rows,
        MemoryScreenEncoding encoding = MemoryScreenEncoding.Ascii)
    {
        _readMemory = readMemory ?? throw new ArgumentNullException(nameof(readMemory));
        _startAddress = startAddress;
        _encoding = encoding;

        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows));

        Columns = columns;
        Rows = rows;
        _cells = new char[rows, columns];
        ClearCells();
    }

    public event EventHandler? Changed;

    public string Name => "MEM";
    public int Columns { get; }
    public int Rows { get; }
    public int CursorColumn => 0;
    public int CursorRow => 0;

    public char GetCell(int row, int column) => _cells[row, column];

    public string GetSnapshotText(bool includeCursor = false)
    {
        var builder = new StringBuilder((Columns + Environment.NewLine.Length) * Rows);

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                if (includeCursor && row == CursorRow && column == CursorColumn)
                    builder.Append('_');
                else
                    builder.Append(_cells[row, column]);
            }

            if (row < Rows - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    public void Clear()
    {
        ClearCells();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Refresh()
    {
        if (!_startAddress.HasValue)
            throw new InvalidOperationException("Memory screen source requires an explicit start address.");

        uint address = _startAddress.Value;
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                _cells[row, column] = Decode(_readMemory(address));
                address++;
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private char Decode(byte value)
    {
        byte displayValue = _encoding == MemoryScreenEncoding.Apple1HighBitAscii
            ? (byte)(value & 0x7F)
            : value;

        return displayValue is >= 0x20 and <= 0x7E ? (char)displayValue : ' ';
    }

    private void ClearCells()
    {
        for (int row = 0; row < Rows; row++)
        for (int column = 0; column < Columns; column++)
            _cells[row, column] = ' ';
    }
}
