using System.Text;

namespace Cpu6502.Apple1.Avalonia.Terminal;

public sealed class Apple1ScreenBuffer
{
    private readonly char[,] _cells;

    public Apple1ScreenBuffer(int columns = 40, int rows = 24)
    {
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows <= 0)
            throw new ArgumentOutOfRangeException(nameof(rows));

        Columns = columns;
        Rows = rows;
        _cells = new char[rows, columns];
        Clear();
    }

    public int Columns { get; }
    public int Rows { get; }
    public int CursorColumn { get; private set; }
    public int CursorRow { get; private set; }

    public void Clear()
    {
        for (int row = 0; row < Rows; row++)
        for (int column = 0; column < Columns; column++)
            _cells[row, column] = ' ';

        CursorColumn = 0;
        CursorRow = 0;
    }

    public void Write(byte value)
    {
        if (value == 0xA3 || value == 0x23)
        {
            PutChar('.');
            return;
        }

        byte displayValue = (byte)(value & 0x7F);

        if (displayValue == 0x0D || displayValue == 0x0A)
        {
            NewLine();
            return;
        }

        if (displayValue < 0x20 || displayValue > 0x7E)
            return;

        PutChar((char)displayValue);
    }

    private void PutChar(char ch)
    {
        _cells[CursorRow, CursorColumn] = ch;
        CursorColumn++;
        if (CursorColumn >= Columns)
            NewLine();
    }

    public string ToDisplayText(bool includeCursor = true)
    {
        var builder = new StringBuilder((Columns + Environment.NewLine.Length) * Rows);

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                if (includeCursor && row == CursorRow && column == CursorColumn)
                    builder.Append('@');
                else
                    builder.Append(_cells[row, column]);
            }

            if (row < Rows - 1)
                builder.AppendLine();
        }

        return builder.ToString();
    }

    public char GetCell(int row, int column) => _cells[row, column];

    private void NewLine()
    {
        CursorColumn = 0;
        CursorRow++;

        if (CursorRow < Rows)
            return;

        ScrollUp();
        CursorRow = Rows - 1;
    }

    private void ScrollUp()
    {
        for (int row = 1; row < Rows; row++)
        for (int column = 0; column < Columns; column++)
            _cells[row - 1, column] = _cells[row, column];

        for (int column = 0; column < Columns; column++)
            _cells[Rows - 1, column] = ' ';
    }
}
