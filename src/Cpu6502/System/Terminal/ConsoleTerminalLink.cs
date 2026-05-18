using System;

namespace Cpu6502.System.Terminal;

/// <summary>
/// A console-based implementation of <see cref="ITerminalLink"/> that reads from
/// System.Console.In and writes to System.Console.Out.
/// Used for interactive terminal sessions with the Apple-1 emulator.
/// </summary>
public sealed class ConsoleTerminalLink : ITerminalLink
{
    private readonly TerminalTextEncoding _encoding;
    private bool _hasPendingInput = false;
    private byte? _pendingByte = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleTerminalLink"/> class
    /// with the specified text encoding.
    /// </summary>
    /// <param name="encoding">The text encoding to use for console I/O.</param>
    public ConsoleTerminalLink(TerminalTextEncoding encoding = TerminalTextEncoding.RawBytes)
    {
        _encoding = encoding;
    }

    /// <summary>
    /// Gets a value indicating whether there is input available to read.
    /// Checks for pending buffered byte first, then checks Console.In.
    /// </summary>
    public bool HasInput => _hasPendingInput || Console.KeyAvailable;

    /// <summary>
    /// Attempts to read a single byte from the console input.
    /// This is a non-blocking operation.
    /// </summary>
    /// <param name="value">On success, contains the byte read.</param>
    /// <returns>True if a byte was read; otherwise, false.</returns>
    public bool TryReadByte(out byte value)
    {
        // Check for pending buffered byte first
        if (_hasPendingInput && _pendingByte.HasValue)
        {
            value = _pendingByte.Value;
            _hasPendingInput = false;
            _pendingByte = null;
            return true;
        }

        // Try to read from console
        if (Console.KeyAvailable)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            value = _encoding.Encode(keyInfo.KeyChar.ToString())[0];
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Writes a single byte to the console output.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    public void WriteByte(byte value)
    {
        // Handle special control characters
        if (value == 0x0D) // Carriage Return
        {
            Console.Write('\r');
        }
        else if (value == 0x0A) // Line Feed
        {
            Console.Write('\n');
        }
        else if (value >= 0x20 && value <= 0x7E) // Printable ASCII
        {
            Console.Write((char)value);
        }
        else
        {
            // Non-printable character - could log or ignore
            // For WOZ Monitor, we'll just output the hex value for debugging
            // Console.Write($"[{value:X2}]");
        }
    }

    /// <summary>
    /// Buffers a byte for later reading. Used to inject input programmatically.
    /// </summary>
    /// <param name="value">The byte to buffer.</param>
    public void EnqueueInput(byte value)
    {
        _pendingByte = value;
        _hasPendingInput = true;
    }

    /// <summary>
    /// Enqueues text to the input buffer using the specified encoding.
    /// </summary>
    /// <param name="text">The text to enqueue.</param>
    /// <param name="encoding">The text encoding to use.</param>
    public void EnqueueText(string text, TerminalTextEncoding encoding)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var bytes = encoding.Encode(text);
        foreach (var b in bytes)
        {
            EnqueueInput(b);
        }
    }
}
