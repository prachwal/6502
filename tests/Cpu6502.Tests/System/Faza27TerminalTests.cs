using NUnit.Framework;
using Cpu6502.System.Terminal;

namespace Cpu6502.Tests.System;

/// <summary>
/// Testy jednostkowe dla Fazy 27 - Abstrakcje terminala.
/// </summary>
[TestFixture]
public class Faza27TerminalTests
{
    // ==================== BufferedTerminalLink - HasInput Tests ====================

    [Test]
    public void BufferedTerminal_WhenEmpty_HasNoInput()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal.HasInput, Is.False);
    }

    [Test]
    public void BufferedTerminal_AfterEnqueueInput_HasInputIsTrue()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x41); // 'A'
        Assert.That(terminal.HasInput, Is.True);
    }

    [Test]
    public void BufferedTerminal_AfterTryReadByte_HasInputIsFalse()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x41);
        _ = terminal.TryReadByte(out _);
        Assert.That(terminal.HasInput, Is.False);
    }

    // ==================== BufferedTerminalLink - FIFO Tests ====================

    [Test]
    public void BufferedTerminal_EnqueueInput_TryReadDequeuesByte()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x41); // 'A'
        terminal.EnqueueInput(0x42); // 'B'

        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0x41));

        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0x42));
    }

    [Test]
    public void BufferedTerminal_TryReadByte_WhenEmpty_ReturnsFalse()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal.TryReadByte(out byte value), Is.False);
        Assert.That(value, Is.EqualTo(0));
    }

    // ==================== BufferedTerminalLink - Output Tests ====================

    [Test]
    public void BufferedTerminal_WriteByte_AppendsOutput()
    {
        var terminal = new BufferedTerminalLink();
        terminal.WriteByte(0x41); // 'A'
        terminal.WriteByte(0x42); // 'B'

        var output = terminal.ReadAllOutputBytes();
        Assert.That(output, Has.Length.EqualTo(2));
        Assert.That(output[0], Is.EqualTo(0x41));
        Assert.That(output[1], Is.EqualTo(0x42));
    }

    [Test]
    public void BufferedTerminal_ReadAllOutputBytes_ClearsOutput()
    {
        var terminal = new BufferedTerminalLink();
        terminal.WriteByte(0x41);
        terminal.WriteByte(0x42);

        _ = terminal.ReadAllOutputBytes();
        var secondRead = terminal.ReadAllOutputBytes();

        Assert.That(secondRead, Has.Length.EqualTo(0));
    }

    [Test]
    public void BufferedTerminal_ReadOutputText_ClearsOutput()
    {
        var terminal = new BufferedTerminalLink();
        terminal.WriteByte(0x41);
        terminal.WriteByte(0x42);

        _ = terminal.ReadOutputText(TerminalTextEncoding.RawBytes);
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(0));
    }

    // ==================== TerminalTextEncoding - RawBytes Tests ====================

    [Test]
    public void RawBytes_Encode_ReturnsAsciiBytes()
    {
        var bytes = TerminalTextEncoding.RawBytes.Encode("ABC");
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
    }

    [Test]
    public void RawBytes_Decode_ReturnsOriginalText()
    {
        var text = TerminalTextEncoding.RawBytes.Decode(new byte[] { 0x41, 0x42, 0x43 });
        Assert.That(text, Is.EqualTo("ABC"));
    }

    // ==================== TerminalTextEncoding - AsciiUppercase Tests ====================

    [Test]
    public void AsciiUppercase_ConvertsLowercaseToUppercase()
    {
        var bytes = TerminalTextEncoding.AsciiUppercase.Encode("abc");
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
    }

    [Test]
    public void AsciiUppercase_PreservesUppercase()
    {
        var bytes = TerminalTextEncoding.AsciiUppercase.Encode("ABC");
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
    }

    [Test]
    public void AsciiUppercase_ConvertsMixedCase()
    {
        var bytes = TerminalTextEncoding.AsciiUppercase.Encode("aBc");
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43 }));
    }

    [Test]
    public void AsciiUppercase_Decode_ReturnsUppercaseText()
    {
        // Decode doesn't change case for AsciiUppercase - it uppercases the result
        var text = TerminalTextEncoding.AsciiUppercase.Decode(new byte[] { 0x61, 0x62, 0x63 });
        Assert.That(text, Is.EqualTo("ABC"));
    }

    // ==================== TerminalTextEncoding - Apple1 Tests ====================

    [Test]
    public void Apple1Encoding_ConvertsLineEndingToCarriageReturn()
    {
        var bytes = TerminalTextEncoding.Apple1.Encode("HELLO\nWORLD");
        var expected = new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x0D, 0x57, 0x4F, 0x52, 0x4C, 0x44 };
        Assert.That(bytes, Is.EqualTo(expected));
    }

    [Test]
    public void Apple1Encoding_ConvertsToUppercase()
    {
        var bytes = TerminalTextEncoding.Apple1.Encode("hello");
        Assert.That(bytes, Is.EqualTo(new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F }));
    }

    [Test]
    public void Apple1Encoding_Decode_ConvertsCarriageReturnToNewline()
    {
        var text = TerminalTextEncoding.Apple1.Decode(new byte[] { 0x48, 0x45, 0x4C, 0x4C, 0x4F, 0x0D });
        Assert.That(text, Is.EqualTo("HELLO\n"));
    }

    // ==================== EnqueueText Tests ====================

    [Test]
    public void EnqueueText_WithRawBytes_EnqueuesExactBytes()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("AB", TerminalTextEncoding.RawBytes);

        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0x41));
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0x42));
    }

    [Test]
    public void EnqueueText_WithAsciiUppercase_ConvertsToUppercase()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("ab", TerminalTextEncoding.AsciiUppercase);

        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0x41)); // 'A'
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0x42)); // 'B'
    }

    [Test]
    public void EnqueueText_WithApple1_ConvertsToUppercaseAndCr()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("a\nb", TerminalTextEncoding.Apple1);

        Assert.That(terminal.TryReadByte(out byte first), Is.True);
        Assert.That(first, Is.EqualTo(0x41)); // 'A'
        Assert.That(terminal.TryReadByte(out byte second), Is.True);
        Assert.That(second, Is.EqualTo(0x0D)); // CR
        Assert.That(terminal.TryReadByte(out byte third), Is.True);
        Assert.That(third, Is.EqualTo(0x42)); // 'B'
    }

    // ==================== Clear Tests ====================

    [Test]
    public void Clear_ClearsBothBuffers()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x01);
        terminal.WriteByte(0x02);

        terminal.Clear();

        Assert.That(terminal.HasInput, Is.False);
        Assert.That(terminal.InputBufferSize, Is.EqualTo(0));
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(0));
    }

    // ==================== Edge Cases ====================

    [Test]
    public void EnqueueText_WithNull_ThrowsArgumentNullException()
    {
        var terminal = new BufferedTerminalLink();
        Assert.Throws<ArgumentNullException>(() => terminal.EnqueueText(null!, TerminalTextEncoding.RawBytes));
    }

    [Test]
    public void Encode_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TerminalTextEncoding.RawBytes.Encode(null!));
    }

    [Test]
    public void Decode_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => TerminalTextEncoding.RawBytes.Decode(null!));
    }

    [Test]
    public void EnqueueText_WithEmptyString_DoesNothing()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("", TerminalTextEncoding.RawBytes);
        Assert.That(terminal.HasInput, Is.False);
    }

    [Test]
    public void ReadOutputText_WithEmptyBuffer_ReturnsEmptyString()
    {
        var terminal = new BufferedTerminalLink();
        var text = terminal.ReadOutputText(TerminalTextEncoding.RawBytes);
        Assert.That(text, Is.EqualTo(""));
    }

    // ==================== Buffer Size Tests ====================

    [Test]
    public void InputBufferSize_ReturnsCorrectCount()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal.InputBufferSize, Is.EqualTo(0));
        
        terminal.EnqueueInput(0x01);
        Assert.That(terminal.InputBufferSize, Is.EqualTo(1));
        
        terminal.EnqueueInput(0x02);
        Assert.That(terminal.InputBufferSize, Is.EqualTo(2));
        
        terminal.TryReadByte(out _);
        Assert.That(terminal.InputBufferSize, Is.EqualTo(1));
    }

    [Test]
    public void OutputBufferSize_ReturnsCorrectCount()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(0));
        
        terminal.WriteByte(0x01);
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(1));
        
        terminal.WriteByte(0x02);
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(2));
        
        terminal.ReadAllOutputBytes();
        Assert.That(terminal.OutputBufferSize, Is.EqualTo(0));
    }

    // ==================== Interface Tests ====================

    [Test]
    public void BufferedTerminalLink_Implements_IByteInput()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal, Is.AssignableTo<IByteInput>());
    }

    [Test]
    public void BufferedTerminalLink_Implements_IByteOutput()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal, Is.AssignableTo<IByteOutput>());
    }

    [Test]
    public void BufferedTerminalLink_Implements_ITerminalLink()
    {
        var terminal = new BufferedTerminalLink();
        Assert.That(terminal, Is.AssignableTo<ITerminalLink>());
    }
}
