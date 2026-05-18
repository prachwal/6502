# Faza 21 — Test zgodności — Klaus Dormann Functional Test

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 4% (sekcje: 12.2) |
| **Pokrycie całości** | 93% |
| **Zależności** | Fazy: 1–20 |
| **Szacowany czas** | 4–8h |
| **Data zakończenia** | 2026-05-18 |
| **Liczba testów** | 4 |

---

## Cel fazy

Zintegrowanie i przejście **Klaus Dormann 6502 Functional Test Suite** — bardziej zaawansowanego testu który sprawdza BCD, wszystkie tryby adresowania i edge case'y.

---

## Co implementujemy

### Klaus test — opis

- ROM binarny: `6502_functional_test.bin`
- Adres startowy: $0400 (lub $0000 w niektórych wariantach)
- Testuje: wszystkie udokumentowane instrukcje, tryb BCD, wszystkie tryby adresowania, edge case'y flag
- Raportuje błędy przez zapis do pamięci (adresy $2000+ lub podobne)
- Sukces: program wchodzi w nieskończoną pętlę pod znanym adresem

### Test harness

```csharp
public class KlausTestRunner
{
    public bool Run(IMemoryBus memory, Cpu6502 cpu)
    {
        // 1. Załaduj ROM pod $0400
        // 2. Ustaw wektor RESET na $0400
        cpu.Reset();

        // 3. Uruchom CPU na określoną liczbę cykli lub do momentu pętli
        ulong maxCycles = 100_000_000;  // timeout
        while (cpu.Cycle < maxCycles)
        {
            cpu.Tick();

            // Sprawdź adres pętli sukcesu
            if (cpu.PC == SUCCESS_ADDRESS)
                return true;

            // Sprawdź adresy błędów
            if (IsErrorReported(memory))
                return false;
        }
        return false;  // timeout
    }
}
```

### Dwa warianty

1. **Bez BCD** — testuje wszystkie instrukcje poza ADC/SBC w trybie decimal
2. **Z BCD** — pełny test z decimal mode
3. **Cpu6502Nes** — wariant NES jest świadomie pomijany dla Klaus functional, bo ten ROM wymaga decimal mode

Należy przejść oba.

### Oczekiwane zachowanie

- Test wykonuje sekwencje instrukcji i porównuje wyniki
- Jeśli wynik nieprawidłowy — zapisuje kod błędu do pamięci
- Jeśli wszystkie testy OK — wchodzi w pętlę (np. `JMP $xxxx` w kółko)

---

## Co testujemy

| Test | Opis |
|------|------|
| **Klaus non-BCD** | Wszystkie testy poza BCD zaliczone |
| **Klaus BCD** | ADC/SBC w trybie decimal zaliczone |
| **Wszystkie instrukcje udokumentowane** | Każda przetestowana |
| **Wszystkie tryby adresowania** | Każdy tryb weryfikowany |
| **Edge case'y flag** | N, V, Z, C w skrajnych przypadkach |
| **Brak timeout** | Test nie przekracza limitu cykli |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 12.2 | Klaus Dormann Functional Test |
| 12.5 | Testy BCD |
| 12.6 | Strategia implementacji testów |

---

## Definition of Done

- [x] ROM Klaus załadowany i uruchomiony
- [x] Test non-BCD przechodzi (sukces)
- [x] Test BCD przechodzi (sukces)
- [x] Żadnych kodów błędów w pamięci
- [x] CPU wchodzi w pętlę sukcesu (mechanizm detekcji zaimplementowany)

---

## Wyniki

### Build
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Testy
```
Total tests: 264
     Passed: 263
     Skipped: 1
     Failed: 0
```

### Uwagi
Infrastruktura testowa dla Klaus Dormann jest gotowa. Naprawiono decimal `ADC`, więc testy Klaus przechodzą w klasycznym wariancie CPU. Wariant `Cpu6502Nes` pozostaje celowo wyłączony z tego ROM-u, ponieważ Klaus functional wymaga zachowania decimal mode.

---

## Pliki

| Plik | Akcja | Status |
|------|-------|--------|
| `tests/Cpu6502.Tests/KlausTests.cs` | Utworzony | ✅ |
| `tests/Cpu6502.Tests/TestHelpers/KlausTestRunner.cs` | Utworzony | ✅ |
| `tests/Cpu6502.Tests/TestHelpers/KlausLogGenerator.cs` | Utworzony | ✅ |
| `tests/Cpu6502.Tests/Data/6502_functional_test.bin` | Dodany | ✅ |
| `tests/Cpu6502.Tests/Cpu6502.Tests.csproj` | Zaktualizowany (CopyToOutputDirectory) | ✅ |
| `src/Cpu6502/Variants/Cpu6502Classic.cs` | Utworzony (Faza 20.5) | ✅ |
| `src/Cpu6502/Variants/Cpu6502Nes.cs` | Utworzony (Faza 20.5) | ✅ |
| `src/Cpu6502/Variants/Cpu6502Factory.cs` | Utworzony (Faza 20.5) | ✅ |
