namespace Cpu6502.System.Profiles;

/// <summary>
/// Describes how a device is mapped to the address/port space.
/// </summary>
/// <param name="Kind">The kind of mapping (memory or port).</param>
/// <param name="BaseAddress">The base address or port number for the mapping.</param>
/// <param name="Size">The size of the mapped region in bytes.</param>
public sealed record DeviceMappingProfile(
    AddressSpaceKind Kind,
    string BaseAddress,
    string Size)
{
    private uint? _parsedBaseAddress;
    private uint? _parsedSize;

    /// <summary>
    /// Parsed base address.
    /// </summary>
    public uint ParsedBaseAddress => _parsedBaseAddress ??= AddressParser.Parse(BaseAddress);

    /// <summary>
    /// Parsed size in bytes.
    /// </summary>
    public uint ParsedSize => _parsedSize ??= AddressParser.Parse(Size);

    /// <summary>
    /// End address (exclusive) of this mapping.
    /// </summary>
    public uint EndAddress => ParsedBaseAddress + ParsedSize;

    /// <summary>
    /// Validates the mapping profile.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the size is 0.</exception>
    public void Validate(string deviceId, string profileId)
    {
        // These will throw FormatException if invalid
        _ = ParsedBaseAddress;
        _ = ParsedSize;

        if (ParsedSize == 0)
            throw new ArgumentOutOfRangeException(
                nameof(Size),
                $"Device mapping size cannot be 0. Device: {deviceId}, Profile: {profileId}");
    }
}
