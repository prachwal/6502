namespace Cpu6502.System.Terminal;

/// <summary>
/// Optional host-side input injection contract for Apple-1 terminal links.
/// </summary>
public interface IApple1HostInput
{
    /// <summary>Queues one Apple-1 encoded key byte.</summary>
    void EnqueueApple1Key(byte value);
}
