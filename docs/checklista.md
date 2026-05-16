# Checklista faz implementacji symulatora 6502

Legenda statusu: `[ ]` = nie rozpoczęte | `[~]` = w trakcie | `[x]` = zakończone

## Legenda wskaźników

| Wskaźnik | Opis |
|----------|------|
| **Pokrycie dokumentacji** | % dokumentacji głównej, który dana faza implementuje |
| **Pokrycie całości** | Skumulowany % pełnej specyfikacji 6502 (instrukcje + timing + przerwania + quirk) |

---

| # | Faza | Plik | Status | % pokrycia dok. | % pokrycia całości |
|---|------|------|--------|----------------:|-------------------:|
| 0 | Szkielet projektu i struktura .NET | [faza-00-szkielet.md](faza-00-szkielet.md) | [x] | 100% | 1% |
| 1 | Load / Store — LDA, LDX, LDY, STA, STX, STY | [faza-01-load-store.md](faza-01-load-store.md) | [x] | 100% | 3% |
| 2 | Transfer między rejestrami — TAX, TAY, TSX, TXA, TXS, TYA | [faza-02-transfer.md](faza-02-transfer.md) | [x] | 3% | 5% |
| 3 | Flagi Set/Clear — CLC, SEC, CLD, SED, CLI, SEI, CLV | [faza-03-flags.md](faza-03-flags.md) | [x] | 3% | 7% |
| 4 | Arytmetyka binarna — ADC, SBC (bez BCD) | [faza-04-arithmetic.md](faza-04-arithmetic.md) | [x] | 5% | 10% |
| 5 | Inkrementacja / Dekrementacja — INC, DEC, INX, INY, DEX, DEY | [faza-05-inc-dec.md](faza-05-inc-dec.md) | [x] | 3% | 13% |
| 6 | Porównania i BIT — CMP, CPX, CPY, BIT | [faza-06-compare-bit.md](faza-06-compare-bit.md) | [x] | 3% | 16% |
| 7 | Operacje logiczne — AND, ORA, EOR | [faza-07-logic.md](faza-07-logic.md) | [x] | 3% | 19% |
| 8 | Przesunięcia i rotacje — ASL, LSR, ROL, ROR | [faza-08-shift-rotate.md](faza-08-shift-rotate.md) | [x] | 4% | 23% |
| 9 | Skoki i rozgałęzienia — JMP, JSR, RTS, BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS | [faza-09-branch-jump.md](faza-09-branch-jump.md) | [x] | 6% | 30% |
| 10 | Stos i NOP — PHA, PHP, PLA, PLP, NOP | [faza-10-stack-nop.md](faza-10-stack-nop.md) | [x] | 3% | 33% |
| 11 | Przerwania software — BRK, RTI | [faza-11-brk-rti.md](faza-11-brk-rti.md) | [x] | 3% | 37% |
| 12 | Pełne tryby adresowania + page crossing | [faza-12-addressing.md](faza-12-addressing.md) | [~] | 5% | 42% |
| 13 | Tryb BCD — ADC/SBC decimal mode | [faza-13-bcd.md](faza-13-bcd.md) | [ ] | 3% | 46% |
| 14 | Sekwencja RESET | [faza-14-reset.md](faza-14-reset.md) | [ ] | 3% | 50% |
| 15 | Przerwania sprzętowe — IRQ, NMI (podstawowa obsługa) | [faza-15-irq-nmi.md](faza-15-irq-nmi.md) | [ ] | 5% | 56% |
| 16 | Architektura cycle-stepped — Tick() per cykl | [faza-16-cycle-stepped.md](faza-16-cycle-stepped.md) | [ ] | 6% | 63% |
| 17 | R-M-W double write + quirk JMP indirect | [faza-17-rmw-quirks.md](faza-17-rmw-quirks.md) | [ ] | 4% | 68% |
| 18 | Nieudokumentowane opkody — stabilne | [faza-18-illegal-stable.md](faza-18-illegal-stable.md) | [ ] | 8% | 76% |
| 19 | Nieudokumentowane opkody — niestabilne + NOP + KIL | [faza-19-illegal-unstable.md](faza-19-illegal-unstable.md) | [ ] | 6% | 83% |
| 20 | Test zgodności — nestest | [faza-20-nestest.md](faza-20-nestest.md) | [ ] | 4% | 88% |
| 21 | Test zgodności — Klaus Dormann Functional Test | [faza-21-klaus.md](faza-21-klaus.md) | [ ] | 4% | 93% |
| 22 | Test zgodności — Wolfgang Lorenz | [faza-22-wolfgang.md](faza-22-wolfgang.md) | [ ] | 5% | 97% |
| 23 | Test zgodności — perfect6502 (opcjonalnie) | [faza-23-perfect6502.md](faza-23-perfect6502.md) | [ ] | 3% | 100% |

---

## Postęp faz

```
Całkowity postęp:     10 / 24 faz (42%)

Fazy zakończone   [x]: 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11
Fazy w trakcie     [~]: 12
Fazy w trakcie     [~]:
Fazy nie rozpoczęte [ ]: 11,12,13,14,15,16,17,18,19,20,21,22,23
```

---

## Refaktoryzacja (2026-05-16)

Plik `Cpu6502.cs` podzielono na 9 plików partial class:

