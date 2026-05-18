using System.Collections.Immutable;

namespace Cpu6502.System.Profiles;

/// <summary>
/// Profile describing the memory configuration of a computer.
/// </summary>
/// <param name="RamRegions">List of RAM regions.</param>
/// <param name="RomRegions">List of ROM regions.</param>
public sealed record MemoryProfile(
    ImmutableArray<RamRegionProfile> RamRegions,
    ImmutableArray<RomRegionProfile> RomRegions)
{
    /// <summary>
    /// Creates an empty memory profile.
    /// </summary>
    public MemoryProfile() : this(ImmutableArray<RamRegionProfile>.Empty, ImmutableArray<RomRegionProfile>.Empty) { }

    /// <summary>
    /// Creates a memory profile with the specified RAM and ROM regions.
    /// </summary>
    public MemoryProfile(
        IEnumerable<RamRegionProfile>? ramRegions = null,
        IEnumerable<RomRegionProfile>? romRegions = null) : this(
            (ramRegions ?? Array.Empty<RamRegionProfile>()).ToImmutableArray(),
            (romRegions ?? Array.Empty<RomRegionProfile>()).ToImmutableArray()) { }

    /// <summary>
    /// Gets all memory regions (both RAM and ROM).
    /// </summary>
    public IEnumerable<MemoryRegionProfile> AllRegions =>
        RamRegions.Cast<MemoryRegionProfile>().Concat(RomRegions);

    /// <summary>
    /// Validates the memory profile.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when there are overlapping regions.</exception>
    public void Validate(string profileId)
    {
        foreach (var region in AllRegions)
            region.Validate(profileId);

        // Check for overlaps between all regions
        var regions = AllRegions.ToArray();
        for (int i = 0; i < regions.Length; i++)
        {
            for (int j = i + 1; j < regions.Length; j++)
            {
                var a = regions[i];
                var b = regions[j];

                // Check if ranges overlap
                uint aStart = a.ParsedStart;
                uint aEnd = a.EndAddress;
                uint bStart = b.ParsedStart;
                uint bEnd = b.EndAddress;

                if (aStart < aEnd && bStart < bEnd &&
                    aStart < bEnd && bStart < aEnd)
                {
                    throw new ArgumentException(
                        $"Memory regions '{a.Id}' and '{b.Id}' overlap. " +
                        $"Region '{a.Id}': 0x{aStart:X4}-0x{aEnd:X4}, " +
                        $"Region '{b.Id}': 0x{bStart:X4}-0x{bEnd:X4}. " +
                        $"Profile: {profileId}");
                }
            }
        }
    }
}
