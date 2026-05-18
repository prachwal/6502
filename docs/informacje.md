Oto **kompletne podsumowanie** wszystkich niezbędnych informacji do implementacji **Apple-1 PIA Terminal Device**, zorganizowane w logiczne sekcje: **architektura, specyfikacja techniczna, decyzje implementacyjne, zależności, testy, źródła i harmonogram**. Wszystkie dane są spójne z Twoim planem i uzupełnione o brakujące detale techniczne.

---

---

## **1. Podsumowanie Celów i Zakresu**
### **1.1 Cel główny**
- Uruchomić **Apple-1 WOZ Monitor** z poprawną obsługą **klawiatury** i **wyjścia znakowego** poprzez **PIA (MOS 6820/6821)**.
- Zapewnić **reusable** implementację `Mos682xPiaDevice`, która może być użyta w innych profilach (np. **PET-like, SBC**).

### **1.2 Zakres adresowy Apple-1**
| Adres  | Rejestr PIA | Funkcja Apple-1          | Kierunek | Semantyka                                                                 |
|--------|-------------|--------------------------|----------|---------------------------------------------------------------------------|
| `$D010` | ORA         | **KBD** (dane klawiatury) | Read     | Bit 7 = 1 (znak gotowy), bity 0-6 = ASCII (uppercase)                     |
| `$D011` | CRA         | **KBDCR** (kontrola)     | R/W      | Bit 7 = 1 (znak gotowy), inicjalizowany jako `$A7` przez WOZ Monitor     |
| `$D012` | ORB         | **DSP** (dane wyświetlacza)| Write    | Bity 0-6 = znak do wyświetlenia, bit 7 **ignorowany** (DDRB = `$7F`)      |
| `$D013` | CRB         | **DSPCR** (kontrola)     | R/W      | Bit 7 = 0 (gotowy na zapis), inicjalizowany jako `$A7` przez WOZ Monitor |

---
---
## **2. Specyfikacja Techniczna PIA (MOS 6820/6821)**

### **2.1 Rejestry PIA**
| Rejestr | Offset | Opis                                                                                     | Uwagi dla Apple-1                                                                 |
|---------|--------|------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------|
| **ORA** | +0     | Output Register A (latch wyjściowy)                                                     | Mapowany na `$D010` (KBD) — **CRA.2 = 1** (wybór ORA zamiast DDRA)                  |
| **DDRA**| +0     | Data Direction Register A (kierunek pinów PA0-PA7)                                      | Nie używany bezpośrednio w Apple-1 (DDR=0 dla Port A)                                |
| **CRA** | +1     | Control Register A (kontrola CA1/CA2, IRQ, bit 2: 0=DDRA, 1=ORA)                         | Inicjalizowany jako `$A7` (bit 2=1 → ORA na `$D010`)                                |
| **ORB** | +2     | Output Register B (latch wyjściowy)                                                     | Mapowany na `$D012` (DSP) — **CRB.2 = 1** (wybór ORB zamiast DDRB)                  |
| **DDRB**| +2     | Data Direction Register B                                                               | DDRB = `$7F` (bity 0-6 = output, bit 7 = input)                                       |
| **CRB** | +3     | Control Register B (kontrola CB1/CB2, IRQ, bit 2: 0=DDRB, 1=ORB)                         | Inicjalizowany jako `$A7` (bit 2=1 → ORB na `$D012`)                                |

### **2.2 Zachowanie odczytu portu (mieszanie)**
```csharp
byte ReadPortA() => (byte)((OutputLatchA & Ddra) | (ExternalInputA & ~Ddra));
```
- **Bity output (DDR=1)**: Zwracają wartość z **latch** (ORA/ORB).
- **Bity input (DDR=0)**: Zwracają wartość z **zewnętrznych pinów** (binding terminalowy).

### **2.3 Linie kontrolne (CA1/CA2, CB1/CB2)**
| Linia | Tryb (CRA/CRB) | Funkcja                                                                 |
|-------|----------------|-------------------------------------------------------------------------|
| CA1   | Input          | Przerwanie na zboczu (konfigurowalne: opadające/wstępujące)           |
| CA2   | Input/Output   | Output: sterowany przez CRA.3; Input: przerwanie na zboczu            |
| CB1   | Input          | Przerwanie na zboczu (konfigurowalne)                                 |
| CB2   | Input/Output   | Output: sterowany przez CRB.3; Input: przerwanie na zboczu            |

**Uwaga**: WOZ Monitor **nie używa przerwań IRQ** od PIA, ale implementacja powinna je obsługiwać dla przyszłych profili.

