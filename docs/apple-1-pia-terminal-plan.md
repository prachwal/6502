# Apple-1 PIA Terminal Device - plan implementacji

## Status

Repozytorium nie zawiera jeszcze kompletnej implementacji PIA dla Apple-1. Aktualny kierunek nie zaklada jednorazowego adaptera Apple-1 jako glownego urzadzenia. Wlasciwa implementacja powinna zaczac sie od srednio-dokladnego, parametryzowanego `Mos682xPiaDevice`, ktory Apple-1 wykorzysta przez preset/binding terminalowy mapowany na `$D010-$D013`.

## Cel

Celem jest uruchomienie Apple-1 WOZ Monitor z poprawna obsluga klawiatury i wyjscia znakowego, ale bez zamykania architektury na Apple-1. Ten sam rdzen PIA powinien byc mozliwy do uzycia w profilach PET-like, SBC i innych komputerach z inna adresacja oraz innymi bindingami portow.

## Zakres adresow Apple-1

| Adres | Nazwa | Kierunek | Znaczenie |
|---|---:|---|---|
| `$D010` | `KBD` | read | Dane klawiatury. Znak z ustawionym bitem 7, gdy znak jest gotowy. |
| `$D011` | `KBDCR` | read/write | Rejestr kontrolny klawiatury/status gotowosci. |
| `$D012` | `DSP` | write | Dane wyswietlacza. Zapisany znak trafia do terminala. |
| `$D013` | `DSPCR` | read/write | Rejestr kontrolny wyswietlacza/status gotowosci. |

## Wariant medium reusable

Dodać generyczne urządzenie PIA:

```csharp
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    public uint StartAddress { get; }
    public uint Size => 4;

    public byte ReadMemory(uint address);
    public void WriteMemory(uint address, byte value);
    public void Reset();
}
```

Logika wymagana w wersji medium:

- `ORA/ORB` i `DDRA/DDRB`,
- `CRA/CRB` w zakresie wyboru DDR/data register przez bit 2,
- mieszanie odczytu portu: `(outputLatch & ddr) | (externalInput & ~ddr)`,
- callbacki/bindingi pinow zewnetrznych przez `IPiaPortBinding`,
- konfigurowalny layout rejestrow i adres bazowy,
- minimalne IRQ jako `ICpuSignalSource`,
- preset `apple-1-terminal`,
- drugi binding PET-like z inna adresacja, aby potwierdzic reuse.

Nie wymagamy w tej fazie pelnego handshake CA2/CB2 ani dokladnosci tranzystorowej.

## Terminal/link bajtowy

Aby nie wiazac PIA z konkretnym frontendem, terminal powinien byc neutralnym linkiem bajtowym:

```csharp
public interface ITerminalLink
{
    bool HasInput { get; }
    bool TryReadByte(out byte value);
    void WriteByte(byte value);
}
```

Implementacje frontendu moga mapowac ten interfejs na:

- TUI / terminal konsolowy,
- WPF,
- Avalonia,
- Blazor,
- testowy bufor wejscia/wyjscia.

## Testy jednostkowe

Przypadki wymagane dla PIA i presetow:

1. `WriteDdra_WhenCraSelectsDdr_StoresDirection`
2. `WritePortA_WhenCraSelectsData_UpdatesOutputLatch`
3. `ReadPortA_MergesOutputAndExternalInput`
4. `Device_WithBaseD010_MapsApple1Offsets`
5. `Device_WithDifferentBase_MapsSameRegisters`
6. `Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet`
7. `Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus`
8. `Apple1Preset_WriteDsp_StripsHighBitBeforeOutput`
9. `PetLikeBinding_UsesSamePiaWithDifferentAddress`

Przykladowy kierunek testu:

```csharp
[Test]
public void Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var value = device.ReadMemory(0xD010);

    Assert.That(value, Is.EqualTo(0xC1));
}
```

## Integracja z profilem Apple-1

Profil Apple-1 powinien zawierac generyczna PIA mapowana na `$D010-$D013`.

Przykladowy wpis koncepcyjny:

```json
{
  "id": "pia0",
  "type": "mos6821-pia",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0xD010",
    "size": "0x0004"
  },
  "preset": "apple-1-terminal"
}
```

Loader profilu powinien utworzyc `Mos682xPiaDevice` na podstawie tego wpisu i podlaczyc go do magistrali pamieci. Preset `apple-1-terminal` konfiguruje bindingi, ale nie zmienia rdzenia PIA w urzadzenie jednorazowe.

## Reuse poza Apple-1

PIA musi przejsc test drugiego profilu walidacyjnego:

- inny adres bazowy niz `$D010`,
- inny binding portow,
- brak zaleznosci od WOZ Monitor,
- brak klas lub warunkow `if Apple1` w rdzeniu PIA.

Najblizszy drugi scenariusz to PET-like keyboard matrix, opisany w fazie 30.

## Kolejnosc prac

1. Dodać uniwersalne abstrakcje runtime i bus.
2. Dodać `ITerminalLink` oraz buforowany terminal testowy.
3. Dodać `Mos682xPiaDevice` medium implementation.
4. Dodać preset `apple-1-terminal`.
5. Dodać drugi binding PET-like, aby potwierdzic reuse.
6. Podlaczyc PIA do loadera profilu.
7. Uzupelnic `profiles/computers/apple-1.json` o urzadzenie `mos6821-pia` z presetem `apple-1-terminal`.
8. Uruchomic Apple-1 z WOZ Monitor i zweryfikowac prompt oraz echo znakow.

## Fazy implementacyjne

