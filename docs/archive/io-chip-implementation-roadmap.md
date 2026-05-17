# Roadmapa implementacji układów wejścia/wyjścia

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument koncepcyjny / plan implementacyjny  
Zakres: układy I/O dla komputerów 6502/6510/65C02, własnego SBC, Apple-1, KIM-1, PET, VIC-20, BBC Micro, C64, Atari i maszyn pokrewnych

---

## 1. Cel dokumentu

Celem jest uporządkowanie implementacji układów wejścia/wyjścia jako osobnych komponentów maszyny, podłączanych do magistrali pamięci i sygnałów CPU. Układy I/O nie powinny być częścią rdzenia CPU.

CPU powinien widzieć tylko:

- odczyt spod adresu,
- zapis pod adres,
- sygnały IRQ/NMI/RDY/HALT, jeśli dany układ je generuje albo wymusza.

Komputery powinny składać się z CPU, pamięci i urządzeń I/O przez profile maszyn.

---

## 2. Wspólna infrastruktura

### 2.1. Urządzenie memory-mapped

```csharp
public interface IMemoryMappedDevice
{
    bool Handles(ushort address);
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
```

### 2.2. Urządzenie cyklowe

```csharp
public interface ICycleDevice
{
    void Tick();
}
```

### 2.3. Źródło IRQ

```csharp
public interface IIrqSource
{
    bool IsIrqAsserted { get; }
}
```

### 2.4. Źródło NMI

```csharp
public interface INmiSource
{
    bool IsNmiAsserted { get; }
}
```

### 2.5. Kontroler linii CPU

Dla układów typu ANTIC/SALLY, DMA albo urządzeń wymuszających zatrzymanie CPU przyda się później:

```csharp
public interface ICpuLineController
{
    bool HaltRequested { get; }
    bool ReadyRequested { get; }
}
```

### 2.6. Trace urządzeń

Do testów i debugowania dodać zapis dostępu do urządzeń:

```csharp
public sealed record DeviceAccess(
    string DeviceId,
    MemoryAccessKind Kind,
    ushort Address,
    byte Value,
    long Cycle);
```

---

## 3. Priorytety układów

| Priorytet | Układ / urządzenie | Główne zastosowanie |
|---|---|---|
| P0 | `IMemoryMappedDevice`, `SystemBus`, `TracingMemoryBus` | fundament wszystkich profili |
| P1 | `UartSimpleDevice` | własny SBC, monitor ROM, TTY/mainframe link |
| P1 | `Mos6821PiaDevice` / `Mos6820PiaDevice` | Apple-1, ogólne PIA |
| P1 | `Mos6522ViaDevice` | PET, VIC-20, BBC Micro, custom SBC |
| P1 | `Mos6520PiaDevice` | Commodore PET |
| P2 | `Mos6530RriotDevice` | KIM-1 |
| P2 | `Mos6532RiotDevice` | Atari 2600, proste SBC |
| P2 | `Mos6551AciaDevice` | historyczny UART/serial |
| P3 | `Mos6526CiaDevice` | C64/C128 |
| P3 | `Motorola6850AciaDevice` | starsze systemy terminalowe |
| P3 | `Intel8255PpiDevice` | custom SBC, systemy CP/M-style |
| P3 | `Ay38910IoPorts` | AY-3-8910/YM2149 jako I/O + audio |
| P4 | `PokeyDevice` | Atari 8-bit |
| P4 | `NesControllerPorts` | NES / Ricoh 2A03 profile |

---

## 4. `UartSimpleDevice`

### 4.1. Cel

Prosty emulatorowy UART do własnego SBC, monitora ROM, Tiny BASIC, TTY i połączenia z fake mainframe.

### 4.2. Rejestry

```text
base + 0  DATA
base + 1  STATUS
```

Status:

```text
bit 0 = RX_READY
bit 1 = TX_READY
```

### 4.3. Zakres MVP

- DATA read/write.
- STATUS read.
- Integracja z `ITerminalLink`.
- Brak baud rate i timingów w MVP.

### 4.4. Testy

