# AGENTS.md - Konfiguracja agentów dla projektu 6502 Emulator

## Opis projektu

To jest **symulator procesora MOS 6502** napisany w C# z wykorzystaniem platformy .NET. Celem projektu jest stworzenie **cycle-accurate** (dokładnego co do cykli) symulatora, który:

- Wykonuje wszystkie udokumentowane instrukcje 6502 z poprawnym timingu
- Wykonuje wszystkie nieudokumentowane (illegal) opkody NMOS 6502
- Poprawnie obsługuje przerwania (IRQ, NMI, RESET, BRK) z uwzględnieniem dokładnego timingu
- Zaliczy testy zgodności binarnych: nestest, Klaus Dormann Functional Test Suite, Wolfgang Lorenz Test Suite, perfect6502

## Struktura projektu

```
6502Emulator/
├── src/
│   └── Cpu6502/           # Główna biblioteka symulatora
│       ├── Cpu6502.cs     # Klasa procesora 6502
│       ├── IMemoryBus.cs  # Interfejs magistrali pamięci
│       └── CpuState.cs    # Klasa stanu procesora
├── tests/
│   └── Cpu6502.Tests/     # Testy jednostkowe
├── docs/                   # Dokumentacja faz implementacji
└── .kilo/                  # Konfiguracja Kilo
```

## Workflow implementacji faz

Każda faza implementacji powinna być realizowana zgodnie ze specyfikacją w `docs/faza-XX-*.md`.

### Standardowy flow (krok po kroku):

1. **Odczyt specyfikacji fazy** - Przeczytaj plik `docs/faza-XX-*.md`
2. **Zaprojektowanie** - Analiza wymagań i plan implementacji
3. **Implementacja** - Dodaj nowe pliki/kod
4. **Testowanie** - `dotnet build && dotnet test`
5. **Commit** - `git add . && git commit -m "feat: implementacja fazy XX"`
6. **Aktualizacja dokumentacji** - Zaktualizuj status w `checklista.md`

### Kryteria przyjęcia (Definition of Done):

- [ ] Kod się kompiluje bez ostrzeżeń
- [ ] Wszystkie testy jednostkowe przechodzą (zielone)
- [ ] Kod jest commitowany do git
- [ ] Dokumentacja jest zaktualizowana

## Narzędzia

- **Język:** C# 12
- **Framework:** .NET 10.0
- **Testy:** NUnit 4.3.2
- **Build:** .NET CLI

## Fazy implementacji

| # | Faza | Status |
|---|------|--------|
| 0 | Szkielet | ✅ Zakończone |
| 1 | Load/Store | ⏳ Do zrobienia |
| 2 | Transfer | ⏳ Do zrobienia |
| 3 | Flagi | ⏳ Do zrobienia |
| 4 | Arytmetyka | ⏳ Do zrobienia |
| 5 | Inc/Dec | ⏳ Do zrobienia |
| 6 | Porównania | ⏳ Do zrobienia |
| 7 | Logiczne | ⏳ Do zrobienia |
| 8 | Shift/Rotate | ⏳ Do zrobienia |
| 9 | Skoki | ⏳ Do zrobienia |
| 10 | Stos/NOP | ⏳ Do zrobienia |
| 11 | Przerwania SW | ⏳ Do zrobienia |
| 12 | Adresowanie | ⏳ Do zrobienia |
| 13 | BCD | ⏳ Do zrobienia |
| 14 | RESET | ⏳ Do zrobienia |
| 15 | IRQ/NMI | ⏳ Do zrobienia |
| 16 | Cycle-stepped | ⏳ Do zrobienia |
| 17 | R-M-W | ⏳ Do zrobienia |
| 18 | Illegal stable | ⏳ Do zrobienia |
| 19 | Illegal unstable | ⏳ Do zrobienia |
| 20 | Nestest | ⏳ Do zrobienia |
| 21 | Klaus | ⏳ Do zrobienia |
| 22 | Wolfgang | ⏳ Do zrobienia |
| 23 | Perfect6502 | ⏳ Do zrobienia |

## Przykładowe polecenia

```bash
# Budowanie
dotnet build

# Testy
dotnet test --logger "console;verbosity=detailed"

# Testy konkretnego projektu
dotnet test src/Cpu6502 --filter "Category=Unit"

# Restore packages
dotnet restore
```
