# Review 2026-05-18 — Klaus Dormann i jakość kodu emulatora 6502

## 001. Cel dokumentu

Ten dokument zbiera dwie analizy wykonane dla repozytorium `prachwal/6502`:

1. ocenę ostatniego commita dotyczącego Fazy 21 — Klaus Dormann Functional Test,
2. ocenę ogólnej jakości kodu emulatora CPU 6502.

Celem dokumentu jest wskazanie realnego stanu projektu, niespójności w dokumentacji, głównych przyczyn problemu z testami oraz kolejności napraw.

---

## 002. Analiza ostatniego commita

### 002.001. Commit

```txt
d83d09b1b2117054b19575ccdc19c3011ddd7b03
```

Commit:

```txt
feat: implementacja Fazy 21 - Klaus Dormann Functional Test
```

Zakres commita:

1. dodanie ROM-u testowego `6502_functional_test.bin`,
2. dodanie `KlausTestRunner`,
3. dodanie testów `KlausTests`,
4. aktualizacja `Cpu6502.Tests.csproj`,
5. aktualizacja dokumentacji fazy oraz checklisty,
6. rozszerzenie konfiguracji `.vibe/config.toml`.

### 002.002. Ocena commita

Ocena: **6/10**.

Commit jest wartościowy, ponieważ dodaje realną infrastrukturę pod test zgodności Klaus Dormann. Nie powinien jednak zamykać Fazy 21 jako zakończonej, ponieważ testy Klaus nie przechodzą.

### 002.003. Co jest dobre

1. Dodano osobną klasę runnera dla Klaus Dormann.
2. Dodano binarny ROM testowy jako dane testowe.
3. Dodano trzy scenariusze testowe: non-BCD, BCD oraz wariant NES.
4. Projekt testowy kopiuje ROM do katalogu wyjściowego.
5. Dokumentacja jawnie informuje, że testy kończą się timeoutem i wymagają poprawek CPU.

### 002.004. Główne problemy commita

#### Status fazy jest błędny

Dokumentacja oznacza Fazę 21 jako zakończoną `[x]`, mimo że testy nadal nie przechodzą.

Obecny stan logiczny:

```txt
Build: OK
Testy: 259 passed, 3 failed
KlausTests: timeout / fail
```

Dlatego Faza 21 powinna mieć status `[~]`, nie `[x]`.

Poprawny wpis w `docs/checklista.md`:

```md
| 21 | Test zgodności — Klaus Dormann Functional Test | [faza-21-klaus.md](faza-21-klaus.md) | [~] | 4% | 93% |
```

Poprawny status w `docs/faza-21-klaus.md`:

```md
| **Status** | [~] Infrastruktura gotowa, testy nie przechodzą |
```

#### Commit miesza infrastrukturę testową z deklaracją zakończenia fazy

Lepsza interpretacja commita:

```txt
test: add Klaus Dormann functional test infrastructure
```

Nie jest to jeszcze pełna implementacja zakończonej Fazy 21.

#### Runner nie daje diagnostyki

`KlausTestRunner` zwraca tylko `bool`. Przy timeoutach to za mało, ponieważ nie wiadomo:

1. na jakim `PC` zatrzymał się test,
2. jaki był ostatni opcode,
3. ile cykli wykonano,
4. czy test wszedł w pętlę błędu,
5. czy test rzeczywiście doszedł do pętli sukcesu,
6. jakie były ostatnie instrukcje przed błędem.

Docelowo runner powinien zwracać obiekt diagnostyczny.

Proponowany model:

```csharp
public sealed record KlausTestResult(
    bool Success,
    ushort FinalPc,
    byte Opcode,
    ulong Cycles,
    string FailureReason
);
```

#### Rozszerzenie uprawnień `.vibe/config.toml` wymaga uzasadnienia

Commit dodaje m.in.:

```txt
/tmp/*
mv
timeout
wget
xxd
web_search = always
```

To może być przydatne dla agenta, ale powinno być uzasadnione osobnym wpisem w dokumentacji albo osobnym commitem. W obecnym commicie miesza się z testami CPU.

---

## 003. Przyczyny problemu z testami Klaus

### 003.001. Najważniejszy problem: semantyka `Tick()`

Kod i komentarz sugerują, że `Tick()` wykonuje jeden cykl zegara. W praktyce metoda przechodzi pętlą do końca instrukcji.

Obserwowana semantyka:

```txt
Tick() == wykonaj całą instrukcję do sync
```

Deklarowana semantyka:

```txt
Tick() == wykonaj jeden cykl CPU
```

To jest krytyczne, ponieważ projekt rozwija model `cycle-stepped`. Jeżeli publiczne API nazywa się `Tick`, ale wykonuje całą instrukcję, to runner testowy, peryferia, magistrala, debugger i przyszłe komponenty Apple-1/KIM-1/C64 mogą opierać się na błędnym założeniu.

