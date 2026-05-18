using Cpu6502.Apple1.Avalonia.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class Apple1ScreenBufferTests
{
    [Test]
    public void Apple1ScreenBuffer_WrapsAt40Columns()
    {
        var buffer = new Apple1ScreenBuffer();

        for (int i = 0; i < 40; i++)
            buffer.Write((byte)'A');

        Assert.That(buffer.CursorColumn, Is.EqualTo(0));
        Assert.That(buffer.CursorRow, Is.EqualTo(1));
    }

    [Test]
    public void Apple1ScreenBuffer_ScrollsAt24Rows()
    {
        var buffer = new Apple1ScreenBuffer();

        for (int i = 0; i < 25; i++)
        {
            buffer.Write((byte)('A' + i));
            buffer.Write(0x0D);
        }

        string display = buffer.ToDisplayText(includeCursor: false);
        Assert.That(display[0], Is.EqualTo('C'));
        Assert.That(buffer.CursorRow, Is.EqualTo(23));
    }

    [Test]
    public void Apple1ScreenBuffer_CarriageReturnStartsNewLine()
    {
        var buffer = new Apple1ScreenBuffer();

        buffer.Write((byte)'A');
        buffer.Write(0x0D);
        buffer.Write((byte)'B');

        string display = buffer.ToDisplayText(includeCursor: false);
        Assert.That(display.Split(Environment.NewLine)[0][0], Is.EqualTo('A'));
        Assert.That(display.Split(Environment.NewLine)[1][0], Is.EqualTo('B'));
    }

    [Test]
    public void Apple1ScreenBuffer_CursorRendersAsApple1AtSign()
    {
        var buffer = new Apple1ScreenBuffer();

        string display = buffer.ToDisplayText(includeCursor: true);

        Assert.That(display[0], Is.EqualTo('@'));
    }
}
