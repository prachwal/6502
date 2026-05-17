# Feedback — przegląd dokumentacji, implementacji i planów

Data przeglądu: 2026-05-17  
Repozytorium: `prachwal/6502`  
Zakres: stan gałęzi `main` po commicie `b6c23dd01fe65d98798efdb4c6be5a9b702ad72c`

---

## 1. Podsumowanie wykonawcze

Projekt ma sensowny kierunek: implementacja CPU MOS 6502 jest prowadzona fazami, dokumentacja zawiera checklistę, a kod został rozbity na pliki `partial class`, co poprawia czytelność względem jednego dużego pliku `Cpu6502.cs`.

Stan deklarowany w dokumentacji to 11 / 24 faz, czyli 46% faz zakończonych. Jednocześnie checklista oznacza fazy 0–14 jako zakończone, czyli 15 faz, nie 11. To jest najważniejsza niespójność dokumentacyjna, bo utrudnia ocenę realnego postępu.

Implementacja fazy 14 — `Reset()` — jest wystarczająca dla obecnego modelu instruction-stepped, ale nie jest pełną sekwencją resetu cycle-accurate. Dokument fazy 14 opisuje cykle resetu, lecz kod wykonuje uproszczony reset atomowo. To jest akceptowalne tylko jako etap przejściowy przed fazą 16.

Najpoważniejszy problem techniczno-procesowy: w ostatnim commicie usunięto plik `tools/test.py`, który zawierał jawny klucz API NVIDIA. Samo usunięcie pliku nie usuwa sekretu z historii Git. Klucz trzeba natychmiast unieważnić/obrócić, a historię repo oczyścić, jeśli repo pozostaje publiczne.

---

## 2. Ocena dokumentacji

### Mocne strony

- Dokumentacja fazowa jest użyteczna: każda faza ma cel, zakres, testy i Definition of Done.
- Checklista daje szybki obraz zakresu projektu: instrukcje, tryby adresowania, reset, przerwania, cycle stepping, quirki i testy zgodności.
- Dokumenty faz 15–17 dobrze identyfikują krytyczne obszary zgodności 6502: IRQ/NMI, cycle-stepped execution, R-M-W double write, JMP indirect bug, CLI latency i timing branchy.

### Problemy

#### 2.1. Brak `README.md`

Repo nie ma widocznego `README.md`. Dla projektu emulatora powinien istnieć minimalny punkt wejścia:

- cel projektu,
- aktualny status,
- jak zbudować,
- jak uruchomić testy,
- zakres zgodności,
- link do `docs/checklista.md`,
- ostrzeżenie, że projekt nie jest jeszcze cycle-accurate.

#### 2.2. Niespójny postęp w `docs/checklista.md`

Tabela oznacza fazy 0–14 jako `[x]`, czyli 15 faz zakończonych. Sekcja postępu mówi jednak `11 / 24 faz (46%)`. To jest sprzeczne.

Rekomendacja:

- albo zmienić licznik na `15 / 24 faz (62.5%)`,
- albo zmienić statusy faz 11–14, jeśli nie są faktycznie ukończone,
- osobno trzymać „pokrycie funkcjonalne CPU” i „liczbę faz”, bo obecne procenty mieszają dwa różne znaczenia.

#### 2.3. Błąd tabeli w `docs/faza-14-reset.md`

Sekcja `## Pliki` zawiera niedokończoną tabelę:

```md
| Plik | Akcja |

## Pliki implementacyjne
...
|------|-------|
```

To psuje strukturę Markdown. Trzeba rozdzielić tabelę planowaną i listę plików wykonanych albo usunąć pustą tabelę.

#### 2.4. Dokumentacja faz nie odróżnia modelu instruction-stepped od cycle-stepped

Faza 14 opisuje pełną sekwencję resetu cykl po cyklu, ale implementacja jest atomowa. Faza 16 dopiero planuje cycle stepping. W dokumentach trzeba jawnie oznaczyć:

- `Implemented now: instruction-stepped approximation`,
- `Deferred to phase 16: cycle-accurate reset sequence`.

---

## 3. Ocena implementacji

### 3.1. Architektura kodu

Podział `Cpu6502` na pliki partial jest praktyczny na tym etapie. Pliki typu `Cpu6502.LoadStore.cs`, `Cpu6502.BranchJump.cs`, `Cpu6502.PublicMethods.cs` i `Cpu6502.Constructor.cs` są czytelniejsze niż jeden monolit.

Ryzyko: wraz z fazą 16 obecna architektura `Action[256]` wykonująca całą instrukcję w jednym wywołaniu będzie przeszkodą. Cycle-stepped execution wymaga albo mikrooperacji, albo tabeli cykli, albo generatora case'ów per opcode/cycle.

Rekomendacja przed fazą 16:

- wydzielić warstwę dekodowania opcode od wykonania,
- dodać strukturę `InstructionDescriptor` z opcode, mnemonic, addressing mode, cycles, pageCrossPenalty,
- utrzymać obecny instruction-stepped backend jako tryb testowy/regresyjny,
- dopiero potem dodać cycle-stepped backend.

