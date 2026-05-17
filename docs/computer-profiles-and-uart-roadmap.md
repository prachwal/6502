# Profile komputerów i roadmapa UART

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument koncepcyjny / roadmapa  
Zakres: profile komputerów, format plików maszyn, UART i urządzenia I/O

---

## 1. Cel dokumentu

Ten dokument opisuje, jak traktować komputery budowane na bazie CPU 6502/6510/65C02 jako **oddzielny typ konfiguracji/profilu**, niezwiązany bezpośrednio z implementacją rdzenia CPU.

CPU powinien pozostać modułem wykonującym instrukcje. Komputer powinien być osobną definicją maszyny zawierającą:

- wariant CPU,
- mapę pamięci,
- ROM-y,
- RAM,
- urządzenia I/O,
- wektory startowe,
- zegar,
- terminal lub ekran,
- opcjonalne układy specjalizowane.

---

## 2. Zasada architektoniczna

### 2.1. CPU nie powinien znać komputera

Rdzeń CPU nie powinien zawierać logiki typu:

- `if Apple1`,
- `if C64`,
- `if KIM1`,
- `if UART`,
- `if PIA`,
- `if VIC`.

CPU powinien znać tylko:

- magistralę pamięci,
- odczyt bajtu,
- zapis bajtu,
- reset/IRQ/NMI,
- wariant CPU, jeśli wpływa na instrukcje lub quirki.

### 2.2. Komputer jako profil maszyny

Komputer powinien być definiowany jako osobny plik profilu, np. JSON/YAML:

```text
profiles/computers/apple-1.json
profiles/computers/kim-1.json
profiles/computers/minimal-sbc-6502.json
profiles/computers/c64.json
```

Nie należy mieszać tych profili z dokumentami planów ani z kodem CPU.

---

## 3. Proponowany typ pliku profilu komputera

### 3.1. Lokalizacja

```text
profiles/
  computers/
    minimal-sbc-6502.json
    apple-1.json
    kim-1.json
    commodore-pet.json
    apple-ii.json
    c64.json
  cpu/
    mos6502-nmos.json
    mos6510.json
    ricoh-2a03.json
```

### 3.2. Minimalny schemat profilu komputera

```json
{
  "schema": "computer-profile/v1",
  "id": "minimal-sbc-6502",
  "name": "Minimal SBC 6502",
  "status": "planned",
  "cpu": {
    "variant": "mos6502-nmos",
    "clockHz": 1000000
  },
  "memory": {
    "ram": [
      { "start": "0x0000", "size": "0x8000" }
    ],
    "rom": [
      {
        "id": "monitor",
        "address": "0xFF00",
        "size": "0x0100",
        "file": "roms/minimal/monitor.bin"
      }
    ]
  },
  "devices": [
    {
      "id": "uart0",
      "type": "uart-simple",
      "baseAddress": "0xD000"
    }
  ],
  "vectors": {
    "reset": "0xFFFC",
    "irq": "0xFFFE",
    "nmi": "0xFFFA"
  },
  "frontend": {
    "default": "terminal"
  }
}
```

### 3.3. Statusy profilu

| Status | Znaczenie |
|---|---|
| `planned` | tylko koncepcja |
| `stub` | profil istnieje, ale nie uruchamia realnego ROM-u |
| `partial` | działa część maszyny |
| `working` | profil uruchamia podstawowy monitor/program |
| `compatible` | profil ma testy zgodności i znane ograniczenia |

---

## 4. Propozycje komputerów na bazie 6502

### 4.1. Minimalny SBC 6502

| Cecha | Wartość |
|---|---|
| Priorytet | P0 |
| Trudność | Bardzo mała |
| CPU | MOS 6502 |
| Ekran | Terminal/UART |
| Cel | Testowanie CPU, ROM-ów i magistrali |

#### Zakres

Minimalny komputer testowy powinien mieć:

- RAM,
- ROM,
- reset vector,
- prosty UART,
- możliwość ładowania programu binarnego.

#### Proponowana mapa pamięci

