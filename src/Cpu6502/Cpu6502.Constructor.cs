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

        // Transfer instructions - Implied mode (6 opcode'ów)
        _opcodeTable[0xAA] = Tax;  // TAX
        _opcodeTable[0xA8] = Tay;  // TAY
        _opcodeTable[0xBA] = Tsx;  // TSX
        _opcodeTable[0x8A] = Txa;  // TXA
        _opcodeTable[0x9A] = Txs;  // TXS
        _opcodeTable[0x98] = Tya;  // TYA

        // Flag Set/Clear instructions - Implied mode (7 opcode'ów)
        _opcodeTable[0x18] = Clc;  // CLC - Clear Carry
        _opcodeTable[0x38] = Sec;  // SEC - Set Carry
        _opcodeTable[0xD8] = Cld;  // CLD - Clear Decimal
        _opcodeTable[0xF8] = Sed;  // SED - Set Decimal
        _opcodeTable[0x58] = Cli;  // CLI - Clear Interrupt
        _opcodeTable[0x78] = Sei;  // SEI - Set Interrupt
        _opcodeTable[0xB8] = Clv;  // CLV - Clear Overflow

        // ADC - Add with Carry (8 opcode'ów)
        _opcodeTable[0x69] = AdcImm;  // Immediate
        _opcodeTable[0x65] = AdcZp;   // Zero Page
        _opcodeTable[0x75] = AdcZpX;  // Zero Page,X
        _opcodeTable[0x6D] = AdcAbs;  // Absolute
        _opcodeTable[0x7D] = AdcAbsX; // Absolute,X
        _opcodeTable[0x79] = AdcAbsY; // Absolute,Y
        _opcodeTable[0x61] = AdcIndX; // (Indirect,X)
        _opcodeTable[0x71] = AdcIndY; // (Indirect),Y

        // SBC - Subtract with Carry (8 opcode'ów)
        _opcodeTable[0xE9] = SbcImm;  // Immediate
        _opcodeTable[0xE5] = SbcZp;   // Zero Page
        _opcodeTable[0xF5] = SbcZpX;  // Zero Page,X
        _opcodeTable[0xED] = SbcAbs;  // Absolute
        _opcodeTable[0xFD] = SbcAbsX; // Absolute,X
        _opcodeTable[0xF9] = SbcAbsY; // Absolute,Y
        _opcodeTable[0xE1] = SbcIndX; // (Indirect,X)
        _opcodeTable[0xF1] = SbcIndY; // (Indirect),Y

        // INC - Increment Memory (4 opcode'y)
        _opcodeTable[0xE6] = IncZp;   // Zero Page
        _opcodeTable[0xF6] = IncZpX;  // Zero Page,X
        _opcodeTable[0xEE] = IncAbs;  // Absolute
        _opcodeTable[0xFE] = IncAbsX; // Absolute,X

        // DEC - Decrement Memory (4 opcode'y)
        _opcodeTable[0xC6] = DecZp;   // Zero Page
        _opcodeTable[0xD6] = DecZpX;  // Zero Page,X
        _opcodeTable[0xCE] = DecAbs;  // Absolute
        _opcodeTable[0xDE] = DecAbsX; // Absolute,X

        // INX, INY, DEX, DEY - Register Increment/Decrement (4 opcode'y)
        _opcodeTable[0xE8] = Inx;  // INX
        _opcodeTable[0xC8] = Iny;  // INY
        _opcodeTable[0xCA] = Dex;  // DEX
        _opcodeTable[0x88] = Dey;  // DEY

        // CMP - Compare Accumulator (8 opcode'ów)
        _opcodeTable[0xC9] = CmpImm;  // Immediate
        _opcodeTable[0xC5] = CmpZp;   // Zero Page
        _opcodeTable[0xD5] = CmpZpX;  // Zero Page,X
        _opcodeTable[0xCD] = CmpAbs;  // Absolute
        _opcodeTable[0xDD] = CmpAbsX; // Absolute,X
        _opcodeTable[0xD9] = CmpAbsY; // Absolute,Y
        _opcodeTable[0xC1] = CmpIndX; // (Indirect,X)
        _opcodeTable[0xD1] = CmpIndY; // (Indirect),Y

        // CPX - Compare X Register (3 opcode'y)
        _opcodeTable[0xE0] = CpxImm;  // Immediate
        _opcodeTable[0xE4] = CpxZp;   // Zero Page
        _opcodeTable[0xEC] = CpxAbs;  // Absolute

        // CPY - Compare Y Register (3 opcode'y)
        _opcodeTable[0xC0] = CpyImm;  // Immediate
        _opcodeTable[0xC4] = CpyZp;   // Zero Page
        _opcodeTable[0xCC] = CpyAbs;  // Absolute

        // BIT - Bit Test (2 opcode'y)
        _opcodeTable[0x24] = BitZp;   // Zero Page
        _opcodeTable[0x2C] = BitAbs;  // Absolute

        // AND - Logical AND (8 opcode'ów)
        _opcodeTable[0x29] = AndImm;  // Immediate
        _opcodeTable[0x25] = AndZp;   // Zero Page
        _opcodeTable[0x35] = AndZpX;  // Zero Page,X
        _opcodeTable[0x2D] = AndAbs;  // Absolute
        _opcodeTable[0x3D] = AndAbsX; // Absolute,X
        _opcodeTable[0x39] = AndAbsY; // Absolute,Y
        _opcodeTable[0x21] = AndIndX; // (Indirect,X)
        _opcodeTable[0x31] = AndIndY; // (Indirect),Y

        // ORA - Logical OR (8 opcode'ów)
        _opcodeTable[0x09] = OraImm;  // Immediate
        _opcodeTable[0x05] = OraZp;   // Zero Page
        _opcodeTable[0x15] = OraZpX;  // Zero Page,X
        _opcodeTable[0x0D] = OraAbs;  // Absolute
        _opcodeTable[0x1D] = OraAbsX; // Absolute,X
        _opcodeTable[0x19] = OraAbsY; // Absolute,Y
        _opcodeTable[0x01] = OraIndX; // (Indirect,X)
        _opcodeTable[0x11] = OraIndY; // (Indirect),Y

        // EOR - Exclusive OR (8 opcode'ów)
        _opcodeTable[0x49] = EorImm;  // Immediate
        _opcodeTable[0x45] = EorZp;   // Zero Page
        _opcodeTable[0x55] = EorZpX;  // Zero Page,X
        _opcodeTable[0x4D] = EorAbs;  // Absolute
        _opcodeTable[0x5D] = EorAbsX; // Absolute,X
        _opcodeTable[0x59] = EorAbsY; // Absolute,Y
        _opcodeTable[0x41] = EorIndX; // (Indirect,X)
        _opcodeTable[0x51] = EorIndY; // (Indirect),Y

        // ASL - Arithmetic Shift Left (5 opcode'ów)
        _opcodeTable[0x0A] = AslAcc;  // Accumulator
        _opcodeTable[0x06] = AslZp;   // Zero Page
        _opcodeTable[0x16] = AslZpX;  // Zero Page,X
        _opcodeTable[0x0E] = AslAbs;  // Absolute
        _opcodeTable[0x1E] = AslAbsX; // Absolute,X

        // LSR - Logical Shift Right (5 opcode'ów)
        _opcodeTable[0x4A] = LsrAcc;  // Accumulator
        _opcodeTable[0x46] = LsrZp;   // Zero Page
        _opcodeTable[0x56] = LsrZpX;  // Zero Page,X
        _opcodeTable[0x4E] = LsrAbs;  // Absolute
        _opcodeTable[0x5E] = LsrAbsX; // Absolute,X

        // ROL - Rotate Left (5 opcode'ów)
        _opcodeTable[0x2A] = RolAcc;  // Accumulator
        _opcodeTable[0x26] = RolZp;   // Zero Page
        _opcodeTable[0x36] = RolZpX;  // Zero Page,X
        _opcodeTable[0x2E] = RolAbs;  // Absolute
        _opcodeTable[0x3E] = RolAbsX; // Absolute,X

        // ROR - Rotate Right (5 opcode'ów)
        _opcodeTable[0x6A] = RorAcc;  // Accumulator
        _opcodeTable[0x66] = RorZp;   // Zero Page
        _opcodeTable[0x76] = RorZpX;  // Zero Page,X
        _opcodeTable[0x6E] = RorAbs;  // Absolute
        _opcodeTable[0x7E] = RorAbsX; // Absolute,X

        // JMP - Jump (2 opcode'y)
        _opcodeTable[0x4C] = JmpAbs;   // Absolute
        _opcodeTable[0x6C] = JmpInd;   // Indirect

        // JSR/RTS - Subroutine (2 opcode'y)
        _opcodeTable[0x20] = JsrAbs;  // Absolute
        _opcodeTable[0x60] = Rts;     // Implied

        // Branch - 8 instrukcji
        _opcodeTable[0x90] = BccRel;  // BCC - Branch if Carry Clear
        _opcodeTable[0xB0] = BcsRel;  // BCS - Branch if Carry Set
        _opcodeTable[0xF0] = BeqRel;  // BEQ - Branch if Equal
        _opcodeTable[0x30] = BmiRel;  // BMI - Branch if Minus
        _opcodeTable[0xD0] = BneRel;  // BNE - Branch if Not Equal
        _opcodeTable[0x10] = BplRel;  // BPL - Branch if Plus
        _opcodeTable[0x50] = BvcRel;  // BVC - Branch if Overflow Clear
        _opcodeTable[0x70] = BvsRel;  // BVS - Branch if Overflow Set

        // Stack operations - 4 instrukcje
        _opcodeTable[0x48] = Pha;   // PHA - Push A
        _opcodeTable[0x08] = Php;   // PHP - Push P
        _opcodeTable[0x68] = Pla;   // PLA - Pull A
        _opcodeTable[0x28] = Plp;   // PLP - Pull P

        // NOP
        _opcodeTable[0xEA] = Nop;  // NOP - No Operation
    }

    #endregion
}
