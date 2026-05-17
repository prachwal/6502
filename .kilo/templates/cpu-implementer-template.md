# Szablon dla cpu-implementer

## 📌 Zadanie
Implementuj **Fazę [XX] - [Nazwa Fazy]** zgodnie z dokumentacją w `docs/faza-[XX]-[nazwa].md`.

---

## 📝 Wymagania

### 1. Pliki do utworzenia
- [ ] `src/Cpu6502/Cpu6502.[NazwaPliku].cs` - Główna implementacja instrukcji
- [ ] `tests/Cpu6502.Tests/[NazwaPliku]Tests.cs` - Testy jednostkowe

### 2. Instrukcje do zaimplementowania
[Lista instrukcji z opcode'ami i trybami adresowania]

### 3. Wymagania techniczne
- [ ] Poprawna implementacja zgodnie ze specyfikacją MOS 6502
- [ ] Obsługa wszystkich trybów adresowania
- [ ] Poprawny timing cykli
- [ ] Poprawne ustawianie flag (C, Z, N, V, D, I, B, U)
- [ ] Komentarze XML dla wszystkich publicznych metod
- [ ] Krótkie metody (max 10-15 linii)

---

## 🎯 Implementacja

### Krok 1: Utwórz plik implementacyjny
```bash
# Utwórz nowy plik partial class
 touch src/Cpu6502/Cpu6502.[NazwaPliku].cs
```

### Krok 2: Zaimplementuj instrukcje
```csharp
/// <summary>
/// Wykonuje instrukcję [NAZWA] w trybie [TRYB].
/// [Opis działania]
/// </summary>
/// <remarks>
/// Opcode: 0x[XX]
/// Tryb adresowania: [TRYB]
/// Liczba cykli: [X]
/// Flagi: [FLAGI]
/// </remarks>
public void [NazwaMetody]()
{
    // Implementacja
}
```

### Krok 3: Zarejestruj opcode'y
W pliku `Cpu6502.Constructor.cs`:
```csharp
_opcodeTable[0x[XX]] = () => [NazwaMetody]();
```

---

## 🧪 Testowanie

### Krok 1: Utwórz plik testowy
```bash
# Utwórz nowy plik testowy
 touch tests/Cpu6502.Tests/[NazwaPliku]Tests.cs
```

### Krok 2: Zaimplementuj testy
```csharp
[TestFixture]
public class [NazwaPliku]Tests
{
    [Test]
    public void [Instrukcja]_[TrybAdresowania]_[Scenariusz]_[OczekiwanyRezultat]()
    {
        // Arrange
        var cpu = new Cpu6502();
        // Setup

        // Act
        cpu.[Metoda]();

        // Assert
        Assert.AreEqual([wartość], cpu.[Rejestr]);
        Assert.IsTrue(cpu.GetFlag(Flag[X]));
    }
}
```

### Krok 3: Uruchom testy
```bash
dotnet build
dotnet test --logger "console;verbosity=detailed"
```

---

## 📊 Weryfikacja

- [ ] `dotnet build` - 0 błędów, 0 ostrzeżeń
- [ ] `dotnet test` - wszystkie testy zielone
- [ ] Kod zgodny z `.kilo/rules/coding-standards.md`
- [ ] Testy zgodne z `.kilo/rules/testing-guidelines.md`

---

## 📝 Dokumentacja

### Krok 1: Zaktualizuj dokumentację fazy
W pliku `docs/faza-[XX]-[nazwa].md`:
- [ ] Zmień status na `[x] Zakończone`
- [ ] Dodaj `Data zakończenia` (YYYY-MM-DD)
- [ ] Dodaj `Liczba testów`
- [ ] Dodaj sekcję "Pliki implementacyjne"
- [ ] Dodaj sekcję "Wyniki" z Build/Test

### Krok 2: Zaktualizuj checklistę
W pliku `docs/checklista.md`:
- [ ] Zaktualizuj status fazy
- [ ] Dodaj datę zakończenia
- [ ] Dodaj liczbę testów

---

## 📌 Commit

```bash
git add .
git commit -m "feat: implementacja fazy [XX]"
```

---

## 🔍 Źródła referencyjne

1. [Official 6502 documentation](https://www.masswerk.at/6502/6502_instruction_set.html)
2. [nestest](https://github.com/chrismear/nestest)
3. [Visual6502.org](http://visual6502.org/)
4. [6502 Opcode Reference](https://www.nesdev.org/6502%20opcode%20reference.pdf)

---

## 💡 Wskazówki

1. **Zawsze zaczynaj od specyfikacji** - Przeczytaj `docs/faza-[XX]-[nazwa].md`
2. **Testuj często** - Uruchamiaj testy po każdej zmianie
3. **Dokumentuj** - Dodawaj komentarze XML i aktualizuj dokumentację
4. **Przestrzegaj konwencji** - Używaj istniejących wzorców z kodu
5. **Dziel na małe kroki** - Implementuj jedną instrukcję na raz
