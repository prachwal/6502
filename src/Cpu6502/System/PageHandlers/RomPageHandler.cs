namespace Cpu6502.System;

/// <summary>
/// Handler strony ROM. Obsługuje odczyt z pamięci ROM.
/// Zapis jest obsługiwany zgodnie z polityką RomWritePolicy.
/// </summary>
public sealed class RomPageHandler : IPageHandler
{
    private readonly byte[] _data;
    private readonly RomWritePolicy _writePolicy;
    private readonly string? _regionName;

    /// <summary>
    /// Tworzy nowy handler strony ROM.
    /// </summary>
    /// <param name="data">Tablica bajtów reprezentująca stronę ROM (256 bajtów).</param>
    /// <param name="writePolicy">Polityka obsługi zapisu do ROM.</param>
    /// <param name="regionName">Opcjonalna nazwa regionu dla logowania.</param>
    public RomPageHandler(byte[] data, RomWritePolicy writePolicy = RomWritePolicy.ThrowException, string? regionName = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _writePolicy = writePolicy;
        _regionName = regionName;
        if (data.Length < 256)
            throw new ArgumentException("ROM page data must be at least 256 bytes", nameof(data));
    }

    /// <summary>
    /// Tworzy nowy handler strony ROM z nową alokacją pamięci.
    /// </summary>
    /// <param name="fillValue">Wartość wypełnienia (domyślnie 0xFF).</param>
    /// <param name="writePolicy">Polityka obsługi zapisu do ROM.</param>
    /// <param name="regionName">Opcjonalna nazwa regionu dla logowania.</param>
    public RomPageHandler(byte fillValue = 0xFF, RomWritePolicy writePolicy = RomWritePolicy.ThrowException, string? regionName = null)
        : this(new byte[256], writePolicy, regionName)
    {
        for (int i = 0; i < 256; i++)
            _data[i] = fillValue;
    }

    /// <inheritdoc/>
    public byte ReadByte(uint offset) => _data[offset];

    /// <inheritdoc/>
    public void WriteByte(uint offset, byte value)
    {
        switch (_writePolicy)
        {
            case RomWritePolicy.Ignore:
                break;
            case RomWritePolicy.ThrowException:
                throw new RomWriteException(offset, _regionName);
            case RomWritePolicy.LogAndIgnore:
                break;
        }
    }

    /// <summary>
    /// Podstawowa tablica danych strony (tylko do odczytu).
    /// </summary>
    public byte[] Data => _data;

    /// <summary>
    /// Polityka obsługi zapisu.
    /// </summary>
    public RomWritePolicy WritePolicy => _writePolicy;
}

/// <summary>
/// Wyjątek rzucony przy próbie zapisu do obszaru ROM.
/// </summary>
public sealed class RomWriteException : InvalidOperationException
{
    /// <summary>
    /// Tworzy nowy wyjątek.
    /// </summary>
    /// <param name="address">Adres zapisu.</param>
    /// <param name="regionName">Opcjonalna nazwa regionu.</param>
    public RomWriteException(uint address, string? regionName = null)
        : base(FormatMessage(address, regionName))
    {
        Address = address;
        RegionName = regionName;
    }

    /// <summary>Adres, do którego próbowano zapisać.</summary>
    public uint Address { get; }

    /// <summary>Nazwa regionu ROM (jeśli dostępna).</summary>
    public string? RegionName { get; }

    private static string FormatMessage(uint address, string? regionName)
    {
        if (regionName != null)
            return "Write to ROM at address 0x" + address.ToString("X4") + ": region '" + regionName + "'";
        return "Write to ROM at address 0x" + address.ToString("X4");
    }
}