- [ ] `Status_WhenNoInput_ReturnsTxReadyOnly`
- [ ] `Status_WhenInputAvailable_ReturnsRxReadyAndTxReady`
- [ ] `ReadData_DequeuesInputByte`
- [ ] `ReadStatus_DoesNotDequeueInputByte`
- [ ] `WriteData_ForwardsByteToTerminalLink`
- [ ] `Handles_OnlyDataAndStatusRegisters`

---

## 5. `Mos6821PiaDevice` / `Mos6820PiaDevice`

### 5.1. Cel

Generyczna PIA dla Apple-1 i innych prostych systemów równoległego I/O.

### 5.2. Zastosowanie

| Maszyna | Użycie |
|---|---|
| Apple-1 | klawiatura/wyświetlacz terminalowy |
| własny SBC | porty równoległe |
| urządzenia testowe | proste I/O |

### 5.3. Rejestry

Typowy układ 4 rejestrów:

```text
base + 0  ORA / DDRA zależnie od CRA
base + 1  CRA
base + 2  ORB / DDRB zależnie od CRB
base + 3  CRB
```

### 5.4. Zakres MVP

- Port A/B.
- DDR A/B.
- CRA/CRB w zakresie wyboru port vs DDR.
- Mieszanie wejść z wyjściami.
- Callbacki dla pinów zewnętrznych.
- IRQ jako późniejszy etap.

### 5.5. Testy

- [ ] `WriteDdrA_ConfiguresDirectionBits`
- [ ] `WriteDdrB_ConfiguresDirectionBits`
- [ ] `WritePortA_DrivesOnlyOutputBits`
- [ ] `ReadPortA_MergesOutputLatchAndExternalInputs`
- [ ] `ControlA_SelectsDataDirectionRegister`
- [ ] `ControlA_SelectsOutputRegister`
- [ ] `PortB_BehavesLikePortA`
- [ ] `Apple1Terminal_CanBeBuiltOnPiaOrCompatibilityAdapter`

---

## 6. `Mos6520PiaDevice`

### 6.1. Cel

PIA używana przez Commodore PET. W wielu przypadkach może współdzielić rdzeń z `Mos6821PiaDevice`, ale warto mieć jawny wariant profilu układu.

### 6.2. Zakres MVP

- Te same podstawy co PIA 6821: porty, DDR, control registers.
- Integracja z keyboard matrix PET.
- Podstawowe flagi sterujące.

### 6.3. Testy

- [ ] `PetPia_ReadsKeyboardRows`
- [ ] `PetPia_WritesKeyboardColumnSelect`
- [ ] `PetPia_PortDirectionAffectsReadValue`
- [ ] `PetPia_ControlRegisterSelectsDdrOrPort`
- [ ] `PetPia_IrqFlagCanBeAssertedLater`

---

## 7. `Mos6522ViaDevice` / `Wdc65C22ViaDevice`

### 7.1. Cel

VIA to jeden z najważniejszych układów dla PET, VIC-20, BBC Micro i własnego SBC. Zawiera porty I/O, timery, shift register i IRQ.

### 7.2. Rejestry

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

### 7.3. Zakres MVP

- ORA/ORB.
- DDRA/DDRB.
- Timer 1 podstawowy.
- IFR/IER.
- `IsIrqAsserted`.
- Timer 2 później.
- Shift register później.
- Handshake lines później.

### 7.4. Testy

- [ ] `WriteDdra_ConfiguresPortA`
- [ ] `WriteDdrb_ConfiguresPortB`
- [ ] `ReadPort_MergesInputAndOutputBits`
- [ ] `Timer1_WriteLowHigh_LoadsCounter`
- [ ] `Timer1_Tick_DecrementsCounter`
- [ ] `Timer1_Underflow_SetsIfrFlag`
- [ ] `Ier_EnableTimer1_AllowsIrq`
- [ ] `Ier_DisableTimer1_BlocksIrq`
- [ ] `Ifr_WriteOne_ClearsFlag`
- [ ] `IrqAsserted_WhenEnabledFlagIsSet`

---

## 8. `Mos6530RriotDevice`

### 8.1. Cel

