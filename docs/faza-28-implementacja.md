# Faza 28 - MOS 6820/6821 PIA Medium Implementation

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | ✅ Zaimplementowana |
| **Data implementacji** | 2025-01-19 |
| **Zakres** | Generyczny rownolegly uklad I/O |
| **Zaleznosci** | Fazy 24-27 ✅ |
| **Cel projektowy** | Jeden PIA dla Apple-1, PET-like profili i SBC |

---

## Cel fazy

Zaimplementowano srednio-dokladny `Mos682xPiaDevice`, ktory:

- Obsluguje ORA/ORB i DDRA/DDRB
- Implementuje CRA/CRB z wyborem DDR/data register przez bit 2
- Mieszanie odczytu: `(outputLatch & ddr) | (externalInput & ~ddr)`
- Callbacki/bindingi pinow zewnetrznych przez `IPiaPortBinding`
- Konfigurowalny layout rejestrow i adres bazowy
- Minimalne IRQ jako `ICpuSignalSource`
- Preset `apple-1-terminal`
- Dziala z co najmniej dwoma roznymi bazowymi adresami

---

## Zaimplementowane klasy

### 1. IPiaPortBinding (`src/Cpu6502/System/Devices/Pia/IPiaPortBinding.cs`)

Interfejs do laczenia portow PIA z urzadzeniami zewnetrznymi.

```csharp
public interface IPiaPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte directionMask);
    bool HasInputReady { get; }
    bool IsOutputReady { get; }
}
```

### 2. PiaRegisterLayout (`src/Cpu6502/System/Devices/Pia/PiaRegisterLayout.cs`)

Konfiguracja layoutu rejestrow PIA. Pozwala na rozne układy dla róznych profili.

```csharp
public sealed record PiaRegisterLayout(
    int OraDdraOffset,   // Typowo 0
    int CraOffset,      // Typowo 1
    int OrbDdrbOffset,  // Typowo 2
    int CrbOffset)      // Typowo 3
```

### 3. NullPiaPortBinding (`src/Cpu6502/System/Devices/Pia/NullPiaPortBinding.cs`)

Pusta implementacja bindingu do testów. Wszystkie odczyt zwracaja 0, zapisy sa ignorowane.

### 4. Apple1TerminalBinding (`src/Cpu6502/System/Devices/Pia/Apple1TerminalBinding.cs`)

Binding terminalowy dla Apple-1. Laczy PIA z `BufferedTerminalLink`.

**Kluczowe zachowania:**
- `ReadPins()`: Zwraca znak z bitem 7 = 1 (format oczekiwany przez WOZ)
- `HasInputReady`: Ustawia CRA.7 = 1 gdy terminal ma wejscie
- `IsOutputReady`: Zawsze true (buforowany terminal)
- `WritePins()`: Pisze bity 0-6 do terminala (bit 7 ignorowany przez DDRB = 0x7F)

**Uwaga:** `ReadPins()` uzywa `TryPeekByte()` aby nie konsumowac znaku. To pozwala na:
1. Odczyt KBD ($D010) zwraca znak z bit 7 = 1
2. Odczyt KBDCR ($D011) wciąż widzi CRA.7 = 1 (znak dostepny)

### 5. Mos682xPiaDevice (`src/Cpu6502/System/Devices/Pia/Mos682xPiaDevice.cs`)

Glowna implementacja PIA. Implementuje:
- `IMemoryMappedDevice` (ReadMemory, WriteMemory)
- `IResettableDevice` (Reset)
- `ICpuSignalSource` (IsAsserted dla IRQ)

**Rejestry:**
- DDRA, DDRB: Data Direction Registers
- ORA, ORB: Output Registers (latch)
- CRA, CRB: Control Registers

**Mieszanie odczytu portu:**
```csharp
ReadPortA() = (byte)((_ora & _ddra) | (_portABinding.ReadPins() & ~_ddra));
ReadPortB() = (byte)((_orb & _ddrb) | (_portBBinding.ReadPins() & ~_ddrb));
```

**CRA.7 / CRB.7 status flags:**
- CRA.7 = 1 gdy `_portABinding.HasInputReady` = true
- CRB.7 / ORB.7 = 0 gdy `_portBBinding.IsOutputReady` = true (inverted logic!)

