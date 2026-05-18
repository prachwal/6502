namespace Cpu6502.System.Profiles;

/// <summary>
/// Profile describing the address space characteristics of a computer.
/// </summary>
/// <param name="MemoryAddressBits">Number of bits for memory address space (e.g., 16 = 64KB).</param>
/// <param name="PortAddressBits">Number of bits for port address space (0 = no separate port space).</param>
/// <param name="HasSeparatePortSpace">Whether the CPU has separate memory and port address spaces.</param>
/// <param name="DataBusBits">Number of bits in the data bus (typically 8).</param>
public sealed record AddressSpaceProfile(
    int MemoryAddressBits = 16,
    int PortAddressBits = 0,
    bool HasSeparatePortSpace = false,
    int DataBusBits = 8)
{
    /// <summary>
    /// Creates a default 6502-style address space profile (16-bit memory, no ports).
    /// </summary>
    public static AddressSpaceProfile Mos6502 => new(16, 0, false, 8);

    /// <summary>
    /// Creates a Z80-style address space profile (16-bit memory, 8-bit ports).
    /// </summary>
    public static AddressSpaceProfile Z80 => new(16, 8, true, 8);

    /// <summary>
    /// Validates the profile.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when address bits are invalid.</exception>
    public void Validate(string profileId)
    {
        if (MemoryAddressBits < 1 || MemoryAddressBits > 32)
            throw new ArgumentOutOfRangeException(
                nameof(MemoryAddressBits),
                $"MemoryAddressBits must be between 1 and 32. Profile: {profileId}");

        if (PortAddressBits < 0 || PortAddressBits > 32)
            throw new ArgumentOutOfRangeException(
                nameof(PortAddressBits),
                $"PortAddressBits must be between 0 and 32. Profile: {profileId}");

        if (DataBusBits < 1 || DataBusBits > 64)
            throw new ArgumentOutOfRangeException(
                nameof(DataBusBits),
                $"DataBusBits must be between 1 and 64. Profile: {profileId}");

        // If there are port bits, there must be separate port space
        if (PortAddressBits > 0 && !HasSeparatePortSpace)
            throw new ArgumentOutOfRangeException(
                nameof(HasSeparatePortSpace),
                $"PortAddressBits > 0 requires HasSeparatePortSpace = true. Profile: {profileId}");
    }
}
