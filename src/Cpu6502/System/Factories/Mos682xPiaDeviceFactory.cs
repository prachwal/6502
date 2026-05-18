using Cpu6502.System.Devices.Pia;
using Cpu6502.System.Profiles;
using Cpu6502.System.Terminal;

namespace Cpu6502.System.Factories;

/// <summary>
/// Factory for creating MOS 6820/6821 PIA devices from profiles.
/// This factory supports creating PIA devices with different configurations
/// and binding them to external devices like terminals or keyboard matrices.
/// </summary>
public sealed class Mos682xPiaDeviceFactory : IDeviceFactory
{
    /// <summary>Device type identifier for MOS 6821 PIA.</summary>
    public const string DeviceTypeMos6821 = "mos6821-pia";

    /// <summary>Device type identifier for MOS 6820 PIA.</summary>
    public const string DeviceTypeMos6820 = "mos6820-pia";

    /// <summary>Preset name for Apple-1 terminal binding.</summary>
    public const string PresetApple1Terminal = "apple-1-terminal";

    /// <summary>Gets the device type this factory creates.</summary>
    public string DeviceType => DeviceTypeMos6821;

    /// <summary>
    /// Creates a PIA device from a device profile.
    /// </summary>
    /// <param name="deviceProfile">The device profile.</param>
    /// <param name="systemBus">The system bus to connect to.</param>
    /// <param name="loadOptions">Optional loading options.</param>
    /// <returns>The created PIA device.</returns>
    /// <exception cref="ArgumentNullException">Thrown when deviceProfile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when profile is invalid.</exception>
    public IDevice CreateDevice(
        DeviceProfile deviceProfile,
        ISystemBus systemBus,
        ProfileLoadOptions? loadOptions = null)
    {
        if (deviceProfile == null)
            throw new ArgumentNullException(nameof(deviceProfile));

        if (deviceProfile.Type != DeviceTypeMos6821 && deviceProfile.Type != DeviceTypeMos6820)
            throw new ArgumentException(
                $"Device factory {DeviceType} cannot create device of type '{deviceProfile.Type}'");

        // Parse base address from mapping
        uint baseAddress = deviceProfile.Mapping.ParsedBaseAddress;

        // Get preset name from options
        string? preset = null;
        if (deviceProfile.Options != null && deviceProfile.Options.TryGetPropertyValue("preset", out var presetNode))
            preset = presetNode?.GetValue<string>();

        // Create bindings based on preset
        // Note: For now, we use NullPiaPortBinding as the terminal needs to be
        // provided separately. The Apple1TerminalBinding will be created by
        // the caller when using the static CreateApple1Terminal method.
        IPiaPortBinding portABinding = new NullPiaPortBinding();
        IPiaPortBinding portBBinding = new NullPiaPortBinding();

        // If preset is apple-1-terminal and we have a terminal in loadOptions' extensions,
        // we would use it, but ProfileLoadOptions doesn't have Extensions yet.
        // For now, the static CreateApple1Terminal method is the primary way to create
        // a fully configured Apple-1 PIA device.
        
        if (preset == PresetApple1Terminal)
        {
            if (loadOptions?.TerminalLinks != null &&
                loadOptions.TerminalLinks.TryGetValue(deviceProfile.Id, out var terminal))
            {
                return CreateApple1Terminal(baseAddress, terminal, deviceProfile.Id);
            }
        }

        // Create and return the PIA device
        var device = new Mos682xPiaDevice(
            baseAddress,
            portABinding,
            portBBinding,
            layout: null, // Use standard layout
            id: deviceProfile.Id);

        return device;
    }

    // ==================== Static Factory Methods ====================

