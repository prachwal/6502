using Cpu6502.Apple1.Avalonia.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class MemoryMappedTextScreenSourceTests
{
    [Test]
    public void MemoryMappedTextScreenSource_ReadsConfiguredRange()
    {
        byte[] memory = { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };
        var source = new MemoryMappedTextScreenSource(address => memory[address - 0x200], 0x200, 2, 2);

        source.Refresh();

        Assert.That(source.GetCell(0, 0), Is.EqualTo('A'));
        Assert.That(source.GetCell(0, 1), Is.EqualTo('B'));
        Assert.That(source.GetCell(1, 0), Is.EqualTo('C'));
        Assert.That(source.GetCell(1, 1), Is.EqualTo('D'));
    }

    [Test]
    public void MemoryMappedTextScreenSource_UsesConfiguredDimensions()
    {
        var source = new MemoryMappedTextScreenSource(_ => (byte)' ', 0x400, 32, 16);

        Assert.That(source.Columns, Is.EqualTo(32));
        Assert.That(source.Rows, Is.EqualTo(16));
        Assert.That(source.Name, Is.EqualTo("MEM"));
    }

    [Test]
    public void MemoryMappedTextScreenSource_ThrowsWhenRangeNotConfigured()
    {
        var source = new MemoryMappedTextScreenSource(_ => (byte)' ', null, 40, 24);

        Assert.Throws<InvalidOperationException>(() => source.Refresh());
    }
}
