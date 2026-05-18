using Cpu6502.System.Apple1;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class Apple1KeyMapperTests
{
    [Test]
    public void Apple1KeyMapper_MapsEnterEscBackspace()
    {
        Assert.That(Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Enter, out byte enter), Is.True);
        Assert.That(enter, Is.EqualTo(0x8D));
        Assert.That(Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Escape, out byte escape), Is.True);
        Assert.That(escape, Is.EqualTo(0x9B));
        Assert.That(Apple1KeyMapper.TryMapSpecialKey(Apple1SpecialKey.Backspace, out byte backspace), Is.True);
        Assert.That(backspace, Is.EqualTo(0xDF));
    }

    [Test]
    public void Apple1KeyMapper_UppercasesLettersAndSetsHighBit()
    {
        Assert.That(Apple1KeyMapper.MapCharacter('a'), Is.EqualTo(0xC1));
        Assert.That(Apple1KeyMapper.MapCharacter('Z'), Is.EqualTo(0xDA));
        Assert.That(Apple1KeyMapper.MapCharacter('0'), Is.EqualTo(0xB0));
        Assert.That(Apple1KeyMapper.MapCharacter('.'), Is.EqualTo(0xA3));
    }

    [Test]
    public void Apple1ScreenPanel_TextInput_DoesNotDuplicateSpecialKeys()
    {
        Assert.That(Apple1KeyMapper.TryMapPrintableCharacter('\r', out _), Is.False);
        Assert.That(Apple1KeyMapper.TryMapPrintableCharacter('\u001b', out _), Is.False);
        Assert.That(Apple1KeyMapper.TryMapPrintableCharacter('\b', out _), Is.False);
    }
}
