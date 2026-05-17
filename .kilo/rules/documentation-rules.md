# Documentation Rules - Symulator 6502

## 📌 Ogólne zasady

1. **Język**: Polski (dla dokumentacji projektu)
2. **Format**: Markdown
3. **Struktura**: Konsistentna z istniejącą dokumentacją
4. **Aktualność**: Dokumentacja **MUSI** być zawsze aktualna

---

## 🗂️ Struktura dokumentacji

### 1. Główne katalogi
```
6502Emulator/
├── docs/
│   ├── DOKUMENTACJA_SYMULATORA_6502.md  # Główna dokumentacja
│   ├── checklista.md                     # Lista faz i ich status
│   ├── faza-00-szkielet.md              # Faza 0
│   ├── faza-01-load-store.md            # Faza 1
│   ├── faza-02-transfer.md              # Faza 2
│   ├── ...
│   └── faza-23-perfect6502.md            # Faza 23
└── ...
```

### 2. Pliki dokumentacji faz
Każda faza **MUSI** mieć swój plik `faza-XX-*.md` z następującą strukturą:

```markdown
# Faza XX — [Nazwa fazy]

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone / [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | X% |
| **Pokrycie całości** | X% |
| **Zależności** | Fazy: X, Y, Z |
| **Szacowany czas** | Xh |
| **Data rozpoczęcia** | YYYY-MM-DD |
| **Data zakończenia** | YYYY-MM-DD |
| **Liczba testów** | X |

---

## Cel fazy

[Opis celu fazy]

---

## Co implementujemy

### Lista instrukcji

| Instrukcja | Opcode | Opis | Flagi |
|------------|--------|------|-------|
| **XXX** | $XX | [Opis] | [Flagi] |

### Pseudokod

[Przykłady pseudokodu]

### Cykle

[Informacje o liczbie cykli]

---

## Co testujemy

| Test | Opis |
|------|------|
| **Test 1** | [Opis testu] |

### Test jednostkowy — przykład

[Przykład kodu testu]

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| X.X | [Temat] |

---

## Definition of Done

- [ ] Wszystkie instrukcje zaimplementowane
- [ ] Wszystkie testy jednostkowe przechodzą
- [ ] Kod bez ostrzeżeń
- [ ] Dokumentacja zaktualizowana

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.*.cs` | [Opis] |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ X/X (100%)

---

## Pliki do utworzenia / modyfikacji

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.*.cs` | Utwórz / Modyfikuj |
```

---

## 📝 Wymagane sekcje w dokumentacji fazy

### 1. Nagłówek
- Tytuł fazy
- Tabela z metadany (status, pokrycie, zależności, etc.)

### 2. Cel fazy
- Krótki opis, co ma zostać osiągnięte

### 3. Co implementujemy
- **Lista instrukcji** (tabela z opcode, opisem, flagami)
- **Pseudokod** (przykłady implementacji)
- **Cykle** (liczba cykli dla każdej instrukcji)

### 4. Co testujemy
- **Lista testów** (tabela z opisem)
- **Przykład testu jednostkowego** (kod)

### 5. Sekcje dokumentacji pokryte
- Odnośniki do dokumentacji referencyjnej

### 6. Definition of Done
- Lista kryteriów zakończenia

### 7. Pliki implementacyjne
- Lista plików z opisem

### 8. Wyniki
- Wyniki build i testów

### 9. Pliki do utworzenia/modyfikacji
- Lista plików z akcjami

---

## 📜 Dokumentacja referencyjna

### 1. DOKUMENTACJA_SYMULATORA_6502.md
Główna dokumentacja techniczna symulatora, zawierająca:
- Opis architektury 6502
- Opis rejestrów i flag
- Tabele opcode'ów
- Opis trybów adresowania
- Opis przerwań

### 2. checklista.md
Lista wszystkich faz z ich statusem:

```markdown
| # | Faza | Status | Data rozpoczęcia | Data zakończenia | Testy |
|---|------|--------|-----------------|-----------------|-------|
| 0 | Szkielet | ✅ Zakończone | 2026-05-16 | 2026-05-16 | - |
| 1 | Load/Store | ✅ Zakończone | 2026-05-16 | 2026-05-16 | 15 |
| 2 | Transfer | ⏳ Do zrobienia | - | - | - |
```

