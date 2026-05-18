# Faza 28 - Przygotowanie do implementacji MOS 6820/6821 PIA

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | Przygotowanie do implementacji |
| **Data** | 2025-01-19 |
| **Zaleznosci** | Fazy 24-27 ✅ (wszystkie zaimplementowane) |
| **Cel** | Generyczna PIA dla Apple-1, PET-like i SBC |

---

## 📋 Podsumowanie Statusu Zaleznosci

### ✅ Zaimplementowane (Phases 24-27)

| Faza | Dokument | Status | Kluczowe Komponenty |
|------|----------|--------|-------------------|
| 24 | `faza-24-runtime-abstractions.md` | ✅ | `ICpuCore`, `CpuSignal`, `IResettableDevice`, `ICpuSignalSource` |
| 25 | `faza-25-system-bus-memory-map.md` | ✅ | `ISystemBus`, `IMemoryMappedDevice`, `RuntimeBus`, `CompiledMemoryMap` |
| 26 | `faza-26-computer-profiles.md` | ✅ | `ComputerProfile`, `DeviceProfile`, `ComputerBuilder`, `DeviceFactoryRegistry` |
| 27 | `faza-27-terminal-abstractions.md` | ✅ | `ITerminalLink`, `BufferedTerminalLink`, `TerminalTextEncoding` |

**Wniosek**: ✅ Wszystkie zaleznosci sa splnione. Faza 28 MOZE zostac rozpo czeta.

---

## 🎯 Cel Fazy 28

Zaimplementowac **generyczny, parametryzowalny** `Mos682xPiaDevice` (MOS 6820/6821):

1. Pracuje z wieloma profilami: Apple-1 (`$D010-$D013`), PET-like (innym adresem), SBC
2. Konfigurowalny layout rejestrow, bazowy adres, bindingi portow
3. Obsluguje medium accuracy: ORA/ORB, DDRA/DDRB, CRA/CRB, mieszanie odczytu
4. Integruje sie z systemem: `IMemoryMappedDevice`, `IResettableDevice`, `ICpuSignalSource`
5. Uzywa `ITerminalLink` dla bindingu Apple-1
6. Testowalny bez realnego frontendu

---

## 📚 zebrane Informacje Techniczne

### 1. MOS 6820/6821 PIA Specyfikacja

#### Rejestry

| Rejestr | Offset | Funkcja | Selektor |
|---------|--------|---------|----------|
| ORA/DDRA | +0 | Output Register A / Data Direction A | CRA.2 |
| CRA | +1 | Control Register A | - |
| ORB/DDRB | +2 | Output Register B / Data Direction B | CRB.2 |
| CRB | +3 | Control Register B | - |

**CRA.2 / CRB.2**: 0 = DDRA/DDRB, 1 = ORA/ORB

#### Mieszanie odczytu
```csharp
ReadPortA() = (OutputLatchA & DDRA) | (ExternalInput & ~DDRA)
ReadPortB() = (OutputLatchB & DDRB) | (ExternalInput & ~DDRB)
```

### 2. Apple-1 WOZ Monitor

#### Mapowanie
| Adres | Rejestr | Funkcja | WOZ Monitor |
|-------|---------|---------|--------------|
| $D010 | ORA | KBD (keyboard data) | Read, expects bit 7 = 1 |
| $D011 | CRA | KBDCR (control) | Read/write, checks bit 7 |
| $D012 | ORB | DSP (display data) | Write, bits 0-6 only |
| $D013 | CRB | DSPCR (control) | Read/write, BIT DSP checks ORB.7 |

#### Inicjalizacja
```assembly
LDY #$7F
STY DSP       ; DDRB = $7F (bits 0-6 = output, bit 7 = input)
LDA #$A7
STA KBDCR     ; CRA = $A7
STA DSPCR     ; CRB = $A7
```

