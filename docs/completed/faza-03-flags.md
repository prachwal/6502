# Faza 3 — Flagi Set/Clear (CLC, SEC, CLD, SED, CLI, SEI, CLV)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.10 Flagi Set/Clear) |
| **Pokrycie całości** | 7% |
| **Zależności** | Faza 0 |
| **Szacowany czas** | 1h |
| **Data zakończenia** | 2026-05-16 |
| **Liczba testów** | 13 |

---

## Cel fazy

Implementacja 7 instrukcji ustawiających i czyszczących pojedyncze flagi statusowe. Wszystkie w trybie implied, 1 bajt, 2 cykle.

---

## Co implementujemy

### Lista instrukcji

| Instrukcja | Opcode | Efekt | Opis |
|------------|--------|-------|------|
| **CLC** | $18 | C=0 | Clear Carry |
| **SEC** | $38 | C=1 | Set Carry |
| **CLD** | $D8 | D=0 | Clear Decimal |
| **SED** | $F8 | D=1 | Set Decimal |
| **CLI** | $58 | I=0 | Clear Interrupt Disable |
| **SEI** | $78 | I=1 | Set Interrupt Disable |
| **CLV** | $B8 | V=0 | Clear Overflow |

### Pseudokod

```csharp
case 0x18: // CLC
    P &= ~FlagC;  // Clear carry flag
    break;

case 0x38: // SEC
    P |= FlagC;   // Set carry flag
    break;

case 0xD8: // CLD
    P &= ~FlagD;  // Clear decimal flag
    break;

case 0xF8: // SED
    P |= FlagD;   // Set decimal flag
    break;

case 0x58: // CLI
    P &= ~FlagI;  // Clear interrupt disable flag
    break;

case 0x78: // SEI
    P |= FlagI;   // Set interrupt disable flag
    break;

case 0xB8: // CLV
    P &= ~FlagV;  // Clear overflow flag
    break;
```

### Uwagi

- Każda instrukcja modyfikuje **tylko jedną** flagę. Pozostałe pozostają bez zmian.
- Wszystkie 2 cykle, 1 bajt.
- `CLI` i `SEI` mają opóźnienie 1 instrukcji dla efektu na przerwania — ale to dotyczy logiki przerwań, nie samego ustawienia flagi.

---

## Co testujemy

| Test | Opis |
|------|------|
| **CLC czyści C** | Ustaw C=1, wykonaj CLC → C=0, inne flagi bez zmian |
| **SEC ustawia C** | Ustaw C=0, wykonaj SEC → C=1 |
| **CLD czyści D** | Ustaw D=1, wykonaj CLD → D=0 |
| **SED ustawia D** | Ustaw D=0, wykonaj SED → D=1 |
| **CLI czyści I** | Ustaw I=1, wykonaj CLI → I=0 |
| **SEI ustawia I** | Ustaw I=0, wykonaj SEI → I=1 |
| **CLV czyści V** | Ustaw V=1, wykonaj CLV → V=0 |
| **Brak wpływu na inne flagi** | Przed każdą instrukcją ustaw wszystkie flagi na przeciwne wartości i sprawdź, że tylko właściwa flaga się zmieniła |

### Test jednostkowy — przykład

```csharp
[Test]
public void CLC_ClearsCarry()
{
    var cpu = CreateCpu();
    cpu.SetFlag(FlagC, true);  // C=1
    cpu.SetFlag(FlagN, true);  // N=1 — powinna zostać
    cpu.Execute(0x18);         // CLC
    Assert.IsFalse(cpu.GetFlag(FlagC));  // C=0
    Assert.IsTrue(cpu.GetFlag(FlagN));   // N nadal 1
}
```

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 4.10 | Flagi (Set/Clear) — pełna tabela |
| 2.2 | Rejestr statusowy — opisy flag C, D, I, V |

---

## Definition of Done

- [x] Wszystkie 7 instrukcji zaimplementowanych
- [x] Każda modyfikuje tylko swoją flagę
- [x] Wszystkie testy jednostkowe zielone (13/13)
- [x] Kod bez ostrzeżeń

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.FlagsSetClear.cs` | Implementacja 7 instrukcji (partial class) |
| `src/Cpu6502/Cpu6502.Constructor.cs` | Inicjalizacja opcode'ów w konstruktorze |
| `tests/Cpu6502.Tests/FlagsSetClearTests.cs` | 13 testów jednostkowych |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 97/97 (100%)

---

## Pliki do utworzenia / modyfikacji

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — dodaj case'y |
| `tests/Cpu6502.Tests/FlagTests.cs` | Utwórz |
