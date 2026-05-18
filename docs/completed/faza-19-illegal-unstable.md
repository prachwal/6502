# Faza 19 — Nieudokumentowane opkody — niestabilne, NOP-y, KIL

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 6% (sekcje: 5 — niestabilne, NOP, KIL) |
| **Pokrycie całości** | 84% |
| **Zależności** | Faza 18 |
| **Szacowany czas** | 4–6h |
| **Data rozpoczęcia** | 2026-05-27 |
| **Data zakończenia** | 2026-05-27 |
| **Liczba testów** | 20 |

---

## Cel fazy

Implementacja niestabilnych opkodów, nieudokumentowanych NOP-ów oraz instrukcji KIL/JAM.

---

## Co implementujemy

### ANE (XAA) — $8B
```csharp
// (A OR CONST) & X & oper → A
// CONST = $FF w większości implementacji
A = (byte)((A | 0xFF) & X & operand);
SetNZ(A);
```

### LXA (LAX immediate) — $AB
```csharp
A = X = (byte)((A | 0xFF) & operand);
SetNZ(A);
```

### SHA (AHX/AXA) — $9F, $93
```csharp
// M ← A & X & (H+1), gdzie H = high byte adresu docelowego + 1
byte h = (byte)((addr >> 8) + 1);
memory.Write(addr, (byte)(A & X & h));
```

### SHX (SXA/XAS) — $9E
```csharp
byte h = (byte)((addr >> 8) + 1);
memory.Write(addr, (byte)(X & h));
```

### SHY (SYA/SAY) — $9C
```csharp
byte h = (byte)((addr >> 8) + 1);
memory.Write(addr, (byte)(Y & h));
```

### TAS (XAS/SHS) — $9B
```csharp
SP = (byte)(A & X);
byte h = (byte)((addr >> 8) + 1);
memory.Write(addr, (byte)(SP & h));
```

### USBC — $EB
```csharp
// Zachowuje się jak SBC #immediate
ExecuteSBC(operand);
```

### Nieudokumentowane NOP-y (ok. 25 opcode'ów)

| Opcode | Tryb | Cykle | Opis |
|--------|------|-------|------|
| $04, $44, $64 | zp | 3 | NOP zp — czyta z zp, ignoruje |
| $14, $34, $54, $74, $0C | zp,X | 4 | NOP zp,X — czyta, ignoruje |
| $1C, $3C, $5C, $7C, $DC, $FC | abs,X | 4+p | NOP abs,X — czyta |
| $80, $82, $89, $C2, $E2 | # | 2 | NOP immediate |
| $1A, $3A, $5A, $7A, $DA, $FA | impl | 2 | NOP implied |

Wszystkie czytają z pamięci (tam gdzie jest adres) i ignorują wartość. Nie modyfikują rejestrów ani flag.

### KIL/JAM/HLT — instrukcje zabijające

Opcodes: $02, $12, $22, $32, $42, $52, $62, $72, $92, $B2, $D2, $F2

```csharp
case 0x02 << 3:
case 0x12 << 3:
// ... wszystkie KIL opcode'y
    Halted = true;
    // CPU się zatrzymuje — nie wykonuje już instrukcji
    break;
```

W głównej pętli Tick():
```csharp
public void Tick()
{
    if (Halted)
        return;  // CPU zatrzymany
    // ...
}
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **ANE z CONST=$FF** | A=$FF,X=$0F,oper=$FF → A=$0F |
| **LXA** | A i X załadowane |
| **SHA, SHX, SHY** | Zapis z AND (H+1) |
| **TAS** | SP ← A&X + zapis |
| **USBC** | Działa jak SBC # |
| **NOP-y nie modyfikują stanu** | Rejestry/flagi bez zmian |
| **NOP-y czytają z pamięci** | Adres odczytany |
| **KIL zatrzymuje CPU** | Tick() nic nie robi po KIL |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 5 | Niestabilne opkody, NOP-y, KIL |

---

## Definition of Done

- [x] Wszystkie niestabilne opkody zaimplementowane (ANE, LXA, SHA, SHX, SHY, TAS, USBC)
- [x] ~25 nieudokumentowanych NOP-ów
- [x] 12 instrukcji KIL
- [x] Działanie zgodne z dokumentacją
- [x] 20 testów jednostkowych zielonych

---

## Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.Fields.cs` | Dodano `_halted` |
| `src/Cpu6502/Cpu6502.PublicMethods.cs` | `Reset()` ustawia `_halted=false`, `Tick()` sprawdza `_halted` |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Dodano cykle i dispatch dla nowych opcode'ów |
| `src/Cpu6502/Cpu6502.CycleStepped.UnstableOpcodes.cs` | Implementacja ANE, LXA, SHA, SHX, SHY, TAS, USBC |
| `src/Cpu6502/Cpu6502.CycleStepped.NopKilOpcodes.cs` | Implementacja 25 NOP-ów + 12 KIL-ów |
| `tests/Cpu6502.Tests/Phase19UnstableOpcodesTests.cs` | 20 testów jednostkowych |

---

## Wyniki

| Metryka | Wartość |
|---------|---------|
| **Build** | ✅ 0 błędów, 9 ostrzeżeń (istniejące) |
| **Testy** | ✅ 252/252 (100%) |
| **Status** | Zakończone |
