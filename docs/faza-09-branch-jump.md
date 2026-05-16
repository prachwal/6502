# Faza 9 — Skoki i rozgałęzienia (JMP, JSR, RTS, BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 6% (sekcje: 4.8) |
| **Pokrycie całości** | 30% |
| **Zależności** | Fazy: 0, 1, 2, 3, 4, 5, 6 |
| **Szacowany czas** | 4–6h |

---

## Cel fazy

Implementacja JMP (absolute i indirect), JSR, RTS oraz wszystkich 8 instrukcji branch. To kluczowa faza — bez niej programy nie mogą zmieniać flow.

---

## Co implementujemy

### JMP

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| abs | $4C | 3 | 3 |
| (abs) | $6C | 3 | 5 |

```
JMP abs:   PC = (mem[PC+2] << 8) | mem[PC+1]
JMP (abs): PC = (mem[addr+1] << 8) | mem[addr]
           BUG NMOS: jeśli addr & 0xFF == 0xFF, high byte czytane z (addr & 0xFF00)
```

### JSR — Jump to Subroutine

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| abs | $20 | 3 | 6 |

```
JSR:
  PC+2 → załaduj adres docelowy
  Push PCH (PC+2 >> 8)
  Push PCL (PC+2 & 0xFF)
  PC = adres docelowy
```

### RTS — Return from Subroutine

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| impl | $60 | 1 | 6 |

```
RTS:
  Pull PCL
  Pull PCH
  PC = (PCH << 8 | PCL) + 1
```

### Branch — 8 instrukcji

| Instrukcja | Opcode | Warunek |
|------------|--------|---------|
| BCC | $90 | C=0 |
| BCS | $B0 | C=1 |
| BEQ | $F0 | Z=1 |
| BMI | $30 | N=1 |
| BNE | $D0 | Z=0 |
| BPL | $10 | N=0 |
| BVC | $50 | V=0 |
| BVS | $70 | V=1 |

Branch: 2 bajty (opcode + signed offset).
- Not taken: 2 cykle
- Taken, same page: 3 cykle
- Taken, different page: 4 cykle

```csharp
void Branch(bool condition)
{
    sbyte offset = (sbyte)memory.Read(PC);
    ushort target = (ushort)(PC + 1 + offset);
    if (condition)
    {
        bool pageCrossed = (target >> 8) != ((PC + 1) >> 8);
        cycles += pageCrossed ? 2 : 1; // +1 lub +2 do bazowych 2
        PC = target;
    }
    else
    {
        PC++; // pomiń offset byte
    }
}
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **JMP abs przeskakuje** | PC ustawiony na nowy adres |
| **JMP (abs) indirect** | Odczyt wskaźnika i skok |
| **JMP ($xxFF) NMOS bug** | High byte z tej samej strony |
| **JSR pushuje return address** | PC+2 na stosie, skok do celu |
| **RTS wraca** | PC = adres ze stosu + 1 |
| **Branch not taken** | PC++ (tylko offset pominięty) |
| **Branch taken same page** | 3 cykle, PC poprawny |
| **Branch taken different page** | 4 cykle |
| **Branch backward** | Ujemny offset |
| **Każdy warunek branch** | BCC/BCS/BEQ/BNE/BMI/BPL/BVC/BVS |
| **JSR + RTS roundtrip** | Wykonanie JSR, potem RTS wraca |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.8 | Skoki i rozgałęzienia |
| 6.2 | Page boundary crossing dla branch |
| 10 | JMP indirect bug |

---

## Definition of Done

- [ ] JMP abs, JMP indirect działają
- [ ] JMP indirect NMOS bug zaimplementowany
- [ ] JSR pushuje poprawny adres
- [ ] RTS wraca poprawnie
- [ ] 8 branchy z poprawnymi warunkami
- [ ] Branch cycle count: 2/3/4 w zależności od sytuacji
- [ ] 15 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/BranchJumpTests.cs` | Utwórz |
