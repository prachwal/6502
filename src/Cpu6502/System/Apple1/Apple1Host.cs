using Cpu6502.System.Builder;
using Cpu6502.System.Terminal;

namespace Cpu6502.System.Apple1;

/// <summary>
/// Convenience host for driving an Apple-1 computer from tests, CLI, or UI.
/// </summary>
public sealed class Apple1Host
{
    private readonly ITerminalLink _terminal;
    private readonly Apple1Options _options;

    /// <summary>
    /// Creates an Apple-1 host.
    /// </summary>
    public Apple1Host(ITerminalLink terminal, Apple1Options? options = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _options = options ?? new Apple1Options();
        Computer = Apple1ComputerFactory.Create(_terminal, _options);
    }

    /// <summary>The emulated Apple-1 computer.</summary>
    public EmulatedComputer Computer { get; }

    /// <summary>Resets the computer and returns execution to WOZ Monitor.</summary>
    public void Reset()
    {
        Computer.Reset();
        if (Computer.Cpu is IProgramCounterControl programCounter)
            programCounter.SetProgramCounter(_options.EntryPoint);
    }

    /// <summary>Runs one instruction.</summary>
    public void Step() => Computer.StepInstruction();

    /// <summary>Runs up to the specified number of instructions.</summary>
    public void Run(long instructionCount) => Computer.Run(instructionCount);

    /// <summary>Queues one already encoded Apple-1 key byte.</summary>
    public void TypeKey(byte value)
    {
        if (_terminal is BufferedTerminalLink buffered)
            buffered.EnqueueInput(value);
        else if (_terminal is IApple1HostInput hostInput)
            hostInput.EnqueueApple1Key(value);
        else
            throw new NotSupportedException("The configured terminal does not support host input injection.");
    }

    /// <summary>Queues text encoded for Apple-1 keyboard input.</summary>
    public void TypeText(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        foreach (char c in text)
            TypeKey(Apple1KeyMapper.MapCharacter(c));
    }

    /// <summary>Reads all available terminal output when the link supports buffering.</summary>
    public string ReadOutput()
    {
        if (_terminal is BufferedTerminalLink buffered)
            return buffered.ReadOutputText(TerminalTextEncoding.Apple1);

        return string.Empty;
    }

    /// <summary>Runs until expected output appears or the instruction limit is reached.</summary>
    public RunResult RunUntilOutput(string expectedText, long maxInstructions)
    {
        if (expectedText == null)
            throw new ArgumentNullException(nameof(expectedText));

        string accumulated = string.Empty;
        for (long i = 0; i < maxInstructions; i++)
        {
            Step();
            accumulated += ReadOutput();
            if (accumulated.Contains(expectedText, StringComparison.Ordinal))
                return new RunResult(i + 1, true);
        }

        return new RunResult(maxInstructions, false);
    }

    /// <summary>Runs a bounded number of instructions. Placeholder for future input-wait detection.</summary>
    public RunResult RunUntilInputWait(long maxInstructions)
    {
        Run(maxInstructions);
        return new RunResult(maxInstructions, false);
    }

    /// <summary>Encodes a host character as an Apple-1 key byte.</summary>
    public static byte EncodeApple1Key(char keyChar) => Apple1KeyMapper.MapCharacter(keyChar);
}
