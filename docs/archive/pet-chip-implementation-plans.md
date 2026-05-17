# Plany implementacyjne układów dla Commodore PET

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: roadmapa / plany implementacyjne  
Zakres: układy i urządzenia wymagane do profilu Commodore PET

---

## 1. Cel dokumentu

Ten dokument opisuje plan implementacji układów potrzebnych do emulacji Commodore PET. Układy PET powinny być implementowane jako osobne komponenty maszyny podłączane do magistrali pamięci i sygnałów CPU, a nie jako część rdzenia CPU 6502.

Główna zasada:

- CPU wykonuje instrukcje,
- bus mapuje adresy,
- urządzenia reagują na odczyt/zapis,
- profil komputera składa CPU, RAM, ROM i urządzenia w całość.

---

## 2. Docelowe komponenty PET

| Priorytet | Komponent | Typ | Cel |
|---|---|---|---|
| P0 | `PetMemoryMap` | memory bus / mapper | RAM, ROM, video RAM, I/O |
| P0 | `PetTextDisplayDevice` | video/text output | renderowanie video RAM jako PETSCII |
| P0 | `PetKeyboardMatrixDevice` | input | matryca klawiatury PET |
| P1 | `Mos6520PiaDevice` | I/O chip | porty A/B, DDR, control lines |
| P1 | `Mos6522ViaDevice` | I/O/timer chip | porty, timery, IRQ |
| P1 | `Mos6545CrtcDevice` | video controller | CRTC dla modeli 4032/8032 |
| P2 | `PetCassetteDevice` | storage/input | Datassette/TAP |
| P2 | `Ieee488BusDevice` | peripheral bus | dyski i drukarki PET |
| P3 | `PetBeeperDevice` | audio | prosty dźwięk przez VIA |
| P3 | `CommodoreDiskDriveDevice` | external computer/device | 4040/8050/8250 jako osobny emulator |

---

## 3. Wspólna infrastruktura urządzeń

### 3.1. Interfejs urządzenia memory-mapped

```csharp
public interface IMemoryMappedDevice
{
    bool Handles(ushort address);
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
```

### 3.2. Interfejs urządzenia cyklowego

Niektóre układy, np. VIA, CRTC albo cassette, będą wymagały aktualizacji w czasie.

```csharp
public interface ICycleDevice
{
    void Tick();
}
```

### 3.3. Interfejs IRQ

```csharp
public interface IIrqSource
{
    bool IsIrqAsserted { get; }
}
```

### 3.4. Testowy bus z trace

Do testów urządzeń potrzebny jest `TracingMemoryBus`, który zapisuje kolejność operacji.

```csharp
public sealed record MemoryAccess(
    MemoryAccessKind Kind,
    ushort Address,
    byte Value,
    long Cycle);

public enum MemoryAccessKind
{
    Read,
    Write
}
```

---

## 4. Plan: `PetMemoryMap`

### 4.1. Cel

`PetMemoryMap` składa RAM, ROM, video RAM i urządzenia I/O zgodnie z profilem komputera.

Nie powinien zawierać logiki CPU. Jego zadanie to wyłącznie mapowanie adresów.

### 4.2. Zakres MVP

- RAM jako tablica bajtów.
- ROM jako region read-only.
- Video RAM jako osobny region albo zwykły RAM obserwowany przez display.
- Lista urządzeń memory-mapped.
- Blokada zapisu do ROM.
- Obsługa konfliktu mapowań przez walidację profilu.

### 4.3. Proponowany model

```csharp
public sealed class PetMemoryMap : IMemoryBus
{
    private readonly List<MemoryRegion> _regions;
    private readonly List<IMemoryMappedDevice> _devices;

    public byte Read(ushort address);
    public void Write(ushort address, byte value);
}
```

### 4.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `Read_FromRam_ReturnsStoredValue` | zapis i odczyt RAM |
| `Write_ToRom_IsIgnoredOrThrowsDependingOnMode` | ROM jest read-only |
| `Read_FromRom_ReturnsLoadedByte` | ROM ładuje dane z obrazu |
| `Device_Address_IsForwardedToDevice` | adres I/O trafia do urządzenia |
| `OverlappingRegions_ThrowsValidationError` | profil nie pozwala na konflikt mapowań |
| `UnmappedRead_ReturnsConfiguredOpenBusValue` | zachowanie pustego adresu jest jawne |

### 4.5. Testy integracyjne

