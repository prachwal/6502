using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cpu6502.System.Profiles;

/// <summary>
/// Profile describing a device in a computer.
/// </summary>
/// <param name="Id">Unique identifier for the device.</param>
/// <param name="Type">Type identifier for the device (e.g., "mos6821-pia", "uart-simple").</param>
/// <param name="Mapping">The mapping configuration for the device.</param>
/// <param name="Bindings">Optional bindings configuration for the device.</param>
/// <param name="Options">Optional device-specific options as JSON object.</param>
public sealed record DeviceProfile(
    string Id,
    string Type,
    DeviceMappingProfile Mapping,
    JsonObject? Bindings = null,
    JsonObject? Options = null)
{
    /// <summary>
    /// Validates the device profile.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when Id or Type is null or empty.</exception>
    public void Validate(string profileId, AddressSpaceProfile addressSpace)
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new ArgumentNullException(
                nameof(Id),
                $"Device Id cannot be null or empty. Profile: {profileId}");

        if (string.IsNullOrWhiteSpace(Type))
            throw new ArgumentNullException(
                nameof(Type),
                $"Device Type cannot be null or empty. Profile: {profileId}");

        Mapping.Validate(Id, profileId);

        // Validate that port mapping requires separate port space
        if (Mapping.Kind == AddressSpaceKind.Port && !addressSpace.HasSeparatePortSpace)
        {
            throw new ArgumentException(
                $"Device '{Id}' uses port-mapped I/O but the address space does not have separate port space. " +
                $"Profile: {profileId}");
        }
    }
}
