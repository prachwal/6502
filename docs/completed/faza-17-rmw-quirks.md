# Faza 17 — R-M-W double write, JMP indirect bug i branch quirki

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 4% (sekcje: 9, 10) |
| **Pokrycie całości** | 68% |
| **Zależności** | Faza 16 |
| **Szacowany czas** | 3–4h |
| **Data rozpoczęcia** | 2026-05-17 |
| **Data zakończenia** | 2026-05-17 |
| **Liczba testów** | 6 |

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

#### Notatka testowa po regresji

Test R-M-W double write ma liczyć wywołania `IMemoryBus.Write()` dla konkretnego adresu docelowego, a nie zmiany wartości w RAM. Dummy write często zapisuje tę samą wartość, która już jest w pamięci, więc porównywanie `Read()` przed i po cyklu zaniża liczbę zapisów.

Jeśli test tworzy lokalne `Cpu6502 cpu = new Cpu6502(memory)`, musi wykonać właśnie ten lokalny `cpu`. Nie wolno w takim teście używać fixture helpera `ExecuteOne()` pracującego na polu `_cpu`, bo wtedy lokalna pamięć śledząca zapisy zobaczy `0` zapisów i test będzie diagnozował zły obiekt.

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

Adres po branchu liczony jest od PC po pobraniu offsetu. Dla programu pod `$8000` z `BCC +2`:
- branch not taken kończy na `$8002`,
- branch taken same page kończy na `$8004`.

Testy nie powinny zamieniać tych oczekiwań miejscami.

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

## Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.FlagsSetClear.cs` | Poprawka CLI - ustawianie `_interruptDelay` |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Poprawka `TryServiceInterruptBoundary()` - obsługa `_interruptDelay` |
| `src/Cpu6502/Cpu6502.CycleStepped.BranchCycles.cs` | Branch interrupt timing - sprawdzanie przerwań na przedostatnim cyklu |
| `src/Cpu6502/Cpu6502.CycleStepped.ControlFlowCycles.cs` | JMP indirect bug - już zaimplementowany |
| `src/Cpu6502/Cpu6502.CycleStepped.RMW.cs` | R-M-W double write - już zaimplementowany |
| `tests/Cpu6502.Tests/QuirkTests.cs` | 6 testów jednostkowych dla quirków |

## Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 209/209 (100%)

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.FlagsSetClear.cs` | Modyfikuj — CLI latency |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Modyfikuj — interrupt boundary handling |
| `src/Cpu6502/Cpu6502.CycleStepped.BranchCycles.cs` | Modyfikuj — branch interrupt timing |
| `tests/Cpu6502.Tests/QuirkTests.cs` | Utwórz — testy quirków |