#### Zachowanie (KRYTYCZNE)
- **BPL** = Branch if Plus (bit 7 = 0)
- WOZ czeka na **KBDCR.7 = 1** (LDA KBDCR / BPL loops while 0)
- WOZ czeka na **DSP.7 = 0** (BIT DSP / BPL loops while DSP.7 = 1)
- **Binding musi**: CRA.7 = 1 gdy input ready, ORB.7 = 0 gdy output ready

---

## 🏗️ Plan Implementacji

### Struktura plikow
```
src/Cpu6502/System/Devices/Pia/
├── IPiaPortBinding.cs
├── PiaRegisterLayout.cs
├── Mos682xPiaDevice.cs
├── Apple1TerminalBinding.cs
└── NullPiaPortBinding.cs

src/Cpu6502/System/Factories/
└── Mos682xPiaDeviceFactory.cs

tests/Cpu6502.Tests/System/
└── Faza28PiaTests.cs (19+ testow)
```

### Kolejnosc
1. IPiaPortBinding + PiaRegisterLayout
2. NullPiaPortBinding + Apple1TerminalBinding
3. Mos682xPiaDevice
4. Mos682xPiaDeviceFactory
5. Testy
6. Dokumentacja

---

## 📋 Specyfikacja Interfejsow

### IPiaPortBinding
```csharp
public interface IPiaPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte directionMask);
    bool HasInputReady { get; }
    bool IsOutputReady { get; }
}
```

### Apple1TerminalBinding
```csharp
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    
    public Apple1TerminalBinding(ITerminalLink terminal)
    {
        _terminal = terminal;
    }
    
    public bool HasInputReady => _terminal.HasInput;
    public bool IsOutputReady => true; // Buffered terminal always ready
    
    public byte ReadPins()
    {
        if (_terminal.HasInput && _terminal.TryReadByte(out byte value))
            return (byte)(value | 0x80); // Set bit 7 for WOZ
        return 0;
    }
    
    public void WritePins(byte value, byte directionMask)
    {
        // Only bits 0-6 are output (DDRB = 0x7F)
        _terminal.WriteByte((byte)(value & 0x7F));
    }
}
```

### Mos682xPiaDevice
```csharp
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    public string Id { get; }
    public uint StartAddress { get; }
    public uint Size => 4;
    
    private byte _ddra, _ddrb, _cra, _crb, _ora, _orb;
    private readonly IPiaPortBinding _portA, _portB;
    private readonly PiaRegisterLayout _layout;
    
    public Mos682xPiaDevice(uint baseAddress, IPiaPortBinding portA, 
        IPiaPortBinding portB, PiaRegisterLayout? layout = null, string? id = null)
    { ... }
    
    public byte ReadMemory(uint address)
    {
        var offset = address - StartAddress;
        return offset switch
        {
            0 => ReadPortA(),  // ORA or DDRA
            1 => ReadCRA(),    // CRA with status flags
            2 => ReadPortB(),  // ORB or DDRB
            3 => ReadCRB(),    // CRB with status flags
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private byte ReadPortA()
    {
        if ((_cra & 0x04) == 0) return _ddra; // CRA.2=0 -> DDRA
        return (byte)((_ora & _ddra) | (_portA.ReadPins() & ~_ddra));
    }
    
    private byte ReadCRA()
    {
        byte cra = _cra;
        // Set bit 7 if port A has input ready
        if (_portA.HasInputReady) cra |= 0x80;
        else cra &= 0x7F;
        return cra;
    }
    
    private byte ReadPortB()
    {
        if ((_crb & 0x04) == 0) return _ddrb; // CRB.2=0 -> DDRB
        return (byte)((_orb & _ddrb) | (_portB.ReadPins() & ~_ddrb));
    }
    
    private byte ReadCRB()
    {
        byte crb = _crb;
        // WOZ checks ORB.7 through BIT DSP / BPL
        // BPL = branch if bit 7 = 0, so ORB.7 = 0 means ready
        if (_portB.IsOutputReady) crb &= 0x7F; // ORB.7 = 0 = ready
        else crb |= 0x80; // ORB.7 = 1 = busy
        return crb;
    }
    
    public void WriteMemory(uint address, byte value)
    {
        var offset = address - StartAddress;
        switch (offset)
        {
            case 0:
                if ((_cra & 0x04) == 0) _ddra = value; // CRA.2=0 -> DDRA
                else _ora = value;                    // CRA.2=1 -> ORA
                break;
            case 1: _cra = value; break;
            case 2:
                if ((_crb & 0x04) == 0) _ddrb = value; // CRB.2=0 -> DDRB
                else _orb = value;                    // CRB.2=1 -> ORB
                break;
            case 3: _crb = value; break;
        }
    }
    
    public void Reset()
    {
        _ddra = _ddrb = 0;
        _cra = _crb = 0;
        _ora = _orb = 0;
    }
    
    public bool IsAsserted(CpuSignal signal)
    {
        if (signal == CpuSignal.Irq)
        {
            // Minimal IRQ support
            bool irqA = (_cra & 0x80) != 0; // IRQA1
            bool irqB = (_crb & 0x80) != 0; // IRQB1
            return irqA || irqB;
        }
        return false;
    }
}
```

