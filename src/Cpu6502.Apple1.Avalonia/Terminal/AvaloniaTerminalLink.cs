using System.Collections.Concurrent;
using Cpu6502.System.Apple1;
using Cpu6502.System.Terminal;

namespace Cpu6502.Apple1.Avalonia.Terminal;

public sealed class AvaloniaTerminalLink : ITerminalLink, IApple1HostInput
{
    private readonly ConcurrentQueue<byte> _input = new();

    public event EventHandler<byte>? OutputWritten;

    public bool HasInput => !_input.IsEmpty;

    public bool TryReadByte(out byte value) => _input.TryDequeue(out value);

    public void WriteByte(byte value) => OutputWritten?.Invoke(this, value);

    public void EnqueueApple1Key(byte value) => _input.Enqueue(value);

    public void EnqueueText(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        foreach (char c in text)
            _input.Enqueue(Apple1KeyMapper.MapCharacter(c));
    }
}
