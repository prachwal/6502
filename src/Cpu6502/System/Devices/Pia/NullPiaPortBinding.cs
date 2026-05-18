namespace Cpu6502.System.Devices.Pia;

/// <summary>
/// Null implementation of <see cref="IPiaPortBinding"/>. 
/// This is useful for testing and scenarios where a PIA port is not connected to any external device.
/// All reads return 0, all writes are ignored, and it's always ready for output.
/// </summary>
public sealed class NullPiaPortBinding : IPiaPortBinding
{
    /// <summary>Reads from null binding - always returns 0.</summary>
    public byte ReadPins() => 0;

    /// <summary>Writes to null binding - does nothing.</summary>
    public void WritePins(byte value, byte directionMask) { }

    /// <summary>Null binding never has input ready.</summary>
    public bool HasInputReady => false;

    /// <summary>Null binding is always ready for output.</summary>
    public bool IsOutputReady => true;
}
