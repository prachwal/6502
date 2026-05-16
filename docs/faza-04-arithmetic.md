# Faza 4 — Arytmetyka binarna (ADC, SBC bez trybu BCD)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 5% (sekcje: 4.3 Arytmetyka) |
| **Pokrycie całości** | 10% |
| **Zależności** | Fazy: 0, 1, 3 |
| **Szacowany czas** | 3–5h |

---

## Cel fazy

Implementacja ADC i SBC we wszystkich trybach adresowania. Tylko tryb binarny (D=0). Tryb BCD w fazie 13.

---

## Co implementujemy

### Lista opcode'ów ADC

```
#     $69  2 bajty  2 cykle
zp    $65  2 bajty  3 cykle
zp,X  $75  2 bajty  4 cykle
abs   $6D  3 bajty  4 cykle
abs,X $7D  3 bajty  4+p
abs,Y $79  3 bajty  4+p
(zp,X) $61  2 bajty  6 cykli
(zp),Y $71  2 bajty  5+p
```

### Lista opcode'ów SBC

```
#     $E9  2 bajty  2 cykle
zp    $E5  2 bajty  3 cykle
zp,X  $F5  2 bajty  4 cykle
abs   $ED  3 bajty  4 cykle
abs,X $FD  3 bajty  4+p
abs,Y $F9  3 bajty  4+p
(zp,X) $E1  2 bajty  6 cykli
(zp),Y $F1  2 bajty  5+p
```

### Algorytm ADC

```csharp
void ExecuteADC(byte operand)
{
    if ((P & FlagD) != 0)
    {
        // Tryb BCD — faza 13
        BCD_ADC(operand);
        return;
    }

    ushort sum = (ushort)(A + operand + ((P & FlagC) != 0 ? 1 : 0));
    bool carry = sum > 0xFF;
    byte result = (byte)(sum & 0xFF);

    // Overflow: (A^result) & (M^result) & 0x80
    bool overflow = ((A ^ result) & (operand ^ result) & 0x80) != 0;
    // Zero
    bool zero = result == 0;
    // Negative
    bool negative = (result & 0x80) != 0;

    A = result;
    SetFlag(FlagC, carry);
    SetFlag(FlagZ, zero);
    SetFlag(FlagV, overflow);
    SetFlag(FlagN, negative);
}
```

### Algorytm SBC

```csharp
void ExecuteSBC(byte operand)
{
    if ((P & FlagD) != 0)
    {
        BCD_SBC(operand);
        return;
    }

    // SBC = A - M - ~C = A + ~M + C
    ushort sum = (ushort)(A + (byte)(~operand) + ((P & FlagC) != 0 ? 1 : 0));
    bool carry = sum > 0xFF;
    byte result = (byte)(sum & 0xFF);

    // Overflow dla SBC: (A^result) & (~M^result) & 0x80
    bool overflow = ((A ^ result) & ((byte)(~operand) ^ result) & 0x80) != 0;
    bool zero = result == 0;
    bool negative = (result & 0x80) != 0;

    A = result;
    SetFlag(FlagC, carry);
    SetFlag(FlagZ, zero);
    SetFlag(FlagV, overflow);
    SetFlag(FlagN, negative);
}
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **ADC 0+0 z C=0** | A=0, M=0, C=0 → A=0, C=0, Z=1, N=0, V=0 |
| **ADC 0+0 z C=1** | A=0, M=0, C=1 → A=1, C=0, Z=0 |
| **ADC $7F+$01** | A=$7F, M=$01, C=0 → A=$80, V=1, N=1 (overflow) |
| **ADC $80+$80** | A=$80, M=$80, C=0 → A=$00, V=1, C=1, Z=1 |
| **ADC $FF+$01** | A=$FF, M=$01, C=0 → A=$00, C=1, Z=1 |
| **SBC $05-$03** | A=$05, M=$03, C=1 → A=$02, C=1, Z=0 |
| **SBC $00-$01** | A=$00, M=$01, C=1 → A=$FF, C=0, N=1 (borrow) |
| **SBC $80-$01** | A=$80, M=$01, C=1 → A=$7F, V=1 (overflow) |
| **ADC wszystkie tryby adresowania** | Każdy tryb daje ten sam wynik dla tej samej wartości |
| **SBC wszystkie tryby adresowania** | jw. |

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 4.3 | Arytmetyka — ADC, SBC (tabele + algorytmy) |

---

## Definition of Done

- [ ] Wszystkie 16 opcode'ów ADC/SBC zaimplementowanych
- [ ] Poprawne flagi N, Z, C, V
- [ ] Algorytmy SBC i ADC zgodne ze specyfikacją
- [ ] Tryb BCD odkłada wykonanie do fazy 13 (skip lub NotImplemented)
- [ ] 12 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — dodaj case'y ADC, SBC |
| `tests/Cpu6502.Tests/ArithmeticTests.cs` | Utwórz |
