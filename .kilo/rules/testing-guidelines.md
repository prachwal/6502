# Testing Guidelines - Symulator 6502

## 📌 Ogólne zasady

1. **Framework**: NUnit 4.3.2
2. **Pokrycie**: 100% dla każdej instrukcji
3. **Izolacja**: Każdy test powinien być niezależny
4. **Powtarzalność**: Testy powinny zawsze dawać ten sam wynik

---

## 🗂️ Struktura testów

### 1. Organizacja plików
- **Jeden plik testowy na grupę instrukcji**
  - `TransferTests.cs` - Testy dla TAX, TAY, TSX, TXA, TXS, TYA
  - `ArithmeticTests.cs` - Testy dla ADC, SBC
  - `LogicTests.cs` - Testy dla AND, OR, EOR
- **Nazwa klasy testowej**: `{Grupa}Tests`

### 2. Struktura klasy testowej
```csharp
[TestFixture]
public class TransferTests
{
    // Testy dla TAX
    [Test]
    public void TAX_CopiesAToX() { ... }
    
    [Test]
    public void TAX_SetsZeroFlagWhenAIsZero() { ... }
    
    // Testy dla TAY
    [Test]
    public void TAY_CopiesAToY() { ... }
    
    // ...
}
```

---

## 📝 Konwencje nazewnictwa testów

### Format nazwy testu:
```
{Instrukcja}_{TrybAdresowania}_{Scenariusz}_{OczekiwanyRezultat}
```

**Przykłady:**
- `Lda_Immediate_ZeroValue_SetsZeroFlag`
- `Adc_Absolute_CarrySet_ResultCorrect`
- `Tax_TransferAToX_UpdatesXAndFlags`
- `Clc_ClearsCarry_FlagUnchanged`

### Składowe nazwy:
1. **Instrukcja**: Nazwa instrukcji (LDA, ADC, TAX, etc.)
2. **TrybAdresowania** (opcjonalnie): Immediate, ZeroPage, Absolute, etc.
3. **Scenariusz**: Co jest testowane (ZeroValue, CarrySet, Overflow, etc.)
4. **OczekiwanyRezultat**: Co powinno się stać (SetsZeroFlag, ResultCorrect, etc.)

---

## 🧪 Typy testów

### 0. Testy cycle-stepped i przerwań

Testy dla instrukcji wielocyklowych muszą odróżniać koniec instrukcji od obsługi przerwania na kolejnej granicy. W helperach typu `ExecuteOne()` przyjmujemy, że jedno wywołanie wykonuje CPU do następnego `Sync`.

Stałe oczekiwania projektu:

1. `CLI, NOP` z aktywnym IRQ: po pierwszym `ExecuteOne()` PC wskazuje `NOP`, po drugim PC wskazuje bajt po `NOP`, po trzecim CPU wchodzi w wektor IRQ.
2. Branch not-taken z aktywnym IRQ: po jednym `ExecuteOne()` PC wskazuje następną instrukcję, a flaga `I` pozostaje bez zmian.
3. Testy opisujące tę samą właściwość nie mogą mieć sprzecznych asercji. Gdy zmieniasz kontrakt timingowy, wyszukaj duplikaty przez `rg "CLI|IRQ|Branch_InterruptTiming|JMP_Indirect" tests`.

### 0.1. Pułapka testu JMP indirect bug

Przy testowaniu błędu NMOS `JMP ($xxFF)` high byte jest czytany z `$xx00`. Nie wolno umieszczać kodu testowego pod tym samym adresem, który służy jako `$xx00`, bo zapis high byte nadpisze opcode.

Poprawny wzorzec:

```csharp
LoadProgram(0x0300, 0x6C, 0xFF, 0x01); // JMP ($01FF)
memory.Write(0x01FF, 0x12);            // low
memory.Write(0x0100, 0x34);            // high z tej samej strony
```

Niepoprawny wzorzec:

```csharp
LoadProgram(0x0100, 0x6C, 0xFF, 0x01);
memory.Write(0x0100, 0x34); // nadpisuje opcode 0x6C
```

### 0.2. Jak unikać zapętlenia debugowania

Jeśli wiele niepowiązanych testów wielocyklowych zaczyna kończyć się z PC tuż po operandzie, najpierw sprawdź mechanikę dispatchera i `_sync`, nie pojedyncze implementacje opcode'ów.

Procedura:

1. Zawęź do jednego testu i jednego opcode'u.
2. Uruchom mały harness albo test z logowaniem cykli.
3. Potwierdź, że każdy cykl instrukcji wraca z dispatchera jako obsłużony.
4. Dopiero potem poprawiaj semantykę danej instrukcji.

