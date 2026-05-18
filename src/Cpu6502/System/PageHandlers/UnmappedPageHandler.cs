namespace Cpu6502.System;

/// <summary>
/// Handler strony niezmapowanej (unmapped memory).
/// Odczyt zwraca 0xFF, zapis jest ignorowany.
/// </summary>
public sealed class UnmappedPageHandler : IPageHandler
{
    /// <summary>Instancja singleton.</summary>
    public static readonly UnmappedPageHandler Instance = new();

    private UnmappedPageHandler() { }

    /// <inheritdoc/>
    /// <remarks>Zawsze zwraca 0xFF dla niezmapowanej pamięci.</remarks>
    public byte ReadByte(uint offset) => 0xFF;

    /// <inheritdoc/>
    /// <remarks>Zapis do niezmapowanej pamięci jest ignorowany.</remarks>
    public void WriteByte(uint offset, byte value) { }
}
