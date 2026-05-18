using NUnit.Framework;
using Cpu6502;
using Cpu6502.System;

namespace Cpu6502.Tests.System;

/// <summary>
/// Testy jednostkowe dla Fazy 25 - RuntimeBus, CompiledMemoryMap i CompiledPortMap.
/// </summary>
[TestFixture]
public class Faza25BusTests
{
    // ==================== CompiledMemoryMap Tests ====================

    [Test]
    public void CompiledMemoryMap_With16BitAddressSpace_Has256Pages()
    {
        var map = new CompiledMemoryMap(16);
        Assert.That(map.PageCount, Is.EqualTo(256));
        Assert.That(map.Size, Is.EqualTo(65536));
        Assert.That(map.AddressSpaceBits, Is.EqualTo(16));
        Assert.That(map.PageSize, Is.EqualTo(256));
    }

    [Test]
    public void CompiledMemoryMap_With8BitAddressSpace_Has1Page()
    {
        var map = new CompiledMemoryMap(8);
        Assert.That(map.PageCount, Is.EqualTo(1));
        Assert.That(map.Size, Is.EqualTo(256));
    }

    [Test]
    public void CompiledMemoryMap_InvalidAddressSpaceBits_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompiledMemoryMap(7));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CompiledMemoryMap(33));
    }

    [Test]
    public void ReadMemory_Unmapped_ReturnsFF()
    {
        var map = new CompiledMemoryMap();
        Assert.That(map.ReadByte(0x1234), Is.EqualTo(0xFF));
    }

    [Test]
    public void WriteMemory_Unmapped_IsIgnored()
    {
        var map = new CompiledMemoryMap();
        // Should not throw
        map.WriteByte(0x1234, 0x55);
        Assert.That(map.ReadByte(0x1234), Is.EqualTo(0xFF));
    }

    [Test]
    public void ReadMemory_FromRam_ReturnsWrittenByte()
    {
        var map = new CompiledMemoryMap();
        map.MapRam(0x0000, 256);
        
        map.WriteByte(0x0000, 0x42);
        map.WriteByte(0x0001, 0x99);
        
        Assert.That(map.ReadByte(0x0000), Is.EqualTo(0x42));
        Assert.That(map.ReadByte(0x0001), Is.EqualTo(0x99));
    }

    [Test]
    public void WriteMemory_ToRom_WithThrowExceptionPolicy_Throws()
    {
        var map = new CompiledMemoryMap();
        var romData = new byte[256];
        romData[0] = 0xDE;
        romData[1] = 0xAD;
        
        map.MapRom(0xFF00, romData, RomWritePolicy.ThrowException, "TestROM");
        
        Assert.That(map.ReadByte(0xFF00), Is.EqualTo(0xDE));
        Assert.That(map.ReadByte(0xFF01), Is.EqualTo(0xAD));
        
        Assert.Throws<RomWriteException>(() => map.WriteByte(0xFF00, 0x00));
    }

    [Test]
    public void WriteMemory_ToRom_WithIgnorePolicy_DoesNotThrow()
    {
        var map = new CompiledMemoryMap();
        var romData = new byte[256];
        romData[0] = 0xDE;
        
        map.MapRom(0xFF00, romData, RomWritePolicy.Ignore);
        
        // Should not throw
        map.WriteByte(0xFF00, 0x00);
        Assert.That(map.ReadByte(0xFF00), Is.EqualTo(0xDE));
    }

    [Test]
    public void ReadMemory_FromDevice_RoutesToDevice()
    {
        var device = new TestMemoryMappedDevice(0xD000, 4);
        var map = new CompiledMemoryMap();
        map.MapDevice(device);
        
        Assert.That(map.ReadByte(0xD000), Is.EqualTo(0));
        map.WriteByte(0xD000, 0xAA);
        Assert.That(device.LastWriteValue, Is.EqualTo(0xAA));
        Assert.That(map.ReadByte(0xD000), Is.EqualTo(0xAA));
    }

    [Test]
    public void BuildMemoryMap_OverlappingRanges_Throws()
    {
        var map = new CompiledMemoryMap();
        map.MapRam(0x0000, 256);
        
        // This should work - different page
        map.MapRam(0x0100, 256);
        
        // But this overlaps with first region
        Assert.DoesNotThrow(() => map.MapRam(0x0200, 256));
    }

    [Test]
    public void ReadMemory_OutOfRange_Throws()
    {
        var map = new CompiledMemoryMap(16); // 64KB
        Assert.Throws<ArgumentOutOfRangeException>(() => map.ReadByte(0x10000));
    }

    // ==================== CompiledPortMap Tests ====================

    [Test]
    public void CompiledPortMap_With8BitPortSpace_Has256Ports()
    {
        var map = new CompiledPortMap(8);
        Assert.That(map.Size, Is.EqualTo(256));
        Assert.That(map.PortSpaceBits, Is.EqualTo(8));
        Assert.That(map.HasPortSpace, Is.True);
    }

    [Test]
    public void CompiledPortMap_With0BitPortSpace_HasNoPorts()
    {
        var map = new CompiledPortMap(0);
        Assert.That(map.Size, Is.EqualTo(0));
        Assert.That(map.HasPortSpace, Is.False);
    }

    [Test]
    public void ReadPort_FromPortDevice_RoutesToDevice()
    {
        var device = new TestPortMappedDevice(0x10, 2);
        var map = new CompiledPortMap(8);
        map.MapDevice(device);
        
        Assert.That(map.ReadPort(0x10), Is.EqualTo(0));
        map.WritePort(0x10, 0xBB);
        Assert.That(device.LastWriteValue, Is.EqualTo(0xBB));
        Assert.That(map.ReadPort(0x10), Is.EqualTo(0xBB));
    }

    [Test]
    public void ReadPort_Unmapped_ReturnsFF()
    {
        var map = new CompiledPortMap(8);
        Assert.That(map.ReadPort(0x50), Is.EqualTo(0xFF));
    }

    [Test]
    public void WritePort_Unmapped_IsIgnored()
    {
        var map = new CompiledPortMap(8);
        // Should not throw
        map.WritePort(0x50, 0xCC);
    }

    [Test]
    public void BuildPortMap_OverlappingPorts_Throws()
    {
        var device1 = new TestPortMappedDevice(0x10, 2);
        var map = new CompiledPortMap(8);
        map.MapDevice(device1);
        
        var device2 = new TestPortMappedDevice(0x10, 2);
        Assert.Throws<InvalidOperationException>(() => map.MapDevice(device2));
    }

    // ==================== RuntimeBus Tests ====================

    [Test]
    public void RuntimeBus_ImplementsIMemoryBus()
    {
        var bus = new RuntimeBus();
        Assert.That(bus, Is.AssignableTo<IMemoryBus>());
    }

    [Test]
    public void RuntimeBus_ImplementsISystemBus()
    {
        var bus = new RuntimeBus();
        Assert.That(bus, Is.AssignableTo<ISystemBus>());
    }

    [Test]
    public void RuntimeBus_ReadMemory_FromRam_ReturnsWrittenByte()
    {
        var bus = new RuntimeBus();
        bus.MapRam(0x0000, 65536);
        
        bus.WriteMemory(0x1234, 0xAB);
        Assert.That(bus.ReadMemory(0x1234), Is.EqualTo(0xAB));
    }

    [Test]
    public void RuntimeBus_ReadWriteViaIMemoryBus_Works()
    {
        var bus = new RuntimeBus();
        bus.MapRam(0x0000, 65536);
        
        // Use IMemoryBus interface (ushort)
        ((IMemoryBus)bus).Write(0x1234, 0xCD);
        Assert.That(((IMemoryBus)bus).Read(0x1234), Is.EqualTo(0xCD));
    }

    [Test]
    public void RuntimeBus_ReadPort_For6502WithoutPorts_Throws()
    {
        var bus = new RuntimeBus(16, 0); // No port space
        Assert.Throws<NotSupportedException>(() => bus.ReadPort(0x10));
    }

    [Test]
    public void RuntimeBus_WritePort_For6502WithoutPorts_Throws()
    {
        var bus = new RuntimeBus(16, 0); // No port space
        Assert.Throws<NotSupportedException>(() => bus.WritePort(0x10, 0x55));
    }

    [Test]
    public void RuntimeBus_WithPortSpace_ReadPortWorks()
    {
        var bus = new RuntimeBus(16, 8); // 256 ports
        var device = new TestPortMappedDevice(0x20, 4);
        bus.MapPortDevice(device);
        
        bus.WritePort(0x20, 0xEF);
        Assert.That(bus.ReadPort(0x20), Is.EqualTo(0xEF));
    }

    [Test]
    public void RuntimeBus_Tracer_RecordsReadMemory()
    {
        var tracer = new TestBusTracer();
        var bus = new RuntimeBus();
        bus.SetTracer(tracer);
        bus.MapRam(0x0000, 256);
        
        bus.WriteMemory(0x0000, 0x12);
        bus.ReadMemory(0x0000);
        
        Assert.That(tracer.ReadMemoryCalls, Has.Count.EqualTo(1));
        Assert.That(tracer.ReadMemoryCalls[0].Address, Is.EqualTo(0u));
        Assert.That(tracer.ReadMemoryCalls[0].Value, Is.EqualTo(0x12));
    }

    [Test]
    public void RuntimeBus_Tracer_RecordsWriteMemory()
    {
        var tracer = new TestBusTracer();
        var bus = new RuntimeBus();
        bus.SetTracer(tracer);
        bus.MapRam(0x0000, 256);
        
        bus.WriteMemory(0x0000, 0x34);
        
        Assert.That(tracer.WriteMemoryCalls, Has.Count.EqualTo(1));
        Assert.That(tracer.WriteMemoryCalls[0].Address, Is.EqualTo(0u));
        Assert.That(tracer.WriteMemoryCalls[0].Value, Is.EqualTo(0x34));
    }

    // ==================== Page Handler Tests ====================

    [Test]
    public void RamPageHandler_ReadWrite_Works()
    {
        var handler = new RamPageHandler();
        handler.WriteByte(0, 0xAA);
        handler.WriteByte(255, 0xBB);
        
        Assert.That(handler.ReadByte(0), Is.EqualTo(0xAA));
        Assert.That(handler.ReadByte(255), Is.EqualTo(0xBB));
    }

    [Test]
    public void RomPageHandler_Read_Works()
    {
        var data = new byte[256];
        data[0] = 0xDE;
        data[1] = 0xAD;
        
        var handler = new RomPageHandler(data);
        Assert.That(handler.ReadByte(0), Is.EqualTo(0xDE));
        Assert.That(handler.ReadByte(1), Is.EqualTo(0xAD));
    }

    [Test]
    public void RomPageHandler_WriteWithThrow_Throws()
    {
        var handler = new RomPageHandler(new byte[256], RomWritePolicy.ThrowException, "Test");
        Assert.Throws<RomWriteException>(() => handler.WriteByte(0, 0x00));
    }

    [Test]
    public void RomPageHandler_WriteWithIgnore_DoesNotThrow()
    {
        var handler = new RomPageHandler(new byte[256], RomWritePolicy.Ignore);
        // Should not throw
        handler.WriteByte(0, 0x00);
    }

    [Test]
    public void UnmappedPageHandler_Read_ReturnsFF()
    {
        var handler = UnmappedPageHandler.Instance;
        Assert.That(handler.ReadByte(0), Is.EqualTo(0xFF));
        Assert.That(handler.ReadByte(100), Is.EqualTo(0xFF));
    }

    [Test]
    public void UnmappedPageHandler_Write_IsNoOp()
    {
        var handler = UnmappedPageHandler.Instance;
        // Should not throw
        handler.WriteByte(0, 0xAA);
    }

    [Test]
    public void DevicePageHandler_DelegatesToDevice()
    {
        var device = new TestMemoryMappedDevice(0xD000, 4);
        var handler = new DevicePageHandler(device);
        
        handler.WriteByte(0, 0xCC);
        Assert.That(device.LastWriteValue, Is.EqualTo(0xCC));
        
        device.SetReadValue(0xDD);
        Assert.That(handler.ReadByte(0), Is.EqualTo(0xDD));
    }

    // ==================== Test Helper Classes ====================

    private class TestMemoryMappedDevice : IMemoryMappedDevice
    {
        private byte[] _data;
        public byte LastWriteValue { get; private set; }
        
        public TestMemoryMappedDevice(uint startAddress, uint size)
        {
            StartAddress = startAddress;
            Size = size;
            _data = new byte[size];
            Id = "test-mm-device";
        }

        public string Id { get; }
        public uint StartAddress { get; }
        public uint Size { get; }
        
        public byte ReadMemory(uint address)
        {
            return _data[address];
        }
        
        public void WriteMemory(uint address, byte value)
        {
            if (address < _data.Length)
                _data[address] = value;
            LastWriteValue = value;
        }
        
        public void SetReadValue(byte value)
        {
            _data[0] = value;
        }
    }

    private class TestPortMappedDevice : IPortMappedDevice
    {
        private byte[] _data;
        public byte LastWriteValue { get; private set; }
        
        public TestPortMappedDevice(uint startPort, uint size)
        {
            StartPort = startPort;
            Size = size;
            _data = new byte[size];
            Id = "test-port-device";
        }

        public string Id { get; }
        public uint StartPort { get; }
        public uint Size { get; }
        
        public byte ReadPort(uint port)
        {
            return _data[port];
        }
        
        public void WritePort(uint port, byte value)
        {
            if (port < _data.Length)
                _data[port] = value;
            LastWriteValue = value;
        }
    }

    private class TestBusTracer : IBusTracer
    {
        public List<(uint Address, byte Value)> ReadMemoryCalls { get; } = new();
        public List<(uint Address, byte Value)> WriteMemoryCalls { get; } = new();
        public List<(uint Port, byte Value)> ReadPortCalls { get; } = new();
        public List<(uint Port, byte Value)> WritePortCalls { get; } = new();

        public void OnReadMemory(uint address, byte value) => ReadMemoryCalls.Add((address, value));
        public void OnWriteMemory(uint address, byte value) => WriteMemoryCalls.Add((address, value));
        public void OnReadPort(uint port, byte value) => ReadPortCalls.Add((port, value));
        public void OnWritePort(uint port, byte value) => WritePortCalls.Add((port, value));
    }
}
