namespace Cpu6502.System.Terminal;

/// <summary>
/// Represents a bidirectional link between a device and a terminal/host.
/// This is the primary abstraction for terminal I/O in the emulator.
/// Implementations are responsible for:
/// - Buffering input/output as needed
/// - Text encoding/decoding
/// - Synchronization (if multi-threaded)
/// </summary>
public interface ITerminalLink : IByteInput, IByteOutput
{
}
