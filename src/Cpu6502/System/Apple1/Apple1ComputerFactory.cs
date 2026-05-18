using Cpu6502.System.Builder;
using Cpu6502.System.Factories;
using Cpu6502.System.Profiles;
using Cpu6502.System.Terminal;

namespace Cpu6502.System.Apple1;

/// <summary>
/// Creates Apple-1 computers using the profile/runtime builder path.
/// </summary>
public static class Apple1ComputerFactory
{
    /// <summary>
    /// Creates an Apple-1 computer wired to the supplied terminal link.
    /// </summary>
    public static EmulatedComputer Create(ITerminalLink terminal, Apple1Options? options = null)
    {
        if (terminal == null)
            throw new ArgumentNullException(nameof(terminal));

        options ??= new Apple1Options();
        string root = ResolveRepositoryRoot(options.RepositoryRoot);
        string profilePath = Path.Combine(root, options.ProfilePath);

        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromFile(profilePath);
        profile = profile with { Cpu = profile.Cpu with { InitialPC = options.EntryPoint } };
        var romDataOverrides = LoadRomDataOverrides(root, profile);

        var loadOptions = new ProfileLoadOptions(
            RomDataOverrides: romDataOverrides,
            TerminalLinks: new Dictionary<string, ITerminalLink>
            {
                ["pia0"] = terminal
            });

        var registry = CreateRegistry();
        return new ComputerBuilder(registry).Build(profile, loadOptions);
    }

    private static Dictionary<string, byte[]> LoadRomDataOverrides(string root, ComputerProfile profile)
    {
        var overrides = new Dictionary<string, byte[]>();

        foreach (var rom in profile.Memory.RomRegions)
        {
            if (string.IsNullOrWhiteSpace(rom.File))
                continue;

            string romPath = Path.Combine(root, rom.File);
            overrides[rom.File] = File.ReadAllBytes(romPath);
        }

        return overrides;
    }

    private static DeviceFactoryRegistry CreateRegistry()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(new Mos6502CpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos));
        registry.RegisterDeviceFactory(new Mos682xPiaDeviceFactory());
        return registry;
    }

    private static string ResolveRepositoryRoot(string? configuredRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
            return Path.GetFullPath(configuredRoot);

        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "6502Emulator.slnx")))
                return current;

            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == current)
                break;
            current = parent ?? string.Empty;
        }

        return Directory.GetCurrentDirectory();
    }
}