---
---
## **3. Binding Terminalowy (Apple-1)**
### **3.1 Interfejs `ITerminalLink`**
```csharp
public interface ITerminalLink
{
    bool HasInput { get; }          // Czy jest znak do odczytu?
    bool TryReadByte(out byte value); // Odczyt znaku (ASCII, bit 7 = 1 jeśli gotowy)
    void WriteByte(byte value);     // Zapis znaku (bity 0-6)
}
```
- **Implementacje**:
  - `BufferedTerminalLink` (bufor testowy),
  - `ConsoleTerminalLink` (TUI),
  - `AvaloniaTerminalLink` (UI),
  - `BlazorTerminalLink` (web).

### **3.2 Zachowanie bindingu `apple-1-terminal`**
| Sygnał PIA       | Kierunek | Semantyka                                                                 | Implementacja w bindingu                     |
|------------------|----------|---------------------------------------------------------------------------|---------------------------------------------|
| **ORA (KBD)**    | Read     | Bit 7 = 1 (znak gotowy), bity 0-6 = ASCII                                 | `TryReadByte()` → ustaw `ORA = (byte)(value | 0x80)` |
| **CRA (KBDCR)**  | R/W      | Bit 7 = 1 (znak gotowy)                                                  | Ustaw `CRA.7 = 1` gdy `HasInput == true`     |
| **ORB (DSP)**    | Write    | Bity 0-6 = znak do wyświetlenia, bit 7 **ignorowany** (DDRB = `$7F`)      | `WriteByte(value & 0x7F)`                   |
| **CRB (DSPCR)**  | R/W      | Bit 7 = 0 (gotowy na zapis), bit 7 = 1 (zajęty)                          | Ustaw `ORB.7 = 0` gdy terminal gotowy       |

**Korekta WOZ Monitor**:
- WOZ sprawdza **`KBDCR.7`** (nie `KBD.7`) w pętli wejścia.
- WOZ sprawdza **`DSP.7`** (ORB.7) w pętli wyjścia:
  - `BIT DSP` / `BPL` → czeka na **ORB.7 = 0** (gotowy).
  - Binding musi **inwertować ORB.7** (0 = gotowy, 1 = zajęty).

---
---
## **4. Implementacja `Mos682xPiaDevice`**
### **4.1 Struktura klasy**
```csharp
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    public uint StartAddress { get; }
    public uint Size => 4; // 4 rejestry (ORA/CRA/ORB/CRB)

    // Rejestry
    private byte _ora, _orb, _ddra, _ddrb, _cra, _crb;
    private byte _outputLatchA, _outputLatchB;
    private byte _externalInputA, _externalInputB; // Z bindingów

    // Bindingi portów (IPiaPortBinding)
    private readonly IPiaPortBinding _portABinding;
    private readonly IPiaPortBinding _portBBinding;

    // IRQ
    public event Action<CpuSignal>? OnSignal;

    public byte ReadMemory(uint address)
    {
        var offset = address - StartAddress;
        return offset switch
        {
            0 => ReadPortA(), // ORA lub DDRA (zależy od CRA.2)
            1 => _cra,
            2 => ReadPortB(), // ORB lub DDRB (zależy od CRB.2)
            3 => _crb,
            _ => throw new InvalidOperationException()
        };
    }

    public void WriteMemory(uint address, byte value)
    {
        var offset = address - StartAddress;
        switch (offset)
        {
            case 0:
                if ((_cra & 0x04) == 0) _ddra = value; // CRA.2 = 0 → DDRA
                else _outputLatchA = value;            // CRA.2 = 1 → ORA
                break;
            case 1: _cra = value; break;
            case 2:
                if ((_crb & 0x04) == 0) _ddrb = value; // CRB.2 = 0 → DDRB
                else _outputLatchB = value;            // CRB.2 = 1 → ORB
                break;
            case 3: _crb = value; break;
        }
    }

    private byte ReadPortA() => (byte)((_outputLatchA & _ddra) | (_portABinding.ReadPins() & ~_ddra));
    private byte ReadPortB() => (byte)((_outputLatchB & _ddrb) | (_portBBinding.ReadPins() & ~_ddrb));

    public void Reset()
    {
        _ora = _orb = _ddra = _ddrb = _cra = _crb = 0;
        _outputLatchA = _outputLatchB = 0;
    }
}
```

### **4.2 Interfejs `IPiaPortBinding`**
```csharp
public interface IPiaPortBinding
{
    byte ReadPins();          // Odczyt stanu pinów (dla input)
    void WritePins(byte value, byte directionMask); // Zapis do pinów (dla output)
}
```