RRIOT dla KIM-1 i pokrewnych SBC. Układ łączy ROM, RAM, I/O i timer. Dla emulatora można rozdzielić funkcje wewnętrznie, ale profil powinien traktować go jako jeden układ.

### 8.2. Zastosowanie

| Maszyna | Użycie |
|---|---|
| KIM-1 | monitor ROM, RAM, porty, timer |
| systemy SBC | warianty zależnie od konfiguracji |

### 8.3. Zakres MVP

- Region ROM układu.
- Mały region RAM.
- Port A/B z DDR.
- Timer uproszczony.
- Mapowanie zależne od profilu.

### 8.4. Testy

- [ ] `ReadRom_ReturnsConfiguredRomByte`
- [ ] `WriteRom_IsIgnoredOrThrowsDependingOnMode`
- [ ] `Ram_ReadWrite_WorksWithinRriotRange`
- [ ] `PortA_DdrControlsDirection`
- [ ] `PortB_DdrControlsDirection`
- [ ] `Timer_Tick_Decrements`
- [ ] `Timer_Underflow_SetsFlag`
- [ ] `Kim1Profile_CanMapRriotDevices`

---

## 9. `Mos6532RiotDevice`

### 9.1. Cel

RIOT dla Atari 2600 i prostych systemów SBC. Ma RAM, porty I/O i timer, ale bez ROM jak 6530.

### 9.2. Zakres MVP

- 128 bajtów RAM, jeśli profil tego wymaga.
- Port A/B.
- DDR A/B.
- Timer.
- IRQ flag.

### 9.3. Testy

- [ ] `Ram_ReadWrite_Works`
- [ ] `PortA_DirectionBits_Work`
- [ ] `PortB_DirectionBits_Work`
- [ ] `Timer_LoadAndTick_Works`
- [ ] `Timer_Underflow_SetsInterruptFlag`
- [ ] `Atari2600Profile_CanUseRiotLater`

---

## 10. `Mos6551AciaDevice`

### 10.1. Cel

Historycznie poprawniejszy UART/serial niż `UartSimpleDevice`. Przydatny dla własnego SBC, terminali, modemów i komputerów z ACIA.

### 10.2. Rejestry

```text
base + 0  DATA
base + 1  STATUS
base + 2  COMMAND
base + 3  CONTROL
```

### 10.3. Zakres MVP

- DATA read/write.
- STATUS RX/TX.
- COMMAND i CONTROL jako rejestry zapamiętywane.
- Integracja z `ITerminalLink`.
- Brak realnej emulacji baud rate w MVP.

### 10.4. Testy

- [ ] `WriteData_SendsByteToTerminalLink`
- [ ] `ReadData_ReadsByteFromTerminalLink`
- [ ] `Status_ReportsRxReady`
- [ ] `Status_ReportsTxReady`
- [ ] `CommandRegister_ReadWrite_RoundTrips`
- [ ] `ControlRegister_ReadWrite_RoundTrips`
- [ ] `ReadStatus_DoesNotConsumeRxByte`

---

## 11. `Motorola6850AciaDevice`

### 11.1. Cel

Alternatywny historyczny ACIA, przydatny dla systemów terminalowych i własnych profili. Nie jest pierwszym układem do implementacji, ale pasuje jako P3.

### 11.2. Zakres MVP

- DATA.
- STATUS.
- CONTROL.
- Integracja z `ITerminalLink`.

### 11.3. Testy

- [ ] `ReadStatus_ReportsRxReady`
- [ ] `ReadStatus_ReportsTxReady`
- [ ] `WriteData_ForwardsByte`
- [ ] `ReadData_ReturnsInputByte`
- [ ] `ControlRegister_RoundTripsSupportedBits`

---

## 12. `Mos6526CiaDevice`

### 12.1. Cel

CIA dla C64/C128. To duży układ: porty, timery, TOD clock, serial shift, IRQ. Nie implementować przed 6510 i C64 memory banking.

### 12.2. Zakres MVP

- Port A/B.
- DDRA/DDRB.
- Timer A.
- Timer B później.
- IRQ mask/flags.
- TOD później.
- Serial później.

### 12.3. Testy

