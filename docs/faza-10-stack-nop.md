# Faza 10 — Stos i NOP (PHA, PHP, PLA, PLP, NOP)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.9, 4.12) |
| **Pokrycie całości** | 33% |
| **Zależności** | Fazy: 0, 1, 2, 3 |
| **Szacowany czas** | 2h |

---

## Cel fazy

Implementacja operacji stosowych i NOP. Stos rośnie w dół od $0100. Push: write + SP--. Pull: SP++ + read.

---

## Co implementujemy

| Instrukcja | Opcode | Bajty | Cykle | Opis |
|------------|--------|-------|-------|------|
| **PHA** | $48 | 1 | 3 | Push A |
| **PHP** | $08 | 1 | 3 | Push P (z B=1, bit5=1) |
| **PLA** | $68 | 1 | 4 | Pull A (N, Z) |
| **PLP** | $28 | 1 | 4 | Pull P |
| **NOP** | $EA | 1 | 2 | Nothing |

### Pseudokod

```csharp
void Push(byte value)
{
    ushort addr = (ushort)(0x0100 + SP);
    memory.Write(addr, value);
    SP--;
}

byte Pull()
{
    SP++;
    ushort addr = (ushort)(0x0100 + SP);
    return memory.Read(addr);
}

// PHA
void ExecutePHA()
{
    Push(A);
}

// PHP: P | 0x30 → B=1, bit5=1
void ExecutePHP()
{
    Push((byte)(P | 0x30));
}

// PLA
void ExecutePLA()
{
    A = Pull();
    SetNZ(A);
}

// PLP: P = Pull() — B i bit5 ignorowane w hardware,
// ale w emulacji po prostu ustawiamy P na wartość
void ExecutePLP()
{
    P = Pull();
}

// NOP
void ExecuteNOP()
{
    // nic
}
```

### Ważne

- SP zawija się (wrap-around): 0 → $FF, $FF → 0.
- PHP zapisuje P z B=1 i bit5=1 — ale sam rejestr P **nie** jest modyfikowany.
- PLP nadpisuje P wartością ze stosu.

---

## Co testujemy

| Test | Opis |
|------|------|
| **PHA zapisuje A na stos** | SP--, $0100+(SP+1)=A |
| **PHP zapisuje P z B=1** | Sprawdź bit 4 = 1 na stosie |
| **PHP zapisuje P z bit5=1** | Sprawdź bit 5 = 1 na stosie |
| **PLA ładuje A ze stosu** | SP++, N,Z ustawione |
| **PLP ładuje flagi** | Ustaw flagi przed push, pull przywraca |
| **NOP nic nie robi** | Stan CPU bez zmian |
| **SP wrap 0→FF** | Push przy SP=0 → SP=$FF |
| **SP wrap FF→0** | Pull przy SP=$FF → SP=0 |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.9 | Stos |
| 4.12 | NOP |
| 2.4 | Stos (adresacja, LIFO) |
| 2.2 | B-flag, bit5 |

---

## Definition of Done

- [ ] PHA, PHP, PLA, PLP, NOP zaimplementowane
- [ ] PHP ustawia B=1, bit5=1 na stosie
- [ ] SP poprawnie inkrementowany/dekrementowany
- [ ] SP wrap-around działa
- [ ] 8 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/StackNopTests.cs` | Utwórz |
