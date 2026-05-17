# Roadmapa wariantów CPU 6502

Data: 2026-05-17  
Zakres: warianty CPU zgodne lub częściowo zgodne z rodziną 6502, możliwe do dodania do emulatora jako profile procesora.

---

## 1. Cel dokumentu

Celem jest rozdzielenie implementacji rdzenia 6502 od wariantów CPU. Projekt powinien mieć bazowy rdzeń `MOS 6502`, a kolejne warianty powinny rozszerzać lub konfigurować zachowanie CPU bez duplikowania całej implementacji instrukcji.

Najważniejsza zasada: większość wariantów 6502 różni się nie zestawem podstawowych instrukcji, ale detalami elektrycznymi, portami I/O, mapowaniem pamięci, dodatkowymi opcode, trybem decimal, timingiem lub nieudokumentowanymi instrukcjami.

---

## 2. Proponowany model architektury

### 2.1. Enum wariantu CPU

```csharp
public enum CpuVariant
{
    Mos6502,
    Mos6502Nmos,
    Mos6502Cmos,
    Mos6502Ricoh2A03,
    Mos6510,
    Mos8500,
    Wdc65C02,
    Wdc65C816,
    Rockwell65C02,
    Synertek6502,
    Ricoh2A07
}
```

### 2.2. Deskryptor wariantu

```csharp
public sealed record CpuVariantDescriptor(
    CpuVariant Variant,
    string Name,
    bool SupportsDecimalMode,
    bool SupportsIllegalOpcodes,
    bool HasIntegratedIoPort,
    bool IsCmos,
    bool Is16BitCapable,
    IReadOnlySet<byte> ExtraOpcodes,
    IReadOnlySet<byte> DisabledOpcodes);
```

### 2.3. Konfiguracja CPU

```csharp
public sealed record CpuOptions(
    CpuVariant Variant,
    bool ThrowOnUnknownOpcode = true,
    bool EnableIllegalOpcodes = false,
    bool EnableCycleAccuracy = false);
```

---

## 3. Wariant bazowy: MOS 6502 / NMOS 6502

| Cecha | Wartość |
|---|---|
| Priorytet | P0 |
| Status | Obecna baza projektu |
| Tryb decimal | Tak |
| Illegal opcodes | Tak, ale jeszcze niezaimplementowane |
| Cycle accuracy | Docelowo faza 16 |
| Typowe maszyny | Apple I, Apple II, KIM-1, BBC Micro, Atari 2600/800 pochodne zależnie od wersji |

### Zakres

To powinien być główny rdzeń projektu. Wszystkie oficjalne instrukcje, addressing modes, interrupt sequence, reset sequence, page crossing, R-M-W double write i bug `JMP ($xxFF)` powinny być poprawne dla NMOS 6502.

### Do zrobienia

- [ ] Zakończyć official opcodes.
- [ ] Ustabilizować BCD.
- [ ] Dodać cycle-stepped.
- [ ] Dodać illegal opcodes jako opcjonalny tryb.
- [ ] Dodać testy Klaus Dormann / nestest / Wolfgang Lorenz.

---

## 4. MOS 6510

| Cecha | Wartość |
|---|---|
| Priorytet | P1 |
| Złożoność | Mała dla CPU, średnia dla memory banking |
| Typowe maszyny | Commodore 64 |
| Instrukcje | Jak NMOS 6502 |
| Różnica główna | Wbudowany 6-bitowy port I/O pod `$0000/$0001` |

### Różnice względem 6502

6510 jest bardzo bliski NMOS 6502. Główna różnica to wbudowany port I/O:

- `$0000` — Data Direction Register,
- `$0001` — Data Register,
- linie portu sterują mapowaniem RAM/ROM/I/O w C64.

### Do zrobienia

- [ ] Dodać `CpuVariant.Mos6510`.
- [ ] Dodać `I6510Port`.
- [ ] Dodać obsługę adresów `$0000/$0001` w busie albo jako urządzenie memory-mapped.
- [ ] Dodać testy DDR/data register.
- [ ] Dodać osobny `C64MemoryBus` dla bankowania pamięci.

### Nie robić w samym CPU

- VIC-II,
- SID,
- CIA,
- pełne mapowanie C64,
- cartridge logic.

To są elementy profilu komputera, nie rdzenia CPU.

---

## 5. MOS 8500 / 8502

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Złożoność | Mała po 6510 |
| Typowe maszyny | C64C, C128 |
| Instrukcje | Jak 6510 / NMOS 6502 |
| Różnica główna | Technologia HMOS, inne parametry elektryczne, w 8502 wyższe taktowanie |

### Zakres

Dla emulatora instrukcyjnego 8500 jest praktycznie wariantem 6510. 8502 z C128 może wymagać profilu taktowania i trybu 2 MHz, ale nie wymaga dużej zmiany instruction set.

### Do zrobienia

