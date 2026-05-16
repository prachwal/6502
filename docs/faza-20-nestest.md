# Faza 20 — Test zgodności — nestest

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 4% (sekcje: 12.1) |
| **Pokrycie całości** | 88% |
| **Zależności** | Fazy: 1–16 (wszystkie udokumentowane instrukcje + cycle-stepped) |
| **Szacowany czas** | 3–5h |

---

## Cel fazy

Zintegrowanie i przejście testu zgodności **nestest** — najważniejszego pierwszego testu poprawności emulatora 6502. Test weryfikuje wszystkie udokumentowane instrukcje i flagi.

---

## Co implementujemy

### nestest — opis

- ROM testowy: `nestest.nes` (lub sam binarny kod testu)
- Start: PC = $C000
- Test wykonuje każdą udokumentowaną instrukcję i porównuje wyniki z oczekiwanymi
- Nie testuje BCD ani nieudokumentowanych opcode'ów
- Format logu referencyjnego (`nestest.log`):
  ```
  C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD CYC:7
  C5F5  A2 00     LDX #$00                        A:00 X:00 Y:00 P:26 SP:FD CYC:10
  ...
  ```
- Kolumny: PC, opcode bytes, instrukcja w asm, A, X, Y, P, SP, CYC

### Test harness

```csharp
public class NestestRunner
{
    public void Run(IMemoryBus memory, Cpu6502 cpu)
    {
        // 1. Załaduj ROM testowy pod $C000
        // 2. Ustaw wektor RESET na $C000 (lub ręcznie ustaw PC)
        // 3. Wczytaj nestest.log jako listę oczekiwanych stanów

        cpu.PC = 0xC000;
        cpu.ExecuteOne();  // pierwsza instrukcja

        foreach (var expected in expectedStates)
        {
            AssertState(expected, cpu);
            cpu.ExecuteOne();
        }
    }
}
```

### Log

Linia logu nestest:
```
C000  4C F5 C5  JMP $C5F5                       A:00 X:00 Y:00 P:24 SP:FD CYC:7
```

Parsowanie:
- PC: 4 pierwsze znaki hex
- A, X, Y, P, SP po dwucyfrowym hexie poprzedzonym etykietą
- CYC: liczba dziesiętna

### Procedura testowa

1. Wczytaj plik `nestest.log` linia po linii
2. Dla każdej linii: wykonaj jedną instrukcję emulatorem
3. Porównaj: PC, A, X, Y, P, SP, Cycle z wartościami w logu
4. Jeśli niezgodność — wypisz expected vs actual i przerwij test

### Najczęstsze błędy wykrywane przez nestest

- Zła flaga po ADC/SBC (overflow, carry)
- Zła flaga Z/N po transferach
- Nieprawidłowa wartość PC po branch
- Brakujące cykle (CYC się nie zgadza)
- Page crossing nieprawidłowy
- Błąd w adresowaniu pośrednim

---

## Co testujemy

| Test | Opis |
|------|------|
| **nestest pełen przebieg** | Wszystkie linie logu zgodne |
| **Pierwsza instrukcja (JMP $C5F5)** | PC, A, X, Y, P, SP, CYC |
| **Ostatnia instrukcja przed pętlą** | PC w pętli sukcesu |
| **Wszystkie flagi** | N, V, B, D, I, Z, C zgodne w każdej instrukcji |
| **Wszystkie tryby adresowania** | Przez różne instrukcje w teście |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 12.1 | nestest — opis i sposób testowania |
| 12.6 | Strategia implementacji testów |

---

## Definition of Done

- [ ] Nestest ROM załadowany i uruchomiony
- [ ] Wszystkie linie logu nestest porównane — zero niezgodności
- [ ] PC, A, X, Y, P, SP, CYC zgodne w 100%
- [ ] Test kończy się sukcesem (PC w pętli)

---

## Pliki

| Plik | Akcja |
|------|-------|
| `tests/Cpu6502.Tests/NestestTest.cs` | Utwórz |
| `tests/Cpu6502.Tests/Data/nestest.log` | Dodaj |
| `tests/Cpu6502.Tests/Data/nestest.bin` | Dodaj |
