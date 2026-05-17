# AGENTS.md - Konfiguracja agentów dla projektu 6502 Emulator

## 📌 Opis projektu

To jest **symulator procesora MOS 6502** napisany w C# z wykorzystaniem platformy .NET. Celem projektu jest stworzenie **cycle-accurate** (dokładnego co do cykli) symulatora, który:

- Wykonuje wszystkie udokumentowane instrukcje 6502 z poprawnym timingu
- Wykonuje wszystkie nieudokumentowane (illegal) opkody NMOS 6502
- Poprawnie obsługuje przerwania (IRQ, NMI, RESET, BRK) z uwzględnieniem dokładnego timingu
- Zaliczy testy zgodności binarnych: nestest, Klaus Dormann Functional Test Suite, Wolfgang Lorenz Test Suite, perfect6502

---

## 🗂️ Struktura projektu

```
6502Emulator/
├── src/
│   └── Cpu6502/           # Główna biblioteka symulatora
│       ├── Cpu6502.*.cs   # Pliki partial class (modularna struktura)
│       │   ├── Constants.cs    # Stałe flag (C, Z, I, D, B, U, V, N)
│       │   ├── Fields.cs       # Pola rejestrowe i zależności
│       │   ├── CycleStepped.Core.cs  # Inicjalizacja opcode'ów i dispatch cykli
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
│   ├── checklista.md      # Lista faz i ich statusów
│   └── faza-XX-*.md       # Specyfikacje poszczególnych faz
└── .kilo/                  # Konfiguracja Kilo
    ├── kilo.json          # Główna konfiguracja agentów
    ├── agents/            # Konfiguracje subagentów
    │   ├── cpu-implementer.md
    │   ├── test-writer.md
    │   ├── opcode-researcher.md
    │   ├── code-reviewer.md
    │   ├── debugger.md
    │   ├── documentation-writer.md
    │   └── performance-analyzer.md
    ├── rules/              # Reguły i wytyczne
    │   ├── coding-standards.md
    │   ├── testing-guidelines.md
    │   ├── opcode-implementation.md
    │   └── documentation-rules.md
    └── templates/          # Szablony dla subagentów
        ├── cpu-implementer-template.md
        └── test-writer-template.md
```

---

## 📋 Referencje do dokumentacji

- **Lista faz i statusów:** [`docs/checklista.md`](docs/checklista.md)
- **Specyfikacje faz:** [`docs/faza-XX-*.md`](docs/)
- **Zasady pisania kodu:** [`.kilo/rules/coding-standards.md`](.kilo/rules/coding-standards.md)
- **Wytyczne testowania:** [`.kilo/rules/testing-guidelines.md`](.kilo/rules/testing-guidelines.md)
- **Implementacja opcode'ów:** [`.kilo/rules/opcode-implementation.md`](.kilo/rules/opcode-implementation.md)
- **Zasady dokumentacji:** [`.kilo/rules/documentation-rules.md`](.kilo/rules/documentation-rules.md)

---

## 🚀 Workflow implementacji faz

Każda faza implementacji **MUSI** być realizowana zgodnie ze specyfikacją w `docs/faza-XX-*.md`.

### Standardowy flow (krok po kroku):

1. **Odczyt specyfikacji fazy** - Przeczytaj plik `docs/faza-XX-*.md`
2. **Zaprojektowanie** - Analiza wymagań i plan implementacji
3. **Implementacja** - Dodaj nowe pliki/kod
4. **Testowanie** - `dotnet build && dotnet test`
5. **Commit** - `git add . && git commit -m "feat: implementacja fazy XX"`
6. **Aktualizacja dokumentacji** - Zaktualizuj status w `docs/checklista.md` oraz plik `docs/faza-XX-*.md`:
   - Zmień status z `[ ] Nie rozpoczęte` na `[x] Zakończone`
   - Dodaj `Data zakończenia` (YYYY-MM-DD)
   - Dodaj `Liczba testów`
   - Dodaj sekcję **"Pliki implementacyjne"** z listą plików
   - Dodaj sekcję **"Wyniki"** z Build/Test
   - Dodaj tabelę opcode'ów (jeśli dotyczy)

### Kryteria przyjęcia (Definition of Done):

- [ ] Kod się kompiluje bez ostrzeżeń
- [ ] Wszystkie testy jednostkowe przechodzą (zielone)
- [ ] Kod jest commitowany do git
- [ ] Dokumentacja jest zaktualizowana

---

## 📜 Style codebase

### Pliki partial class

Kod źródłowy jest podzielony na wiele plików **partial class** w celu lepszej organizacji:

