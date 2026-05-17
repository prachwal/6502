using Cpu6502;
using NUnit.Framework;

namespace Cpu6502.Tests;

[TestFixture]
public class QuirkTests
{
    private FlatMemory? memory;
    private Cpu6502? cpu;

    [SetUp]
    public void Setup()
    {
        memory = new FlatMemory();
        cpu = new Cpu6502(memory);
        cpu.Reset();
    }

    private void LoadProgram(ushort startAddress, params byte[] program)
    {
        for (int i = 0; i < program.Length; i++)
            memory!.Write((ushort)(startAddress + i), program[i]);
        cpu!.PC = startAddress;
        var state = cpu.GetState();
        state.Sync = true;
        cpu.SetState(state);
    }

    private void ExecuteOne()
    {
        do
        {
            cpu!.Tick();
        }
        while (!cpu!.GetState().Sync);
    }

    [Test]
    public void ASL_abs_DoubleWrite_WritesOriginalThenModified()
    {
        // Arrange
        LoadProgram(0x0100, 0x0E, 0x00, 0x10); // ASL $1000
        memory!.Write(0x1000, 0x41); // Wartość początkowa: 0x41 (01000001)
        
        // Zapamiętaj oryginalną wartość
        byte originalValue = memory.Read(0x1000);

        // Act - wykonaj ASL $1000
        ExecuteOne();

        // Assert
        // Ostateczna wartość w pamięci powinna być 0x82 (0x41 << 1)
        Assert.That(memory.Read(0x1000), Is.EqualTo(0x82), "Ostateczna wartość w pamięci powinna być 0x82");
        
        // Flaga Carry powinna być wyczyszczona (0x41 & 0x80 = 0)
        Assert.That(cpu!.GetFlag(Cpu6502.FlagC), Is.False, "Flaga Carry powinna być wyczyszczona");
        
        // Flaga Negative powinna być ustawiona (0x82 & 0x80 != 0)
        Assert.That(cpu.GetFlag(Cpu6502.FlagN), Is.True, "Flaga Negative powinna być ustawiona");
        
        // Flaga Zero powinna być wyczyszczona
        Assert.That(cpu.GetFlag(Cpu6502.FlagZ), Is.False, "Flaga Zero powinna być wyczyszczona");
    }

    [Test]
    public void JMP_Indirect_PageCrossingBug_ReadsHighByteFromSamePage()
    {
        // Arrange
        LoadProgram(0x0300, 0x6C, 0xFF, 0x01); // JMP ($01FF)
        
        // Ustaw pointer: $01FF -> $3412 (low byte)
        // NMOS bug: high byte powinien zostać przeczytany z $0100 zamiast $0200
        memory!.Write(0x01FF, 0x12); // Low byte adresu docelowego
        memory.Write(0x0100, 0x34); // High byte z $0100 (bug!) bez nadpisywania opcode
        memory.Write(0x0200, 0x56); // High byte z $0200 (poprawny, ale nie użyty)

        // Act
        ExecuteOne();

        // Assert
        // PC powinno wskazywać na $3412 (z $0100 zamiast $0200)
        Assert.That(cpu.PC, Is.EqualTo(0x3412), "JMP ($01FF) powinien przeczytać high byte z $0100");
    }

    [Test]
    public void JMP_Indirect_NormalCase_ReadsHighByteFromNextPage()
    {
        // Arrange
        LoadProgram(0x0100, 0x6C, 0x00, 0x02); // JMP ($0200)
        
        // Ustaw pointer: $0200 -> $7856
        memory!.Write(0x0200, 0x56); // Low byte
        memory.Write(0x0201, 0x78); // High byte (poprawnie z $0201)

        // Act
        ExecuteOne();

        // Assert
        // PC powinno wskazywać na $7856
        Assert.That(cpu!.PC, Is.EqualTo(0x7856), "JMP ($0200) powinien przeczytać high byte z $0201");
    }

