# Indeks dokumentów planistycznych

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument porządkujący

---

## 1. Cel

Ten dokument porządkuje istniejące dokumenty planistyczne i architektoniczne. Celem jest rozdzielenie:

- dokumentów oceny,
- dokumentów roadmapy,
- dokumentów implementacyjnych,
- profili runtime,
- przyszłych schematów JSON.

Dokumenty w `docs/` nie są konfiguracją uruchomieniową emulatora. Konfiguracją runtime powinny być pliki pod `profiles/`.

---

## 2. Dokumenty architektoniczne nadrzędne

| Dokument | Rola |
|---|---|
| `docs/modular-computer-composition-architecture.md` | nadrzędna architektura składania komputerów z CPU, busa, pamięci i urządzeń |
| `docs/universal-emulation-elements-roadmap.md` | brakujące elementy uniwersalne dla wielu CPU: address spaces, CPU features, trace, sygnały, DMA, bankowanie |
| `docs/video-audio-emulation-roadmap.md` | architektura grafiki, dźwięku, terminala tekstowego, buforów i adapterów PC |
| `docs/computer-profiles-and-uart-roadmap.md` | koncepcja profili komputerów, UART i lista kandydatów maszyn |
| `docs/io-chip-implementation-roadmap.md` | roadmapa układów wejścia/wyjścia |

---

## 3. Dokumenty dla konkretnych maszyn

| Dokument | Rola |
|---|---|
| `docs/apple-1-pia-terminal-plan.md` | plan Apple-1 PIA terminal `$D010-$D013` |
| `docs/pet-chip-implementation-plans.md` | plan układów Commodore PET |
| `docs/tty-mainframe-link-plan.md` | plan TTY/UART/mainframe link |

---

## 4. Dokumenty oceny i feedbacku

| Dokument | Rola |
|---|---|
| `docs/zbiorcza-lista-poprawek-2026-05-17.md` | zbiorcza lista poprawek dla zaimplementowanej części |

Jeżeli powstaną kolejne przeglądy, zalecany format nazwy:

```text
docs/reviews/<yyyy-mm-dd>-<topic>.md
```

Przykład:

```text
docs/reviews/2026-05-17-implementation-review.md
```

---

## 5. Docelowy podział katalogów

### 5.1. Dokumenty

```text
docs/
  architecture/
  roadmaps/
  plans/
  reviews/
```

### 5.2. Profile runtime

```text
profiles/
  computers/
  cpu/
  devices/
```

### 5.3. ROM-y

```text
roms/
  README.md
  apple-1/
  pet/
  kim-1/
  custom-sbc/
```

ROM-y nie powinny być commitowane, jeśli licencja na to nie pozwala.

### 5.4. Programy assemblerowe i testowe

```text
asm/
  minimal-sbc-6502/
  apple-1/
  pet/
```

---

## 6. Proponowana klasyfikacja dokumentów

### 6.1. Architecture

Dokumenty opisujące decyzje systemowe:

```text
docs/architecture/modular-computer-composition-architecture.md
docs/architecture/universal-emulation-elements-roadmap.md
docs/architecture/video-audio-emulation-roadmap.md
```

### 6.2. Roadmaps

Dokumenty opisujące kolejność i priorytety:

```text
docs/roadmaps/computer-profiles-and-uart-roadmap.md
docs/roadmaps/io-chip-implementation-roadmap.md
```

### 6.3. Plans

Dokumenty opisujące implementację konkretnego obszaru:

```text
docs/plans/apple-1-pia-terminal-plan.md
docs/plans/pet-chip-implementation-plans.md
docs/plans/tty-mainframe-link-plan.md
```

### 6.4. Reviews

Dokumenty feedbacku i oceny:

```text
docs/reviews/zbiorcza-lista-poprawek-2026-05-17.md
```

---

## 7. Rekomendacja migracji dokumentów

Na razie można zostawić pliki w `docs/`, żeby nie robić dużej reorganizacji ścieżek. Przy kolejnym porządkowaniu warto przenieść je do podkatalogów:

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

## 8. Najważniejsza kolejność czytania

Dla nowej osoby w projekcie:

1. `docs/modular-computer-composition-architecture.md`
2. `docs/universal-emulation-elements-roadmap.md`
3. `docs/video-audio-emulation-roadmap.md`
4. `docs/io-chip-implementation-roadmap.md`
5. `docs/computer-profiles-and-uart-roadmap.md`
6. `docs/apple-1-pia-terminal-plan.md`
7. `docs/pet-chip-implementation-plans.md`
8. `docs/tty-mainframe-link-plan.md`
9. `docs/zbiorcza-lista-poprawek-2026-05-17.md`

---

## 9. Decyzja porządkująca

Od tego momentu:

- nowe decyzje systemowe trafiają do `architecture`,
- kolejność i priorytety do `roadmaps`,
- konkretne implementacje do `plans`,
- oceny kodu do `reviews`,
- dane uruchomieniowe do `profiles`,
- ROM-y do `roms`,
- programy testowe do `asm`.

---

## 10. Następny krok

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
IVideoDevice
VideoFrame
IAudioDevice
AudioRingBuffer
SimpleFramebufferDevice
BeeperDevice
```

Dopiero po tym warto implementować kolejne układy i profile komputerów.
