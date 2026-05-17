namespace Cpu6502.Variants;

/// <summary>
/// Standardowy procesor MOS 6502 (NMOS) — klasyczny wariant.
/// Obsługuje tryb BCD (Decimal Mode) dla instrukcji ADC i SBC.
/// </summary>
public sealed class Cpu6502Classic : Cpu6502
{
    /// <summary>
    /// Inicjalizuje nowy egzemplarz klasycznego procesora MOS 6502.
    /// </summary>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    public Cpu6502Classic(IMemoryBus memory) : base(memory)
    {
        // Włącz obsługę trybu BCD (standardowy 6502 go obsługuje)
        DecimalModeEnabled = true;
    }
}
