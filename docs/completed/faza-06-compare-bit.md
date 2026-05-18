# Faza 6 — Porównania i BIT (CMP, CPX, CPY, BIT)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.7) |
| **Pokrycie całości** | 16% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 2–3h |
| **Data zakończenia** | 2026-05-16 |
| **Liczba testów** | 16 |

---

## Cel fazy

Implementacja CMP, CPX, CPY (porównanie przez odejmowanie bez zapisu wyniku) i BIT (test bitów z AND + N, V z operandu).

---

## Co implementujemy

### CMP — Compare Accumulator

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $C9 | 2 | 2 |
| zp | $C5 | 2 | 3 |
| zp,X | $D5 | 2 | 4 |
| abs | $CD | 3 | 4 |
| abs,X | $DD | 3 | 4+p |
| abs,Y | $D9 | 3 | 4+p |
| (zp,X) | $C1 | 2 | 6 |
| (zp),Y | $D1 | 2 | 5+p |

Flagi: N, Z, C (V niezmienione).

### CPX — Compare X

| Tryb | Opcode |
|------|--------|
| # | $E0 |
| zp | $E4 |
| abs | $EC |

### CPY — Compare Y

| Tryb | Opcode |
|------|--------|
| # | $C0 |
| zp | $C4 |
| abs | $CC |

### BIT — Bit Test

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $24 | 2 | 3 |
| abs | $2C | 3 | 4 |

### Pseudokod

```csharp
// CMP: A - M → ustaw flagi, A niezmienione
void Compare(byte reg, byte operand)
{
    ushort diff = (ushort)(reg - operand);
    SetFlag(FlagC, reg >= operand);     // carry = !borrow
    SetFlag(FlagZ, (diff & 0xFF) == 0);
    SetFlag(FlagN, (diff & 0x80) != 0);
}

// BIT
void ExecuteBit(byte operand)
{
    byte result = (byte)(A & operand);
    SetFlag(FlagZ, result == 0);
    SetFlag(FlagN, (operand & 0x80) != 0);  // bit 7 operandu → N
    SetFlag(FlagV, (operand & 0x40) != 0);  // bit 6 operandu → V
}
```

### Ważne

- C przy CMP/CPX/CPY: C=1 gdy rejestr >= operand, C=0 gdy rejestr < operand.
- BIT nie modyfikuje A — tylko ustawia flagi.
- BIT ustawia N i V **bezpośrednio z bitów operandu**, niezależnie od A.

---

## Co testujemy

| Test | Opis |
|------|------|
| **CMP równe** | A=$42, M=$42 → Z=1, C=1, N=0 |
| **CMP A > M** | A=$80, M=$10 → Z=0, C=1, N=0 |
| **CMP A < M** | A=$10, M=$80 → Z=0, C=0, N=1 |
| **CPX, CPY** | Tak samo jak CMP ale z X/Y |
| **BIT Z=1** | A=$00, M=$FF → Z=1 (A&M=0) |
| **BIT Z=0** | A=$FF, M=$FF → Z=0 |
| **BIT N i V** | M=$C0 → N=1, V=1 |
| **BIT nie zmienia A** | A=$55 przed i po BIT |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.7 | Porównania i test bitów |
| 2.2 | Flagi N, Z, C przy porównaniach |

---

## Definition of Done

- [x] Wszystkie 13 opcode'ów zaimplementowanych
- [x] Porównania poprawnie ustawiają C (>=)
- [x] BIT ustawia N, V z bitów operandu
- [x] BIT nie modyfikuje A
- [x] 16 testów jednostkowych zielonych (113/113 łącznie)

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.CompareBit.cs` | Implementacja 13 instrukcji (partial class) |
| `src/Cpu6502/Cpu6502.Constructor.cs` | Inicjalizacja opcode'ów w konstruktorze |
| `tests/Cpu6502.Tests/CompareBitTests.cs` | 16 testów jednostkowych |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 113/113 (100%)

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/CompareBitTests.cs` | Utwórz |
