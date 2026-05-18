using Cpu6502.System;
using Cpu6502.System.Devices.Pia;
using Cpu6502.System.Factories;
using Cpu6502.System.Profiles;
using Cpu6502.System.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

/// <summary>
/// Unit tests for Phase 28: MOS 6820/6821 PIA medium implementation.
/// </summary>
[TestFixture]
public class Faza28PiaTests
{
    // ==================== Test Helper Classes ====================

    /// <summary>
    /// Mock PIA port binding for testing with controllable input/output.
    /// </summary>
    private sealed class MockPiaPortBinding : IPiaPortBinding
    {
        public byte ExternalInput { get; set; } = 0;
        public bool HasInputReady { get; set; } = false;
        public bool IsOutputReady { get; set; } = true;
        public byte LastWrittenValue { get; private set; }
        public byte LastDirectionMask { get; private set; }

        public byte ReadPins() => ExternalInput;

        public void WritePins(byte value, byte directionMask)
        {
            LastWrittenValue = value;
            LastDirectionMask = directionMask;
        }
    }

    // ==================== PiaRegisterLayout Tests ====================

    [Test]
    public void PiaRegisterLayout_Standard_HasCorrectOffsets()
    {
        var layout = PiaRegisterLayout.Standard;
        Assert.That(layout.OraDdraOffset, Is.EqualTo(0));
        Assert.That(layout.CraOffset, Is.EqualTo(1));
        Assert.That(layout.OrbDdrbOffset, Is.EqualTo(2));
        Assert.That(layout.CrbOffset, Is.EqualTo(3));
    }

    [Test]
    public void PiaRegisterLayout_Validate_WithValidLayout_DoesNotThrow()
    {
        var layout = new PiaRegisterLayout(0, 1, 2, 3);
        Assert.DoesNotThrow(() => layout.Validate());
    }

    [Test]
    public void PiaRegisterLayout_Validate_WithDuplicateOffsets_ThrowsArgumentException()
    {
        var layout = new PiaRegisterLayout(0, 1, 0, 3); // Duplicate offset 0
        Assert.Throws<ArgumentException>(() => layout.Validate());
    }