- [ ] Dodać alias/variant `Mos8500` oparty o `Mos6510`.
- [ ] Dodać `Mos8502` opcjonalnie dla C128.
- [ ] Dodać taktowanie jako parametr profilu maszyny, nie CPU.

---

## 6. Ricoh 2A03 / 2A07

| Cecha | Wartość |
|---|---|
| Priorytet | P2 |
| Złożoność | Średnia |
| Typowe maszyny | NES / Famicom |
| Instrukcje | Bazowo 6502 |
| Różnica główna | Brak trybu decimal, zintegrowane APU, inne otoczenie systemowe |

### Różnice względem 6502

Ricoh 2A03 to wariant 6502 używany w NTSC NES/Famicom. 2A07 to wariant PAL. Najważniejsza różnica dla CPU: tryb decimal nie działa jak w standardowym 6502. Flaga `D` może istnieć, ale arytmetyka BCD nie jest wspierana.

### Do zrobienia

- [ ] Dodać `CpuVariant.Ricoh2A03`.
- [ ] Dodać `CpuVariant.Ricoh2A07`.
- [ ] W descriptorze ustawić `SupportsDecimalMode = false`.
- [ ] Dla `ADC/SBC` ignorować decimal mode.
- [ ] Dodać test: `SED` nie przełącza arytmetyki na BCD.
- [ ] APU traktować jako osobne urządzenie, nie część CPU core.

### Uwaga

`nestest` jest przydatny właśnie dla tego wariantu, ale wymaga NES-owego harnessu pamięci i wektorów.

---

## 7. WDC 65C02

| Cecha | Wartość |
|---|---|
| Priorytet | P2/P3 |
| Złożoność | Średnia |
| Typowe maszyny | Apple IIc, BBC Master, systemy embedded |
| Instrukcje | CMOS 6502 + dodatkowe opcode |
| Różnica główna | Poprawione błędy NMOS, nowe instrukcje/adresowania |

### Różnice względem NMOS 6502

65C02 poprawia część zachowań NMOS i dodaje instrukcje. Typowe różnice:

- nowe instrukcje: `BRA`, `STZ`, `TRB`, `TSB`, `PHX`, `PLX`, `PHY`, `PLY`, `INC A`, `DEC A`, `RMB`, `SMB`, `BBR`, `BBS`, zależnie od producenta,
- poprawiony `JMP (abs)` bug,
- inne zachowanie części nieudokumentowanych opcode,
- bardziej zdefiniowany stan po resecie,
- zwykle brak NMOS illegal opcodes.

### Do zrobienia

- [ ] Dodać `CpuVariant.Wdc65C02`.
- [ ] Dodać opcję `IsCmos = true`.
- [ ] Wyłączyć NMOS illegal opcodes.
- [ ] Dodać dodatkowe instrukcje 65C02.
- [ ] Warunkowo wyłączyć bug `JMP ($xxFF)`.
- [ ] Dodać osobne testy 65C02.

---

## 8. Rockwell 65C02 / R65C02

| Cecha | Wartość |
|---|---|
| Priorytet | P3 |
| Złożoność | Średnia po WDC 65C02 |
| Instrukcje | CMOS 6502 + instrukcje bitowe Rockwell |
| Różnica główna | Dodatkowe operacje bit branch/set/reset |

### Zakres

Rockwell 65C02 ma istotne rozszerzenia bitowe, m.in. `RMB`, `SMB`, `BBR`, `BBS`. Jeśli WDC 65C02 zostanie zaimplementowany przez metadane opcode, Rockwell powinien być tylko wariantem zestawu opcode.

### Do zrobienia

- [ ] Dodać `CpuVariant.Rockwell65C02`.
- [ ] Dodać zestaw opcode Rockwell.
- [ ] Dodać testy bit branch.

---

## 9. Synertek 6502

| Cecha | Wartość |
|---|---|
| Priorytet | P3 |
| Złożoność | Mała |
| Instrukcje | Zwykle jak NMOS 6502 |
| Różnica główna | Producent/rewizja, potencjalne niuanse elektryczne/timingowe |

### Zakres

Dla emulatora programowego Synertek można traktować jako profil NMOS 6502, chyba że projekt będzie odtwarzał bardzo szczegółowe różnice rewizji krzemu.

### Do zrobienia

- [ ] Dodać alias `Synertek6502` do NMOS 6502.
- [ ] Nie duplikować implementacji.

---

## 10. WDC 65C816 / 65816

| Cecha | Wartość |
|---|---|
| Priorytet | P4 |
| Złożoność | Duża |
| Typowe maszyny | Apple IIGS, SNES/Super Famicom |
| Instrukcje | Rozszerzony 65C02/65816 |
| Różnica główna | Tryb 16-bit, banki pamięci, emulation/native mode |

### Różnice względem 6502

