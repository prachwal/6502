namespace Cpu6502.System.Terminal;

/// <summary>
/// Represents a destination for byte output.
/// Implementations should be thread-safe for single writer scenarios.
/// </summary>
public interface IByteOutput
{
    /// <summary>
    /// Writes a single byte to the output.
    /// </summary>
    /// <param name="value">The byte to write.</param>
    void WriteByte(byte value);
}
