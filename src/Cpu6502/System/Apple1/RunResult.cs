namespace Cpu6502.System.Apple1;

/// <summary>
/// Result returned by bounded Apple-1 run helpers.
/// </summary>
public sealed record RunResult(long InstructionsExecuted, bool ConditionMet);
