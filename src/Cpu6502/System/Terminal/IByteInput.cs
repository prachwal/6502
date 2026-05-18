namespace Cpu6502.System.Terminal;

/// <summary>
/// Represents a source of byte input.
/// Implementations should be thread-safe for single reader/single writer scenarios.
/// </summary>
public interface IByteInput
{
    /// <summary>
    /// Gets a value indicating whether there is at least one byte available to read.
    /// </summary>
    bool HasInput { get; }

    /// <summary>
    /// Attempts to read a single byte from the input.
    /// This is a non-blocking operation.
    /// </summary>
    /// <param name="value">On success, contains the byte read from the input.</param>
    /// <returns>True if a byte was successfully read; otherwise, false.</returns>
    bool TryReadByte(out byte value);
}
