using System;
using System.Collections.Generic;

namespace Cpu6502.System;

/// <summary>
/// Adapter dla istniejącego Cpu6502, który opakowuje go w interfejs ICpuCore.
/// Pozwala używać Cpu6502 w nowym systemie runtime bez modyfikacji oryginalnego kodu.
/// </summary>
public sealed class Cpu6502CoreAdapter : ICpuCore, ICpuSignalSink, IProgramCounterControl
{
    private readonly Cpu6502 _cpu;
    private readonly string _cpuType;
    
    /// <summary>
    /// Tworzy nowy adapter dla podanego CPU 6502.
    /// </summary>
    /// <param name="cpu">Instancja CPU 6502 do opakowania.</param>
    /// <param name="cpuType">Typ CPU (domyślnie "mos6502-nmos").</param>
    public Cpu6502CoreAdapter(Cpu6502 cpu, string cpuType = "mos6502-nmos")
    {
        _cpu = cpu ?? throw new ArgumentNullException(nameof(cpu));
        _cpuType = cpuType ?? throw new ArgumentNullException(nameof(cpuType));
    }
    
    /// <inheritdoc/>
    public string CpuType => _cpuType;
    
    /// <inheritdoc/>
    public AddressSpaceDescriptor AddressSpace => AddressSpaceDescriptor.Mos6502;
    
    /// <inheritdoc/>
    public void Reset()
    {
        _cpu.Reset();
    }
    
    /// <inheritdoc/>
    public void StepInstruction()
    {
        _cpu.StepInstruction();
    }
    
    /// <inheritdoc/>
    /// <remarks>Obecna implementacja Cpu6502 wykonuje pełne instrukcje, nie pojedyncze cykle.</remarks>
    public void StepCycle()
    {
        throw new NotSupportedException(
            "Cpu6502 currently executes full instructions. StepCycle() is not supported. " +
            "Use StepInstruction() for instruction-level stepping.");
    }
    
    /// <inheritdoc/>
    public CpuSnapshot GetSnapshot()
    {
        // Pobierz flagi z rejestru P
        var status = _cpu.Status;
        var flags = new Dictionary<string, bool>
        {
            ["N"] = (status & Cpu6502.FlagN) != 0,
            ["Z"] = (status & Cpu6502.FlagZ) != 0,
            ["C"] = (status & Cpu6502.FlagC) != 0,
            ["I"] = (status & Cpu6502.FlagI) != 0,
            ["D"] = (status & Cpu6502.FlagD) != 0,
            ["V"] = (status & Cpu6502.FlagV) != 0,
            ["U"] = (status & Cpu6502.FlagU) != 0,
            ["B"] = (status & Cpu6502.FlagB) != 0
        };
        
        // Pobierz rejestry
        var registers = new Dictionary<string, ulong>
        {
            ["A"] = _cpu.A,
            ["X"] = _cpu.X,
            ["Y"] = _cpu.Y,
            ["PC"] = _cpu.PC,
            ["SP"] = _cpu.SP,
            ["P"] = _cpu.Status
        };
        
        return new CpuSnapshot(
            _cpuType,
            _cpu.PC,
            _cpu.SP,
            registers,
            flags,
            (long)_cpu.CycleCount,
            (long)_cpu.InstructionCount);
    }

    /// <inheritdoc/>
    public void SetProgramCounter(ulong address)
    {
        _cpu.PC = (ushort)address;
    }
    
    /// <inheritdoc/>
    public void SetSignal(CpuSignal signal, bool asserted)
    {
        switch (signal)
        {
            case CpuSignal.Reset:
                if (asserted)
                    _cpu.Reset();
                break;
                
            case CpuSignal.Irq:
                _cpu.SetIRQ(asserted);
                break;
                
            case CpuSignal.Nmi:
                _cpu.SetNMI(asserted);
                break;
                
            // Te sygnały nie są obsługiwane przez 6502
            case CpuSignal.Int:
            case CpuSignal.Firq:
            case CpuSignal.Halt:
            case CpuSignal.Ready:
            case CpuSignal.BusRequest:
                // Ignoruj sygnały nieobsługiwane przez 6502
                break;
                
            default:
                throw new ArgumentOutOfRangeException(nameof(signal), signal, null);
        }
    }
}

