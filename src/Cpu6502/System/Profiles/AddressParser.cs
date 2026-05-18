using System;
using System.Globalization;

namespace Cpu6502.System.Profiles;

/// <summary>
/// Parser for address values in hex (0x...) or decimal format.
/// Used for parsing address values from computer profiles.
/// </summary>
public static class AddressParser
{
    /// <summary>
    /// Parses an address string in hex (0x1234, 0X1234) or decimal (1234) format.
    /// </summary>
    /// <param name="address">The address string to parse.</param>
    /// <returns>The parsed unsigned integer address.</returns>
    /// <exception cref="FormatException">Thrown when the address string is not in a recognized format.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the address string is null or empty.</exception>
    public static uint Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentNullException(nameof(address), "Address string cannot be null or empty.");

        address = address.Trim();

        // Try hex with 0x prefix
        if (address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (uint.TryParse(address[2..], NumberStyles.HexNumber, null, out var result))
                return result;
            throw new FormatException($"Invalid hex address format: '{address}'. Expected format like '0xD010' or '0XFF00'.");
        }

        // Try decimal
        if (uint.TryParse(address, NumberStyles.None, null, out var decimalResult))
            return decimalResult;

        throw new FormatException($"Invalid address format: '{address}'. Expected hex (0x1234) or decimal (1234) format.");
    }

    /// <summary>
    /// Parses an optional address string, returning 0 if null, empty, or whitespace.
    /// </summary>
    /// <param name="address">The address string to parse.</param>
    /// <returns>The parsed unsigned integer address, or 0 if the input is null/empty.</returns>
    public static uint ParseOrDefault(string? address) =>
        string.IsNullOrWhiteSpace(address) ? 0 : Parse(address);

    /// <summary>
    /// Tries to parse an address string without throwing exceptions.
    /// </summary>
    /// <param name="address">The address string to parse.</param>
    /// <param name="result">The parsed address if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string? address, out uint result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        try
        {
            result = Parse(address);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
