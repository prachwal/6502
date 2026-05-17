# Dokumentacja projektu 6502

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: główny indeks dokumentacji

---

## 1. Cel

Ten katalog zawiera dokumentację architektoniczną, roadmapy, plany implementacyjne i przeglądy projektu emulatora.

Dokumentacja nie jest konfiguracją runtime. Pliki uruchomieniowe powinny znajdować się poza `docs/`, głównie w:

```text
profiles/
roms/
asm/
```

---

## 2. Kolejność czytania

Dla nowej osoby w projekcie zalecana kolejność:

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

## 3. Klasyfikacja dokumentów

### 3.1. Architektura

| Dokument | Rola |
|---|---|
| `docs/modular-computer-composition-architecture.md` | nadrzędna architektura składania komputerów z wymiennych komponentów |
| `docs/universal-emulation-elements-roadmap.md` | elementy wspólne dla wielu CPU i szybkiego runtime |
| `docs/video-audio-emulation-roadmap.md` | architektura grafiki, dźwięku, terminala tekstowego i adapterów PC |

### 3.2. Roadmapy

| Dokument | Rola |
|---|---|
| `docs/computer-profiles-and-uart-roadmap.md` | profile komputerów, UART i lista kandydatów maszyn |
| `docs/io-chip-implementation-roadmap.md` | roadmapa układów wejścia/wyjścia |

### 3.3. Plany konkretnych obszarów

| Dokument | Rola |
|---|---|
| `docs/apple-1-pia-terminal-plan.md` | plan Apple-1 PIA terminal `$D010-$D013` |
| `docs/pet-chip-implementation-plans.md` | plan układów Commodore PET |
| `docs/tty-mainframe-link-plan.md` | plan TTY/UART/mainframe link |

### 3.4. Przeglądy i feedback

| Dokument | Rola |
|---|---|
| `docs/zbiorcza-lista-poprawek-2026-05-17.md` | zbiorcza lista poprawek dla zaimplementowanej części |
| `docs/planning-documents-index.md` | techniczny indeks porządkujący i plan migracji dokumentów |

---

## 4. Docelowy podział katalogów

Docelowo dokumenty powinny zostać przeniesione do podkatalogów:

```text
docs/
  README.md
  architecture/
  roadmaps/
  plans/
  reviews/
  profiles-spec/
```

Obecny stan jest przejściowy: pliki leżą płasko w `docs/`, ale są już sklasyfikowane i opisane.

---

## 5. Zasady dla nowych dokumentów

1. Nowe decyzje systemowe trafiają do `architecture`.
2. Kolejność i priorytety implementacji trafiają do `roadmaps`.
3. Konkretne plany układów, maszyn i integracji trafiają do `plans`.
4. Oceny kodu i listy poprawek trafiają do `reviews`.
5. Specyfikacje profili JSON trafiają do `profiles-spec`.
6. Profile runtime nie trafiają do `docs/`, tylko do `profiles/`.
7. ROM-y nie trafiają do `docs/`, tylko do `roms/`.
8. Programy assemblerowe i testowe trafiają do `asm/`.

---

## 6. Najbliższy kierunek implementacji

Najbliższy sensowny krok:

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

Następny etap:

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

---

## 7. Decyzja porządkująca

Na tym etapie nie trzeba przepisywać treści dokumentów. Priorytetem jest:

1. utrzymanie jednego głównego wejścia przez `docs/README.md`,
2. niedublowanie nowych decyzji w wielu dokumentach,
3. stopniowe przeniesienie plików do podkatalogów,
4. oddzielenie dokumentacji od profili runtime.