| Plik | Zawartość |
|------|-----------|
| `Cpu6502.Constants.cs` | Stałe flag procesora (C, Z, I, D, B, U, V, N) |
| `Cpu6502.Fields.cs` | Pola rejestrowe i zależności |
| `Cpu6502.Constructor.cs` | Konstruktor i inicjalizacja tabeli opcode |
| `Cpu6502.Properties.cs` | Właściwości publiczne |
| `Cpu6502.AddressingModes.cs` | 9 trybów adresowania (Imm, Zp, ZpX, ZpY, Abs, AbsX, AbsY, IndX, IndY) |
| `Cpu6502.Flags.cs` | Metody pracy z flagami (GetFlag, SetFlag, SetNZ) |
| `Cpu6502.LoadStore.cs` | Implementacja LDA, LDX, LDY, STA, STX, STY |
| `Cpu6502.PublicMethods.cs` | Tick(), Reset(), GetState(), SetState() |
| `Cpu6502.Placeholders.cs` | Obecnie niezaimplementowane opcode'y (placeholder) |

Wszystkie pliki zawierają szczegółowe komentarze XML dokumentujące funkcje.

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 164/164 (100%)

---

## Faza 10 (2026-05-17)

Implementacja 5 instrukcji stosowych i NOP:

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| PHA | $48 | Push A | Implied | 3 |
| PHP | $08 | Push P (z B=1, bit5=1) | Implied | 3 |
| PLA | $68 | Pull A | Implied | 4 |
| PLP | $28 | Pull P | Implied | 4 |
| NOP | $EA | No Operation | Implied | 2 |

**Flagi:** PHA/PHP nie zmieniają flag, PLA ustawia N,Z, PLP ustawia wszystkie flagi

**Pliki:**
- `src/Cpu6502/Cpu6502.StackNop.cs` - Implementacja 5 instrukcji
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
- `tests/Cpu6502.Tests/StackNopTests.cs` - 8 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 164/164 (100%)

---

## Faza 9 (2026-05-16)

Implementacja 11 instrukcji skoków i rozgałęzień:

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| JMP abs | $4C | Skok bezwzględny | Absolute | 3 |
| JMP (abs) | $6C | Skok pośredni | Indirect | 5 |
| JSR abs | $20 | Skok do podprogramu | Absolute | 6 |
| RTS | $60 | Powrót z podprogramu | Implied | 6 |
| BCC | $90 | Branch if Carry Clear | Relative | 2/3/4 |
| BCS | $B0 | Branch if Carry Set | Relative | 2/3/4 |
| BEQ | $F0 | Branch if Equal | Relative | 2/3/4 |
| BMI | $30 | Branch if Minus | Relative | 2/3/4 |
| BNE | $D0 | Branch if Not Equal | Relative | 2/3/4 |
| BPL | $10 | Branch if Plus | Relative | 2/3/4 |
| BVC | $50 | Branch if Overflow Clear | Relative | 2/3/4 |
| BVS | $70 | Branch if Overflow Set | Relative | 2/3/4 |

**Flagi:** Brak zmian dla JMP/JSR, RTS nie zmienia flag

**Pliki:**
- `src/Cpu6502/Cpu6502.BranchJump.cs` - Implementacja 11 instrukcji
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
- `src/Cpu6502/Cpu6502.PublicMethods.cs` - Metody Push/Pop dla stosu
- `tests/Cpu6502.Tests/BranchJumpTests.cs` - 11 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 156/157 (99.4%)

---

## Faza 8 (2026-05-16)

Implementacja 20 instrukcji przesunięć i rotacji (ASL, LSR, ROL, ROR):

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| ASL A | $0A | A << 1 | Accumulator | 2 |
| ASL zp | $06 | M << 1 | Zero Page | 5 |
| ASL zp,X | $16 | M << 1 | Zero Page,X | 6 |
| ASL abs | $0E | M << 1 | Absolute | 6 |
| ASL abs,X | $1E | M << 1 | Absolute,X | 7 |
| LSR A | $4A | A >> 1 | Accumulator | 2 |
| LSR zp | $46 | M >> 1 | Zero Page | 5 |
| LSR zp,X | $56 | M >> 1 | Zero Page,X | 6 |
| LSR abs | $4E | M >> 1 | Absolute | 6 |
| LSR abs,X | $5E | M >> 1 | Absolute,X | 7 |
| ROL A | $2A | A ROL | Accumulator | 2 |
| ROL zp | $26 | M ROL | Zero Page | 5 |
| ROL zp,X | $36 | M ROL | Zero Page,X | 6 |
| ROL abs | $2E | M ROL | Absolute | 6 |
| ROL abs,X | $3E | M ROL | Absolute,X | 7 |
| ROR A | $6A | A ROR | Accumulator | 2 |
| ROR zp | $66 | M ROR | Zero Page | 5 |
| ROR zp,X | $76 | M ROR | Zero Page,X | 6 |
| ROR abs | $6E | M ROR | Absolute | 6 |
| ROR abs,X | $7E | M ROR | Absolute,X | 7 |

**Flagi:** N, Z, C (dla wszystkich operacji)

**Pliki:**
- `src/Cpu6502/Cpu6502.ShiftRotate.cs` - Implementacja 20 instrukcji
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
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
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
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
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
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
- `src/Cpu6502/Cpu6502.Constructor.cs` - Zainicjalizowanie opcode'ów
- `tests/Cpu6502.Tests/IncDecTests.cs` - 15 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 97/97 (100%)
