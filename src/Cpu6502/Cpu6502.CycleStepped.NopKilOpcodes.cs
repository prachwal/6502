namespace Cpu6502;

/// <summary>
/// Nieudokumentowane opkody NOP i KIL/JAM
/// </summary>
public partial class Cpu6502
{
    #region NOP - Zero Page - $04, $44, $64 - 3 cykle

    /// <summary>
    /// NOP Zero Page - reads from zero page, ignores value
    /// Opcode: $04 - 3 cycles
    /// </summary>
    private void NopZp_04_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    private void NopZp_04_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZp_04_Cycle2()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Zero Page - reads from zero page, ignores value
    /// Opcode: $44 - 3 cycles
    /// </summary>
    private void NopZp_44_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    private void NopZp_44_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZp_44_Cycle2()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Zero Page - reads from zero page, ignores value
    /// Opcode: $64 - 3 cycles
    /// </summary>
    private void NopZp_64_Cycle0()
    {
        _tempAddr = _memory.Read(_pc++);
    }

    private void NopZp_64_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZp_64_Cycle2()
    {
        _sync = true;
    }

    #endregion

    #region NOP - Zero Page,X - $14, $34, $54, $74 - 4 cykle

    /// <summary>
    /// NOP Zero Page,X - reads from zp,X, ignores value
    /// Opcode: $14 - 4 cycles
    /// </summary>
    private void NopZpX_14_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void NopZpX_14_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZpX_14_Cycle2()
    {
        // Wraparound read for page crossing emulation
        _tempAddr = (ushort)((_tempAddr & 0xFF) + (_x << 8));
        _memory.Read(_tempAddr);
    }

    private void NopZpX_14_Cycle3()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Zero Page,X - reads from zp,X, ignores value
    /// Opcode: $34 - 4 cycles
    /// </summary>
    private void NopZpX_34_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void NopZpX_34_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZpX_34_Cycle2()
    {
        // Wraparound read for page crossing emulation
        _tempAddr = (ushort)((_tempAddr & 0xFF) + (_x << 8));
        _memory.Read(_tempAddr);
    }

    private void NopZpX_34_Cycle3()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Zero Page,X - reads from zp,X, ignores value
    /// Opcode: $54 - 4 cycles
    /// </summary>
    private void NopZpX_54_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void NopZpX_54_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZpX_54_Cycle2()
    {
        // Wraparound read for page crossing emulation
        _tempAddr = (ushort)((_tempAddr & 0xFF) + (_x << 8));
        _memory.Read(_tempAddr);
    }

