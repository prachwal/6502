# Faza 7 — Operacje logiczne (AND, ORA, EOR)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.6) |
| **Pokrycie całości** | 19% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 2h |

---

## Cel fazy

Implementacja AND, ORA, EOR we wszystkich trybach adresowania. Flagi N, Z. C i V bez zmian.

---

## Co implementujemy

Wszystkie trzy instrukcje mają identyczne tryby adresowania:

| Tryb | AND | ORA | EOR |
|------|-----|-----|-----|
| # | $29 | $09 | $49 |
| zp | $25 | $05 | $45 |
| zp,X | $35 | $15 | $55 |
| abs | $2D | $0D | $4D |
| abs,X | $3D | $1D | $5D |
| abs,Y | $39 | $19 | $59 |
| (zp,X) | $21 | $01 | $41 |
| (zp),Y | $31 | $11 | $51 |

Wszystkie: 2 bajty (#, zp, zp,X, (zp,X), (zp),Y), 3 bajty (abs, abs,X, abs,Y).

### Pseudokod

```csharp
void AND(byte operand)
{
    A &= operand;
    SetNZ(A);
}

void ORA(byte operand)
{
    A |= operand;
    SetNZ(A);
}

void EOR(byte operand)
{
    A ^= operand;
    SetNZ(A);
}
```

### Flagi

- **AND/ORA/EOR**: N, Z = ++, C, V, I, D = bez zmian.
- Wszystkie modyfikują A bezpośrednio.

---

## Co testujemy

| Test | Opis |
|------|------|
| **AND $FF & $0F** | A=$FF, oper=$0F → A=$0F, Z=0, N=0 |
| **AND $00 & $FF** | A=$00 → A=$00, Z=1 |
| **AND $80 & $80** | A=$80, N=1 |
| **ORA $00 | $55** | A=$00, oper=$55 → A=$55 |
| **ORA ustawia N** | A=$80, oper=$00 → N=1 |
| **EOR $FF ^ $AA** | A=$FF, oper=$AA → A=$55 |
| **EOR $00 ^ $00** | Z=1 |
| **C i V niezmienione** | Ustaw C=1, V=1 przed AND/ORA/EOR → nadal 1 |
| **Wszystkie tryby adresowania** | Każdy tryb daje ten sam wynik |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.6 | Operacje logiczne |

---

## Definition of Done

- [ ] 24 opcode'y zaimplementowane
- [ ] AND, ORA, EOR poprawne we wszystkich trybach
- [ ] Flagi N, Z poprawnie ustawiane
- [ ] Flagi C, V niezmienione
- [ ] 10 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/LogicTests.cs` | Utwórz |
