namespace Cpu6502.System.Apple1;

/// <summary>
/// Options for creating an Apple-1 host/computer.
/// </summary>
public sealed record Apple1Options(
    string? RepositoryRoot = null,
    string ProfilePath = "profiles/computers/apple-1.json",
    string WozMonitorRomPath = "roms/apple-1/wozmon.bin",
    ushort EntryPoint = 0xFF00,
    long DefaultInstructionsPerFrame = 2_000)
{
    public const string WozMonitorProfilePath = "profiles/computers/apple-1.json";
    public const string BasicProfilePath = "profiles/computers/apple-1-basic.json";

    public static Apple1Options WozMonitor => new(ProfilePath: WozMonitorProfilePath);

    public static Apple1Options Basic => new(ProfilePath: BasicProfilePath);
}