### 3.2. `Reset()`

Aktualny `Reset()`:

- zeruje A/X/Y,
- ustawia SP na `$FD`,
- ustawia P na `FlagI`,
- ładuje PC z `$FFFC/$FFFD`,
- ustawia `Sync = true`, `Cycle = 0`.

To jest dobra implementacja uproszczona dla obecnego modelu. Brakuje jednak bitu U (`FlagU = 0x20`) w rejestrze P. W wielu emulatorach bit 5 statusu jest traktowany jako zawsze ustawiony przy odczycie/push statusu. Jeśli projekt chce zgodności z typowym zachowaniem 6502, warto rozważyć `P = FlagI | FlagU` albo konsekwentnie maskować bit U przy `GetState()`/push statusu.

Ryzyko: testy aktualnie oczekują tylko I=1 i D=0, więc nie wykrywają potencjalnej niespójności bitu U.

### 3.3. `Tick()`

Obecny `Tick()` nadal wykonuje pełną instrukcję przez delegate z `_opcodeTable[opcode]()`. To nie jest model „jeden Tick = jeden cykl”. Nazwa `Tick()` jest więc myląca po fazach 0–15.

Rekomendacja:

- do czasu fazy 16 nazwać to w dokumentacji `InstructionStep()` lub opisać `Tick()` jako „instruction-step tick”,
- w fazie 16 zmienić semantykę `Tick()` dopiero po przygotowaniu testów regresyjnych.

### 3.4. Domyślne mapowanie wszystkich niezaimplementowanych opcode na `Nop`

W `InitOpcodeTable()` wszystkie 256 opcode'ów są domyślnie ustawiane na `Nop`. To jest wygodne podczas rozwoju, ale ryzykowne:

- ukrywa brak implementacji,
- może fałszować testy zgodności,
- utrudnia wykrywanie nieudokumentowanych opcode'ów.

Rekomendacja:

- dla trybu developerskiego: domyślnie `IllegalOpcode()` rzucający wyjątek,
- dla trybu zgodności: osobna tabela illegal/stable/unstable,
- dla testów: jawnie włączać `TreatUnknownAsNop`, jeśli jest potrzebne.

### 3.5. Testy

Testy są w NUnit (`using NUnit.Framework`). Preferowany standard projektu powinien być spójny z docelowym stackiem testowym: MSTest + Moq + FluentAssertions.

Rekomendacja:

- albo świadomie zostawić NUnit i wpisać to do README,
- albo zaplanować migrację testów do MSTest + FluentAssertions,
- dodać testy właściwościowe dla flag i page crossing,
- dodać testy z logiem śladu CPU: PC, opcode, A/X/Y/P/SP/cycles.

---

## 4. Ocena faz zakończonych

### Fazy 0–10

Zakres wygląda logicznie: szkielet, load/store, transfery, flagi, arytmetyka, INC/DEC, compare/BIT, logiczne, shift/rotate, branch/jump, stack/NOP. Dokumentacja podaje wyniki testów i pliki implementacyjne.

Największe ryzyka:

- poprawność cykli jest obecnie deklaratywna, nie cycle-accurate,
- page crossing i dummy reads/writes nie są pełne bez fazy 16,
- domyślne NOP dla nieznanych opcode może maskować braki.

### Faza 11 — BRK/RTI

Faza jest oznaczona jako zakończona, ale bez przeglądu pliku implementacyjnego nie można potwierdzić detali. W fazie 15 IRQ/NMI mają używać sekwencji podobnej do BRK, więc przed rozpoczęciem fazy 15 trzeba zweryfikować:

- PC pushowany przez BRK ma właściwą wartość,
- B=1 tylko dla BRK,
- bit U jest ustawiany na stosie,
- RTI przywraca P i PC poprawnie,
- zachowanie I flag jest zgodne z 6502.

### Faza 12 — addressing/page crossing

Oznaczona jako zakończona, ale realne page crossing w cycle-accurate sensie będzie pełne dopiero po fazie 16. Obecnie prawdopodobnie chodzi o korektę liczby cykli i adresowania w modelu instruction-stepped.

Rekomendacja: zmienić opis statusu na „functional addressing complete, cycle behavior deferred”.

### Faza 13 — BCD

BCD ADC/SBC jest krytyczne i łatwo o błędy flag C/Z/N/V. Warto dodać testy referencyjne z tabelami przypadków, nie tylko pojedyncze przykłady.

### Faza 14 — RESET

Status: warunkowo zaakceptowane jako etap instruction-stepped. Nie powinno być oznaczane jako pełna sekwencja resetu cycle-accurate.

---

## 5. Ocena pozostałych planów

### Faza 15 — IRQ/NMI

Plan jest merytorycznie dobry, ale zależy od decyzji architektonicznej: implementować IRQ/NMI teraz w modelu instruction-stepped czy poczekać na fazę 16.

