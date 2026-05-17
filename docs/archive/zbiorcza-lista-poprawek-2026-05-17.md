# Zbiorcza lista elementów do poprawy

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Zakres: dokumentacja, implementacja CPU 6502, testy, plany dalszych faz

---

## 1. Priorytety

| Priorytet | Znaczenie |
|---|---|
| P0 | Błąd krytyczny: bezpieczeństwo, błędna emulacja podstawowej funkcji CPU albo problem blokujący dalszy rozwój |
| P1 | Wysoki priorytet: niespójność architektury, testów lub dokumentacji, która utrudni fazę cycle-stepped |
| P2 | Średni priorytet: poprawa jakości, czytelności, kompletności testów lub ergonomii projektu |
| P3 | Niski priorytet: kosmetyka, porządkowanie, przyszłe usprawnienia |

---

## 2. P0 — elementy krytyczne

### 2.1. Sekret API w historii Git

**Problem**  
W historii repo pojawił się jawny klucz API NVIDIA w usuniętym pliku testowym. Usunięcie pliku w kolejnym commicie nie usuwa sekretu z historii.

**Ryzyko**

- przejęcie/zużycie klucza,
- billing abuse,
- problem bezpieczeństwa w publicznym repo,
- fałszywe poczucie bezpieczeństwa po samym usunięciu pliku.

**Do poprawy**

- [ ] Unieważnić/obrócić klucz NVIDIA.
- [ ] Usunąć sekret z historii repo, jeśli repo pozostaje publiczne.
- [ ] Dodać secret scanning.
- [ ] Dodać pre-commit hook blokujący sekrety.
- [ ] Dodać `.env.example` bez realnych wartości.
- [ ] Dodać do `.gitignore`: `.env`, `.env.*`, `secrets.*`, lokalne pliki testowe z kluczami.

**Sugerowane narzędzia**

- `gitleaks`
- GitHub secret scanning
- `git filter-repo` albo BFG Repo-Cleaner

---

### 2.2. Błędne flagi overflow w BCD `ADC/SBC`

**Problem**  
W implementacji BCD wynik jest przypisywany do `_a` przed obliczeniem flagi `V`. W efekcie wyrażenia typu `(_a ^ result)` mogą zawsze dawać `0`, bo `_a` już równa się `result`.

**Ryzyko**

- błędne flagi `V` w trybie decimal,
- błędne działanie programów używających BCD,
- fałszywie zielone testy, jeśli nie obejmują przypadków overflow.

**Do poprawy**

- [ ] Przed obliczeniami zapisać `byte oldA = _a`.
- [ ] Liczyć `V` na podstawie `oldA`, operandu i wyniku binarnego/końcowego zgodnie z wybraną specyfikacją NMOS 6502.
- [ ] Dodać testy tabelaryczne dla `ADC` decimal.
- [ ] Dodać testy tabelaryczne dla `SBC` decimal.
- [ ] Zweryfikować przypadki: `$50 + $50`, `$99 + $01`, `$00 - $01`, `$10 - $01`, `$00 - $00` z różnymi wartościami `C`.

**Minimalny kierunek poprawki**

```csharp
private void ExecuteAdcBcd(byte operand)
{
    byte oldA = _a;
    bool oldCarry = GetFlag(FlagC);

    // binary sum for N/V/Z reference where required
    int binarySum = oldA + operand + (oldCarry ? 1 : 0);
    byte binaryResult = (byte)binarySum;

    // BCD correction ...

    bool overflow = ((oldA ^ binaryResult) & (operand ^ binaryResult) & 0x80) != 0;
}
```

---

### 2.3. Status register: bit `U` i `B` po `PLP`, `BRK`, `RTI`, `PHP`, `Reset`

**Problem**  
Status register nie ma jeszcze spójnej polityki dla bitów `U` i `B`.

Obecne ryzyka:

- `Reset()` ustawia `P = FlagI`, bez `FlagU`.
- `PLP()` robi bezpośrednio `_p = Pop()`.
- `PHP()` ustawia `B=1` i `U=1`, ale trzeba zweryfikować analogiczne zachowanie `BRK`, `IRQ/NMI`, `RTI`.

**Do poprawy**

- [ ] Zdefiniować jedną regułę dla statusu wewnętrznego `_p`.
- [ ] Wymuszać `FlagU` przy zapisie do `_p`, jeśli projekt traktuje bit 5 jako zawsze ustawiony.
- [ ] Czyścić `FlagB` po `PLP`/`RTI`, jeśli `B` nie ma być przechowywany jako realny wewnętrzny bit CPU.
- [ ] Dodać testy `PHP`, `PLP`, `BRK`, `RTI`, później `IRQ/NMI`.

**Proponowany wariant**

