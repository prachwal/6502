# Faza 13 — Tryb BCD (ADC/SBC decimal mode)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 2.7, 4.3) |
| **Pokrycie całości** | 46% |
| **Zależności** | Faza 4 |
| **Szacowany czas** | 3–5h |
| **Data zakończenia** | 2026-05-17 |
| **Liczba testów** | 9 |

---

## Cel fazy

Implementacja trybu BCD (Decimal Mode) dla ADC i SBC. Gdy flaga D=1, operacje arytmetyczne działają w systemie dziesiętnym zakodowanym binarnie.

---

## Co implementujemy

### Algorytm ADC w BCD

```csharp
void BCD_ADC(byte operand)
{
    byte al = (byte)(A & 0x0F);
    byte ml = (byte)(operand & 0x0F);
    byte ah = (byte)(A >> 4);
    byte mh = (byte)(operand >> 4);
    bool c = (P & FlagC) != 0;

    // Binary sum
    ushort sum = (ushort)(A + operand + (c ? 1 : 0));
    byte result = (byte)(sum & 0xFF);
    bool carry = sum > 0xFF;

    // Set N, V, Z based on BINARY result (before BCD correction)
    SetFlag(FlagN, (result & 0x80) != 0);
    SetFlag(FlagV, ((A ^ result) & (operand ^ result) & 0x80) != 0);
    SetFlag(FlagZ, result == 0);

    // BCD correction
    if ((al + ml + (c ? 1 : 0)) > 9)
        result += 6;
    if (result > 0x99)
    {
        result += 0x60;
        carry = true;
    }

    A = result;
    SetFlag(FlagC, carry);
}
```

### Algorytm SBC w BCD

```csharp
void BCD_SBC(byte operand)
{
    bool c = (P & FlagC) != 0;

    // Binary subtraction via complement
    ushort sum = (ushort)(A + (byte)(~operand) + (c ? 1 : 0));
    byte result = (byte)(sum & 0xFF);
    bool carry = sum > 0xFF;

    // N, V, Z based on binary result
    SetFlag(FlagN, (result & 0x80) != 0);
    SetFlag(FlagV, ((A ^ result) & ((byte)(~operand) ^ result) & 0x80) != 0);
    SetFlag(FlagZ, result == 0);

    // BCD correction for subtraction
    byte al = (byte)(A & 0x0F);
    byte ml = (byte)(operand & 0x0F);
    // If low nibble borrow needed
    if ((al - ml - (c ? 0 : 1)) > 9 || (al - ml - (c ? 0 : 1)) < 0)
    {
        int borrow = (al - ml - (c ? 0 : 1)) < 0 ? 1 : 0;
        if (borrow != 0)
            result -= 6;
    }
    if (!carry)
        result -= 0x60;

    A = result;
    SetFlag(FlagC, carry);
}
```

### Uwagi

- Różne implementacje NMOS 6502 różnią się w detalach flag V w BCD. Standardowe podejście: liczyć V na podstawie binarnego wyniku pośredniego.
- Testy Klaus i Wolfgang Lorenz weryfikują poprawność BCD.

---

## Co testujemy

| Test | Opis |
|------|------|
| **ADC $09 + $01 = $10** | BCD, C=0, Z=0 |
| **ADC $99 + $01 = $00, C=1** | Przeniesienie dziesiętne |
| **ADC $50 + $50 = $00, C=1, V=1** | Overflow |
| **SBC $10 - $01 = $09** | BCD, C=1 |
| **SBC $00 - $01 = $99, C=0** | Borrow |
| **ADC / SBC bez zmian w D=0** | Tryb binarny nadal działa |
| **CLD / SED przełączają tryb** | Weryfikacja flagi D |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 2.7 | Tryb BCD |
| 4.3 | ADC/SBC — tabela cykli |
| 12.5 | Testy BCD |

---

## Definition of Done

- [x] ADC w BCD: $09+$01=$10, $99+$01=$00+C
- [x] SBC w BCD: $10-$01=$09, $00-$01=$99
- [x] Flagi N, V, Z, C poprawne
- [x] Tryb binarny nie zepsuty (testy regresyjne)
- [x] 9 testów jednostkowych BCD zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.Bcd.cs` | Utworzono |
| `src/Cpu6502/Cpu6502.Arithmetic.cs` | Zmodyfikowano |
| `tests/Cpu6502.Tests/BcdTests.cs` | Utworzono |

## Pliki implementacyjne

- `Cpu6502.Bcd.cs`: Implementacja ADC/SBC w trybie BCD
- `Cpu6502.Arithmetic.cs`: Zmodyfikowane ExecuteAdc/ExecuteSbc z obsługą BCD
- `BcdTests.cs`: 9 testów jednostkowych

## Wyniki

- Build: ✅ 0 błędów, 7 ostrzeżeń (nullable references)
- Testy: ✅ 186/186 (100%)
- Testy regresyjne: ✅ Wszystkie poprzednie instrukcje nadal działają
- Nowe testy: ✅ 9/9 testów BCD passing

## Tabela testów BCD

| Test | Opis | Wynik |
|------|------|-------|
| ADC $09 + $01 = $10 | BCD, C=0, Z=0 | ✅ |
| ADC $99 + $01 = $00, C=1 | Przeniesienie dziesiętne | ✅ |
| ADC $50 + $50 = $00, C=1, V=1 | Overflow | ✅ |
| SBC $10 - $01 = $09 | BCD, C=1 | ✅ |
| SBC $00 - $01 = $99, C=0 | Borrow | ✅ |
| ADC/SBC bez zmian w D=0 | Tryb binarny nadal działa | ✅ |
| CLD / SED przełączają tryb | Weryfikacja flagi D | ✅ |
| ADC $19 + $23 = $42 | Dodatkowy test BCD | ✅ |
| SBC $30 - $15 = $15 | Dodatkowy test BCD | ✅ |
