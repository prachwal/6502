using NUnit.Framework;
using System.Collections.Immutable;

using Cpu6502.System;
using Cpu6502.System.Builder;
using Cpu6502.System.Factories;
using Cpu6502.System.Profiles;

namespace Cpu6502.Tests.System;

/// <summary>
/// Testy jednostkowe dla Fazy 26 - Profile komputerow, fabryki i builder.
/// </summary>
[TestFixture]
public class Faza26ComputerProfilesTests
{
    // ==================== AddressParser Tests ====================

    [Test]
    public void AddressParser_ParsesHexAddresses()
    {
        Assert.That(AddressParser.Parse("0xD010"), Is.EqualTo(0xD010));
        Assert.That(AddressParser.Parse("0XFF00"), Is.EqualTo(0xFF00));
        Assert.That(AddressParser.Parse("0xd010"), Is.EqualTo(0xD010));
    }

    [Test]
    public void AddressParser_ParsesDecimalAddresses()
    {
        Assert.That(AddressParser.Parse("53248"), Is.EqualTo(53248));
        Assert.That(AddressParser.Parse("65536"), Is.EqualTo(65536));
    }

    [Test]
    public void AddressParser_ThrowsOnEmptyString()
    {
        Assert.Throws<ArgumentNullException>(() => AddressParser.Parse(""));
        Assert.Throws<ArgumentNullException>(() => AddressParser.Parse(null!));
    }

    [Test]
    public void AddressParser_ThrowsOnInvalidFormat()
    {
        Assert.Throws<FormatException>(() => AddressParser.Parse("abc"));
        Assert.Throws<FormatException>(() => AddressParser.Parse("0x"));
        Assert.Throws<FormatException>(() => AddressParser.Parse("0xZZZZ"));
    }

    // ==================== Profile Model Tests ====================

    [Test]
    public void AddressSpaceProfile_ValidatesMemoryBits()
    {
        var profile = new AddressSpaceProfile(16, 0, false, 8);
        Assert.DoesNotThrow(() => profile.Validate("test"));
    }

    [Test]
    public void AddressSpaceProfile_ThrowsOnInvalidMemoryBits()
    {
        var profile = new AddressSpaceProfile(0, 0, false, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.Validate("test"));
    }

    [Test]
    public void AddressSpaceProfile_ThrowsOnPortBitsWithoutSeparateSpace()
    {
        var profile = new AddressSpaceProfile(16, 8, false, 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.Validate("test"));
    }

    [Test]
    public void CpuProfile_ValidatesTypeAndClock()
    {
        var profile = new CpuProfile("mos6502-nmos", 1023000);
        Assert.DoesNotThrow(() => profile.Validate("test"));
    }

    [Test]
    public void CpuProfile_ThrowsOnEmptyType()
    {
        var profile = new CpuProfile("", 1023000);
        Assert.Throws<ArgumentNullException>(() => profile.Validate("test"));
    }

    [Test]
    public void CpuProfile_ThrowsOnZeroClock()
    {
        var profile = new CpuProfile("mos6502-nmos", 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.Validate("test"));
    }

    [Test]
    public void MemoryRegionProfile_ParsesStartAndSize()
    {
        var region = new RamRegionProfile("ram0", "0x0000", "0x1000");
        Assert.That(region.ParsedStart, Is.EqualTo(0x0000));
        Assert.That(region.ParsedSize, Is.EqualTo(0x1000));
        Assert.That(region.EndAddress, Is.EqualTo(0x1000));
    }

    [Test]
    public void MemoryProfile_DetectsOverlappingRegions()
    {
        var ram1 = new RamRegionProfile("ram0", "0x0000", "0x1000");
        var ram2 = new RamRegionProfile("ram1", "0x0800", "0x1000");
        var memory = new MemoryProfile(new[] { ram1, ram2 });
        Assert.Throws<ArgumentException>(() => memory.Validate("test"));
    }

