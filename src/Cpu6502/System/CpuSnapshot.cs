using System;
using System.Collections.Generic;

namespace Cpu6502.System;

/// <summary>
/// Snapshot (migawka) stanu CPU.
/// Przechowuje stan CPU w formie słownikowej, aby obsłużyć różne architektury
/// (6502, Z80, 6809, itd.) bez konieczności zmiany struktury.
/// </summary>
public sealed record CpuSnapshot
{
    /// <summary>Typ CPU (np. "mos6502-nmos", "z80", "m6809").</summary>
    public string CpuType { get; }
    
    /// <summary>Wskaźnik instrukcji.</summary>
    public ulong ProgramCounter { get; }
    
    /// <summary>Wskaźnik stosu.</summary>
    public ulong StackPointer { get; }
    
    /// <summary>Słownik rejestrów (nazwa -> wartość).</summary>
    public IReadOnlyDictionary<string, ulong> Registers { get; }
    
    /// <summary>Słownik flag (nazwa -> stan).</summary>
    public IReadOnlyDictionary<string, bool> Flags { get; }
    
    /// <summary>Liczba wykonanych cykli zegara.</summary>
    public long CycleCount { get; }
    
    /// <summary>Liczba wykonanych instrukcji.</summary>
    public long InstructionCount { get; }
    
    /// <summary>
    /// Tworzy nowy snapshot z podanymi parametrami.
    /// </summary>
    public CpuSnapshot(
        string cpuType,
        ulong programCounter,
        ulong stackPointer,
        IReadOnlyDictionary<string, ulong> registers,
        IReadOnlyDictionary<string, bool> flags,
        long cycleCount,
        long instructionCount)
    {
        if (string.IsNullOrWhiteSpace(cpuType))
            throw new ArgumentException("CPU type cannot be null or whitespace", nameof(cpuType));
        
        CpuType = cpuType;
        ProgramCounter = programCounter;
        StackPointer = stackPointer;
        Registers = registers;
        Flags = flags;
        CycleCount = cycleCount;
        InstructionCount = instructionCount;
    }
    
    /// <summary>
    /// Zwraca wartość rejestru jako byte.
    /// </summary>
    /// <param name="registerName">Nazwa rejestru.</param>
    /// <returns>Wartość rejestru.</returns>
    /// <exception cref="KeyNotFoundException">Gdy rejestr nie istnieje.</exception>
    public byte GetRegisterByte(string registerName)
    {
        if (Registers.TryGetValue(registerName, out var value))
            return (byte)value;
        throw new KeyNotFoundException("Register '" + registerName + "' not found in snapshot");
    }
    
    /// <summary>
    /// Zwraca flagę jako bool.
    /// </summary>
    /// <param name="flagName">Nazwa flagi.</param>
    /// <returns>Stan flagi.</returns>
    /// <exception cref="KeyNotFoundException">Gdy flaga nie istnieje.</exception>
    public bool GetFlag(string flagName)
    {
        if (Flags.TryGetValue(flagName, out var value))
            return value;
        throw new KeyNotFoundException("Flag '" + flagName + "' not found in snapshot");
    }
}
