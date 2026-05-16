# Aplikacja 6502 Emulator - Konfiguracja i Flow

## Opis projektu

To jest **symulator procesora MOS 6502** napisany w C# z wykorzystaniem platformy .NET. Celem projektu jest stworzenie **cycle-accurate** (dokładnego co do cykli) symulatora, który:

- Wykonuje wszystkie udokumentowane instrukcje 6502 z poprawnym timingu
- Wykonuje wszystkie nieudokumentowane (illegal) opkody NMOS 6502
- Poprawnie obsługuje przerwania (IRQ, NMI, RESET, BRK) z uwzględnieniem dokładnego timingu
- Zaliczy testy zgodności binarnych: nestest, Klaus Dormann Functional Test Suite, Wolfgang Lorenz Test Suite, perfect6502

## Architektura

### Struktura katalogów

```
6502Emulator/
├── src/
│   └── Cpu6502/           # Główna biblioteka symulatora
│       ├── Cpu6502.cs     # Klasa procesora 6502
│       ├── IMemoryBus.cs  # Interfejs magistrali pamięci
│       └── CpuState.cs    # Klasa stanu procesora
├── tests/
│   └── Cpu6502.Tests/     # Testy jednostkowe
│       └── SkeletonTests.cs
├── docs/                   # Dokumentacja faz implementacji
│   ├── faza-00-szkielet.md
│   ├── faza-01-load-store.md
│   └── ... (do faza-23)
└── appconfig.md           # Ten plik
```

### Kluczowe komponenty

1. **IMemoryBus** - Interfejs definiujący operacje odczytu/zapisu pamięci. Umożliwia Dependency Injection pamięci, co pozwala na mapowanie I/O, bank switching i mirroring adresów.

2. **Cpu6502** - Główna klasa symulatora procesora. Zawiera:
   - Rejestry: A, X, Y, PC, SP, P
   - Stan wewnętrzny: IR (Instruction Register), Sync, Cycle
   - Metody: `Tick()`, `Reset()`, `GetFlag()`, `SetFlag()`, `GetState()`, `SetState()`

3. **CpuState** - Klasa przechowująca cały stan procesora w jednym obiekcie.

## Workflow implementacji faz

### Standardowy flow implementacji fazy

1. **Odczyt specyfikacji fazy**
   ```bash
   # Przejście do dokumentacji fazy
   cd docs/
   # Odczyt faza-XX-*.md
   ```

2. **Zaprojektowanie implementacji**
   - Analiza wymagań z dokumentacji
   - Projekt klas i interfejsów
   - Plan testów jednostkowych

3. **Implementacja**
   - Dodanie nowych plików do `src/Cpu6502/`
   - Modyfikacja `Cpu6502.cs` jeśli to konieczne
   - Dodanie testów do `tests/Cpu6502.Tests/`

4. **Testowanie**
   ```bash
   dotnet build
   dotnet test --logger "console;verbosity=detailed"
   ```

5. **Commit**
   ```bash
   git add .
   git commit -m "feat: implementacja fazy XX - <opis>"
   ```

6. **Aktualizacja dokumentacji**
   - Zaktualizowanie statusu fazy w `checklista.md`
   - Dodanie daty zakończenia w pliku fazy

### Kryteria przyjęcia (Definition of Done)

Każda faza jest uznawana za zakończoną tylko gdy:

- [ ] Kod się kompiluje bez ostrzeżeń
- [ ] Wszystkie testy jednostkowe przechodzą (zielone)
- [ ] Kod jest commitowany do git
- [ ] Dokumentacja jest zaktualizowana

## Tabela faz implementacji

| # | Faza | Opis | Status |
|---|------|------|--------|
| 0 | Szkielet | Struktura .NET, IMemoryBus, Cpu6502 | ✅ Zakończone |
| 1 | Load/Store | LDA, LDX, LDY, STA, STX, STY (wszystkie tryby) | ✅ Zakończone |
| 2 | Transfer | TAX, TAY, TSX, TXA, TXS, TYA | ⏳ Do zrobienia |
| 3 | Flagi CLC/SEC | CLC, SEC, CLD, SED, CLI, SEI, CLV | ⏳ Do zrobienia |
| 4 | Arytmetyka | ADC, SBC (bez BCD) | ⏳ Do zrobienia |
| 5 | Inc/Dec | INC, DEC, INX, INY, DEX, DEY | ⏳ Do zrobienia |
| 6 | Porównania | CMP, CPX, CPY, BIT | ⏳ Do zrobienia |
| 7 | Logiczne | AND, ORA, EOR | ⏳ Do zrobienia |
| 8 | Shift/Rotate | ASL, LSR, ROL, ROR | ⏳ Do zrobienia |
| 9 | Skoki | JMP, JSR, RTS, BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS | ⏳ Do zrobienia |
| 10 | Stos/NOP | PHA, PHP, PLA, PLP, NOP | ⏳ Do zrobienia |
| 11 | Przerwania SW | BRK, RTI | ⏳ Do zrobienia |
| 12 | Adresowanie | Pełne tryby + page crossing | ⏳ Do zrobienia |
| 13 | BCD | ADC/SBC decimal mode | ⏳ Do zrobienia |
| 14 | RESET | Sekwencja resetu | ⏳ Do zrobienia |
| 15 | IRQ/NMI | Przerwania sprzętowe | ⏳ Do zrobienia |
| 16 | Cycle-stepped | Tick() per cykl | ⏳ Do zrobienia |
| 17 | R-M-W | Double write + quirk JMP indirect | ⏳ Do zrobienia |
| 18 | Illegal stable | Nieudokumentowane opkody - stabilne | ⏳ Do zrobienia |
| 19 | Illegal unstable | Nieudokumentowane opkody - niestabilne | ⏳ Do zrobienia |
| 20 | Nestest | Test zgodności nestest | ⏳ Do zrobienia |
| 21 | Klaus | Test zgodności Klaus | ⏳ Do zrobienia |
| 22 | Wolfgang | Test zgodności Wolfgang | ⏳ Do zrobienia |
| 23 | Perfect6502 | Test zgodności perfect6502 | ⏳ Do zrobienia |

## Narzędzia i technologie

- **Język:** C# 12
- **Framework:** .NET 10.0
- **Testy:** NUnit 4.3.2
- **Build:** .NET CLI

## Linki pomocnicze

- [MOS 6502 Programming Reference](https://www.obelisk.demon.co.uk/6502/)
- [6502.org](http://6502.org/)
- [Nestest ROM test](https://github.com/frederic-maraud/nestest)
