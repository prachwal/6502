namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Rejestry CPU

    /// <summary>
    /// Accumulator - główny rejestr arytmetyczny.
    /// </summary>
    private byte _a;

    /// <summary>
    /// Rejestr indeksowy X.
    /// </summary>
    private byte _x;

    /// <summary>
    /// Rejestr indeksowy Y.
    /// </summary>
    private byte _y;

    /// <summary>
    /// Program Counter - wskaźnik bieżącej instrukcji.
    /// </summary>
    private ushort _pc;

    /// <summary>
    /// Stack Pointer - wskaźnik stosu (zawsze w zakresie 0x0100-0x01FF).
    /// </summary>
    private byte _sp;

    /// <summary>
    /// Processor Status Register - rejestr flag.
    /// </summary>
    private byte _p;

    #endregion

    #region Stan wewnętrzny

    /// <summary>
    /// Instruction Register - przechowuje bieżący opcode.
    /// </summary>
    private byte _ir;

    /// <summary>
    /// Sygnalizuje, czy kolejny Tick() ma pobrać nowy opcode.
    /// </summary>
    private bool _sync;

    /// <summary>
    /// Licznik cykli zegara.
    /// </summary>
    private ulong _cycle;

    #endregion

    #region Zależności

    /// <summary>
    /// Interfejs magistrali pamięci do odczytu/zapisu.
    /// </summary>
    private readonly IMemoryBus _memory;

    #endregion

    #region Tabela opcode'ów

    /// <summary>
    /// Tablica delegatów dla wszystkich 256 możliwych opcode'ów.
    /// </summary>
    private Action[] _opcodeTable = null!;

    #endregion
}
