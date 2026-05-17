# Coding Standards - Symulator 6502

## 📌 Ogólne zasady

1. **Język**: C# 12
2. **Framework**: .NET 10.0
3. **Styl**: Konsistentny z istniejącym kodem
4. **Architektura**: Partial classes z podziałem na pliki według funkcjonalności

---

## 🗂️ Struktura plików

### Partial Classes
- Każda grupa instrukcji **MUSI** być w oddzielnym pliku `Cpu6502.*.cs`
- Nazwa pliku powinna odzwierciedlać grupę instrukcji (np. `Cpu6502.Arithmetic.cs`)

**Dozwolone pliki do modyfikacji:**
- `Cpu6502.CycleStepped.Core.cs` - Inicjalizacja opcode'ów / dispatch cykli
- `Cpu6502.Fields.cs` - Pola rejestrowe
- `Cpu6502.Constants.cs` - Stałe

**ZABRONIONE:**
- Dodawanie kodu do istniejących plików (poza wyżej wymienionymi)
- Łączenie różnych grup instrukcji w jednym pliku

---

## 📏 Zasady pisania kodu

### 1. Metody
- **Długość**: Max 10-15 linii (wyjątki wymagają uzasadnienia w komentarzu)
- **Nazewnictwo**: PascalCase, opisowe nazwy
- **Parametry**: camelCase
- **Zmienne lokalne**: camelCase

**Przykłady dobrych nazw:**
- `FetchImmediate()`
- `SetNZ(byte value)`
- `AdcAbsolute()`
- `CalculateOverflow()`

**Przykłady złych nazw:**
- `DoStuff()`
- `Process()`
- `HandleOpcode()`

### 2. Komentarze
- **WYMAGANE** komentarze XML dla **wszystkich** publicznych typów i metod
- Komentarze powinny opisywać:
  - Co robi metoda
  - Parametry (jeśli dotyczy)
  - Zwracana wartość (jeśli dotyczy)
  - Opcode (jeśli dotyczy)
  - Tryb adresowania (jeśli dotyczy)
  - Liczba cykli (jeśli dotyczy)

**Przykład:**
```csharp
/// <summary>
/// Wykonuje instrukcję ADC (Add with Carry) w trybie Immediate.
/// Dodaje wartość bezpośrenią do rejestru A z uwzględnieniem flagi Carry.
/// </summary>
/// <remarks>
/// Opcode: 0x69
/// Tryb adresowania: Immediate
/// Liczba cykli: 2
/// Flagi: C, V, N, Z
/// </remarks>
public void AdcImm()
{
    byte value = FetchImmediate();
    Adc(value);
}
```

### 3. Flagi i rejestry
- **Nazewnictwo flag**: Używaj stałych z `Cpu6502.Constants.cs`
  - `FlagC` - Carry
  - `FlagZ` - Zero
  - `FlagN` - Negative
  - `FlagV` - Overflow
  - `FlagD` - Decimal
  - `FlagI` - Interrupt Disable
  - `FlagB` - Break
  - `FlagU` - Unused

- **Rejestry**:
  - `A` - Akumulator
  - `X` - Rejestr X
  - `Y` - Rejestr Y
  - `SP` - Stack Pointer
  - `PC` - Program Counter
  - `P` - Status Register

### 4. Operacje na flagach
- **Ustawianie flagi**: `P |= FlagC`
- **Czyszczenie flagi**: `P &= ~FlagC`
- **Sprawdzanie flagi**: `(P & FlagC) != 0`

**WYMAGANE** używanie metod pomocniczych:
- `SetFlag(FlagC, true)` - Ustaw flagę
- `SetFlag(FlagC, false)` - Wyczyść flagę
- `GetFlag(FlagC)` - Pobierz flagę
- `SetNZ(byte value)` - Ustaw flagi N i Z na podstawie wartości

---

## 🔤 Konwencje nazewnictwa

| Typ | Konwencja | Przykład |
|-----|-----------|----------|
| **Klasy** | PascalCase | `Cpu6502`, `MemoryBus` |
| **Metody publiczne** | PascalCase | `Execute`, `SetNZ`, `FetchImmediate` |
| **Metody prywatne** | PascalCase | `CalculateOverflow`, `UpdateFlags` |
| **Pola prywatne** | `_camelCase` | `_memoryBus`, `_opcodeTable` |
| **Stałe** | `PascalCase` | `FlagC`, `FlagZ`, `StackPage` |
| **Parametry** | camelCase | `opcode`, `address`, `value` |
| **Zmienne lokalne** | camelCase | `result`, `temp`, `flagValue` |
| **Interfejsy** | `IPascalCase` | `IMemoryBus` |
| **Właściwości** | PascalCase | `A`, `X`, `Y`, `PC` |

---

## 📝 Struktura metod

### 1. Metody instrukcji
Każda instrukcja powinna mieć:
1. **Nazwę odzwierciedlającą instrukcję i tryb adresowania**
2. **Komentarz XML**
3. **Implementację zgodną ze specyfikacją**

**Przykład:**
```csharp
/// <summary>
/// Wykonuje instrukcję LDA (Load Accumulator) w trybie Immediate.
/// </summary>
/// <remarks>
/// Opcode: 0xA9
/// Tryb adresowania: Immediate
/// Liczba cykli: 2
/// Flagi: N, Z
/// </remarks>
public void LdaImm()
{
    A = FetchImmediate();
    SetNZ(A);
}
```

### 2. Metody pomocnicze
Metody pomocnicze powinny:
- Być **krótkie** (max 10-15 linii)
- Mieć **opisowe nazwy**
- Być **czyste** (pure functions) tam, gdzie to możliwe

**Przykład:**
```csharp
/// <summary>
/// Ustawia flagi Negative i Zero na podstawie wartości.
/// </summary>
/// <param name="value">Wartość do sprawdzenia.</param>
private void SetNZ(byte value)
{
    SetFlag(FlagN, (value & 0x80) != 0);
    SetFlag(FlagZ, value == 0);
}
```

---

## 🚫 Zakazane praktyki

1. **Długie metody** (powyżej 20 linii bez uzasadnienia)
2. **Nieopisowe nazwy** (np. `DoStuff`, `Process`)
3. **Brak komentarzy XML** dla publicznych metod
4. **Pliki z wieloma niepowiązanymi odpowiedzialnościami**
5. **Kod bez testów**
6. **Mutowanie stanu bez potrzeby**
7. **Duplikacja kodu** (używaj metod pomocniczych)
8. **Magiczne liczby** (używaj stałych)

---

## 🔍 Weryfikacja kodu

Przed commitowaniem:
1. Uruchom `dotnet build` - **0 ostrzeżeń**
2. Uruchom `dotnet test` - **wszystkie testy zielone**
3. Sprawdź, czy kod jest zgodny z tymi standardami

---

## 📌 Podsumowanie

| Zasada | Wymaganie |
|--------|------------|
| **Długość metod** | Max 10-15 linii |
| **Komentarze XML** | Wymagane dla publicznych metod |
| **Nazewnictwo** | Opisowe, PascalCase/camelCase |
| **Partial classes** | Oddzielne pliki dla każdej grupy |
| **Testy** | Wymagane dla każdej funkcjonalności |
| **Flagi** | Używaj stałych i metod pomocniczych |