### **4.3 Binding `Apple1TerminalBinding`**
```csharp
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    private bool _hasInput = false;
    private byte _inputByte = 0;

    public Apple1TerminalBinding(ITerminalLink terminal)
    {
        _terminal = terminal;
    }

    public byte ReadPins()
    {
        if (_terminal.HasInput && _terminal.TryReadByte(out var value))
        {
            _hasInput = true;
            _inputByte = value;
        }
        return _hasInput ? (byte)(_inputByte | 0x80) : (byte)0;
    }

    public void WritePins(byte value, byte directionMask)
    {
        if ((directionMask & 0x7F) != 0) // Bity 0-6 = output
        {
            _terminal.WriteByte((byte)(value & 0x7F));
        }
        // Bit 7 = input (DDRB = 0x7F) → ignorowany
    }
}
```

---
---
## **5. Preset `apple-1-terminal`**
### **5.1 Konfiguracja fabryki**
```csharp
public static class Mos682xPiaDeviceFactory
{
    public static Mos682xPiaDevice CreateApple1Terminal(uint baseAddress, ITerminalLink terminal)
    {
        var device = new Mos682xPiaDevice(baseAddress);
        device.SetPortABinding(new Apple1TerminalBinding(terminal));
        device.SetPortBBinding(new Apple1TerminalBinding(terminal)); // DSP używa Port B

        // Inicjalizacja rejestrów (WOZ Monitor)
        device.WriteMemory(baseAddress + 1, 0xA7); // CRA = $A7 (bit 2=1 → ORA na base+0)
        device.WriteMemory(baseAddress + 3, 0xA7); // CRB = $A7 (bit 2=1 → ORB na base+2)
        device.WriteMemory(baseAddress + 2, 0x7F); // DDRB = $7F (bity 0-6 = output)

        return device;
    }
}
```

### **5.2 Konfiguracja profilu JSON**
```json
{
  "id": "pia0",
  "type": "mos6821-pia",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0xD010",
    "size": "0x0004"
  },
  "preset": "apple-1-terminal",
  "bindings": {
    "portA": { "type": "terminal", "terminalId": "console" },
    "portB": { "type": "terminal", "terminalId": "console" }
  }
}
```

---
---
## **6. Testy Jednostkowe**
### **6.1 Przypadki testowe dla PIA**
| Test | Opis | Oczekiwany wynik |
|------|------|------------------|
| `WriteDdra_WhenCraSelectsDdr_StoresDirection` | Zapis do `$D010` z CRA.2=0 → DDRA | DDRA = value |
| `WritePortA_WhenCraSelectsData_UpdatesOutputLatch` | Zapis do `$D010` z CRA.2=1 → ORA | ORA = value |
| `ReadPortA_MergesOutputAndExternalInput` | Odczyt `$D010` z DDR=0xFF i external=0xAA | Zwraca ORA |
| `ReadPortA_MergesInputAndLatch` | Odczyt `$D010` z DDR=0x00 i external=0xAA | Zwraca external |
| `Device_WithBaseD010_MapsApple1Offsets` | Adresy `$D010-$D013` mapują ORA/CRA/ORB/CRB | Prawidłowe mapowanie |
| `Device_WithDifferentBase_MapsSameRegisters` | Adres bazowy `$E810` → rejestry na `$E810-$E813` | Prawidłowe mapowanie |

### **6.2 Testy dla bindingu `apple-1-terminal`**
```csharp
[Test]
public void Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1); // 'A' = 0x41 → 0xC1 (bit 7=1)
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var value = device.ReadMemory(0xD010); // ORA
    Assert.That(value, Is.EqualTo(0xC1));
}

[Test]
public void Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var kbdCr = device.ReadMemory(0xD011); // CRA
    Assert.That(kbdCr & 0x80, Is.EqualTo(0x80)); // Bit 7 = 1 (gotowy)
}

[Test]
public void Apple1Preset_WriteDsp_StripsHighBitBeforeOutput()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    device.WriteMemory(0xD012, 0xFF); // DSP (ORB) — bit 7 powinien zostać zignorowany
    Assert.That(terminal.LastWrittenByte, Is.EqualTo(0x7F)); // Tylko bity 0-6
}

[Test]
public void Apple1Preset_DspCr_Bit7_ClearedWhenReady()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var dspCr = device.ReadMemory(0xD013); // CRB
    Assert.That(dspCr & 0x80, Is.EqualTo(0x00)); // Bit 7 = 0 (gotowy)
}
```

