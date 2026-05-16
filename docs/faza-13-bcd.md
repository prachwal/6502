# Faza 13 — Tryb BCD (ADC/SBC decimal mode)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 3% (sekcje: 2.7, 4.3) |
| **Pokrycie całości** | 46% |
| **Zależności** | Faza 4 |
| **Szacowany czas** | 3–5h |

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

- [ ] ADC w BCD: $09+$01=$10, $99+$01=$00+C
- [ ] SBC w BCD: $10-$01=$09, $00-$01=$99
- [ ] Flagi N, V, Z, C poprawne
- [ ] Tryb binarny nie zepsuty (testy regresyjne)
- [ ] 8 testów jednostkowych BCD zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — rozszerz ADC/SBC |
| `tests/Cpu6502.Tests/BcdTests.cs` | Utwórz |
