namespace Cpu6502.Apple1.Avalonia.Terminal;

public interface IScreenSource
{
    event EventHandler? Changed;

    string Name { get; }
    int Columns { get; }
    int Rows { get; }
    int CursorColumn { get; }
    int CursorRow { get; }

    char GetCell(int row, int column);
    string GetSnapshotText(bool includeCursor = false);
    void Clear();
    void Refresh();
}
