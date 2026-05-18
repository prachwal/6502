namespace Cpu6502.System;

/// <summary>
/// Linie sygnałów CPU dla komunikacji między urządzeniami a procesorem.
/// </summary>
/// <remarks>
/// Te sygnały modelują linie kontrolne typowe dla różnych architektur CPU:
/// - 6502: Reset, IRQ, NMI
/// - Z80: Reset, INT, NMI, Halt, BusRequest, Ready
/// - 6809: Reset, IRQ, FIRQ, NMI, Halt
/// </remarks>
public enum CpuSignal
{
    /// <summary>Sygnal RESET - resetuje CPU i urządzenia.</summary>
    Reset,
    
    /// <summary>Sygnal IRQ - przerwanie maskowalne (6502, Z80 INT, 6809 IRQ).</summary>
    Irq,
    
    /// <summary>Sygnal NMI - niemaskowalne przerwanie (6502, Z80 NMI, 6809 NMI).</summary>
    Nmi,
    
    /// <summary>Sygnal INT - przerwanie maskowalne (Z80).</summary>
    Int,
    
    /// <summary>Sygnal FIRQ - szybkie przerwanie maskowalne (6809).</summary>
    Firq,
    
    /// <summary>Sygnal HALT - CPU zatrzymany (Z80 HALT, 6809 HALT).</summary>
    Halt,
    
    /// <summary>Sygnal READY - CPU gotowy na kolejny cykl magistrali (Z80).</summary>
    Ready,
    
    /// <summary>Sygnal BUS REQUEST - żądanie magistrali (Z80).</summary>
    BusRequest
}
