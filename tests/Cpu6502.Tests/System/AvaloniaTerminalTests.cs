using Cpu6502.Apple1.Avalonia.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class AvaloniaTerminalTests
{
    [Test]
    public void AvaloniaTerminalLink_InputDequeuesFifo()
    {
        var terminal = new AvaloniaTerminalLink();

        terminal.EnqueueText("AB");

        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0xC1));
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0xC2));
    }

    [Test]
    public void AvaloniaTerminalLink_MapsSpecialCharacters()
    {
        var terminal = new AvaloniaTerminalLink();

        terminal.EnqueueText("\r\u001b\b");

        Assert.That(terminal.TryReadByte(out byte enter), Is.True);
        Assert.That(enter, Is.EqualTo(0x8D));
        Assert.That(terminal.TryReadByte(out byte escape), Is.True);
        Assert.That(escape, Is.EqualTo(0x9B));
        Assert.That(terminal.TryReadByte(out byte backspace), Is.True);
        Assert.That(backspace, Is.EqualTo(0xDF));
    }

    [Test]
    public void AvaloniaTerminalLink_OutputRaisesEvent()
    {
        var terminal = new AvaloniaTerminalLink();
        byte? output = null;
        terminal.OutputWritten += (_, value) => output = value;

        terminal.WriteByte(0x41);

        Assert.That(output, Is.EqualTo(0x41));
    }

    [Test]
    public void AvaloniaTerminalLink_FifoInputAfterMappedKeys()
    {
        var terminal = new AvaloniaTerminalLink();

        terminal.EnqueueText("300R\r");

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
        Assert.That(terminal.HasInput, Is.False);
    }

    [Test]
    public void TerminalByteScreenSource_WritesOutputBytesToBuffer()
    {
        var terminal = new AvaloniaTerminalLink();
        using var source = new TerminalByteScreenSource(terminal);

        terminal.WriteByte(0xC1);

        Assert.That(source.Name, Is.EqualTo("PIA"));
        Assert.That(source.GetCell(0, 0), Is.EqualTo('A'));
        Assert.That(source.CursorColumn, Is.EqualTo(1));
    }
}
