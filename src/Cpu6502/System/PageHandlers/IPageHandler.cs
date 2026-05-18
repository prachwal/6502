namespace Cpu6502.System;

/// <summary>
/// Interfejs obsługi stron pamięci w CompiledMemoryMap.
/// Każdy handler odpowiedzialny jest za obsługę odczytu/zapisu dla 256-bajtowej strony.
/// </summary>
public interface IPageHandler
{
    /// <summary>
    /// Odczytuje bajt z podanego offsetu w stronie (0-255).
    /// </summary>
    /// <param name="offset">Offset w obrębie strony (0-255).</param>
    /// <returns>Wartość odczytana.</returns>
    byte ReadByte(uint offset);

    /// <summary>
    /// Zapisuje bajt pod podany offset w stronie (0-255).
    /// </summary>
    /// <param name="offset">Offset w obrębie strony (0-255).</param>
    /// <param name="value">Wartość do zapisania.</param>
    void WriteByte(uint offset, byte value);
}
