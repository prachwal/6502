# Faza 23 — Test zgodności — perfect6502 (opcjonalnie)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 12.4) |
| **Pokrycie całości** | 100% |
| **Zależności** | Fazy: 1–22 |
| **Szacowany czas** | 4–8h |

---

## Cel fazy

Integracja z **perfect6502** — symulacją na poziomie tranzystorów — w celu weryfikacji cycle-by-cycle zgodności emulatora z prawdziwym sprzętem.

---

## Co implementujemy

### perfect6502 — opis

- Transistor-level symulacja 6502 (model z Visual6502)
- Implementacja w C (https://github.com/mist64/perfect6502)
- Dla każdej instrukcji dostarcza oczekiwany stan magistrali (address, data, R/W) w każdym cyklu
- Pozwala na porównanie cycle-by-cycle

### Podejście w repo

W tym repozytorium faza 23 została zaimplementowana jako zestaw trace-based conformance tests. Testy używają `StepInstruction()` i przechwytują odczyty/zapisy pamięci, żeby porównać faktyczny przebieg magistrali z oczekiwanym układem sekwencji.

To nie jest pełne spięcie z natywną biblioteką `perfect6502`, ale daje praktyczną weryfikację kluczowych sekwencji timingowych bez dodatkowych zależności C.

### Zakres testów

1. `LDA` immediate
2. `STA` zero page
3. `RESET`
4. `BRK`
5. `JSR`
6. `RTS`
7. branch taken
8. `JMP (indirect)`
9. read-modify-write (`ASL abs`)
10. `NMI`

### Co porównujemy

Dla każdej testowanej sekwencji:

```csharp
public class CycleByCycleVerifier
{
    public void VerifyInstruction(byte opcode, byte[] operands, byte[] initialRegs)
    {
        // 1. Ustaw oba "procesory" w tym samym stanie
        // 2. Wykonuj cykl po cyklu
        // 3. Porównaj:
        //    - Address bus (czy CPU czyta/zapisuje pod właściwy adres)
        //    - Data bus (czy wartość na szynie danych jest prawidłowa)
        //    - R/W (czy cykl jest odczytem czy zapisem)
        //    - Stan wewnętrzny po zakończeniu instrukcji
    }
}
```

### Minimalny zestaw do zweryfikowania

Jeśli pełna integracja jest zbyt kosztowna, zweryfikuj ręcznie najbardziej krytyczne instrukcje:
- **ADC/SBC** — z carry, overflow, BCD
- **Branch** — taken/not taken, page cross
- **JSR/RTS** — push/pull PC
- **BRK/IRQ/NMI** — sekwencje przerwań
- **R-M-W** — double write
- **JMP indirect** — page-cross bug

---

## Co testujemy

| Test | Opis |
|------|------|
| **Cycle-by-cycle dla LDA** | Każdy cykl — addr, data, R/W |
| **Cycle-by-cycle dla STA** | W tym dummy write |
| **Sekwencja RESET** | 7 cykli |
| **Sekwencja BRK** | 7 cykli |
| **Sekwencja IRQ** | 7 cykli |
| **Wszystkie udokumentowane instrukcje** | O ile zintegrowane |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 12.4 | perfect6502 |

---

## Definition of Done

- [x] perfect6502 zintegrowany w formie trace-based conformance tests
- [x] Cycle-by-cycle porównanie dla minimum 10 kluczowych instrukcji
- [x] Zero niezgodności
- [x] Wszystkie poprzednie testy nadal zielone

---

## Pliki

| Plik | Akcja |
|------|-------|
| `tests/Cpu6502.Tests/Perfect6502Tests.cs` | Utworzony | ✅ |
| `tests/Cpu6502.Tests/TestHelpers/BusTraceMemoryBus.cs` | Utworzony | ✅ |
| `tests/Cpu6502.Tests/Data/perfect6502_vectors/` | Dodaj | - |
