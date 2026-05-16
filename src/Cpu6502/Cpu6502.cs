namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public class Cpu6502
{
    #region Stałe flag

    /// <summary>
    /// Flag Carry (bit 0).
    /// </summary>
    public const byte FlagC = 0x01;

    /// <summary>
    /// Flag Zero (bit 1).
    /// </summary>
    public const byte FlagZ = 0x02;

    /// <summary>
    /// Flag Interrupt Disable (bit 2).
    /// </summary>
    public const byte FlagI = 0x04;

    /// <summary>
    /// Flag Decimal (bit 3).
    /// </summary>
    public const byte FlagD = 0x08;

    /// <summary>
    /// Flag Break (bit 4).
    /// </summary>
    public const byte FlagB = 0x10;

    /// <summary>
    /// Flag Unused (bit 5) - zawsze 1 na stosie.
    /// </summary>
    public const byte FlagU = 0x20;

    /// <summary>
    /// Flag Overflow (bit 6).
    /// </summary>
    public const byte FlagV = 0x40;

    /// <summary>
    /// Flag Negative (bit 7).
    /// </summary>
    public const byte FlagN = 0x80;

    #endregion

    #region Rejestry

    private byte _a;           // Accumulator
    private byte _x;           // X register
    private byte _y;           // Y register
    private ushort _pc;        // Program Counter
    private byte _sp;          // Stack Pointer
    private byte _p;           // Processor Status Register

    #endregion

    #region Stan wewnętrzny

    private byte _ir;          // Instruction Register = (opcode << 3) | cycleCounter
    private bool _sync;        // true = rozpoczęcie nowej instrukcji
    private ulong _cycle;      // licznik cykli

    #endregion

    #region Zależności

    private readonly IMemoryBus _memory;

    #endregion

    #region Konstruktor

    /// <summary>
    /// Inicjalizuje nową instancję procesora 6502 z podanym magistralą pamięci.
    /// </summary>
    /// <param name="memoryBus">Magistrala pamięci do odczytu/zapisu.</param>
    public Cpu6502(IMemoryBus memoryBus)
    {
        _memory = memoryBus ?? throw new ArgumentNullException(nameof(memoryBus));
    }

    #endregion

    #region Właściwości

    /// <summary>
    /// Pobiera stan procesora (czytelna kopia rejestru P).
    /// </summary>
    public byte Status => _p;

    /// <summary>
    /// Pobiera/lub ustawia accumulator.
    /// </summary>
    public byte A { get => _a; set => _a = value; }

    /// <summary>
    /// Pobiera/lub ustawia rejestr X.
    /// </summary>
    public byte X { get => _x; set => _x = value; }

    /// <summary>
    /// Pobiera/lub ustawia rejestr Y.
    /// </summary>
    public byte Y { get => _y; set => _y = value; }

    /// <summary>
    /// Pobiera/lub ustawia program counter.
    /// </summary>
    public ushort PC { get => _pc; set => _pc = value; }

    /// <summary>
    /// Pobiera/lub ustawia stack pointer.
    /// </summary>
    public byte SP { get => _sp; set => _sp = value; }

    #endregion

    #region Metody pomocnicze dla flag

    /// <summary>
    /// Pobiera wartość wybranej flagi.
    /// </summary>
    /// <param name="flag">Bit flagi do sprawdzenia.</param>
    /// <returns>True jeśli flaga jest ustawiona.</returns>
    public bool GetFlag(byte flag) => (_p & flag) != 0;

    /// <summary>
    /// Ustawia lub kasuje wybraną flagę.
    /// </summary>
    /// <param name="flag">Bit flagi do zmodyfikowania.</param>
    /// <param name="value">Czy flaga ma być ustawiona (true) czy skasowana (false).</param>
    public void SetFlag(byte flag, bool value)
    {
        if (value)
            _p |= flag;
        else
            _p &= (byte)~flag;
    }

    #endregion

    #region Metody publiczne

    /// <summary>
    /// Symuluje jeden cykl zegara procesora.
    /// W obecnej wersji rzuca wyjątek NotImplementedException.
    /// </summary>
    public void Tick()
    {
        // 1. Jeśli SYNC — pobierz opcode, zainicjuj IR
        if (_sync)
        {
            byte opcode = _memory.Read(_pc);
            _ir = (byte)(opcode << 3);  // opcode przesunięty o 3, cycleCounter = 0
            _sync = false;
        }

        // 2. Wykonaj jeden cykl bieżącej instrukcji
        //    Na razie: NotImplementedException lub pułapka debug
        throw new NotImplementedException($"Opcode not implemented: {(_ir >> 3):X2}");
    }

    /// <summary>
    /// Resetuje procesor do stanu początkowego.
    /// </summary>
    /// <param name="resetVectorAddress">Adres wektora RESET (domyślnie 0xFFFC).</param>
    public void Reset(ushort resetVectorAddress = 0xFFFC)
    {
        _a = 0;
        _x = 0;
        _y = 0;
        _sp = 0xFD;          // po 3 pseudo-pushach
        _p = FlagI | FlagU;  // I=1, unused=1
        _pc = 0;

        // Pobranie wektora RESET
        byte lo = _memory.Read(resetVectorAddress);
        byte hi = _memory.Read((ushort)(resetVectorAddress + 1));
        _pc = (ushort)(hi << 8 | lo);

        _sync = true;
        _cycle = 0;
    }

    /// <summary>
    /// Pobiera aktualny stan procesora.
    /// </summary>
    /// <returns>Obiekt CpuState z aktualnymi wartościami rejestru.</returns>
    public CpuState GetState()
    {
        return new CpuState
        {
            A = _a,
            X = _x,
            Y = _y,
            PC = _pc,
            SP = _sp,
            P = _p,
            Cycle = _cycle,
            IR = _ir,
            Sync = _sync
        };
    }

    /// <summary>
    /// Ustawia stan procesora.
    /// </summary>
    /// <param name="state">Obiekt CpuState z wartościami do ustawienia.</param>
    public void SetState(CpuState state)
    {
        _a = state.A;
        _x = state.X;
        _y = state.Y;
        _pc = state.PC;
        _sp = state.SP;
        _p = state.P;
        _cycle = state.Cycle;
        _ir = state.IR;
        _sync = state.Sync;
    }

    #endregion
}
