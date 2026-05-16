# Faza 22 — Test zgodności — Wolfgang Lorenz Test Suite

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 5% (sekcje: 12.3) |
| **Pokrycie całości** | 97% |
| **Zależności** | Fazy: 1–21 |
| **Szacowany czas** | 8–16h |

---

## Cel fazy

Zintegrowanie i przejście **Wolfgang Lorenz Test Suite** — najbardziej zaawansowanego zestawu testów, który sprawdza timing, interrupty i nieudokumentowane opkody.

---

## Co implementujemy

### Wolfgang Lorenz — opis

- Zestaw testów dla emulatora C64 (CPU + CIA + VIC)
- Format: obrazy `.d64` Commodore 64
- Testuje:
  - Wszystkie opkody (w tym nieudokumentowane)
  - Dokładny timing (liczba cykli)
  - Przerwania (IRQ, NMI, timing, hijacking)
  - Branch quirki
  - Zachowanie Read-Modify-Write
  - Interakcję CPU z CIA (Complex Interface Adapter)

### Minimalne środowisko C64

Do uruchomienia testów Wolfganga potrzeba minimum:

```csharp
// Podstawowa emulacja C64:
// - CPU 6510 (6502 + port I/O $00/$01)
// - 64KB RAM
// - Podstawowa CIA 1/2 (timery dla przerwań)
// - Podstawowy VIC-II (raster interrupt w odpowiednich liniach)
// - ROM kernal/basic (lub stub)
```

### Architektura test harnessu

```csharp
public class WolfgangTestRunner
{
    private Cpu6502 cpu;      // lub Cpu6510
    private Cia cia1, cia2;
    private VicII vic;
    private byte[] ram = new byte[65536];

    public void RunTest(string d64Path)
    {
        // 1. Załaduj obraz .d64 do RAM (z obsługą mapowania pamięci C64)
        // 2. Uruchom CPU
        // 3. Każdy Tick() → tick CIA, VIC
        // 4. Sprawdź wyniki przez odczyt ekranu lub pamięci
    }

    public void Tick()
    {
        // Kolejność: CPU → CIA → VIC
        cpu.Tick();
        cia1.Tick();
        cia2.Tick();
        vic.Tick();

        // Sprawdź sygnały przerwań
        cpu.SetIRQ(cia1.IRQ || vic.IRQ);
        cpu.SetNMI(cia2.NMI);
    }
}
```

### Podział testów Wolfganga

Testy są podzielone na kategorie:
- `cpuport` — testy portu I/O 6510
- `cputiming` — testy timingu instrukcji
- `irq` — testy przerwań IRQ
- `nmi` — testy NMI
- `cia1`, `cia2` — testy CIA
- `vicii` — testy VIC-II

Dla czystego CPU, najważniejsze są testy z grupy `cpu*` i `irq`/`nmi`.

---

## Co testujemy

| Test | Opis |
|------|------|
| **Wszystkie testy CPU** | Poprawność wszystkich opcode'ów |
| **Testy timingu** | Dokładna liczba cykli |
| **Testy IRQ** | Poprawność obsługi IRQ |
| **Testy NMI** | Poprawność NMI, hijacking |
| **Testy nieudokumentowanych** | Poprawne działanie illegal opcodes |
| **Brak regresji nestest/Klaus** | Wcześniejsze testy nadal zielone |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 12.3 | Wolfgang Lorenz Test Suite |

---

## Definition of Done

- [ ] Minimalne środowisko C64 zaimplementowane
- [ ] Testy CPU z Wolfganga przechodzą
- [ ] Testy timingu przechodzą
- [ ] Testy przerwań (IRQ/NMI) przechodzą
- [ ] Wszystkie testy regresyjne (nestest, Klaus) nadal zielone

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6510.cs` | Utwórz (rozszerzenie 6502 o port $00/$01) |
| `src/C64/Cia.cs` | Utwórz (podstawowa emulacja CIA) |
| `src/C64/VicII.cs` | Utwórz (minimalny VIC-II) |
| `tests/Cpu6502.Tests/WolfgangTest.cs` | Utwórz |
