using Cpu6502.System.Terminal;

namespace Cpu6502.System.Devices.Pia;

/// <summary>
/// PIA port binding for Apple-1 terminal device.
/// This binding connects PIA Port A (keyboard) and Port B (display) to an <see cref="ITerminalLink".
/// 
/// Apple-1 Specifics:
/// - KBD ($D010, ORA): Reads keyboard input with bit 7 = 1 when character is ready
/// - KBDCR ($D011, CRA): Bit 7 = 1 when terminal has input ready
/// - DSP ($D012, ORB): Writes display output (bits 0-6 only, bit 7 is input per DDRB=0x7F)
/// - DSPCR ($D013, CRB): Bit 7 = 0 when terminal is ready (inverted logic!)
/// 
/// WOZ Monitor expects:
/// - LDA KBDCR / BPL: Waits for CRA.7 = 1 (character ready)
/// - BIT DSP / BPL: Waits for ORB.7 = 0 (display ready)
/// </summary>
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    private readonly bool _isKeyboardPort;

    /// <summary>
    /// Creates a new Apple-1 terminal binding.
    /// </summary>
    /// <param name="terminal">The terminal link to connect to.</param>
    /// <exception cref="ArgumentNullException">Thrown when terminal is null.</exception>
    public Apple1TerminalBinding(ITerminalLink terminal, bool isKeyboardPort = true)
    {
        if (terminal == null)
            throw new ArgumentNullException(nameof(terminal));

        _terminal = terminal;
        _isKeyboardPort = isKeyboardPort;
    }

    /// <summary>
    /// Gets whether there is input ready to be read from the terminal.
    /// This sets CRA.7 = 1 when true.
    /// </summary>
    public bool HasInputReady => _isKeyboardPort && _terminal.HasInput;

    /// <summary>
    /// Gets whether the output is ready for more data.
    /// For Apple-1: ORB.7 = 0 means ready (inverted logic).
    /// Since BufferedTerminalLink is always ready, this returns true.
    /// </summary>
    public bool IsOutputReady => true;

    /// <summary>
    /// Reads the current state of the pins from the terminal (keyboard input).
    /// When a character is available, it returns the character with bit 7 set to 1.
    /// This matches the WOZ Monitor expectation: KBD returns character with high bit set.
    /// 
    /// Reading consumes the next character, matching the keyboard strobe behavior
    /// expected by the monitor input loop.
    /// </summary>
    /// <returns>Byte with bit 7 = 1 if character is available, otherwise 0.</returns>
    public byte ReadPins()
    {
        if (!_isKeyboardPort)
            return 0;

        if (_terminal.HasInput && _terminal.TryReadByte(out byte value))
        {
            // Set bit 7 = 1 (character ready) - WOZ Monitor expects this format
            return (byte)(value | 0x80);
        }
        return 0; // No character available
    }

    /// <summary>
    /// Writes output values to the terminal (display output).
    /// Only bits 0-6 are written to the terminal, as bit 7 is configured as input
    /// in the Apple-1 PIA (DDRB = 0x7F, so bit 7 = 0 = input).
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    /// <param name="directionMask">Mask indicating which bits are configured as output.
    ///   In Apple-1, this is typically 0x7F (bits 0-6 = output, bit 7 = input).</param>
    public void WritePins(byte value, byte directionMask)
    {
        // Only write bits that are configured as output
        // For Apple-1: DDRB = 0x7F, so directionMask typically has bits 0-6 set
        byte outputValue = (byte)(value & directionMask);
        
        if (outputValue != 0 || directionMask != 0)
        {
            _terminal.WriteByte(outputValue);
        }
        // Bit 7 is input when DDRB bit 7 = 0, so it's ignored
    }
}