### 6. Mos682xPiaDeviceFactory (`src/Cpu6502/System/Factories/Mos682xPiaDeviceFactory.cs`)

Fabryka PIA dla profili. Implementuje `IDeviceFactory`.

**Metody statyczne:**
- `CreateApple1Terminal(baseAddress, terminal)`: Tworzy PIA z presetem Apple-1
- `CreateWithBindings(baseAddress, portA, portB)`: Tworzy PIA z custom bindingami
- `CreateWithNullBindings(baseAddress)`: Tworzy PIA z null bindingami
- `RegisterDefault()` / `RegisterWith(registry)`: Rejestracja fabryki

---

## Rozszerzenia BufferedTerminalLink

Dodano metode `TryPeekByte()` do `BufferedTerminalLink` aby wsparc binding Apple-1:

```csharp
public bool TryPeekByte(out byte value)
{
    if (_inputBuffer.Count > 0)
    {
        value = _inputBuffer.Peek();
        return true;
    }
    value = 0;
    return false;
}
```

---

## Testy jednostkowe

**Liczba testów:** 43 (wszystkie przechodza)

### Kategorie testów:

#### 1. PiaRegisterLayout (4 testy)
- Standard offsets
- Walidacja layoutu
- Duplicate offsets
- Offset out of range

#### 2. NullPiaPortBinding (4 testy)
- ReadPins zwraca 0
- WritePins nic nie robi
- HasInputReady = false
- IsOutputReady = true

#### 3. Apple1TerminalBinding (6 testów)
- ReadPins bez inputu
- ReadPins z inputem (bit 7 = 1)
- HasInputReady z inputem
- HasInputReady bez inputu
- IsOutputReady = true
- WritePins ignoruje bit 7

#### 4. Mos682xPiaDevice - Properties (2 testy)
- Id, StartAddress, Size
- Default Id nie jest pusty

#### 5. Register Selection (CRA.2 / CRB.2) (3 testy)
- WriteDdra when CRA.2 = 0
- WritePortA when CRA.2 = 1
- ControlBit2 selects DDR or data
- WriteDdrB and PortB behave like PortA

#### 6. Pin Reading Mixing (3 testy)
- ReadPortA merges output and external
- ReadPortA with all output
- ReadPortA with all input

#### 7. Reset (1 test)
- Reset clears all registers

#### 8. Address Mapping (4 testy)
- Device with base $D010 maps correctly
- Device with different base maps same registers
- ReadMemory out of range throws
- WriteMemory out of range throws

#### 9. Apple-1 Preset (5 testów)
- ReadKbd with input returns character with high bit set
- ReadKbdCr with input returns ready status
- ReadKbdCr without input returns not ready
- WriteDsp strips high bit
- ReadDspCr returns zero in bit 7

#### 10. WOZ Monitor Simulation (2 testy)
- Simulate WOZ input loop (KBDCR.7)
- Simulate WOZ output loop (DSP.7)

#### 11. Factory (2 testy)
- Factory creates from profile
- CreateApple1Terminal with different base

#### 12. IRQ (3 testy)
- IRQ asserted when flag set
- IRQ with multiple flags
- IRQ not asserted when no flags
- IRQ for non-IRQ signals returns false

#### 13. GetRegisterState (1 test)
- Returns correct register values

---

## Integracja z systemem

### Profil JSON (przykladowy)

```json
{
  "id": "pia0",
  "type": "mos6821-pia",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0xD010",
    "size": "0x0004"
  },
  "options": {
    "preset": "apple-1-terminal"
  }
}
```

**Uwaga:** Obecnie preset `apple-1-terminal` wymaga podania terminala w code (nie przez profil JSON).
To zostanie ulepszone w przyszlych fazach.

### Rejestracja fabryki

```csharp
// Rejestracja domyslna
Mos682xPiaDeviceFactory.RegisterDefault();

// Lub z konkretna rejestria
Mos682xPiaDeviceFactory.RegisterWith(registry);
```

### Uzycie z terminalem

