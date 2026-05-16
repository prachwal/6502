using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class InterruptTests
{
    private FlatMemory? memory;
    private Cpu6502? cpu;

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502(memory);
        memory.Write(0xFFFC, 0x00);
        memory.Write(0xFFFD, 0x00);
        cpu.Reset();
    }

    private void LoadProgram(params byte[] program)
    {
        // Ładuj program od $0100, aby nie nadpisywać Zero Page ($00-$FF)
        ushort baseAddr = 0x0100;
        for (int i = 0; i < program.Length; i++)
            memory!.Write((ushort)(baseAddr + i), program[i]);
        cpu!.PC = baseAddr;
        // Wymuś sync aby Tick() pobrał nowy opcode z PC
        var state = cpu.GetState();
        state.Sync = true;
        cpu.SetState(state);
    }

    [Test]
    public void Brk_PushesPCPlus2()
    {
        LoadProgram(0x00, 0x12, 0x34); // BRK at $0100, signature byte $12
        cpu!.SP = 0xFF;
        
        // Set IRQ vector to $3412
        memory!.Write(0xFFFE, 0x12);
        memory.Write(0xFFFF, 0x34);
        
        cpu.Tick(); // Execute BRK
        
        // After BRK: PC should be at $FFFE/$FFFF vector
        Assert.That(cpu.PC, Is.EqualTo(0x3412));
        
        // Stack should have: PCH, PCL, P (with B=1)
        // SP starts at 0xFF, after 3 pushes: SP = 0xFC
        Assert.That(cpu.SP, Is.EqualTo(0xFC));
    }

    [Test]
    public void Brk_PushesPWithBFlagSet()
    {
        LoadProgram(0x00); // BRK
        cpu!.SP = 0xFF;
        cpu.P = 0x00; // Clear all flags
        
        cpu.Tick(); // Execute BRK
        
        // Pushed P should have B=1 (bit 4) and bit5=1
        byte pushedP = memory!.Read((ushort)(0x0100 + cpu.SP + 1));
        Assert.That((pushedP & 0x30), Is.EqualTo(0x30)); // B=1 and bit5=1
    }

    [Test]
    public void Brk_SetsInterruptFlag()
    {
        LoadProgram(0x00); // BRK
        cpu!.P = 0x00; // Clear all flags
        
        cpu.Tick(); // Execute BRK
        
        // I flag should be set after BRK
        Assert.That((cpu.P & 0x04), Is.EqualTo(0x04)); // I=1
    }

    [Test]
    public void Brk_JumpsToInterruptVector()
    {
        LoadProgram(0x00); // BRK
        
        // Set IRQ vector to $4000
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x40);
        
        cpu.Tick(); // Execute BRK
        
        // PC should be at $4000
        Assert.That(cpu.PC, Is.EqualTo(0x4000));
    }

    [Test]
    public void Rti_RestoresPCAndP()
    {
        LoadProgram(0x40); // RTI
        cpu!.SP = 0xFC; // Stack pointer after BRK
        
        // Set up stack: P, PCL, PCH
        memory!.Write(0x01FD, 0x24); // P (with some flags)
        memory.Write(0x01FE, 0x34); // PCL
        memory.Write(0x01FF, 0x12); // PCH
        
        cpu.Tick(); // Execute RTI
        
        // PC should be restored to $1234
        Assert.That(cpu.PC, Is.EqualTo(0x1234));
        
        // P should be restored to 0x24
        Assert.That(cpu.P, Is.EqualTo(0x24));
    }

    [Test]
    public void Rti_WithBFlagZeroOnStack()
    {
        LoadProgram(0x40); // RTI
        cpu!.SP = 0xFC;
        
        // Set up stack with B=0
        memory!.Write(0x01FD, 0x04); // P with B=0
        memory.Write(0x01FE, 0x34); // PCL
        memory.Write(0x01FF, 0x12); // PCH
        
        cpu.Tick(); // Execute RTI
        
        // P should be restored with B=0
        Assert.That(cpu.P, Is.EqualTo(0x04));
    }
}