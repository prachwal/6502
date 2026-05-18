namespace Cpu6502.System;

/// <summary>
/// Skompilowana mapa portów I/O.
/// Obsługuje port-mapped devices z optymalnym routingiem.
/// </summary>
public sealed class CompiledPortMap
{
    private readonly IPortMappedDevice?[] _portDevices;
    private readonly uint _portSpaceBits;

    /// <summary>
    /// Tworzy nową skompilowaną mapę portów.
    /// </summary>
    /// <param name="portSpaceBits">Liczba bitów przestrzeni portów (0 = brak portów).</param>
    public CompiledPortMap(int portSpaceBits = 8)
    {
        if (portSpaceBits < 0 || portSpaceBits > 32)
            throw new ArgumentOutOfRangeException(nameof(portSpaceBits), "Port space bits must be between 0 and 32");

        _portSpaceBits = (uint)portSpaceBits;
        var portCount = portSpaceBits > 0 ? (1U << portSpaceBits) : 0;
        _portDevices = new IPortMappedDevice?[portCount];
    }

    /// <summary>
    /// Liczba bitów przestrzeni portów.
    /// </summary>
    public int PortSpaceBits => (int)_portSpaceBits;

    /// <summary>
    /// Rozmiar przestrzeni portów w bajtach.
    /// </summary>
    public uint Size => _portSpaceBits > 0 ? (1U << (int)_portSpaceBits) : 0;

    /// <summary>
    /// Czy przestrzeń portów jest dostępna.
    /// </summary>
    public bool HasPortSpace => _portSpaceBits > 0;

    /// <summary>
    /// Mapuje urządzenie port-mapped do mapy.
    /// </summary>
    /// <param name="device">Urządzenie do zamapowania.</param>
    /// <exception cref="ArgumentException">Jeśli urządzenie wykracza poza przestrzeń portów.</exception>
    public void MapDevice(IPortMappedDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        if (!HasPortSpace)
            throw new InvalidOperationException("Port space is not available (portSpaceBits = 0)");

        ValidatePortRange(device.StartPort, device.Size);

        for (uint port = device.StartPort; port < device.StartPort + device.Size; port++)
        {
            if (port >= Size)
                throw new ArgumentOutOfRangeException(nameof(device), "Device port range exceeds port space");

            if (_portDevices[port] != null)
                throw new InvalidOperationException(
                    "Port 0x" + port.ToString("X4") + " is already mapped to device '" + _portDevices[port]!.Id + "'");

            _portDevices[port] = device;
        }
    }

    /// <summary>
    /// Odczytuje bajt z podanego portu.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <returns>Wartość odczytana, lub 0xFF jeśli port niezmapowany.</returns>
    public byte ReadPort(uint port)
    {
        if (port >= Size)
            return 0xFF;

        var device = _portDevices[port];
        if (device == null)
            return 0xFF;

        uint offsetInDevice = port - device.StartPort;
        return device.ReadPort(offsetInDevice);
    }

    /// <summary>
    /// Zapisuje bajt do podanego portu.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <param name="value">Wartość do zapisania.</param>
    /// <remarks>Zapis do niezmapowanego portu jest ignorowany.</remarks>
    public void WritePort(uint port, byte value)
    {
        if (port >= Size)
            return;

        var device = _portDevices[port];
        if (device == null)
            return;

        uint offsetInDevice = port - device.StartPort;
        device.WritePort(offsetInDevice, value);
    }

    /// <summary>
    /// Zwraca urządzenie zamapowane na podany port, lub null jeśli niezmapowany.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <returns>Urządzenie port-mapped lub null.</returns>
    public IPortMappedDevice? GetDevice(uint port) => port < Size ? _portDevices[port] : null;

    private void ValidatePortRange(uint startPort, uint size)
    {
        if (size == 0)
            throw new ArgumentException("Size cannot be zero", nameof(size));

        uint endPort = startPort + size;
        if (endPort > Size || endPort < startPort) // Overflow check
            throw new ArgumentOutOfRangeException(nameof(size), "Device port range exceeds port space");
    }
}
