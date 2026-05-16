// <copyright file="Cpu6502.AddressingHelpers.cs" company="6502 Emulator">
// Copyright © 2026 6502 Emulator. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Helper methods for addressing modes (return tuples)

    /// <summary>
    /// Immediate addressing helper - returns (address, value, pageCrossed)
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) Imm()
    {
        ushort addr = AddrImmediate();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Zero Page addressing helper
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) Zp()
    {
        ushort addr = AddrZp();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Zero Page,X addressing helper
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) ZpX()
    {
        ushort addr = AddrZpX();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Zero Page,Y addressing helper
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) ZpY()
    {
        ushort addr = AddrZpY();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Absolute addressing helper
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) Abs()
    {
        ushort addr = AddrAbs();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Absolute,X addressing helper - may cross page boundary
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) AbsX()
    {
        bool pageCrossed;
        ushort addr = AddrAbsX(out pageCrossed);
        byte val = _memory.Read(addr);
        return (addr, val, pageCrossed);
    }

    /// <summary>
    /// Absolute,Y addressing helper - may cross page boundary
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) AbsY()
    {
        bool pageCrossed;
        ushort addr = AddrAbsY(out pageCrossed);
        byte val = _memory.Read(addr);
        return (addr, val, pageCrossed);
    }

    /// <summary>
    /// Indirect,X addressing helper
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) IndX()
    {
        ushort addr = AddrIndX();
        byte val = _memory.Read(addr);
        return (addr, val, false);
    }

    /// <summary>
    /// Indirect,Y addressing helper - may cross page boundary
    /// </summary>
    /// <returns>Tuple with address, value, and pageCrossed flag</returns>
    private (ushort, byte, bool) IndY()
    {
        bool pageCrossed;
        ushort addr = AddrIndY(out pageCrossed);
        byte val = _memory.Read(addr);
        return (addr, val, pageCrossed);
    }

    #endregion
}