---

## 📝 Zasady pisania dokumentacji

### 1. Formatowanie
- **Nagłówki**: Używaj `#`, `##`, `###` itd.
- **Listy**: Używaj `-` lub `*` dla list nieuporządkowanych
- **Tabele**: Używaj formatu Markdown dla tabel
- **Kod**: Używaj bloków kodu z podświetlaniem składni
- **Linki**: Używaj względnych ścieżek

### 2. Język
- **Polski** dla dokumentacji projektu
- **Angielski** dla komentarzy w kodzie (XML comments)
- **Krótkie zdania** - Unikaj długich zdań
- **Czytelność** - Dokumentacja powinna być łatwa do zrozumienia

### 3. Konsystencja
- **Terminologia**: Używaj tych samych terminów w całej dokumentacji
  - "instrukcja" zamiast "komenda" lub "rozkaz"
  - "rejestr" zamiast "register"
  - "flaga" zamiast "flag" (w tekście polskim)
- **Formatowanie**: Utrzymuj spójny styl
- **Struktura**: Wszystkie pliki faz powinny mieć taką samą strukturę

### 4. Aktualność
- **Zawsze aktualizuj** dokumentację po zmianach w kodzie
- **Zawsze aktualizuj** status fazy po zakończeniu
- **Zawsze aktualizuj** listę testów
- **Zawsze aktualizuj** wyniki build i testów

---

## 📊 Tabele opcode'ów

### 1. Format tabeli
```markdown
| Instrukcja | Opcode | Tryb adresowania | Bajty | Cykle | Flagi |
|------------|--------|-------------------|-------|-------|-------|
| LDA | 0xA9 | Immediate | 2 | 2 | N, Z |
| LDA | 0xA5 | Zero Page | 2 | 3 | N, Z |
| LDA | 0xB5 | Zero Page,X | 2 | 4 | N, Z |
```

### 2. Kolumny
- **Instrukcja**: Nazwa instrukcji
- **Opcode**: Hex value (z prefiksem `0x` lub `$`)
- **Tryb adresowania**: Immediate, Zero Page, Absolute, etc.
- **Bajty**: Liczba bajtów instrukcji
- **Cykle**: Liczba cykli (+ dodatkowe przy page crossing)
- **Flagi**: Flagi ustawiane przez instrukcję (`-` jeśli żadne)

---

## 🔤 Opisy instrukcji

### 1. Format opisu
```markdown
### [Instrukcja]

**Opcode:** 0xXX
**Tryb adresowania:** [Tryb]
**Liczba bajtów:** X
**Liczba cykli:** X (+Y przy page crossing)
**Flagi:** [Flagi]
**Opis:** [Opis działania]

**Pseudokod:**
```
[Pseudokod]
```

**Przykład:**
```
LDA $44
; A = $44
; Flagi: N=0, Z=0
```
```

### 2. Przykład dla ADC
```markdown
### ADC (Add with Carry)

**Opcode:** 0x69 (Immediate), 0x65 (Zero Page), 0x75 (Zero Page,X), etc.
**Tryb adresowania:** Immediate, Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y
**Liczba bajtów:** 2-3
**Liczba cykli:** 2-5 (+1 przy page crossing)
**Flagi:** C, V, N, Z
**Opis:** Dodaje wartość i flagę Carry do rejestru A.

**Pseudokod:**
```
A + value + C → A, C, V, N, Z
```

**Przykład:**
```
ADC #$05  ; A = A + $05 + C
```
```

---

## 📌 Przykłady dokumentacji