    private void NopZpX_54_Cycle3()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Zero Page,X - reads from zp,X, ignores value
    /// Opcode: $74 - 4 cycles
    /// </summary>
    private void NopZpX_74_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = (ushort)(zp + _x);
    }

    private void NopZpX_74_Cycle1()
    {
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopZpX_74_Cycle2()
    {
        // Wraparound read for page crossing emulation
        _tempAddr = (ushort)((_tempAddr & 0xFF) + (_x << 8));
        _memory.Read(_tempAddr);
    }

    private void NopZpX_74_Cycle3()
    {
        _sync = true;
    }

    #endregion

    #region NOP - Absolute - $0C - 4 cykle

    /// <summary>
    /// NOP Absolute - reads from abs, ignores value.
    /// Opcode: $0C - 4 cycles
    /// </summary>
    private void NopAbs_0C_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbs_0C_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbs_0C_Cycle2()
    {
        _memory.Read(_tempAddr);
    }

    private void NopAbs_0C_Cycle3()
    {
        _sync = true;
    }

    #endregion

    #region NOP - Absolute,X - $1C, $3C, $5C, $7C, $DC, $FC - 4(+1) cykle

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $1C - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_1C_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_1C_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_1C_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_1C_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $3C - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_3C_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_3C_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_3C_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_3C_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $5C - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_5C_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_5C_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_5C_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_5C_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $7C - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_7C_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_7C_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_7C_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_7C_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $DC - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_DC_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_DC_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_DC_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_DC_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// NOP Absolute,X - reads from abs,X, ignores value
    /// Opcode: $FC - 4 cycles (+1 if page crossed)
    /// </summary>
    private void NopAbsX_FC_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void NopAbsX_FC_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void NopAbsX_FC_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
        _memory.Read(_tempAddr); // Read and ignore
    }

    private void NopAbsX_FC_Cycle3()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    #endregion

    #region NOP - Immediate - $80, $82, $89, $C2, $E2 - 2 cykle

    /// <summary>
    /// NOP Immediate - reads immediate byte, ignores value
    /// Opcode: $80 - 2 cycles
    /// </summary>
    private void NopImm_80()
    {
        _memory.Read(_pc++); // Read and ignore immediate
        _sync = true;
    }

    /// <summary>
    /// NOP Immediate - reads immediate byte, ignores value
    /// Opcode: $82 - 2 cycles
    /// </summary>
    private void NopImm_82()
    {
        _memory.Read(_pc++); // Read and ignore immediate
        _sync = true;
    }

    /// <summary>
    /// NOP Immediate - reads immediate byte, ignores value
    /// Opcode: $89 - 2 cycles
    /// </summary>
    private void NopImm_89()
    {
        _memory.Read(_pc++); // Read and ignore immediate
        _sync = true;
    }

    /// <summary>
    /// NOP Immediate - reads immediate byte, ignores value
    /// Opcode: $C2 - 2 cycles
    /// </summary>
    private void NopImm_C2()
    {
        _memory.Read(_pc++); // Read and ignore immediate
        _sync = true;
    }

    /// <summary>
    /// NOP Immediate - reads immediate byte, ignores value
    /// Opcode: $E2 - 2 cycles
    /// </summary>
    private void NopImm_E2()
    {
        _memory.Read(_pc++); // Read and ignore immediate
        _sync = true;
    }

    #endregion

    #region NOP - Implied - $1A, $3A, $5A, $7A, $DA, $FA - 2 cykle

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $1A - 2 cycles
    /// </summary>
    private void NopImpl_1A()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $3A - 2 cycles
    /// </summary>
    private void NopImpl_3A()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $5A - 2 cycles
    /// </summary>
    private void NopImpl_5A()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $7A - 2 cycles
    /// </summary>
    private void NopImpl_7A()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $DA - 2 cycles
    /// </summary>
    private void NopImpl_DA()
    {
        _sync = true;
    }

    /// <summary>
    /// NOP Implied - does nothing
    /// Opcode: $FA - 2 cycles
    /// </summary>
    private void NopImpl_FA()
    {
        _sync = true;
    }

    #endregion

    #region KIL/JAM - $02, $12, $22, $32, $42, $52, $62, $72, $92, $B2, $D2, $F2

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $02 - 1 cycle
    /// </summary>
    private void Kil_02()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $12 - 1 cycle
    /// </summary>
    private void Kil_12()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $22 - 1 cycle
    /// </summary>
    private void Kil_22()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $32 - 1 cycle
    /// </summary>
    private void Kil_32()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $42 - 1 cycle
    /// </summary>
    private void Kil_42()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $52 - 1 cycle
    /// </summary>
    private void Kil_52()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $62 - 1 cycle
    /// </summary>
    private void Kil_62()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $72 - 1 cycle
    /// </summary>
    private void Kil_72()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $92 - 1 cycle
    /// </summary>
    private void Kil_92()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $B2 - 1 cycle
    /// </summary>
    private void Kil_B2()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $D2 - 1 cycle
    /// </summary>
    private void Kil_D2()
    {
        _halted = true;
        _sync = true;
    }

    /// <summary>
    /// KIL - Halt the CPU
    /// Opcode: $F2 - 1 cycle
    /// </summary>
    private void Kil_F2()
    {
        _halted = true;
        _sync = true;
    }

    #endregion
}
