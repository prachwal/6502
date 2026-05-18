namespace Cpu6502.System;

/// <summary>
/// Interfejs magistrali systemowej obsługującej pamięć i porty I/O.
/// Jest fundamentem dla CPU z zarówno memory-mapped jak i port-mapped I/O.
/// </summary>
public interface ISystemBus
{
    /// <summary>
    /// Odczytuje bajt z podanego adresu pamięci.
    /// </summary>
    /// <param name="address">Adres w przestrzeni pamięci.</param>
    /// <returns>Wartość odczytana z pamięci.</returns>
    byte ReadMemory(uint address);

    /// <summary>
    /// Zapisuje bajt pod podany adres pamięci.
    /// </summary>
    /// <param name="address">Adres w przestrzeni pamięci.</param>
    /// <param name="value">Wartość do zapisania.</param>
    void WriteMemory(uint address, byte value);

    /// <summary>
    /// Odczytuje bajt z podanego portu I/O.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <returns>Wartość odczytana z portu.</returns>
    byte ReadPort(uint port);

    /// <summary>
    /// Zapisuje bajt do podanego portu I/O.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <param name="value">Wartość do zapisania.</param>
    void WritePort(uint port, byte value);
}

/// <summary>
/// Interfejs urządzenia mapowanego w przestrzeni pamięci.
/// </summary>
public interface IMemoryMappedDevice : IDevice
{
    /// <summary>Adres startowy urządzenia w przestrzeni pamięci.</summary>
    uint StartAddress { get; }

    /// <summary>Rozmiar obszaru pamięci zajmowanego przez urządzenie.</summary>
    uint Size { get; }

    /// <summary>
    /// Odczytuje bajt z urządzenia.
    /// </summary>
    /// <param name="address">Adres względny (offset od StartAddress).</param>
    /// <returns>Wartość odczytana z urządzenia.</returns>
    byte ReadMemory(uint address);

    /// <summary>
    /// Zapisuje bajt do urządzenia.
    /// </summary>
    /// <param name="address">Adres względny (offset od StartAddress).</param>
    /// <param name="value">Wartość do zapisania.</param>
    void WriteMemory(uint address, byte value);
}

/// <summary>
/// Interfejs urządzenia mapowanego w przestrzeni portów I/O.
/// </summary>
public interface IPortMappedDevice : IDevice
{
    /// <summary>Numer startowy portu.</summary>
    uint StartPort { get; }

    /// <summary>Liczba portów zajmowanych przez urządzenie.</summary>
    uint Size { get; }

    /// <summary>
    /// Odczytuje bajt z portu urządzenia.
    /// </summary>
    /// <param name="port">Port względny (offset od StartPort).</param>
    /// <returns>Wartość odczytana z portu.</returns>
    byte ReadPort(uint port);

    /// <summary>
    /// Zapisuje bajt do portu urządzenia.
    /// </summary>
    /// <param name="port">Port względny (offset od StartPort).</param>
    /// <param name="value">Wartość do zapisania.</param>
    void WritePort(uint port, byte value);
}
