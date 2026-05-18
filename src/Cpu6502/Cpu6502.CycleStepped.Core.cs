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

    #region Metoda StepInstruction() - pełna instrukcja

    /// <summary>
    /// Wykonuje **jedną pełną instrukcję** procesora (od pobrania opcodu do zakończenia).
    /// Jest to zalecane API dla nowego kodu.
    /// </summary>
    public void StepInstruction()
    {
        StepInstructionCore();
    }

    #endregion

    #region Metoda Tick() - przestarzałe, kompatybilne wstecz

    /// <summary>
    /// Wykonuje jeden cykl zegara procesora.
    /// 
    /// UWAGA: Obecnie metoda ta wykonuje CAŁĄ instrukcję (nie pojedynczy cykl).
    /// Jest zachowana dla wstecznej zgodności. Użyj <see cref="StepInstruction()"/> dla nowego kodu.
    /// </summary>
    [Obsolete("Tick() currently executes a full instruction. Use StepInstruction() for clarity.")]
    public void Tick()
    {
        StepInstructionCore();
    }

    #endregion

    #region Wspólna implementacja StepInstruction/Tick

    /// <summary>
    /// Wspólna implementacja dla <see cref="StepInstruction"/> i <see cref="Tick"/>.
    /// Wykonuje pełną instrukcję od pobrania opcodu do stanu sync.
    /// </summary>
    private void StepInstructionCore()
    {
        if (_halted)
        {
            return;
        }

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
            _pageCrossed = false;
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
            // Faza 19 - Niestabilne opkody
            0x8B => 2, 0xAB => 2, 0xEB => 2,  // ANE, LXA, USBC - Immediate
            0x83 => 6,  // SAX (ind,X)
            0x9F => 5, 0x93 => 6,  // SHA - abs,Y, ind,Y
            0x9E => 5,  // SHX - abs,Y
            0x9C => 5,  // SHY - abs,X
            0x9B => 5,  // TAS - abs,Y
            // Faza 19 - NOP-y
            0x04 => 3, 0x44 => 3, 0x64 => 3,  // NOP zp
            0x14 => 4, 0x34 => 4, 0x54 => 4, 0x74 => 4, 0xD4 => 4, 0xF4 => 4,  // NOP zp,X
            0x0C => 4,  // NOP abs
            0x1C => 4, 0x3C => 4, 0x5C => 4, 0x7C => 4, 0xDC => 4, 0xFC => 4,  // NOP abs,X
            0x80 => 2, 0x82 => 2, 0x89 => 2, 0xC2 => 2, 0xE2 => 2,  // NOP imm
            0x1A => 2, 0x3A => 2, 0x5A => 2, 0x7A => 2, 0xDA => 2, 0xFA => 2,  // NOP impl
            // Faza 19 - KIL
            0x02 => 1, 0x12 => 1, 0x22 => 1, 0x32 => 1, 0x42 => 1, 0x52 => 1,
            0x62 => 1, 0x72 => 1, 0x92 => 1, 0xB2 => 1, 0xD2 => 1, 0xF2 => 1,
            _ => 2
        };
    }

    private byte GetEffectiveInstructionCycles(byte opcode)
    {
        byte cycles = GetInstructionCycles(opcode);
        if (_pageCrossed && HasReadPageCrossPenalty(opcode))
        {
            cycles++;
        }

        return cycles;
    }

    private static bool HasReadPageCrossPenalty(byte opcode)
    {
        return opcode is
            0xBD or 0xB9 or 0xB1 or 0xBE or 0xBC or
            0x7D or 0x79 or 0x71 or
            0xFD or 0xF9 or 0xF1 or
            0xDD or 0xD9 or 0xD1 or
            0x3D or 0x39 or 0x31 or
            0x1D or 0x19 or 0x11 or
            0x5D or 0x59 or 0x51;
    }

    /// <summary>
    /// Wykonuje pojedynczy cykl instrukcji na podstawie złożonego klucza opcode/cycle.
    /// </summary>
    private void ExecuteCycle(ushort key)
    {
        if (ExecuteCycleLoadStoreTransferFlags((byte)(key >> 3), (byte)(key & 0x07), key))
        {
            return;
        }

        if (ExecuteCycleArithmeticCompareLogic(key))
        {
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

        if (ExecuteCycleIllegalRMW(key))
        {
            return;
        }

        if (ExecuteCycleUnstableOpcodes(key))
        {
            return;
        }

        if (ExecuteCycleNopKilOpcodes(key))
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
    /// Wykonuje cykl dla niestabilnych opcode'ów (ANE, LXA, SHA, SHX, SHY, TAS, USBC).
    /// </summary>
    private bool ExecuteCycleUnstableOpcodes(ushort key)
    {
        switch (key)
        {
            // ANE (XAA) - $8B
            case 0x8B << 3 | 0: AneImm(); return true;
            // LXA (LAX Immediate) - $AB
            case 0xAB << 3 | 0: LxaImm(); return true;
            // USBC - $EB
            case 0xEB << 3 | 0: UsbcImm(); return true;
            // SHA - $9F (abs,Y)
            case 0x9F << 3 | 0: ShaAbsY_Cycle0(); break;
            case 0x9F << 3 | 1: ShaAbsY_Cycle1(); break;
            case 0x9F << 3 | 2: ShaAbsY_Cycle2(); break;
            case 0x9F << 3 | 3: ShaAbsY_Cycle3(); break;
            case 0x9F << 3 | 4: ShaAbsY_Cycle4(); return true;
            // SHA - $93 (ind,Y)
            case 0x93 << 3 | 0: ShaIndY_Cycle0(); break;
            case 0x93 << 3 | 1: ShaIndY_Cycle1(); break;
            case 0x93 << 3 | 2: ShaIndY_Cycle2(); break;
            case 0x93 << 3 | 3: ShaIndY_Cycle3(); break;
            case 0x93 << 3 | 4: ShaIndY_Cycle4(); break;
            case 0x93 << 3 | 5: ShaIndY_Cycle5(); return true;
            // SHX - $9E (abs,Y)
            case 0x9E << 3 | 0: ShxAbsY_Cycle0(); break;
            case 0x9E << 3 | 1: ShxAbsY_Cycle1(); break;
            case 0x9E << 3 | 2: ShxAbsY_Cycle2(); break;
            case 0x9E << 3 | 3: ShxAbsY_Cycle3(); break;
            case 0x9E << 3 | 4: ShxAbsY_Cycle4(); return true;
            // SHY - $9C (abs,X)
            case 0x9C << 3 | 0: ShyAbsX_Cycle0(); break;
            case 0x9C << 3 | 1: ShyAbsX_Cycle1(); break;
            case 0x9C << 3 | 2: ShyAbsX_Cycle2(); break;
            case 0x9C << 3 | 3: ShyAbsX_Cycle3(); break;
            case 0x9C << 3 | 4: ShyAbsX_Cycle4(); return true;
            // TAS - $9B (abs,Y)
            case 0x9B << 3 | 0: TasAbsY_Cycle0(); break;
            case 0x9B << 3 | 1: TasAbsY_Cycle1(); break;
            case 0x9B << 3 | 2: TasAbsY_Cycle2(); break;
            case 0x9B << 3 | 3: TasAbsY_Cycle3(); break;
            case 0x9B << 3 | 4: TasAbsY_Cycle4(); return true;
            default: return false;
        }
        return false;
    }

    /// <summary>
    /// Wykonuje cykl dla NOP i KIL opcode'ów.
    /// </summary>
    private bool ExecuteCycleNopKilOpcodes(ushort key)
    {
        switch (key)
        {
            // NOP - Zero Page ($04, $44, $64)
            case 0x04 << 3 | 0: NopZp_04_Cycle0(); break;
            case 0x04 << 3 | 1: NopZp_04_Cycle1(); break;
            case 0x04 << 3 | 2: NopZp_04_Cycle2(); return true;
            case 0x44 << 3 | 0: NopZp_44_Cycle0(); break;
            case 0x44 << 3 | 1: NopZp_44_Cycle1(); break;
            case 0x44 << 3 | 2: NopZp_44_Cycle2(); return true;
            case 0x64 << 3 | 0: NopZp_64_Cycle0(); break;
            case 0x64 << 3 | 1: NopZp_64_Cycle1(); break;
            case 0x64 << 3 | 2: NopZp_64_Cycle2(); return true;
            // NOP - Zero Page,X ($14, $34, $54, $74, $0C)
            case 0x14 << 3 | 0: NopZpX_14_Cycle0(); break;
            case 0x14 << 3 | 1: NopZpX_14_Cycle1(); break;
            case 0x14 << 3 | 2: NopZpX_14_Cycle2(); break;
            case 0x14 << 3 | 3: NopZpX_14_Cycle3(); return true;
            case 0x34 << 3 | 0: NopZpX_34_Cycle0(); break;
            case 0x34 << 3 | 1: NopZpX_34_Cycle1(); break;
            case 0x34 << 3 | 2: NopZpX_34_Cycle2(); break;
            case 0x34 << 3 | 3: NopZpX_34_Cycle3(); return true;
            case 0x54 << 3 | 0: NopZpX_54_Cycle0(); break;
            case 0x54 << 3 | 1: NopZpX_54_Cycle1(); break;
            case 0x54 << 3 | 2: NopZpX_54_Cycle2(); break;
            case 0x54 << 3 | 3: NopZpX_54_Cycle3(); return true;
            case 0x74 << 3 | 0: NopZpX_74_Cycle0(); break;
            case 0x74 << 3 | 1: NopZpX_74_Cycle1(); break;
            case 0x74 << 3 | 2: NopZpX_74_Cycle2(); break;
            case 0x74 << 3 | 3: NopZpX_74_Cycle3(); return true;
            case 0xD4 << 3 | 0: NopZpX_54_Cycle0(); break;
            case 0xD4 << 3 | 1: NopZpX_54_Cycle1(); break;
            case 0xD4 << 3 | 2: NopZpX_54_Cycle2(); break;
            case 0xD4 << 3 | 3: NopZpX_54_Cycle3(); return true;
            case 0xF4 << 3 | 0: NopZpX_74_Cycle0(); break;
            case 0xF4 << 3 | 1: NopZpX_74_Cycle1(); break;
            case 0xF4 << 3 | 2: NopZpX_74_Cycle2(); break;
            case 0xF4 << 3 | 3: NopZpX_74_Cycle3(); return true;
            case 0x0C << 3 | 0: NopAbs_0C_Cycle0(); break;
            case 0x0C << 3 | 1: NopAbs_0C_Cycle1(); break;
            case 0x0C << 3 | 2: NopAbs_0C_Cycle2(); break;
            case 0x0C << 3 | 3: NopAbs_0C_Cycle3(); return true;
            // NOP - Absolute,X ($1C, $3C, $5C, $7C, $DC, $FC)
            case 0x1C << 3 | 0: NopAbsX_1C_Cycle0(); break;
            case 0x1C << 3 | 1: NopAbsX_1C_Cycle1(); break;
            case 0x1C << 3 | 2: NopAbsX_1C_Cycle2(); break;
            case 0x1C << 3 | 3: NopAbsX_1C_Cycle3(); return true;
            case 0x3C << 3 | 0: NopAbsX_3C_Cycle0(); break;
            case 0x3C << 3 | 1: NopAbsX_3C_Cycle1(); break;
            case 0x3C << 3 | 2: NopAbsX_3C_Cycle2(); break;
            case 0x3C << 3 | 3: NopAbsX_3C_Cycle3(); return true;
            case 0x5C << 3 | 0: NopAbsX_5C_Cycle0(); break;
            case 0x5C << 3 | 1: NopAbsX_5C_Cycle1(); break;
            case 0x5C << 3 | 2: NopAbsX_5C_Cycle2(); break;
            case 0x5C << 3 | 3: NopAbsX_5C_Cycle3(); return true;
            case 0x7C << 3 | 0: NopAbsX_7C_Cycle0(); break;
            case 0x7C << 3 | 1: NopAbsX_7C_Cycle1(); break;
            case 0x7C << 3 | 2: NopAbsX_7C_Cycle2(); break;
            case 0x7C << 3 | 3: NopAbsX_7C_Cycle3(); return true;
            case 0xDC << 3 | 0: NopAbsX_DC_Cycle0(); break;
            case 0xDC << 3 | 1: NopAbsX_DC_Cycle1(); break;
            case 0xDC << 3 | 2: NopAbsX_DC_Cycle2(); break;
            case 0xDC << 3 | 3: NopAbsX_DC_Cycle3(); return true;
            case 0xFC << 3 | 0: NopAbsX_FC_Cycle0(); break;
            case 0xFC << 3 | 1: NopAbsX_FC_Cycle1(); break;
            case 0xFC << 3 | 2: NopAbsX_FC_Cycle2(); break;
            case 0xFC << 3 | 3: NopAbsX_FC_Cycle3(); return true;
            // NOP - Immediate ($80, $82, $89, $C2, $E2)
            case 0x80 << 3 | 0: NopImm_80(); return true;
            case 0x82 << 3 | 0: NopImm_82(); return true;
            case 0x89 << 3 | 0: NopImm_89(); return true;
            case 0xC2 << 3 | 0: NopImm_C2(); return true;
            case 0xE2 << 3 | 0: NopImm_E2(); return true;
            // NOP - Implied ($1A, $3A, $5A, $7A, $DA, $FA)
            case 0x1A << 3 | 0: NopImpl_1A(); return true;
            case 0x3A << 3 | 0: NopImpl_3A(); return true;
            case 0x5A << 3 | 0: NopImpl_5A(); return true;
            case 0x7A << 3 | 0: NopImpl_7A(); return true;
            case 0xDA << 3 | 0: NopImpl_DA(); return true;
            case 0xFA << 3 | 0: NopImpl_FA(); return true;
            // KIL ($02, $12, $22, $32, $42, $52, $62, $72, $92, $B2, $D2, $F2)
            case 0x02 << 3 | 0: Kil_02(); return true;
            case 0x12 << 3 | 0: Kil_12(); return true;
            case 0x22 << 3 | 0: Kil_22(); return true;
            case 0x32 << 3 | 0: Kil_32(); return true;
            case 0x42 << 3 | 0: Kil_42(); return true;
            case 0x52 << 3 | 0: Kil_52(); return true;
            case 0x62 << 3 | 0: Kil_62(); return true;
            case 0x72 << 3 | 0: Kil_72(); return true;
            case 0x92 << 3 | 0: Kil_92(); return true;
            case 0xB2 << 3 | 0: Kil_B2(); return true;
            case 0xD2 << 3 | 0: Kil_D2(); return true;
            case 0xF2 << 3 | 0: Kil_F2(); return true;
            default: return false;
        }
        return true;
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