    // ==================== ComputerProfileLoader Tests ====================

    [Test]
    public void LoadProfile_ParsesMinimalProfile()
    {
        var json = GetMinimalProfileJson();
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromString(json);
        Assert.That(profile.Id, Is.EqualTo("test-computer"));
        Assert.That(profile.Name, Is.EqualTo("Test Computer"));
        Assert.That(profile.Cpu.Type, Is.EqualTo("mos6502-nmos"));
        Assert.That(profile.Cpu.ClockHz, Is.EqualTo(1023000));
        Assert.That(profile.Memory.RamRegions.Length, Is.EqualTo(1));
    }

    [Test]
    public void LoadProfile_ParsesHexAddresses()
    {
        var json = GetEmptyProfileJson();
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromString(json);
        Assert.That(profile.Memory.RamRegions.Length, Is.EqualTo(0));
    }

    [Test]
    public void LoadProfile_WithDevices_ParsesDeviceMapping()
    {
        var json = GetProfileWithDeviceJson();
        var loader = new ComputerProfileLoader();
        var profile = loader.LoadFromString(json);
        Assert.That(profile.Devices.Length, Is.EqualTo(1));
        Assert.That(profile.Devices[0].Id, Is.EqualTo("pia0"));
        Assert.That(profile.Devices[0].Type, Is.EqualTo("mos6821-pia"));
        Assert.That(profile.Devices[0].Mapping.Kind, Is.EqualTo(AddressSpaceKind.Memory));
        Assert.That(profile.Devices[0].Mapping.ParsedBaseAddress, Is.EqualTo(0xD010));
    }

    private static string GetMinimalProfileJson() =>
        "{\"schema\":\"computer-profile/v1\",\"id\":\"test-computer\",\"name\":\"Test Computer\",\"status\":\"planned\","
        + "\"cpu\":{\"type\":\"mos6502-nmos\",\"clockHz\":1023000},"
        + "\"addressSpace\":{\"memoryAddressBits\":16,\"portAddressBits\":0,\"hasSeparatePortSpace\":false,\"dataBusBits\":8},"
        + "\"memory\":{\"ram\":[{\"id\":\"ram0\",\"start\":\"0x0000\",\"size\":\"0x1000\"}],\"rom\":[]},"
        + "\"devices\":[]}";

    private static string GetEmptyProfileJson() =>
        "{\"schema\":\"computer-profile/v1\",\"id\":\"test\",\"name\":\"Test\",\"status\":\"planned\","
        + "\"cpu\":{\"type\":\"test-cpu\",\"clockHz\":1000000},"
        + "\"addressSpace\":{\"memoryAddressBits\":16,\"portAddressBits\":0,\"hasSeparatePortSpace\":false,\"dataBusBits\":8},"
        + "\"memory\":{\"ram\":[],\"rom\":[]},\"devices\":[]}";

    private static string GetProfileWithDeviceJson() =>
        "{\"schema\":\"computer-profile/v1\",\"id\":\"test\",\"name\":\"Test\",\"status\":\"planned\","
        + "\"cpu\":{\"type\":\"test-cpu\",\"clockHz\":1000000},"
        + "\"addressSpace\":{\"memoryAddressBits\":16,\"portAddressBits\":0,\"hasSeparatePortSpace\":false,\"dataBusBits\":8},"
        + "\"memory\":{\"ram\":[],\"rom\":[]},"
        + "\"devices\":[{\"id\":\"pia0\",\"type\":\"mos6821-pia\",\"mapping\":{\"kind\":\"memory\",\"baseAddress\":\"0xD010\",\"size\":\"0x0004\"}}]}";

    // ==================== ComputerBuilder Tests ====================

