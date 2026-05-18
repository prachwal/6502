using System;
using Cpu6502.System;

namespace Cpu6502.System.Devices.Pia;

/// <summary>
/// Medium-accuracy implementation of MOS 6820/6821 Peripheral Interface Adapter (PIA).
/// 
/// This implementation is designed to be reusable across different computer profiles
/// (Apple-1, PET-like, SBC) and supports configurable register layouts and port bindings.
/// 
/// Medium Accuracy Features:
/// - ORA/ORB output latches
/// - DDRA/DDRB data direction registers
/// - CRA/CRB control registers with bit 2 selecting DDR/data
/// - Pin reading mixing: (outputLatch & DDR) | (externalInput & ~DDR)
/// - External pin callbacks through IPiaPortBinding
/// - Minimal IRQ support as ICpuSignalSource
/// - Configurable register layout
/// - Reset support
/// 
/// Not Implemented (for perfect accuracy):
/// - Full CA2/CB2 handshake
/// - Precise pin transition timings
/// - Edge/level IRQ with full datasheet compliance
/// - Analog effects emulation
/// </summary>
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    // ==================== Properties ====================

    /// <summary>Device identifier.</summary>
    public string Id { get; }

    /// <summary>Start address in memory space.</summary>
    public uint StartAddress { get; }

    /// <summary>Size of the memory-mapped region (always 4 bytes for PIA).</summary>
    public uint Size => 4;

    // ==================== Registers ====================

    private byte _ddra;  // Data Direction Register A
    private byte _ddrb;  // Data Direction Register B
    private byte _cra;   // Control Register A
    private byte _crb;   // Control Register B
    private byte _ora;   // Output Register A (latch)
    private byte _orb;   // Output Register B (latch)

    // ==================== Port Bindings ====================

    private readonly IPiaPortBinding _portABinding;
    private readonly IPiaPortBinding _portBBinding;
    private readonly PiaRegisterLayout _layout;

    // ==================== Constructor ====================

    /// <summary>
    /// Creates a new MOS 6820/6821 PIA device.
    /// </summary>
    /// <param name="baseAddress">Base address in memory space.</param>
    /// <param name="portABinding">Binding for Port A (e.g., keyboard input).</param>
    /// <param name="portBBinding">Binding for Port B (e.g., display output).</param>
    /// <param name="layout">Register layout configuration. Defaults to standard layout.</param>
    /// <param name="id">Optional device identifier. If null, a GUID will be generated.</param>
    /// <exception cref="ArgumentNullException">Thrown when any binding is null.</exception>
    public Mos682xPiaDevice(
        uint baseAddress,
        IPiaPortBinding portABinding,
        IPiaPortBinding portBBinding,
        PiaRegisterLayout? layout = null,
        string? id = null)
    {
        StartAddress = baseAddress;
        _portABinding = portABinding ?? throw new ArgumentNullException(nameof(portABinding));
        _portBBinding = portBBinding ?? throw new ArgumentNullException(nameof(portBBinding));
        _layout = layout ?? PiaRegisterLayout.Standard;
        Id = id ?? global::System.Guid.NewGuid().ToString();
    }

    // ==================== IMemoryMappedDevice ====================

    /// <summary>
    /// Reads a byte from the PIA device at the specified address.
    /// </summary>
    /// <param name="address">The relative address (offset from StartAddress).</param>
    /// <returns>The byte value read from the PIA register.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when address is outside the device range.</exception>
    public byte ReadMemory(uint address)
    {
        var offset = (int)address;
        
        if (offset < 0 || offset > 3)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Address {StartAddress + address:X4} is outside PIA device range [{StartAddress:X4}-{StartAddress + 3:X4}]");

        return offset switch
        {
            0 => ReadPortA(),
            1 => ReadControlRegisterA(),
            2 => ReadPortB(),
            3 => ReadControlRegisterB(),
            _ => throw new ArgumentOutOfRangeException(nameof(offset))
        };
    }

    /// <summary>
    /// Reads from Port A (offset 0).
    /// Returns either DDRA or ORA depending on CRA bit 2.
    /// When reading data: mixes output latch and external input based on DDR.
    /// </summary>
    private byte ReadPortA()
    {
        // CRA bit 2: 0 = DDRA, 1 = ORA
        if ((_cra & 0x04) == 0)
            return _ddra; // CRA.2 = 0 -> DDRA
        
        // CRA.2 = 1 -> ORA
        // Mix: (outputLatch & DDR) | (externalInput & ~DDR)
        return (byte)((_ora & _ddra) | (_portABinding.ReadPins() & ~_ddra));
    }

    /// <summary>
    /// Reads Control Register A (offset 1).
    /// Sets bit 7 based on Port A input ready status.
    /// </summary>
    private byte ReadControlRegisterA()
    {
        byte cra = _cra;
        
        // Set bit 7 if port A has input ready (for Apple-1: CRA.7 = 1 when ready)
        if (_portABinding.HasInputReady)
            cra |= 0x80;
        else
            cra &= 0x7F;
        
        return cra;
    }

    /// <summary>
    /// Reads from Port B (offset 2).
    /// Returns either DDRB or ORB depending on CRB bit 2.
    /// When reading data: mixes output latch and external input based on DDR.
    /// </summary>
    private byte ReadPortB()
    {
        // CRB bit 2: 0 = DDRB, 1 = ORB
        if ((_crb & 0x04) == 0)
            return _ddrb; // CRB.2 = 0 -> DDRB
        
        // CRB.2 = 1 -> ORB
        // Mix: (outputLatch & DDR) | (externalInput & ~DDR)
        return (byte)((_orb & _ddrb) | (_portBBinding.ReadPins() & ~_ddrb));
    }

    /// <summary>
    /// Reads Control Register B (offset 3).
    /// For Apple-1: ORB.7 = 0 when terminal is ready (inverted logic).
    /// WOZ Monitor checks this through BIT DSP / BPL (branch if bit 7 = 0).
    /// </summary>
    private byte ReadControlRegisterB()
    {
        byte crb = _crb;
        
        // WOZ Monitor does: BIT DSP / BPL
        // BPL = branch if N flag = 0 (bit 7 = 0)
        // So DSP.7 (ORB.7) = 0 means ready, 1 means busy
        // This is INVERTED from typical ready flags
        
        // Port B output is ready (IsOutputReady = true) means ORB.7 = 0 (ready)
        if (_portBBinding.IsOutputReady)
            crb &= 0x7F; // Clear bit 7: ORB.7 = 0 = ready
        else
            crb |= 0x80; // Set bit 7: ORB.7 = 1 = busy
        
        return crb;
    }

    /// <summary>
    /// Writes a byte to the PIA device at the specified address.
    /// </summary>
    /// <param name="address">The relative address (offset from StartAddress).</param>
    /// <param name="value">The byte value to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when address is outside the device range.</exception>
    public void WriteMemory(uint address, byte value)
    {
        var offset = (int)address;
        
        if (offset < 0 || offset > 3)
            throw new ArgumentOutOfRangeException(nameof(address),
                $"Address {StartAddress + address:X4} is outside PIA device range [{StartAddress:X4}-{StartAddress + 3:X4}]");

        switch (offset)
        {
            case 0:
                // CRA bit 2: 0 = DDRA, 1 = ORA
                if ((_cra & 0x04) == 0)
                    _ddra = value; // CRA.2 = 0 -> Write to DDRA
                else
                    _ora = value;  // CRA.2 = 1 -> Write to ORA
                break;

            case 1:
                _cra = value; // Write to CRA
                break;

            case 2:
                // CRB bit 2: 0 = DDRB, 1 = ORB
                if ((_crb & 0x04) == 0)
                    _ddrb = value; // CRB.2 = 0 -> Write to DDRB
                else
                {
                    _orb = value;  // CRB.2 = 1 -> Write to ORB
                    // Also write to the external binding (display output)
                    _portBBinding.WritePins(value, _ddrb);
                }
                break;

            case 3:
                _crb = value; // Write to CRB
                break;
        }
    }

    // ==================== IResettableDevice ====================

    /// <summary>
    /// Resets the PIA device to its initial state.
    /// All registers are cleared to 0.
    /// </summary>
    public void Reset()
    {
        _ddra = 0;
        _ddrb = 0;
        _cra = 0;
        _crb = 0;
        _ora = 0;
        _orb = 0;
    }

    // ==================== ICpuSignalSource ====================

    /// <summary>
    /// Checks if a CPU signal is asserted by this device.
    /// Currently only IRQ is supported (minimal implementation).
    /// </summary>
    /// <param name="signal">The CPU signal to check.</param>
    /// <returns>True if the signal is asserted, false otherwise.</returns>
    public bool IsAsserted(CpuSignal signal)
    {
        if (signal == CpuSignal.Irq)
        {
            // Minimal IRQ support: check IRQ flags in CRA and CRB
            // CRA bit 7 = IRQA1 flag, CRA bit 4 = IRQA2 flag
            // CRB bit 7 = IRQB1 flag, CRB bit 4 = IRQB2 flag
            bool irqA1 = (_cra & 0x80) != 0; // IRQA1
            bool irqA2 = (_cra & 0x10) != 0; // IRQA2
            bool irqB1 = (_crb & 0x80) != 0; // IRQB1
            bool irqB2 = (_crb & 0x10) != 0; // IRQB2
            
            return irqA1 || irqA2 || irqB1 || irqB2;
        }
        
        return false;
    }

    // ==================== Helper Methods ====================

    /// <summary>
    /// Gets the current state of all PIA registers for debugging/testing.
    /// </summary>
    public (byte DDRA, byte DDRB, byte CRA, byte CRB, byte ORA, byte ORB) GetRegisterState()
        => (_ddra, _ddrb, _cra, _crb, _ora, _orb);

    /// <summary>
    /// Gets the current register layout.
    /// </summary>
    public PiaRegisterLayout Layout => _layout;

    /// <summary>
    /// Gets the Port A binding.
    /// </summary>
    public IPiaPortBinding PortABinding => _portABinding;

    /// <summary>
    /// Gets the Port B binding.
    /// </summary>
    public IPiaPortBinding PortBBinding => _portBBinding;
}
