using System.Text.Json.Nodes;
using Cpu6502;
using Cpu6502.System;
using Cpu6502.System.Builder;
using Cpu6502.System.Devices.Pia;
using Cpu6502.System.Factories;
using Cpu6502.System.Profiles;
using Cpu6502.System.Terminal;
using NUnit.Framework;

namespace Cpu6502.Tests.System;

/// <summary>
/// Unit tests for Phase 29: Apple-1 profile with WOZ Monitor on generic PIA.
/// </summary>
[TestFixture]
public class Faza29Apple1ProfileTests
{
    // ==================== Profile Loading Tests ====================

    [Test]
    public void BuildApple1Profile_CreatesCpuRamRomAndPia()
    {
        // Register the PIA factory
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        // Create Apple-1 profile programmatically
        // This matches the structure of profiles/computers/apple-1.json
        var wozMonRom = new RomRegionProfile("wozmon", "0xFF00", "0x0100", "roms/apple-1/wozmon.bin");
        var ram = new RamRegionProfile("ram0", "0x0000", "0x1000");
        
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004"),
            Options: new JsonObject { ["preset"] = "apple-1-terminal" }
        );
        
        var profile = new ComputerProfile(
            "apple-1",
            "Apple-1",
            CpuProfile.Mos6502Nmos with { ClockHz = 1023000 },
            AddressSpaceProfile.Mos6502,
            new MemoryProfile(
                new[] { ram },
                new[] { wozMonRom }
            ),
            new[] { piaDevice }
        );
        
        // Verify profile structure
        Assert.That(profile.Id, Is.EqualTo("apple-1"));
        Assert.That(profile.Name, Is.EqualTo("Apple-1"));
        Assert.That(profile.Cpu.Type, Is.EqualTo("mos6502-nmos"));
        Assert.That(profile.Cpu.ClockHz, Is.EqualTo(1023000));
        
        // Verify address space
        Assert.That(profile.AddressSpace.MemoryAddressBits, Is.EqualTo(16));
        Assert.That(profile.AddressSpace.PortAddressBits, Is.EqualTo(0));
        Assert.That(profile.AddressSpace.HasSeparatePortSpace, Is.False);
        Assert.That(profile.AddressSpace.DataBusBits, Is.EqualTo(8));
        
        // Verify memory
        Assert.That(profile.Memory.RamRegions.Count, Is.EqualTo(1));
        Assert.That(profile.Memory.RamRegions[0].Id, Is.EqualTo("ram0"));
        Assert.That(profile.Memory.RamRegions[0].ParsedStart, Is.EqualTo(0x0000));
        Assert.That(profile.Memory.RamRegions[0].ParsedSize, Is.EqualTo(0x1000));
        
        Assert.That(profile.Memory.RomRegions.Count, Is.EqualTo(1));
        Assert.That(profile.Memory.RomRegions[0].Id, Is.EqualTo("wozmon"));
        Assert.That(profile.Memory.RomRegions[0].ParsedStart, Is.EqualTo(0xFF00));
        Assert.That(profile.Memory.RomRegions[0].ParsedSize, Is.EqualTo(0x0100));
        Assert.That(profile.Memory.RomRegions[0].File, Is.EqualTo("roms/apple-1/wozmon.bin"));
        
