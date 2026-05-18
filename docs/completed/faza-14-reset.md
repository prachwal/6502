# Faza 14 — Sekwencja RESET

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 2.8, 8) |
| **Pokrycie całości** | 50% |
| **Zależności** | Fazy: 0, 1, 3, 9, 10 |
| **Szacowany czas** | 2–3h |
| **Data zakończenia** | 2026-05-17 |
| **Liczba testów** | 7 |

---

## Cel fazy

Implementacja poprawnej sekwencji RESET — 7-cyklowej procedury startowej, która ładuje PC z wektora $FFFC/$FFFD i inicjalizuje stan procesora.

---

## Co implementujemy

### Sekwencja RESET — 7 cykli

```
Cykl 0: Reset aktywny → wymuszenie IR = 0x00 (BRK)
Cykl 1: Dummy read (adres "śmieciowy" zależny od stanu)
Cykl 2: Dummy read
Cykl 3: Dummy read $0100+SP (symulacja push PCH), SP--
Cykl 4: Dummy read $0100+SP (symulacja push PCL), SP--
Cykl 5: Dummy read $0100+SP (symulacja push P),   SP--
Cykl 6: Fetch PCL z $FFFC
Cykl 7: Fetch PCH z $FFFD → ustaw PC
Cykl 8: Normalny fetch pierwszej instrukcji
```

### Stan po resecie

```csharp
public void Reset()
{
    A = 0;        // niezdefiniowane w realnym HW — zerujemy dla determinizmu
    X = 0;
    Y = 0;
    SP = 0xFD;    // po 3 pseudo-pushach (start od $00, wrap → $FD)
    P = FlagI;    // I=1, reszta flag 0 (w realnym HW inne flagi nieokreślone)
    // D=0, reszta nieokreślona

    byte lo = memory.Read(0xFFFC);
    byte hi = memory.Read(0xFFFD);
    PC = (ushort)(hi << 8 | lo);

    Sync = true;  // następny Tick() pobierze pierwszą instrukcję
    Cycle = 0;
}
```

### Uproszczona wersja vs pełna cycle-stepped

W modelu instruction-stepped wystarczy wywołać `Reset()`. W modelu cycle-stepped (faza 16) każdy cykl RESET będzie osobnym case'em w `Tick()`.

---

## Co testujemy

| Test | Opis |
|------|------|
| **Reset ładuje PC z wektora** | $FFFC=$00, $FFFD=$C0 → PC=$C000 |
| **SP = $FD po resecie** | SP ustawiony na $FD |
| **I = 1 po resecie** | Interrupt disable |
| **D = 0 po resecie** | Decimal mode wyłączony |
| **A=0, X=0, Y=0** | Rejestry wyzerowane |
| **Pierwsza instrukcja po resecie** | Tick() wykonuje instrukcję spod PC |
| **Reset sequence = 7 cykli** | W modelu cycle-stepped |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 2.8 | Inicjalizacja i reset |
| 8 | Sekwencja RESET — przebieg cykl po cyklu |
| 2.5 | Wektory systemowe |

---

## Definition of Done

- [x] Reset() ustawia wszystkie rejestry wg specyfikacji
- [x] PC pobierany z $FFFC/$FFFD
- [x] SP = $FD, I = 1, D = 0
- [x] Po resecie procesor gotowy do Tick()
- [x] 7 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |

## Pliki implementacyjne

- `Cpu6502.PublicMethods.cs`: Zmodyfikowana metoda Reset()
- `ResetTests.cs`: 7 testów jednostkowych
- `SkeletonTests.cs`: Zaktualizowane testy szkieletowe

## Wyniki

- Build: ✅ 0 błędów, 7 ostrzeżeń (nullable references)
- Testy: ✅ 193/193 (100%)
- Testy regresyjne: ✅ Wszystkie poprzednie instrukcje nadal działają
- Nowe testy: ✅ 7/7 testów RESET passing

## Tabela testów RESET

| Test | Opis | Wynik |
|------|------|-------|
| Reset_LoadsPCFromResetVector | PC z $FFFC/$FFFD | ✅ |
| Reset_SPEqualsFd | SP = $FD po resecie | ✅ |
| Reset_IFlagSet | I = 1 (interrupt disable) | ✅ |
| Reset_DFlagClear | D = 0 (decimal mode off) | ✅ |
| Reset_RegistersZeroed | A=0, X=0, Y=0 | ✅ |
| Reset_FirstInstructionExecution | Pierwsza instrukcja po resecie | ✅ |
| Reset_MultipleResetsConsistent | Wielokrotne resety spójne | ✅ |
|------|-------|
| `src/Cpu6502/Cpu6502.PublicMethods.cs` | Modyfikuj — dokończ Reset() |
| `tests/Cpu6502.Tests/ResetTests.cs` | Utwórz |
| `tests/Cpu6502.Tests/SkeletonTests.cs` | Zaktualizuj — popraw flagi RESET |