    /// <summary>
    /// Creates a MOS 6821 PIA device for Apple-1 terminal with standard configuration.
    /// This is a convenience method for creating Apple-1 compatible PIA devices.
    /// 
    /// The PIA is initialized with WOZ Monitor settings:
    /// - DDRB = 0x7F (Port B: bits 0-6 = output, bit 7 = input)
    /// - CRA = 0xA7 (bit 2 = 1, so ORA is at offset 0)
    /// - CRB = 0xA7 (bit 2 = 1, so ORB is at offset 2)
    /// </summary>
    /// <param name="baseAddress">Base address for the PIA (typically 0xD010 for Apple-1).</param>
    /// <param name="terminal">The terminal link for keyboard/display I/O.</param>
    /// <param name="id">Optional device identifier.</param>
    /// <returns>Configured PIA device for Apple-1 terminal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when terminal is null.</exception>
    public static Mos682xPiaDevice CreateApple1Terminal(
        uint baseAddress,
        ITerminalLink terminal,
        string? id = null)
    {
        if (terminal == null)
            throw new ArgumentNullException(nameof(terminal));

        var keyboardBinding = new Apple1TerminalBinding(terminal, isKeyboardPort: true);
        var displayBinding = new Apple1TerminalBinding(terminal, isKeyboardPort: false);

        var device = new Mos682xPiaDevice(
            baseAddress,
            keyboardBinding,
            displayBinding,
            layout: null,
            id: id);

        // Initialize with WOZ Monitor settings
        // Note: We need to set CRA.2=0 first to access DDRA/DDRB, then set to 1 for ORA/ORB
        
        // Write to CRB first: set CRB.2 = 0 to access DDRB at offset 2
        device.WriteMemory(3, 0x00); // CRB = 0 (bit 2 = 0)
        // Now write DDRB = 0x7F at offset 2
        device.WriteMemory(2, 0x7F); // DDRB = 0x7F
        
        // Set CRB = 0xA7 (bit 2 = 1, so ORB is at offset 2)
        device.WriteMemory(3, 0xA7); // CRB = 0xA7
        
        // Set CRA = 0xA7 (bit 2 = 1, so ORA is at offset 0)
        device.WriteMemory(1, 0xA7); // CRA = 0xA7

        return device;
    }

    /// <summary>
    /// Creates a MOS 6821 PIA device with custom bindings.
    /// </summary>
    /// <param name="baseAddress">Base address for the PIA.</param>
    /// <param name="portABinding">Binding for Port A.</param>
    /// <param name="portBBinding">Binding for Port B.</param>
    /// <param name="id">Optional device identifier.</param>
    /// <returns>PIA device with custom bindings.</returns>
    public static Mos682xPiaDevice CreateWithBindings(
        uint baseAddress,
        IPiaPortBinding portABinding,
        IPiaPortBinding portBBinding,
        string? id = null)
    {
        return new Mos682xPiaDevice(
            baseAddress,
            portABinding,
            portBBinding,
            layout: null,
            id: id);
    }

    /// <summary>
    /// Creates a MOS 6821 PIA device with no external bindings (null bindings).
    /// Useful for testing or when ports are not connected.
    /// </summary>
    /// <param name="baseAddress">Base address for the PIA.</param>
    /// <param name="id">Optional device identifier.</param>
    /// <returns>PIA device with null bindings.</returns>
    public static Mos682xPiaDevice CreateWithNullBindings(uint baseAddress, string? id = null)
    {
        return new Mos682xPiaDevice(
            baseAddress,
            new NullPiaPortBinding(),
            new NullPiaPortBinding(),
            layout: null,
            id: id);
    }

    // ==================== Register Factory ====================

    /// <summary>
    /// Registers this factory with the default device factory registry.
    /// </summary>
    public static void RegisterDefault()
    {
        DeviceFactoryRegistry.Default.RegisterDeviceFactory(new Mos682xPiaDeviceFactory());
    }

    /// <summary>
    /// Registers this factory with a specific registry.
    /// </summary>
    /// <param name="registry">The registry to register with.</param>
    public static void RegisterWith(DeviceFactoryRegistry registry)
    {
        if (registry == null)
            throw new ArgumentNullException(nameof(registry));
        
        registry.RegisterDeviceFactory(new Mos682xPiaDeviceFactory());
    }
}
