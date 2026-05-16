# Faza 17 — R-M-W double write, JMP indirect bug i branch quirki

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 4% (sekcje: 9, 10) |
| **Pokrycie całości** | 68% |
| **Zależności** | Faza 16 |
| **Szacowany czas** | 3–4h |

---

## Cel fazy

Poprawienie subtelnych zachowań NMOS 6502: podwójny zapis w R-M-W, bug JMP indirect na granicy strony, timing przerwań podczas branch, opóźnienie CLI.

---

## Co implementujemy

### 1. R-M-W double write

Instrukcje R-M-W (ASL, LSR, ROL, ROR, INC, DEC, DCP, ISC, RLA, RRA, SLO, SRE) na adresie absolutnym:

```csharp
// W cycle-stepped, w odpowiednich cyklach:
// Cykl odczytu:
byte original = memory.Read(addr);
// Cykl "zapisu" niezmodyfikowanego (NMOS quirk):
memory.Write(addr, original);  // dummy write — zapisuje oryginał
// CPU modyfikuje wartość
byte modified = ExecuteOperation(original);
// Cykl zapisu zmodyfikowanego:
memory.Write(addr, modified);
```

W modelu cycle-stepped, cykl zapisu niezmodyfikowanej wartości to osobny `case`.

### 2. JMP indirect page-crossing bug

```csharp
ushort AddrIndirectAbs()
{
    byte lo = memory.Read(PC++);
    byte hi = memory.Read(PC++);
    ushort ptrAddr = (ushort)(hi << 8 | lo);

    byte addrLo = memory.Read(ptrAddr);

    // NMOS bug: jeśli ptrAddr kończy się na $xxFF,
    // high byte czytany z $xx00 zamiast $(xx+1)00
    ushort ptrAddrHi;
    if ((ptrAddr & 0xFF) == 0xFF)
        ptrAddrHi = (ushort)(ptrAddr & 0xFF00);  // ta sama strona
    else
        ptrAddrHi = (ushort)(ptrAddr + 1);

    byte addrHi = memory.Read(ptrAddrHi);

    return (ushort)(addrHi << 8 | addrLo);
}
```

### 3. Branch + interrupt timing

Przerwania sprawdzane na przedostatnim cyklu instrukcji. Dla branch:
- Not taken (2 cykle): sprawdzane w cyklu 1
- Taken same page (3 cykle): sprawdzane w cyklu 2
- Taken different page (4 cykle): sprawdzane w cyklu 3

W cycle-stepped: umieść `CheckInterrupts()` na odpowiednim case'ie przed Sync=true.

### 4. CLI latency

```csharp
case 0x58:  // CLI
    shouldClearI = true;  // opóźnienie o 1 instrukcję
    Sync = true;
    break;

// W SYNC handlerze:
if (shouldClearI)
{
    SetFlag(FlagI, false);
    shouldClearI = false;
}
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **ASL abs zapisuje oryginał przed modyfikacją** | Dwa write pod ten sam adres |
| **JMP ($01FF) bug** | High byte z $0100 |
| **JMP ($0200) normalny** | High byte z $0201 |
| **IRQ nie przerywa branch w cyklu 1** | IRQ sprawdzane tylko w cyklu 2 |
| **CLI opóźnienie** | IRQ nie fires natychmiast po CLI |
| **Interrupt hijacking** | NMI podczas IRQ → NMI vector |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 9 | R-M-W double write |
| 10 | JMP indirect bug, branch quirki |
| 7.8 | Interrupt hijacking |

---

## Definition of Done

- [ ] R-M-W double write dla wszystkich instrukcji
- [ ] JMP indirect bug na $xxFF
- [ ] Branch interrupt timing poprawny
- [ ] CLI 1-instruction latency
- [ ] Wszystkie testy regresyjne zielone
- [ ] 6 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — poprawki cycle-stepped |
| `tests/Cpu6502.Tests/QuirkTests.cs` | Utwórz |
