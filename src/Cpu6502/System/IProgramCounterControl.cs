namespace Cpu6502.System;

/// <summary>
/// Optional CPU adapter capability for host-level boot entry control.
/// </summary>
public interface IProgramCounterControl
{
    /// <summary>Sets the program counter.</summary>
    void SetProgramCounter(ulong address);
}