| Test | Cel |
|---|---|
| `PetProfile_LoadsRamRomVideoAndIo` | profil PET buduje pełną mapę |
| `Cpu_CanFetchResetVectorFromPetRom` | CPU pobiera reset vector z ROM |
| `Cpu_WriteToVideoRam_UpdatesDisplayBuffer` | zapis CPU do video RAM widoczny w display |

---

## 5. Plan: `PetTextDisplayDevice`

### 5.1. Cel

Renderowanie pamięci ekranu PET jako tekstu. W pierwszej wersji nie emulujemy sygnału CRT, tylko stan tekstowy ekranu.

### 5.2. Zakres MVP

- 40 × 25 znaków.
- Odczyt bajtów z video RAM.
- Mapowanie PETSCII/screencode do znaków Unicode.
- Cursor opcjonalnie.
- Snapshot ekranu jako `string[]` albo `PetScreenFrame`.

### 5.3. Proponowany model

```csharp
public sealed class PetTextDisplayDevice
{
    public int Columns { get; }
    public int Rows { get; }
    public ushort VideoRamStart { get; }

    public string[] RenderLines(IMemoryBus memory);
}
```

### 5.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `Render_EmptyVideoRam_ReturnsSpaces` | pusty ekran daje spacje |
| `Render_CharacterA_ReturnsA` | kod znaku mapuje się na `A` |
| `Render_40x25_Returns25LinesOf40Chars` | wymiary są poprawne |
| `Render_UnsupportedCode_UsesFallbackGlyph` | nieznany kod ma fallback |
| `Render_CursorEnabled_MarksCursorPosition` | cursor jest widoczny, jeśli włączony |

### 5.5. Testy integracyjne

| Test | Cel |
|---|---|
| `Cpu_StoresTextInVideoRam_DisplayShowsText` | program CPU pisze tekst na ekran |
| `PetProfile_DisplayUsesConfiguredVideoRamBase` | display korzysta z adresu z profilu |

---

## 6. Plan: `PetKeyboardMatrixDevice`

### 6.1. Cel

Emulacja matrycy klawiatury PET. Host keyboard powinien być mapowany na wiersze/kolumny PET, a PIA/VIA powinny widzieć stan linii.

### 6.2. Zakres MVP

- Mapa host key -> PET key.
- Stan naciśniętych klawiszy.
- Odczyt wybranego wiersza/kolumny.
- Obsługa Shift jako oddzielny klawisz matrycy.
- Bez key repeat w pierwszej wersji.

### 6.3. Proponowany model

```csharp
public sealed class PetKeyboardMatrixDevice
{
    public void KeyDown(PetKey key);
    public void KeyUp(PetKey key);
    public byte ReadRow(byte selectedRowMask);
}
```

### 6.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `KeyDown_SetsMatrixBit` | naciśnięcie ustawia stan matrycy |
| `KeyUp_ClearsMatrixBit` | puszczenie klawisza czyści stan |
| `ReadRow_NoKey_ReturnsAllInactive` | brak klawiszy daje stan nieaktywny |
| `ReadRow_SelectedKey_ReturnsActiveBit` | wybrany wiersz zwraca aktywną kolumnę |
| `Shift_IsIndependentMatrixKey` | Shift działa jako osobny klawisz |
| `MultipleKeys_ReturnsCombinedState` | kilka klawiszy jest widocznych jednocześnie |

### 6.5. Testy integracyjne

| Test | Cel |
|---|---|
| `Pia_ReadsKeyboardMatrix` | PIA odczytuje stan klawiatury |
| `HostKey_A_ProducesExpectedPetMatrixState` | mapowanie host -> PET działa |

---

## 7. Plan: `Mos6520PiaDevice`

### 7.1. Cel

Implementacja MOS 6520 PIA jako generycznego układu I/O używanego przez PET i później Apple-1.

### 7.2. Zakres MVP

- Port A i Port B.
- DDRA i DDRB.
- Control Register A/B w zakresie potrzebnym do wyboru DDR/portu.
- Linie CA1/CA2/CB1/CB2 jako stan logiczny.
- Podstawowe flagi przerwań jako późniejszy etap.

### 7.3. Rejestry logiczne

Typowy układ ekspozycji powinien obsługiwać cztery adresy:

```text
base + 0  Port A / DDRA zależnie od control bit
base + 1  Control A
base + 2  Port B / DDRB zależnie od control bit
base + 3  Control B
```

### 7.4. Proponowany model

