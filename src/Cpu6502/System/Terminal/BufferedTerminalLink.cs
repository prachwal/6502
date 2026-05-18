using System.Text;

namespace Cpu6502.System.Terminal;

/// <summary>
/// A buffered implementation of <see cref="ITerminalLink"/> for testing and simple scenarios.
/// Provides FIFO buffers for both input and output.
/// Thread-safety: Safe for single reader/single writer; not safe for concurrent access.
/// </summary>
public sealed class BufferedTerminalLink : ITerminalLink
{
    private readonly Queue<byte> _inputBuffer = new();
    private readonly List<byte> _outputBuffer = new();

    /// <summary>
    /// Gets a value indicating whether there is input available to read.
    /// </summary>
    public bool HasInput => _inputBuffer.Count > 0;

    /// <summary>
    /// Attempts to read a single byte from the input buffer without removing it.
    /// </summary>
    /// <param name="value">On success, contains the byte at the front of the buffer.</param>
    /// <returns>True if a byte is available; otherwise, false.</returns>
    public bool TryPeekByte(out byte value)
    {
        if (_inputBuffer.Count > 0)
        {
            value = _inputBuffer.Peek();
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Attempts to read a single byte from the input buffer.
    /// </summary>
    /// <param name="value">On success, contains the byte read.</param>
    /// <returns>True if a byte was read; otherwise, false.</returns>
    public bool TryReadByte(out byte value)
    {
        if (_inputBuffer.Count > 0)
        {
            value = _inputBuffer.Dequeue();
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Writes a single byte to the output buffer.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public void WriteByte(byte value)
    {
        _outputBuffer.Add(value);
    }

    /// <summary>
    /// Enqueues a single byte to the input buffer.
    /// </summary>
    /// <param name="value">The byte to enqueue.</param>
    public void EnqueueInput(byte value)
    {
        _inputBuffer.Enqueue(value);
    }

    /// <summary>
    /// Enqueues text to the input buffer using the specified encoding.
    /// </summary>
    /// <param name="text">The text to enqueue.</param>
    /// <param name="encoding">The text encoding to use.</param>
    /// <exception cref="ArgumentNullException">Thrown if text is null.</exception>
    public void EnqueueText(string text, TerminalTextEncoding encoding)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var bytes = encoding.Encode(text);
        foreach (var b in bytes)
        {
            _inputBuffer.Enqueue(b);
        }
    }

    /// <summary>
    /// Reads all bytes from the output buffer and clears it.
    /// </summary>
    /// <returns>Array of all output bytes in order.</returns>
    public byte[] ReadAllOutputBytes()
    {
        var result = _outputBuffer.ToArray();
        _outputBuffer.Clear();
        return result;
    }

    /// <summary>
    /// Reads all output as text using the specified encoding and clears the output buffer.
    /// </summary>
    /// <param name="encoding">The text encoding to use.</param>
    /// <returns>The decoded text.</returns>
    public string ReadOutputText(TerminalTextEncoding encoding)
    {
        var bytes = ReadAllOutputBytes();
        return encoding.Decode(bytes);
    }

    /// <summary>
    /// Gets the number of bytes currently in the input buffer.
    /// Useful for testing.
    /// </summary>
    public int InputBufferSize => _inputBuffer.Count;

    /// <summary>
    /// Gets the number of bytes currently in the output buffer.
    /// Useful for testing.
    /// </summary>
    public int OutputBufferSize => _outputBuffer.Count;

    /// <summary>
    /// Clears both input and output buffers.
    /// </summary>
    public void Clear()
    {
        _inputBuffer.Clear();
        _outputBuffer.Clear();
    }
}