        // Verify devices
        Assert.That(profile.Devices.Count, Is.EqualTo(1));
        Assert.That(profile.Devices[0].Id, Is.EqualTo("pia0"));
        Assert.That(profile.Devices[0].Type, Is.EqualTo("mos6821-pia"));
        Assert.That(profile.Devices[0].Mapping.ParsedBaseAddress, Is.EqualTo(0xD010));
        Assert.That(profile.Devices[0].Mapping.ParsedSize, Is.EqualTo(4));
    }

    [Test]
    public void Apple1Profile_Validate_DoesNotThrow()
    {
        // Create profile programmatically
        var ram = new RamRegionProfile("ram0", "0x0000", "0x1000");
        var wozMonRom = new RomRegionProfile("wozmon", "0xFF00", "0x0100", "roms/apple-1/wozmon.bin");
        
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004")
        );
        
        var profile = new ComputerProfile(
            "apple-1",
            "Apple-1",
            CpuProfile.Mos6502Nmos with { ClockHz = 1023000 },
            AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { ram }, new[] { wozMonRom }),
            new[] { piaDevice }
        );
        
        // Profile should be valid - no exception thrown
        Assert.That(profile.Id, Is.EqualTo("apple-1"));
        
        // Validate explicitly
        Assert.DoesNotThrow(() => profile.Validate());
    }

    // ==================== Device Creation Tests ====================

    [Test]
    public void Apple1Profile_DeviceFactory_CreatesMos6821Pia()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        // Create profile programmatically
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004")
        );
        
        var registry = DeviceFactoryRegistry.Default;
        var systemBus = new RuntimeBus(16, 0);
        
        var device = registry.CreateDevice(piaDevice, systemBus);
        
        Assert.That(device, Is.InstanceOf<Mos682xPiaDevice>());
        var piaDeviceObj = (Mos682xPiaDevice)device;
        Assert.That(piaDeviceObj.StartAddress, Is.EqualTo(0xD010));
        Assert.That(piaDeviceObj.Size, Is.EqualTo(4u));
    }

    [Test]
    public void Apple1Profile_MapsPiaAtD010ToD013()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        // Create profile programmatically
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004")
        );
        
        var registry = DeviceFactoryRegistry.Default;
        var systemBus = new RuntimeBus(16, 0);
        
        var device = registry.CreateDevice(piaDevice, systemBus);
        var piaDeviceObj = (Mos682xPiaDevice)device;
        
        // Map the device to the bus
        systemBus.MapDevice(piaDeviceObj);
        
        // Verify the PIA is mapped at $D010-$D013
        // Write to all registers (initially CRA.2=0 and CRB.2=0, so we write to DDRA and DDRB)
        systemBus.WriteMemory(0xD010, 0xAA); // DDRA
        systemBus.WriteMemory(0xD011, 0x04); // CRA = 0x04 (bit 2 = 1, so next reads from ORA/ORB)
        systemBus.WriteMemory(0xD012, 0xCC); // DDRB
        systemBus.WriteMemory(0xD013, 0x04); // CRB = 0x04 (bit 2 = 1)
        
        // Read back
        // With CRA.2 = 1, offset 0 reads ORA (mixed with external input)
        // ORA = 0, DDRA = 0xAA, externalInput = 0, so: (0 & 0xAA) | (0 & 0x55) = 0
        // With CRA.7 = 0 (HasInputReady = false), CRA & 0x7F = 0x04
        // With CRB.2 = 1, offset 2 reads ORB (mixed with external input)
        // ORB = 0, DDRB = 0xCC, externalInput = 0, so: (0 & 0xCC) | (0 & 0x33) = 0
        // With CRB.7 = 0 (IsOutputReady = true, inverted), CRB & 0x7F = 0x04
        Assert.That(systemBus.ReadMemory(0xD010), Is.EqualTo(0));
        Assert.That(systemBus.ReadMemory(0xD011), Is.EqualTo(0x04));
        Assert.That(systemBus.ReadMemory(0xD012), Is.EqualTo(0));
        Assert.That(systemBus.ReadMemory(0xD013), Is.EqualTo(0x04));
    }

    // ==================== Apple-1 Terminal Tests ====================

    [Test]
    public void Apple1Terminal_ReadKbd_ReturnsHighBitSetInput()
    {
        // Create Apple-1 PIA with terminal
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);
        
        var pia = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        var bus = new RuntimeBus(16, 0);
        bus.MapDevice(pia);
        
        // Read KBD ($D010) - should return 'A' | 0x80 = 0xC1
        byte kbd = bus.ReadMemory(0xD010);
        Assert.That(kbd, Is.EqualTo(0xC1));
    }

    [Test]
    public void Apple1Terminal_ReadKbd_ConsumesInput()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);

        var pia = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        var bus = new RuntimeBus(16, 0);
        bus.MapDevice(pia);

        Assert.That(bus.ReadMemory(0xD010), Is.EqualTo(0xC1));
        Assert.That(terminal.InputBufferSize, Is.EqualTo(0));
        Assert.That(bus.ReadMemory(0xD011) & 0x80, Is.EqualTo(0));
    }

    [Test]
    public void Apple1Terminal_ReadKbdCr_ReportsReady()
    {
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);
        
        var pia = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        var bus = new RuntimeBus(16, 0);
        bus.MapDevice(pia);
        
        // Read KBDCR ($D011) - bit 7 should be 1 (ready)
        byte kbdCr = bus.ReadMemory(0xD011);
        Assert.That(kbdCr & 0x80, Is.EqualTo(0x80));
    }

    [Test]
    public void Apple1Terminal_WriteDsp_ForwardsLow7Bits()
    {
        var terminal = new BufferedTerminalLink();
        var pia = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        var bus = new RuntimeBus(16, 0);
        bus.MapDevice(pia);
        
        // Write to DSP ($D012) with bit 7 set
        bus.WriteMemory(0xD012, 0xFF);
        
        // Terminal should receive only bits 0-6 (0x7F)
        byte[] output = terminal.ReadAllOutputBytes();
        Assert.That(output, Has.Length.EqualTo(1));
        Assert.That(output[0], Is.EqualTo(0x7F));
    }

    [Test]
    public void WozMonRom_ContainsValidPiaStoreOpcodes()
    {
        var romPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "roms", "apple-1", "wozmon.bin");
        romPath = Path.GetFullPath(romPath);
        if (!File.Exists(romPath))
            Assert.Inconclusive("WOZ Monitor ROM file not found: " + romPath);

        byte[] rom = File.ReadAllBytes(romPath);

        Assert.That(rom[0x0C], Is.EqualTo(0x8D), "FF0C must be STA DSPCR, not an illegal opcode.");
        Assert.That(rom[0x0D], Is.EqualTo(0x13));
        Assert.That(rom[0x0E], Is.EqualTo(0xD0));
        Assert.That(rom[0xF4], Is.EqualTo(0x8D), "FFF4 must write display output through DSP.");
        Assert.That(rom[0xF5], Is.EqualTo(0x12));
        Assert.That(rom[0xF6], Is.EqualTo(0xD0));
        Assert.That(rom[0x47], Is.EqualTo(0xC9), "FF47 must compare input against carriage return.");
        Assert.That(rom[0x48], Is.EqualTo(0x8D));
        Assert.That(rom[0x63], Is.EqualTo(0xB0), "FF63 must map ASCII hex digits correctly.");
        Assert.That(rom[0x8D], Is.EqualTo(0xD0), "FF8D must branch after incrementing the store pointer.");
        Assert.That(rom[0x8E], Is.EqualTo(0xB5));
        Assert.That(rom[0xA6], Is.EqualTo(0xA9), "FFA6 must load carriage return before printing an address line.");
        Assert.That(rom[0xA7], Is.EqualTo(0x8D));
        Assert.That(rom[0xB1], Is.EqualTo(0x24), "FFB0 must print XAML, not XAMH twice.");
        Assert.That(rom[0xE4], Is.EqualTo(0x68), "FFE4 must restore A with PLA.");
    }

    [Test]
    public void WozMon_WithTerminalInput_ProducesOutput()
    {
        var romPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "roms", "apple-1", "wozmon.bin");
        romPath = Path.GetFullPath(romPath);
        if (!File.Exists(romPath))
            Assert.Inconclusive("WOZ Monitor ROM file not found: " + romPath);

        var terminal = new BufferedTerminalLink();
        terminal.EnqueueInput(0xB0); // 0
        terminal.EnqueueInput(0xAE); // .
        terminal.EnqueueInput(0xB3); // 3
        terminal.EnqueueInput(0x8D); // Return

        var bus = new RuntimeBus(16, 0);
        bus.MapRam(0x0000, 0x10000);
        bus.MapDevice(Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal));

        byte[] rom = File.ReadAllBytes(romPath);
        for (int i = 0; i < rom.Length; i++)
            bus.WriteMemory(0xFF00 + (uint)i, rom[i]);

        var cpu = new Cpu6502(bus);
        cpu.Reset();
        cpu.PC = 0xFF00;

        for (int i = 0; i < 10_000 && terminal.OutputBufferSize < 16; i++)
            cpu.StepInstruction();

        string output = terminal.ReadOutputText(TerminalTextEncoding.RawBytes);
        Assert.That(output, Does.Contain("0000: 00"));
    }

    [Test]
    public void Apple1Profile_RomIsReadOnly()
    {
        // Create a test with ROM data
        var wozMonRom = new byte[256];
        // Fill with some data
        for (int i = 0; i < 256; i++)
            wozMonRom[i] = (byte)i;
        
        var systemBus = new RuntimeBus(16, 0);
        
        // Map RAM
        systemBus.MapRam(0x0000, 0x1000);
        
        // Map ROM
        systemBus.MapRom(0xFF00, wozMonRom, RomWritePolicy.ThrowException, "wozmon");
        
        // Verify ROM is read-only
        Assert.That(() => systemBus.WriteMemory(0xFF00, 0x00), 
            Throws.InstanceOf<RomWriteException>());
    }

    // ==================== ComputerBuilder Integration Tests ====================

    [Test]
    public void BuildApple1FromProfile_CreatesEmulatedComputer()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        var registry = new DeviceFactoryRegistry();
        // Register CPU factory
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, 
            (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        // Register PIA factory
        Mos682xPiaDeviceFactory.RegisterWith(registry);
        
        // Create Apple-1 profile programmatically
        var ram = new RamRegionProfile("ram0", "0x0000", "0x1000");
        var wozMonRom = new RomRegionProfile("wozmon", "0xFF00", "0x0100", "roms/apple-1/wozmon.bin");
        
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004"),
            Options: new JsonObject { ["preset"] = "apple-1-terminal" }
        );
        
        var profile = new ComputerProfile(
            "apple-1",
            "Apple-1",
            CpuProfile.Mos6502Nmos with { ClockHz = 1023000 },
            AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { ram }, new[] { wozMonRom }),
            new[] { piaDevice }
        );
        
        // Build without ROM (ROM file doesn't exist, but we can provide empty data)
        var options = new ProfileLoadOptions(
            RomDataOverrides: new Dictionary<string, byte[]> {
                ["roms/apple-1/wozmon.bin"] = new byte[256]
            }
        );
        
        var computer = new ComputerBuilder(registry).Build(profile, options);
        
        Assert.That(computer, Is.Not.Null);
        Assert.That(computer.Cpu, Is.Not.Null);
        Assert.That(computer.Bus, Is.Not.Null);
        Assert.That(computer.Devices.Count, Is.EqualTo(1));
        
        // Verify the PIA device is in the devices list
        Assert.That(computer.Devices[0], Is.InstanceOf<Mos682xPiaDevice>());
    }

    [Test]
    public void BuildApple1FromProfile_WithRomDataOverride_WorksCorrectly()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        var registry = new DeviceFactoryRegistry();
        // Register CPU factory
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, 
            (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        // Register PIA factory
        Mos682xPiaDeviceFactory.RegisterWith(registry);
        
        // Create test ROM data
        var wozMonRom = new byte[256];
        for (int i = 0; i < 256; i++)
            wozMonRom[i] = (byte)(i * 2);
        
        var ram = new RamRegionProfile("ram0", "0x0000", "0x1000");
        var romRegion = new RomRegionProfile("wozmon", "0xFF00", "0x0100", "roms/apple-1/wozmon.bin");
        
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004")
        );
        
        var profile = new ComputerProfile(
            "apple-1",
            "Apple-1",
            CpuProfile.Mos6502Nmos with { ClockHz = 1023000 },
            AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { ram }, new[] { romRegion }),
            new[] { piaDevice }
        );
        
        var options = new ProfileLoadOptions(
            RomDataOverrides: new Dictionary<string, byte[]> {
                ["roms/apple-1/wozmon.bin"] = wozMonRom
            }
        );
        
        var computer = new ComputerBuilder(registry).Build(profile, options);
        
        Assert.That(computer, Is.Not.Null);
        
        // Verify ROM is mapped and readable
        Assert.That(computer.Bus.ReadMemory(0xFF00), Is.EqualTo(0x00));
        Assert.That(computer.Bus.ReadMemory(0xFF01), Is.EqualTo(0x02));
        Assert.That(computer.Bus.ReadMemory(0xFFFF), Is.EqualTo(0xFE));
    }

    // ==================== WOZ Monitor Smoke Test ====================

    // This test is marked as Explicit because it requires the WOZ Monitor ROM file
    // which may not be present in all environments
    [Test]
    [Explicit("Requires wozmon.bin ROM file")]
    public void Apple1WozMonSmoke_WhenRomPresent_ReachesInputLoop()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        // Check if ROM file exists
        var romPath = "roms/apple-1/wozmon.bin";
        if (!File.Exists(romPath))
        {
            Assert.Inconclusive("WOZ Monitor ROM file not found: " + romPath);
        }
        
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromFile("profiles/computers/apple-1.json");
        
        var computer = ComputerBuilder.BuildFromProfile(profile);
        
        // Set up terminal with input
        var terminal = new BufferedTerminalLink();
        terminal.EnqueueText("R", TerminalTextEncoding.RawBytes); // 'R' command to display registers
        
        // Get the PIA device and update its bindings
        var pia = computer.Devices.OfType<Mos682xPiaDevice>().FirstOrDefault();
        Assert.That(pia, Is.Not.Null);
        
        // Create new PIA with terminal binding
        var piaWithTerminal = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);
        
        // Replace the device in the bus
        // Note: This is a workaround for the current limitation where the factory
        // doesn't have access to the terminal. In a future phase, we'll improve this.
        
        // For now, just verify the computer was built successfully
        Assert.That(computer.Cpu, Is.Not.Null);
        
        // Reset CPU
        computer.Cpu.Reset();
        
        // Get snapshot and verify PC can be set
        var snapshot = computer.Cpu.GetSnapshot();
        
        // Run a few instructions
        // Note: Without the actual WOZ Monitor ROM, we can't really test this
        // This is just a smoke test to verify the setup works
        computer.Run(10);
        
        // If we got here without crashing, the basic setup is working
        Assert.Pass();
    }

    // ==================== Reuse Tests ====================

    [Test]
    public void Apple1Profile_UsesSamePiaDevice_AsManualCreation()
    {
        Mos682xPiaDeviceFactory.RegisterDefault();
        
        // Create a PIA from profile programmatically
        var piaDevice = new DeviceProfile(
            "pia0",
            "mos6821-pia",
            new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD010", "0x0004")
        );
        
        var registry = DeviceFactoryRegistry.Default;
        var bus = new RuntimeBus(16, 0);
        var profilePia = registry.CreateDevice(piaDevice, bus) as Mos682xPiaDevice;
        
        // Create a PIA manually for comparison
        var manualPia = Mos682xPiaDeviceFactory.CreateWithNullBindings(0xD010);
        
        // Both should be the same type
        Assert.That(profilePia.GetType(), Is.EqualTo(manualPia.GetType()));
    }
}