```text
$0000-$7FFF  RAM
$8000-$CFFF  wolne / rozszerzenia
$D000-$D001  UART simple
$FF00-$FFFF  ROM monitor
```

#### Dlaczego pierwszy

To najprostszy profil do testowania emulatora bez komplikacji historycznych układów I/O.

---

### 4.2. Apple-1

| Cecha | Wartość |
|---|---|
| Priorytet | P1 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| I/O | PIA 6820/6821 |
| Ekran | Terminal znakowy |
| ROM | WOZ Monitor, opcjonalnie BASIC |

#### Zakres

Apple-1 jest dobrym pierwszym historycznym komputerem dla projektu. Nie wymaga złożonej grafiki, ale wymaga emulacji PIA i terminala.

#### Proponowana mapa pamięci

```text
$0000-$7FFF  RAM
$D010-$D013  PIA terminal
$E000-$EFFF  BASIC ROM opcjonalnie
$FF00-$FFFF  WOZ Monitor ROM
```

#### Do zrobienia

- [ ] Profil `profiles/computers/apple-1.json`.
- [ ] Urządzenie `pia-6821-terminal`.
- [ ] Obsługa klawiatury i wyjścia znakowego.
- [ ] Test uruchomienia WOZ Monitor.

---

### 4.3. KIM-1

| Cecha | Wartość |
|---|---|
| Priorytet | P1/P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| I/O | 6530 RRIOT / uproszczony keypad/display |
| Ekran | LED/keypad albo terminal zastępczy |

#### Zakres

KIM-1 jest dobrym drugim historycznym SBC. Jest prostszy niż pełne komputery domowe, ale wymaga modelu układów I/O albo uproszczonej emulacji.

#### Do zrobienia

- [ ] Profil `profiles/computers/kim-1.json`.
- [ ] Uproszczony keypad/display.
- [ ] Później dokładniejszy model 6530 RRIOT.

---

### 4.4. Commodore PET

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Trudność | Duża |
| CPU | MOS 6502 |
| I/O | VIA/PIA |
| Ekran | Tekstowy video RAM |
| ROM | BASIC/KERNAL/editor |

#### Zakres

PET jest dobrym pierwszym pełniejszym komputerem tekstowym. Jest prostszy niż C64, ale wymaga obsługi klawiatury, video RAM i ROM-ów systemowych.

#### Do zrobienia

- [ ] Profil `profiles/computers/commodore-pet.json`.
- [ ] Video RAM jako urządzenie/obszar pamięci.
- [ ] Klawiatura jako matrix albo uproszczony input.
- [ ] VIA/PIA.

---

### 4.5. Apple II

| Cecha | Wartość |
|---|---|
| Priorytet | P3 |
| Trudność | Duża |
| CPU | MOS 6502 |
| I/O | soft-switches, sloty, Disk II |
| Ekran | tekst/grafika Apple II |

#### Zakres

Apple II jest logicznym następnym krokiem po Apple-1, ale to dużo większy projekt. Wymaga soft-switches, video memory layout, ROM-ów i docelowo Disk II.

#### Do zrobienia

- [ ] Profil `profiles/computers/apple-ii.json`.
- [ ] Soft-switches.
- [ ] Tryb tekstowy.
- [ ] Później grafika i dysk.

---

### 4.6. Commodore 64

| Cecha | Wartość |
|---|---|
| Priorytet | P3/P4 |
| Trudność | Bardzo duża |
| CPU | MOS 6510 |
| I/O | CIA, VIC-II, SID |
| Ekran | VIC-II |
| Dźwięk | SID |

#### Zakres

C64 nie powinien być traktowany jako prosty profil 6502. Wymaga wariantu CPU 6510, portu `$0000/$0001`, bankowania pamięci i wielu układów specjalizowanych.

#### Do zrobienia

- [ ] Najpierw `CpuVariant.Mos6510`.
- [ ] Potem port 6510 `$0000/$0001`.
- [ ] Potem `C64MemoryBus`.
- [ ] Dopiero potem VIC-II/CIA/SID.

---

## 5. Podobne komputery z epoki — kandydaci dodatkowi

