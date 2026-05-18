using Cpu6502.System.Profiles;

namespace Cpu6502.System.Factories;

/// <summary>
/// Factory interface for creating device instances from profiles.
/// </summary>
public interface IDeviceFactory
{
    /// <summary>
    /// The type of device this factory creates.
    /// </summary>
    string DeviceType { get; }

    /// <summary>
    /// Creates a device from the given profile.
    /// </summary>
    /// <param name="deviceProfile">The device profile.</param>
    /// <param name="systemBus">The system bus to connect the device to.</param>
    /// <param name="loadOptions">Optional loading options for the profile.</param>
    /// <returns>The created device (must implement IDevice).</returns>
    IDevice CreateDevice(
        DeviceProfile deviceProfile,
        ISystemBus systemBus,
        ProfileLoadOptions? loadOptions = null);
}
