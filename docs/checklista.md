# Checklista faz implementacji symulatora 6502

Legenda statusu: `[ ]` = nie rozpoczęte | `[~]` = w trakcie | `[x]` = zakończone | `[s]` = pominięte

## Legenda wskaźników

| Wskaźnik | Opis |
|----------|------|
| **Pokrycie dokumentacji** | % dokumentacji głównej, który dana faza implementuje |
| **Pokrycie całości** | Skumulowany % pełnej specyfikacji 6502 (instrukcje + timing + przerwania + quirk) |

Od fazy 24 pokrycie CPU pozostaje na 100%. Kolejne fazy dotyczą budowy kompletnych komputerów, API runtime, profili i urządzeń I/O.

---

| # | Faza | Plik | Status | % pokrycia dok. | % pokrycia całości |
|---|------|------|--------|----------------:|-------------------:|
| 0 | Szkielet projektu i struktura .NET | [faza-00-szkielet.md](completed/faza-00-szkielet.md) | [x] | 100% | 1% |
| 1 | Load / Store — LDA, LDX, LDY, STA, STX, STY | [faza-01-load-store.md](completed/faza-01-load-store.md) | [x] | 100% | 3% |
| 2 | Transfer między rejestrami — TAX, TAY, TSX, TXA, TXS, TYA | [faza-02-transfer.md](completed/faza-02-transfer.md) | [x] | 3% | 5% |
| 3 | Flagi Set/Clear — CLC, SEC, CLD, SED, CLI, SEI, CLV | [faza-03-flags.md](completed/faza-03-flags.md) | [x] | 3% | 7% |
| 4 | Arytmetyka binarna — ADC, SBC (bez BCD) | [faza-04-arithmetic.md](completed/faza-04-arithmetic.md) | [x] | 5% | 10% |
| 5 | Inkrementacja / Dekrementacja — INC, DEC, INX, INY, DEX, DEY | [faza-05-inc-dec.md](completed/faza-05-inc-dec.md) | [x] | 3% | 13% |
| 6 | Porównania i BIT — CMP, CPX, CPY, BIT | [faza-06-compare-bit.md](completed/faza-06-compare-bit.md) | [x] | 3% | 16% |
| 7 | Operacje logiczne — AND, ORA, EOR | [faza-07-logic.md](completed/faza-07-logic.md) | [x] | 3% | 19% |
| 8 | Przesunięcia i rotacje — ASL, LSR, ROL, ROR | [faza-08-shift-rotate.md](completed/faza-08-shift-rotate.md) | [x] | 4% | 23% |
| 9 | Skoki i rozgałęzienia — JMP, JSR, RTS, BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS | [faza-09-branch-jump.md](completed/faza-09-branch-jump.md) | [x] | 6% | 30% |
| 10 | Stos i NOP — PHA, PHP, PLA, PLP, NOP | [faza-10-stack-nop.md](completed/faza-10-stack-nop.md) | [x] | 3% | 33% |
| 11 | Przerwania software — BRK, RTI | [faza-11-brk-rti.md](completed/faza-11-brk-rti.md) | [x] | 3% | 37% |
| 12 | Pełne tryby adresowania + page crossing | [faza-12-addressing.md](completed/faza-12-addressing.md) | [x] | 5% | 42% |
| 13 | Tryb BCD — ADC/SBC decimal mode | [faza-13-bcd.md](completed/faza-13-bcd.md) | [x] | 3% | 46% |
| 14 | Sekwencja RESET | [faza-14-reset.md](completed/faza-14-reset.md) | [x] | 3% | 50% |
| 15 | Przerwania sprzętowe — IRQ, NMI (podstawowa obsługa) | [faza-15-irq-nmi.md](completed/faza-15-irq-nmi.md) | [x] | 5% | 56% |
| 16 | Architektura cycle-stepped — dispatch cykli i StepInstruction API | [faza-16-cycle-stepped.md](completed/faza-16-cycle-stepped.md) | [x] | 6% | 63% |
| 17 | R-M-W double write + quirk JMP indirect | [faza-17-rmw-quirks.md](completed/faza-17-rmw-quirks.md) | [x] | 4% | 68% |
| 18 | Nieudokumentowane opkody — stabilne | [faza-18-illegal-stable.md](completed/faza-18-illegal-stable.md) | [x] | 8% | 76% |
| 18C | Nieudokumentowane opkody stabilne - ANC, ALR, ARR, SBX, LAS | [faza-18C-nieudokumentowane-opkody-stabilne.md](completed/faza-18C-nieudokumentowane-opkody-stabilne.md) | [x] | 2% | 78% |
| 19 | Nieudokumentowane opkody — niestabilne + NOP + KIL | [faza-19-illegal-unstable.md](completed/faza-19-illegal-unstable.md) | [x] | 6% | 84% |
| 20 | Test zgodności — nestest | [faza-20-nestest.md](completed/faza-20-nestest.md) | [x] | 4% | 88% |
| 21 | Test zgodności — Klaus Dormann Functional Test | [faza-21-klaus.md](completed/faza-21-klaus.md) | [x] | 4% | 93% |
| 22 | Test zgodności — Wolfgang Lorenz (pomijana, wymaga logiki C64) | [faza-22-wolfgang.md](faza-22-wolfgang.md) | [s] | 5% | 97% |
| 23 | Test zgodności — perfect6502 (opcjonalnie) | [faza-23-perfect6502.md](completed/faza-23-perfect6502.md) | [x] | 3% | 100% |
| 24 | Abstrakcje runtime i API komputera | [faza-24-runtime-abstractions.md](faza-24-runtime-abstractions.md) | [ ] | 0% | 100% |
| 25 | SystemBus i mapa pamięci | [faza-25-system-bus-memory-map.md](faza-25-system-bus-memory-map.md) | [ ] | 0% | 100% |
| 26 | Profile komputerów i builder runtime | [faza-26-computer-profiles.md](faza-26-computer-profiles.md) | [ ] | 0% | 100% |
| 27 | Abstrakcje terminala tekstowego | [faza-27-terminal-abstractions.md](faza-27-terminal-abstractions.md) | [ ] | 0% | 100% |
| 28 | MOS 6820/6821 PIA medium implementation | [faza-28-mos682x-pia-medium.md](faza-28-mos682x-pia-medium.md) | [ ] | 0% | 100% |
| 29 | Apple-1 jako profil na generycznej PIA | [faza-29-apple1-profile-wozmon.md](faza-29-apple1-profile-wozmon.md) | [ ] | 0% | 100% |
| 30 | PET-ready PIA bindings i drugi profil walidacyjny | [faza-30-pet-ready-pia-bindings.md](faza-30-pet-ready-pia-bindings.md) | [ ] | 0% | 100% |
| 31 | API uruchamiania Apple-1 i test end-to-end | [faza-31-apple1-runtime-api.md](faza-31-apple1-runtime-api.md) | [ ] | 0% | 100% |
| 32 | Profile smoke dla wielu architektur | [faza-32-cross-architecture-smoke-profiles.md](faza-32-cross-architecture-smoke-profiles.md) | [ ] | 0% | 100% |