Ta sekcja opisuje maszyny podobne do już planowanych profili. Nie są one od razu częścią implementacji, ale pomagają projektować wspólne komponenty: bus, ROM/RAM, terminal, matrycę klawiatury, video text buffer, PIA/VIA, ACIA, RRIOT i proste kontrolery wideo.

### 5.1. Klasy podobieństwa

| Klasa | Przykłady | Wspólne komponenty | Priorytet |
|---|---|---|---:|
| SBC / monitor board | AIM-65, SYM-1, Microtan 65 | RAM, ROM, keypad/terminal, PIA/RRIOT/ACIA | P1/P2 |
| Prosty komputer tekstowy | Ohio Scientific, Compukit UK101 | RAM, ROM, video text buffer, keyboard matrix | P2 |
| Komputer tekstowy z układami I/O | PET, Acorn Atom, BBC Micro | VIA/PIA, keyboard matrix, video logic | P2/P3 |
| Komputer z układem wideo | VIC-20, Atari 400/800 | układ wideo, keyboard, sound, memory map | P3/P4 |
| System z wariantem CPU | C64, NES, C128 | 6510/2A03/8502, specjalizowany bus | P3/P4 |

---

### 5.2. AIM-65

| Cecha | Wartość |
|---|---|
| Priorytet | P1/P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| Podobny do | KIM-1 / własny SBC |
| I/O | wyświetlacz, klawiatura, opcjonalna drukarka, terminal |

#### Sens dla projektu

AIM-65 jest dobrym rozszerzeniem po KIM-1, bo nadal jest to jednopłytkowy system 6502 z monitorem ROM, ale z innym zestawem urządzeń wejścia/wyjścia.

#### Komponenty wielokrotnego użycia

- ROM monitor loader,
- keypad/display abstraction,
- terminal abstraction,
- PIA/RRIOT/ACIA zależnie od wariantu profilu.

#### Do zrobienia

- [ ] Profil `profiles/computers/aim-65.json`.
- [ ] Uproszczony display/keyboard.
- [ ] Opcjonalnie integracja z drukarką jako `line-printer-device`.

---

### 5.3. SYM-1

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| Podobny do | KIM-1 |
| I/O | keypad/display, VIA/RIOT/ACIA zależnie od konfiguracji |

#### Sens dla projektu

SYM-1 może użyć tych samych abstrakcji co KIM-1 i AIM-65. To dobry target do sprawdzenia, czy profile SBC nie są zbyt mocno dopasowane do jednej maszyny.

#### Do zrobienia

- [ ] Profil `profiles/computers/sym-1.json`.
- [ ] Wspólny model keypad/display.
- [ ] Test uruchomienia monitora, jeśli ROM będzie dostępny legalnie.

---

### 5.4. Microtan 65

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| Podobny do | KIM-1 / Apple-1 |
| I/O | terminal/video zależnie od konfiguracji |

#### Sens dla projektu

Microtan 65 jest dobrym kandydatem do profilu SBC z prostym terminalem lub prostym wyświetlaniem tekstu. Może być użyty jako test elastyczności konfiguracji pamięci i urządzeń.

#### Do zrobienia

- [ ] Profil `profiles/computers/microtan-65.json`.
- [ ] Minimalna mapa RAM/ROM.
- [ ] Terminal albo prosty renderer tekstowy.

---

### 5.5. Ohio Scientific Challenger 1P / Superboard II

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| Podobny do | Apple-1 / PET-like |
| I/O | keyboard, video text, ROM BASIC/monitor |

#### Sens dla projektu

To bardzo dobry kandydat po Apple-1 i przed PET. Maszyna jest bliżej pełnego komputera domowego, ale nie wymaga jeszcze złożonej grafiki klasy C64/Atari.

#### Komponenty wielokrotnego użycia

- `video-text-buffer`,
- keyboard matrix albo prosty keyboard device,
- RAM/ROM profile,
- BASIC ROM loader.

#### Do zrobienia