Rekomendacja:

1. `Tick()` powinien wykonywać dokładnie jeden cykl.
2. Dodać osobną metodę `StepInstruction()` do wykonywania pełnej instrukcji.
3. Testy zgodności, takie jak `nestest` i Klaus, powinny świadomie używać jednej z tych metod.

Przykład:

```csharp
public void StepInstruction()
{
    if (GetState().Sync)
    {
        Tick();
    }

    while (!GetState().Sync)
    {
        Tick();
    }
}
```

### 003.002. Detekcja pętli sukcesu jest zbyt słaba

Runner zakłada stały adres sukcesu:

```csharp
private const ushort SuccessLoopAddress = 0x3469;
```

Następnie uznaje sukces po powtarzającym się `PC` równym temu adresowi. Lepszym podejściem jest wykrywanie faktycznej instrukcji `JMP *`:

```txt
memory[pc]     == 0x4C
memory[pc + 1] == low(pc)
memory[pc + 2] == high(pc)
```

To pozwala odróżnić:

1. pętlę sukcesu,
2. pętlę błędu,
3. fałszywy brak zmiany `PC`,
4. zatrzymanie CPU przez nieobsłużony opcode.

### 003.003. Tryb BCD jest mieszany z flagą D

W testach ustawiane jest:

```csharp
_cpu.DecimalModeEnabled = false;
```

oraz w runnerze:

```csharp
_cpu.SetFlag(Cpu6502.FlagD, false);
```

To są dwie różne rzeczy:

1. `DecimalModeEnabled` oznacza, czy wariant CPU fizycznie obsługuje arytmetykę BCD.
2. Flaga `D` oznacza stan programu emulowanego procesora.

Dla klasycznego MOS 6502 `DecimalModeEnabled` powinno pozostać `true`. Test non-BCD powinien być kontrolowany konfiguracją samego programu testowego albo początkowym stanem flagi D, a nie wyłączaniem cechy CPU.

Dla NES/Ricoh 2A03 `DecimalModeEnabled = false` ma sens, ponieważ ten wariant nie wykonuje arytmetyki BCD mimo widocznej flagi D.

### 003.004. Timeout nie mówi, gdzie leży błąd

Aktualny wynik `false` nie daje informacji diagnostycznej. Przy testach zgodności potrzebny jest raport:

```txt
Success: false
Reason: Timeout
Final PC: $....
Opcode: $..
Cycles: ....
Last instructions:
  ....
```

Bez tego poprawianie CPU będzie zgadywaniem.

---

## 004. Ocena ogólna kodu emulatora

### 004.001. Ocena ogólna

Ocena: **6.5/10**.

Projekt jest już dobry jako funkcjonalny emulator CPU 6502 w fazie rozwoju, ale nie jest jeszcze stabilnym, w pełni zweryfikowanym, cycle-accurate core pod modularny komputer retro.

### 004.002. Mocne strony

1. Projekt ma jasny kierunek: CPU 6502, warianty CPU, testy zgodności, dokumentacja faz.
2. CPU jest rozdzielony na pliki partial według obszarów funkcjonalnych.
3. Jest interfejs magistrali pamięci `IMemoryBus`, co jest dobrym fundamentem pod emulację komputerów, a nie tylko samego CPU.
4. Dodano warianty CPU: klasyczny MOS 6502 oraz NES/Ricoh 2A03.
5. Są testy jednostkowe i testy zgodności: `nestest`, Klaus, illegal opcodes.
6. `NestestRunner` ma sensowną diagnostykę: porównuje `PC`, `A`, `X`, `Y`, `P`, `SP` i cykle.
7. Dokumentacja faz jest rozbudowana i dobrze pokazuje roadmapę.

### 004.003. Słabe strony

#### Publiczne API nie jest semantycznie spójne

Największy problem to nazwa i działanie `Tick()`. Przy emulatorze retro różnica między cyklem a instrukcją jest fundamentalna.

Docelowo API powinno mieć co najmniej:

```csharp
void Tick();              // jeden cykl CPU
void StepInstruction();   // jedna pełna instrukcja
CpuState GetState();
```

#### Model cycle-stepped jest częściowo funkcjonalny

W wielu miejscach efekt instrukcji jest wykonywany od razu, a cykle są tylko symulowane przez licznik. To może wystarczać dla testów funkcjonalnych, ale nie dla wiernej emulacji magistrali i urządzeń.

Ryzyka:

1. błędne momenty odczytu/zapisu pamięci,
2. nierealistyczne zachowanie przerwań,
3. błędne interakcje z PIA/VIA/UART,
4. problemy z DMA i synchronizacją grafiki/dźwięku w przyszłości.

#### Duża partial class utrudnia utrzymanie

`Cpu6502` jest podzielony na wiele plików, ale nadal jest jedną dużą klasą z bardzo dużą ilością stanu prywatnego.

Przy dalszym rozwoju warto wydzielić przynajmniej:

