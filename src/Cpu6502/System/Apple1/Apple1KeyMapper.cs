namespace Cpu6502.System.Apple1;

/// <summary>
/// Central Apple-1 keyboard encoder used by hosts and user interfaces.
/// </summary>
public static class Apple1KeyMapper
{
    /// <summary>Maps a special key to the byte expected by the Apple-1 PIA keyboard port.</summary>
    public static bool TryMapSpecialKey(Apple1SpecialKey key, out byte value)
    {
        value = key switch
        {
            Apple1SpecialKey.Enter => 0x8D,
            Apple1SpecialKey.Escape => 0x9B,
            Apple1SpecialKey.Backspace => 0xDF,
            Apple1SpecialKey.Delete => 0xDF,
            Apple1SpecialKey.LeftArrow => 0xDF,
            _ => 0
        };

        return value != 0;
    }

    /// <summary>Maps a host character to the byte expected by the Apple-1 PIA keyboard port.</summary>
    public static byte MapCharacter(char keyChar)
    {
        if (keyChar == '\n' || keyChar == '\r')
            return 0x8D;

        if (keyChar == '\b' || keyChar == 0x7F)
            return 0xDF;

        if (keyChar == 0x1B)
            return 0x9B;

        if (keyChar == '.')
            return 0xA3;

        return (byte)(char.ToUpperInvariant(keyChar) | 0x80);
    }

    /// <summary>Maps printable text input. Control characters are intentionally ignored.</summary>
    public static bool TryMapPrintableCharacter(char keyChar, out byte value)
    {
        value = 0;

        if (char.IsControl(keyChar) || keyChar < 0x20 || keyChar > 0x7E)
            return false;

        value = MapCharacter(keyChar);
        return true;
    }
}
