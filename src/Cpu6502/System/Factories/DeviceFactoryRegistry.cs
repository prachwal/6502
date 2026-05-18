using System.Collections.Concurrent;
using Cpu6502.System.Profiles;

namespace Cpu6502.System.Factories;

/// <summary>
/// Registry for CPU and device factories.
/// Allows registration and lookup of factories by type.
/// </summary>
public sealed class DeviceFactoryRegistry
{
    private readonly ConcurrentDictionary<string, ICpuFactory> _cpuFactories = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IDeviceFactory> _deviceFactories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new empty factory registry.
    /// </summary>
    public DeviceFactoryRegistry() { }

    // ==================== CPU Factory Registration ====================

    /// <summary>
    /// Registers a CPU factory for the specified CPU type.
    /// </summary>
    /// <param name="factory">The CPU factory to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    public void RegisterCpuFactory(ICpuFactory factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _cpuFactories[factory.CpuType] = factory;
    }

    /// <summary>
    /// Registers a CPU factory for the specified type with a factory function.
    /// </summary>
    /// <param name="cpuType">The CPU type identifier.</param>
    /// <param name="factoryFunc">Function that creates CPU instances.</param>
    public void RegisterCpuFactory(string cpuType, Func<CpuProfile, AddressSpaceDescriptor, IMemoryBus?, ICpuCore> factoryFunc)
    {
        if (string.IsNullOrWhiteSpace(cpuType))
            throw new ArgumentNullException(nameof(cpuType));
        if (factoryFunc == null)
            throw new ArgumentNullException(nameof(factoryFunc));

        _cpuFactories[cpuType] = new DelegateCpuFactory(cpuType, factoryFunc);
    }

    /// <summary>
    /// Gets the CPU factory for the specified type.
    /// </summary>
    /// <param name="cpuType">The CPU type identifier.</param>
    /// <returns>The CPU factory, or null if not found.</returns>
    public ICpuFactory? GetCpuFactory(string cpuType) =>
        _cpuFactories.TryGetValue(cpuType, out var factory) ? factory : null;

    /// <summary>
    /// Checks if a CPU factory is registered for the specified type.
    /// </summary>
    /// <param name="cpuType">The CPU type identifier.</param>
    /// <returns>True if a factory is registered, false otherwise.</returns>
    public bool HasCpuFactory(string cpuType) => _cpuFactories.ContainsKey(cpuType);

    /// <summary>
    /// Gets all registered CPU types.
    /// </summary>
    public IEnumerable<string> RegisteredCpuTypes => _cpuFactories.Keys;

    // ==================== Device Factory Registration ====================

    /// <summary>
    /// Registers a device factory for the specified device type.
    /// </summary>
    /// <param name="factory">The device factory to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    public void RegisterDeviceFactory(IDeviceFactory factory)
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _deviceFactories[factory.DeviceType] = factory;
    }

    /// <summary>
    /// Registers a device factory for the specified type with a factory function.
    /// </summary>
    /// <param name="deviceType">The device type identifier.</param>
    /// <param name="factoryFunc">Function that creates device instances.</param>
    public void RegisterDeviceFactory(string deviceType, Func<DeviceProfile, ISystemBus, ProfileLoadOptions?, IDevice> factoryFunc)
    {
        if (string.IsNullOrWhiteSpace(deviceType))
            throw new ArgumentNullException(nameof(deviceType));
        if (factoryFunc == null)
            throw new ArgumentNullException(nameof(factoryFunc));

        _deviceFactories[deviceType] = new DelegateDeviceFactory(deviceType, factoryFunc);
    }

    /// <summary>
    /// Gets the device factory for the specified type.
    /// </summary>
    /// <param name="deviceType">The device type identifier.</param>
    /// <returns>The device factory, or null if not found.</returns>
    public IDeviceFactory? GetDeviceFactory(string deviceType) =>
        _deviceFactories.TryGetValue(deviceType, out var factory) ? factory : null;

    /// <summary>
    /// Checks if a device factory is registered for the specified type.
    /// </summary>
    /// <param name="deviceType">The device type identifier.</param>
    /// <returns>True if a factory is registered, false otherwise.</returns>
    public bool HasDeviceFactory(string deviceType) => _deviceFactories.ContainsKey(deviceType);

    /// <summary>
    /// Gets all registered device types.
    /// </summary>
    public IEnumerable<string> RegisteredDeviceTypes => _deviceFactories.Keys;

    /// <summary>
    /// Creates a CPU instance using the registered factory.
    /// </summary>
    /// <param name="cpuProfile">The CPU profile.</param>
    /// <param name="addressSpace">The address space descriptor.</param>
    /// <param name="memoryBus">The memory bus for the CPU.</param>
    /// <returns>The created CPU core.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no factory is registered for the CPU type.</exception>
    public ICpuCore CreateCpu(CpuProfile cpuProfile, AddressSpaceDescriptor addressSpace, IMemoryBus? memoryBus = null)
    {
        var factory = GetCpuFactory(cpuProfile.Type);
        if (factory == null)
            throw new KeyNotFoundException($"No CPU factory registered for type: '{cpuProfile.Type}'");

        return factory.CreateCpu(cpuProfile, addressSpace, memoryBus);
    }

    /// <summary>
    /// Creates a device instance using the registered factory.
    /// </summary>
    /// <param name="deviceProfile">The device profile.</param>
    /// <param name="systemBus">The system bus to connect the device to.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The created device.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no factory is registered for the device type.</exception>
    public IDevice CreateDevice(
        DeviceProfile deviceProfile,
        ISystemBus systemBus,
        ProfileLoadOptions? loadOptions = null)
    {
        var factory = GetDeviceFactory(deviceProfile.Type);
        if (factory == null)
            throw new KeyNotFoundException($"No device factory registered for type: '{deviceProfile.Type}'");

        return factory.CreateDevice(deviceProfile, systemBus, loadOptions);
    }

    // ==================== Static Instance ====================

    /// <summary>
    /// Default global registry instance.
    /// </summary>
    public static readonly DeviceFactoryRegistry Default = new();

    // ==================== Delegate Factory Implementations ====================

    /// <summary>
    /// A CPU factory that uses a delegate function.
    /// </summary>
    private sealed class DelegateCpuFactory : ICpuFactory
    {
        private readonly string _cpuType;
        private readonly Func<CpuProfile, AddressSpaceDescriptor, IMemoryBus?, ICpuCore> _factoryFunc;

        public DelegateCpuFactory(string cpuType, Func<CpuProfile, AddressSpaceDescriptor, IMemoryBus?, ICpuCore> factoryFunc)
        {
            _cpuType = cpuType;
            _factoryFunc = factoryFunc;
        }

        public string CpuType => _cpuType;

        public ICpuCore CreateCpu(CpuProfile cpuProfile, AddressSpaceDescriptor addressSpace, IMemoryBus? memoryBus = null) =>
            _factoryFunc(cpuProfile, addressSpace, memoryBus);
    }

    /// <summary>
    /// A device factory that uses a delegate function.
    /// </summary>
    private sealed class DelegateDeviceFactory : IDeviceFactory
    {
        private readonly string _deviceType;
        private readonly Func<DeviceProfile, ISystemBus, ProfileLoadOptions?, IDevice> _factoryFunc;

        public DelegateDeviceFactory(string deviceType, Func<DeviceProfile, ISystemBus, ProfileLoadOptions?, IDevice> factoryFunc)
        {
            _deviceType = deviceType;
            _factoryFunc = factoryFunc;
        }

        public string DeviceType => _deviceType;

        public IDevice CreateDevice(DeviceProfile deviceProfile, ISystemBus systemBus, ProfileLoadOptions? loadOptions = null) =>
            _factoryFunc(deviceProfile, systemBus, loadOptions);
    }
}
