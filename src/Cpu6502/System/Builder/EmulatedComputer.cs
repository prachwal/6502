namespace Cpu6502.System.Builder;

/// <summary>
/// Represents a fully configured and built emulated computer.
/// Contains the CPU, bus, and all devices.
/// </summary>
public sealed class EmulatedComputer : IDevice
{
    private readonly ICpuCore _cpu;
    private readonly ISystemBus _bus;
    private readonly IDevice[] _devices;

    /// <summary>
    /// Creates a new emulated computer.
    /// </summary>
    /// <param name="id">Unique identifier for this computer.</param>
    /// <param name="name">Human-readable name for this computer.</param>
    /// <param name="cpu">The CPU core.</param>
    /// <param name="bus">The system bus.</param>
    /// <param name="devices">Array of devices in this computer.</param>
    public EmulatedComputer(
        string id,
        string name,
        ICpuCore cpu,
        ISystemBus bus,
        IDevice[] devices)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <summary>
    /// Human-readable name for this computer.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The CPU core of this computer.
    /// </summary>
    public ICpuCore Cpu => _cpu;

    /// <summary>
    /// The system bus of this computer.
    /// </summary>
    public ISystemBus Bus => _bus;

    /// <summary>
    /// Array of all devices in this computer.
    /// </summary>
    public IReadOnlyList<IDevice> Devices => _devices;

    /// <summary>
    /// Gets a device by its identifier.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns>The device, or null if not found.</returns>
    public IDevice? GetDevice(string deviceId) =>
        _devices.FirstOrDefault(d => d.Id == deviceId);

    /// <summary>
    /// Gets a device of a specific type.
    /// </summary>
    /// <typeparam name="T">The device type.</typeparam>
    /// <returns>The first device of type T, or null if not found.</returns>
    public T? GetDevice<T>() where T : class, IDevice =>
        _devices.OfType<T>().FirstOrDefault();

    /// <summary>
    /// Gets all devices of a specific type.
    /// </summary>
    /// <typeparam name="T">The device type.</typeparam>
    /// <returns>All devices of type T.</returns>
    public IEnumerable<T> GetDevices<T>() where T : class, IDevice =>
        _devices.OfType<T>();

    /// <summary>
    /// Resets the entire computer (CPU and all devices that support reset).
    /// </summary>
    public void Reset()
    {
        _cpu.Reset();

        foreach (var device in _devices)
        {
            if (device is IResettableDevice resettable)
                resettable.Reset();
        }
    }

    /// <summary>
    /// Executes one CPU instruction.
    /// </summary>
    public void StepInstruction() => _cpu.StepInstruction();

    /// <summary>
    /// Executes one CPU cycle.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown if the CPU doesn't support cycle-level stepping.</exception>
    public void StepCycle() => _cpu.StepCycle();

    /// <summary>
    /// Reads a byte from memory.
    /// </summary>
    /// <param name="address">The memory address.</param>
    /// <returns>The byte value at the address.</returns>
    public byte ReadMemory(uint address) => _bus.ReadMemory(address);

    /// <summary>
    /// Writes a byte to memory.
    /// </summary>
    /// <param name="address">The memory address.</param>
    /// <param name="value">The byte value to write.</param>
    public void WriteMemory(uint address, byte value) => _bus.WriteMemory(address, value);

    /// <summary>
    /// Reads a byte from a port.
    /// </summary>
    /// <param name="port">The port number.</param>
    /// <returns>The byte value from the port.</returns>
    public byte ReadPort(uint port) => _bus.ReadPort(port);

    /// <summary>
    /// Writes a byte to a port.
    /// </summary>
    /// <param name="port">The port number.</param>
    /// <param name="value">The byte value to write.</param>
    public void WritePort(uint port, byte value) => _bus.WritePort(port, value);

    /// <summary>
    /// Gets a snapshot of the CPU state.
    /// </summary>
    /// <returns>The CPU snapshot.</returns>
    public CpuSnapshot GetCpuSnapshot() => _cpu.GetSnapshot();

    /// <summary>
    /// Executes the computer for a specified number of instructions.
    /// </summary>
    /// <param name="instructionCount">Number of instructions to execute.</param>
    public void Run(long instructionCount)
    {
        for (long i = 0; i < instructionCount; i++)
            _cpu.StepInstruction();
    }

    /// <summary>
    /// Executes the computer for a specified number of cycles.
    /// </summary>
    /// <param name="cycleCount">Number of cycles to execute.</param>
    /// <exception cref="NotSupportedException">Thrown if the CPU doesn't support cycle-level stepping.</exception>
    public void RunCycles(long cycleCount)
    {
        for (long i = 0; i < cycleCount; i++)
            _cpu.StepCycle();
    }
}