### **6.3 Testy dla drugiego profilu (PET-like)**
```csharp
[Test]
public void PetLikeProfile_MapsPiaAtE810()
{
    var terminal = new BufferedTerminalLink();
    var device = new Mos682xPiaDevice(0xE810);
    device.SetPortABinding(new PetKeyboardMatrixBinding(terminal));

    // Sprawdź, czy rejestry są mapowane na $E810-$E813
    device.WriteMemory(0xE811, 0xA7); // CRA
    Assert.That(device.ReadMemory(0xE811), Is.EqualTo(0xA7));
}

[Test]
public void Mos682xPia_SameDeviceSupportsApple1AndPetLike()
{
    var apple1Device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, new BufferedTerminalLink());
    var petDevice = new Mos682xPiaDevice(0xE810);
    petDevice.SetPortABinding(new PetKeyboardMatrixBinding(new BufferedTerminalLink()));

    // Obie instancje używają tej samej klasy `Mos682xPiaDevice`
    Assert.That(apple1Device.GetType(), Is.EqualTo(petDevice.GetType()));
}
```

---
---
## **7. Zależności i Blokery**
### **7.1 Zależności od innych faz**
| Faza | Plik | Status | Wpływ na PIA |
|------|------|--------|--------------|
| 24 | `faza-24-runtime-abstractions.md` | ❌ Nie zaimplementowana | `ICpuSignalSource` (IRQ), `IResettableDevice` |
| 25 | `faza-25-system-bus-memory-map.md` | ❌ Nie zaimplementowana | `IMemoryMappedDevice`, `ISystemBus` |
| 26 | `faza-26-computer-profiles.md` | ❌ Nie zaimplementowana | `DeviceFactoryRegistry`, `ComputerProfile` |
| 27 | `faza-27-terminal-abstractions.md` | ❌ Nie zaimplementowana | `ITerminalLink`, `BufferedTerminalLink` |

**Bloker**: Faza 28 **nie może zostać rozpoczęta** przed ukończeniem faz 24-27.

### **7.2 Decyzje implementacyjne do podjęcia**
| Decyzja | Propozycja | Uzasadnienie |
|---------|------------|--------------|
| **CRA/CRB bit 2** | Zezwolić na konfigurowalny layout rejestrów | Elastyczność dla innych profili (np. PET) |
| **KBD bit 7** | Ustawiać w bindingu terminalowym | PIA nie zna semantyki Apple-1 |
| **DSP bit 7** | Inwertować w bindingu (0 = gotowy) | WOZ Monitor oczekuje ORB.7 = 0 |
| **IRQ** | Implementować minimalnie | Dla przyszłych profili (np. PET) |
| **Layout rejestrów** | Parametryzować przez `PiaRegisterLayout` | Obsługa różnych układów PIA |

---
---
## **8. Harmonogram Implementacji**
| Etap | Zadanie | Czas (szacowany) | Zależności | Priorytet |
|------|---------|------------------|------------|-----------|
| 1 | Zaimplementować fazy 24-27 (abstrakcje runtime, bus, profile, terminal) | 4-6 dni | Brak | ⭐⭐⭐ |
| 2 | Zaimplementować `ITerminalLink` i `BufferedTerminalLink` | 2 godziny | Faza 27 | ⭐⭐⭐ |
| 3 | Zaimplementować `Mos682xPiaDevice` (medium) | 4-6 godzin | Fazy 24-25 | ⭐⭐⭐ |
| 4 | Zaimplementować `Apple1TerminalBinding` | 2 godziny | Faza 27 | ⭐⭐⭐ |
| 5 | Dodać preset `apple-1-terminal` | 1 godzina | Etap 3-4 | ⭐⭐⭐ |
| 6 | Zaimplementować drugi binding (PET-like) | 3-4 godziny | Etap 3 | ⭐⭐ |
| 7 | Testy jednostkowe dla PIA | 3 godziny | Etap 3-6 | ⭐⭐⭐ |
| 8 | Integracja z loaderem profilu | 2 godziny | Faza 26 | ⭐⭐⭐ |
| 9 | Uruchomić Apple-1 z WOZ Monitor | 2 godziny | Etap 8 | ⭐⭐⭐ |
| 10 | Walidacja drugiego profilu (PET-like) | 2 godziny | Etap 6 | ⭐⭐ |