    [Test]
    public void PiaRegisterLayout_Validate_WithOffsetOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var layout = new PiaRegisterLayout(0, 1, 2, 4); // CRB offset = 4 is invalid
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.Validate());
    }

    // ==================== NullPiaPortBinding Tests ====================

    [Test]
    public void NullPiaPortBinding_ReadPins_ReturnsZero()
    {
        var binding = new NullPiaPortBinding();
        Assert.That(binding.ReadPins(), Is.EqualTo(0));
    }

    [Test]
    public void NullPiaPortBinding_WritePins_DoesNothing()
    {
        var binding = new NullPiaPortBinding();
        Assert.DoesNotThrow(() => binding.WritePins(0xFF, 0xFF));
    }

    [Test]
    public void NullPiaPortBinding_HasInputReady_ReturnsFalse()
    {
        var binding = new NullPiaPortBinding();
        Assert.That(binding.HasInputReady, Is.False);
    }

    [Test]
    public void NullPiaPortBinding_IsOutputReady_ReturnsTrue()
    {
        var binding = new NullPiaPortBinding();
        Assert.That(binding.IsOutputReady, Is.True);
    }

    // ==================== Apple1TerminalBinding Tests ====================

    [Test]
    public void Apple1TerminalBinding_ReadPins_WhenNoInput_ReturnsZero()
    {
        var terminal = new BufferedTerminalLink();
        var binding = new Apple1TerminalBinding(terminal);
        
        Assert.That(binding.ReadPins(), Is.EqualTo(0));
    }

    [Test]
    public void Apple1TerminalBinding_ReadPins_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x41); // 'A'
        var binding = new Apple1TerminalBinding(terminal);
        
        byte result = binding.ReadPins();
        Assert.That(result, Is.EqualTo(0xC1)); // 0x41 | 0x80 = 0xC1
    }

    [Test]
    public void Apple1TerminalBinding_HasInputReady_WhenTerminalHasInput_ReturnsTrue()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0x41);
        var binding = new Apple1TerminalBinding(terminal);
        
        Assert.That(binding.HasInputReady, Is.True);
    }

    [Test]
    public void Apple1TerminalBinding_HasInputReady_WhenTerminalEmpty_ReturnsFalse()
    {
        var terminal = new BufferedTerminalLink();
        var binding = new Apple1TerminalBinding(terminal);
        
        Assert.That(binding.HasInputReady, Is.False);
    }

    [Test]
    public void Apple1TerminalBinding_IsOutputReady_AlwaysReturnsTrue()
    {
        var terminal = new BufferedTerminalLink();
        var binding = new Apple1TerminalBinding(terminal);
        
        Assert.That(binding.IsOutputReady, Is.True);
    }

    [Test]
    public void Apple1TerminalBinding_WritePins_StripsHighBit()
    {
        var terminal = new BufferedTerminalLink();
        var binding = new Apple1TerminalBinding(terminal);
        
        binding.WritePins(0xFF, 0x7F); // Write 0xFF with direction mask 0x7F
        
        byte[] output = terminal.ReadAllOutputBytes();
        Assert.That(output, Has.Length.EqualTo(1));
        Assert.That(output[0], Is.EqualTo(0x7F)); // Only bits 0-6 written
    }

    // ==================== Mos682xPiaDevice Basic Tests ====================

    [Test]
    public void Mos682xPiaDevice_Properties_HaveCorrectValues()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010, "test-pia");
        
        Assert.That(device.Id, Is.EqualTo("test-pia"));
        Assert.That(device.StartAddress, Is.EqualTo(0xD010));
        Assert.That(device.Size, Is.EqualTo(4u));
    }

    [Test]
    public void Mos682xPiaDevice_DefaultId_IsNotEmpty()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        Assert.That(device.Id, Is.Not.Empty);
    }

    // ==================== Register Selection Tests (CRA.2 / CRB.2) ====================

    [Test]
    public void WriteDdra_WhenCraSelectsDdr_StoresDirection()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Set CRA.2 = 0 to select DDRA at offset 0
        device.WriteMemory(1, 0x00);
        // Write to DDRA
        device.WriteMemory(0, 0xFF);
        
        // Read back DDRA (CRA.2 = 0)
        byte result = device.ReadMemory(0);
        Assert.That(result, Is.EqualTo(0xFF));
    }

    [Test]
    public void WritePortA_WhenCraSelectsData_UpdatesOutputLatch()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // First set CRA.2 = 0 to access DDRA
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        // Set DDRA = 0xFF (all bits output)
        device.WriteMemory(0, 0xFF);
        // Set CRA.2 = 1 to select ORA at offset 0
        device.WriteMemory(1, 0x04);
        // Write to ORA
        device.WriteMemory(0, 0xAA);
        
        // Read back ORA (CRA.2 = 1)
        // With DDR=0xFF, all bits are output, so we get ORA value
        byte result = device.ReadMemory(0);
        Assert.That(result, Is.EqualTo(0xAA));
    }

    [Test]
    public void ControlBit2_SelectsDdrOrDataRegister()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Write to DDRA (CRA.2 = 0)
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        device.WriteMemory(0, 0xFF);
        Assert.That(device.ReadMemory(0), Is.EqualTo(0xFF));
        
        // Switch to ORA (CRA.2 = 1)
        device.WriteMemory(1, 0x04); // CRA.2 = 1
        device.WriteMemory(0, 0xAA);
        Assert.That(device.ReadMemory(0), Is.EqualTo(0xAA));
        
        // Switch back to DDRA (CRA.2 = 0)
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        Assert.That(device.ReadMemory(0), Is.EqualTo(0xFF));
    }

    [Test]
    public void WriteDdrB_AndPortB_BehaveLikePortA()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Write to DDRB (CRB.2 = 0)
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        device.WriteMemory(2, 0x55);
        Assert.That(device.ReadMemory(2), Is.EqualTo(0x55));
        
        // Switch to ORB (CRB.2 = 1)
        device.WriteMemory(3, 0x04); // CRB.2 = 1
        // Set DDRB = 0xFF first (need to switch back to CRB.2=0)
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        device.WriteMemory(2, 0xFF); // DDRB = 0xFF
        device.WriteMemory(3, 0x04); // CRB.2 = 1
        device.WriteMemory(2, 0xAA); // ORB = 0xAA
        Assert.That(device.ReadMemory(2), Is.EqualTo(0xAA));
    }

    // ==================== Pin Reading Mixing Tests ====================

    [Test]
    public void ReadPortA_MergesOutputAndExternalInput()
    {
        var mockBinding = new MockPiaPortBinding();
        var device = new Mos682xPiaDevice(
            0xD010,
            mockBinding,
            new NullPiaPortBinding());
        
        // Set CRA.2 = 1 (ORA mode)
        device.WriteMemory(1, 0x04);
        
        // Set DDRA = 0xF0 (bits 4-7 = output, bits 0-3 = input)
        device.WriteMemory(1, 0x00); // CRA.2 = 0 for DDRA access
        device.WriteMemory(0, 0xF0);
        device.WriteMemory(1, 0x04); // CRA.2 = 1 for ORA access
        
        // Set ORA = 0xA0 (bits 4-7 = 0xA)
        device.WriteMemory(0, 0xA0);
        
        // Set external input = 0x0F (bits 0-3)
        mockBinding.ExternalInput = 0x0F;
        
        // Read ORA: (0xA0 & 0xF0) | (0x0F & 0x0F) = 0xA0 | 0x0F = 0xAF
        byte result = device.ReadMemory(0);
        Assert.That(result, Is.EqualTo(0xAF));
    }

    [Test]
    public void ReadPortA_WhenDdraAllOutput_ReturnsOutputLatch()
    {
        var mockBinding = new MockPiaPortBinding { ExternalInput = 0xFF };
        var device = new Mos682xPiaDevice(
            0xD010,
            mockBinding,
            new NullPiaPortBinding());
        
        // Set CRA.2 = 1 (ORA mode)
        device.WriteMemory(1, 0x04);
        
        // Set DDRA = 0xFF (all bits output)
        device.WriteMemory(1, 0x00);
        device.WriteMemory(0, 0xFF);
        device.WriteMemory(1, 0x04);
        
        // Set ORA = 0xAA
        device.WriteMemory(0, 0xAA);
        
        // Read ORA: should return ORA (0xAA) since all bits are output
        byte result = device.ReadMemory(0);
        Assert.That(result, Is.EqualTo(0xAA));
    }

    [Test]
    public void ReadPortA_WhenDdraAllInput_ReturnsExternalInput()
    {
        var mockBinding = new MockPiaPortBinding { ExternalInput = 0x55 };
        var device = new Mos682xPiaDevice(
            0xD010,
            mockBinding,
            new NullPiaPortBinding());
        
        // Set CRA.2 = 1 (ORA mode)
        device.WriteMemory(1, 0x04);
        
        // Set DDRA = 0x00 (all bits input)
        device.WriteMemory(1, 0x00);
        device.WriteMemory(0, 0x00);
        device.WriteMemory(1, 0x04);
        
        // Read ORA: should return external input (0x55) since all bits are input
        byte result = device.ReadMemory(0);
        Assert.That(result, Is.EqualTo(0x55));
    }

    // ==================== Reset Tests ====================

    [Test]
    public void Reset_ClearsDirectionAndOutputLatches()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Set all registers to non-zero values
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        device.WriteMemory(0, 0xFF); // DDRA = 0xFF
        device.WriteMemory(1, 0x04); // CRA.2 = 1
        device.WriteMemory(0, 0xAA); // ORA = 0xAA
        device.WriteMemory(1, 0xA7); // CRA = 0xA7
        
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        device.WriteMemory(2, 0x55); // DDRB = 0x55
        device.WriteMemory(3, 0x04); // CRB.2 = 1
        device.WriteMemory(2, 0xBB); // ORB = 0xBB
        device.WriteMemory(3, 0xA7); // CRB = 0xA7
        
        // Reset
        device.Reset();
        
        // All registers should be 0
        var state = device.GetRegisterState();
        Assert.That(state.DDRA, Is.EqualTo(0));
        Assert.That(state.DDRB, Is.EqualTo(0));
        Assert.That(state.CRA, Is.EqualTo(0));
        Assert.That(state.CRB, Is.EqualTo(0));
        Assert.That(state.ORA, Is.EqualTo(0));
        Assert.That(state.ORB, Is.EqualTo(0));
    }

    // ==================== Address Mapping Tests ====================

    [Test]
    public void Device_WithBaseD010_MapsApple1Offsets()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Write to all registers and read them back
        // CRA doesn't depend on CRA.2 bit for reading/writing
        device.WriteMemory(1, 0xA7); // CRA
        Assert.That(device.ReadMemory(1), Is.EqualTo(0x27)); // 0xA7 & 0x7F (bit 7 cleared by NullPiaPortBinding)
        
        // Set CRA.2 = 0 to access DDRA
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        device.WriteMemory(0, 0xAA); // DDRA
        device.WriteMemory(1, 0x04); // CRA.2 = 1
        
        // Set CRB.2 = 0 to access DDRB
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        device.WriteMemory(2, 0xCC); // DDRB
        device.WriteMemory(3, 0x04); // CRB.2 = 1
        
        // Read DDRA and DDRB
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        Assert.That(device.ReadMemory(0), Is.EqualTo(0xAA));
        
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        Assert.That(device.ReadMemory(2), Is.EqualTo(0xCC));
    }

    [Test]
    public void Device_WithDifferentBase_MapsSameRegisters()
    {
        uint base1 = 0xD010;
        uint base2 = 0xE810;
        
        var device1 = Mos682xPiaDeviceFactory.CreateWithNullBindings(base1);
        var device2 = Mos682xPiaDeviceFactory.CreateWithNullBindings(base2);
        
        // Write to CRA at offset 1
        device1.WriteMemory(1, 0xA7); // CRA
        
        // Write to CRA at offset 1
        device2.WriteMemory(1, 0xA7); // CRA
        
        // Read back from both - CRA.7 is cleared by NullPiaPortBinding (HasInputReady = false)
        // 0xA7 & 0x7F = 0x27
        Assert.That(device1.ReadMemory(1), Is.EqualTo(0x27));
        Assert.That(device2.ReadMemory(1), Is.EqualTo(0x27));
    }

    [Test]
    public void ReadMemory_WithAddressOutsideRange_ThrowsArgumentOutOfRangeException()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => device.ReadMemory(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.ReadMemory(0xFF));
    }

    [Test]
    public void WriteMemory_WithAddressOutsideRange_ThrowsArgumentOutOfRangeException()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => device.WriteMemory(4, 0x00));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.WriteMemory(0xFF, 0x00));
    }

    // ==================== Apple-1 Preset Tests ====================

    [Test]
    public void Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Read KBD ($D010, ORA)
        byte value = device.ReadMemory(0);
        Assert.That(value, Is.EqualTo(0xC1)); // 'A' (0x41) | 0x80 = 0xC1
    }

    [Test]
    public void Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Read KBDCR ($D011, CRA) - bit 7 should be 1 (ready)
        byte kbdCr = device.ReadMemory(1);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x80));
    }

    [Test]
    public void Apple1Preset_ReadKbdCr_WhenNoInput_ReturnsNotReady()
    {
        var terminal = new BufferedTerminalLink();
        // Don't enqueue any input
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Read KBDCR ($D011, CRA) - bit 7 should be 0 (not ready)
        byte kbdCr = device.ReadMemory(1);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x00));
    }

    [Test]
    public void Apple1Preset_WriteDsp_StripsHighBitBeforeOutput()
    {
        var terminal = new BufferedTerminalLink();
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Write to DSP ($D012, ORB) with bit 7 set
        device.WriteMemory(2, 0xFF);
        
        // Terminal should receive only bits 0-6 (0x7F)
        byte[] output = terminal.ReadAllOutputBytes();
        Assert.That(output, Has.Length.EqualTo(1));
        Assert.That(output[0], Is.EqualTo(0x7F));
    }

    [Test]
    public void Apple1Preset_ReadDspCr_WhenReady_ReturnsZeroInBit7()
    {
        var terminal = new BufferedTerminalLink();
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Read DSPCR ($D013, CRB) - bit 7 should be 0 (ready, inverted logic)
        byte dspCr = device.ReadMemory(3);
        Assert.That(dspCr & 0x80, Is.EqualTo(0x00));
    }

    // ==================== WOZ Monitor Simulation Tests ====================

    [Test]
    public void Apple1Terminal_SimulateWozInputLoop_WaitsForKbdCrBit7()
    {
        var terminal = new BufferedTerminalLink();
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Initially: CRA.7 = 0 (not ready)
        byte kbdCr = device.ReadMemory(1);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x00));
        
        // Enqueue input
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);
        
        // Now: CRA.7 = 1 (ready)
        kbdCr = device.ReadMemory(1);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x80));
        
        // WOZ would read KBD
        byte kbd = device.ReadMemory(0);
        Assert.That(kbd, Is.EqualTo(0xC1));
    }

    [Test]
    public void Apple1Terminal_SimulateWozOutputLoop_WaitsForDspBit7()
    {
        var terminal = new BufferedTerminalLink();
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Initially: CRB.7 = 0 (ready, inverted logic)
        byte dspCr = device.ReadMemory(3);
        Assert.That(dspCr & 0x80, Is.EqualTo(0x00));
        
        // WOZ would write to DSP
        device.WriteMemory(2, 0x41); // 'A'
        
        // Terminal should have received the character
        byte[] output = terminal.ReadAllOutputBytes();
        Assert.That(output, Has.Length.EqualTo(1));
        Assert.That(output[0], Is.EqualTo(0x41));
        
        // CRB.7 should still be 0 (ready)
        dspCr = device.ReadMemory(3);
        Assert.That(dspCr & 0x80, Is.EqualTo(0x00));
    }

    // ==================== Factory Tests ====================

    [Test]
    public void Factory_CreatesMos6821FromProfile_WithCorrectBaseAddress()
    {
        var factory = new Mos682xPiaDeviceFactory();
        var profile = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004"),
            null,
            null);
        
        var systemBus = new RuntimeBus(16, 0);
        var device = factory.CreateDevice(profile, systemBus);
        
        Assert.That(device, Is.InstanceOf<Mos682xPiaDevice>());
        var piaDevice = (Mos682xPiaDevice)device;
        Assert.That(piaDevice.StartAddress, Is.EqualTo(0xD010));
    }

    [Test]
    public void CreateApple1Terminal_WithBaseAddressE810_WorksCorrectly()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("X", TerminalTextEncoding.RawBytes);
        var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xE810, terminal);
        
        // Test at different base address
        byte kbd = device.ReadMemory(0);
        Assert.That(kbd, Is.EqualTo(0xD8)); // 'X' (0x58) | 0x80 = 0xD8
        
        byte kbdCr = device.ReadMemory(1);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x00));
    }

    [Test]
    public void Mos682xPia_SameDeviceSupportsApple1AndPetLikeProfiles()
    {
        // Create device for Apple-1
        var apple1Device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, 
            new BufferedTerminalLink());
        
        // Create device for PET-like with different address
        var petDevice = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xE810);
        
        // Both should be the same type
        Assert.That(apple1Device.GetType(), Is.EqualTo(petDevice.GetType()));
    }

    // ==================== IRQ Tests ====================

    [Test]
    public void IrqSource_WhenEnabledFlagSet_AssertsIrq()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Initially no IRQ
        Assert.That(device.IsAsserted(CpuSignal.Irq), Is.False);
        
        // Set IRQA1 flag (CRA.7 = 1)
        device.WriteMemory(1, 0x80); // CRA = 0x80 (IRQA1 = 1)
        
        Assert.That(device.IsAsserted(CpuSignal.Irq), Is.True);
    }

    [Test]
    public void IrqSource_WithMultipleFlags_AssertsIrq()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Set IRQB1 flag (CRB.7 = 1)
        device.WriteMemory(3, 0x80); // CRB = 0x80 (IRQB1 = 1)
        
        Assert.That(device.IsAsserted(CpuSignal.Irq), Is.True);
        
        // Clear IRQB1, set IRQA2 (CRA.4 = 1)
        device.WriteMemory(3, 0x00);
        device.WriteMemory(1, 0x10); // CRA = 0x10 (IRQA2 = 1)
        
        Assert.That(device.IsAsserted(CpuSignal.Irq), Is.True);
    }

    [Test]
    public void IrqSource_WhenNoFlagsSet_DoesNotAssertIrq()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Set CRA and CRB to values without IRQ flags
        device.WriteMemory(1, 0x07); // CRA = 0x07 (no IRQ flags)
        device.WriteMemory(3, 0x07); // CRB = 0x07 (no IRQ flags)
        
        Assert.That(device.IsAsserted(CpuSignal.Irq), Is.False);
    }

    [Test]
    public void IrqSource_ForNonIrqSignals_ReturnsFalse()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        Assert.That(device.IsAsserted(CpuSignal.Reset), Is.False);
        Assert.That(device.IsAsserted(CpuSignal.Nmi), Is.False);
        Assert.That(device.IsAsserted(CpuSignal.Halt), Is.False);
    }

    // ==================== GetRegisterState Tests ====================

    [Test]
    public void GetRegisterState_ReturnsCorrectValues()
    {
        var device = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        device.WriteMemory(1, 0x00); // CRA.2 = 0
        device.WriteMemory(0, 0x12); // DDRA = 0x12
        device.WriteMemory(1, 0x04); // CRA.2 = 1
        device.WriteMemory(0, 0x34); // ORA = 0x34
        device.WriteMemory(1, 0x56); // CRA = 0x56
        
        device.WriteMemory(3, 0x00); // CRB.2 = 0
        device.WriteMemory(2, 0x78); // DDRB = 0x78
        device.WriteMemory(3, 0x04); // CRB.2 = 1
        device.WriteMemory(2, 0x9A); // ORB = 0x9A
        device.WriteMemory(3, 0xBC); // CRB = 0xBC
        
        var state = device.GetRegisterState();
        
        Assert.That(state.DDRA, Is.EqualTo(0x12));
        Assert.That(state.DDRB, Is.EqualTo(0x78));
        Assert.That(state.CRA, Is.EqualTo(0x56));
        Assert.That(state.CRB, Is.EqualTo(0xBC));
        Assert.That(state.ORA, Is.EqualTo(0x34));
        Assert.That(state.ORB, Is.EqualTo(0x9A));
    }
}
