namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Tryby adresowania - Immediate

    /// <summary>
    /// Tryb adresowania natywnego (immediate).
    /// Odczytuje wartość bezpośrednio z pamięci pod PC.
    /// </summary>
    /// <returns>Tuple z adresem (0), wartością i liczbą cykli.</returns>
    private (ushort address, byte value, int cycles) Imm()
    {
        byte val = _memory.Read(_pc);
        _pc++;
        return (0, val, 2);
    }

    #endregion

    #region Tryby adresowania - Zero Page

    /// <summary>
    /// Tryb adresowania zero page (bezpośredni).
    /// Odczytuje 8-bitowy adres z pamięci i odczytuje wartość z tego adresu.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
    private (ushort address, byte value, int cycles) Zp()
    {
        byte addr = _memory.Read(_pc);
        _pc++;
        byte val = _memory.Read(addr);
        return (addr, val, 3);
    }

    /// <summary>
    /// Tryb adresowania zero page + X (indeksowany X).
    /// Odczytuje 8-bitowy adres, dodaje X, odczytuje wartość.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
    private (ushort address, byte value, int cycles) ZpX()
    {
        byte addrBase = _memory.Read(_pc);
        _pc++;
        byte addr = (byte)(addrBase + _x);
        byte val = _memory.Read(addr);
        return (addr, val, 4);
    }

    /// <summary>
    /// Tryb adresowania zero page + Y (indeksowany Y).
    /// Odczytuje 8-bitowy adres, dodaje Y, odczytuje wartość.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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

    /// <summary>
    /// Tryb adresowania bezwzględny.
    /// Odczytuje 16-bitowy adres z pamięci i odczytuje wartość.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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

    /// <summary>
    /// Tryb adresowania bezwzględny + X (indeksowany X).
    /// Odczytuje 16-bitowy adres, dodaje X, zlicza cykle przy page crossing.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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

    /// <summary>
    /// Tryb adresowania bezwzględny + Y (indeksowany Y).
    /// Odczytuje 16-bitowy adres, dodaje Y, zlicza cykle przy page crossing.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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

    /// <summary>
    /// Tryb adresowania pośredni pre-indexed (Indirect,X).
    /// Odczytuje adres z zerowej strony + X, odczytuje wartość z wskazanego adresu.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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

    /// <summary>
    /// Tryb adresowania pośredni post-indexed (Indirect,Y).
    /// Odczytuje adres z zerowej strony, dodaje Y, zlicza cykle przy page crossing.
    /// </summary>
    /// <returns>Tuple z adresem, wartością i liczbą cykli.</returns>
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
}
