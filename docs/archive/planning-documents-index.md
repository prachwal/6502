# Indeks dokumentów planistycznych

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: techniczny indeks porządkujący / plan migracji

---

## 1. Cel

Ten dokument jest technicznym indeksem porządkującym dokumenty planistyczne i architektoniczne.

Głównym wejściem do dokumentacji jest:

```text
docs/README.md
```

Ten plik pozostaje jako roboczy plan migracji i klasyfikacji dokumentów.

Celem jest rozdzielenie:

- dokumentów architektonicznych,
- roadmap,
- planów implementacyjnych,
- przeglądów i feedbacku,
- profili runtime,
- przyszłych schematów JSON.

Dokumenty w `docs/` nie są konfiguracją uruchomieniową emulatora. Konfiguracją runtime powinny być pliki pod `profiles/`.

---

## 2. Aktualna klasyfikacja dokumentów

### 2.1. Architektura

| Dokument | Rola |
|---|---|
| `docs/modular-computer-composition-architecture.md` | nadrzędna architektura składania komputerów z CPU, busa, pamięci i urządzeń |
| `docs/universal-emulation-elements-roadmap.md` | elementy wspólne dla wielu CPU, address spaces, CPU features, trace, sygnały, DMA, bankowanie, szybki runtime |
| `docs/video-audio-emulation-roadmap.md` | architektura grafiki, dźwięku, terminala tekstowego, buforów i adapterów PC |

### 2.2. Roadmapy

| Dokument | Rola |
|---|---|
| `docs/computer-profiles-and-uart-roadmap.md` | koncepcja profili komputerów, UART i lista kandydatów maszyn |
| `docs/io-chip-implementation-roadmap.md` | roadmapa układów wejścia/wyjścia |

### 2.3. Plany konkretnych obszarów

| Dokument | Rola |
|---|---|
| `docs/apple-1-pia-terminal-plan.md` | plan Apple-1 PIA terminal `$D010-$D013` |
| `docs/pet-chip-implementation-plans.md` | plan układów Commodore PET |
| `docs/tty-mainframe-link-plan.md` | plan TTY/UART/mainframe link |

### 2.4. Przeglądy i feedback

| Dokument | Rola |
|---|---|
| `docs/zbiorcza-lista-poprawek-2026-05-17.md` | zbiorcza lista poprawek dla zaimplementowanej części |

---

## 3. Docelowy podział katalogów

### 3.1. Dokumenty

```text
docs/
  README.md
  architecture/
  roadmaps/
  plans/
  reviews/
  profiles-spec/
```

### 3.2. Profile runtime

```text
profiles/
  computers/
  cpu/
  devices/
```

### 3.3. ROM-y

```text
roms/
  README.md
  apple-1/
  pet/
  kim-1/
  custom-sbc/
```

ROM-y nie powinny być commitowane, jeśli licencja na to nie pozwala.

### 3.4. Programy assemblerowe i testowe

```text
asm/
  minimal-sbc-6502/
  apple-1/
  pet/
```

---

## 4. Rekomendowana migracja dokumentów

Na razie pliki mogą zostać płasko w `docs/`, ale przy najbliższej większej reorganizacji należy przenieść je do podkatalogów:

```text
docs/modular-computer-composition-architecture.md
  -> docs/architecture/modular-computer-composition-architecture.md

docs/universal-emulation-elements-roadmap.md
  -> docs/architecture/universal-emulation-elements-roadmap.md

docs/video-audio-emulation-roadmap.md
  -> docs/architecture/video-audio-emulation-roadmap.md

docs/computer-profiles-and-uart-roadmap.md
  -> docs/roadmaps/computer-profiles-and-uart-roadmap.md

docs/io-chip-implementation-roadmap.md
  -> docs/roadmaps/io-chip-implementation-roadmap.md

docs/apple-1-pia-terminal-plan.md
  -> docs/plans/apple-1-pia-terminal-plan.md

docs/pet-chip-implementation-plans.md
  -> docs/plans/pet-chip-implementation-plans.md

docs/tty-mainframe-link-plan.md
  -> docs/plans/tty-mainframe-link-plan.md

docs/zbiorcza-lista-poprawek-2026-05-17.md
  -> docs/reviews/zbiorcza-lista-poprawek-2026-05-17.md
```

---

## 5. Kolejność czytania

Dla nowej osoby w projekcie:

1. `docs/README.md`
2. `docs/modular-computer-composition-architecture.md`
3. `docs/universal-emulation-elements-roadmap.md`
4. `docs/video-audio-emulation-roadmap.md`
5. `docs/io-chip-implementation-roadmap.md`
6. `docs/computer-profiles-and-uart-roadmap.md`
7. `docs/apple-1-pia-terminal-plan.md`
8. `docs/pet-chip-implementation-plans.md`
9. `docs/tty-mainframe-link-plan.md`
10. `docs/zbiorcza-lista-poprawek-2026-05-17.md`

---

## 6. Zasady utrzymania dokumentacji

1. `docs/README.md` jest głównym wejściem do dokumentacji.
2. Ten plik jest technicznym indeksem i planem migracji.
3. Nowe decyzje systemowe trafiają do dokumentów architektury.
4. Nowe kolejności implementacji trafiają do roadmap.
5. Szczegółowe plany układów i maszyn trafiają do planów.
6. Oceny kodu i listy poprawek trafiają do reviews.
7. Profile runtime nie trafiają do `docs/`.
8. ROM-y nie trafiają do `docs/`.
9. Programy testowe i assemblerowe nie trafiają do `docs/`.
10. Nie powielać tej samej decyzji w kilku dokumentach; dokument podrzędny powinien odwoływać się do dokumentu nadrzędnego.

---

## 7. Najbliższy krok implementacyjny

Najbliższy sensowny krok implementacyjny:

```text
ICpuCore
ISystemBus
IDevice
IMemoryMappedDevice
IPortMappedDevice
ICycleDevice
IResettableDevice
CpuSnapshot
AddressSpaceDescriptor
CpuFeatureDescriptor
ComputerProfile
ComputerBuilder
UartSimpleDevice
ITextDisplayDevice
TextScreenSnapshot
```

Po tym warto dodać:

```text
RuntimeBus
CompiledMemoryMap
CompiledPortMap
IVideoDevice
VideoFrame
IAudioDevice
AudioRingBuffer
SimpleFramebufferDevice
BeeperDevice
```

Dopiero po tym warto implementować kolejne układy i profile komputerów.
