# Faza 18C — Nieudokumentowane opkody stabilne - ANC, ALR, ARR, SBX, LAS

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 100% |
| **Pokrycie całości** | 100% |
| **Zależności** | Fazy: 0, 1, 2, 3, 4, 5, 6, 7, 8, 16 |
| **Szacowany czas** | 2-3h |
| **Data rozpoczęcia** | 2026-05-17 |
| **Data zakończenia** | 2026-05-17 |
| **Liczba testów** | 15 |

---

## Cel fazy

Implementacja 5 stabilnych nieudokumentowanych opcode'ów NMOS 6502: ANC, ALR, ARR, SBX, LAS.

---

## Co implementujemy

### Lista instrukcji

| Instrukcja | Opcode | Tryb adresowania | Bajty | Cykle | Flagi | Opis |
|------------|--------|-------------------|-------|-------|-------|------|
| **ANC** | 0x0B | Immediate | 2 | 2 | N, Z, C | AND + C ← bit7 wyniku |
| **ANC** | 0x2B | Immediate | 2 | 2 | N, Z, C | AND + C ← bit7 wyniku (alternatywny) |
| **ALR** | 0x4B | Immediate | 2 | 2 | C, N, Z | AND + LSR |
| **ARR** | 0x6B | Immediate | 2 | 2 | C, N, Z, V | AND + ROR (V = (A ^ (A >> 1)) & 0x40) |
| **SBX** | 0xCB | Immediate | 2 | 2 | N, Z, C | (A & X) - operand → X |
| **LAS** | 0xBB | Absolute,Y | 3 | 4 (+1) | N, Z | M & SP → A, X, SP |

### Pseudokod

```csharp
// ANC
A &= operand;
SetNZ(A);
SetFlag(FlagC, (A & 0x80) != 0);  // C = bit 7

// ALR
A &= operand;
A = ExecuteLsr(A);  // LSR na A

// ARR
A &= operand;
// ROR na A z specjalnym V
bool carryIn = (P & FlagC) != 0;
bool carryOut = (A & 0x01) != 0;
byte result = (byte)((A >> 1) | (carryIn ? 0x80 : 0x00));
SetFlag(FlagC, carryOut);
SetFlag(FlagV, ((A ^ result) & 0x40) != 0);  // V = (A ^ (A >> 1)) & 0x40
A = result;
SetNZ(A);

// SBX
byte temp = (byte)(A & X);
int result = temp - operand;
SetFlag(FlagC, result >= 0);  // C = !borrow
X = (byte)(result & 0xFF);
SetNZ(X);

// LAS
byte result = (byte)(memory.Read(addr) & SP);
A = result;
X = result;
SP = result;
SetNZ(result);
```

### Cykle

- ANC: 2 cykle (Immediate)
- ALR: 2 cykle (Immediate)
- ARR: 2 cykle (Immediate)
- SBX: 2 cykle (Immediate)
- LAS: 4 cykle (Absolute,Y) + 1 przy page crossing

---

## Co testujemy

| Test | Opis |
|------|------|
| **ANC_AndWithOperand_SetsCarryFromBit7** | ANC #$80 ustawia C=1 (bit 7 = 1) |
| **ANC_AndWithZero_SetsZeroFlag** | ANC #$00 ustawia Z=1 (wynik = 0) |
| **ANC_AlternatywnyOpcode** | ANC 0x2B działa tak samo jak 0x0B |
| **ALR_AndThenLsr_ResultCorrect** | ALR #$AA → A = $55, C=0 |
| **ALR_WithOddOperand_SetsCarry** | ALR #$55 → A = $2A, C=1 |
| **ALR_ZeroResult_SetsZeroFlag** | ALR #$00 → A = $00, Z=1 |
| **ARR_AndThenRor_ResultCorrect** | ARR #$AA z C=0 → A = $55, C=0 |
| **ARR_WithCarrySet_ResultCorrect** | ARR #$55 z C=1 → A = $AA, C=1 |
| **ARR_OverflowFlag_SetsCorrectly** | ARR #$40 sprawdza flagę V |
| **SBX_SubtractFromAX_ResultInX** | SBX #$05 → X = $0A, C=1 |
| **SBX_WithBorrow_SetsCarryFalse** | SBX #$10 → X = $FF, C=0 |
| **SBX_ZeroResult_SetsZeroFlag** | SBX #$0F → X = $00, Z=1 |
| **LAS_LoadsA_X_SP_FromMemoryAndSP** | LAS $1000,Y → A=X=SP=$AA |
| **LAS_ZeroResult_SetsZeroFlag** | LAS z wynikiem 0 ustawia Z=1 |

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 5 | Nieudokumentowane instrukcje — tabela stabilnych |

---

## Definition of Done

- [x] Wszystkie 5 instrukcji zaimplementowane (ANC, ALR, ARR, SBX, LAS)
- [x] Wszystkie tryby adresowania obsłużone
- [x] Poprawne flagi dla każdej instrukcji
- [x] 15 testów jednostkowych zielonych
- [x] Kod bez ostrzeżeń
- [x] Dokumentacja zaktualizowana

---

## Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.UnstableOpcodes.cs` | Implementacja ANC, ALR, ARR, SBX, LAS |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Rejestracja cykli w GetInstructionCycles |
| `src/Cpu6502/Cpu6502.CycleStepped.ArithmeticCompareLogic.cs` | Rejestracja ANC, ALR, ARR, SBX w dispatchu |
| `src/Cpu6502/Cpu6502.CycleStepped.LoadStoreTransferFlags.cs` | Rejestracja LAS w dispatchu |
| `tests/Cpu6502.Tests/QuirkTests.cs` | Testy jednostkowe (15 nowych testów) |

---

## Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 15/15 (100%) - wszystkie testy dla Fazy 18C zielone

---

## Tabela opcode'ów

| Instrukcja | Opcode | Tryb | Bajty | Cykle | Flagi |
|------------|--------|------|-------|-------|-------|
| ANC | 0x0B | Immediate | 2 | 2 | N, Z, C |
| ANC | 0x2B | Immediate | 2 | 2 | N, Z, C |
| ALR | 0x4B | Immediate | 2 | 2 | C, N, Z |
| ARR | 0x6B | Immediate | 2 | 2 | C, N, Z, V |
| SBX | 0xCB | Immediate | 2 | 2 | N, Z, C |
| LAS | 0xBB | Absolute,Y | 3 | 4 (+1) | N, Z |

---

## Pliki do utworzenia / modyfikacji

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.UnstableOpcodes.cs` | Utworzono |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Modyfikacja (GetInstructionCycles) |
| `src/Cpu6502/Cpu6502.CycleStepped.ArithmeticCompareLogic.cs` | Modyfikacja (dispatch) |
| `src/Cpu6502/Cpu6502.CycleStepped.LoadStoreTransferFlags.cs` | Modyfikacja (dispatch LAS) |
| `tests/Cpu6502.Tests/QuirkTests.cs` | Modyfikacja (dodano 15 testów) |
| `docs/faza-18C-nieudokumentowane-opkody-stabilne.md` | Utworzono |