    [Test]
    public void Build_Minimal6502Profile_CreatesComputer()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));

        var profile = new ComputerProfile("minimal-6502-sbc", "Minimal 6502 SBC", CpuProfile.Mos6502Nmos, AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { new RamRegionProfile("ram0", "0x0000", "0x8000") }, ImmutableArray<RomRegionProfile>.Empty),
            ImmutableArray<DeviceProfile>.Empty);

        var builder = new ComputerBuilder(registry);
        var computer = builder.Build(profile);

        Assert.That(computer.Id, Is.EqualTo("minimal-6502-sbc"));
        Assert.That(computer.Name, Is.EqualTo("Minimal 6502 SBC"));
        Assert.That(computer.Devices.Count, Is.EqualTo(0));
    }

    [Test]
    public void Build_UnknownCpuType_ThrowsValidationError()
    {
        var registry = new DeviceFactoryRegistry();
        var profile = new ComputerProfile("test", "Test", new CpuProfile("unknown-cpu", 1000000), AddressSpaceProfile.Mos6502, new MemoryProfile(), ImmutableArray<DeviceProfile>.Empty);
        var builder = new ComputerBuilder(registry);
        Assert.Throws<ComputerBuildException>(() => builder.Build(profile));
    }

    [Test]
    public void Build_UnknownDeviceType_ThrowsValidationError()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var device = new DeviceProfile("unknown-device", "unknown-type", new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD000", "0x10"));
        var profile = new ComputerProfile("test", "Test", CpuProfile.Mos6502Nmos, AddressSpaceProfile.Mos6502, new MemoryProfile(), ImmutableArray.Create(device));
        var builder = new ComputerBuilder(registry);
        Assert.Throws<ComputerBuildException>(() => builder.Build(profile));
    }

    [Test]
    public void Build_OverlappingMemoryRanges_ThrowsValidationError()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var profile = new ComputerProfile("test", "Test", CpuProfile.Mos6502Nmos, AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { new RamRegionProfile("ram0", "0x0000", "0x1000"), new RamRegionProfile("ram1", "0x0800", "0x1000") }, ImmutableArray<RomRegionProfile>.Empty),
            ImmutableArray<DeviceProfile>.Empty);
        var builder = new ComputerBuilder(registry);
        Assert.Throws<ComputerBuildException>(() => builder.Build(profile));
    }

    // ==================== Factory Registry Tests ====================

    [Test]
    public void Registry_CreatesCpuWithRegisteredFactory()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var cpuProfile = new CpuProfile("mos6502-nmos", 1023000);
        var cpu = registry.CreateCpu(cpuProfile, AddressSpaceDescriptor.Mos6502);
        Assert.That(cpu.CpuType, Is.EqualTo("mos6502-nmos"));
    }

    [Test]
    public void Registry_ThrowsWhenCpuFactoryNotRegistered()
    {
        var registry = new DeviceFactoryRegistry();
        var cpuProfile = new CpuProfile("unknown-cpu", 1000000);
        Assert.Throws<KeyNotFoundException>(() => registry.CreateCpu(cpuProfile, AddressSpaceDescriptor.Mos6502));
    }

    // ==================== EmulatedComputer Tests ====================

    [Test]
    public void EmulatedComputer_ReadWriteMemory()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var profile = new ComputerProfile("test", "Test", CpuProfile.Mos6502Nmos, AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { new RamRegionProfile("ram0", "0x0000", "0x100") }, ImmutableArray<RomRegionProfile>.Empty),
            ImmutableArray<DeviceProfile>.Empty);
        var builder = new ComputerBuilder(registry);
        var computer = builder.Build(profile);
        computer.WriteMemory(0x0000, 0x42);
        computer.WriteMemory(0x0001, 0x99);
        Assert.That(computer.ReadMemory(0x0000), Is.EqualTo(0x42));
        Assert.That(computer.ReadMemory(0x0001), Is.EqualTo(0x99));
    }

    [Test]
    public void EmulatedComputer_StepInstruction_Works()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var profile = new ComputerProfile("test", "Test", CpuProfile.Mos6502Nmos, AddressSpaceProfile.Mos6502,
            new MemoryProfile(new[] { new RamRegionProfile("ram0", "0x0000", "0x10000") }, ImmutableArray<RomRegionProfile>.Empty),
            ImmutableArray<DeviceProfile>.Empty);
        var builder = new ComputerBuilder(registry);
        var computer = builder.Build(profile);
        computer.WriteMemory(0x0000, 0x00);
        var snapshot1 = computer.GetCpuSnapshot();
        computer.StepInstruction();
        var snapshot2 = computer.GetCpuSnapshot();
        Assert.That(snapshot2.ProgramCounter, Is.GreaterThanOrEqualTo(snapshot1.ProgramCounter));
    }

    // ==================== FakeDeviceFactory Tests ====================

    [Test]
    public void FakeDeviceFactory_CreatesMemoryMappedDevice()
    {
        var factory = new FakeDeviceFactory("test-device", false);
        var profile = new DeviceProfile("test0", "test-device", new DeviceMappingProfile(AddressSpaceKind.Memory, "0xD000", "0x10"));
        var device = factory.CreateDevice(profile, new RuntimeBus(), null);
        Assert.That(device.Id, Is.EqualTo("test0"));
        Assert.That(factory.MemoryDevice, Is.Not.Null);
    }

    [Test]
    public void FakeDeviceFactory_CreatesPortMappedDevice()
    {
        var factory = new FakeDeviceFactory("test-device", true);
        var profile = new DeviceProfile("test0", "test-device", new DeviceMappingProfile(AddressSpaceKind.Port, "0x00", "0x10"));
        var device = factory.CreateDevice(profile, new RuntimeBus(16, 8), null);
        Assert.That(device.Id, Is.EqualTo("test0"));
        Assert.That(factory.PortDevice, Is.Not.Null);
    }

    // ==================== Z80-style Profile Tests ====================

    [Test]
    public void Build_Z80StylePortDevice_WithRegisteredFactory()
    {
        var registry = new DeviceFactoryRegistry();
        registry.RegisterCpuFactory(Mos6502CpuFactory.CpuTypeMos6502Nmos, (p, a, b) => new Cpu6502CoreAdapter(new Cpu6502(b ?? new RuntimeBus()), "mos6502-nmos"));
        var fakeFactory = new FakeDeviceFactory("test-port-device", true);
        registry.RegisterDeviceFactory(fakeFactory);
        var addressSpace = new AddressSpaceProfile(16, 8, true, 8);
        var device = new DeviceProfile("port0", "test-port-device", new DeviceMappingProfile(AddressSpaceKind.Port, "0x00", "0x10"));
        var profile = new ComputerProfile("test-z80", "Test Z80", CpuProfile.Mos6502Nmos, addressSpace, new MemoryProfile(), ImmutableArray.Create(device));
        var builder = new ComputerBuilder(registry);
        var computer = builder.Build(profile);
        Assert.That(computer.Devices.Count, Is.EqualTo(1));
        Assert.That(fakeFactory.PortDevice, Is.Not.Null);
    }

    [Test]
    public void Build_PortDeviceOnCpuWithoutPorts_ThrowsValidationError()
    {
        var json = "{\"schema\":\"computer-profile/v1\",\"id\":\"test\",\"name\":\"Test\",\"status\":\"planned\",\"cpu\":{\"type\":\"test-cpu\",\"clockHz\":1000000},\"addressSpace\":{\"memoryAddressBits\":16,\"portAddressBits\":0,\"hasSeparatePortSpace\":false,\"dataBusBits\":8},\"memory\":{\"ram\":[],\"rom\":[]},\"devices\":[{\"id\":\"port0\",\"type\":\"test-device\",\"mapping\":{\"kind\":\"port\",\"baseAddress\":\"0x00\",\"size\":\"0x10\"}}]}";
        var loader = new ComputerProfileLoader();
        Assert.Throws<ComputerProfileValidationException>(() => loader.LoadFromString(json));
    }
}