Pelna implementacja Apple-1 wymaga najpierw warstwy skladania komputerow i reusable PIA. Szczegolowe fazy sa opisane w osobnych plikach:

| Faza | Dokument | Zakres | Status |
|---:|---|---|---|
| 24 | [`faza-24-runtime-abstractions.md`](faza-24-runtime-abstractions.md) | Uniwersalne abstrakcje runtime dla wielu CPU | ✅ Zaimplementowana |
| 25 | [`faza-25-system-bus-memory-map.md`](faza-25-system-bus-memory-map.md) | `RuntimeBus`, memory map, port map, szybki routing | ✅ Zaimplementowana |
| 26 | [`faza-26-computer-profiles.md`](faza-26-computer-profiles.md) | Profile JSON, loader, `ComputerBuilder`, rejestr fabryk | ✅ Zaimplementowana |
| 27 | [`faza-27-terminal-abstractions.md`](faza-27-terminal-abstractions.md) | Terminal/link bajtowy niezalezny od frontendu | ✅ Zaimplementowana |
| 28 | [`faza-28-mos682x-pia-medium.md`](faza-28-mos682x-pia-medium.md) | Generyczny MOS 6820/6821 PIA reusable dla Apple-1/PET/SBC | [ ] Nie rozpoczęte |
| 29 | [`faza-29-apple1-profile-wozmon.md`](faza-29-apple1-profile-wozmon.md) | Apple-1 jako profil na generycznej PIA | [ ] Nie rozpoczęte |
| 30 | [`faza-30-pet-ready-pia-bindings.md`](faza-30-pet-ready-pia-bindings.md) | PET-ready bindingi PIA i drugi profil walidacyjny | [ ] Nie rozpoczęte |
| 31 | [`faza-31-apple1-runtime-api.md`](faza-31-apple1-runtime-api.md) | Publiczne API uruchamiania Apple-1 i test end-to-end |
| 32 | [`faza-32-cross-architecture-smoke-profiles.md`](faza-32-cross-architecture-smoke-profiles.md) | Profile smoke dla wielu architektur, port-mapped I/O |

## Kryteria akceptacji

- Apple-1 startuje bez bledu mapowania urzadzen.
- WOZ Monitor moze odczytac znak z klawiatury przez `$D010/$D011`.
- WOZ Monitor moze wypisac znak przez `$D012/$D013`.
- Testy jednostkowe pokrywaja srednia semantyke PIA oraz preset Apple-1.
- Ten sam rdzen PIA jest uzyty w drugim profilu walidacyjnym z inna adresacja.
- Implementacja nie zalezy bezposrednio od konkretnego frontendu.

---

## Suplement — Zebrane informacje techniczne

### 1. MOS 6820/6821 PIA — Specyfikacja techniczna

#### 1.1 Rejestry i ich funkcje

| Rejestr | Adres (offset) | Opis |
|---|---|---|
| **ORA** | base+0 (jeśli CRA.2=1) | Output Register A — latch wyjściowy portu A |
| **DDRA** | base+0 (jeśli CRA.2=0) | Data Direction Register A — kierunek pinów PA0-PA7 (1=output, 0=input) |
| **CRA** | base+1 | Control Register A — kontrola CA1/CA2, IRQ, bit 2: 0=DDRA, 1=ORA |
| **ORB** | base+2 (jeśli CRB.2=1) | Output Register B — latch wyjściowy portu B |
| **DDRB** | base+2 (jeśli CRB.2=0) | Data Direction Register B — kierunek pinów PB0-PB7 |
| **CRB** | base+3 | Control Register B — kontrola CB1/CB2, IRQ, bit 2: 0=DDRB, 1=ORB |

#### 1.2 Zachowanie odczytu portu (mieszanie)

```
Wartość odczytana z portu = (outputLatch & ddrMask) | (externalInput & ~ddrMask)
```

- Bity skonfigurowane jako **output** (DDR=1) zwracają wartość z **output latch** (ORA/ORB)
- Bity skonfigurowane jako **input** (DDR=0) zwracają wartość z **zewnętrznych pinów**

#### 1.3 Linie kontrolne CA1/CA2, CB1/CB2

| Linia | Tryb (CRA/CRB) | Funkcja |
|---|---|---|
| CA1 | Input | Interrupt na zboczu (konfigurowalne: opadające/wstępujące) |
| CA2 | Input/Output | Output: sterowany przez CRA.3 (0=low, 1=high); Input: interrupt na zboczu |
| CB1 | Input | Interrupt na zboczu (konfigurowalne) |
| CB2 | Input/Output | Output: sterowany przez CRB.3; Input: interrupt na zboczu |

- **IRQA1/IRQA2** (CRA.0, CRA.4) i **IRQB1/IRQB2** (CRB.0, CRB.4) — flagi przerwań
- **Bit 7 ORA/ORB** — nie jest używany jako flag status w Apple-1 (do ustalenia w bindingu)

#### 1.4 Inicjalizacja PIA (WOZ Monitor Apple-1)

```
DSP (Port B) DDR: $7F (bity 0-6 = output, bit 7 = input)
KBD (Port A) DDR: niejawnie input (DDR=0)
KBDCR (CRA): $A7 = 1010 0111
  - bit 7: nie używany
  - bit 6: nie używany  
  - bit 5: nie używany
  - bit 4: IRQA2 flag
  - bit 3: CA2 control
  - bit 2: 0 (DDRA selected at base+0) — UWAGA: w Apple-1 $D010 to ORA, nie DDRA
  - bit 1: CA1 control (positive edge)
  - bit 0: IRQA1 flag
DSPCR (CRB): $A7 = 1010 0111
  - bit 2: 0 (DDRB selected at base+2)
```

