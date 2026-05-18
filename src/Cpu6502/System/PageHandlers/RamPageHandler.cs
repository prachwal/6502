namespace Cpu6502.System;

/// <summary>
/// Handler strony RAM. Obsługuje odczyt i zapis do pamięci RAM.
/// </summary>
public sealed class RamPageHandler : IPageHandler
{
    private readonly byte[] _data;

    /// <summary>
    /// Tworzy nowy handler strony RAM.
    /// </summary>
    /// <param name="data">Tablica bajtów reprezentująca stronę (256 bajtów).</param>
    public RamPageHandler(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        if (data.Length < 256)
            throw new ArgumentException("RAM page data must be at least 256 bytes", nameof(data));
    }

    /// <summary>
    /// Tworzy nowy handler strony RAM z nową alokacją pamięci.
    /// </summary>
    /// <param name="fillValue">Wartość wypełnienia (domyślnie 0).</param>
    public RamPageHandler(byte fillValue = 0) : this(new byte[256])
    {
        if (fillValue != 0)
            for (int i = 0; i < 256; i++)
                _data[i] = fillValue;
    }

    /// <inheritdoc/>
    public byte ReadByte(uint offset) => _data[offset];

    /// <inheritdoc/>
    public void WriteByte(uint offset, byte value) => _data[offset] = value;

    /// <summary>
    /// Podstawowa tablica danych strony.
    /// </summary>
    public byte[] Data => _data;
}
