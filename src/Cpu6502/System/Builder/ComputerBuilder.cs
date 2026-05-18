using System.Collections.Immutable;
using Cpu6502.System.Factories;
using Cpu6502.System.Profiles;

namespace Cpu6502.System.Builder;

/// <summary>
/// Exception thrown when computer building fails.
/// </summary>
public sealed class ComputerBuildException : Exception
{
    /// <summary>
    /// Creates a new build exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="profileId">The profile identifier.</param>
    public ComputerBuildException(string message, string? profileId = null)
        : base(profileId != null ? $"{message} Profile: {profileId}" : message)
    {
        ProfileId = profileId ?? string.Empty;
    }

    /// <summary>The profile identifier associated with the error.</summary>
    public string ProfileId { get; }
}

/// <summary>
/// Builds a computer from a profile, creating the bus, CPU, memory, and devices.
/// </summary>
public sealed class ComputerBuilder
{
    private readonly DeviceFactoryRegistry _factoryRegistry;

    /// <summary>
    /// Creates a new ComputerBuilder with the specified factory registry.
    /// </summary>
    /// <param name="factoryRegistry">The factory registry to use for creating CPUs and devices.</param>
    public ComputerBuilder(DeviceFactoryRegistry? factoryRegistry = null)
    {
        _factoryRegistry = factoryRegistry ?? DeviceFactoryRegistry.Default;
    }

    /// <summary>
    /// Builds a computer from the given profile.
    /// </summary>
    /// <param name="profile">The computer profile.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The built emulated computer.</returns>
    /// <exception cref="ComputerBuildException">Thrown when building fails.</exception>
    public EmulatedComputer Build(ComputerProfile profile, ProfileLoadOptions? loadOptions = null)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        try
        {
            // Validate the profile
            profile.Validate();

            // Create the runtime bus based on address space
            var bus = CreateRuntimeBus(profile.AddressSpace);

            // Map memory regions (RAM and ROM)
            MapMemoryRegions(profile.Memory, bus, loadOptions);

            // Create and map devices
            var devices = CreateAndMapDevices(profile.Devices, bus, loadOptions);

            // Create the CPU
            var cpu = CreateCpu(profile.Cpu, bus);

            // Create the emulated computer
            return new EmulatedComputer(profile.Id, profile.Name, cpu, bus, devices);
        }
        catch (Exception ex) when (ex is not ComputerBuildException)
        {
            throw new ComputerBuildException(ex.Message, profile.Id);
        }
    }

    private RuntimeBus CreateRuntimeBus(AddressSpaceProfile addressSpace)
    {
        // 6502 has 16-bit address space, no ports
        // Z80 has 16-bit address space, 8-bit ports
        var addressSpaceBits = addressSpace.MemoryAddressBits;
        var portSpaceBits = addressSpace.HasSeparatePortSpace ? addressSpace.PortAddressBits : 0;

        return new RuntimeBus(addressSpaceBits, portSpaceBits);
    }

    private void MapMemoryRegions(MemoryProfile memory, RuntimeBus bus, ProfileLoadOptions? loadOptions)
    {
        // Map RAM regions first
        foreach (var ram in memory.RamRegions)
        {
            bus.MapRam(
                ram.ParsedStart,
                ram.ParsedSize,
                ram.FillValue);
        }

        // Map ROM regions
        foreach (var rom in memory.RomRegions)
        {
            byte[]? romData = null;

            // Try to get ROM data from overrides
            if (rom.File != null && loadOptions?.RomDataOverrides != null)
            {
                loadOptions.RomDataOverrides.TryGetValue(rom.File, out romData);
            }

            // If no data from overrides and file is specified, throw
            if (romData == null && rom.File != null)
            {
                throw new ComputerBuildException(
                    $"ROM file '{rom.File}' not found and no override provided. " +
                    $"ROM region: {rom.Id}");
            }

            // Use empty array if no data (for testing)
            romData ??= Array.Empty<byte>();

            // Validate ROM data size matches region size
            if ((uint)romData.Length != rom.ParsedSize)
            {
                throw new ComputerBuildException(
                    $"ROM data size ({romData.Length} bytes) does not match region size ({rom.ParsedSize} bytes). " +
                    $"ROM region: {rom.Id}");
            }

            bus.MapRom(
                rom.ParsedStart,
                romData,
                rom.WritePolicy,
                rom.Id);
        }
    }

    private IDevice[] CreateAndMapDevices(
        ImmutableArray<DeviceProfile> deviceProfiles,
        RuntimeBus bus,
        ProfileLoadOptions? loadOptions)
    {
        var devices = new List<IDevice>();

        foreach (var deviceProfile in deviceProfiles)
        {
            // Create the device using the factory
            var device = _factoryRegistry.CreateDevice(deviceProfile, bus, loadOptions);

            // Map the device based on its mapping profile
            MapDeviceToBus(device, deviceProfile.Mapping, bus);

            devices.Add(device);
        }

        return devices.ToArray();
    }

    private void MapDeviceToBus(IDevice device, DeviceMappingProfile mapping, RuntimeBus bus)
    {
        if (mapping.Kind == AddressSpaceKind.Memory)
        {
            if (device is IMemoryMappedDevice memoryDevice)
            {
                bus.MapDevice(memoryDevice);
            }
            else
            {
                throw new ComputerBuildException(
                    $"Device '{device.Id}' is mapped as memory-mapped but does not implement IMemoryMappedDevice.");
            }
        }
        else if (mapping.Kind == AddressSpaceKind.Port)
        {
            if (device is IPortMappedDevice portDevice)
            {
                bus.MapPortDevice(portDevice);
            }
            else
            {
                throw new ComputerBuildException(
                    $"Device '{device.Id}' is mapped as port-mapped but does not implement IPortMappedDevice.");
            }
        }
    }

    private ICpuCore CreateCpu(CpuProfile cpuProfile, RuntimeBus bus)
    {
        // Get the address space descriptor - for now use Mos6502 default
        // The RuntimeBus doesn't expose AddressSpaceDescriptor, so we use the profile's address space
        // This is a simplification for Phase 26
        var addressSpace = AddressSpaceDescriptor.Mos6502;

        // Create the CPU using the factory with the bus
        var cpu = _factoryRegistry.CreateCpu(cpuProfile, addressSpace, bus);

        return cpu;
    }

    // ==================== Static Methods ====================

    /// <summary>
    /// Creates a computer from a profile using the default factory registry.
    /// </summary>
    /// <param name="profile">The computer profile.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The built emulated computer.</returns>
    public static EmulatedComputer BuildFromProfile(ComputerProfile profile, ProfileLoadOptions? loadOptions = null)
    {
        var builder = new ComputerBuilder();
        return builder.Build(profile, loadOptions);
    }

    /// <summary>
    /// Creates a computer from a JSON string using the default factory registry.
    /// </summary>
    /// <param name="json">The JSON profile string.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The built emulated computer.</returns>
    public static EmulatedComputer BuildFromJson(string json, ProfileLoadOptions? loadOptions = null)
    {
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromString(json, loadOptions);
        return BuildFromProfile(profile, loadOptions);
    }

    /// <summary>
    /// Creates a computer from a profile file using the default factory registry.
    /// </summary>
    /// <param name="filePath">Path to the JSON profile file.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The built emulated computer.</returns>
    public static EmulatedComputer BuildFromFile(string filePath, ProfileLoadOptions? loadOptions = null)
    {
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromFile(filePath, loadOptions);
        return BuildFromProfile(profile, loadOptions);
    }
}