```csharp
public sealed class Mos6520PiaDevice : IMemoryMappedDevice, IIrqSource
{
    public byte PortA { get; private set; }
    public byte PortB { get; private set; }
    public byte DdrA { get; private set; }
    public byte DdrB { get; private set; }

    public Func<byte>? ReadExternalPortA { get; set; }
    public Func<byte>? ReadExternalPortB { get; set; }
    public Action<byte>? WriteExternalPortA { get; set; }
    public Action<byte>? WriteExternalPortB { get; set; }
}
```

### 7.5. Testy jednostkowe

| Test | Cel |
|---|---|
| `WriteDdrA_ConfiguresOutputBits` | DDRA ustawia kierunki bitów |
| `WritePortA_OnlyOutputBitsAreDriven` | tylko bity output są wystawiane |
| `ReadPortA_MergesOutputLatchAndInputPins` | odczyt miesza latch i wejścia |
| `ControlA_SelectsDdrOrPort` | control register wybiera DDR/port |
| `PortB_BehavesLikePortA` | port B ma analogiczne zachowanie |
| `UnhandledRegister_ReturnsStableValueOrOpenBus` | niezdefiniowany odczyt jest jawny |

### 7.6. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Pia_KeyboardRows_SelectAndRead` | PET odczytuje klawiaturę przez PIA |
| `Pia_IrqLine_CanAssertCpuIrq` | PIA może wystawić IRQ, gdy etap IRQ CPU będzie gotowy |
| `Apple1_PiaTerminal_CanReusePiaCore` | układ nadaje się też do Apple-1 |

---

## 8. Plan: `Mos6522ViaDevice`

### 8.1. Cel

Implementacja MOS 6522 VIA: porty I/O, timery i IRQ. W PET VIA jest potrzebna dla części I/O, cassette, timerów i prostego dźwięku.

### 8.2. Zakres MVP

- ORB/ORA.
- DDRB/DDRA.
- Timer 1 podstawowy.
- IFR/IER.
- Linia IRQ.
- Timer 2 jako etap późniejszy.
- Shift register jako etap późniejszy.

### 8.3. Rejestry VIA

```text
base + 0x0  ORB / IRB
base + 0x1  ORA / IRA
base + 0x2  DDRB
base + 0x3  DDRA
base + 0x4  T1C-L
base + 0x5  T1C-H
base + 0x6  T1L-L
base + 0x7  T1L-H
base + 0x8  T2C-L
base + 0x9  T2C-H
base + 0xA  SR
base + 0xB  ACR
base + 0xC  PCR
base + 0xD  IFR
base + 0xE  IER
base + 0xF  ORA no handshake
```

### 8.4. Proponowany model

```csharp
public sealed class Mos6522ViaDevice : IMemoryMappedDevice, ICycleDevice, IIrqSource
{
    public void Tick();
    public bool IsIrqAsserted { get; }
}
```

### 8.5. Testy jednostkowe

| Test | Cel |
|---|---|
| `WriteDdrA_ConfiguresPortA` | DDRA działa |
| `WriteDdrB_ConfiguresPortB` | DDRB działa |
| `ReadPort_MergesInputAndOutput` | odczyt portu uwzględnia kierunek bitów |
| `Timer1_LoadCounterLowHigh_SetsCounter` | zapis T1 low/high ładuje licznik |
| `Timer1_Tick_DecrementsCounter` | Tick zmniejsza licznik |
| `Timer1_Underflow_SetsIfrBit` | underflow ustawia IFR |
| `Ier_EnableTimer1_IrqAssertedOnUnderflow` | IER włącza IRQ |
| `Ifr_WriteOne_ClearsSelectedFlag` | kasowanie IFR przez zapis 1 działa |
| `Ier_Bit7ControlsSetOrClear` | bit 7 IER decyduje set/clear |

### 8.6. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Via_TimerCanAssertCpuIrq` | VIA wystawia IRQ do CPU |
| `Via_PortCanDriveBeeper` | port/linia steruje beeperem |
| `Via_CassetteLinesExposeExpectedState` | linie cassette mają stan widoczny dla ROM |

---

## 9. Plan: `Mos6545CrtcDevice`

### 9.1. Cel

Implementacja kontrolera CRTC dla modeli PET/CBM 4032/8032. MVP nie emuluje sygnału CRT, tylko rejestry i parametry potrzebne rendererowi tekstowemu.

### 9.2. Zakres MVP

- Rejestr indeksu.
- Rejestry danych CRTC.
- Rejestry start address.
- Cursor address.
- Horizontal displayed / vertical displayed.
- Brak dokładnego HSYNC/VSYNC w pierwszej wersji.

### 9.3. Rejestry logiczne