**UWAGA IMPLEMENTACYJNA**: W Apple-1 $D010 mapuje się na **ORA** (nie DDRA). Oznacza to, że CRA.2 = 1. 
Preset `apple-1-terminal` musi ustawić CRA.2=1 i CRB.2=1, aby adresy $D010 i $D012 mapowały się na ORA/ORB.

---

### 2. Apple-1 PIA Binding — Połączenia i adresacja

#### 2.1 Mapowanie adresów

| Adres | Rejestr PIA | Funkcja Apple-1 | Kierunek |
|---|---|---|---|
| `$D010` | ORA (Port A Data) | **KBD** — dane klawiatury | Read |
| `$D011` | CRA (Port A Control) | **KBDCR** — status/kontrola klawiatury | Read/Write |
| `$D012` | ORB (Port B Data) | **DSP** — dane wyświetlacza | Write |
| `$D013` | CRB (Port B Control) | **DSPCR** — status/kontrola wyświetlacza | Read/Write |

#### 2.2 Zachowanie terminala

**Odczyt klawiatury (KBD/$D010):**
- Gdy znak jest dostępny: **bit 7 = 1**, bity 0-6 = ASCII (uppercase)
- WOZ Monitor czeka na `KBDCR.7 == 1`, potem czyta `KBD`
- Odczyt `KBD` **nie czyści flagi gotowości** — to robi zapis do KBDCR

**Status klawiatury (KBDCR/$D011):**
- Bit 7: **1 = znak gotowy** (ustawiany przez binding terminalowy przy dostępności wejścia)
- Bit 6: nie używany w Apple-1
- WOZ Monitor inicjalizuje: `STA #$A7` → CRA = $A7

**Zapis wyświetlacza (DSP/$D012):**
- Zapisany bajt (bity 0-6) trafia do terminala
- **Bit 7 jest ignorowany** (DSP DDR = $7F → bit 7 = input)
- WOZ Monitor pisze: `STA DSP` (po sprawdzeniu DSPCR.7)

**Status wyświetlacza (DSPCR/$D013):**
- Bit 7: **1 = gotowy na kolejny znak** (ustawiany przez binding po wyświetleniu)
- WOZ Monitor inicjalizuje: `STA #$A7` → CRB = $A7

#### 2.3 Sekwencja WOZ Monitor

```
Pętla wejścia:
1. Czekaj: LDA KBDCR / BPL (pętla jeśli bit 7 = 0)
2. Odczyt: LDA KBD (pobiera znak z bit 7 = 1)
3. Przetworzenie znaku...

Pętla wyjścia (ECHO):
1. Czekaj: LDA DSP / BPL (czeka na gotowość - bit 7 DSP)
   UWAGA: WOZ Monitor używa BIT DSP / BPL, co oznacza czekanie na bit 7 = 0!
   Poprawka: W oryginale BIT DSP sprawdza bit 7 ORB, który jest ustawiany przez terminal
2. Zapis: STA DSP (zapisuje znak)
```

**Korekta implementacyjna**: WOZ Monitor używa `BIT DSP` (czyli sprawdza ORB bit 7). 
Binding terminalowy musi ustawić **ORB.7 = 0** gdy terminal gotowy, i **ORB.7 = 1** gdy zajęty.
To jest odwrotność typowego flagi "gotowy"!

**Decyzja**: 
- `KBD` (ORA): bit 7 = 1 gdy znak dostępny (ustawiany przez terminal link)
- `DSP` (ORB): bit 7 = 0 gdy terminal gotowy (ustawiany przez terminal link)

---

### 3. WOZ Monitor — Kluczowe fragmenty kodu

```assembly
KBD     = $D010       ; PIA.A keyboard input
KBDCR   = $D011       ; PIA.A keyboard control register  
DSP     = $D012       ; PIA.B display output register
DSPCR   = $D013       ; PIA.B display control register

RESET:
    CLD
    CLI
    LDY #$7F
    STY DSP           ; DDRB = $7F (Port B: bity 0-6 = output)
    LDA #$A7
    STA KBDCR         ; CRA = $A7
    STA DSPCR         ; CRB = $A7

NEXTCHAR:
    LDA KBDCR         ; Czekaj na znak
    BPL NEXTCHAR      ; Bit 7 KBDCR? (WOZ używa KBDCR.7, nie KBD.7!)
    LDA KBD           ; Pobierz znak
    ...

ECHO:
    BIT DSP           ; Czekaj na DSP gotowy
    BPL ECHO          ; Pętla jeśli ORB.7 = 1 (zajęty)
    STA DSP           ; Wyślij znak (bity 0-6)
    ...
```

**WAŻNE**: WOZ Monitor sprawdza **KBDCR.7** (nie KBD.7) w pętli wejścia, i **DSP.7** (ORB.7) w pętli wyjścia!

Binding `apple-1-terminal` musi:
1. Ustawić **KBDCR.7 = 1** gdy terminal ma znak do odczytu
2. Ustawić **ORB.7 = 0** gdy terminal gotowy na zapis
3. **KBD** zwraca znak z bit 7 = 1 (WOZ oczekuje tego formatu)
4. **DSP** zapisuje bity 0-6 do terminala (bit 7 ignorowany)

---

### 4. PET-like Binding — Drugi profil walidacyjny

#### 4.1 Cel
Udowodnić, że `Mos682xPiaDevice` **nie jest hardcoded pod Apple-1**.

