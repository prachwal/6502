using Cpu6502.System.Apple1;
using Cpu6502.System.Devices.Pia;
using Cpu6502.System.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class Apple1HostTests
{
    [Test]
    public void Apple1Factory_Create_ReturnsComputerWithTerminalPia()
    {
        var terminal = new BufferedTerminalLink();

        var computer = Apple1ComputerFactory.Create(terminal);

        Assert.That(computer.Id, Is.EqualTo("apple-1"));
        Assert.That(computer.GetDevice("pia0"), Is.InstanceOf<Mos682xPiaDevice>());
    }

    [Test]
    public void Apple1Factory_CreateBasicProfile_LoadsWozAndBasicRoms()
    {
        var terminal = new BufferedTerminalLink();

        var computer = Apple1ComputerFactory.Create(terminal, Apple1Options.Basic);
        string root = ResolveRepositoryRoot();

        Assert.That(computer.Id, Is.EqualTo("apple-1-basic"));
        Assert.That(computer.ReadMemory(0xFF00), Is.EqualTo(File.ReadAllBytes(Path.Combine(root, "roms/apple-1/wozmon.bin"))[0]));
        Assert.That(computer.ReadMemory(0xE000), Is.EqualTo(File.ReadAllBytes(Path.Combine(root, "roms/apple-1/basic.bin"))[0]));
    }

    [Test]
    public void Apple1Host_TypeText_QueuesTerminalInput()
    {
        var terminal = new BufferedTerminalLink();
        var host = new Apple1Host(terminal);

        host.TypeText("a\r");

        Assert.That(terminal.InputBufferSize, Is.EqualTo(2));
        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0xC1));
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0x8D));
    }

    [Test]
    public void Apple1Host_WozMonitor_ExamineMemory_Returns0000Line()
    {
        var terminal = new BufferedTerminalLink();
        var host = new Apple1Host(terminal);

        host.TypeText("0.3\r");
        var result = host.RunUntilOutput("0000: 00", 20_000);

        Assert.That(result.ConditionMet, Is.True);
    }

    [Test]
    public void Apple1Host_Reset_ShowsPromptAgain()
    {
        var terminal = new BufferedTerminalLink();
        var host = new Apple1Host(terminal);

        host.Reset();
        var result = host.RunUntilOutput("\\", 20_000);

        Assert.That(result.ConditionMet, Is.True);
    }

    [Test]
    public void Apple1Host_Type300R_DoesNotDuplicateCharacters()
    {
        var terminal = new BufferedTerminalLink();
        var host = new Apple1Host(terminal);

        host.TypeText("300R\r");

        Assert.That(terminal.InputBufferSize, Is.EqualTo(5));
        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0xB3));
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0xB0));
        Assert.That(terminal.TryReadByte(out byte third), Is.True);
        Assert.That(third, Is.EqualTo(0xB0));
        Assert.That(terminal.TryReadByte(out byte fourth), Is.True);
        Assert.That(fourth, Is.EqualTo(0xD2));
        Assert.That(terminal.TryReadByte(out byte fifth), Is.True);
        Assert.That(fifth, Is.EqualTo(0x8D));
        Assert.That(terminal.InputBufferSize, Is.Zero);
    }

    private static string ResolveRepositoryRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "6502Emulator.slnx")))
                return current;

            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == current)
                break;

            current = parent ?? string.Empty;
        }

        return Directory.GetCurrentDirectory();
    }
}
