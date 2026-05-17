namespace Cpu6502.Variants;

/// <summary>
/// Fabryka procesorów 6502 — tworzy odpowiedni wariant CPU.
/// </summary>
public static class Cpu6502Factory
{
    /// <summary>
    /// Wariant procesora 6502.
    /// </summary>
    public enum CpuVariant
    {
        /// <summary>Standardowy MOS 6502 (NMOS) — obsługuje BCD.</summary>
        Classic,
        /// <summary>Ricoh 2A03 używany w NES — BCD wyłączony.</summary>
        Nes
    }

    /// <summary>
    /// Tworzy nowy procesor 6502 odpowiedniego wariantu.
    /// </summary>
    /// <param name="variant">Wariant procesora.</param>
    /// <param name="memory">Interfejs magistrali pamięci.</param>
    /// <returns>Instancja procesora odpowiedniego typu.</returns>
    public static Cpu6502 Create(CpuVariant variant, IMemoryBus memory)
    {
        return variant switch
        {
            CpuVariant.Classic => new Cpu6502Classic(memory),
            CpuVariant.Nes => new Cpu6502Nes(memory),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
        };
    }
}
