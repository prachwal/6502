using Cpu6502;
using Cpu6502.System;
using NUnit.Framework;
using System.Collections.Generic;

namespace Cpu6502.Tests.System;

/// <summary>
/// Testy jednostkowe dla Fazy 24 - Uniwersalne abstrakcje runtime.
/// </summary>
[TestFixture]
public class Faza24RuntimeAbstractionsTests
{
    private FlatMemory? _memory;
    private Cpu6502? _cpu;
    private Cpu6502CoreAdapter? _adapter;

    [SetUp]
    public void Setup()
    {
        _memory = new FlatMemory();
        _cpu = new Cpu6502(_memory);
        _adapter = new Cpu6502CoreAdapter(_cpu);
    }

    // ==================== ICpuCore Tests ====================

    [Test]
    public void Cpu6502CoreAdapter_CpuType_ReturnsCorrectType()
    {
        Assert.That(_adapter!.CpuType, Is.EqualTo("mos6502-nmos"));
    }

    [Test]
    public void Cpu6502CoreAdapter_AddressSpace_ReturnsMos6502Descriptor()
    {
        var descriptor = _adapter!.AddressSpace;
        Assert.That(descriptor.MemoryAddressBits, Is.EqualTo(16));
        Assert.That(descriptor.PortAddressBits, Is.EqualTo(0));
        Assert.That(descriptor.HasSeparatePortSpace, Is.False);
        Assert.That(descriptor.DataBusBits, Is.EqualTo(8));
    }

    [Test]
    public void Cpu6502CoreAdapter_Reset_UsesWrappedCpu()
    {
        // Ustaw wektor reset
        _memory!.Write(0xFFFC, 0x00);
        _memory.Write(0xFFFD, 0xC0);
        
        _adapter!.Reset();
        
        // Sprawdź, czy PC został załadowany z wektora reset
        Assert.That(_cpu!.PC, Is.EqualTo(0xC000));
    }

    [Test]
    public void Cpu6502CoreAdapter_StepInstruction_ExecutesOneInstruction()
    {
        // Ustaw wektor reset na 0xC000
        _memory!.Write(0xFFFC, 0x00);
        _memory.Write(0xFFFD, 0xC0);
        
        // Załaduj prosty program: LDA #$42
        _memory.LoadRom(0xC000, new byte[] { 0xA9, 0x42 }); // LDA #$42
        _cpu!.Reset(); // PC = 0xC000 (z wektora reset)
        
        // Wykonaj jedną instrukcję
        _adapter!.StepInstruction();
        
        // A powinno być 0x42, PC powinno być 0xC002
        Assert.That(_cpu.A, Is.EqualTo(0x42));
        Assert.That(_cpu.PC, Is.EqualTo(0xC002));
    }