- [ ] Profil `profiles/computers/ohio-superboard-ii.json`.
- [ ] Text display.
- [ ] Keyboard input.
- [ ] Test programu BASIC albo monitora.

---

### 5.6. Compukit UK101

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Trudność | Średnia |
| CPU | MOS 6502 |
| Podobny do | Ohio Scientific / PET-like |
| I/O | keyboard, video text, ROM BASIC |

#### Sens dla projektu

Compukit UK101 jest blisko koncepcyjnie do Ohio Scientific i może wykorzystać tę samą warstwę prostego tekstowego komputera 6502.

#### Do zrobienia

- [ ] Profil `profiles/computers/compukit-uk101.json`.
- [ ] Wspólny moduł `video-text-buffer`.
- [ ] Wspólny keyboard input.

---

### 5.7. Acorn Atom

| Cecha | Wartość |
|---|---|
| Priorytet | P2/P3 |
| Trudność | Średnia/duża |
| CPU | MOS 6502 |
| Podobny do | Apple II / BBC Micro, ale prostszy |
| I/O | VIA, keyboard, video, ROM BASIC |

#### Sens dla projektu

Acorn Atom jest dobrym pomostem między prostymi SBC/komputerami tekstowymi a bardziej rozbudowanym BBC Micro. Wymaga już lepszej warstwy wideo i klawiatury, ale jest mniej złożony niż BBC Micro.

#### Do zrobienia

- [ ] Profil `profiles/computers/acorn-atom.json`.
- [ ] VIA integration.
- [ ] Keyboard matrix.
- [ ] Text/video renderer.

---

### 5.8. BBC Micro Model A/B

| Cecha | Wartość |
|---|---|
| Priorytet | P3 |
| Trudność | Duża |
| CPU | MOS 6502 |
| Podobny do | Acorn Atom + rozbudowany system I/O |
| I/O | VIA, video ULA, keyboard, sound, OS ROM |

#### Sens dla projektu

BBC Micro jest technicznie wartościowy, ale nie powinien być wczesnym celem. Wymaga rozbudowanego modelu wideo, VIA, OS ROM i dokładniejszej obsługi przerwań.

#### Do zrobienia

- [ ] Profil `profiles/computers/bbc-micro.json`.
- [ ] VIA x2 albo zgodnie z profilem modelu.
- [ ] Video ULA jako osobny układ.
- [ ] OS ROM / BASIC ROM loader.

---

### 5.9. VIC-20

| Cecha | Wartość |
|---|---|
| Priorytet | P3 |
| Trudność | Duża |
| CPU | MOS 6502 |
| Podobny do | PET -> C64, ale nadal bez 6510 |
| I/O | VIC, VIA, keyboard, memory expansion |

#### Sens dla projektu

VIC-20 jest dobrym krokiem przed C64, ponieważ nadal używa 6502, ale wprowadza układ wideo i specyficzne mapowanie pamięci. Pozwala rozwijać komponenty Commodore bez od razu pełnego VIC-II/SID/CIA.

#### Do zrobienia

- [ ] Profil `profiles/computers/vic-20.json`.
- [ ] VIC video chip jako osobny plan.
- [ ] VIA integration.
- [ ] Keyboard matrix.
- [ ] Memory expansion profile.

---

### 5.10. Oric-1 / Oric Atmos

| Cecha | Wartość |
|---|---|
| Priorytet | P3/P4 |
| Trudność | Duża |
| CPU | 6502A |
| Podobny do | Acorn Atom / Apple II |
| I/O | ULA, AY sound, keyboard, cassette |

#### Sens dla projektu

Oric jest ciekawy, ale wymaga układów niestandardowych: ULA, obsługi atrybutów wideo, AY sound i cassette. Nie powinien wyprzedzać PET/Atom/VIC-20.

#### Do zrobienia

- [ ] Profil `profiles/computers/oric-atmos.json`.
- [ ] ULA video plan.
- [ ] AY sound jako osobny chip.
- [ ] Cassette loader.

---

### 5.11. Atari 400/800

