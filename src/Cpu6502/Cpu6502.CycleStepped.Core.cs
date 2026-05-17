namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Konstruktor

    /// <summary>
    /// Inicjalizuje nową instancję procesora 6502.
    /// </summary>
    /// <param name="memoryBus">Interfejs magistrali pamięci.</param>
    public Cpu6502(IMemoryBus memoryBus)
    {
        _memory = memoryBus ?? throw new ArgumentNullException(nameof(memoryBus));
    }

    #endregion

    #region Metoda Tick() - cykle zegara

    /// <summary>
    /// Wykonuje jeden cykl zegara procesora.
    /// W modelu cycle-stepped, każdy Tick() wykonuje dokładnie jeden cykl.
    /// </summary>
    public void Tick()
    {
        if (TryServiceInterruptBoundary())
        {
            return;
        }

        if (_sync)
        {
            if (TryServiceInterruptBoundary())
            {
                return;
            }

            byte opcode = _memory.Read(_pc);
            _currentOpcode = opcode;
            _ir = (byte)(opcode << 3);
            _cycleCount = 0;
            _sync = false;
            _pc++;
        }

        while (!_sync)
        {
            var key = (ushort)((_currentOpcode << 3) | _cycleCount);
            ExecuteCycle(key);
            _cycleCount++;
            _cycle++;
        }

        ServicePostInstructionIrqBoundary();
    }

    /// <summary>
    /// Zwraca liczbę cykli dla podanego opcode'u.
    /// </summary>
    private byte GetInstructionCycles(byte opcode)
    {
        return opcode switch
        {
            0xA9 => 2, 0xA5 => 3, 0xB5 => 4, 0xAD => 4, 0xBD => 4, 0xB9 => 4, 0xA1 => 6, 0xB1 => 5,
            0x85 => 3, 0x95 => 4, 0x8D => 4, 0x9D => 5, 0x99 => 5, 0x81 => 6, 0x91 => 6,
            0xA2 => 2, 0xA6 => 3, 0xB6 => 4, 0xAE => 4, 0xBE => 4,
            0x86 => 3, 0x96 => 4, 0x8E => 4,
            0xA0 => 2, 0xA4 => 3, 0xB4 => 4, 0xAC => 4, 0xBC => 4,
            0x84 => 3, 0x94 => 4, 0x8C => 4,
            0xAA => 2, 0xA8 => 2, 0xBA => 2, 0x8A => 2, 0x9A => 2, 0x98 => 2,
            0x18 => 2, 0x38 => 2, 0xD8 => 2, 0xF8 => 2, 0x58 => 2, 0x78 => 2, 0xB8 => 2,
            0xEA => 2,
            0x69 => 2, 0x65 => 3, 0x75 => 4, 0x6D => 4, 0x7D => 4, 0x79 => 4, 0x61 => 6, 0x71 => 5,
            0xE9 => 2, 0xE5 => 3, 0xF5 => 4, 0xED => 4, 0xFD => 4, 0xF9 => 4, 0xE1 => 6, 0xF1 => 5,
            0xC9 => 2, 0xC5 => 3, 0xD5 => 4, 0xCD => 4, 0xDD => 4, 0xD9 => 4, 0xC1 => 6, 0xD1 => 5,
            0xE0 => 2, 0xE4 => 3, 0xEC => 4,
            0xC0 => 2, 0xC4 => 3, 0xCC => 4,
            0x29 => 2, 0x25 => 3, 0x35 => 4, 0x2D => 4, 0x3D => 4, 0x39 => 4, 0x21 => 6, 0x31 => 5,
            0x09 => 2, 0x05 => 3, 0x15 => 4, 0x0D => 4, 0x1D => 4, 0x19 => 4, 0x01 => 6, 0x11 => 5,
            0x49 => 2, 0x45 => 3, 0x55 => 4, 0x4D => 4, 0x5D => 4, 0x59 => 4, 0x41 => 6, 0x51 => 5,
            0xE6 => 5, 0xF6 => 6, 0xEE => 6, 0xFE => 7,
            0xC6 => 5, 0xD6 => 6, 0xCE => 6, 0xDE => 7,
            0xE8 => 2, 0xC8 => 2, 0xCA => 2, 0x88 => 2,
            0x0A => 2, 0x06 => 5, 0x16 => 6, 0x0E => 6, 0x1E => 7,
            0x4A => 2, 0x46 => 5, 0x56 => 6, 0x4E => 6, 0x5E => 7,
            0x2A => 2, 0x26 => 5, 0x36 => 6, 0x2E => 6, 0x3E => 7,
            0x6A => 2, 0x66 => 5, 0x76 => 6, 0x6E => 6, 0x7E => 7,
            0x90 => 2, 0xB0 => 2, 0xF0 => 2, 0x30 => 2, 0xD0 => 2, 0x10 => 2, 0x50 => 2, 0x70 => 2,
            0x48 => 3, 0x08 => 3, 0x68 => 4, 0x28 => 4,
            0x4C => 3, 0x6C => 5, 0x20 => 6, 0x60 => 6,
            0x00 => 7, 0x40 => 6,
            0x24 => 3, 0x2C => 4,
            0x0B => 2, 0x2B => 2, 0x4B => 2, 0x6B => 2, 0xCB => 2, 0xBB => 4,
            _ => 2
        };
    }

    /// <summary>
    /// Wykonuje pojedynczy cykl instrukcji na podstawie złożonego klucza opcode/cycle.
    /// </summary>
    private void ExecuteCycle(ushort key)
    {
        if (ExecuteCycleLoadStoreTransferFlags((byte)(key >> 3), (byte)(key & 0x07), key))
        {
            _sync = true;
            return;
        }

        if (ExecuteCycleArithmeticCompareLogic(key))
        {
            _sync = true;
            return;
        }

        if (ExecuteCycleMathsStackBranches(key))
        {
            return;
        }

        if (ExecuteCycleControlFlow(key))
        {
            return;
        }

        if (ExecuteCycleBranches(key))
        {
            return;
        }

        // Jeśli żadna metoda nie obsłużyła cyklu, to jest to nieznany opcode
        // Ustaw sync, aby zapobiec nieskończonej pętli
        _sync = true;
    }

    /// <summary>
    /// Obsługuje przerwania na granicy instrukcji.
    /// Zwraca true, jeśli CPU wstrzyknął przerwanie i Tick() powinien zakończyć się natychmiast.
    /// </summary>
    private bool TryServiceInterruptBoundary()
    {
        if (_shouldClearI)
        {
            SetFlag(FlagI, false);
            _shouldClearI = false;
        }

        if (_nmiLatched)
        {
            _nmiLatched = false;
            InjectInterrupt(InterruptType.NMI);
            return true;
        }

        if (_interruptDelay)
        {
            _interruptDelay = false;
            _suppressPostInstructionIrq = true;
        }

        if (_irqReadyAtBoundary && _irqPending && !GetFlag(FlagI))
        {
            _irqPending = false;
            _irqReadyAtBoundary = false;
            InjectInterrupt(InterruptType.IRQ);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Obsługuje IRQ po zakończeniu instrukcji, o ile dana instrukcja nie blokuje tej granicy.
    /// </summary>
    private void ServicePostInstructionIrqBoundary()
    {
        if (_interruptDelay)
        {
            return;
        }

        if (_suppressPostInstructionIrq)
        {
            _suppressPostInstructionIrq = false;
            if (_irqPending && !GetFlag(FlagI))
            {
                _irqReadyAtBoundary = true;
            }
            return;
        }

        if (_irqPending && !GetFlag(FlagI))
        {
            _irqPending = false;
            _irqReadyAtBoundary = false;
            InjectInterrupt(InterruptType.IRQ);
        }
    }





    #endregion
}
