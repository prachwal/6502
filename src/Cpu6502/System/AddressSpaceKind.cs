namespace Cpu6502.System;

/// <summary>
/// Rodzaj przestrzeni adresowej dla urządzeń.
/// </summary>
public enum AddressSpaceKind
{
    /// <summary>Urządzenie jest mapowane w przestrzeni pamięci (memory-mapped I/O).</summary>
    Memory,

    /// <summary>Urządzenie jest mapowane w przestrzeni portów (port-mapped I/O).</summary>
    Port
}
