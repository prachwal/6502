namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Metody publiczne - Tick i Reset

    /// <summary>
    /// Wykonuje jeden cykl zegara procesora.
    /// Pobiera opcode, inkrementuje PC i wykonuje odpowiednią instrukcję.
    /// </summary>
    public void Tick()
    {
        byte opcode;
        if (_sync)
        {
            // Pobierz nowy opcode z pamięci
            opcode = _memory.Read(_pc);
            _ir = opcode;  // przechowuj opcode bezpośrednio
            _pc++;
            _sync = false;
        }
        else
        {
            // Użyj poprzednio zapisanego opcode
            opcode = _ir;
        }
        _opcodeTable[opcode]();
    }

    /// <summary>
    /// Inicjalizuje procesor do stanu po zasilaniu (RESET).
    /// Ustawia rejestry na domyślne wartości i ładuje PC z wektora RESET.
    /// </summary>
    /// <param name="resetVectorAddress">Adres wektora RESET (domyślnie 0xFFFC).</param>
    public void Reset(ushort resetVectorAddress = 0xFFFC)
    {
        _a = 0;
        _x = 0;
        _y = 0;
        _sp = 0xFD;
        _p = FlagI | FlagU;
        _pc = 0;

        // Odczytaj wektor RESET (16-bit, little-endian)
        byte lo = _memory.Read(resetVectorAddress);
        byte hi = _memory.Read((ushort)(resetVectorAddress + 1));
        _pc = (ushort)(hi << 8 | lo);

        // Ustaw sygnalizację pobrania nowego opcode
        _sync = true;
        _cycle = 0;
    }

    #endregion

    #region Metody publiczne - Stan CPU

    /// <summary>
    /// Zwraca pełny stan procesora (wszystkie rejestry).
    /// </summary>
    /// <returns>Obiekt CpuState z bieżącym stanem.</returns>
    public CpuState GetState()
    {
        return new CpuState
        {
            A = _a, X = _x, Y = _y, PC = _pc, SP = _sp, P = _p,
            Cycle = _cycle, IR = _ir, Sync = _sync
        };
    }

    /// <summary>
    /// Ustawia stan procesora z obiektu CpuState.
    /// Wykorzystywane przez testy do przywracania stanu.
    /// </summary>
    /// <param name="state">Obiekt CpuState z nowym stanem.</param>
    public void SetState(CpuState state)
    {
        _a = state.A; _x = state.X; _y = state.Y;
        _pc = state.PC; _sp = state.SP; _p = state.P;
        _cycle = state.Cycle; _ir = state.IR; _sync = state.Sync;
    }

    #endregion
}
