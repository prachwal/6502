# Szablon dla test-writer

## 📌 Zadanie
Utwórz **testy jednostkowe** dla **Fazy [XX] - [Nazwa Fazy]** w pliku `tests/Cpu6502.Tests/[NazwaPliku]Tests.cs`.

---

## 📝 Wymagania

### 1. Plik testowy
- [ ] Utwórz plik `[NazwaPliku]Tests.cs` w `tests/Cpu6502.Tests/`
- [ ] Użyj atrybutu `[TestFixture]` dla klasy testowej

### 2. Pokrycie testowe
- [ ] Testuj wszystkie instrukcje z fazy
- [ ] Testuj wszystkie tryby adresowania
- [ ] Testuj wpływ na wszystkie flagi (C, Z, N, V, D, I, B, U)
- [ ] Testuj przypadki brzegowe

---

## 🎯 Struktura testów

### 1. Klasa testowa
```csharp
using NUnit.Framework;

[TestFixture]
public class [NazwaPliku]Tests
{
    // Metoda pomocnicza do tworzenia CPU
    private Cpu6502 CreateCpu()
    {
        var memoryBus = new MockMemoryBus();
        return new Cpu6502(memoryBus);
    }

    // Testy
}
```

### 2. Nazewnictwo testów
Format: `[Instrukcja]_[TrybAdresowania]_[Scenariusz]_[OczekiwanyRezultat]`

**Przykłady:**
- `Lda_Immediate_ZeroValue_SetsZeroFlag`
- `Adc_Absolute_CarrySet_ResultCorrect`
- `Tax_TransferAToX_UpdatesXAndFlags`

---

## 🧪 Typy testów

### 1. Testy funkcjonalne
Testują podstawową funkcjonalność instrukcji.

```csharp
[Test]
public void [Instrukcja]_[Tryb]_[Działanie]()
{
    // Arrange
    var cpu = CreateCpu();
    cpu.Reset();
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0x[OPCODE]); // Opcode
    cpu._memoryBus.Write(0x8001, 0x[VALUE]); // Wartość

    // Act
    cpu.Tick(); // Fetch opcode
    cpu.Tick(); // Execute

    // Assert
    Assert.AreEqual(0x[EXPECTED], cpu.[REGISTER]);
}
```

### 2. Testy flag
Testują wpływ instrukcji na flagi.

```csharp
[Test]
public void [Instrukcja]_[Scenariusz]_[FlagResult]()
{
    // Arrange
    var cpu = CreateCpu();
    cpu.[REGISTER] = 0x[VALUE];
    cpu.SetFlag(Flag[X], [initialValue]);

    // Act
    cpu.[Method]();

    // Assert
    Assert.IsTrue(cpu.GetFlag(Flag[C]));
    Assert.IsFalse(cpu.GetFlag(Flag[Z]));
    Assert.IsTrue(cpu.GetFlag(Flag[N]));
    Assert.IsFalse(cpu.GetFlag(Flag[V]));
}
```

### 3. Testy przypadków brzegowych
Testują specjalne przypadki.

```csharp
[Test]
public void [Instrukcja]_[Tryb]_[EdgeCase]()
{
    // Arrange - ustaw stan dla przypadku brzegowego
    var cpu = CreateCpu();
    cpu.A = 0x7F; // Max positive value
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0x69); // ADC Immediate
    cpu._memoryBus.Write(0x8001, 0x01); // +1

    // Act
    cpu.Tick();
    cpu.Tick();

    // Assert - sprawdź overflow
    Assert.AreEqual(0x80, cpu.A);
    Assert.IsTrue(cpu.GetFlag(FlagV)); // Overflow
    Assert.IsTrue(cpu.GetFlag(FlagN)); // Negative
}
```

### 4. Testy trybów adresowania
Testują wszystkie tryby adresowania dla instrukcji.

```csharp
// Immediate
[Test]
public void Adc_Immediate_BasicAddition() { ... }

// Zero Page
[Test]
public void Adc_ZeroPage_BasicAddition() { ... }

// Zero Page,X
[Test]
public void Adc_ZeroPageX_BasicAddition() { ... }

// Absolute
[Test]
public void Adc_Absolute_BasicAddition() { ... }

// Absolute,X
[Test]
public void Adc_AbsoluteX_BasicAddition() { ... }

// Absolute,Y
[Test]
public void Adc_AbsoluteY_BasicAddition() { ... }

// (Indirect,X)
[Test]
public void Adc_IndirectX_BasicAddition() { ... }

// (Indirect),Y
[Test]
public void Adc_IndirectY_BasicAddition() { ... }
```

---

## 🔧 Setup testów

### 1. Inicjalizacja CPU
```csharp
var cpu = CreateCpu();
cpu.Reset();
```

### 2. Inicjalizacja pamięci
```csharp
// Ustawianie kodu w pamięci
cpu._memoryBus.Write(0x8000, 0xA9); // LDA Immediate
cpu._memoryBus.Write(0x8001, 0x42); // Value

// Ustawianie danych w pamięci
cpu._memoryBus.Write(0x1000, 0x55); // Zero Page
cpu._memoryBus.Write(0x1234, 0xAA); // Absolute
```

