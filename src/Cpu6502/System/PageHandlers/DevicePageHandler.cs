namespace Cpu6502.System;

/// <summary>
/// Handler strony urządzenia memory-mapped.
/// Deleguje odczyt/zapis do zarejestrowanego urządzenia IMemoryMappedDevice.
/// </summary>
public sealed class DevicePageHandler : IPageHandler
{
    private readonly IMemoryMappedDevice _device;
    private readonly uint _baseOffset;

    /// <summary>
    /// Tworzy nowy handler strony urządzenia.
    /// </summary>
    /// <param name="device">Urządzenie memory-mapped.</param>
    /// <param name="baseOffset">Offset bazowy w obrębie urządzenia (domyślnie 0).</param>
    public DevicePageHandler(IMemoryMappedDevice device, uint baseOffset = 0)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _baseOffset = baseOffset;
    }

    /// <inheritdoc/>
    public byte ReadByte(uint offset) => _device.ReadMemory(offset - _baseOffset);

    /// <inheritdoc/>
    public void WriteByte(uint offset, byte value) => _device.WriteMemory(offset - _baseOffset, value);

    /// <summary>
    /// Zwraca referencję do urządzenia.
    /// </summary>
    public IMemoryMappedDevice Device => _device;
}