```text
base + 0  Address Register / Index
base + 1  Data Register
```

Wewnętrznie:

```text
registers[18]
```

### 9.4. Proponowany model

```csharp
public sealed class Mos6545CrtcDevice : IMemoryMappedDevice, ICycleDevice
{
    public byte SelectedRegister { get; private set; }
    public ushort DisplayStartAddress { get; }
    public ushort CursorAddress { get; }
    public int Columns { get; }
    public int Rows { get; }
}
```

### 9.5. Testy jednostkowe

| Test | Cel |
|---|---|
| `WriteIndex_SelectsRegister` | zapis indexu wybiera rejestr |
| `WriteData_StoresSelectedRegister` | zapis danych trafia do rejestru |
| `ReadData_ReturnsSelectedRegister` | odczyt zwraca wybrany rejestr |
| `DisplayStartAddress_CombinesHighLowRegisters` | adres startu ekranu składa high/low |
| `CursorAddress_CombinesHighLowRegisters` | cursor składa high/low |
| `Columns_UsesHorizontalDisplayedRegister` | liczba kolumn z rejestru |
| `Rows_UsesVerticalDisplayedRegister` | liczba wierszy z rejestru |

### 9.6. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Crtc_ControlsDisplayStartAddress` | renderer czyta ekran od adresu CRTC |
| `Crtc_CursorVisibleAtConfiguredAddress` | cursor widoczny w rendererze |
| `Pet4032_Profile_Configures40x25` | profil 4032 daje 40×25 |
| `Pet8032_Profile_Configures80x25` | profil 8032 daje 80×25 |

---

## 10. Plan: `PetCassetteDevice`

### 10.1. Cel

Emulacja Datassette/TAP jako późniejszy etap. Na początku programy można ładować bezpośrednio do RAM lub przez ROM monitor.

### 10.2. Zakres MVP

- Stan play/stop.
- Linia read/write cassette.
- Wczytywanie prostego bufora danych jako impulsy logiczne.
- Brak dokładnej emulacji analogowego sygnału w pierwszej wersji.

### 10.3. Proponowany model

```csharp
public sealed class PetCassetteDevice : ICycleDevice
{
    public bool MotorOn { get; set; }
    public bool ReadLine { get; }
    public void LoadTap(Stream stream);
    public void Tick();
}
```

### 10.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `MotorOff_ReadLineStable` | przy wyłączonym motorze linia stabilna |
| `LoadTap_ParsesHeaderOrRawPulses` | loader przyjmuje dane |
| `Tick_AdvancesPulseStream` | Tick przesuwa stan impulsów |
| `EndOfTape_SetsFinishedState` | koniec taśmy jest wykryty |

### 10.5. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Via_CanReadCassetteLine` | VIA widzi linię cassette |
| `RomLoadRoutine_CanReceiveSyntheticTapeSignal` | ROM loader dostaje sygnał testowy |

---

## 11. Plan: `Ieee488BusDevice`

### 11.1. Cel

Emulacja magistrali IEEE-488 używanej przez PET do komunikacji z dyskami i urządzeniami peryferyjnymi.

### 11.2. Zakres MVP

- Linie danych DIO1-DIO8.
- Linie sterujące: ATN, DAV, NRFD, NDAC, EOI, IFC, SRQ, REN.
- Prosty model urządzenia podłączonego do busa.
- Brak dokładnego timingu w pierwszej wersji.

### 11.3. Proponowany model

```csharp
public sealed class Ieee488BusDevice
{
    public byte DataLines { get; set; }
    public bool Atn { get; set; }
    public bool Dav { get; set; }
    public bool Nrfd { get; set; }
    public bool Ndac { get; set; }
    public bool Eoi { get; set; }

    public void Attach(IIeee488Peripheral peripheral);
}
```

### 11.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `Attach_AddsPeripheral` | urządzenie dołącza do busa |
| `SetDataLines_PeripheralCanRead` | peryferium widzi dane |
| `ControlLines_AreSharedAcrossDevices` | linie sterujące są wspólne |
| `TalkerListener_HandshakeBasicFlow` | prosty handshake działa |

### 11.5. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Pia_ControlsIeee488Lines` | PIA steruje liniami IEEE-488 |
| `Pet_CanSendCommandToFakeDisk` | PET wysyła komendę do fikcyjnego dysku |

---

## 12. Plan: `PetBeeperDevice`

### 12.1. Cel

Prosty dźwięk sterowany linią VIA albo portem. Na start wystarczy rejestrować przełączenia stanu, nie generować audio.

