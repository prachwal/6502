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
        InitOpcodeTable();
    }

    /// <summary>
    /// Inicjalizuje tabelę opcode'ów przypisując delegate'y do odpowiednich metod.
    /// Wszystkie niezaimplementowane opcode'y domyślnie wskazują na Nop.
    /// </summary>
    private void InitOpcodeTable()
    {
        _opcodeTable = new Action[256];
        
        // Wypełnij wszystkie pozycje NOP-em jako domyślnym
        for (int i = 0; i < 256; i++)
            _opcodeTable[i] = Nop;

        // LDA - Load Accumulator (8 opcode'ów)
        _opcodeTable[0xA9] = LdaImm;  // Immediate
        _opcodeTable[0xA5] = LdaZp;   // Zero Page
        _opcodeTable[0xB5] = LdaZpX;  // Zero Page,X
        _opcodeTable[0xAD] = LdaAbs;  // Absolute
        _opcodeTable[0xBD] = LdaAbsX; // Absolute,X
        _opcodeTable[0xB9] = LdaAbsY; // Absolute,Y
        _opcodeTable[0xA1] = LdaIndX; // (Indirect,X)
        _opcodeTable[0xB1] = LdaIndY; // (Indirect),Y

        // LDX - Load X Register (5 opcode'ów)
        _opcodeTable[0xA2] = LdxImm;  // Immediate
        _opcodeTable[0xA6] = LdxZp;   // Zero Page
        _opcodeTable[0xB6] = LdxZpY;  // Zero Page,Y
        _opcodeTable[0xAE] = LdxAbs;  // Absolute
        _opcodeTable[0xBE] = LdxAbsY; // Absolute,Y

        // LDY - Load Y Register (5 opcode'ów)
        _opcodeTable[0xA0] = LdyImm;  // Immediate
        _opcodeTable[0xA4] = LdyZp;   // Zero Page
        _opcodeTable[0xB4] = LdyZpX;  // Zero Page,X
        _opcodeTable[0xAC] = LdyAbs;  // Absolute
        _opcodeTable[0xBC] = LdyAbsX; // Absolute,X

        // STA - Store Accumulator (7 opcode'ów)
        _opcodeTable[0x85] = StaZp;   // Zero Page
        _opcodeTable[0x95] = StaZpX;  // Zero Page,X
        _opcodeTable[0x8D] = StaAbs;  // Absolute
        _opcodeTable[0x9D] = StaAbsX; // Absolute,X
        _opcodeTable[0x99] = StaAbsY; // Absolute,Y
        _opcodeTable[0x81] = StaIndX; // (Indirect,X)
        _opcodeTable[0x91] = StaIndY; // (Indirect),Y

        // STX - Store X Register (3 opcode'y)
        _opcodeTable[0x86] = StxZp;   // Zero Page
        _opcodeTable[0x96] = StxZpY;  // Zero Page,Y
        _opcodeTable[0x8E] = StxAbs;  // Absolute

        // STY - Store Y Register (3 opcode'y)
        _opcodeTable[0x84] = StyZp;   // Zero Page
        _opcodeTable[0x94] = StyZpX;  // Zero Page,X
        _opcodeTable[0x8C] = StyAbs;  // Absolute
    }

    #endregion
}
