using System.Collections.Concurrent;
using System.Diagnostics;
using Cpu6502.System.Apple1;
using Cpu6502.System.Terminal;

namespace Cpu6502.Apple1.Avalonia.Terminal;

public sealed class AvaloniaTerminalLink : ITerminalLink, IApple1HostInput
{
    private readonly ConcurrentQueue<byte> _input = new();
    private static readonly bool _debugEnabled = true;

    public event EventHandler<byte>? OutputWritten;

    public bool HasInput => !_input.IsEmpty;

    public bool TryReadByte(out byte value)
    {
        bool result = _input.TryDequeue(out value);
        if (_debugEnabled && result)
            Debug.WriteLine($"[TERM IN] {value:X2} ('{(char)(value & 0x7F)}')");
        return result;
    }

    public void ClearInput()
    {
        if (_debugEnabled)
            Debug.WriteLine("[TERM] Input queue cleared");
        while (_input.TryDequeue(out _)) { }
    }

    public void WriteByte(byte value)
    {
        if (_debugEnabled)
            Debug.WriteLine($"[TERM OUT] {value:X2} ('{(char)(value & 0x7F)}')");
        OutputWritten?.Invoke(this, value);
    }

    public void EnqueueApple1Key(byte value)
    {
        if (_debugEnabled)
            Debug.WriteLine($"[TERM ENQUEUE] {value:X2}");
        _input.Enqueue(value);
    }

    public void EnqueueText(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        if (_debugEnabled)
            Debug.WriteLine($"[TERM ENQUEUE TEXT] \"{text.Replace("\r", "\\r").Replace("\n", "\\n")}\"");

        foreach (char c in text)
            _input.Enqueue(Apple1KeyMapper.MapCharacter(c));
    }
}