### 3. Ustawianie stanu początkowego
```csharp
cpu.PC = 0x8000;
cpu.A = 0x42;
cpu.X = 0x05;
cpu.Y = 0x10;
cpu.SP = 0xFF;
cpu.SetFlag(FlagC, true);
cpu.SetFlag(FlagN, false);
cpu.SetFlag(FlagZ, false);
cpu.SetFlag(FlagV, false);
```

---

## 📊 Asercje

### 1. Wartości rejestrów
```csharp
Assert.AreEqual(0x42, cpu.A);
Assert.AreEqual(0x05, cpu.X);
Assert.AreEqual(0x10, cpu.Y);
Assert.AreEqual(0xFF, cpu.SP);
Assert.AreEqual(0x8002, cpu.PC);
```

### 2. Flagi
```csharp
Assert.IsTrue(cpu.GetFlag(FlagC));
Assert.IsFalse(cpu.GetFlag(FlagZ));
Assert.IsTrue(cpu.GetFlag(FlagN));
Assert.IsFalse(cpu.GetFlag(FlagV));
```

### 3. Pamięć
```csharp
Assert.AreEqual(0x42, cpu._memoryBus.Read(0x1000));
```

### 4. Wyjątki
```csharp
Assert.Throws<InvalidOperationException>(() => cpu.Execute(0xFF));
```

---

## 📌 Przykłady pełnych testów

### 1. Test dla TAX
```csharp
[Test]
public void TAX_CopiesAToX_UpdatesXAndFlags()
{
    // Arrange
    var cpu = CreateCpu();
    cpu.A = 0x42;
    cpu.X = 0x00;

    // Act
    cpu.Tax();

    // Assert
    Assert.AreEqual(0x42, cpu.X);
    Assert.IsFalse(cpu.GetFlag(FlagZ));
    Assert.IsFalse(cpu.GetFlag(FlagN));
}
```

### 2. Test dla ADC z Carry
```csharp
[Test]
public void Adc_Immediate_WithCarry_ResultCorrect()
{
    // Arrange
    var cpu = CreateCpu();
    cpu.A = 0xFE;
    cpu.SetFlag(FlagC, true); // Carry = 1
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0x69); // ADC Immediate
    cpu._memoryBus.Write(0x8001, 0x02); // +2

    // Act
    cpu.Tick(); // Fetch opcode
    cpu.Tick(); // Execute

    // Assert
    Assert.AreEqual(0x01, cpu.A); // FE + 2 + 1 (carry) = 101
    Assert.IsTrue(cpu.GetFlag(FlagC)); // Carry
}
```

### 3. Test dla CLC
```csharp
[Test]
public void CLC_ClearsCarry_OtherFlagsUnchanged()
{
    // Arrange
    var cpu = CreateCpu();
    cpu.SetFlag(FlagC, true);
    cpu.SetFlag(FlagN, true);
    cpu.SetFlag(FlagZ, true);
    cpu.SetFlag(FlagV, true);

    // Act
    cpu.Clc();

    // Assert
    Assert.IsFalse(cpu.GetFlag(FlagC));
    Assert.IsTrue(cpu.GetFlag(FlagN)); // Unchanged
    Assert.IsTrue(cpu.GetFlag(FlagZ)); // Unchanged
    Assert.IsTrue(cpu.GetFlag(FlagV)); // Unchanged
}
```

---

## 🚫 Zakazane praktyki

1. **Testy zależne od siebie** - Każdy test powinien być niezależny
2. **Testy bez asercji** - Każdy test powinien coś weryfikować
3. **Testy zbyt ogólne** - Testuj konkretne zachowania
4. **Testy powolne** - Unikaj długich operacji w testach
5. **Testy losowe** - Unikaj losowości, używaj stałych wartości

---

## 🔍 Weryfikacja testów

Przed commitowaniem:
1. Uruchom `dotnet test` - **wszystkie testy zielone**
2. Sprawdź pokrycie kodu (opcjonalnie)
3. Upewnij się, że testy są czytelne i zrozumiałe

---

## 📌 Podsumowanie

| Zasada | Wymaganie |
|--------|------------|
| **Pokrycie** | 100% dla każdej instrukcji |
| **Nazewnictwo** | `Instrukcja_TrybAdresowania_Scenariusz_OczekiwanyRezultat` |
| **Izolacja** | Każdy test niezależny |
| **Asercje** | Sprawdzaj wartości, flagi, pamięć |
| **Setup** | Inicjalizuj CPU, pamięć, rejestry |
| **Przypadki brzegowe** | Testuj overflow, carry, zero, etc. |

---

## 💡 Wskazówki

1. **Testuj małe fragmenty** - Jedna asercja na jeden aspekt
2. **Używaj opisowych nazw** - Nazwa testu powinna mówić, co testuje
3. **Testuj wszystkie scenariusze** - Normalne, brzegowe, błędne
4. **Utrzymuj czytelność** - Testy powinny być łatwe do zrozumienia
5. **Aktualizuj dokumentację** - Dodawaj przykłady testów do dokumentacji fazy
