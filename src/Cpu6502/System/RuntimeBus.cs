namespace Cpu6502.System;

/// <summary>
/// Główna magistrala systemowa implementująca zarówno ISystemBus jak i IMemoryBus.
/// Łączy CompiledMemoryMap i CompiledPortMap w jeden spójny system.
/// </summary>
public sealed class RuntimeBus : ISystemBus, IMemoryBus
{
    private readonly CompiledMemoryMap _memoryMap;
    private readonly CompiledPortMap _portMap;
    private IBusTracer _tracer = NullBusTracer.Instance;

    /// <summary>
    /// Tworzy nową magistralę systemową.
    /// </summary>
    /// <param name="addressSpaceBits">Liczba bitów przestrzeni adresowej (domyślnie 16 = 64KB).</param>
    /// <param name="portSpaceBits">Liczba bitów przestrzeni portów (0 = brak portów, domyślnie 0 dla 6502).</param>
    public RuntimeBus(int addressSpaceBits = 16, int portSpaceBits = 0)
    {
        _memoryMap = new CompiledMemoryMap(addressSpaceBits);
        _portMap = new CompiledPortMap(portSpaceBits);
    }

    /// <summary>
    /// Mapuje region pamięci RAM.
    /// </summary>
    /// <param name="startAddress">Adres startowy.</param>
    /// <param name="size">Rozmiar w bajtach.</param>
    /// <param name="fillValue">Wartość wypełnienia (domyślnie 0).</param>
    public void MapRam(uint startAddress, uint size, byte fillValue = 0) =>
        _memoryMap.MapRam(startAddress, size, fillValue);

    /// <summary>
    /// Mapuje region pamięci ROM.
    /// </summary>
    /// <param name="startAddress">Adres startowy.</param>
    /// <param name="data">Dane ROM.</param>
    /// <param name="writePolicy">Polityka obsługi zapisu.</param>
    /// <param name="regionName">Opcjonalna nazwa regionu.</param>
    public void MapRom(uint startAddress, byte[] data, RomWritePolicy writePolicy = RomWritePolicy.ThrowException, string? regionName = null) =>
        _memoryMap.MapRom(startAddress, data, writePolicy, regionName);

    /// <summary>
    /// Mapuje urządzenie memory-mapped.
    /// </summary>
    /// <param name="device">Urządzenie do zamapowania.</param>
    public void MapDevice(IMemoryMappedDevice device) =>
        _memoryMap.MapDevice(device);

    /// <summary>
    /// Mapuje urządzenie port-mapped.
    /// </summary>
    /// <param name="device">Urządzenie do zamapowania.</param>
    public void MapPortDevice(IPortMappedDevice device) =>
        _portMap.MapDevice(device);

    /// <summary>
    /// Ustawia tracer dla magistrali.
    /// </summary>
    /// <param name="tracer">Implementacja IBusTracer.</param>
    public void SetTracer(IBusTracer tracer) =>
        _tracer = tracer ?? throw new ArgumentNullException(nameof(tracer));

    /// <summary>
    /// Zwraca podłączony tracer.
    /// </summary>
    public IBusTracer Tracer => _tracer;

    /// <summary>
    /// Zwraca mapę pamięci.
    /// </summary>
    public CompiledMemoryMap MemoryMap => _memoryMap;

    /// <summary>
    /// Zwraca mapę portów.
    /// </summary>
    public CompiledPortMap PortMap => _portMap;

    // ==================== ISystemBus Implementation ====================

    /// <inheritdoc/>
    public byte ReadMemory(uint address)
    {
        var value = _memoryMap.ReadByte(address);
        _tracer.OnReadMemory(address, value);
        return value;
    }

    /// <inheritdoc/>
    public void WriteMemory(uint address, byte value)
    {
        _memoryMap.WriteByte(address, value);
        _tracer.OnWriteMemory(address, value);
    }

    /// <inheritdoc/>
    public byte ReadPort(uint port)
    {
        // 6502 nie ma oddzielnej przestrzeni portów - używa memory-mapped I/O
        if (!_portMap.HasPortSpace)
            throw new NotSupportedException(
                "6502 CPU uses memory-mapped I/O. Port operations are not supported. " +
                "Use ReadMemory/WriteMemory instead.");

        var value = _portMap.ReadPort(port);
        _tracer.OnReadPort(port, value);
        return value;
    }

    /// <inheritdoc/>
    public void WritePort(uint port, byte value)
    {
        // 6502 nie ma oddzielnej przestrzeni portów - używa memory-mapped I/O
        if (!_portMap.HasPortSpace)
            throw new NotSupportedException(
                "6502 CPU uses memory-mapped I/O. Port operations are not supported. " +
                "Use ReadMemory/WriteMemory instead.");

        _portMap.WritePort(port, value);
        _tracer.OnWritePort(port, value);
    }

    // ==================== IMemoryBus Implementation (for 6502 compatibility) ====================

    /// <inheritdoc/>
    public byte Read(ushort address) => ReadMemory(address);

    /// <inheritdoc/>
    public void Write(ushort address, byte value) => WriteMemory(address, value);
}