```csharp
// Tworzenie terminala
var terminal = new BufferedTerminalLink();
terminal.EnqueueText("A", TerminalTextEncoding.RawBytes);

// Tworzenie PIA z presetem Apple-1
var pia = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

// Dodanie do magistrali
bus.MapDevice(pia);

// Odczyt KBD (czeka na CRA.7 = 1)
byte kbdCr = bus.ReadMemory(0xD011);
if ((kbdCr & 0x80) != 0)
{
    byte kbd = bus.ReadMemory(0xD010); // Zwraca 'A' | 0x80 = 0xC1
}

// Zapis DSP
bus.WriteMemory(0xD012, 0x41); // Pisze 'A' do terminala (bity 0-6)
```

---

## Decyzje implementacyjne

| Decyzja | Wybor | Uzasadnienie |
|---------|-------|--------------|
| **CRA/CRB bit 2** | 0 = DDRA/DDRB, 1 = ORA/ORB | Zgodne z datasheetem MOS 6821 |
| **KBD bit 7** | Ustawiany w bindingu | PIA nie zna semantyki Apple-1 |
| **DSP bit 7** | Inwertowany w bindingu | WOZ Monitor oczekuje ORB.7 = 0 |
| **ReadPins()** | Peek, nie consume | Zachowuje stan pinów do kolejnych odczytów |
| **IRQ** | Minimalna obsługa | Tylko flagi w CRA/CRB, bez full handshake |
| **Layout rejestrów** | Parametryzowalny | Obsługa róznych układów (Apple-1, PET) |
| **Terminal** | BufferedTerminalLink | Wsparcie dla testów i prostych scenariuszy |

---

## Kryteria akceptacji

- ✅ Ta sama klasa PIA dziala z co najmniej dwoma bazowymi adresami
- ✅ Apple-1 jest presetem/bindingiem, nie osobnym urzadzeniem
- ✅ Testy nie odwoluja sie do stalej $D010 (poza testem presetowym)
- ✅ Minimalne IRQ jest zaimplementowane
- ✅ Wszystkie 43 testy przechodza
- ✅ Dokumentacja jest kompletna

---

## Poza zakresem (odlozone do przyszlych faz)

- Pelny handshake CA2/CB2
- Dokladne timingi przejsc pinow
- Edge/level IRQ w pelnej zgodnosci z datasheetem
- Emulacja analogowych efektów ukladu
- Pelny PET (VIA, CRTC, video RAM)
- Pelny Apple-1 WOZ Monitor (ROM, integration tests)
- Podlaczenie terminala przez profil JSON (wymaga rozszerzenia ProfileLoadOptions)

---

## Pliki

| Plik | Rozmiar | Opis |
|------|--------|------|
| `src/Cpu6502/System/Devices/Pia/IPiaPortBinding.cs` | ~1.7 KB | Interfejs bindingu portów |
| `src/Cpu6502/System/Devices/Pia/PiaRegisterLayout.cs` | ~2.7 KB | Layout rejestrow PIA |
| `src/Cpu6502/System/Devices/Pia/NullPiaPortBinding.cs` | ~0.8 KB | Pusty binding do testów |
| `src/Cpu6502/System/Devices/Pia/Apple1TerminalBinding.cs` | ~4.1 KB | Binding terminalowy Apple-1 |
| `src/Cpu6502/System/Devices/Pia/Mos682xPiaDevice.cs` | ~10.3 KB | Glowna implementacja PIA |
| `src/Cpu6502/System/Factories/Mos682xPiaDeviceFactory.cs` | ~8.4 KB | Fabryka PIA |
| `src/Cpu6502/System/Terminal/BufferedTerminalLink.cs` | +17 lines | Dodano TryPeekByte |
| `tests/Cpu6502.Tests/System/Faza28PiaTests.cs` | ~23.3 KB | 43 testy jednostkowe |
| `docs/faza-28-implementacja.md` | - | Dokumentacja |

---

## Zrodla

- [MOS 6821 Datasheet](https://www.wdc65xx.com/wdc/documentation/w65c21.pdf)
- [Apple-1 Block Diagram](https://www.sbprojects.net/projects/apple1/a1block.php)
- [WOZ Monitor Source](https://github.com/jefftranter/6502/blob/master/asm/wozmon/wozmon.s)
- [WOZ Monitor Analysis](https://www.steckschwein.de/post/wozmon-a-memory-monitor-in-256-bytes/)
