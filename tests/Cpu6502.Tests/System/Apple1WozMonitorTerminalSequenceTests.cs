using Cpu6502.Apple1.Avalonia.Terminal;
using Cpu6502.System.Apple1;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

[TestFixture]
public class Apple1WozMonitorTerminalSequenceTests
{
    [Test]
    public void Apple1WozMonitor_FullCommandSequence_UsesTerminalInputAndScreenOutput()
    {
        var terminal = new AvaloniaTerminalLink();
        using var screen = new TerminalByteScreenSource(terminal);
        var host = new Apple1Host(terminal);

        RunUntilScreenContains(host, screen, "\\", 20_000);

        SendLine(host, terminal, screen, string.Empty, "\\");
        SendLine(host, terminal, screen, "0", "0000:");
        SendLine(host, terminal, screen, "0.3", "0000: 00");
        Assert.That(screen.GetSnapshotText(), Does.Contain("0000: 00 00 00 00"));
        SendLine(host, terminal, screen, "0: AA 55", "\\");
        SendLine(host, terminal, screen, "0", "0000: AA");
        SendLine(host, terminal, screen, "1", "0001: 55");

        SendLine(host, terminal, screen, "300: 4C 00 FF", "\\");
        SendLine(host, terminal, screen, "300", "0300: 4C");
        SendLine(host, terminal, screen, "301", "0301: 00");
        SendLine(host, terminal, screen, "302", "0302: FF");
        SendLine(host, terminal, screen, "300R", "\\");

        string snapshot = screen.GetSnapshotText();
        Assert.That(snapshot, Does.Contain("300R"));
        Assert.That(snapshot, Does.Contain("0300: 4C"));
        Assert.That(snapshot, Does.Contain("0301: 00"));
        Assert.That(snapshot, Does.Contain("0302: FF"));
        Assert.That(snapshot, Does.Not.Contain("1!1XCVXCV"));
    }

    [Test]
    public void Apple1WozMonitor_BlockExamine_PrintsEightBytesPerLine()
    {
        var terminal = new AvaloniaTerminalLink();
        using var screen = new TerminalByteScreenSource(terminal);
        var host = new Apple1Host(terminal);

        RunUntilScreenContains(host, screen, "\\", 20_000);
        SendLine(host, terminal, screen, "FF00.FF2F", "FF28:");

        string snapshot = screen.GetSnapshotText();
        Assert.That(snapshot, Does.Contain("FF00: D8 58 A0 7F 8C 12 D0 A9"));
        Assert.That(snapshot, Does.Contain("FF08: A7 8D 11 D0 8D 13 D0 C9"));
        Assert.That(snapshot, Does.Contain("FF28: F6 AD 11 D0 10 FB AD 10"));
    }

    private static void SendLine(
        Apple1Host host,
        AvaloniaTerminalLink terminal,
        IScreenSource screen,
        string text,
        string expectedScreenText)
    {
        terminal.EnqueueText(text + "\r");
        RunUntilInputConsumed(host, terminal, 40_000);
        RunInstructions(host, 8_000);

        string snapshot = screen.GetSnapshotText();
        Assert.That(snapshot, Does.Contain(text));
        Assert.That(snapshot, Does.Contain(expectedScreenText));
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
