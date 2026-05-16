namespace Cpu6502;

/// <summary>
/// Interfejs definiujący operacje odczytu i zapisu pamięci dla procesora 6502.
/// </summary>
public interface IMemoryBus
{
    /// <summary>
    /// Odczytuje bajt z podanego adresu.
    /// </summary>
    /// <param name="address">Adres 16-bitowy (0x0000-0xFFFF).</param>
    /// <returns>Wartość odczytana z pamięci.</returns>
    byte Read(ushort address);

    /// <summary>
    /// Zapisuje bajt pod podany adres.
    /// </summary>
    /// <param name="address">Adres 16-bitowy (0x0000-0xFFFF).</param>
    /// <param name="value">Wartość do zapisania.</param>
    void Write(ushort address, byte value);
}