1. trace/debugger,
2. dekoder opcode,
3. warianty zachowań CPU,
4. moduł arytmetyki BCD,
5. model cyklu/mikrooperacji,
6. snapshot/state serialization.

#### TestMemoryBus ma ryzyko overflow

Aktualny wzorzec iterowania po `ushort` może powodować błędne zachowanie przy ładowaniu danych blisko końca przestrzeni adresowej.

Bezpieczniejsza wersja:

```csharp
public void LoadData(ushort address, byte[] data)
{
    for (int i = 0; i < data.Length && address + i <= 0xFFFF; i++)
    {
        _memory[address + i] = data[i];
    }
}
```

#### Dokumentacja bywa zbyt optymistyczna

Faza 21 została oznaczona jako zakończona mimo nieprzechodzących testów. Dokumentacja powinna rozróżniać:

```txt
[x] infrastruktura gotowa
[ ] test przechodzi
[ ] CPU zgodny z testem
```

---

## 005. Rekomendowana kolejność napraw

### 005.001. Krok 1 — poprawić status dokumentacji

Zmienić Fazę 21 z `[x]` na `[~]`.

Nie oznaczać fazy jako zakończonej, dopóki testy Klaus nie przechodzą.

### 005.002. Krok 2 — wprowadzić diagnostyczny wynik Klaus runnera

Zastąpić `bool` przez `KlausTestResult`.

Minimalny model:

```csharp
public enum KlausFailureReason
{
    None,
    Timeout,
    ErrorLoop,
    Halted,
    UnexpectedPc,
    Unknown
}

public sealed record KlausTestResult(
    bool Success,
    KlausFailureReason FailureReason,
    ushort FinalPc,
    byte FinalOpcode,
    ulong Cycles,
    IReadOnlyList<CpuTraceEntry> Trace
);
```

### 005.003. Krok 3 — dodać trace ostatnich instrukcji

Przykład wpisu:

```csharp
public sealed record CpuTraceEntry(
    ushort Pc,
    byte Opcode,
    byte A,
    byte X,
    byte Y,
    byte P,
    byte Sp,
    ulong Cycle
);
```

Runner powinien trzymać ring buffer, np. ostatnie 32 lub 64 instrukcje.

### 005.004. Krok 4 — rozdzielić `Tick()` i `StepInstruction()`

To jest kluczowe dla dalszego rozwoju projektu.

Wariant bezpieczny migracyjnie:

1. zostawić obecne zachowanie jako `StepInstruction()`,
2. tymczasowo oznaczyć obecne `Tick()` jako problematyczne,
3. dopiero potem przepisać `Tick()` na prawdziwy pojedynczy cykl,
4. zaktualizować testy.

### 005.005. Krok 5 — debugować CPU na podstawie konkretnego PC

Dopiero po poprawie runnera będzie wiadomo, która instrukcja powoduje pierwszą rozbieżność.

Bez tego naprawianie CPU będzie losowe.

---

## 006. Proponowane commity naprawcze

### Commit 1

```txt
docs: mark Klaus phase as in progress
```

Zakres:

1. `docs/checklista.md`,
2. `docs/faza-21-klaus.md`,
3. doprecyzowanie Definition of Done.

### Commit 2

```txt
test: return diagnostic result from Klaus runner
```

Zakres:

1. `KlausTestResult`,
2. `KlausFailureReason`,
3. `FinalPc`, `FinalOpcode`, `Cycles`,
4. czytelne komunikaty asercji.

### Commit 3

```txt
test: add CPU trace buffer for functional test runners
```

Zakres:

1. `CpuTraceEntry`,
2. ring buffer ostatnich instrukcji,
3. dump trace przy błędzie.

### Commit 4

```txt
refactor(cpu): split clock tick from instruction step
```

Zakres:

1. `Tick()` jako jeden cykl,
2. `StepInstruction()` jako pełna instrukcja,
3. aktualizacja `NestestRunner`,
4. aktualizacja `KlausTestRunner`.

### Commit 5

```txt
fix(test): harden memory bus loading near address space end
```

Zakres:

1. poprawa `TestMemoryBus.LoadData`,
2. test ładowania danych przy końcu pamięci.

---

## 007. Podsumowanie

Repozytorium jest w dobrym kierunku, ale obecnie ma trzy krytyczne niespójności:

1. `Tick()` nie oznacza jednoznacznie cyklu CPU,
2. test Klaus nie daje wystarczającej diagnostyki,
3. dokumentacja oznacza fazę jako zakończoną mimo nieprzechodzących testów.

Największa wartość najbliższego etapu to nie natychmiastowe poprawianie instrukcji CPU, ale poprawa narzędzi diagnostycznych. Po dodaniu `KlausTestResult` i trace będzie można wskazać pierwszy konkretny adres/opcode, na którym CPU rozjeżdża się z oczekiwanym zachowaniem.