### 1. Przykład dokumentacji fazy (fragment)
```markdown
# Faza 4 — Arytmetyka (ADC, SBC bez BCD)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 5% |
| **Pokrycie całości** | 10% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 3-4h |

---

## Cel fazy

Implementacja instrukcji arytmetycznych ADC i SBC (bez obsługi trybu BCD).

---

## Co implementujemy

### Lista instrukcji

| Instrukcja | Opcode | Tryb adresowania | Bajty | Cykle | Flagi |
|------------|--------|-------------------|-------|-------|-------|
| **ADC** | 0x69 | Immediate | 2 | 2 | C, V, N, Z |
| **ADC** | 0x65 | Zero Page | 2 | 3 | C, V, N, Z |
| **ADC** | 0x75 | Zero Page,X | 2 | 4 | C, V, N, Z |
| **ADC** | 0x6D | Absolute | 3 | 4 | C, V, N, Z |
| **ADC** | 0x7D | Absolute,X | 3 | 4 (+1) | C, V, N, Z |
| **ADC** | 0x79 | Absolute,Y | 3 | 4 (+1) | C, V, N, Z |
| **ADC** | 0x61 | (Indirect,X) | 2 | 6 | C, V, N, Z |
| **ADC** | 0x71 | (Indirect),Y | 2 | 5 (+1) | C, V, N, Z |
| **SBC** | 0xE9 | Immediate | 2 | 2 | C, V, N, Z |
| **SBC** | 0xE5 | Zero Page | 2 | 3 | C, V, N, Z |
| **SBC** | 0xF5 | Zero Page,X | 2 | 4 | C, V, N, Z |
| **SBC** | 0xED | Absolute | 3 | 4 | C, V, N, Z |
| **SBC** | 0xFD | Absolute,X | 3 | 4 (+1) | C, V, N, Z |
| **SBC** | 0xF9 | Absolute,Y | 3 | 4 (+1) | C, V, N, Z |
| **SBC** | 0xE1 | (Indirect,X) | 2 | 6 | C, V, N, Z |
| **SBC** | 0xF1 | (Indirect),Y | 2 | 5 (+1) | C, V, N, Z |

### Pseudokod

```csharp
// ADC
A + value + (C ? 1 : 0) → A
// Flagi: C = carry, V = overflow, N = bit 7, Z = result == 0

// SBC
A - value - (C ? 0 : 1) → A
// Flagi: C = !borrow, V = overflow, N = bit 7, Z = result == 0
```

### Cykle

- Immediate: 2 cykle
- Zero Page: 3 cykle
- Zero Page,X/Y: 4 cykle
- Absolute: 4 cykle
- Absolute,X/Y: 4 cykle (+1 przy page crossing)
- (Indirect,X): 6 cykli
- (Indirect),Y: 5 cykli (+1 przy page crossing)

---

## Co testujemy

| Test | Opis |
|------|------|
| **ADC_Immediate_BasicAddition** | Proste dodawanie bez carry |
| **ADC_Immediate_WithCarry** | Dodawanie z carry |
| **ADC_Immediate_Overflow** | Przepełnienie (signed overflow) |
| **ADC_Immediate_CarrySet** | Ustawienie flagi carry |
| **ADC_ZeroPage_PageCrossing** | Page crossing w trybie Zero Page,X |
| **SBC_Immediate_BasicSubtraction** | Proste odejmowanie |
| **SBC_Immediate_WithBorrow** | Odejmowanie z borrow |
| **SBC_Immediate_Underflow** | Przepełnienie w dół |
```
```

---

## 🚫 Zakazane praktyki

1. **Nieaktualna dokumentacja** - Dokumentacja **MUSI** być zawsze aktualna
2. **Brak struktur** - Każdy plik **MUSI** mieć zdefiniowaną strukturę
3. **Niespójna terminologia** - Używaj tych samych terminów w całej dokumentacji
4. **Brak przykładów** - Każda faza **POWINNA** mieć przykłady kodu
5. **Brak odnośników** - Odnoś się do dokumentacji referencyjnej

---

## 🔍 Weryfikacja dokumentacji

Przed commitowaniem:
1. Sprawdź, czy wszystkie sekcje są wypełnione
2. Sprawdź, czy dokumentacja jest aktualna
3. Sprawdź, czy nie ma błędów ortograficznych
4. Sprawdź, czy formatowanie jest poprawne

---

## 📌 Podsumowanie

| Zasada | Wymaganie |
|--------|------------|
| **Język** | Polski (dla dokumentacji) |
| **Format** | Markdown |
| **Struktura** | Konsistentna z szablonem |
| **Aktualność** | Zawsze aktualna |
| **Tabele** | Używaj formatu Markdown |
| **Kod** | Bloki kodu z podświetlaniem |
| **Terminologia** | Spójna w całej dokumentacji |
