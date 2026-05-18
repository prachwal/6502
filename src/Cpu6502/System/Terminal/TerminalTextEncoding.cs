using System.Text;

namespace Cpu6502.System.Terminal;

/// <summary>
/// Text encoding modes for terminal operations.
/// </summary>
public enum TerminalTextEncoding
{
    /// <summary>No transformation - raw bytes are passed through unchanged.</summary>
    RawBytes,

    /// <summary>Convert letters to uppercase using ASCII encoding.</summary>
    AsciiUppercase,

    /// <summary>
    /// Apple-1 compatible mode: uppercase letters, carriage return line endings.
    /// Note: Bit 7 handling is NOT done here - that is the responsibility of the PIA binding.
    /// </summary>
    Apple1
}

/// <summary>
/// Extension methods for <see cref="TerminalTextEncoding"/>.
/// </summary>
public static class TerminalTextEncodingExtensions
{
    /// <summary>
    /// Encodes text to bytes using the specified encoding.
    /// </summary>
    /// <param name="encoding">The text encoding to use.</param>
    /// <param name="text">The text to encode.</param>
    /// <returns>The encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown encoding values.</exception>
    public static byte[] Encode(this TerminalTextEncoding encoding, string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        return encoding switch
        {
            TerminalTextEncoding.RawBytes => Encoding.ASCII.GetBytes(text),
            TerminalTextEncoding.AsciiUppercase => Encoding.ASCII.GetBytes(text.ToUpperInvariant()),
            TerminalTextEncoding.Apple1 => EncodeApple1(text),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
        };
    }

    /// <summary>
    /// Decodes bytes to text using the specified encoding.
    /// </summary>
    /// <param name="encoding">The text encoding to use.</param>
    /// <param name="bytes">The bytes to decode.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="ArgumentNullException">Thrown if bytes is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown encoding values.</exception>
    public static string Decode(this TerminalTextEncoding encoding, byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        return encoding switch
        {
            TerminalTextEncoding.RawBytes => Encoding.ASCII.GetString(bytes),
            TerminalTextEncoding.AsciiUppercase => Encoding.ASCII.GetString(bytes).ToUpperInvariant(),
            TerminalTextEncoding.Apple1 => DecodeApple1(bytes),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
        };
    }

    /// <summary>
    /// Encodes text for Apple-1 compatibility: uppercase letters, CR line endings.
    /// </summary>
    private static byte[] EncodeApple1(string text)
    {
        var upper = text.ToUpperInvariant();
        var replaced = upper.Replace("\n", "\r");
        return Encoding.ASCII.GetBytes(replaced);
    }

    /// <summary>
    /// Decodes bytes from Apple-1 format: converts CR to newlines.
    /// </summary>
    private static string DecodeApple1(byte[] bytes)
    {
        var text = Encoding.ASCII.GetString(bytes);
        return text.Replace("\r", "\n");
    }
}
