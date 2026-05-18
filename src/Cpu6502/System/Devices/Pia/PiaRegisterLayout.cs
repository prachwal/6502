namespace Cpu6502.System.Devices.Pia;

/// <summary>
/// Defines the register layout (offsets) for a PIA device.
/// This allows different configurations for different computer profiles (Apple-1, PET-like, SBC).
/// The standard MOS 6820/6821 PIA has 4 registers at consecutive offsets.
/// </summary>
/// <param name="OraDdraOffset">Offset for ORA/DDRA register (typically 0).</param>
/// <param name="CraOffset">Offset for CRA register (typically 1).</param>
/// <param name="OrbDdrbOffset">Offset for ORB/DDRB register (typically 2).</param>
/// <param name="CrbOffset">Offset for CRB register (typically 3).</param>
public sealed record PiaRegisterLayout(
    int OraDdraOffset,
    int CraOffset,
    int OrbDdrbOffset,
    int CrbOffset)
{
    /// <summary>Standard Apple-1 and most common layout: ORA/DDRA=0, CRA=1, ORB/DDRB=2, CRB=3.</summary>
    public static readonly PiaRegisterLayout Standard = new(0, 1, 2, 3);

    /// <summary>Validates that the layout is correct.</summary>
    /// <exception cref="ArgumentException">Thrown when offsets are not unique.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when offsets are out of range (0-3).</exception>
    public void Validate()
    {
        // All offsets must be unique
        var offsets = new HashSet<int> { OraDdraOffset, CraOffset, OrbDdrbOffset, CrbOffset };
        if (offsets.Count != 4)
            throw new ArgumentException("All register offsets must be unique.");

        // All offsets must be in valid range (0-3)
        foreach (var offset in offsets)
        {
            if (offset < 0 || offset > 3)
                throw new ArgumentOutOfRangeException(nameof(offset), 
                    "Register offsets must be in range 0-3.");
        }
    }

    /// <summary>
    /// Gets the offset for the data register of Port A (ORA).
    /// This is the same as OraDdraOffset since ORA and DDRA share the same address.
    /// </summary>
    public int OraOffset => OraDdraOffset;

    /// <summary>
    /// Gets the offset for the data direction register of Port A (DDRA).
    /// This is the same as OraDdraOffset since ORA and DDRA share the same address.
    /// </summary>
    public int DdraOffset => OraDdraOffset;

    /// <summary>
    /// Gets the offset for the data register of Port B (ORB).
    /// This is the same as OrbDdrbOffset since ORB and DDRB share the same address.
    /// </summary>
    public int OrbOffset => OrbDdrbOffset;

    /// <summary>
    /// Gets the offset for the data direction register of Port B (DDRB).
    /// This is the same as OrbDdrbOffset since ORB and DDRB share the same address.
    /// </summary>
    public int DdrbOffset => OrbDdrbOffset;
}
