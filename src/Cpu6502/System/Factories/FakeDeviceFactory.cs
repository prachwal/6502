using Cpu6502.System.Profiles;

namespace Cpu6502.System.Factories;

/// <summary>
/// A fake device factory for testing purposes.
/// Creates simple memory-mapped or port-mapped devices that don't do anything.
/// </summary>
public sealed class FakeDeviceFactory : IDeviceFactory
{
    private readonly string _deviceType;
    private readonly bool _isPortMapped;

    /// <summary>
    /// Creates a new FakeDeviceFactory.
    /// </summary>
    /// <param name="deviceType">The device type this factory handles.</param>
    /// <param name="isPortMapped">Whether the created devices should be port-mapped.</param>
    public FakeDeviceFactory(string deviceType, bool isPortMapped = false)
    {
        _deviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
        _isPortMapped = isPortMapped;
    }

    /// <inheritdoc/>
    public string DeviceType => _deviceType;

    /// <summary>
    /// The memory-mapped device created by this factory.
    /// </summary>
    public FakeMemoryMappedDevice? MemoryDevice { get; private set; }

    /// <summary>
    /// The port-mapped device created by this factory.
    /// </summary>
    public FakePortMappedDevice? PortDevice { get; private set; }

    /// <summary>
    /// Last created device (for testing assertions).
    /// </summary>
    public IDevice? LastCreatedDevice { get; private set; }

    /// <summary>
    /// Resets tracking of created devices.
    /// </summary>
    public void ResetTracking()
    {
        MemoryDevice = null;
        PortDevice = null;
        LastCreatedDevice = null;
    }

    /// <inheritdoc/>
    public IDevice CreateDevice(
        DeviceProfile deviceProfile,
        ISystemBus systemBus,
        ProfileLoadOptions? loadOptions = null)
    {
        var start = deviceProfile.Mapping.ParsedBaseAddress;
        var size = deviceProfile.Mapping.ParsedSize;

        if (_isPortMapped || deviceProfile.Mapping.Kind == AddressSpaceKind.Port)
        {
            var device = new FakePortMappedDevice(deviceProfile.Id, start, size);
            PortDevice = device;
            LastCreatedDevice = device;
            return device;
        }
        else
        {
            var device = new FakeMemoryMappedDevice(deviceProfile.Id, start, size);
            MemoryDevice = device;
            LastCreatedDevice = device;
            return device;
        }
    }
}

/// <summary>
/// A fake memory-mapped device for testing.
/// </summary>
public sealed class FakeMemoryMappedDevice : IMemoryMappedDevice, IResettableDevice
{
    private readonly byte[] _memory;

    /// <summary>
    /// Creates a new fake memory-mapped device.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    /// <param name="startAddress">Start address in memory.</param>
    /// <param name="size">Size of the device memory.</param>
    public FakeMemoryMappedDevice(string id, uint startAddress, uint size)
    {
        Id = id;
        StartAddress = startAddress;
        Size = size;
        _memory = new byte[size];
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public uint StartAddress { get; }

    /// <inheritdoc/>
    public uint Size { get; }

    /// <summary>
    /// Gets or sets the byte at the specified offset.
    /// </summary>
    public byte this[uint offset] { get => _memory[offset]; set => _memory[offset] = value; }

    /// <summary>
    /// The backing memory array.
    /// </summary>
    public byte[] Memory => _memory;

    /// <summary>
    /// Last read address (for testing).
    /// </summary>
    public uint? LastReadAddress { get; private set; }

    /// <summary>
    /// Last write address and value (for testing).
    /// </summary>
    public (uint Address, byte Value)? LastWrite { get; private set; }

    /// <inheritdoc/>
    public byte ReadMemory(uint address)
    {
        LastReadAddress = address;
        return _memory[address];
    }

    /// <inheritdoc/>
    public void WriteMemory(uint address, byte value)
    {
        LastWrite = (address, value);
        _memory[address] = value;
    }

    /// <summary>
    /// Resets the device.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_memory);
        LastReadAddress = null;
        LastWrite = null;
    }
}

/// <summary>
/// A fake port-mapped device for testing.
/// </summary>
public sealed class FakePortMappedDevice : IPortMappedDevice, IResettableDevice
{
    private readonly byte[] _ports;

    /// <summary>
    /// Creates a new fake port-mapped device.
    /// </summary>
    /// <param name="id">Device identifier.</param>
    /// <param name="startPort">Start port number.</param>
    /// <param name="size">Number of ports.</param>
    public FakePortMappedDevice(string id, uint startPort, uint size)
    {
        Id = id;
        StartPort = startPort;
        Size = size;
        _ports = new byte[size];
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public uint StartPort { get; }

    /// <inheritdoc/>
    public uint Size { get; }

    /// <summary>
    /// Gets or sets the byte at the specified port offset.
    /// </summary>
    public byte this[uint port] { get => _ports[port]; set => _ports[port] = value; }

    /// <summary>
    /// The backing port array.
    /// </summary>
    public byte[] Ports => _ports;

    /// <summary>
    /// Last read port (for testing).
    /// </summary>
    public uint? LastReadPort { get; private set; }

    /// <summary>
    /// Last write port and value (for testing).
    /// </summary>
    public (uint Port, byte Value)? LastWrite { get; private set; }

    /// <inheritdoc/>
    public byte ReadPort(uint port)
    {
        LastReadPort = port;
        return _ports[port];
    }

    /// <inheritdoc/>
    public void WritePort(uint port, byte value)
    {
        LastWrite = (port, value);
        _ports[port] = value;
    }

    /// <summary>
    /// Resets the device.
    /// </summary>
    public void Reset()
    {
        Array.Clear(_ports);
        LastReadPort = null;
        LastWrite = null;
    }
}