- [ ] `PortA_DdrControlsDirection`
- [ ] `PortB_DdrControlsDirection`
- [ ] `TimerA_Load_Start_Tick_Underflow`
- [ ] `TimerA_Underflow_SetsIcrFlag`
- [ ] `IcrMask_ControlsIrqAssertion`
- [ ] `C64KeyboardMatrix_CanUseCiaPorts`
- [ ] `C64Joystick_CanUseCiaPorts`

---

## 13. `Intel8255PpiDevice`

### 13.1. Cel

Ogólny równoległy układ I/O dla własnych SBC i systemów stylizowanych na CP/M/retro. Nie jest typowy dla klasycznych komputerów Commodore/Apple, ale jest prosty i użyteczny.

### 13.2. Rejestry

```text
base + 0  Port A
base + 1  Port B
base + 2  Port C
base + 3  Control
```

### 13.3. Zakres MVP

- Mode 0.
- Port A/B/C.
- Kierunki portów przez control word.
- Bez handshake mode 1/2 na początku.

### 13.4. Testy

- [ ] `ControlWord_ConfiguresPortDirections`
- [ ] `WritePortA_DrivesOutputWhenConfigured`
- [ ] `ReadPortA_ReturnsExternalInputWhenInput`
- [ ] `PortC_UpperLowerDirections_Work`
- [ ] `Mode1AndMode2_AreRejectedOrIgnoredInMvp`

---

## 14. `Ay38910Device` — porty I/O i audio

### 14.1. Cel

AY-3-8910/YM2149 to układ dźwiękowy z trzema kanałami oraz portami I/O. Dla tego projektu może być użyty w custom SBC i później w maszynach typu Oric/MSX/Amstrad-like.

### 14.2. Interfejs rejestrów

Prosty wariant memory-mapped:

```text
base + 0  ADDRESS
base + 1  DATA
```

### 14.3. Zakres MVP

- 16 rejestrów.
- Wybór rejestru.
- Read/write danych.
- Rejestry portów A/B.
- Generacja dźwięku później.

### 14.4. Testy

- [ ] `WriteAddress_SelectsRegister`
- [ ] `WriteData_WritesSelectedRegister`
- [ ] `ReadData_ReturnsSelectedRegister`
- [ ] `SelectedRegister_IsMaskedTo0F`
- [ ] `PortA_ReadWrite_WorksInSimpleMode`
- [ ] `PortB_ReadWrite_WorksInSimpleMode`
- [ ] `ToneRegisters_CombinePeriodValues`

---

## 15. `PokeyDevice`

### 15.1. Cel

POKEY dla Atari 8-bit. Obejmuje dźwięk, klawiaturę, timery, serial I/O i IRQ. To duży etap, dopiero po prostszych układach.

### 15.2. Zakres przyszły

- Rejestry audio.
- Timery.
- Keyboard scan.
- Serial I/O.
- IRQ.

### 15.3. Testy przyszłe

- [ ] `AudioRegister_Write_RoundTripsWhereReadable`
- [ ] `Timer_Underflow_SetsIrq`
- [ ] `KeyboardScan_ReturnsConfiguredKey`
- [ ] `PokeyIrq_AssertedWhenEnabled`

---

## 16. `NesControllerPorts`

### 16.1. Cel

Porty kontrolerów NES jako część profilu Ricoh 2A03/NES. To nie jest klasyczny układ PIA/VIA, ale pasuje do warstwy I/O.

### 16.2. Zakres MVP

- Rejestr strobe.
- Szeregowy odczyt przycisków.
- Dwa kontrolery.

### 16.3. Testy

- [ ] `WriteStrobe_LatchesControllerState`
- [ ] `ReadController_ReturnsBitsInExpectedOrder`
- [ ] `SecondController_UsesSeparateState`
- [ ] `RepeatedReads_After8Buttons_ReturnsStableValue`

---

## 17. Dopasowanie układów do profili komputerów

