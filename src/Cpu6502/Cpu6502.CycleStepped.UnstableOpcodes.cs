namespace Cpu6502;

/// <summary>
/// Nieudokumentowane opkody niestabilne - ANE, LXA, SHA, SHX, SHY, TAS, USBC
/// </summary>
public partial class Cpu6502
{
    #region ANE (XAA) - $8B - Immediate - 2 cykle

    /// <summary>
    /// ANE (XAA) - (A OR $FF) AND X AND immediate -> A
    /// Opcode: $8B, Immediate mode, 2 cycles
    /// </summary>
    private void AneImm()
    {
        byte operand = _memory.Read(_pc++);
        _a = (byte)((_a | 0xFF) & _x & operand);
        SetNZ(_a);
        _sync = true;
    }

    #endregion

    #region LXA (LAX Immediate) - $AB - Immediate - 2 cykle

    /// <summary>
    /// LXA - LDA + LDX, Immediate mode
    /// A = X = (A OR $FF) AND immediate
    /// Opcode: $AB, Immediate mode, 2 cycles
    /// </summary>
    private void LxaImm()
    {
        byte operand = _memory.Read(_pc++);
        _a = _x = (byte)((_a | 0xFF) & operand);
        SetNZ(_a);
        _sync = true;
    }

    #endregion

    #region USBC - $EB - Immediate - 2 cykle

    /// <summary>
    /// USBC - Undocumented SBC, Immediate mode
    /// Behaves exactly like SBC #immediate
    /// Opcode: $EB, Immediate mode, 2 cycles
    /// </summary>
    private void UsbcImm()
    {
        byte operand = _memory.Read(_pc++);
        ExecuteSbc(operand);
        _sync = true;
    }

    #endregion

    #region SHA (AHX/AXA) - $9F (abs,Y), $93 (ind,Y)

    /// <summary>
    /// SHA (AHX/AXA) - Store A AND X AND (high byte + 1)
    /// Opcode: $9F - Absolute,Y - 5 cycles
    /// </summary>
    private void ShaAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void ShaAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void ShaAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
    }

    private void ShaAbsY_Cycle3()
    {
        byte h = (byte)((_tempAddr >> 8) + 1);
        _memory.Write(_tempAddr, (byte)(_a & _x & h));
    }

    private void ShaAbsY_Cycle4()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    /// <summary>
    /// SHA (AHX/AXA) - Store A AND X AND (high byte + 1)
    /// Opcode: $93 - (Indirect),Y - 6 cycles
    /// </summary>
    private void ShaIndY_Cycle0()
    {
        byte zp = _memory.Read(_pc++);
        _tempAddr = zp;
    }

    private void ShaIndY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void ShaIndY_Cycle2()
    {
        _tempAddr = _memory.Read(_tempAddr);
    }

    private void ShaIndY_Cycle3()
    {
        byte highByte = _memory.Read((ushort)(_tempAddr + 1));
        _tempAddr |= (ushort)(highByte << 8);
    }

    private void ShaIndY_Cycle4()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
    }

    private void ShaIndY_Cycle5()
    {
        byte h = (byte)((_tempAddr >> 8) + 1);
        _memory.Write(_tempAddr, (byte)(_a & _x & h));
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    #endregion

    #region SHX (SXA/XAS) - $9E - Absolute,Y - 5 cykli

    /// <summary>
    /// SHX (SXA/XAS) - Store X AND (high byte + 1)
    /// Opcode: $9E - Absolute,Y - 5 cycles
    /// </summary>
    private void ShxAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void ShxAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void ShxAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
    }

    private void ShxAbsY_Cycle3()
    {
        byte h = (byte)((_tempAddr >> 8) + 1);
        _memory.Write(_tempAddr, (byte)(_x & h));
    }

    private void ShxAbsY_Cycle4()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    #endregion

    #region SHY (SYA/SAY) - $9C - Absolute,X - 5 cykli

    /// <summary>
    /// SHY (SYA/SAY) - Store Y AND (high byte + 1)
    /// Opcode: $9C - Absolute,X - 5 cycles
    /// </summary>
    private void ShyAbsX_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void ShyAbsX_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void ShyAbsX_Cycle2()
    {
        _tempAddr += _x;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _x)) & 0xFF00) != 0;
    }

    private void ShyAbsX_Cycle3()
    {
        byte h = (byte)((_tempAddr >> 8) + 1);
        _memory.Write(_tempAddr, (byte)(_y & h));
    }

    private void ShyAbsX_Cycle4()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    #endregion

    #region TAS (XAS/SHS) - $9B - Absolute,Y - 5 cykli

    /// <summary>
    /// TAS (XAS/SHS) - Store A AND X in SP, then Store SP AND (high byte + 1)
    /// Opcode: $9B - Absolute,Y - 5 cycles
    /// </summary>
    private void TasAbsY_Cycle0()
    {
        byte low = _memory.Read(_pc++);
        _tempAddr = low;
    }

    private void TasAbsY_Cycle1()
    {
        byte high = _memory.Read(_pc++);
        _tempAddr |= (ushort)(high << 8);
    }

    private void TasAbsY_Cycle2()
    {
        _tempAddr += _y;
        _pageCrossed = ((_tempAddr ^ (_tempAddr - _y)) & 0xFF00) != 0;
    }

    private void TasAbsY_Cycle3()
    {
        _sp = (byte)(_a & _x);
        byte h = (byte)((_tempAddr >> 8) + 1);
        _memory.Write(_tempAddr, (byte)(_sp & h));
    }

    private void TasAbsY_Cycle4()
    {
        if (_pageCrossed)
        {
            // Additional cycle for page crossing - dummy read
            _memory.Read(_tempAddr);
        }
        _sync = true;
    }

    #endregion
}
