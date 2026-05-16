namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Stałe flag

    public const byte FlagC = 0x01;
    public const byte FlagZ = 0x02;
    public const byte FlagI = 0x04;
    public const byte FlagD = 0x08;
    public const byte FlagB = 0x10;
    public const byte FlagU = 0x20;
    public const byte FlagV = 0x40;
    public const byte FlagN = 0x80;

    #endregion

    #region Rejestry

    private byte _a;
    private byte _x;
    private byte _y;
    private ushort _pc;
    private byte _sp;
    private byte _p;

    #endregion

    #region Stan wewnętrzny

    private byte _ir;
    private bool _sync;
    private ulong _cycle;

    #endregion

    #region Zależności

    private readonly IMemoryBus _memory;

    #endregion

    #region Opcode table

    private Action[] _opcodeTable = null!;

    #endregion

    #region Konstruktor

    public Cpu6502(IMemoryBus memoryBus)
    {
        _memory = memoryBus ?? throw new ArgumentNullException(nameof(memoryBus));
        InitOpcodeTable();
    }

    private void InitOpcodeTable()
    {
        _opcodeTable = new Action[256];
        // Wypełnij wszystkie NOP
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

    #region Właściwości

    public byte Status => _p;
    public byte A { get => _a; set => _a = value; }
    public byte X { get => _x; set => _x = value; }
    public byte Y { get => _y; set => _y = value; }
    public ushort PC { get => _pc; set => _pc = value; }
    public byte SP { get => _sp; set => _sp = value; }

    #endregion

    #region Tryby adresowania - Immediate

    private (ushort address, byte value, int cycles) Imm()
    {
        byte val = _memory.Read(_pc);
        _pc++;
        return (0, val, 2);
    }

    #endregion

    #region Tryby adresowania - Zero Page

    private (ushort address, byte value, int cycles) Zp()
    {
        byte addr = _memory.Read(_pc);
        _pc++;
        byte val = _memory.Read(addr);
        return (addr, val, 3);
    }

    private (ushort address, byte value, int cycles) ZpX()
    {
        byte addrBase = _memory.Read(_pc);
        _pc++;
        byte addr = (byte)(addrBase + _x);
        byte val = _memory.Read(addr);
        return (addr, val, 4);
    }

    private (ushort address, byte value, int cycles) ZpY()
    {
        byte addrBase = _memory.Read(_pc);
        _pc++;
        byte addr = (byte)(addrBase + _y);
        byte val = _memory.Read(addr);
        return (addr, val, 4);
    }

    #endregion

    #region Tryby adresowania - Absolute

    private (ushort address, byte value, int cycles) Abs()
    {
        byte lo = _memory.Read(_pc);
        _pc++;
        byte hi = _memory.Read(_pc);
        _pc++;
        ushort addr = (ushort)(hi << 8 | lo);
        byte val = _memory.Read(addr);
        return (addr, val, 4);
    }

    private (ushort address, byte value, int cycles) AbsX()
    {
        byte lo = _memory.Read(_pc);
        _pc++;
        byte hi = _memory.Read(_pc);
        _pc++;
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _x);
        int cycles = 4 + (((baseAddr ^ addr) & 0xFF00) != 0 ? 1 : 0);
        byte val = _memory.Read(addr);
        return (addr, val, cycles);
    }

    private (ushort address, byte value, int cycles) AbsY()
    {
        byte lo = _memory.Read(_pc);
        _pc++;
        byte hi = _memory.Read(_pc);
        _pc++;
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _y);
        int cycles = 5 + (((baseAddr ^ addr) & 0xFF00) != 0 ? 1 : 0);
        byte val = _memory.Read(addr);
        return (addr, val, cycles);
    }

    #endregion

    #region Tryby adresowania - Indirect

    private (ushort address, byte value, int cycles) IndX()
    {
        byte zpp = _memory.Read(_pc);
        _pc++;
        byte lo = _memory.Read((byte)(zpp + _x));
        byte hi = _memory.Read((byte)(zpp + _x + 1));
        ushort addr = (ushort)(hi << 8 | lo);
        byte val = _memory.Read(addr);
        return (addr, val, 6);
    }

    private (ushort address, byte value, int cycles) IndY()
    {
        byte zpp = _memory.Read(_pc);
        _pc++;
        byte lo = _memory.Read(zpp);
        byte hi = _memory.Read((byte)(zpp + 1));
        ushort baseAddr = (ushort)(hi << 8 | lo);
        ushort addr = (ushort)(baseAddr + _y);
        int cycles = 5 + (((baseAddr ^ addr) & 0xFF00) != 0 ? 1 : 0);
        byte val = _memory.Read(addr);
        return (addr, val, cycles);
    }

    #endregion

    #region Metody pomocnicze dla flag

    public bool GetFlag(byte flag) => (_p & flag) != 0;

    public void SetFlag(byte flag, bool value)
    {
        if (value)
            _p |= flag;
        else
            _p &= (byte)~flag;
    }

    private void SetNZ(byte value)
    {
        _p = (byte)((_p & ~(FlagN | FlagZ)) | (value & FlagN) | (value == 0 ? FlagZ : 0));
    }

    #endregion

    #region Implementacja instrukcji Load (LDA, LDX, LDY)

    private void LdaImm() { var (_, val, _) = Imm(); _a = val; SetNZ(_a); }
    private void LdaZp() { var (_, val, _) = Zp(); _a = val; SetNZ(_a); }
    private void LdaZpX() { var (_, val, _) = ZpX(); _a = val; SetNZ(_a); }
    private void LdaAbs() { var (_, val, _) = Abs(); _a = val; SetNZ(_a); }
    private void LdaAbsX() { var (_, val, _) = AbsX(); _a = val; SetNZ(_a); }
    private void LdaAbsY() { var (_, val, _) = AbsY(); _a = val; SetNZ(_a); }
    private void LdaIndX() { var (_, val, _) = IndX(); _a = val; SetNZ(_a); }
    private void LdaIndY() { var (_, val, _) = IndY(); _a = val; SetNZ(_a); }

    private void LdxImm() { var (_, val, _) = Imm(); _x = val; SetNZ(_x); }
    private void LdxZp() { var (_, val, _) = Zp(); _x = val; SetNZ(_x); }
    private void LdxZpY() { var (_, val, _) = ZpY(); _x = val; SetNZ(_x); }
    private void LdxAbs() { var (_, val, _) = Abs(); _x = val; SetNZ(_x); }
    private void LdxAbsY() { var (_, val, _) = AbsY(); _x = val; SetNZ(_x); }

    private void LdyImm() { var (_, val, _) = Imm(); _y = val; SetNZ(_y); }
    private void LdyZp() { var (_, val, _) = Zp(); _y = val; SetNZ(_y); }
    private void LdyZpX() { var (_, val, _) = ZpX(); _y = val; SetNZ(_y); }
    private void LdyAbs() { var (_, val, _) = Abs(); _y = val; SetNZ(_y); }
    private void LdyAbsX() { var (_, val, _) = AbsX(); _y = val; SetNZ(_y); }

    #endregion

    #region Implementacja instrukcji Store (STA, STX, STY)

    private void StaZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _a); }
    private void StaZpX() { var (addr, _, _) = ZpX(); _memory.Write(addr, _a); }
    private void StaAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _a); }
    private void StaAbsX() { var (addr, _, _) = AbsX(); _memory.Write(addr, _a); }
    private void StaAbsY() { var (addr, _, _) = AbsY(); _memory.Write(addr, _a); }
    private void StaIndX() { var (addr, _, _) = IndX(); _memory.Write(addr, _a); }
    private void StaIndY() { var (addr, _, _) = IndY(); _memory.Write(addr, _a); }

    private void StxZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _x); }
    private void StxZpY() { var (addr, _, _) = ZpY(); _memory.Write(addr, _x); }
    private void StxAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _x); }

    private void StyZp() { var (addr, _, _) = Zp(); _memory.Write(addr, _y); }
    private void StyZpX() { var (addr, _, _) = ZpX(); _memory.Write(addr, _y); }
    private void StyAbs() { var (addr, _, _) = Abs(); _memory.Write(addr, _y); }

    #endregion

    #region Metody publiczne

    public void Tick()
    {
        byte opcode;
        if (_sync)
        {
            opcode = _memory.Read(_pc);
            _ir = opcode;  // przechowuj opcode bezpośrednio
            _pc++;
            _sync = false;
        }
        else
        {
            opcode = _ir;
        }
        _opcodeTable[opcode]();
    }

    public void Reset(ushort resetVectorAddress = 0xFFFC)
    {
        _a = 0;
        _x = 0;
        _y = 0;
        _sp = 0xFD;
        _p = FlagI | FlagU;
        _pc = 0;

        byte lo = _memory.Read(resetVectorAddress);
        byte hi = _memory.Read((ushort)(resetVectorAddress + 1));
        _pc = (ushort)(hi << 8 | lo);

        _sync = true;
        _cycle = 0;
    }

    public CpuState GetState()
    {
        return new CpuState
        {
            A = _a, X = _x, Y = _y, PC = _pc, SP = _sp, P = _p,
            Cycle = _cycle, IR = _ir, Sync = _sync
        };
    }

    public void SetState(CpuState state)
    {
        _a = state.A; _x = state.X; _y = state.Y;
        _pc = state.PC; _sp = state.SP; _p = state.P;
        _cycle = state.Cycle; _ir = state.IR; _sync = state.Sync;
    }

    #endregion

    #region Placeholder methods (dla brakujących opcode'ów)

    private void Brk() => throw new NotImplementedException("BRK not implemented");
    private void OraIndX() => throw new NotImplementedException("ORA (ind,X) not implemented");
    private void OraZp() => throw new NotImplementedException("ORA zp not implemented");
    private void OraZpX() => throw new NotImplementedException("ORA zp,X not implemented");
    private void OraAbs() => throw new NotImplementedException("ORA abs not implemented");
    private void OraAbsX() => throw new NotImplementedException("ORA abs,X not implemented");
    private void OraAbsY() => throw new NotImplementedException("ORA abs,Y not implemented");
    private void OraIndY() => throw new NotImplementedException("ORA (ind),Y not implemented");
    private void AslAcc() => throw new NotImplementedException("ASL A not implemented");
    private void OraImm() => throw new NotImplementedException("ORA #imm not implemented");
    private void BplRel() => throw new NotImplementedException("BPL rel not implemented");
    private void JsrAbs() => throw new NotImplementedException("JSR abs not implemented");
    private void AndIndX() => throw new NotImplementedException("AND (ind,X) not implemented");
    private void AndZp() => throw new NotImplementedException("AND zp not implemented");
    private void AndZpX() => throw new NotImplementedException("AND zp,X not implemented");
    private void AndAbs() => throw new NotImplementedException("AND abs not implemented");
    private void AndAbsX() => throw new NotImplementedException("AND abs,X not implemented");
    private void AndAbsY() => throw new NotImplementedException("AND abs,Y not implemented");
    private void AndIndY() => throw new NotImplementedException("AND (ind),Y not implemented");
    private void RolAcc() => throw new NotImplementedException("ROL A not implemented");
    private void AndImm() => throw new NotImplementedException("AND #imm not implemented");
    private void BmiRel() => throw new NotImplementedException("BMI rel not implemented");
    private void Rti() => throw new NotImplementedException("RTI not implemented");
    private void EorIndX() => throw new NotImplementedException("EOR (ind,X) not implemented");
    private void EorZp() => throw new NotImplementedException("EOR zp not implemented");
    private void EorZpX() => throw new NotImplementedException("EOR zp,X not implemented");
    private void EorAbs() => throw new NotImplementedException("EOR abs not implemented");
    private void EorAbsX() => throw new NotImplementedException("EOR abs,X not implemented");
    private void EorAbsY() => throw new NotImplementedException("EOR abs,Y not implemented");
    private void EorIndY() => throw new NotImplementedException("EOR (ind),Y not implemented");
    private void LsrAcc() => throw new NotImplementedException("LSR A not implemented");
    private void EorImm() => throw new NotImplementedException("EOR #imm not implemented");
    private void BvcRel() => throw new NotImplementedException("BVC rel not implemented");
    private void Rts() => throw new NotImplementedException("RTS not implemented");
    private void AdcIndX() => throw new NotImplementedException("ADC (ind,X) not implemented");
    private void AdcZp() => throw new NotImplementedException("ADC zp not implemented");
    private void AdcZpX() => throw new NotImplementedException("ADC zp,X not implemented");
    private void AdcAbs() => throw new NotImplementedException("ADC abs not implemented");
    private void AdcAbsX() => throw new NotImplementedException("ADC abs,X not implemented");
    private void AdcAbsY() => throw new NotImplementedException("ADC abs,Y not implemented");
    private void AdcIndY() => throw new NotImplementedException("ADC (ind),Y not implemented");
    private void RorAcc() => throw new NotImplementedException("ROR A not implemented");
    private void AdcImm() => throw new NotImplementedException("ADC #imm not implemented");
    private void Nop() { /* no operation */ }
    private void BvsRel() => throw new NotImplementedException("BVS rel not implemented");
    private void BccRel() => throw new NotImplementedException("BCC rel not implemented");
    private void BcsRel() => throw new NotImplementedException("BCS rel not implemented");
    private void CpyImm() => throw new NotImplementedException("CPY #imm not implemented");
    private void CmpIndX() => throw new NotImplementedException("CMP (ind,X) not implemented");
    private void CmpZp() => throw new NotImplementedException("CMP zp not implemented");
    private void CmpZpX() => throw new NotImplementedException("CMP zp,X not implemented");
    private void CmpAbs() => throw new NotImplementedException("CMP abs not implemented");
    private void CmpAbsX() => throw new NotImplementedException("CMP abs,X not implemented");
    private void CmpAbsY() => throw new NotImplementedException("CMP abs,Y not implemented");
    private void CmpIndY() => throw new NotImplementedException("CMP (ind),Y not implemented");
    private void DecAcc() => throw new NotImplementedException("DEC A not implemented");
    private void CmpImm() => throw new NotImplementedException("CMP #imm not implemented");
    private void BneRel() => throw new NotImplementedException("BNE rel not implemented");
    private void CpxImm() => throw new NotImplementedException("CPX #imm not implemented");
    private void SbcIndX() => throw new NotImplementedException("SBC (ind,X) not implemented");
    private void SbcZp() => throw new NotImplementedException("SBC zp not implemented");
    private void SbcZpX() => throw new NotImplementedException("SBC zp,X not implemented");
    private void SbcAbs() => throw new NotImplementedException("SBC abs not implemented");
    private void SbcAbsX() => throw new NotImplementedException("SBC abs,X not implemented");
    private void SbcAbsY() => throw new NotImplementedException("SBC abs,Y not implemented");
    private void SbcIndY() => throw new NotImplementedException("SBC (ind),Y not implemented");
    private void IncAcc() => throw new NotImplementedException("INC A not implemented");
    private void SbcImm() => throw new NotImplementedException("SBC #imm not implemented");
    private void BeqRel() => throw new NotImplementedException("BEQ rel not implemented");

    #endregion
}
