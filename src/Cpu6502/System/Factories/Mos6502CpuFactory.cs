using Cpu6502.System.Profiles;

namespace Cpu6502.System.Factories;

/// <summary>
/// Factory for creating MOS 6502 CPU instances.
/// </summary>
public sealed class Mos6502CpuFactory : ICpuFactory
{
    /// <summary>
    /// The CPU type identifier for MOS 6502 NMOS.
    /// </summary>
    public const string CpuTypeMos6502Nmos = "mos6502-nmos";

    /// <summary>
    /// The CPU type identifier for MOS 6502 (generic).
    /// </summary>
    public const string CpuTypeMos6502 = "mos6502";

    /// <summary>
    /// The CPU type identifier for MOS 6510.
    /// </summary>
    public const string CpuTypeMos6510 = "mos6510";

    private readonly string _cpuType;

    /// <summary>
    /// Creates a new MOS 6502 CPU factory.
    /// </summary>
    /// <param name="cpuType">The CPU type identifier.</param>
    public Mos6502CpuFactory(string cpuType = CpuTypeMos6502Nmos)
    {
        _cpuType = cpuType;
    }

    /// <inheritdoc/>
    public string CpuType => _cpuType;

    /// <inheritdoc/>
    public ICpuCore CreateCpu(CpuProfile cpuProfile, AddressSpaceDescriptor addressSpace, IMemoryBus? memoryBus = null)
    {
        // Create the underlying Cpu6502 instance with the memory bus
        // If no bus is provided, create a dummy one (for testing)
        var bus = memoryBus ?? new RuntimeBus();
        var cpu = new Cpu6502(bus);

        // Create the adapter that wraps it in ICpuCore
        var adapter = new Cpu6502CoreAdapter(cpu, _cpuType);

        // If initial PC is specified, set it after reset
        if (cpuProfile.InitialPC.HasValue)
        {
            cpu.Reset();
            cpu.PC = (ushort)cpuProfile.InitialPC.Value;
        }
        else
        {
            cpu.Reset();
        }

        return adapter;
    }
}
