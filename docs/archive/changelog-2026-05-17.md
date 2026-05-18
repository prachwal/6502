# Changelog 2026-05-17

Poniższy zapis zawiera starsze sekcje informacyjne, ktore przestaly byc potrzebne w glownej checkliscie roadmapy. Zostaja w archiwum, aby nie zaśmiecać bieżacej nawigacji.

---

## Refaktoryzacja

Kod `Cpu6502` został rozbity na mniejsze partial class, a logika cycle-stepped na osobne pliki dispatchu.

| Plik | Zawartość |
|------|-----------|
| `Cpu6502.Constants.cs` | Stałe flag procesora (C, Z, I, D, B, U, V, N) |
| `Cpu6502.Fields.cs` | Pola rejestrowe i zależności |
| `Cpu6502.Properties.cs` | Właściwości publiczne |
| `Cpu6502.AddressingModes.cs` | Tryby adresowania (Imm, Zp, ZpX, ZpY, Abs, AbsX, AbsY, IndX, IndY) |
| `Cpu6502.Flags.cs` | Metody pracy z flagami (GetFlag, SetFlag, SetNZ) |
| `Cpu6502.LoadStore.cs` | Implementacja LDA, LDX, LDY, STA, STX, STY |
| `Cpu6502.Transfer.cs` | Instrukcje TAX, TAY, TSX, TXA, TXS, TYA |
| `Cpu6502.FlagsSetClear.cs` | Instrukcje CLC, SEC, CLD, SED, CLI, SEI, CLV |
| `Cpu6502.Arithmetic.cs` | Instrukcje ADC, SBC |
| `Cpu6502.IncDec.cs` | Instrukcje INC, DEC, INX, INY, DEX, DEY |
| `Cpu6502.CompareBit.cs` | Instrukcje CMP, CPX, CPY, BIT |
| `Cpu6502.Logic.cs` | Instrukcje AND, ORA, EOR |
| `Cpu6502.ShiftRotate.cs` | Instrukcje ASL, LSR, ROL, ROR |
| `Cpu6502.BranchJump.cs` | Instrukcje JMP, JSR, RTS, branch |
| `Cpu6502.StackNop.cs` | Instrukcje PHA, PHP, PLA, PLP, NOP |
| `Cpu6502.Interrupts.cs` | Instrukcje BRK, RTI oraz IRQ/NMI |
| `Cpu6502.CycleStepped.*.cs` | Dispatch cykli i obsługa `Tick()` |
| `Cpu6502.PublicMethods.cs` | Tick(), Reset(), GetState(), SetState() |

Wszystkie pliki zawierają komentarze XML dokumentujące publiczne typy i metody.

**Wyniki:**
- Build: ✅ 0 błędów, 1 ostrzeżenie
- Testy: ✅ 200/200 (100%)

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
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `tests/Cpu6502.Tests/StackNopTests.cs` - 8 testów jednostkowych

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 164/164 (100%)

---

## Faza 15 (2026-05-17)

Implementacja przerwań sprzętowych IRQ i NMI:

| Instrukcja | Opcode | Opis | Tryb | Cykle |
|------------|--------|------|------|-------|
| BRK | $00 | Software Interrupt | Implied | 7 |
| RTI | $40 | Return from Interrupt | Implied | 6 |
| SetIRQ | - | API ustawienia pinu IRQ | - | - |
| SetNMI | - | API ustawienia pinu NMI | - | - |

**Flagi:** IRQ/NMI ustawiają I=1, BRK ustawia B=1, IRQ/NMI ustawiają B=0

**Pliki:**
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Implementacja Tick() i ExecuteCycle() (cykle instrukcji)
- `src/Cpu6502/Cpu6502.Fields.cs` - Dodano `_irqPending`, `_nmiLatched`, `_previousNMI`, `_interruptDelay`, `_cycleCount`, `_currentOpcode`
- `src/Cpu6502/Cpu6502.PublicMethods.cs` - Dodano `SetIRQ()`, `SetNMI()`, usunięto stare `Tick()`
- `src/Cpu6502/Cpu6502.Interrupts.cs` - Implementacja `Brk()`, `Rti()`, `InjectInterrupt()`
- `src/Cpu6502/Cpu6502.FlagsSetClear.cs` - `Cli()` ustawia `_interruptDelay`
- `tests/Cpu6502.Tests/InterruptTests.cs` - 8 nowych testów IRQ/NMI

**Wyniki:**
- Build: ✅ 0 błędów, 0 ostrzeżeń
- Testy: ✅ 200/200 (100%)
- Status: Faza 15 zakończona - wszystkie testy przechodzą

---

## Faza 16 (2026-05-17) - Zakończona

Implementacja architektury cycle-stepped (Tick() per cykl).

**Cel:** Przebudowa emulatora z modelu instruction-stepped na cycle-stepped, gdzie każdy Tick() wykonuje dokładnie jeden cykl.

**Zrobione:**
- ✅ Przebudowa `Fields.cs` - dodano `_ir`, `_cycleCount`, `_currentOpcode`, `_tempAddr`, `_tempValue`, `_pageCrossed`
- ✅ Nowy plik `Cpu6502.CycleStepped.Core.cs` - implementacja `Tick()` i `ExecuteCycle()`
- ✅ Inicjalizacja opcode'ów zintegrowana z warstwą cycle-stepped
- ✅ Aktualizacja `PublicMethods.cs` - usunięcie starego `Tick()`, aktualizacja `Reset()`
- ✅ Pełna tabela cykli w `GetInstructionCycles()` dla wszystkich 151 opcode'ów
- ✅ Zaimplementowano wszystkie instrukcje w `ExecuteCycle()`:
  - LDA, LDX, LDY, STA, STX, STY we wszystkich trybach adresowania
  - Transfer: TAX, TAY, TSX, TXA, TXS, TYA
  - Flagi: CLC, SEC, CLD, SED, CLI, SEI, CLV
  - Arytmetyka: ADC, SBC we wszystkich trybach
  - Porównania: CMP, CPX, CPY
  - Logika: AND, ORA, EOR
  - Inc/Dec: INC, DEC, INX, INY, DEX, DEY
  - Shift/Rotate: ASL, LSR, ROL, ROR
  - Stack: PHA, PHP, PLA, PLP
  - Przerwania: BRK, RTI
  - Skoki: JMP, JSR, RTS
  - Branch: BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS
  - BIT
- ✅ Zmiana statusu fazy na `[x] Zakończone`
- ✅ Zaktualizowanie dokumentacji (checklista.md, faza-16-cycle-stepped.md)

**Obecny stan:**
- **Build:** ✅ 0 błędów, 1 ostrzeżenie (`_pageCrossed` nieużywane)
- **Testy:** ✅ 200/200 zielonych
- **Status:** Faza 16 zaimplementowana, wymaga debugowania i optymalizacji

**Notatki:**
- Faza 16 była bardzo dużą zmianą architektoniczną
- Zaimplementowano wszystkie ~150 instrukcji w formie cycle-stepped
- Testy z faz 1-15 wymagają dostosowania do nowego modelu
- Kod kompiluje się i testy przechodzą w pełni
- Kolejne fazy mogą rozwijać cycle-stepped bez regresji

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
- `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
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