65C816 to nie jest mała wariacja 6502. To większy procesor zgodny w trybie emulacji, ale z trybem natywnym:

- rejestry mogą mieć 8 albo 16 bitów,
- 24-bitowe adresowanie,
- banki pamięci,
- dodatkowe rejestry,
- tryb emulation/native,
- większy zestaw instrukcji.

### Rekomendacja

Nie implementować przed zakończeniem pełnego NMOS 6502 i ewentualnie 65C02. To powinien być osobny duży etap albo osobny rdzeń.

### Do zrobienia w przyszłości

- [ ] Osobny `Cpu65816` albo silnie konfigurowalny core.
- [ ] 24-bit address bus.
- [ ] Bank registers.
- [ ] Native/emulation mode.
- [ ] 8/16-bit accumulator/index modes.

---

## 11. HuC6280

| Cecha | Wartość |
|---|---|
| Priorytet | P4 |
| Złożoność | Duża |
| Typowe maszyny | PC Engine / TurboGrafx-16 |
| Bazuje na | 65C02 |
| Różnica główna | Dodatkowe instrukcje, MMU, timer, port I/O |

### Zakres

HuC6280 jest ciekawy, ale nie jest dobrym następnym krokiem. Wymaga osobnego modelu pamięci, MMU i urządzeń.

### Do zrobienia w przyszłości

- [ ] Najpierw pełny 65C02.
- [ ] Potem MMU HuC6280.
- [ ] Potem dodatkowe instrukcje i timer.

---

## 12. 2A03 vs 6502 vs 6510 — szybkie porównanie

| Cecha | MOS 6502 | MOS 6510 | Ricoh 2A03 |
|---|---:|---:|---:|
| Oficjalne instrukcje 6502 | Tak | Tak | Tak |
| Decimal mode BCD | Tak | Tak | Nie |
| Illegal opcodes NMOS | Tak | Tak | Częściowo jak NMOS, zależnie od testów |
| Wbudowany port I/O | Nie | Tak | APU / I/O specyficzne dla NES |
| Główna maszyna | Apple/KIM/Atari/BBC | C64 | NES |
| Trudność po 6502 | — | Mała/średnia | Średnia |

---

## 13. Priorytet implementacji

| Kolejność | Wariant | Dlaczego |
|---:|---|---|
| 1 | `Mos6502Nmos` | Baza projektu i większości testów |
| 2 | `Mos6510` | Małe zmiany CPU, ważne dla C64 |
| 3 | `Ricoh2A03` | Przydatne do nestest/NES CPU, wyłącza decimal mode |
| 4 | `Mos8500/Mos8502` | Naturalne po 6510 |
| 5 | `Wdc65C02` | Większy zestaw instrukcji, ale nadal logiczne rozszerzenie |
| 6 | `Rockwell65C02` | Po 65C02 jako wariant opcode |
| 7 | `Wdc65C816` | Duży osobny etap |
| 8 | `HuC6280` | Duży osobny etap po 65C02 |

---

## 14. Minimalny plan wdrożenia wariantów

### Etap 1 — infrastruktura wariantów

- [ ] Dodać `CpuVariant`.
- [ ] Dodać `CpuOptions`.
- [ ] Dodać `CpuVariantDescriptor`.
- [ ] Przenieść cechy CPU do descriptorów: decimal mode, illegal opcodes, CMOS/NMOS, quirks.
- [ ] Dodać testy descriptorów.

### Etap 2 — MOS 6502 jako wariant jawny

- [ ] Oznaczyć obecną implementację jako `Mos6502Nmos`.
- [ ] Dodać testy, że NMOS ma BCD i `JMP ($xxFF)` bug.

### Etap 3 — MOS 6510

- [ ] Dodać port `$0000/$0001`.
- [ ] Dodać testy portu.
- [ ] Nie implementować jeszcze pełnego C64.

### Etap 4 — Ricoh 2A03

- [ ] Wyłączyć decimal arithmetic.
- [ ] Dodać testy `SED` + `ADC/SBC`.
- [ ] Przygotować pod `nestest`.

### Etap 5 — CMOS 65C02

- [ ] Dodać descriptor CMOS.
- [ ] Wyłączyć NMOS quirks tam, gdzie trzeba.
- [ ] Dodać nowe instrukcje partiami.

---

## 15. Definition of Done dla wariantów CPU

- [ ] Każdy wariant ma descriptor.
- [ ] Każdy wariant ma testy cech różnicujących.
- [ ] Różnice wariantów nie są zaszyte w instrukcjach przez losowe `if`, tylko wynikają z konfiguracji/descriptorów.
- [ ] Warianty CPU są oddzielone od profili komputerów.
- [ ] `6510` nie oznacza automatycznie pełnego C64.
- [ ] `2A03` nie oznacza automatycznie pełnego NES.
- [ ] `65C816` jest traktowany jako osobny duży etap, nie drobna modyfikacja.