### 12.2. Zakres MVP

- Wejście logiczne beepera.
- Rejestracja zmian stanu.
- Callback `OnBeepStateChanged`.
- Później generator audio.

### 12.3. Proponowany model

```csharp
public sealed class PetBeeperDevice
{
    public bool State { get; private set; }
    public void SetState(bool state);
    public event Action<bool>? StateChanged;
}
```

### 12.4. Testy jednostkowe

| Test | Cel |
|---|---|
| `SetState_ChangesState` | stan się zmienia |
| `SetState_RaisesEventOnlyOnChange` | event tylko przy zmianie |
| `RepeatedSameState_DoesNotRaiseDuplicateEvent` | brak duplikatów |

### 12.5. Testy integracyjne PET

| Test | Cel |
|---|---|
| `Via_OutputLine_DrivesBeeper` | linia VIA steruje beeperem |

---

## 13. Plan: `CommodoreDiskDriveDevice`

### 13.1. Cel

Stacje dysków PET, np. 4040/8050, są osobnymi komputerami z własnym CPU i firmware. Nie powinny być częścią pierwszego profilu PET.

### 13.2. Zakres przyszły

- Osobny CPU 6502 dla stacji.
- ROM stacji.
- RAM stacji.
- IEEE-488 peripheral interface.
- Format obrazu dysku.
- Komendy DOS Commodore.

### 13.3. Testy przyszłe

| Test | Cel |
|---|---|
| `Drive_BootsFirmware` | firmware stacji startuje |
| `Drive_ReceivesIeeeCommand` | dysk odbiera komendę |
| `Drive_LoadDirectory_ReturnsListing` | katalog jest zwracany |
| `Pet_LoadCommand_ReadsProgramFromDrive` | PET ładuje program |

---

## 14. Kolejność implementacji

### Etap 1 — fundament pod PET-like

- [ ] `PetMemoryMap`.
- [ ] `PetTextDisplayDevice`.
- [ ] `PetKeyboardMatrixDevice` w wersji uproszczonej.
- [ ] Profil `commodore-pet-like.json`.
- [ ] Test programu zapisującego tekst do video RAM.

### Etap 2 — układy I/O

- [ ] `Mos6520PiaDevice`.
- [ ] Integracja PIA z klawiaturą.
- [ ] `Mos6522ViaDevice` MVP.
- [ ] Podstawowe IRQ, gdy CPU będzie gotowe na IRQ.

### Etap 3 — CRTC

- [ ] `Mos6545CrtcDevice`.
- [ ] Integracja CRTC z rendererem.
- [ ] Profile 4032 i 8032.

### Etap 4 — nośniki i peryferia

- [ ] `PetCassetteDevice`.
- [ ] `Ieee488BusDevice`.
- [ ] Fake disk jako testowy peripheral.
- [ ] Prawdziwa stacja dysków jako osobny emulator.

---

## 15. Minimalny wariant startowy

Najpierw warto zbudować niepełny profil:

```text
PET-like 40x25
CPU 6502
RAM 32 KB
ROM region
Video RAM
Text display
Simplified keyboard
```

Ten profil nie musi uruchamiać oryginalnych ROM-ów PET. Ma służyć jako etap techniczny do sprawdzenia pamięci, display i wejścia.

---

## 16. Wariant docelowy pierwszy

Pierwszym historycznym celem powinien być:

```text
Commodore PET / CBM 4032
CPU 6502
32 KB RAM
BASIC 4 ROM
EDITOR ROM
KERNAL ROM
Video RAM 40x25
Character ROM
MOS 6545 CRTC
MOS 6520 PIA x2
MOS 6522 VIA
Keyboard matrix
```

---

## 17. Definition of Done

Obszar PET można uznać za gotowy etapowo, gdy:

- [ ] profil PET jest osobnym plikiem w `profiles/computers`,
- [ ] ROM-y nie są commitowane bez licencji,
- [ ] CPU nie zawiera logiki specyficznej dla PET,
- [ ] video RAM renderuje tekst,
- [ ] klawiatura działa przez matrycę,
- [ ] PIA obsługuje porty i DDR,
- [ ] VIA obsługuje porty, Timer 1, IFR/IER i IRQ,
- [ ] CRTC obsługuje rejestry wymagane przez renderer,
- [ ] testy jednostkowe pokrywają każdy układ,
- [ ] test integracyjny uruchamia minimalny program PET-like,
- [ ] test integracyjny uruchamia ROM PET do pierwszego widocznego promptu lub znanego punktu startowego.