### 1. Testy funkcjonalne
Testują podstawową funkcjonalność instrukcji.

**Przykład:**
```csharp
[Test]
public void Lda_Immediate_LoadsValueToA()
{
    // Arrange
    var cpu = new Cpu6502();
    cpu.Reset();
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0xA9); // LDA Immediate
    cpu._memoryBus.Write(0x8001, 0x42); // Value

    // Act
    cpu.Tick(); // Fetch opcode
    cpu.Tick(); // Execute

    // Assert
    Assert.AreEqual(0x42, cpu.A);
}
```

### 2. Testy flag
Testują wpływ instrukcji na flagi procesora.

**Przykład:**
```csharp
[Test]
public void Lda_Immediate_ZeroValue_SetsZeroFlag()
{
    // Arrange
    var cpu = new Cpu6502();
    cpu.Reset();
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0xA9); // LDA Immediate
    cpu._memoryBus.Write(0x8001, 0x00); // Zero value

    // Act
    cpu.Tick();
    cpu.Tick();

    // Assert
    Assert.IsTrue(cpu.GetFlag(FlagZ));
    Assert.IsFalse(cpu.GetFlag(FlagN));
}
```

### 3. Testy przypadków brzegowych
Testują specjalne przypadki:
- Przepełnienie (overflow)
- Przeniesienie (carry)
- Wartości minimalne/maximalne
- Page crossing

**Przykład:**
```csharp
[Test]
public void Adc_Immediate_Overflow_SetsOverflowFlag()
{
    // Arrange
    var cpu = new Cpu6502();
    cpu.Reset();
    cpu.A = 0x7F; // Max positive value
    cpu.PC = 0x8000;
    cpu._memoryBus.Write(0x8000, 0x69); // ADC Immediate
    cpu._memoryBus.Write(0x8001, 0x01); // +1

    // Act
    cpu.Tick();
    cpu.Tick();

    // Assert
    Assert.AreEqual(0x80, cpu.A);
    Assert.IsTrue(cpu.GetFlag(FlagV)); // Overflow
    Assert.IsTrue(cpu.GetFlag(FlagN)); // Negative
}
```

### 4. Testy trybów adresowania
Testują wszystkie tryby adresowania dla danej instrukcji.

**Przykład dla ADC:**
```csharp
// Immediate
[Test]
public void Adc_Immediate_CarrySet_ResultCorrect() { ... }

// Zero Page
[Test]
public void Adc_ZeroPage_CarrySet_ResultCorrect() { ... }

// Zero Page,X
[Test]
public void Adc_ZeroPageX_CarrySet_ResultCorrect() { ... }

// Absolute
[Test]
public void Adc_Absolute_CarrySet_ResultCorrect() { ... }

// Absolute,X
[Test]
public void Adc_AbsoluteX_CarrySet_ResultCorrect() { ... }

// Absolute,Y
[Test]
public void Adc_AbsoluteY_CarrySet_ResultCorrect() { ... }

// (Indirect,X)
[Test]
public void Adc_IndirectX_CarrySet_ResultCorrect() { ... }

// (Indirect),Y
[Test]
public void Adc_IndirectY_CarrySet_ResultCorrect() { ... }
```

---

## 🎯 Co testować

### 1. Dla każdej instrukcji:
- [ ] Poprawność wykonania (czy robi to, co powinna)
- [ ] Wpływ na flagi (C, Z, N, V, D, I, B, U)
- [ ] Wszystkie tryby adresowania (jeśli dotyczy)
- [ ] Przypadki brzegowe

### 2. Dla flag Set/Clear:
- [ ] Czy ustawia/czyści odpowiednią flagę
- [ ] Czy nie wpływa na inne flagi

### 3. Dla instrukcji arytmetycznych:
- [ ] Poprawne obliczenia
- [ ] Carry (przeniesienie)
- [ ] Overflow (przepełnienie)
- [ ] Negative (znak)
- [ ] Zero (zero)

### 4. Dla instrukcji logicznych:
- [ ] Poprawne operacje bitowe
- [ ] Flagi N i Z

### 5. Dla instrukcji shift/rotate:
- [ ] Poprawne przesunięcia/obroty
- [ ] Flagi C, N, Z

---

## 🔧 Setup testów

### 1. Tworzenie CPU
```csharp
private Cpu6502 CreateCpu()
{
    var memoryBus = new MockMemoryBus();
    return new Cpu6502(memoryBus);
}
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
cpu.Reset();
cpu.PC = 0x8000;
cpu.A = 0x42;
cpu.X = 0x05;
cpu.Y = 0x10;
cpu.SetFlag(FlagC, true);
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
