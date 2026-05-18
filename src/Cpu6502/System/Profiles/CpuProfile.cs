namespace Cpu6502.System.Profiles;

/// <summary>
/// Profile describing the CPU of a computer.
/// </summary>
/// <param name="Type">Type identifier for the CPU (e.g., "mos6502-nmos", "z80", "m6809").</param>
/// <param name="ClockHz">Clock frequency in Hz.</param>
/// <param name="InitialPC">Optional initial program counter value (defaults to reset vector).</param>
public sealed record CpuProfile(
    string Type,
    long ClockHz = 1000000,
    uint? InitialPC = null)
{
    /// <summary>
    /// Creates a default MOS 6502 NMOS profile.
    /// </summary>
    public static CpuProfile Mos6502Nmos => new("mos6502-nmos", 1023000);

    /// <summary>
    /// Creates a default Z80 profile.
    /// </summary>
    public static CpuProfile Z80 => new("z80", 3580000);

    /// <summary>
    /// Validates the profile.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when Type is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when ClockHz is invalid.</exception>
    public void Validate(string profileId)
    {
        if (string.IsNullOrWhiteSpace(Type))
            throw new ArgumentNullException(
                nameof(Type),
                $"CPU Type cannot be null or empty. Profile: {profileId}");

        if (ClockHz <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(ClockHz),
                $"ClockHz must be positive. Profile: {profileId}");
    }
}
