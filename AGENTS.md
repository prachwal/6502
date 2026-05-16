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
│       ├── Cpu6502.*.cs   # Pliki partial class (modularna struktura)
│       │   ├── Constants.cs    # Stałe flag (C, Z, I, D, B, U, V, N)
│       │   ├── Fields.cs       # Pola rejestrowe i zależności
│       │   ├── Constructor.cs  # Konstruktor i tabela opcode
│       │   ├── Properties.cs   # Właściwości publiczne
│       │   ├── AddressingModes.cs  # Tryby adresowania
│       │   ├── Flags.cs        # Metody pracy z flagami
│       │   ├── LoadStore.cs    # Instrukcje LDA, LDX, LDY, STA, STX, STY
│       │   ├── PublicMethods.cs    # Tick, Reset, GetState, SetState
│       │   └── Placeholders.cs   # Placeholder dla niezaimplementowanych opcode
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

---

## Style codebase

### Pliki partial class

Kod źródłowy jest podzielony na wiele plików partial class w celu lepszej organizacji:

| Plik | Zawartość |
|------|-----------|
| `Cpu6502.Constants.cs` | Stałe flag procesora |
| `Cpu6502.Fields.cs` | Pola rejestrowe i zależności |
| `Cpu6502.Constructor.cs` | Konstruktor i inicjalizacja |
| `Cpu6502.Properties.cs` | Właściwości publiczne |
| `Cpu6502.AddressingModes.cs` | Tryby adresowania |
| `Cpu6502.Flags.cs` | Metody pracy z flagami |
| `Cpu6502.LoadStore.cs` | Instrukcje load/store |
| `Cpu6502.PublicMethods.cs` | Metody publiczne (Tick, Reset, GetState, SetState) |
| `Cpu6502.Placeholders.cs` | Niezaimplementowane opcode'y |

### Zasady pisania kodu

- **Krótkie metody** - metody powinny mieć max 10-15 linii kodu
- **Dobre nazwy** - nazwy metod i zmiennych opisujące ich działanie
- **Komentarze XML** - każdy publiczny typ i metoda powinien mieć komentarz XML
- **Jedna odpowiedzialność** - każdy plik powinien mieć jasno określony zakres
- **Testy jednostkowe** - każda nowa funkcjonalność powinna mieć odpowiadające jej testy

## Narzędzia

- **Język:** C# 12
- **Framework:** .NET 10.0
- **Testy:** NUnit 4.3.2
- **Build:** .NET CLI

## Fazy implementacji

| # | Faza | Status |
|---|------|--------|
| 0 | Szkielet | ✅ Zakończone |
| 1 | Load/Store | ✅ Zakończone |
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

---

## Podział zadań dla subagenta

### Faza 2 - Transfer (TAX, TAY, TSX, TXA, TXS, TYA)

**Podzadania:**
1. Dodać plik `Cpu6502.Transfer.cs` z metodami:
   - `Tax()` - Transfer A do X
   - `Tay()` - Transfer A do Y
   - `Tsx()` - Transfer SP do X
   - `Txa()` - Transfer X do A
   - `Txs()` - Transfer X do SP
   - `Tya()` - Transfer Y do A (nieudokumentowana)
2. Zainicjalizować opcode'y w konstruktorze
3. Dodać testy w `TransferTests.cs`

### Faza 3 - Flagi Set/Clear (CLC, SEC, CLD, SED, CLI, SEI, CLV)

**Podzadania:**
1. Dodać plik `Cpu6502.FlagsSetClear.cs` z metodami:
   - `Clc()` - Clear Carry
   - `Sec()` - Set Carry
   - `Cld()` - Clear Decimal
   - `Sed()` - Set Decimal
   - `Cli()` - Clear Interrupt
   - `Sei()` - Set Interrupt
   - `Clv()` - Clear Overflow
2. Zainicjalizować opcode'y w konstruktorze
3. Dodać testy w `FlagsSetClearTests.cs`

### Faza 4 - Arytmetyka (ADC, SBC bez BCD)

**Podzadania:**
1. Dodać plik `Cpu6502.Arithmetic.cs` z metodami:
   - `AdcImm()`, `AdcZp()`, `AdcZpX()`, `AdcAbs()`, `AdcAbsX()`, `AdcAbsY()`, `AdcIndX()`, `AdcIndY()`
   - `SbcImm()`, `SbcZp()`, `SbcZpX()`, `SbcAbs()`, `SbcAbsX()`, `SbcAbsY()`, `SbcIndX()`, `SbcIndY()`
2. Zaimplementować ADC/SBC z prawidłowym obliczaniem Carry i Overflow
3. Dodać testy w `ArithmeticTests.cs`

**Wskazówka:** Użyj subagenta do implementacji poszczególnych grup instrukcji (load/store, transfer, flags, arithmetic).

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

---

## Task delegation guide (dla subagenta)

### Przykład delegacji

**Prompt dla subagenta:**
> "Implementuj Phase 2 - Transfer instructions (TAX, TAY, TSX, TXA, TXS, TYA) zgodnie z `docs/faza-02-transfer.md`. Stwórz plik `Cpu6502.Transfer.cs` z metodami dla każdej instrukcji, zainicjalizuj opcode'y w konstruktorze, dodaj testy w `TransferTests.cs`. Zadbaj o komentarze XML i testy jednostkowe."

### Zasady delegacji

1. **Jedna faza na raz** - zlecaj implementację jednej fazy zgodnie z `docs/faza-XX-*.md`
2. **Partial classes** - nowe instrukcje dodawaj jako nowe pliki partial class (`Cpu6502.*.cs`)
3. **Testy jednostkowe** - każda nowa funkcjonalność musi mieć testy
4. **Build i test** - po implementacji uruchom `dotnet build && dotnet test`
5. **Commit** - po zakończeniu fazy zrób commit z opisem `feat: implementacja fazy XX`