---

## Postęp faz

```
Całkowity postęp:     24 / 34 pozycji roadmapy (71%)

Fazy zakończone   [x]: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 18C, 19, 20, 21, 23
Fazy w trakcie     [~]:
Fazy pominięte    [s]: 22
Fazy nie rozpoczęte [ ]: 24, 25, 26, 27, 28, 29, 30, 31, 32
```
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `tests/Cpu6502.Tests/ShiftRotateTests.cs` - 14 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 143/143 (100%)

---

## Faza 7 (2026-05-16)

Implementacja 24 instrukcji operacji logicznych (AND, ORA, EOR):

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| AND #imm | $29 | A &= M | Immediate | 2 |
| AND zp | $25 | A &= M | Zero Page | 3 |
| AND zp,X | $35 | A &= M | Zero Page,X | 4 |
| AND abs | $2D | A &= M | Absolute | 4 |
| AND abs,X | $3D | A &= M | Absolute,X | 4+ |
| AND abs,Y | $39 | A &= M | Absolute,Y | 4+ |
| AND (ind,X) | $21 | A &= M | (Indirect,X) | 6 |
| AND (ind),Y | $31 | A &= M | (Indirect),Y | 5+ |
| ORA #imm | $09 | A |= M | Immediate | 2 |
| ORA zp | $05 | A |= M | Zero Page | 3 |
| ORA zp,X | $15 | A |= M | Zero Page,X | 4 |
| ORA abs | $0D | A |= M | Absolute | 4 |
| ORA abs,X | $1D | A |= M | Absolute,X | 4+ |
| ORA abs,Y | $19 | A |= M | Absolute,Y | 4+ |
| ORA (ind,X) | $01 | A |= M | (Indirect,X) | 6 |
| ORA (ind),Y | $11 | A |= M | (Indirect),Y | 5+ |
| EOR #imm | $49 | A ^= M | Immediate | 2 |
| EOR zp | $45 | A ^= M | Zero Page | 3 |
| EOR zp,X | $55 | A ^= M | Zero Page,X | 4 |
| EOR abs | $4D | A ^= M | Absolute | 4 |
| EOR abs,X | $5D | A ^= M | Absolute,X | 4+ |
| EOR abs,Y | $59 | A ^= M | Absolute,Y | 4+ |
| EOR (ind,X) | $41 | A ^= M | (Indirect,X) | 6 |
| EOR (ind),Y | $51 | A ^= M | (Indirect),Y | 5+ |