```csharp
private byte NormalizeStatus(byte value)
{
    return (byte)((value | FlagU) & ~FlagB);
}

private byte StatusForPush(bool breakFlag)
{
    byte value = (byte)(_p | FlagU);
    return breakFlag ? (byte)(value | FlagB) : (byte)(value & ~FlagB);
}
```

---

## 3. P1 — architektura i poprawność emulatora

### 3.1. `Tick()` wykonuje instrukcję, nie cykl

**Problem**  
Metoda `Tick()` wykonuje pełną instrukcję przez delegate opcode. Nazwa sugeruje cykl zegara, ale obecnie jest to instruction-step.

**Ryzyko**

- nieporozumienia w dokumentacji,
- trudny refaktor w fazie 16,
- błędne założenia przy IRQ/NMI, branch timing i R-M-W.

**Do poprawy**

- [ ] W dokumentacji oznaczyć obecny model jako `instruction-stepped`.
- [ ] Rozważyć dodanie metody `StepInstruction()` jako jawnej nazwy dla obecnego zachowania.
- [ ] Zarezerwować `Tick()` dla modelu cycle-stepped albo jawnie zmienić semantykę w fazie 16.
- [ ] Dodać testy, które rozróżniają `StepInstruction()` od `TickCycle()`.

---

### 3.2. Domyślne mapowanie unknown opcode na `NOP`

**Problem**  
Tabela opcode jest inicjalizowana tak, że wszystkie niezaimplementowane opcode wykonują `NOP`.

**Ryzyko**

- brak implementacji jest ukrywany,
- testy zgodności mogą przechodzić fałszywie,
- illegal opcodes będą trudne do wdrożenia w fazach 18–19.

**Do poprawy**

- [ ] Dodać `IllegalOpcode()` rzucający wyjątek w trybie developerskim.
- [ ] Dodać opcję konfiguracyjną `TreatUnknownOpcodeAsNop` tylko do trybu eksperymentalnego.
- [ ] Dodać test: nieznany opcode powinien zgłaszać błąd.
- [ ] Dodać osobną mapę dla official/illegal/stable/unstable opcodes.

**Przykład**

```csharp
private void IllegalOpcode()
{
    throw new InvalidOperationException($"Illegal or unimplemented opcode: 0x{_ir:X2}");
}
```

---

### 3.3. Brak centralnych metadanych opcode

**Problem**  
Opcode są mapowane ręcznie do delegate w `InitOpcodeTable()`. Brakuje centralnego opisu: mnemonic, addressing mode, bytes, cycles, page-cross penalty, official/illegal.

**Ryzyko**

- duplikacja wiedzy w kodzie, dokumentacji i testach,
- trudna migracja do cycle-stepped,
- trudne generowanie trace logów.

**Do poprawy**

- [ ] Dodać `InstructionDescriptor`.
- [ ] Dodać enum `AddressingMode`.
- [ ] Dodać enum/flagę `OpcodeKind`: `Official`, `IllegalStable`, `IllegalUnstable`, `Kil`.
- [ ] Tabelę opcode budować z metadanych.
- [ ] Testować kompletność tabeli.

**Proponowany model**

```csharp
public sealed record InstructionDescriptor(
    byte Opcode,
    string Mnemonic,
    AddressingMode AddressingMode,
    int Bytes,
    int Cycles,
    bool AddsCycleOnPageCross,
    OpcodeKind Kind);
```

---

### 3.4. Branch page crossing jest wykrywany, ale nieużywany

**Problem**  
`ExecuteBranch()` wykrywa przekroczenie strony, ale nie wpływa to na cykle ani trace.

**Ryzyko**

- dokumentacja deklaruje cykle 2/3/4, ale emulator tego nie egzekwuje,
- IRQ timing w fazie 15/17 będzie niepoprawny bez modelu cykli.

**Do poprawy**

- [ ] W modelu instruction-stepped dodać licznik cykli instrukcji, nawet jeśli uproszczony.
- [ ] W modelu cycle-stepped przenieść branch do mikrocykli.
- [ ] Dodać testy branch: not taken, taken same page, taken page crossed.

---

### 3.5. R-M-W bez double write

**Problem**  
Instrukcje `ASL`, `LSR`, `ROL`, `ROR`, `INC`, `DEC` w modelu funkcjonalnym prawdopodobnie zapisują tylko wynik końcowy. NMOS 6502 wykonuje dummy write oryginalnej wartości przed zapisem zmodyfikowanej.

**Ryzyko**

- brak zgodności z hardware quirks,
- błędy w urządzeniach memory-mapped I/O,
- faza 17 będzie wymagała przebudowy.

**Do poprawy**

- [ ] Dodać `TracingMemoryBus`.
- [ ] Dodać testy kolejności read/write dla R-M-W.
- [ ] Wdrożyć double write dopiero w cycle-stepped.

---

## 4. P1 — dokumentacja