| Cecha | Wartość |
|---|---|
| Priorytet | P4 |
| Trudność | Bardzo duża |
| CPU | 6502/SALLY |
| Podobny do | Apple II/C64 jako złożony komputer domowy |
| I/O | ANTIC, GTIA, POKEY, PIA |

#### Sens dla projektu

Atari 8-bit to duży etap, bo wymaga kilku specjalizowanych układów i dokładniejszej synchronizacji obrazu oraz przerwań. Dodać do roadmapy jako przyszły target, ale nie implementować przed zakończeniem prostszych komputerów.

#### Do zrobienia

- [ ] Profil `profiles/computers/atari-800.json`.
- [ ] ANTIC plan.
- [ ] GTIA plan.
- [ ] POKEY plan.
- [ ] PIA integration.

---

### 5.12. Rekomendowana kolejność dodatkowych profili

| Kolejność | Profil | Dlaczego |
|---:|---|---|
| 1 | `aim-65` | rozszerza rodzinę SBC po KIM-1 |
| 2 | `sym-1` | test elastyczności profili SBC |
| 3 | `microtan-65` | kolejny prosty target terminalowy/tekstowy |
| 4 | `ohio-superboard-ii` | pomost między Apple-1 a PET-like |
| 5 | `compukit-uk101` | reuse komponentów Ohio/text display |
| 6 | `acorn-atom` | pomost do BBC Micro |
| 7 | `vic-20` | pomost do C64 przy zachowaniu CPU 6502 |
| 8 | `bbc-micro` | duży, ale logiczny po Atom/VIA/video |
| 9 | `oric-atmos` | ciekawy, ale wymaga ULA/AY/cassette |
| 10 | `atari-800` | duży etap z ANTIC/GTIA/POKEY |

---

## 6. UART jako późniejszy komponent I/O

### 6.1. Cel UART

UART powinien być prostym, generycznym urządzeniem terminalowym dla maszyn testowych i SBC. Nie powinien być częścią CPU.

UART przyda się dla:

- minimalnego SBC 6502,
- testowego monitora ROM,
- prostych programów assemblerowych,
- terminalowego frontendu,
- automatycznych testów wejścia/wyjścia.

---

### 6.2. Minimalny UART simple

#### Mapa rejestrów

```text
base + 0  UART_DATA
base + 1  UART_STATUS
```

#### Status

```text
bit 0 = RX_READY
bit 1 = TX_READY
```

#### Zachowanie

- odczyt `UART_DATA` pobiera bajt z kolejki RX,
- zapis `UART_DATA` wysyła bajt do callbacka TX,
- odczyt `UART_STATUS` zwraca gotowość RX/TX.

---

### 6.3. Proponowany profil urządzenia

```json
{
  "id": "uart0",
  "type": "uart-simple",
  "baseAddress": "0xD000",
  "registers": {
    "data": "0x00",
    "status": "0x01"
  },
  "statusBits": {
    "rxReady": 0,
    "txReady": 1
  },
  "frontend": "terminal"
}
```

---

### 6.4. Docelowe typy urządzeń terminalowych

| Typ | Cel | Priorytet |
|---|---|---:|
| `uart-simple` | testowy SBC, szybki terminal | P1 |
| `acia-6551` | bardziej historyczny UART/ACIA | P2 |
| `pia-6821-terminal` | Apple-1 | P1 |
| `kim-1-keypad-display` | KIM-1 | P2 |
| `video-text-buffer` | PET/Apple II tekst | P3 |

---

## 7. Oddzielenie typów plików

### 7.1. Dokumenty planów

```text
docs/
  faza-15-irq-nmi.md
  faza-16-cycle-stepped.md
  cpu-variants-roadmap.md
  computer-profiles-and-uart-roadmap.md
```

Dokumenty opisują decyzje, plany i roadmapę. Nie są ładowane przez emulator.

### 7.2. Profile komputerów

```text
profiles/computers/*.json
```

Profile komputerów są ładowane przez emulator. Mają stabilny schemat i powinny być walidowane.

### 7.3. Profile CPU

```text
profiles/cpu/*.json
```