Rekomendacja: nie implementować pełnej fazy 15 przed fazą 16. Można dodać tylko API `SetIRQ`, `SetNMI` i testy latchowania NMI, ale faktyczny timing przerwań powinien być robiony po cycle-stepped.

Ryzyka:

- CLI latency jest trudna bez cycle modelu,
- sprawdzanie IRQ na „przedostatnim cyklu” jest niemożliwe w modelu jednowywołaniowym,
- interrupt hijacking i priorytety będą wymagały refaktoryzacji.

### Faza 16 — cycle-stepped

To jest najważniejsza faza projektu. Powinna być rozbita na mniejsze podfazy:

1. Wprowadzenie `InstructionDescriptor` i metadanych opcode.
2. Dodanie licznika cyklu instrukcji bez zmiany zachowania publicznego.
3. Cycle-stepped tylko dla NOP, LDA immediate, LDA absolute.
4. Migracja load/store.
5. Migracja branch/jump/stack.
6. Migracja R-M-W.
7. Dopiero potem IRQ/NMI i reset cycle sequence.

Obecny plan jest za duży jak na jedną fazę 8–12h. To powinien być epic.

### Faza 17 — quirki

Plan jest poprawny, ale powinien być po fazie 16. R-M-W double write nie ma sensu bez rejestrowania cykli i operacji pamięci per cykl. Warto dodać testowy `TracingMemoryBus`, który zapisuje kolejność `Read/Write` z adresem i wartością.

### Fazy 18–19 — nieudokumentowane opcode

Nie implementować przed testami zgodności i cycle-stepped. Najpierw trzeba zamknąć oficjalne opcode'y i timing.

### Fazy 20–23 — testy zgodności

Kolejność powinna być:

1. Klaus Dormann functional test,
2. nestest,
3. Wolfgang Lorenz,
4. perfect6502 opcjonalnie.

`nestest` jest silnie NES-owy i wymaga odpowiedniego środowiska pamięci/PPU-stubów albo kontrolowanego harnessu. Klaus jest lepszy jako pierwszy test CPU functional.

---

## 6. Krytyczne działania naprawcze

### P0 — bezpieczeństwo

- Natychmiast unieważnić klucz NVIDIA, który był w historii commita.
- Usunąć sekret z historii repo, jeśli repo ma pozostać publiczne.
- Dodać secret scanning / pre-commit hook.
- Nigdy nie commitować plików testowych z realnymi kluczami API.

### P1 — dokumentacja

- Dodać `README.md`.
- Naprawić licznik postępu w `docs/checklista.md`.
- Naprawić uszkodzoną tabelę w `docs/faza-14-reset.md`.
- Oznaczyć wyraźnie, które fazy są functional/instruction-stepped, a które cycle-accurate.

### P1 — architektura

- Przed fazą 16 przygotować model metadanych opcode.
- Zmienić domyślne zachowanie unknown opcode z NOP na błąd w trybie developerskim.
- Dodać `TracingMemoryBus` do testowania sekwencji odczytów/zapisów.

### P2 — testy

- Ujednolicić framework testowy.
- Dodać testy flagi U/B przy PHP/BRK/IRQ/NMI/RTI.
- Dodać testy BCD tabelaryczne.
- Dodać testy page crossing z oczekiwaną liczbą cykli.

---

## 7. Rekomendowana kolejność dalszych prac

1. P0: rotacja sekretu NVIDIA i oczyszczenie historii.
2. Naprawa dokumentacji: README, checklista, faza 14.
3. Dodanie `TracingMemoryBus`.
4. Refaktoryzacja opcode metadata.
5. Rozbicie fazy 16 na mniejsze podfazy.
6. Cycle-stepped MVP: NOP, LDA immediate, LDA absolute, STA absolute.
7. Dopiero potem IRQ/NMI.
8. Następnie R-M-W quirki i JMP indirect bug.
9. Po tym Klaus Dormann functional test.

---

## 8. Ocena końcowa

| Obszar | Ocena | Komentarz |
|--------|------:|-----------|
| Kierunek projektu | 8/10 | Dobra fazowość i sensowny zakres emulatora. |
| Dokumentacja planów | 7/10 | Dużo treści, ale są niespójności statusów i postępu. |
| Implementacja obecna | 6/10 | Dobra jako functional emulator, jeszcze nie cycle-accurate. |
| Testy | 6/10 | Są testy regresyjne, ale framework niespójny z docelowym standardem i brakuje testów trace/cycle. |
| Bezpieczeństwo repo | 2/10 | Jawny sekret w historii wymaga natychmiastowej reakcji. |
| Gotowość do fazy 16 | 4/10 | Potrzebna refaktoryzacja przed zmianą semantyki `Tick()`. |

Wniosek: projekt jest w dobrym miejscu jako emulator funkcjonalny 6502, ale przed przejściem do cycle-accurate trzeba zatrzymać dokładanie kolejnych funkcji i uporządkować dokumentację, sekrety, model opcode oraz testy pamięci/cykli.
