using System;

namespace Cpu6502.System;

/// <summary>
/// Deskryptor przestrzeni adresowej CPU.
/// Definiuje charakterystykę przestrzeni pamięci i portów dla różnych architektur.
/// </summary>
public sealed record AddressSpaceDescriptor
{
    /// <summary>Liczba bitów adresowych przestrzeni pamięci.</summary>
    public int MemoryAddressBits { get; }
    
    /// <summary>Liczba bitów adresowych przestrzeni portów I/O.</summary>
    public int PortAddressBits { get; }
    
    /// <summary>Czy CPU ma oddzielną przestrzeń portów I/O.</summary>
    public bool HasSeparatePortSpace { get; }
    
    /// <summary>Liczba bitów szyny danych.</summary>
    public int DataBusBits { get; }
    
    /// <summary>
    /// Deskryptor dla MOS 6502 / 6510 / 65C02.
    /// </summary>
    public static readonly AddressSpaceDescriptor Mos6502 = new(16, 0, false, 8);
    
    /// <summary>
    /// Deskryptor dla Z80 / 8080.
    /// </summary>
    public static readonly AddressSpaceDescriptor Z80 = new(16, 8, true, 8);
    
    /// <summary>
    /// Deskryptor dla Motorola 6809.
    /// </summary>
    public static readonly AddressSpaceDescriptor Motorola6809 = new(16, 0, false, 8);
    
    /// <summary>
    /// Tworzy nowy deskryptor przestrzeni adresowej.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Gdy parametry są nieprawidłowe.</exception>
    public AddressSpaceDescriptor(int memoryAddressBits, int portAddressBits, bool hasSeparatePortSpace, int dataBusBits)
    {
        if (memoryAddressBits < 8 || memoryAddressBits > 32)
            throw new ArgumentOutOfRangeException(nameof(memoryAddressBits), "Must be between 8 and 32");
        if (portAddressBits < 0 || portAddressBits > 32)
            throw new ArgumentOutOfRangeException(nameof(portAddressBits), "Must be between 0 and 32");
        if (dataBusBits < 8 || dataBusBits > 64)
            throw new ArgumentOutOfRangeException(nameof(dataBusBits), "Must be between 8 and 64");
        
        MemoryAddressBits = memoryAddressBits;
        PortAddressBits = portAddressBits;
        HasSeparatePortSpace = hasSeparatePortSpace;
        DataBusBits = dataBusBits;
    }
}
