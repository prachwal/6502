# Faza 8 — Przesunięcia i rotacje (ASL, LSR, ROL, ROR)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 4% (sekcje: 4.5) |
| **Pokrycie całości** | 23% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 2–3h |
| **Data zakończenia** | 2026-05-16 |
| **Liczba testów** | 14 |

---

## Cel fazy

Implementacja ASL, LSR, ROL, ROR — operacje przesunięcia i rotacji na akumulatorze i pamięci. Wszystkie modyfikują flagi N, Z, C. R-M-W na pamięci.

---

## Co implementujemy

| Instrukcja | A | zp | zp,X | abs | abs,X |
|------------|---|-----|------|-----|-------|
| **ASL** | $0A | $06 | $16 | $0E | $1E |
| **LSR** | $4A | $46 | $56 | $4E | $5E |
| **ROL** | $2A | $26 | $36 | $2E | $3E |
| **ROR** | $6A | $66 | $76 | $6E | $7E |

### Działanie

```
ASL: C ← [7 6 5 4 3 2 1 0] ← 0
LSR: 0 → [7 6 5 4 3 2 1 0] → C
ROL: C ← [7 6 5 4 3 2 1 0] ← C
ROR: C → [7 6 5 4 3 2 1 0] → C
```

### Pseudokod

```csharp
byte ExecuteASL(byte value)
{
    bool carry = (value & 0x80) != 0;
    byte result = (byte)(value << 1);
    SetFlag(FlagC, carry);
    SetNZ(result);
    return result;
}

byte ExecuteLSR(byte value)
{
    bool carry = (value & 0x01) != 0;
    byte result = (byte)(value >> 1);
    SetFlag(FlagC, carry);
    SetNZ(result);
    return result;
}

byte ExecuteROL(byte value)
{
    bool newBit0 = (P & FlagC) != 0;  // old carry → bit 0
    bool carry = (value & 0x80) != 0; // bit 7 → new carry
    byte result = (byte)((value << 1) | (newBit0 ? 1u : 0u));
    SetFlag(FlagC, carry);
    SetNZ(result);
    return result;
}

byte ExecuteROR(byte value)
{
    bool newBit7 = (P & FlagC) != 0;  // old carry → bit 7
    bool carry = (value & 0x01) != 0; // bit 0 → new carry
    byte result = (byte)((value >> 1) | (newBit7 ? 0x80u : 0u));
    SetFlag(FlagC, carry);
    SetNZ(result);
    return result;
}
```

### Cykle

| Tryb | ASL/LSR/ROL/ROR |
|------|-----------------|
| A | 2 |
| zp | 5 |
| zp,X | 6 |
| abs | 6 |
| abs,X | 7 |

---

## Co testujemy

| Test | Opis |
|------|------|
| **ASL A $01 → $02** | C=0, N=0, Z=0 |
| **ASL A $80 → $00** | C=1, Z=1 |
| **LSR A $02 → $01** | C=0 |
| **LSR A $01 → $00** | C=1, Z=1 |
| **ROL z C=0, A=$80 → $00** | C=1 |
| **ROL z C=1, A=$00 → $01** | C=0 |
| **ROR z C=0, A=$01 → $00** | C=1 |
| **ROR z C=1, A=$00 → $80** | C=0 |
| **ASL na pamięci** | INC w pamięci przez ASL |
| **Flagi N, Z dla każdego** | Ujemny wynik, zerowy wynik |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.5 | Przesunięcia i rotacje |
| 9 | R-M-W (podwójny zapis — faza 17) |

---

## Definition of Done

- [x] 20 opcode'ów zaimplementowanych (4×5)
- [x] ASL/ROL/LSR/ROR poprawne na A
- [x] Działanie na pamięci poprawne (R-M-W)
- [x] C przenoszone poprawnie
- [x] 14 testów jednostkowych zielonych (143/143 łącznie)

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.ShiftRotate.cs` | Implementacja ASL/LSR/ROL/ROR (partial class) |
| `src/Cpu6502/Cpu6502.Constructor.cs` | Inicjalizacja opcode'ów w konstruktorze |
| `tests/Cpu6502.Tests/ShiftRotateTests.cs` | 14 testów jednostkowych |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 143/143 (100%)

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/ShiftRotateTests.cs` | Utwórz |
