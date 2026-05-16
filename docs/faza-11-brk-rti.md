# Faza 11 — Przerwania software (BRK, RTI)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.11, 7.4) |
| **Pokrycie całości** | 37% |
| **Zależności** | Fazy: 0, 1, 3, 9, 10 |
| **Szacowany czas** | 3–4h |

---

## Cel fazy

Implementacja BRK (software interrupt) i RTI (powrót z przerwania). BRK używa wektora $FFFE/$FFFF, pushuje PC+2 i P (z B=1). RTI przywraca P i PC ze stosu.

---

## Co implementujemy

| Instrukcja | Opcode | Bajty | Cykle |
|------------|--------|-------|-------|
| BRK | $00 | 1 | 7 |
| RTI | $40 | 1 | 6 |

### BRK — sekwencja 7 cykli

```
Cykl 1: Fetch opcode (BRK = $00)
Cykl 2: Dummy read (PC+1 — "signature byte" odczytany i zignorowany)
Cykl 3: Push PCH  → SP--, write $0100+SP
Cykl 4: Push PCL  → SP--, write $0100+SP
Cykl 5: Push P (z B=1, bit5=1) → SP--
Cykl 6: Fetch PCL z $FFFE
Cykl 7: Fetch PCH z $FFFF, ustaw PC
```

- BRK pushuje **PC+2**, nie PC+1. Dzięki temu byte po BRK może być użyty jako "break signature" (np. numer powodu BRK).
- BRK **nie** ustawia flagi I (w przeciwieństwie do sprzętowego IRQ).
- Pushed P ma B=1 (bit 4 ustawiony), bit5=1.

### RTI — sekwencja 6 cykli

```
Cykl 1: Fetch opcode
Cykl 2: Dummy read
Cykl 3: SP++ (no memory access)
Cykl 4: Pull P  → SP++, read $0100+SP
Cykl 5: Pull PCL → SP++, read $0100+SP
Cykl 6: Pull PCH → SP++, read $0100+SP, ustaw PC
```

### Pseudokod

```csharp
void ExecuteBRK()
{
    Push((byte)(PC >> 8));      // PCH
    Push((byte)(PC & 0xFF));    // PCL (uwaga: PC+2 przed push — BRK jest 2-bajtowy)
    Push((byte)(P | FlagB | FlagU)); // P z B=1
    byte lo = memory.Read(0xFFFE);
    byte hi = memory.Read(0xFFFF);
    PC = (ushort)(hi << 8 | lo);
}

void ExecuteRTI()
{
    P = Pull();
    byte pcl = Pull();
    byte pch = Pull();
    PC = (ushort)(pch << 8 | pcl);
}
```

### Ważne

- BRK: `PC` w momencie pushowania to adres instrukcji BRK + 2 (pomijamy opcode i signature byte).
- RTI: przywraca P w całości — B i bit5 ze stosu są zachowane w emulowanym P (w realnym hardware B i bit5 nie istnieją w rejestrze, ale dla zgodności z testami warto ustawić je z pull).

---

## Co testujemy

| Test | Opis |
|------|------|
| **BRK pushuje PC+2** | Sprawdź wartości na stosie |
| **BRK pushuje P z B=1** | Bit 4 = 1 na stosie |
| **BRK skacze do wektora IRQ** | PC = wartość z $FFFE/$FFFF |
| **BRK nie ustawia I** | Flaga I bez zmian po BRK |
| **RTI przywraca PC i P** | Po BRK → handler → RTI: PC i flagi sprzed BRK |
| **RTI z B=0 na stosie** | P po RTI może mieć B=0 |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 4.11 | Przerwania (BRK, RTI) |
| 7.4 | Instrukcja BRK |
| 7.10 | RTI |
| 7.2 | Wektory przerwań |

---

## Definition of Done

- [ ] BRK pushuje PC+2, P z B=1
- [ ] BRK skacze do $FFFE/$FFFF
- [ ] RTI przywraca stan sprzed przerwania
- [ ] Cykle: BRK=7, RTI=6
- [ ] 6 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/InterruptTests.cs` | Utwórz |