---
---
## **9. Kryteria Akceptacji**
- [ ] Apple-1 startuje **bez błędu mapowania urządzeń**.
- [ ] WOZ Monitor **odczytuje znak z klawiatury** przez `$D010/$D011`.
- [ ] WOZ Monitor **wyświetla znak** przez `$D012/$D013`.
- [ ] Testy jednostkowe pokrywają **średnią semantykę PIA** oraz preset Apple-1.
- [ ] Ten sam rdzeń PIA jest użyty w **drugim profilu walidacyjnym** (PET-like) z inną adresacją.
- [ ] Implementacja **nie zależy bezpośrednio** od konkretnego frontendu (TUI/WPF/Avalonia).
- [ ] Binding terminalowy **poprawnie inwertuje ORB.7** (0 = gotowy, 1 = zajęty).

---
---
## **10. Źródła i Referencje**
### **10.1 Datasheet PIA**
| Źródło | Opis | Link |
|--------|------|------|
| W65C21 Datasheet | Pełna specyfikacja MOS 6821 (kompatybilny z 6820) | [PDF](https://www.wdc65xx.com/wdc/documentation/w65c21.pdf) |
| 6821 PIA Datasheet | Podsumowanie rejestrów i timingów | [Strona](https://tomsheet.b-cdn.net/posts/6821-pia-datasheet/) |
| Rulbus PIA | Dokumentacja programowa | [Strona](https://secure.eld.leidenuniv.nl/~moene/software/rdl/html/group__rdl__pia.html) |

### **10.2 Apple-1 i WOZ Monitor**
| Źródło | Opis | Link |
|--------|------|------|
| WOZMON Analysis | Analiza kodu WOZ Monitor | [Blog](https://www.steckschwein.de/post/wozmon-a-memory-monitor-in-256-bytes/) |
| WOZ Monitor Source | Oryginalny listing (GitHub) | [Kod](https://github.com/jefftranter/6502/blob/master/asm/wozmon/wozmon.s) |
| Apple-1 Block Diagram | Schemat połączeń | [SB-Projects](https://www.sbprojects.net/projects/apple1/a1block.php) |
| Apple-1 Manual | Oficjalna dokumentacja | [PDF](http://retro.hansotten.nl/uploads/apple1/A_ONE%20manual%2011.pdf) |
| Apple-1 Mini | Projekt repliki | [Hackaday](https://hackaday.io/project/26234-apple-1-mini) |

### **10.3 PET i Keyboard Matrix**
| Źródło | Opis | Link |
|--------|------|------|
| PET Keyboard Matrix | Dokumentacja matrycy klawiatury | [Z80.eu](https://www.z80.eu/pet.html) |
| Commodore PET Schematics | Schematy PET | [Z80.eu](http://www.z80.eu/pet.html) |

### **10.4 Retrocomputing Q&A**
| Źródło | Opis | Link |
|--------|------|------|
| 6820 vs 6821 vs 6520 | Różnice między układami PIA | [Retrocomputing SE](https://retrocomputing.stackexchange.com/questions/24709/) |

---
---
## **11. Podsumowanie i Kolejne Kroki**
### **11.1 Co jest gotowe?**
✅ **Pełna specyfikacja techniczna** (PIA, Apple-1, WOZ Monitor).
✅ **Architektura i interfejsy** (`Mos682xPiaDevice`, `ITerminalLink`, `IPiaPortBinding`).
✅ **Plan testów jednostkowych** (9 przypadków dla PIA + 4 dla bindingów).
✅ **Harmonogram i kryteria akceptacji**.

### **11.2 Blokery**
❌ **Fazy 24-27 nie są zaimplementowane** (abstrakcje runtime, bus, profile, terminal).
❌ **Brak implementacji `ITerminalLink` i `BufferedTerminalLink`**.

### **11.3 Kolejne kroki**
1. **Zaimplementować fazy 24-27** (priorytet krytyczny).
2. **Stworzyć `ITerminalLink` i `BufferedTerminalLink`** (2 godziny).
3. **Zaimplementować `Mos682xPiaDevice`** (4-6 godzin).
4. **Dodać binding `apple-1-terminal`** (2 godziny).
5. **Napisać testy jednostkowe** (3 godziny).
6. **Zintegrować z loaderem profilu** (2 godziny).
7. **Uruchomić Apple-1 z WOZ Monitor** i zweryfikować działanie.

---
**Czy chcesz, abym:**
- [ ] **Wygenerował szkielet kodu dla `Mos682xPiaDevice` i bindingów?**
- [ ] **Stworzył przykładowe testy jednostkowe w formie pliku `.cs`?**
- [ ] **Dostarczył implementację `ITerminalLink` i `BufferedTerminalLink`?**
- [ ] **Omówił szczegóły faz 24-27 (abstrakcje runtime, bus, profile)?**