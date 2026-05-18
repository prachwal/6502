namespace Cpu6502.System;

/// <summary>
/// Handler strony urządzenia memory-mapped.
/// Deleguje odczyt/zapis do zarejestrowanego urządzenia IMemoryMappedDevice.
/// </summary>
public sealed class DevicePageHandler : IPageHandler
{
    private readonly IMemoryMappedDevice _device;
    private readonly uint _baseOffset;
    private readonly uint _deviceSize;

    /// <summary>
    /// Tworzy nowy handler strony urządzenia.
    /// </summary>
    /// <param name="device">Urządzenie memory-mapped.</param>
    /// <param name="baseOffset">Offset bazowy w obrębie urządzenia (domyślnie 0).</param>
    public DevicePageHandler(IMemoryMappedDevice device, uint baseOffset = 0)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _baseOffset = baseOffset;
        _deviceSize = device.Size;
    }

    /// <inheritdoc/>
    public byte ReadByte(uint offset)
    {
        uint deviceOffset = offset - _baseOffset;
        if (deviceOffset >= _deviceSize)
            return 0xFF; // Return default value for unmapped addresses within page
        return _device.ReadMemory(deviceOffset);
    }

    /// <inheritdoc/>
    public void WriteByte(uint offset, byte value)
    {
        uint deviceOffset = offset - _baseOffset;
        if (deviceOffset >= _deviceSize)
            return; // Ignore writes to RAM areas on same page as device
        _device.WriteMemory(deviceOffset, value);
    }

    /// <summary>
    /// Zwraca referencję do urządzenia.
    /// </summary>
    public IMemoryMappedDevice Device => _device;
}