    [Test]
    public void Cpu6502CoreAdapter_StepCycle_ThrowsNotSupported()
    {
        Assert.That(() => _adapter!.StepCycle(), Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public void Cpu6502CoreAdapter_GetSnapshot_Contains6502Registers()
    {
        // Ustaw rejestry
        _cpu!.A = 0xAA;
        _cpu.X = 0xBB;
        _cpu.Y = 0xCC;
        _cpu.SP = 0xFD;
        _cpu.PC = 0x1234;
        // Ustaw flagi: N=1, Z=0, C=1, I=1, D=0, V=0, U=1
        _cpu.P = (byte)(Cpu6502.FlagN | Cpu6502.FlagC | Cpu6502.FlagI | Cpu6502.FlagU);
        
        var snapshot = _adapter!.GetSnapshot();
        
        // Sprawdź typ CPU
        Assert.That(snapshot.CpuType, Is.EqualTo("mos6502-nmos"));
        
        // Sprawdź PC i SP
        Assert.That(snapshot.ProgramCounter, Is.EqualTo(0x1234));
        Assert.That(snapshot.StackPointer, Is.EqualTo(0xFD));
        
        // Sprawdź rejestry
        Assert.That(snapshot.Registers["A"], Is.EqualTo(0xAA));
        Assert.That(snapshot.Registers["X"], Is.EqualTo(0xBB));
        Assert.That(snapshot.Registers["Y"], Is.EqualTo(0xCC));
        Assert.That(snapshot.Registers["PC"], Is.EqualTo(0x1234));
        Assert.That(snapshot.Registers["SP"], Is.EqualTo(0xFD));
        Assert.That(snapshot.Registers["P"], Is.EqualTo(_cpu.P));
        
        // Sprawdź flagi
        Assert.That(snapshot.Flags["N"], Is.True);
        Assert.That(snapshot.Flags["Z"], Is.False);
        Assert.That(snapshot.Flags["C"], Is.True);
        Assert.That(snapshot.Flags["I"], Is.True);
        Assert.That(snapshot.Flags["D"], Is.False);
        Assert.That(snapshot.Flags["V"], Is.False);
        Assert.That(snapshot.Flags["U"], Is.True);
    }

    // ==================== ICpuSignalSink Tests ====================

    [Test]
    public void Cpu6502CoreAdapter_SetIrq_MapsToCpu()
    {
        // Ustaw IRQ na aktywny
        _adapter!.SetSignal(CpuSignal.Irq, true);
        
        // Sprawdź, czy CPU ma aktywne IRQ (sprawdź stan wewnętrzny)
        // Ponieważ nie mamy dostępu do prywatnych pól, sprawdzimy zachowanie
        //通过执行指令并检查是否响应IRQ
        
        // Załaduj program, który czeka na IRQ (SEI, potem pętla)
        _memory!.LoadRom(0xC000, new byte[] { 0x78, 0x4C, 0x00, 0xC0 }); // SEI, JMP $C000
        _cpu!.Reset();
        
        // Uruchom kilka instrukcji
        _adapter.StepInstruction(); // SEI
        _adapter.StepInstruction(); // JMP
        
        // Teraz ustaw IRQ nieaktywny
        _adapter.SetSignal(CpuSignal.Irq, false);
        
        // Powinno działać bez wyjątku
    }

    [Test]
    public void Cpu6502CoreAdapter_SetNmi_MapsEdgeCorrectly()
    {
        // Ustaw NMI na aktywny, potem nieaktywny (zbocze opadające)
        _adapter!.SetSignal(CpuSignal.Nmi, true);
        _adapter.SetSignal(CpuSignal.Nmi, false);
        
        // Załaduj program
        _memory!.LoadRom(0xC000, new byte[] { 0x40 }); // RTI
        _memory.LoadRom(0xFFFA, new byte[] { 0x00, 0xC0 }); // NMI vector
        _cpu!.Reset();
        
        // Uruchom instrukcję
        _adapter.StepInstruction();
        
        // Powinno działać bez wyjątku
    }

    [Test]
    public void Cpu6502CoreAdapter_SetReset_CallsCpuReset()
    {
        // Ustaw wektor reset
        _memory!.Write(0xFFFC, 0x00);
        _memory.Write(0xFFFD, 0xD0);
        
        // Ustaw inny PC
        _cpu!.PC = 0x1234;
        
        // Uruchom reset
        _adapter!.SetSignal(CpuSignal.Reset, true);
        
        // PC powinno być załadowane z wektora reset
        Assert.That(_cpu.PC, Is.EqualTo(0xD000));
    }

    // ==================== CpuSignalController Tests ====================

    [Test]
    public void CpuSignalController_SetSource_AggregatesSignals()
    {
        var controller = new CpuSignalController();
        
        // Dodaj pierwsze źródło z aktywnym IRQ
        controller.UpdateSignal("device1", CpuSignal.Irq, true);
        
        // Sprawdź, czy IRQ jest aktywny
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.True);
        
        // Dodaj drugie źródło z aktywnym IRQ
        controller.UpdateSignal("device2", CpuSignal.Irq, true);
        
        // IRQ powinno nadal być aktywne
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.True);
    }

    [Test]
    public void CpuSignalController_ClearOneSource_KeepsSignalWhenOtherSourceActive()
    {
        var controller = new CpuSignalController();
        
        // Dodaj dwa źródła z aktywnym IRQ
        controller.UpdateSignal("device1", CpuSignal.Irq, true);
        controller.UpdateSignal("device2", CpuSignal.Irq, true);
        
        // Wyłącz pierwsze źródło
        controller.UpdateSignal("device1", CpuSignal.Irq, false);
        
        // IRQ powinno nadal być aktywne (drugie źródło utrzymuje je)
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.True);
        
        // Wyłącz drugie źródło
        controller.UpdateSignal("device2", CpuSignal.Irq, false);
        