**Flagi:** N, Z (C, V, I, D niezmienione)

**Pliki:**
- `src/Cpu6502/Cpu6502.Logic.cs` - Implementacja 24 instrukcji
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `tests/Cpu6502.Tests/LogicTests.cs` - 14 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 127/127 (100%)

---

## Faza 6 (2026-05-16)

Implementacja 13 instrukcji CMP/CPX/CPY/BIT:

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| CMP #imm | $C9 | A - M | Immediate | 2 |
| CMP zp | $C5 | A - M | Zero Page | 3 |
| CMP zp,X | $D5 | A - M | Zero Page,X | 4 |
| CMP abs | $CD | A - M | Absolute | 4 |
| CMP abs,X | $DD | A - M | Absolute,X | 4+ |
| CMP abs,Y | $D9 | A - M | Absolute,Y | 4+ |
| CMP (ind,X) | $C1 | A - M | (Indirect,X) | 6 |
| CMP (ind),Y | $D1 | A - M | (Indirect),Y | 5+ |
| CPX #imm | $E0 | X - M | Immediate | 2 |
| CPX zp | $E4 | X - M | Zero Page | 3 |
| CPX abs | $EC | X - M | Absolute | 4 |
| CPY #imm | $C0 | Y - M | Immediate | 2 |
| CPY zp | $C4 | Y - M | Zero Page | 3 |
| CPY abs | $CC | Y - M | Absolute | 4 |
| BIT zp | $24 | A & M | Zero Page | 3 |
| BIT abs | $2C | A & M | Absolute | 4 |

**Flagi:** N, Z, C (dla CMP/CPX/CPY), N, V, Z (dla BIT)

**Pliki:**
- `src/Cpu6502/Cpu6502.CompareBit.cs` - Implementacja 13 instrukcji
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `tests/Cpu6502.Tests/CompareBitTests.cs` - 16 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 113/113 (100%)

---

## Faza 5 (2026-05-16)

Implementacja 10 instrukcji INC/DEC (Read-Modify-Write):

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| INC zp | $E6 | Memory ← Memory+1 | Zero Page | 5 |
| INC zp,X | $F6 | Memory ← Memory+1 | Zero Page,X | 6 |
| INC abs | $EE | Memory ← Memory+1 | Absolute | 6 |
| INC abs,X | $FE | Memory ← Memory+1 | Absolute,X | 7 |
| DEC zp | $C6 | Memory ← Memory-1 | Zero Page | 5 |
| DEC zp,X | $D6 | Memory ← Memory-1 | Zero Page,X | 6 |
| DEC abs | $CE | Memory ← Memory-1 | Absolute | 6 |
| DEC abs,X | $DE | Memory ← Memory-1 | Absolute,X | 7 |
| INX | $E8 | X ← X+1 | Implied | 2 |
| INY | $C8 | Y ← Y+1 | Implied | 2 |
| DEX | $CA | X ← X-1 | Implied | 2 |
| DEY | $88 | Y ← Y-1 | Implied | 2 |

**Flagi:** N, Z (C jest niezmieniana)

**Pliki:**
- `src/Cpu6502/Cpu6502.IncDec.cs` - Implementacja 10 instrukcji
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `tests/Cpu6502.Tests/IncDecTests.cs` - 15 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 97/97 (100%)
