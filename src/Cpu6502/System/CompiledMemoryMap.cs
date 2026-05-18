namespace Cpu6502.System;

/// <summary>
/// Skompilowana mapa pamięci z obsługą stron 256-bajtowych.
/// Pozwala na szybkie routowanie odczytów/zapisów bez iterowania po liście urządzeń.
/// </summary>
public sealed class CompiledMemoryMap
{
    private const int InternalPageSize = 256;
    private const int PageMask = InternalPageSize - 1;
    private const int PageShift = 8;

    private readonly IPageHandler[] _pages;
    private readonly uint _addressSpaceBits;

    /// <summary>
    /// Tworzy nową skompilowaną mapę pamięci.
    /// </summary>
    /// <param name="addressSpaceBits">Liczba bitów adresowych (np. 16 dla 64KB).</param>
    public CompiledMemoryMap(int addressSpaceBits = 16)
    {
        if (addressSpaceBits < 8 || addressSpaceBits > 32)
            throw new ArgumentOutOfRangeException(nameof(addressSpaceBits), "Address space bits must be between 8 and 32");

        _addressSpaceBits = (uint)addressSpaceBits;
        var pageCount = 1U << (addressSpaceBits - PageShift);
        _pages = new IPageHandler[pageCount];

        // Domyślnie wszystkie strony są niezmapowane
        for (uint i = 0; i < pageCount; i++)
            _pages[i] = UnmappedPageHandler.Instance;
    }

    /// <summary>
    /// Liczba bitów adresowych.
    /// </summary>
    public int AddressSpaceBits => (int)_addressSpaceBits;

    /// <summary>
    /// Rozmiar przestrzeni adresowej w bajtach.
    /// </summary>
    public uint Size => 1U << (int)_addressSpaceBits;

    /// <summary>
    /// Rozmiar strony w bajtach (256).
    /// </summary>
    public int PageSize => InternalPageSize;

    /// <summary>
    /// Liczba stron w mapie.
    /// </summary>
    public int PageCount => _pages.Length;

    /// <summary>
    /// Mapuje region pamięci RAM.
    /// </summary>
    /// <param name="startAddress">Adres startowy regionu.</param>
    /// <param name="size">Rozmiar regionu w bajtach.</param>
    /// <param name="fillValue">Wartość wypełnienia (domyślnie 0).</param>
    /// <exception cref="ArgumentException">Jeśli region wykracza poza przestrzeń adresową.</exception>
    public void MapRam(uint startAddress, uint size, byte fillValue = 0)
    {
        ValidateRange(startAddress, size);

        uint endAddress = startAddress + size;
        uint firstPage = startAddress >> PageShift;
        uint lastPage = (endAddress - 1) >> PageShift;

        for (uint pageIndex = firstPage; pageIndex <= lastPage; pageIndex++)
        {
            uint pageStart = pageIndex << PageShift;

            // Utwórz nową stronę RAM
            var pageData = new byte[InternalPageSize];
            if (fillValue != 0)
                for (int i = 0; i < InternalPageSize; i++)
                    pageData[i] = fillValue;

            _pages[pageIndex] = new RamPageHandler(pageData);
        }
    }

    /// <summary>
    /// Mapuje region pamięci ROM.
    /// </summary>
    /// <param name="startAddress">Adres startowy regionu.</param>
    /// <param name="data">Dane ROM do zamapowania.</param>
    /// <param name="writePolicy">Polityka obsługi zapisu.</param>
    /// <param name="regionName">Opcjonalna nazwa regionu.</param>
    /// <exception cref="ArgumentException">Jeśli region wykracza poza przestrzeń adresową.</exception>
    public void MapRom(uint startAddress, byte[] data, RomWritePolicy writePolicy = RomWritePolicy.ThrowException, string? regionName = null)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("ROM data cannot be null or empty", nameof(data));

        ValidateRange(startAddress, (uint)data.Length);

        uint endAddress = startAddress + (uint)data.Length;
        uint firstPage = startAddress >> PageShift;
        uint lastPage = (endAddress - 1) >> PageShift;

        for (uint pageIndex = firstPage; pageIndex <= lastPage; pageIndex++)
        {
            uint pageStart = pageIndex << PageShift;
            uint offsetInRom = pageStart - startAddress;
            uint bytesToCopy = Math.Min((uint)InternalPageSize, (uint)data.Length - offsetInRom);

            var pageData = new byte[InternalPageSize];
            Array.Copy(data, (int)offsetInRom, pageData, 0, (int)bytesToCopy);

            // Wypełnij resztę strony 0xFF
            for (int i = (int)bytesToCopy; i < InternalPageSize; i++)
                pageData[i] = 0xFF;

            _pages[pageIndex] = new RomPageHandler(pageData, writePolicy, regionName);
        }
    }

    /// <summary>
    /// Mapuje urządzenie memory-mapped do mapy.
    /// </summary>
    /// <param name="device">Urządzenie do zamapowania.</param>
    /// <exception cref="ArgumentException">Jeśli urządzenie wykracza poza przestrzeń adresową.</exception>
    public void MapDevice(IMemoryMappedDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        ValidateRange(device.StartAddress, device.Size);

        uint endAddress = device.StartAddress + device.Size;
        uint firstPage = device.StartAddress >> PageShift;
        uint lastPage = (endAddress - 1) >> PageShift;

        for (uint pageIndex = firstPage; pageIndex <= lastPage; pageIndex++)
        {
            uint pageStart = pageIndex << PageShift;
            uint offsetInDevice = pageStart - device.StartAddress;
            _pages[pageIndex] = new DevicePageHandler(device, offsetInDevice);
        }
    }

    /// <summary>
    /// Odczytuje bajt z podanego adresu.
    /// </summary>
    /// <param name="address">Adres do odczytu.</param>
    /// <returns>Wartość odczytana.</returns>
    public byte ReadByte(uint address)
    {
        if (address >= Size)
            throw new ArgumentOutOfRangeException(nameof(address), "Address out of range");

        uint pageIndex = address >> PageShift;
        uint offsetInPage = address & PageMask;
        return _pages[pageIndex].ReadByte(offsetInPage);
    }

    /// <summary>
    /// Zapisuje bajt pod podany adres.
    /// </summary>
    /// <param name="address">Adres docelowy.</param>
    /// <param name="value">Wartość do zapisania.</param>
    public void WriteByte(uint address, byte value)
    {
        if (address >= Size)
            throw new ArgumentOutOfRangeException(nameof(address), "Address out of range");

        uint pageIndex = address >> PageShift;
        uint offsetInPage = address & PageMask;
        _pages[pageIndex].WriteByte(offsetInPage, value);
    }

    /// <summary>
    /// Zwraca handler dla podanej strony.
    /// </summary>
    /// <param name="pageIndex">Indeks strony.</param>
    /// <returns>Handler strony.</returns>
    public IPageHandler GetPageHandler(uint pageIndex) => _pages[pageIndex];

    private void ValidateRange(uint startAddress, uint size)
    {
        if (startAddress >= Size)
            throw new ArgumentOutOfRangeException(nameof(startAddress), "Start address out of range");

        if (size == 0)
            throw new ArgumentException("Size cannot be zero", nameof(size));

        uint endAddress = startAddress + size;
        if (endAddress > Size || endAddress < startAddress) // Overflow check
            throw new ArgumentOutOfRangeException(nameof(size), "Region exceeds address space");
    }
}
