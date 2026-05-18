namespace Cpu6502.System.Profiles;

/// <summary>
/// Base record for memory regions (RAM or ROM).
/// </summary>
/// <param name="Id">Unique identifier for this memory region.</param>
/// <param name="Start">Start address of the region.</param>
/// <param name="Size">Size of the region in bytes.</param>
public record MemoryRegionProfile(
    string Id,
    string Start,
    string Size)
{
    private uint? _parsedStart;
    private uint? _parsedSize;

    /// <summary>
    /// Parsed start address.
    /// </summary>
    public uint ParsedStart => _parsedStart ??= AddressParser.Parse(Start);

    /// <summary>
    /// Parsed size in bytes.
    /// </summary>
    public uint ParsedSize => _parsedSize ??= AddressParser.Parse(Size);

    /// <summary>
    /// End address (exclusive) of this region.
    /// </summary>
    public uint EndAddress => ParsedStart + ParsedSize;

    /// <summary>
    /// Validates the region.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when Id is null or empty.</exception>
    public void Validate(string profileId)
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentNullException(
                nameof(Id),
                $"Memory region Id cannot be null or empty. Profile: {profileId}");

        // These will throw FormatException if invalid
        _ = ParsedStart;
        _ = ParsedSize;

        if (ParsedSize == 0)
            throw new ArgumentOutOfRangeException(
                nameof(Size),
                $"Memory region size cannot be 0. Region: {Id}, Profile: {profileId}");
    }
}

/// <summary>
/// Profile for a RAM memory region.
/// </summary>
/// <param name="Id">Unique identifier for this RAM region.</param>
/// <param name="Start">Start address of the RAM region.</param>
/// <param name="Size">Size of the RAM region in bytes.</param>
/// <param name="FillValue">Optional initial fill value (defaults to 0).</param>
public sealed record RamRegionProfile(
    string Id,
    string Start,
    string Size,
    byte FillValue = 0) : MemoryRegionProfile(Id, Start, Size)
{
    /// <summary>
    /// Creates a RAM region with default fill value of 0.
    /// </summary>
    public RamRegionProfile(string Id, string Start, string Size) : this(Id, Start, Size, 0) { }
}

/// <summary>
/// Profile for a ROM memory region.
/// </summary>
/// <param name="Id">Unique identifier for this ROM region.</param>
/// <param name="Start">Start address of the ROM region.</param>
/// <param name="Size">Size of the ROM region in bytes.</param>
/// <param name="File">Optional path to ROM file (relative to profile directory).</param>
/// <param name="WritePolicy">Policy for handling writes to ROM (defaults to ThrowException).</param>
public sealed record RomRegionProfile(
    string Id,
    string Start,
    string Size,
    string? File = null,
    RomWritePolicy WritePolicy = RomWritePolicy.ThrowException) : MemoryRegionProfile(Id, Start, Size)
{
    /// <summary>
    /// Validates the ROM region.
    /// </summary>
    public new void Validate(string profileId)
    {
        base.Validate(profileId);

        // File is optional (can be provided at runtime or via test data)
    }
}