| Plik | Zawartość |
|------|-----------|
| `Cpu6502.Constants.cs` | Stałe flag procesora (C, Z, I, D, B, U, V, N) |
| `Cpu6502.Fields.cs` | Pola rejestrowe (A, X, Y, SP, PC, P) i zależności |
| `Cpu6502.CycleStepped.Core.cs` | Inicjalizacja opcode'ów i dispatch cykli |
| `Cpu6502.Properties.cs` | Właściwości publiczne |
| `Cpu6502.AddressingModes.cs` | Tryby adresowania (Immediate, Zero Page, Absolute, etc.) |
| `Cpu6502.Flags.cs` | Metody pracy z flagami (SetNZ, SetFlag, GetFlag) |
| `Cpu6502.LoadStore.cs` | Instrukcje load/store (LDA, LDX, LDY, STA, STX, STY) |
| `Cpu6502.Transfer.cs` | Instrukcje transferu (TAX, TAY, TSX, TXA, TXS, TYA) |
| `Cpu6502.FlagsSetClear.cs` | Flagi Set/Clear (CLC, SEC, CLD, SED, CLI, SEI, CLV) |
| `Cpu6502.Arithmetic.cs` | Arytmetyka (ADC, SBC) |
| `Cpu6502.Logic.cs` | Operacje logiczne (AND, OR, EOR) |
| `Cpu6502.CompareBit.cs` | Porównania i test bitu (CMP, CPX, CPY, BIT) |
| `Cpu6502.IncDec.cs` | Inkrementacja/Dekrementacja (INC, DEC, INX, DEX, INY, DEY) |
| `Cpu6502.ShiftRotate.cs` | Shift/Rotate (ASL, LSR, ROL, ROR) |
| `Cpu6502.BranchJump.cs` | Skoki (JMP, JSR, RTS, RTI, BCC, BCS, etc.) |
| `Cpu6502.StackNop.cs` | Stos i NOP (PHA, PHP, PLA, PLP, NOP) |
| `Cpu6502.InterruptsSw.cs` | Przerwania programowe (BRK, RTI) |
| `Cpu6502.Reset.cs` | RESET |
| `Cpu6502.IrqNmi.cs` | IRQ/NMI |
| `Cpu6502.PublicMethods.cs` | Metody publiczne (Tick, Reset, GetState, SetState) |
| `Cpu6502.Placeholders.cs` | Placeholder dla niezaimplementowanych opcode |

**Szczegółowe wytyczne:** [`.kilo/rules/coding-standards.md`](.kilo/rules/coding-standards.md)

---

## 🛠️ Narzędzia

- **Język:** C# 12
- **Framework:** .NET 10.0
- **Testy:** NUnit 4.3.2
- **Build:** .NET CLI

---

## 🤖 Subagenci i ich role

Projekt korzysta z **subagentów** do specjalistycznych zadań. Każdy subagent ma ściśle zdefiniowaną rolę i **MUSI** przestrzegać reguł opisanych w `.kilo/agents/` i `.kilo/rules/`.

### Dostępni subagenci

| Subagent | Opis | Plik konfiguracyjny | Model |
|---------|------|---------------------|-------|
| **cpu-implementer** | Implementuje fazy symulatora 6502 | `.kilo/agents/cpu-implementer.md` | `mistral-medium-2604` |
| **test-writer** | Tworzy testy jednostkowe | `.kilo/agents/test-writer.md` | `mistral-medium-2604` |
| **opcode-researcher** | Bada opcode'y i ich zachowanie | `.kilo/agents/opcode-researcher.md` | `mistral-small-2603` |
| **code-reviewer** | Recenzuje kod | `.kilo/agents/code-reviewer.md` | `mistral-medium-2604` |
| **debugger** | Debuguje problemy | `.kilo/agents/debugger.md` | `mistral-medium-2604` |
| **documentation-writer** | Tworzy dokumentację | `.kilo/agents/documentation-writer.md` | `mistral-small-2603` |
| **performance-analyzer** | Analizuje wydajność | `.kilo/agents/performance-analyzer.md` | `mistral-small-2603` |

**Konfiguracja głównych agentów:** [`.kilo/kilo.json`](.kilo/kilo.json)

---

## 🎯 Zasady delegacji zadań do subagentów

1. **Jedna faza na raz** - Zlecaj implementację **tylko jednej fazy** na raz.
2. **Partial classes** - Nowe instrukcje **MUSZĄ** być dodawane jako **nowe pliki partial class** (`Cpu6502.*.cs`).
3. **Testy jednostkowe** - Każda nowa funkcjonalność **MUSI** mieć testy.
4. **Weryfikacja** - Po implementacji **ZAWSZE** uruchom `dotnet build && dotnet test`.
5. **Commit** - Po zakończeniu fazy **ZAWSZE** zrób commit z opisem `feat: implementacja fazy XX`.
6. **Dokumentacja** - Zaktualizuj **ZAWSZE** dokumentację fazy i `docs/checklista.md`.

**Szablony dla subagentów:** [`.kilo/templates/`](.kilo/templates/)

---

## 📌 Przykładowe polecenia CLI

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

## 📌 Podsumowanie

| Element | Wymaganie |
|---------|------------|
| **Język** | C# 12 |
| **Framework** | .NET 10.0 |
| **Testy** | NUnit 4.3.2 |
| **Struktura** | Partial classes |
| **Komentarze** | XML dla publicznych metod |
| **Testy** | Wymagane dla każdej funkcjonalności |
| **Dokumentacja** | Wymagana dla każdej fazy |
| **Subagenci** | 7 dostępnych (cpu-implementer, test-writer, etc.) |
| **Reguły** | `.kilo/rules/` |