### 4.1. Brak `README.md`

**Do poprawy**

- [ ] Dodać opis projektu.
- [ ] Dodać status implementacji.
- [ ] Dodać komendy build/test.
- [ ] Dodać informację: obecnie functional/instruction-stepped, nie cycle-accurate.
- [ ] Dodać link do `docs/checklista.md`.
- [ ] Dodać informację o wymaganym SDK .NET.

**Minimalna struktura**

```md
# 6502 Emulator

## Status

## Build

## Test

## Documentation

## Compatibility Scope

## Roadmap
```

---

### 4.2. Niespójna checklista postępu

**Problem**  
Statusy faz i licznik postępu są niespójne.

**Do poprawy**

- [ ] Ustalić, czy zakończone są fazy 0–14 czy tylko 11 faz.
- [ ] Poprawić licznik.
- [ ] Oddzielić „liczbę faz” od „pokrycia CPU”.
- [ ] Dodać status pośredni: `[~] functional done, cycle accuracy deferred`.

---

### 4.3. Faza 14 opisuje reset cycle-accurate, ale kod jest uproszczony

**Problem**  
Dokument opisuje sekwencję resetu cykl po cyklu, a implementacja wykonuje reset atomowo.

**Do poprawy**

- [ ] Dopisać sekcję `Current implementation`.
- [ ] Dopisać sekcję `Deferred to phase 16`.
- [ ] Nie oznaczać pełnej sekwencji resetu cycle-accurate jako ukończonej.

---

### 4.4. Uszkodzona tabela Markdown w `faza-14-reset.md`

**Do poprawy**

- [ ] Usunąć pustą tabelę `| Plik | Akcja |` albo ją dokończyć.
- [ ] Oddzielić „Pliki planowane” od „Pliki zaimplementowane”.
- [ ] Sprawdzić renderowanie Markdown.

---

## 5. P1 — testy

### 5.1. Framework testowy

**Problem**  
Testy są w NUnit, a docelowy standard projektu powinien być spójny.

**Do poprawy**

- [ ] Podjąć decyzję: zostaje NUnit albo migracja do MSTest.
- [ ] Jeśli migracja: używać MSTest + FluentAssertions + Moq.
- [ ] Ujednolicić styl asercji.

---

### 5.2. Brak testów status register edge cases

**Do poprawy**

- [ ] `Reset` ustawia oczekiwane flagi.
- [ ] `PHP` pcha `B=1`, `U=1`.
- [ ] `PLP` przywraca status zgodnie z przyjętą normalizacją.
- [ ] `BRK` pcha `B=1`, `U=1`.
- [ ] `IRQ/NMI` pchają `B=0`, `U=1`.
- [ ] `RTI` przywraca `P` i `PC` zgodnie ze specyfikacją.

---

### 5.3. Brak testów trace pamięci

**Do poprawy**

- [ ] Dodać `TracingMemoryBus`.
- [ ] Rejestrować `Read/Write`, adres, wartość, numer cyklu.
- [ ] Używać do testów R-M-W, reset, interrupt, branch, page crossing.

**Proponowany model**

```csharp
public sealed record MemoryAccess(
    MemoryAccessKind Kind,
    ushort Address,
    byte Value,
    long Cycle);

public enum MemoryAccessKind
{
    Read,
    Write
}
```

---

### 5.4. Brak testów zgodności referencyjnej

**Do poprawy**

- [ ] Najpierw dodać Klaus Dormann functional test.
- [ ] Potem nestest.
- [ ] Potem Wolfgang Lorenz.
- [ ] Perfect6502 zostawić jako opcjonalny benchmark zgodności.

---

## 6. P2 — jakość kodu

### 6.1. Nadmiar `partial class`

**Problem**  
Podział na partiale jest czytelny teraz, ale może stać się trudny do utrzymania przy cycle-stepped.

**Do poprawy**

- [ ] Utrzymać partiale instrukcyjne na czas faz funkcjonalnych.
- [ ] Przed fazą 16 rozważyć foldery: `Execution`, `Addressing`, `Interrupts`, `OpcodeTable`, `Tracing`.
- [ ] Nie dodawać kolejnych dużych partiali bez metadanych opcode.

---

### 6.2. Komentarze XML opisują intencję, ale nie zawsze stan faktyczny

**Problem**  
Komentarze podają cykle, ale kod ich realnie nie modeluje.

**Do poprawy**

- [ ] W komentarzach rozróżnić `Nominal cycles` od `Implemented timing`.
- [ ] Nie sugerować cycle accuracy tam, gdzie jej nie ma.

---

### 6.3. Brak jawnego trybu zgodności

**Do poprawy**

