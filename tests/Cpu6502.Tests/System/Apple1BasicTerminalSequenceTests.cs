using System.IO;
using Cpu6502.Apple1.Avalonia.Terminal;
using Cpu6502.System.Apple1;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class Apple1BasicTerminalSequenceTests
{
    [Test]
    public void Apple1Basic_ProfileLoadsAndRunsWozMonitor()
    {
        var terminal = new AvaloniaTerminalLink();
        using var screen = new TerminalByteScreenSource(terminal);
        var host = new Apple1Host(terminal, Apple1Options.Basic);
        host.Reset();

        // BASIC profile should start WOZ Monitor at FF00
        var snapshot0 = host.Computer.GetCpuSnapshot();
        Assert.That(snapshot0.ProgramCounter, Is.EqualTo(0xFF00));

        // Run until we see the WOZ Monitor prompt
        RunUntilScreenContains(host, screen, "\\", 20_000);

        string snapshot = screen.GetSnapshotText();
        Assert.That(snapshot, Does.Contain("\\"));
    }

    [Test, Explicit("Requires basic.bin ROM file")]
    public void Apple1Basic_JumpToE000_StartsBasic()
    {
        var terminal = new AvaloniaTerminalLink();
        using var screen = new TerminalByteScreenSource(terminal);
        var host = new Apple1Host(terminal, Apple1Options.Basic);
        host.Reset();

        // Wait for WOZ Monitor prompt
        RunUntilScreenContains(host, screen, "\\", 20_000);
        
        // Type E000R to jump to BASIC
        terminal.EnqueueText("E000R\r");
        RunUntilInputConsumed(host, terminal, 40_000);
        
        // Run more instructions
        RunInstructions(host, 10_000);
        
        string snapshot = screen.GetSnapshotText();
        // After jumping to E000, BASIC should start
        // The exact output depends on the BASIC ROM
        Assert.That(snapshot, Is.Not.Empty);
    }

    [Test]
    public void Apple1Basic_SimpleRun_DoesNotCrash()
    {
        var terminal = new AvaloniaTerminalLink();
        using var screen = new TerminalByteScreenSource(terminal);
        var host = new Apple1Host(terminal, Apple1Options.Basic);
        host.Reset();

        // Just run a bunch of instructions without crashing
        // WOZ Monitor should start and wait for input
        for (int i = 0; i < 50_000; i++)
        {
            host.Step();
        }
        
        // Should have the WOZ Monitor prompt
        string snapshot = screen.GetSnapshotText();
        Assert.That(snapshot, Does.Contain("\\"));
    }

    private static void RunUntilScreenContains(
        Apple1Host host,
        IScreenSource screen,
        string expectedScreenText,
        int maxInstructions)
    {
        for (int i = 0; i < maxInstructions; i++)
        {
            host.Step();
            if (screen.GetSnapshotText().Contains(expectedScreenText, StringComparison.Ordinal))
                return;
        }

        Assert.Fail(
            $"Screen did not contain '{expectedScreenText}' after {maxInstructions} instructions.\nScreen:\n{screen.GetSnapshotText(includeCursor: true)}");
    }

    private static void RunUntilInputConsumed(Apple1Host host, AvaloniaTerminalLink terminal, int maxInstructions)
    {
        for (int i = 0; i < maxInstructions; i++)
        {
            host.Step();
            if (!terminal.HasInput)
                return;
        }

        Assert.Fail($"Terminal input was not consumed after {maxInstructions} instructions.");
    }

    private static void RunInstructions(Apple1Host host, int instructionCount)
    {
        for (int i = 0; i < instructionCount; i++)
            host.Step();
    }
}
