using System.Collections.Immutable;

namespace Cpu6502.System.Profiles;

/// <summary>
/// Complete profile for a computer system.
/// Defines the CPU, address space, memory, and devices.
/// </summary>
/// <param name="Schema">Schema identifier for the profile format.</param>
/// <param name="Id">Unique identifier for this computer profile.</param>
/// <param name="Name">Human-readable name for the computer.</param>
/// <param name="Status">Profile status (e.g., "planned", "active", "deprecated").</param>
/// <param name="Cpu">The CPU profile.</param>
/// <param name="AddressSpace">The address space profile.</param>
/// <param name="Memory">The memory profile.</param>
/// <param name="Devices">List of device profiles.</param>
public sealed record ComputerProfile(
    string Schema,
    string Id,
    string Name,
    string Status,
    CpuProfile Cpu,
    AddressSpaceProfile AddressSpace,
    MemoryProfile Memory,
    ImmutableArray<DeviceProfile> Devices)
{
    /// <summary>
    /// Schema identifier for version 1 of computer profiles.
    /// </summary>
    public const string SchemaV1 = "computer-profile/v1";

    /// <summary>
    /// Creates a computer profile with the specified parameters.
    /// </summary>
    public ComputerProfile(
        string id,
        string name,
        CpuProfile cpu,
        AddressSpaceProfile addressSpace,
        MemoryProfile memory,
        IEnumerable<DeviceProfile>? devices = null) : this(
            SchemaV1,
            id,
            name,
            "planned",
            cpu,
            addressSpace,
            memory,
            (devices ?? Array.Empty<DeviceProfile>()).ToImmutableArray()) { }

    /// <summary>
    /// Validates the entire computer profile.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when required fields are null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when there are validation errors.</exception>
    public void Validate()
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(Schema))
            throw new ArgumentNullException(nameof(Schema), $"Schema cannot be null or empty. Profile: {Id}");

        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentNullException(nameof(Id), "Computer profile Id cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentNullException(nameof(Name), $"Computer name cannot be null or empty. Profile: {Id}");

        // Validate schema
        if (Schema != SchemaV1)
            throw new ArgumentException($"Unsupported schema: '{Schema}'. Expected: '{SchemaV1}'. Profile: {Id}");

        // Validate sub-profiles
        Cpu.Validate(Id);
        AddressSpace.Validate(Id);
        Memory.Validate(Id);

        // Validate devices
        foreach (var device in Devices)
            device.Validate(Id, AddressSpace);

        // Check for device ID conflicts
        var deviceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var device in Devices)
        {
            if (!deviceIds.Add(device.Id))
                throw new ArgumentException($"Duplicate device Id: '{device.Id}'. Profile: {Id}");
        }
    }
}