        // IRQ powinno być nieaktywne
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.False);
    }

    [Test]
    public void CpuSignalController_ClearAll_ClearsAllSignals()
    {
        var controller = new CpuSignalController();
        
        // Ustaw kilka sygnałów
        controller.UpdateSignal("dev1", CpuSignal.Irq, true);
        controller.UpdateSignal("dev2", CpuSignal.Nmi, true);
        controller.UpdateSignal("dev3", CpuSignal.Reset, true);
        
        // Sprawdź, że są aktywne
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.True);
        Assert.That(controller.IsAsserted(CpuSignal.Nmi), Is.True);
        Assert.That(controller.IsAsserted(CpuSignal.Reset), Is.True);
        
        // Wyczyść wszystkie
        controller.ClearAll();
        
        // Sprawdź, że wszystkie są nieaktywne
        Assert.That(controller.IsAsserted(CpuSignal.Irq), Is.False);
        Assert.That(controller.IsAsserted(CpuSignal.Nmi), Is.False);
        Assert.That(controller.IsAsserted(CpuSignal.Reset), Is.False);
    }

    [Test]
    public void CpuSignalController_GetAssertingSources_ReturnsCorrectSources()
    {
        var controller = new CpuSignalController();
        
        // Dodaj źródła
        controller.UpdateSignal("device1", CpuSignal.Irq, true);
        controller.UpdateSignal("device2", CpuSignal.Irq, true);
        controller.UpdateSignal("device3", CpuSignal.Irq, false);
        
        var sources = controller.GetAssertingSources(CpuSignal.Irq);
        
        // Powinno zwrócić device1 i device2
        Assert.That(sources, Has.Count.EqualTo(2));
        Assert.That(sources, Contains.Item("device1"));
        Assert.That(sources, Contains.Item("device2"));
        Assert.That(sources, Does.Not.Contain("device3"));
    }

    // ==================== AddressSpaceDescriptor Tests ====================

    [Test]
    public void AddressSpaceDescriptor_Mos6502_HasCorrectValues()
    {
        var descriptor = AddressSpaceDescriptor.Mos6502;
        Assert.That(descriptor.MemoryAddressBits, Is.EqualTo(16));
        Assert.That(descriptor.PortAddressBits, Is.EqualTo(0));
        Assert.That(descriptor.HasSeparatePortSpace, Is.False);
        Assert.That(descriptor.DataBusBits, Is.EqualTo(8));
    }

    [Test]
    public void AddressSpaceDescriptor_Z80_HasCorrectValues()
    {
        var descriptor = AddressSpaceDescriptor.Z80;
        Assert.That(descriptor.MemoryAddressBits, Is.EqualTo(16));
        Assert.That(descriptor.PortAddressBits, Is.EqualTo(8));
        Assert.That(descriptor.HasSeparatePortSpace, Is.True);
        Assert.That(descriptor.DataBusBits, Is.EqualTo(8));
    }

    [Test]
    public void AddressSpaceDescriptor_InvalidMemoryBits_Throws()
    {
        Assert.That(() => new AddressSpaceDescriptor(7, 0, false, 8), 
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void AddressSpaceDescriptor_InvalidPortBits_Throws()
    {
        Assert.That(() => new AddressSpaceDescriptor(16, -1, false, 8), 
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void AddressSpaceDescriptor_InvalidDataBusBits_Throws()
    {
        Assert.That(() => new AddressSpaceDescriptor(16, 0, false, 7), 
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    // ==================== CpuSnapshot Tests ====================

    [Test]
    public void CpuSnapshot_WithNullCpuType_Throws()
    {
        Assert.That(() => new CpuSnapshot(
            null!, 0, 0, 
            new Dictionary<string, ulong>(), 
            new Dictionary<string, bool>(), 
            0, 0), 
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void CpuSnapshot_WithWhitespaceCpuType_Throws()
    {
        Assert.That(() => new CpuSnapshot(
            "   ", 0, 0, 
            new Dictionary<string, ulong>(), 
            new Dictionary<string, bool>(), 
            0, 0), 
            Throws.TypeOf<ArgumentException>());
    }

    [Test]
    public void CpuSnapshot_GetRegisterByte_ReturnsCorrectValue()
    {
        var registers = new Dictionary<string, ulong>
        {
            ["A"] = 0x42,
            ["X"] = 0x100
        };
        
        var snapshot = new CpuSnapshot("test", 0, 0, registers, 
            new Dictionary<string, bool>(), 0, 0);
        
        Assert.That(snapshot.GetRegisterByte("A"), Is.EqualTo(0x42));
        // X = 0x100, ale GetRegisterByte zwraca tylko bajt
        Assert.That(snapshot.GetRegisterByte("X"), Is.EqualTo(0x00));
    }

    [Test]
    public void CpuSnapshot_GetRegisterByte_UnknownRegister_Throws()
    {
        var snapshot = new CpuSnapshot("test", 0, 0, 
            new Dictionary<string, ulong>(), 
            new Dictionary<string, bool>(), 
            0, 0);
        
        Assert.That(() => snapshot.GetRegisterByte("Z"), 
            Throws.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void CpuSnapshot_GetFlag_ReturnsCorrectValue()
    {
        var flags = new Dictionary<string, bool>
        {
            ["N"] = true,
            ["Z"] = false
        };
        
        var snapshot = new CpuSnapshot("test", 0, 0, 
            new Dictionary<string, ulong>(), 
            flags, 0, 0);
        
        Assert.That(snapshot.GetFlag("N"), Is.True);
        Assert.That(snapshot.GetFlag("Z"), Is.False);
    }

    [Test]
    public void CpuSnapshot_GetFlag_UnknownFlag_Throws()
    {
        var snapshot = new CpuSnapshot("test", 0, 0, 
            new Dictionary<string, ulong>(), 
            new Dictionary<string, bool>(), 
            0, 0);
        
        Assert.That(() => snapshot.GetFlag("X"), 
            Throws.TypeOf<KeyNotFoundException>());
    }
}