| Profil | Układy I/O |
|---|---|
| `minimal-sbc-6502` | `UartSimpleDevice`, później `Mos6551AciaDevice`, opcjonalnie `Mos6522ViaDevice`, `Ay38910Device` |
| `apple-1` | `Apple1PiaTerminalDevice`, docelowo `Mos6821PiaDevice` |
| `kim-1` | `Mos6530RriotDevice` |
| `aim-65` | keypad/display, ACIA/PIA zależnie od profilu |
| `sym-1` | keypad/display, VIA/RIOT/ACIA zależnie od profilu |
| `commodore-pet` | `Mos6520PiaDevice`, `Mos6522ViaDevice`, keyboard matrix, CRTC |
| `vic-20` | `Mos6522ViaDevice`, VIC, keyboard matrix |
| `bbc-micro` | `Mos6522ViaDevice`, video ULA, keyboard |
| `c64` | `Mos6526CiaDevice`, 6510 port, VIC-II, SID |
| `atari-2600` | `Mos6532RiotDevice`, TIA |
| `atari-800` | `PokeyDevice`, PIA, ANTIC, GTIA |
| `nes` | `NesControllerPorts`, PPU/APU/DMA |

---

## 18. Proponowana kolejność realizacji

### Etap 1 — fundament

- [ ] `IMemoryMappedDevice`.
- [ ] `ICycleDevice`.
- [ ] `IIrqSource`.
- [ ] `SystemBus` z listą urządzeń.
- [ ] `TracingMemoryBus`.

### Etap 2 — terminal i custom SBC

- [ ] `UartSimpleDevice`.
- [ ] `ITerminalLink`.
- [ ] `BufferedTerminalLink`.
- [ ] `FakeMainframeTerminalLink`.
- [ ] Monitor ROM / TTY ROM.

### Etap 3 — Apple-1

- [ ] `Apple1PiaTerminalDevice` jako adapter zgodności.
- [ ] `Mos6821PiaDevice` jako docelowy PIA.
- [ ] Test WOZ Monitor.

### Etap 4 — PET / VIA / PIA

- [ ] `Mos6520PiaDevice`.
- [ ] `Mos6522ViaDevice` MVP.
- [ ] Integracja z keyboard matrix i PET display.

### Etap 5 — KIM-1 / SBC family

- [ ] `Mos6530RriotDevice`.
- [ ] `Mos6532RiotDevice`.
- [ ] Profile KIM-1/AIM-65/SYM-1.

### Etap 6 — historyczny serial

- [ ] `Mos6551AciaDevice`.
- [ ] `Motorola6850AciaDevice` opcjonalnie.

### Etap 7 — custom audio/I/O

- [ ] `Ay38910Device` rejestry.
- [ ] Porty I/O AY.
- [ ] Później generator audio.

### Etap 8 — większe systemy

- [ ] `Mos6526CiaDevice` dla C64.
- [ ] `PokeyDevice` dla Atari.
- [ ] `NesControllerPorts` dla NES.

---

## 19. Standard testów

Docelowo testy powinny być w MSTest + FluentAssertions + Moq.

Kategorie testów:

| Kategoria | Zakres |
|---|---|
| Unit | rejestry, porty, flagi, timery |
| Integration | urządzenie + bus + CPU |
| Profile | loader profilu tworzy poprawne urządzenia |
| Trace | kolejność odczytów/zapisów |
| Timing | ticki, timery, IRQ |

Każdy układ powinien mieć:

- testy rejestrów,
- testy mapowania adresów,
- testy reset state,
- testy odczytu/zapisu,
- testy IRQ, jeśli układ je obsługuje,
- test integracyjny z `SystemBus`.

---

## 20. Definition of Done

Roadmapa układów I/O jest zrealizowana etapowo, gdy:

- [ ] wszystkie urządzenia implementują wspólny model memory-mapped,
- [ ] CPU nie zawiera logiki specyficznej dla urządzeń,
- [ ] profile komputerów deklarują urządzenia przez konfigurację,
- [ ] istnieje trace dostępu do urządzeń,
- [ ] UART działa z TTY/mainframe link,
- [ ] Apple-1 ma działające PIA/terminal,
- [ ] PET ma PIA/VIA/keyboard/display,
- [ ] KIM-1 ma RRIOT albo kompatybilny adapter,
- [ ] C64/NES/Atari są traktowane jako późniejsze etapy z osobnymi układami specjalizowanymi.