- [ ] Dodać `CpuCompatibilityMode`.
- [ ] Rozważyć tryby: `Functional`, `NmOS6502`, `Cmos65C02` w przyszłości.
- [ ] W obecnym projekcie jawnie wspierać tylko `NMOS 6502` albo tylko `functional 6502 subset`.

---

## 7. Ocena pozostałych planów

### 7.1. Faza 15 — IRQ/NMI

**Ocena**  
Plan jest dobry merytorycznie, ale nie powinien być wdrażany w pełni przed fazą 16.

**Zalecenie**

- [ ] Można dodać tylko API `SetIRQ()` / `SetNMI()`.
- [ ] Pełne wykonanie IRQ/NMI przenieść po cycle-stepped MVP.
- [ ] Nie implementować CLI latency bez modelu cykli.

---

### 7.2. Faza 16 — cycle-stepped

**Ocena**  
To jest największy refaktor projektu. Obecny plan jest za duży jako jedna faza.

**Rozbić na podfazy**

- [ ] 16.1 — opcode metadata.
- [ ] 16.2 — cycle counter i trace bez zmiany publicznego zachowania.
- [ ] 16.3 — cycle-stepped `NOP`.
- [ ] 16.4 — cycle-stepped `LDA #`, `LDA abs`.
- [ ] 16.5 — cycle-stepped `STA abs`.
- [ ] 16.6 — migracja load/store.
- [ ] 16.7 — branch/jump/stack.
- [ ] 16.8 — R-M-W.

---

### 7.3. Faza 17 — quirks

**Ocena**  
Plan jest poprawny, ale zależy od fazy 16.

**Do poprawy w planie**

- [ ] Dodać wymaganie `TracingMemoryBus`.
- [ ] Dodać testy kolejności dostępu do pamięci.
- [ ] Oddzielić `JMP indirect bug`, który już częściowo jest zaimplementowany, od R-M-W double write.

---

### 7.4. Fazy 18–19 — illegal opcodes

**Ocena**  
Nie zaczynać przed cycle-stepped i testami zgodności oficjalnych opcode.

**Do poprawy**

- [ ] Najpierw official opcode completeness.
- [ ] Potem stable illegal opcodes.
- [ ] Na końcu unstable/KIL.

---

### 7.5. Fazy 20–23 — testy zgodności

**Zalecana kolejność**

1. Klaus Dormann functional test.
2. nestest.
3. Wolfgang Lorenz.
4. perfect6502 opcjonalnie.

---

## 8. Proponowana kolejność realizacji

### Etap A — bezpieczeństwo i porządek

- [ ] Obrócić sekret NVIDIA.
- [ ] Oczyścić historię repo.
- [ ] Dodać `.env.example` i `.gitignore` dla sekretów.
- [ ] Dodać README.
- [ ] Naprawić checklistę.
- [ ] Naprawić `faza-14-reset.md`.

### Etap B — poprawki CPU bez dużego refaktoru

- [ ] Naprawić BCD `ADC/SBC`.
- [ ] Ustalić i wdrożyć normalizację `P`, `U`, `B`.
- [ ] Zmienić unknown opcode z `NOP` na `IllegalOpcode` w trybie developerskim.
- [ ] Dodać testy status register.
- [ ] Dodać testy BCD.

### Etap C — przygotowanie do cycle-stepped

- [ ] Dodać `InstructionDescriptor`.
- [ ] Dodać `AddressingMode` enum.
- [ ] Dodać `TracingMemoryBus`.
- [ ] Dodać testy trace dla prostych instrukcji.
- [ ] Dodać uproszczony licznik cykli instrukcji.

### Etap D — faza 16 jako seria małych zmian

- [ ] Cycle-stepped `NOP`.
- [ ] Cycle-stepped `LDA #`.
- [ ] Cycle-stepped `LDA abs`.
- [ ] Cycle-stepped `STA abs`.
- [ ] Migracja load/store.
- [ ] Migracja branch/jump/stack.
- [ ] Migracja R-M-W.

### Etap E — zgodność

- [ ] IRQ/NMI.
- [ ] R-M-W double write.
- [ ] Branch interrupt timing.
- [ ] CLI latency.
- [ ] Klaus Dormann.
- [ ] nestest.

---

## 9. Definition of Done dla tej listy

Lista może zostać uznana za zrealizowaną, gdy:

- [ ] repo nie zawiera aktywnych sekretów ani sekretów w historii publicznej,
- [ ] README opisuje aktualny status i komendy,
- [ ] checklista ma spójny licznik,
- [ ] BCD ma testy tabelaryczne i poprawione flagi,
- [ ] status register ma spójną normalizację,
- [ ] unknown opcode nie jest cicho traktowany jako NOP w trybie developerskim,
- [ ] istnieje `TracingMemoryBus`,
- [ ] faza 16 jest rozbita na mniejsze dokumenty/plany,
- [ ] testy rozróżniają functional correctness od cycle accuracy.