Profile CPU opisują warianty CPU i ich cechy. Mogą być ładowane przez emulator albo generowane z kodu.

### 7.4. ROM-y

```text
roms/<machine>/*.bin
```

ROM-y nie powinny być commitowane, jeśli licencja na to nie pozwala. W repo można trzymać tylko placeholdery i instrukcje pozyskania.

### 7.5. Programy assemblerowe

```text
asm/<machine>/*.asm
```

Programy testowe mogą być commitowane, jeśli są własne albo mają zgodną licencję.

---

## 8. Proponowana struktura katalogów

```text
profiles/
  computers/
    minimal-sbc-6502.json
    apple-1.json
    kim-1.json
    aim-65.json
    sym-1.json
    microtan-65.json
    ohio-superboard-ii.json
    compukit-uk101.json
    commodore-pet.json
    acorn-atom.json
    apple-ii.json
    vic-20.json
    bbc-micro.json
    c64.json
  cpu/
    mos6502-nmos.json
    mos6510.json
    ricoh-2a03.json
    wdc65c02.json

roms/
  README.md
  minimal/
  apple-1/
  kim-1/
  aim-65/
  pet/

asm/
  README.md
  minimal-sbc-6502/
  apple-1/
  kim-1/
  pet/

docs/
  computer-profiles-and-uart-roadmap.md
```

---

## 9. Proponowana kolejność realizacji

### Etap 1 — format profilu komputera

- [ ] Dodać schemat `computer-profile/v1`.
- [ ] Dodać loader profili.
- [ ] Dodać walidację wymaganych pól.
- [ ] Dodać profil `minimal-sbc-6502.json`.

### Etap 2 — prosty UART

- [ ] Dodać `IMemoryMappedDevice`.
- [ ] Dodać `UartSimpleDevice`.
- [ ] Dodać `SystemBus` z listą urządzeń.
- [ ] Dodać testy `RX_READY`, `TX_READY`, read/write data.
- [ ] Dodać prosty program assemblerowy echo.

### Etap 3 — Apple-1

- [ ] Dodać profil `apple-1.json`.
- [ ] Dodać `pia-6821-terminal`.
- [ ] Dodać terminal znakowy.
- [ ] Uruchomić WOZ Monitor.

### Etap 4 — KIM-1 i rodzina SBC

- [ ] Dodać profil `kim-1.json`.
- [ ] Dodać uproszczony keypad/display.
- [ ] Później dodać dokładniejszy 6530 RRIOT.
- [ ] Dodać profile `aim-65`, `sym-1`, `microtan-65` jako reuse tej samej infrastruktury.

### Etap 5 — tekstowe komputery domowe

- [ ] Ohio Scientific Superboard II / Challenger 1P.
- [ ] Compukit UK101.
- [ ] Commodore PET.
- [ ] Acorn Atom.

### Etap 6 — większe komputery 6502

- [ ] Apple II.
- [ ] VIC-20.
- [ ] BBC Micro.
- [ ] C64 po 6510 i memory banking.

### Etap 7 — przyszłe złożone targety

- [ ] Oric Atmos.
- [ ] Atari 400/800.
- [ ] NES po Ricoh 2A03.

---

## 10. Definition of Done

Ten obszar można uznać za uporządkowany, gdy:

- [ ] profile komputerów są osobnym typem plików pod `profiles/computers`,
- [ ] dokumenty roadmapy nie są używane jako konfiguracja runtime,
- [ ] CPU nie zna nazw komputerów,
- [ ] urządzenia I/O są podłączane przez memory bus,
- [ ] istnieje minimalny profil SBC 6502,
- [ ] istnieje prosty UART jako urządzenie memory-mapped,
- [ ] Apple-1 używa PIA/terminala zamiast generycznego UART, jeśli celem jest zgodność historyczna,
- [ ] rodzina SBC może współdzielić keypad/display/terminal abstractions,
- [ ] tekstowe komputery domowe współdzielą `video-text-buffer` i keyboard abstraction,
- [ ] C64 jest traktowany jako osobny profil maszyny wymagający 6510 i układów specjalizowanych.