#### 4.2 Wymagania

| Właściwość | Wartość |
|---|---|
| Adres bazowy | **$E810-$E813** (albo dowolny inny niż $D010) |
| Layout rejestrów | Może być inny niż Apple-1 (np. DDRA/DDRB na innych offsetach) |
| Binding portów | `PiaKeyboardMatrixBinding` (keyboard matrix 8x8) |
| Zależności | **Brak** zaleźności od WOZ Monitor, Apple-1 ROM, czy klas `Apple1*` |

#### 4.3 Komponenty do zaimplementowania

```csharp
// Neutralny model matrycy
public sealed class KeyboardMatrix
{
    public void SetKey(int row, int column, bool pressed);
    public byte ReadRows(byte selectedColumnsMask); // Aktywne zera/jedynki jako parametr
}

// Binding PIA do matrycy
public sealed class PiaKeyboardMatrixBinding : IPiaPortBinding
{
    public byte ReadPins();
    public void WritePins(byte value, byte directionMask);
}
```

#### 4.4 Testy walidacyjne

1. `PetLikeProfile_MapsPiaAtE810` — inny adres bazowy
2. `Mos682xPia_SameDeviceSupportsApple1AndPetLike` — ta sama klasa PIA
3. `KeyboardMatrix_Binding_ReadsPressedKey` — matryca działa
4. `PetLikeProfile_DoesNotRequireWozMonitor` — brak zależności

---

### 5. Status zależności (Fazy 24-27)

| Faza | Plik | Status | Uwagi |
|---|---|---|---|
| 24 | `faza-24-runtime-abstractions.md` | ✅ Zaimplementowana | `ICpuCore`, `CpuSignal`, `IResettableDevice`, `ICpuSignalSource` |
| 25 | `faza-25-system-bus-memory-map.md` | ✅ Zaimplementowana | `ISystemBus`, `IMemoryMappedDevice`, `CompiledMemoryMap` |
| 26 | `faza-26-computer-profiles.md` | ✅ Zaimplementowana | `ComputerProfile`, `ComputerBuilder`, `DeviceFactoryRegistry` |
| 27 | `faza-27-terminal-abstractions.md` | ✅ Zaimplementowana | `ITerminalLink`, `BufferedTerminalLink`, `TerminalTextEncoding` |

✅ **Wszystkie zależności spełnione** - Faza 28 (PIA) może zostać rozpoczęta.

#### 5.1 Zależności fazy 28

- **Faza 24**: `ICpuSignalSource` — PIA musi implementować ten interfejs (IRQ) ✅
- **Faza 25**: `IMemoryMappedDevice` — PIA implementuje ten interfejs ✅
- **Faza 26**: `DeviceFactoryRegistry` — rejestracja fabryki PIA ✅
- **Faza 27**: `ITerminalLink` — binding terminalowy używa tego interfejsu ✅

---

### 6. Checklista — Co zebrać przed implementacją

#### 6.1 Dokumentacja techniczna (priorytet: krytyczny)