---

## 🧪 Testy Jednostkowe (19+)

### Testy Podstawowe (9 z specyfikacji)
1. `WriteDdra_WhenCraSelectsDdr_StoresDirection`
2. `WritePortA_WhenCraSelectsData_UpdatesOutputLatch`
3. `ReadPortA_MergesOutputAndExternalInput`
4. `WriteDdrB_AndPortB_BehaveLikePortA`
5. `ControlBit2_SelectsDdrOrDataRegister`
6. `Reset_ClearsDirectionAndOutputLatches`
7. `Device_WithBaseD010_MapsApple1Offsets`
8. `Device_WithDifferentBase_MapsSameRegisters`
9. `Factory_CreatesMos6821FromProfile`

### Testy Flag Statusowych (5)
10. `Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus` (CRA.7 = 1)
11. `Apple1Preset_ReadKbdCr_WhenNoInput_ReturnsNotReady` (CRA.7 = 0)
12. `Apple1Preset_ReadDspCr_WhenReady_ReturnsZeroInBit7` (CRB.7 = 0)
13. `Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet` (0xC1)
14. `Apple1Preset_WriteDsp_StripsHighBitBeforeOutput` (0x7F)

### Testy Mieszania (3)
15. `ReadPortA_WhenDdraAllOutput_ReturnsOutputLatch`
16. `ReadPortA_WhenDdraAllInput_ReturnsExternalInput`
17. `ReadPortA_MixesOutputLatchAndExternalInput`

### Testy Integracyjne (2)
18. `Apple1Terminal_SimulateWozInputLoop_WaitsForKbdCrBit7`
19. `Apple1Terminal_SimulateWozOutputLoop_WaitsForDspBit7`

---

## 📊 Kryteria Akceptacji

- [ ] Ta sama klasa PIA dziala z co najmniej dwoma bazowymi adresami
- [ ] Apple-1 jest presetem/bindingiem, nie osobnym urzadzeniem
- [ ] Testy nie odwoluja sie do stalej $D010 (poza testem presetowym)
- [ ] Minimalne IRQ jest zaimplementowane
- [ ] Wszystkie 19+ testow przechodzi
- [ ] Dokumentacja jest kompletna

---

## 📚 Zrodla i Referencje

- [MOS 6821 Datasheet](https://www.wdc65xx.com/wdc/documentation/w65c21.pdf)
- [Apple-1 Block Diagram](https://www.sbprojects.net/projects/apple1/a1block.php)
- [WOZ Monitor Source](https://github.com/jefftranter/6502/blob/master/asm/wozmon/wozmon.s)
- [WOZ Monitor Analysis](https://www.steckschwein.de/post/wozmon-a-memory-monitor-in-256-bytes/)

---

## ✅ Gotowosc do Implementacji

**Wszystkie zaleznosci sa splnione.**
**Wszystkie decyzje sa podjete.**
**Wszystkie specyfikacje sa zebrane.**

**Status: GOTOWY DO IMPLEMENTACJI** 🟢
