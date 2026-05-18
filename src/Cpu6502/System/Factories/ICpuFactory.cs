using Cpu6502.System.Profiles;

namespace Cpu6502.System.Factories;

/// <summary>
/// Factory interface for creating CPU instances from profiles.
/// </summary>
public interface ICpuFactory
{
    /// <summary>
    /// The type of CPU this factory creates.
    /// </summary>
    string CpuType { get; }

    /// <summary>
    /// Creates a CPU core from the given profile.
    /// </summary>
    /// <param name="cpuProfile">The CPU profile.</param>
    /// <param name="addressSpace">The address space descriptor.</param>
    /// <param name="memoryBus">The memory bus for the CPU (can be null for CPUs that don't need it).</param>
    /// <returns>The created CPU core.</returns>
    ICpuCore CreateCpu(CpuProfile cpuProfile, AddressSpaceDescriptor addressSpace, IMemoryBus? memoryBus = null);
}
