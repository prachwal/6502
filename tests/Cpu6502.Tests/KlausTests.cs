using Cpu6502;
using Cpu6502.Tests.TestHelpers;
using Cpu6502.Variants;
using NUnit.Framework;

namespace Cpu6502.Tests;

/// <summary>
/// Testy zgodności Klaus Dormann 6502 Functional Test Suite.
/// </summary>
[TestFixture]
public class KlausTests
{
    private FlatMemory _memory;
    private Cpu6502 _cpu;
    private KlausTestRunner _runner;

    [SetUp]
    public void Setup()
    {
        _memory = new FlatMemory();
        _cpu = new Cpu6502Classic(_memory);
        _runner = new KlausTestRunner(_cpu, _memory);
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann bez trybu BCD (podstawowe instrukcje)")]
    public void Klaus_NonBcdTest_Passes()
    {
        // Użyj klasycznego 6502 z wyłączonym BCD dla testu non-BCD
        _cpu.DecimalModeEnabled = false;
        
        bool result = _runner.RunNonBcdTest();
        
        Assert.That(result, Is.True, "Klaus Dormann non-BCD test powinien się powieść");
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann z trybem BCD")]
    public void Klaus_BcdTest_Passes()
    {
        // Użyj klasycznego 6502 z włączonym BCD dla testu BCD
        _cpu.DecimalModeEnabled = true;
        
        bool result = _runner.RunBcdTest();
        
        Assert.That(result, Is.True, "Klaus Dormann BCD test powinien się powieść");
    }

    [Test]
    [Category("KlausDormann")]
    [Description("Test Klaus Dormann non-BCD z Cpu6502Nes (Ricoh 2A03)")]
    public void Klaus_NonBcdTest_NesVariant_Passes()
    {
        // Użyj wariantu NES (2A03) - ma wyłączony BCD i poprawiony JMP bug
        var nesCpu = new Cpu6502Nes(_memory!);
        var nesRunner = new KlausTestRunner(nesCpu, _memory);
        
        bool result = nesRunner.RunNonBcdTest();
        
        Assert.That(result, Is.True, "Klaus Dormann non-BCD test powinien się powieść z Cpu6502Nes");
    }
}