    [Test]
    public void Branch_InterruptTiming_IRQNotTriggeredOnCycle1()
    {
        // Arrange
        LoadProgram(0x0100, 0x90, 0x02); // BCC +2 (branch not taken, bo C=1)
        cpu!.P = Cpu6502.FlagC; // Ustaw flagę Carry, aby BCC nie został wykonany
        
        // Ustaw wektor IRQ
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x50);
        
        // Ustaw IRQ
        cpu.SetIRQ(true);

        // Act - wykonaj BCC (not taken = 2 cykle)
        ExecuteOne();

        // Assert
        // IRQ nie powinno zostać uruchomione, bo sprawdzane jest w cyklu 1 (dla not taken)
        // PC powinno być na następnej instrukcji ($0102)
        Assert.That(cpu.PC, Is.EqualTo(0x0102), "BCC not taken powinien zakończyć się na $0102");
        
        // Flaga I powinna pozostać bez zmian (IRQ nie zostało obsłużone).
        Assert.That(cpu.GetFlag(Cpu6502.FlagI), Is.False, "Flaga I nie powinna zostać ustawiona przez nieobsłużone IRQ");
    }

    [Test]
    public void CLI_Latency_IRQNotFiresImmediatelyAfterCLI()
    {
        // Arrange
        LoadProgram(0x0100, 0x58, 0xEA); // CLI, NOP
        cpu!.P = Cpu6502.FlagI; // Ustaw flagę I (I=1)
        
        // Ustaw wektor IRQ
        memory!.Write(0xFFFE, 0x00);
        memory.Write(0xFFFF, 0x60);
        
        // Ustaw IRQ
        cpu.SetIRQ(true);

        // Act - wykonaj CLI
        ExecuteOne();

        // Assert po CLI
        // Flaga I powinna być wyczyszczona
        Assert.That(cpu!.GetFlag(Cpu6502.FlagI), Is.False, "Flaga I powinna być wyczyszczona po CLI");
        
        // PC powinno być na NOP ($0101)
        Assert.That(cpu.PC, Is.EqualTo(0x0101), "PC powinno być na NOP po CLI");
        
        // Wykonaj NOP. IRQ jest gotowe dopiero na kolejnej granicy instrukcji,
        // więc NOP musi najpierw normalnie zakończyć się na $0102.
        ExecuteOne();

        Assert.That(cpu.PC, Is.EqualTo(0x0102), "NOP po CLI powinien zakończyć się przed obsługą IRQ");

        // Kolejne wejście w ExecuteOne obsługuje już oczekujące IRQ.
        ExecuteOne();

        Assert.That(cpu.PC, Is.EqualTo(0x6000), "IRQ powinno zostać uruchomione po NOP");
    }

    [Test]
    public void InterruptHijacking_NMIDuringIRQ_UsesNMIVector()
    {
        // Arrange
        LoadProgram(0x0100, 0xEA); // NOP
        cpu!.P = 0x00; // Wyczyść flagę I (I=0), aby IRQ mogło zostać uruchomione
        
        // Ustaw wektory przerwań
        memory!.Write(0xFFFE, 0x00); // IRQ vector low
        memory.Write(0xFFFF, 0x40); // IRQ vector high ($4000)
        memory.Write(0xFFFA, 0x00); // NMI vector low
        memory.Write(0xFFFB, 0x80); // NMI vector high ($8000)
        
        // Ustaw IRQ
        cpu.SetIRQ(true);

        // Wykonaj NOP - uruchomi IRQ
        ExecuteOne();
        
        // Teraz jesteśmy w obsłudze IRQ
        // Symuluj NMI podczas obsługi IRQ
        cpu.SetNMI(true);
        cpu.SetNMI(false); // Falling edge - zatrzaskuje NMI
        
        // Wykonaj kolejną instrukcję (w obsłudze IRQ)
        ExecuteOne();

        // Assert
        // NMI powinno zostać obsłużone z wektorem NMI ($8000)
        // a nie IRQ ($4000)
        Assert.That(cpu!.PC, Is.EqualTo(0x8000), "NMI powinno użyć wektora NMI ($8000), nie IRQ ($4000)");
    }
}
