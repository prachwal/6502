namespace Cpu6502.System;

/// <summary>
/// Interfejs rdzenia CPU.
/// Definiuje podstawowe operacje, które każdy CPU musi implementować.
/// </summary>
public interface ICpuCore
{
    /// <summary>Typ CPU (np. "mos6502-nmos", "z80", "m6809").</summary>
    string CpuType { get; }
    
    /// <summary>Deskryptor przestrzeni adresowej CPU.</summary>
    AddressSpaceDescriptor AddressSpace { get; }
    
    /// <summary>Resetuje CPU do stanu początkowego.</summary>
    void Reset();
    
    /// <summary>Wykonuje jedną pełną instrukcję.</summary>
    void StepInstruction();
    
    /// <summary>Wykonuje jeden cykl zegara.</summary>
    /// <remarks>Dla CPU, które nie obsługują cykli (np. obecny 6502), może rzucić NotSupportedException.</remarks>
    void StepCycle();
    
    /// <summary>Pobiera snapshot stanu CPU.</summary>
    /// <returns>Migawka stanu CPU.</returns>
    CpuSnapshot GetSnapshot();
}
