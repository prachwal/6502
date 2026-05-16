# Faza 15 — Przerwania sprzętowe (IRQ, NMI — podstawowa obsługa)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 5% (sekcje: 7.1–7.9) |
| **Pokrycie całości** | 56% |
| **Zależności** | Fazy: 0, 1, 3, 9, 10, 11, 14 |
| **Szacowany czas** | 4–6h |

---

## Cel fazy

Implementacja sprzętowych przerwań IRQ i NMI. IRQ maskowalne (flaga I), NMI niemaskowalne (edge-triggered). Oba wstrzykują sekwencję BRK z odpowiednim wektorem.

---

## Co implementujemy

### IRQ (Interrupt Request)

- **Level-triggered** (aktywne niskim stanem).
- Sprawdzane na przedostatnim cyklu instrukcji.
- Warunek: pin IRQ niski AND flaga I = 0.
- Sekwencja jak BRK, ale:
  - Wektor: $FFFE/$FFFF
  - B=0 na stosie
  - Flaga I ← 1 (automatycznie)

### NMI (Non-Maskable Interrupt)

- **Edge-triggered** (opadające zbocze).
- Wykrywane przez porównanie poprzedniego i bieżącego stanu pinu.
- Jeśli NMI wykryte → zatrzaskiwane wewnętrznie.
- Sekwencja jak BRK, ale:
  - Wektor: $FFFA/$FFFB
  - B=0 na stosie
  - Flaga I ← 1

### API procesora

```csharp
// Do wywoływania przez klienta symulatora
public void SetIRQ(bool active)
{
    IRQPending = active;  // true = pin niski (aktywne)
}

public void SetNMI(bool active)
{
    if (previousNMI && !active)  // falling edge
        NMILatched = true;
    previousNMI = active;
}
```

### Sprawdzanie w Tick()

```csharp
// Na końcu każdej instrukcji, przed pobraniem następnego opcode:
if (Sync)  // rozpoczęcie nowej instrukcji
{
    if (NMILatched)
    {
        NMILatched = false;
        InjectInterrupt(InterruptType.NMI);
    }
    else if (IRQPending && !GetFlag(FlagI))
    {
        InjectInterrupt(InterruptType.IRQ);
    }
}
```

### InjectInterrupt

```csharp
void InjectInterrupt(InterruptType type)
{
    // Zapamiętaj bieżący PC (dla pushowania)
    ushort returnPC = PC;

    Push((byte)(returnPC >> 8));        // PCH
    Push((byte)(returnPC & 0xFF));      // PCL

    byte pushedP = P;
    if (type == InterruptType.BRK)
        pushedP |= FlagB;  // B=1 tylko dla BRK
    else
        pushedP &= (byte)~FlagB;  // B=0 dla IRQ/NMI
    pushedP |= FlagU;  // bit5=1
    Push(pushedP);

    SetFlag(FlagI, true);  // wyłącz dalsze IRQ

    ushort vector;
    if (type == InterruptType.NMI)
        vector = 0xFFFA;
    else
        vector = 0xFFFE;

    byte lo = memory.Read(vector);
    byte hi = memory.Read((ushort)(vector + 1));
    PC = (ushort)(hi << 8 | lo);
}
```

### Ważne

- Po `CLI`, przerwania są opóźnione o 1 instrukcję (flaga I sprawdzana przed instrukcją, a CLI wykonuje się w bieżącej — więc następna instrukcja się wykona zanim IRQ zostanie obsłużony).
- Interrupt hijacking (NMI podczas IRQ) — faza 17.

---

## Co testujemy

| Test | Opis |
|------|------|
| **IRQ gdy I=0** | IRQ pending → interrupt wykonany |
| **IRQ gdy I=1** | IRQ zignorowane |
| **IRQ używa $FFFE** | PC = wektor IRQ/BRK |
| **IRQ: B=0 na stosie** | Sprawdź bit 4 na stosie |
| **IRQ ustawia I** | Po IRQ, I=1 |
| **NMI na falling edge** | Wykrycie zbocza |
| **NMI ignoruje I=1** | NMI zawsze wykonane |
| **NMI używa $FFFA** | PC = wektor NMI |
| **CLI opóźnienie 1 instrukcji** | IRQ nie odpala od razu po CLI |
| **RTI po IRQ przywraca stan** | I, PC sprzed IRQ |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 7.1 | Rodzaje przerwań |
| 7.2 | Wektory przerwań |
| 7.3 | Sekwencja obsługi IRQ/NMI |
| 7.6 | Kolejność priorytetów |
| 7.7 | Timing przerwań |
| 7.9 | Flaga I |

---

## Definition of Done

- [ ] SetIRQ/setNMI API działają
- [ ] IRQ maskowalne przez I
- [ ] NMI niemaskowalne, edge-triggered
- [ ] Poprawne wektory i B-flag na stosie
- [ ] CLI opóźnienie o 1 instrukcję
- [ ] 10 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — dodaj interrupt handling |
| `tests/Cpu6502.Tests/InterruptTests.cs` | Modyfikuj — dodaj testy IRQ/NMI |
