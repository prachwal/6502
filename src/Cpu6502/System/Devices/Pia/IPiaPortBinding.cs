namespace Cpu6502.System.Devices.Pia;

/// <summary>
/// Interface for binding PIA ports to external devices (terminal, keyboard matrix, etc.).
/// This abstraction allows the PIA device to work with different external devices
/// without knowing their specific implementation details.
/// </summary>
public interface IPiaPortBinding
{
    /// <summary>
    /// Reads the current state of the pins from the external device.
    /// This is used when the corresponding DDR bit is 0 (input mode).
    /// </summary>
    /// <returns>Byte representing the state of all 8 pins (0-7).</returns>
    byte ReadPins();

    /// <summary>
    /// Writes output values to the external device.
    /// This is used when the corresponding DDR bit is 1 (output mode).
    /// </summary>
    /// <param name="value">The byte value to write to the output pins.</param>
    /// <param name="directionMask">Mask indicating which bits are configured as output (1 = output).
    ///   Only bits where directionMask has 1 will be written to the external device.</param>
    void WritePins(byte value, byte directionMask);

    /// <summary>
    /// Gets whether there is input ready to be read from this port.
    /// This is used to set the status flag (bit 7) in the control register (CRA/CRB).
    /// For Apple-1: CRA.7 = 1 when terminal has input ready.
    /// </summary>
    bool HasInputReady { get; }

    /// <summary>
    /// Gets whether the output is ready for more data.
    /// This is used to set the status flag (bit 7) in the control register and output register.
    /// For Apple-1: ORB.7 = 0 when terminal is ready (inverted logic - WOZ Monitor expects this).
    /// </summary>
    bool IsOutputReady { get; }
}
