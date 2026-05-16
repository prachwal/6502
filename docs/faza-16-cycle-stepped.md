# Faza 16 — Architektura cycle-stepped (Tick() per cykl)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 6% (sekcje: 6, 13.4, 13.5) |
| **Pokrycie całości** | 63% |
| **Zależności** | Wszystkie fazy 1–15 |
| **Szacowany czas** | 8–12h |

---

## Cel fazy

Przebudowa emulatora z modelu instruction-stepped (każda instrukcja w jednym wywołaniu) na **cycle-stepped** (każdy `Tick()` to 1 cykl, instrukcje rozbite na cykle). To kluczowa faza dla docelowej zgodności cycle-accurate.

---

## Co implementujemy

### Rejestr IR (Instruction Register)

W realnym 6502, rejestr IR przechowuje opcode. W emulatorze cycle-stepped:

```csharp
// IR = (opcode << 3) | cycleCounter
// Dolne 3 bity = licznik cykli (0–7)
// Górne bity = opcode
private byte IR;
```

### Przebieg Tick()

```csharp
public void Tick()
{
    // 1. Jeśli SYNC — początek nowej instrukcji
    if (Sync)
    {
        byte opcode = memory.Read(PC);
        IR = (byte)(opcode << 3);  // cycleCounter = 0
        Sync = false;

        // Sprawdź przerwania
        if (NMILatched)
        {
            NMILatched = false;
            currentInterrupt = InterruptType.NMI;
            opcode = 0x00;  // BRK
        }
        else if (IRQPending && !GetFlag(FlagI))
        {
            currentInterrupt = InterruptType.IRQ;
            opcode = 0x00;  // BRK
        }
    }

    // 2. Wykonaj jeden cykl bieżącej instrukcji
    switch (IR)
    {
        // case'y: (opcode << 3) | cycle_number
    }

    // 3. Zwiększ licznik cykli
    IR++;
    Cycle++;
}
```

### Przykład: LDA immediate ($A9) — 2 cykle

```csharp
case (0xA9 << 3) | 0:
    // Cykl 0: Odczyt opcode już wykonany w SYNC
    // Teraz: odczyt operandu
    A = memory.Read(++PC);  // PC teraz wskazuje na operand
    SetNZ(A);
    PC++;
    Sync = true;  // koniec instrukcji
    break;

// Uwaga: dla LDA #, po C0 nie ma już C1 w switch — 
// Sync=true spowoduje, że następny Tick() zacznie nową instrukcję.
// W praktyce cykl 0 = opcode fetch (SYNC), cykl 1 = operand + execute.
// Więc w modelu cycle-stepped:
// C0 (SYNC): IR = (0xA9 << 3) | 0, PC++ → adres operandu
// C1: A = Read(PC), PC++, SYNC=true
```

### Przykład: LDA absolute ($AD) — 4 cykle

```csharp
case (0xAD << 3) | 0:
    // C1: czytamy low byte adresu
    PC++;
    break;

case (0xAD << 3) | 1:
    // C2: czytamy low byte adresu
    addrL = memory.Read(PC++);
    break;

case (0xAD << 3) | 2:
    // C3: czytamy high byte adresu
    addrH = memory.Read(PC++);
    break;

case (0xAD << 3) | 3:
    // C4: czytamy wartość, ładujemy do A
    A = memory.Read((ushort)(addrH << 8 | addrL));
    SetNZ(A);
    Sync = true;  // koniec
    break;
```

### Page crossing w cycle-stepped

Dla trybów indeksowanych — dodatkowy cykl jeśli przekroczono stronę:

```csharp
case (0xBD << 3) | 3:  // LDA abs,X — cykl odczytu wartości
    if (pageCrossed)
        // Ten cykl to dummy read nieprzekraczający strony
        memory.Read((ushort)((addrH << 8) | (addrL & 0xFF)));
    else
    {
        A = memory.Read((ushort)(addrH << 8 | addrL));
        SetNZ(A);
        Sync = true;
    }
    break;

case (0xBD << 3) | 4:  // dodatkowy cykl przy page cross
    A = memory.Read((ushort)(addrH << 8 | addrL));
    SetNZ(A);
    Sync = true;
    break;
```

### Zmienne tymczasowe

Instrukcje wielocyklowe potrzebują zmiennych stanu między Tick():

```csharp
private ushort tempAddr;      // tymczasowy adres
private byte tempValue;       // tymczasowa wartość
private bool pageCrossed;     // flaga page crossing
private InterruptType currentInterrupt;
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **LDA # — 2 cykle** | Po 2 Tick(), A załadowane |
| **LDA abs — 4 cykle** | 4 Tick() → A załadowane |
| **NOP — 2 cykle** | Tick() ×2 |
| **STA abs — 4 cykle** | Zapis do pamięci |
| **Branch not taken — 2 cykle** | PC przesunięte |
| **Branch taken — 3/4 cykle** | Zależnie od strony |
| **Wszystkie udokumentowane instrukcje** | Regresja — każda w cycle-stepped |
| **page crossing dodaje cykl** | abs,X z cross = 5 cykli zamiast 4 |
| **Sekwencja RESET — 7 cykli** | Przed pierwszą instrukcją |
| **BRK — 7 cykli** | Cycle-stepped interrupt |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 6 | Cykle i timing |
| 6.4 | Tabela cykli wszystkich instrukcji |
| 13.4 | Tick() — szkielet pętli cyklowej |
| 13.5 | Wykonywanie instrukcji — switch-case |

---

## Definition of Done

- [ ] Wszystkie udokumentowane instrukcje rozbite na cykle
- [ ] Tick() wykonuje dokładnie 1 cykl
- [ ] SYNC mechanizm działa — nakładanie fetch/execute
- [ ] Page crossing poprawny w cycle-stepped
- [ ] Branch cycle count 2/3/4
- [ ] Interrupty wstrzykiwane przed instrukcją
- [ ] RESET działa w cycle-stepped
- [ ] Wszystkie testy z faz 1–15 nadal przechodzą
- [ ] Cycle count zgodny z dokumentacją dla każdej instrukcji

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — gruntowna przebudowa |
| `tests/Cpu6502.Tests/CycleAccuracyTests.cs` | Utwórz |
| Wszystkie istniejące testy | Modyfikuj — dostosuj do cycle-stepped |