- [ ] **MOS 6820/6821 datasheet** — pełna specyfikacja rejestrów i timingów
  - Źródła: [W65C21 Datasheet (WDC)](https://www.wdc65xx.com/wdc/documentation/w65c21.pdf), [6821 PIA Datasheet](https://tomsheet.b-cdn.net/posts/6821-pia-datasheet/)
- [ ] **Apple-1 schemat** — połączenia PIA z klawiatura/terminalem
  - Źródła: [Apple-1 Block Diagram](https://www.sbprojects.net/projects/apple1/a1block.php), [Apple-1 Mini](https://hackaday.io/project/26234-apple-1-mini)
- [ ] **WOZ Monitor listing** — oryginalny kod źródłowy
  - Źródła: [GitHub jefftranter/6502](https://github.com/jefftranter/6502/blob/master/asm/wozmon/wozmon.s), [WOZMON analysis](https://www.steckschwein.de/post/wozmon-a-memory-monitor-in-256-bytes/)

#### 6.2 Dokumentacja projektowa (priorytet: wysoki)

- [x] Faza 24: Abstrakcje runtime
- [x] Faza 25: System bus i memory map
- [x] Faza 26: Profile komputerów
- [x] Faza 27: Abstrakcje terminala
- [x] Faza 28: PIA medium implementation
- [x] Faza 29: Apple-1 profil
- [x] Faza 30: PET-like bindings
- [x] Faza 31: Apple-1 runtime API

#### 6.3 Decyzje implementacyjne (do podjęcia)

1. **CRA/CRB bit 2 interpretacja**:
   - Apple-1: $D010 = ORA, $D012 = ORB → CRA.2 = 1, CRB.2 = 1
   - Czy zezwolić na inne layouty? (TAK — przez `PiaRegisterLayout`)

2. **KBD bit 7**:
   - WOZ Monitor oczekuje bit 7 = 1 dla gotowego znaku
   - Czy ustawiać w PIA, czy w bindingu terminalowym? (**W bindingu** — PIA nie zna semantyki Apple-1)

3. **DSP bit 7**:
   - WOZ Monitor sprawdza ORB.7 = 0 (gotowy) vs 1 (zajęty)
   - Czy inwertować w bindingu? (**TAK** — binding ustawia ORB.7 = 0 gdy gotowy)

4. **IRQ**:
   - Apple-1 WOZ Monitor nie używa IRQ od PIA
   - Czy implementować minimalne IRQ? (**TAK** — dla przyszłych profili, ale nie wymagane w MVP)

---

### 7. Źródła i referencje

#### 7.1 Datasheet PIA
- [W65C21 Datasheet — Western Design Center](https://www.wdc65xx.com/wdc/documentation/w65c21.pdf)
- [W65C21S PIA Datasheet](https://www.westerndesigncenter.com/wdc/documentation/w65c21s.pdf)
- [6821 PIA Datasheet Summary](https://tomsheet.b-cdn.net/posts/6821-pia-datasheet/)
- [Rulbus Device Library — PIA](https://secure.eld.leidenuniv.nl/~moene/software/rdl/html/group__rdl__pia.html)

#### 7.2 Apple-1 i WOZ Monitor
- [WOZMON — A Memory Monitor in 256 Bytes](https://www.steckschwein.de/post/wozmon-a-memory-monitor-in-256-bytes/)
- [jefftranter/6502 — WOZ Monitor Source](https://github.com/jefftranter/6502/blob/master/asm/wozmon/wozmon.s)
- [Apple 1 Monitor — PreterHuman Wiki](https://wiki.preterhuman.net/Apple_1_Monitor)
- [Apple-1 Block Diagram — SB-Projects](https://www.sbprojects.net/projects/apple1/a1block.php)
- [Apple-1 Manual PDF](http://retro.hansotten.nl/uploads/apple1/A_ONE%20manual%2011.pdf)
- [Dave Cheney — Make your own Apple 1 replica](https://dave.cheney.net/2014/12/26/make-your-own-apple-1-replica)

#### 7.3 PET i keyboard matrix
- [PET Keyboard Matrix Documentation](https://www.z80.eu/pet.html)
- [Commodore PET Schematics](http://www.z80.eu/pet.html)

#### 7.4 Retrocomputing Q&A
- [Retrocomputing SE: 6820 vs 6821 vs 6520 Register Mapping](https://retrocomputing.stackexchange.com/questions/24709/)

---

## 8. Implementacja — Poprawiona wersja bindingów

### 8.1 Problem z oryginalnym bindingiem

Oryginalna wersja `Apple1TerminalBinding` (z `informacje.md`) **nie obsługuje flag gotowości** w CRA.7 i CRB.7:

| Rejestr | Bit | Oczekiwana semantyka | Problem |
|---|---|---|---|
| **CRA** ($D011) | 7 | 1 = znak gotowy do odczytu | Binding nie ustawia CRA.7 |
| **CRB** ($D013) | 7 | 0 = terminal gotowy na zapis | Binding nie ustawia CRB.7 |
| **ORB** ($D012) | 7 | 0 = terminal gotowy (WOZ sprawdza `BIT DSP`) | Binding nie zeruje ORB.7 po zapisie |

WOZ Monitor sprawdza:
- `LDA KBDCR / BPL` → czeka na **CRA.7 = 1** (znak gotowy)
- `BIT DSP / BPL` → czeka na **ORB.7 = 0** (gotowy na zapis)

---

### 8.2 Rozwiązanie — `IPiaPortBinding` z flagami statusu

**Zmiana interfejsu**: Dodaj właściwości statusowe do `IPiaPortBinding`:

```csharp
public interface IPiaPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte directionMask);
    
    // Nowe właściwości dla flag statusowych
    bool HasInputReady { get; }      // Czy terminal ma znak? → CRA.7 = 1
    bool IsOutputReady { get; }      // Czy terminal gotowy na zapis? → ORB.7 = 0
}
```

**Zmiana w `Mos682xPiaDevice`**: Odpytywanie bindingów podczas odczytu CRA/CRB:

```csharp
public byte ReadMemory(uint address)
{
    var offset = address - StartAddress;
    return offset switch
    {
        0 => ReadPortDataA(),
        1 => ReadControlRegisterA(),  // ← Uwzględnia HasInputReady
        2 => ReadPortDataB(),
        3 => ReadControlRegisterB(),  // ← Uwzględnia IsOutputReady
        _ => throw new InvalidOperationException()
    };
}

private byte ReadControlRegisterA()
{
    // Bazowa wartość CRA
    byte cra = _cra;
    
    // Ustaw bit 7 jeśli terminal ma znak gotowy
    if (_portABinding.HasInputReady)
        cra |= 0x80;
    else
        cra &= 0x7F;
    
    return cra;
}

private byte ReadControlRegisterB()
{
    // Bazowa wartość CRB
    byte crb = _crb;
    
    // Wyczyść bit 7 jeśli terminal gotowy (WOZ oczekuje 0 = gotowy)
    if (_portBBinding.IsOutputReady)
        crb &= 0x7F;
    else
        crb |= 0x80;
    
    return crb;
}
```

---

### 8.3 Poprawiona implementacja `Apple1TerminalBinding`

```csharp
/// <summary>
/// Binding terminalowy dla Apple-1 PIA.
/// Obsługuje:
/// - ORA (KBD): bit 7 = 1 gdy znak dostępny
/// - CRA (KBDCR): bit 7 = 1 gdy terminal ma znak (HasInputReady)
/// - ORB (DSP): bity 0-6 do terminala, bit 7 ignorowany
/// - CRB (DSPCR): bit 7 = 0 gdy terminal gotowy (IsOutputReady)
/// </summary>
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    
    public Apple1TerminalBinding(ITerminalLink terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }
    
    /// <summary>
    /// Czy terminal ma znak gotowy do odczytu?
    /// Ustawia CRA.7 = 1 gdy true.
    /// </summary>
    public bool HasInputReady => _terminal.HasInput;
    
    /// <summary>
    /// Czy terminal jest gotowy na kolejny znak?
    /// Ustawia ORB.7 = 0 (i CRB.7 = 0) gdy true.
    /// WOZ Monitor oczekuje ORB.7 = 0 (gotowy) i pętli na BPL (bit 7 = 0).
    /// </summary>
    public bool IsOutputReady => true; // Terminal zawsze gotowy (buforowany)
    
    /// <summary>
    /// Odczyt pinów portu (dla input).
    /// Gdy znak dostępny: zwraca (znak | 0x80) — bit 7 = 1.
    /// </summary>
    public byte ReadPins()
    {
        if (_terminal.HasInput && _terminal.TryReadByte(out byte value))
        {
            // Ustaw bit 7 = 1 (znak gotowy)
            return (byte)(value | 0x80);
        }
        return 0; // Brak znaku
    }
    
    /// <summary>
    /// Zapis do pinów portu (dla output).
    /// Pisze bity 0-6 do terminala, ignoruje bit 7 (DDRB = 0x7F).
    /// Po zapisie terminal jest gotowy na kolejny znak (IsOutputReady = true).
    /// </summary>
    public void WritePins(byte value, byte directionMask)
    {
        // Tylko bity 0-6 są output (DDRB = 0x7F)
        byte outputValue = (byte)(value & 0x7F);
        
        if (outputValue != 0 || directionMask != 0)
        {
            _terminal.WriteByte(outputValue);
            // Terminal buforowany — zawsze gotowy po zapisie
            // ORB.7 = 0 jest ustawiane przez Mos682xPiaDevice.ReadControlRegisterB()
        }
        // Bit 7 = input — ignorowany
    }
}
```

---

### 8.4 Integracja z `Mos682xPiaDevice`

```csharp
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    // ... istniejące pola ...
    
    private byte _cra;
    private byte _crb;
    private readonly IPiaPortBinding _portABinding;
    private readonly IPiaPortBinding _portBBinding;
    
    public Mos682xPiaDevice(uint baseAddress, IPiaPortBinding portA, IPiaPortBinding portB)
    {
        StartAddress = baseAddress;
        _portABinding = portA;
        _portBBinding = portB;
        // ...
    }
    
    public byte ReadMemory(uint address)
    {
        var offset = address - StartAddress;
        return offset switch
        {
            0 => ReadPortDataA(),
            1 => ReadControlRegisterA(),
            2 => ReadPortDataB(),
            3 => ReadControlRegisterB(),
            _ => throw new InvalidOperationException()
        };
    }
    
    private byte ReadPortDataA()
    {
        // CRA.2 decyduje: 0=DDRA, 1=ORA
        if ((_cra & 0x04) == 0)
            return _ddra; // DDRA
        else
            return (byte)((_outputLatchA & _ddra) | (_portABinding.ReadPins() & ~_ddra));
    }
    
    private byte ReadControlRegisterA()
    {
        byte cra = _cra;
        
        // Ustaw bit 7 jeśli binding portu A ma znak gotowy
        if (_portABinding.HasInputReady)
            cra |= 0x80;
        else
            cra &= 0x7F;
        
        return cra;
    }
    
    private byte ReadPortDataB()
    {
        // CRB.2 decyduje: 0=DDRB, 1=ORB
        if ((_crb & 0x04) == 0)
            return _ddrb; // DDRB
        else
            return (byte)((_outputLatchB & _ddrb) | (_portBBinding.ReadPins() & ~_ddrb));
    }
    
    private byte ReadControlRegisterB()
    {
        byte crb = _crb;
        
        // WOZ Monitor sprawdza ORB.7 (DSP.7) przez BIT DSP / BPL
        // BPL = branch if N=0, czyli bit 7 = 0
        // Dlatego ORB.7 = 0 oznacza "gotowy"
        // Binding.IsOutputReady = true → terminal gotowy → ORB.7 = 0
        if (_portBBinding.IsOutputReady)
            crb &= 0x7F; // Wyczyść bit 7 (gotowy)
        else
            crb |= 0x80; // Ustaw bit 7 (zajęty)
        
        return crb;
    }
    
    public void WriteMemory(uint address, byte value)
    {
        var offset = address - StartAddress;
        switch (offset)
        {
            case 0:
                if ((_cra & 0x04) == 0) _ddra = value; // CRA.2=0 → DDRA
                else _outputLatchA = value;            // CRA.2=1 → ORA
                break;
            case 1: _cra = value; break;
            case 2:
                if ((_crb & 0x04) == 0) _ddrb = value; // CRB.2=0 → DDRB
                else _outputLatchB = value;            // CRB.2=1 → ORB
                break;
            case 3: _crb = value; break;
        }
        
        // Po zapisie do ORB: nie Musimy tu nic robić — IsOutputReady jest zarządzane przez binding
        // Po odczycie z ORA: nie Musimy czyscic flagi — binding zarządza HasInputReady
    }
    
    public void Reset()
    {
        _ora = _orb = 0;
        _ddra = _ddrb = 0;
        _cra = _crb = 0;
        _outputLatchA = _outputLatchB = 0;
    }
    
    // ICpuSignalSource
    public bool IsAsserted(CpuSignal signal)
    {
        if (signal == CpuSignal.Irq)
        {
            // Minimalna obsługa IRQ — sprawdź flagi IRQA/IRQB
            bool irqA = (_cra & 0x80) != 0; // IRQA1
            bool irqB = (_crb & 0x80) != 0; // IRQB1
            return irqA || irqB;
        }
        return false;
    }
}
```

---

### 8.5 Fabryka z poprawną inicjalizacją (WOZ Monitor)

```csharp
public static class Mos682xPiaDeviceFactory
{
    /// <summary>
    /// Tworzy PIA skonfigurowane dla Apple-1 Terminal.
    /// Inicjalizuje rejestry zgodnie z WOZ Monitor:
    /// - DDRB = $7F (Port B: bity 0-6 = output, bit 7 = input)
    /// - CRA = $A7 (bit 2=1 → ORA na offset 0, CA1 positive edge)
    /// - CRB = $A7 (bit 2=1 → ORB na offset 2, CB1 positive edge)
    /// </summary>
    public static Mos682xPiaDevice CreateApple1Terminal(uint baseAddress, ITerminalLink terminal)
    {
        var portABinding = new Apple1TerminalBinding(terminal); // KBD (Port A)
        var portBBinding = new Apple1TerminalBinding(terminal); // DSP (Port B)
        
        var device = new Mos682xPiaDevice(baseAddress, portABinding, portBBinding);
        
        // Inicjalizacja WOZ Monitor
        // UWAGA: WOZ pisze do DSP (offset 2) wartość $7F → to DDRB!
        // Ponieważ CRB.2 = 1 (domyślnie 0), trzeba najpierw ustawić CRB.2 = 0
        device.WriteMemory(baseAddress + 3, 0x00); // CRB = 0 (bit 2=0 → DDRB na offset 2)
        device.WriteMemory(baseAddress + 2, 0x7F); // DDRB = $7F
        
        // Teraz ustaw CRB.2 = 1 (ORB na offset 2)
        device.WriteMemory(baseAddress + 3, 0xA7); // CRB = $A7
        
        // Ustaw CRA.2 = 1 (ORA na offset 0) i inne bity
        device.WriteMemory(baseAddress + 1, 0xA7); // CRA = $A7
        
        return device;
    }
}
```

---

## 9. Testy jednostkowe — Brakujące przypadki

### 9.1 Testy dla flag statusowych (CRA.7 / CRB.7)

```csharp
// Test 1: KBDCR.7 = 1 gdy terminal ma znak
[Test]
public void Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var kbdCr = device.ReadMemory(0xD011); // CRA
    Assert.That(kbdCr & 0x80, Is.EqualTo(0x80)); // Bit 7 = 1 (gotowy)
}

// Test 2: KBDCR.7 = 0 gdy terminal nie ma znaku
[Test]
public void Apple1Preset_ReadKbdCr_WhenNoInput_ReturnsNotReady()
{
    var terminal = new BufferedTerminalLink();
    // Nie wkładamy żadnego znaku
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var kbdCr = device.ReadMemory(0xD011); // CRA
    Assert.That(kbdCr & 0x80, Is.EqualTo(0x00)); // Bit 7 = 0 (nie gotowy)
}

// Test 3: DSPCR.7 = 0 (ORB.7 = 0) gdy terminal gotowy
[Test]
public void Apple1Preset_ReadDspCr_WhenReady_ReturnsZeroInBit7()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var dspCr = device.ReadMemory(0xD013); // CRB
    Assert.That(dspCr & 0x80, Is.EqualTo(0x00)); // Bit 7 = 0 (gotowy)
}

// Test 4: KBD zwraca znak z bit 7 = 1
[Test]
public void Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var value = device.ReadMemory(0xD010); // ORA
    Assert.That(value, Is.EqualTo(0xC1)); // 'A' (0x41) | 0x80 = 0xC1
}

// Test 5: DSP ignoruje bit 7 przy zapisie
[Test]
public void Apple1Preset_WriteDsp_StripsHighBitBeforeOutput()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    device.WriteMemory(0xD012, 0xFF); // DSP (ORB) — bit 7 = 1
    
    // Terminal powinien otrzymać tylko bity 0-6 (0x7F)
    var output = terminal.ReadAllOutputBytes();
    Assert.That(output, Has.Length.EqualTo(1));
    Assert.That(output[0], Is.EqualTo(0x7F));
}

// Test 6: Inny adres bazowy — ta sama klasa PIA działa
[Test]
public void Mos682xPia_WithDifferentBaseAddress_MapsRegistersCorrectly()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xE810, terminal);

    // Ustaw CRA na nowym adresie
    device.WriteMemory(0xE811, 0xA7);
    var cra = device.ReadMemory(0xE811);
    Assert.That(cra, Is.EqualTo(0xA7));
    
    // Sprawdź ORA
    terminal.EnqueueText("X", TerminalTextEncoding.Apple1);
    var ora = device.ReadMemory(0xE810);
    Assert.That(ora, Is.EqualTo(0xD8)); // 'X' (0x58) | 0x80 = 0xD8
}
```

### 9.2 Testy dla mieszania odczytu portu

```csharp
// Test 7: Odczyt portu A z DDR=0xFF (wszystkie output) → zwraca ORA
[Test]
public void ReadPortA_WhenDdraAllOutput_ReturnsOutputLatch()
{
    var device = new Mos682xPiaDevice(0xD010, 
        new NullPiaPortBinding(), new NullPiaPortBinding());
    
    // Ustaw DDRA = 0xFF (wszystkie bity output)
    device.WriteMemory(0xD011, 0x00); // CRA.2 = 0 → DDRA na offset 0
    device.WriteMemory(0xD010, 0xFF); // DDRA = 0xFF
    device.WriteMemory(0xD011, 0x04); // CRA.2 = 1 → ORA na offset 0
    
    // Zapis do ORA
    device.WriteMemory(0xD010, 0xAA); // ORA = 0xAA
    
    // Odczyt z ORA (wszystkie bity output) → powinien zwrócić ORA
    var result = device.ReadMemory(0xD010);
    Assert.That(result, Is.EqualTo(0xAA));
}

// Test 8: Odczyt portu A z DDR=0x00 (wszystkie input) → zwraca external input
[Test]
public void ReadPortA_WhenDdraAllInput_ReturnsExternalInput()
{
    var mockBinding = new MockPiaPortBinding { ExternalInput = 0x55 };
    var device = new Mos682xPiaDevice(0xD010, mockBinding, new NullPiaPortBinding());
    
    // Ustaw DDRA = 0x00 (wszystkie bity input)
    device.WriteMemory(0xD011, 0x00); // CRA.2 = 0 → DDRA na offset 0
    device.WriteMemory(0xD010, 0x00); // DDRA = 0x00
    device.WriteMemory(0xD011, 0x04); // CRA.2 = 1 → ORA na offset 0
    
    // Odczyt z ORA (wszystkie bity input) → powinien zwrócić external input
    var result = device.ReadMemory(0xD010);
    Assert.That(result, Is.EqualTo(0x55));
}

// Test 9: Odczyt portu A z DDR=0xF0 → mieszanie ORA (bity 4-7) i external (bity 0-3)
[Test]
public void ReadPortA_MixesOutputLatchAndExternalInput()
{
    var mockBinding = new MockPiaPortBinding { ExternalInput = 0x0F };
    var device = new Mos682xPiaDevice(0xD010, mockBinding, new NullPiaPortBinding());
    
    // Ustaw DDRA = 0xF0 (bity 4-7 = output, 0-3 = input)
    device.WriteMemory(0xD011, 0x00); // CRA.2 = 0 → DDRA na offset 0
    device.WriteMemory(0xD010, 0xF0); // DDRA = 0xF0
    device.WriteMemory(0xD011, 0x04); // CRA.2 = 1 → ORA na offset 0
    
    // Zapis do ORA
    device.WriteMemory(0xD010, 0xA0); // ORA = 0xA0 (bity 4-7 = 0xA)
    
    // Odczyt: (0xA0 & 0xF0) | (0x0F & 0x0F) = 0xA0 | 0x0F = 0xAF
    var result = device.ReadMemory(0xD010);
    Assert.That(result, Is.EqualTo(0xAF));
}

// Pomocowe klasy testowe
private class NullPiaPortBinding : IPiaPortBinding
{
    public bool HasInputReady => false;
    public bool IsOutputReady => true;
    public byte ReadPins() => 0;
    public void WritePins(byte value, byte directionMask) { }
}

private class MockPiaPortBinding : IPiaPortBinding
{
    public bool HasInputReady => false;
    public bool IsOutputReady => true;
    public byte ExternalInput { get; set; }
    public byte ReadPins() => ExternalInput;
    public void WritePins(byte value, byte directionMask) { }
}
```

### 9.3 Testy integracyjne (WOZ Monitor flow)

```csharp
// Test 10: Symulacja pętli wejścia WOZ Monitor
[Test]
public void Apple1Terminal_SimulateWozInputLoop_WaitsForKbdCrBit7()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    // Symuluj WOZ: LDA KBDCR / BPL (pętla jeśli bit 7 = 0)
    // Na początek: CRA.7 = 0 (nie gotowy)
    var kbdCr = device.ReadMemory(0xD011);
    Assert.That(kbdCr & 0x80, Is.EqualTo(0x00));

    // Wkładamy znak
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);

    // Teraz CRA.7 = 1 (gotowy)
    kbdCr = device.ReadMemory(0xD011);
    Assert.That(kbdCr & 0x80, Is.EqualTo(0x80));

    // WOZ czyta KBD
    var kbd = device.ReadMemory(0xD010);
    Assert.That(kbd, Is.EqualTo(0xC1)); // 'A' | 0x80
}

// Test 11: Symulacja pętli wyjścia WOZ Monitor
[Test]
public void Apple1Terminal_SimulateWozOutputLoop_WaitsForDspBit7()
{
    var terminal = new BufferedTerminalLink();
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    // Symuluj WOZ: BIT DSP / BPL (pętla jeśli bit 7 = 1)
    // Na początek: ORB.7 = 0 (gotowy) → CRB.7 = 0
    var dsp = device.ReadMemory(0xD012); // ORB
    var dspCr = device.ReadMemory(0xD013); // CRB
    Assert.That(dspCr & 0x80, Is.EqualTo(0x00)); // CRB.7 = 0 (gotowy)

    // WOZ pisze znak
    device.WriteMemory(0xD012, 0x41); // 'A'

    // Terminal powinien otrzymać 0x41
    var output = terminal.ReadAllOutputBytes();
    Assert.That(output, Has.Length.EqualTo(1));
    Assert.That(output[0], Is.EqualTo(0x41));

    // CRB.7 powinien być wciąż 0 (gotowy)
    dspCr = device.ReadMemory(0xD013);
    Assert.That(dspCr & 0x80, Is.EqualTo(0x00));
}
```

---

### 9.4 Podsumowanie testów

| Kategoria | Liczba testów | Status |
|---|---|---|
| **Flagi statusowe (CRA.7/CRB.7)** | 5 | ✅ Brakujące w oryginale |
| **Mieszanie odczytu portu** | 3 | ✅ Brakujące w oryginale |
| **Integracja WOZ** | 2 | ✅ Brakujące w oryginale |
| **Podstawowe (z planu)** | 9 | ✅ Już w planie |
| **Razem** | **19** | ✅ Kompletne pokrycie |
