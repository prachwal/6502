# Opcode Implementation Rules - Symulator 6502

## 📌 Ogólne zasady

1. **Dokładność**: Każda instrukcja **MUSI** być zaimplementowana zgodnie ze specyfikacją MOS 6502
2. **Timing**: Liczba cykli **MUSI** być poprawna
3. **Flagi**: Wpływ na flagi **MUSI** być dokładny
4. **Tryby adresowania**: Wszystkie tryby **MUSZĄ** być obsługiwane

---

## 🗂️ Struktura implementacji

### 1. Pliki partial class
Każda grupa instrukcji **MUSI** być w oddzielnym pliku:

```
src/Cpu6502/
├── Cpu6502.Arithmetic.cs    # ADC, SBC
├── Cpu6502.Logic.cs         # AND, OR, EOR
├── Cpu6502.Transfer.cs      # TAX, TAY, TSX, TXA, TXS, TYA
├── Cpu6502.FlagsSetClear.cs # CLC, SEC, CLD, SED, CLI, SEI, CLV
└── ...
```

### 2. Rejestracja opcode'ów
Każdy opcode **MUSI** być zarejestrowany w warstwie inicjalizacji opcode'ów używanej przez aktualną architekturę:

```csharp
_opcodeTable[0xAA] = () => Tax();      // TAX
_opcodeTable[0xA8] = () => Tay();      // TAY
_opcodeTable[0x69] = () => AdcImm();   // ADC Immediate
_opcodeTable[0x65] = () => AdcZp();    // ADC Zero Page
// ...
```

---

## 📝 Implementacja instrukcji

### 1. Struktura metody instrukcji
Każda instrukcja powinna:
1. Mieć **opisową nazwę** (np. `AdcImm`, `LdaZpX`)
2. Mieć **komentarz XML** z opcode, trybem adresowania, cyklami i flagami
3. **Pobierać operand** (jeśli dotyczy)
4. **Wykonywać operację**
5. **Ustawiać flagi** (jeśli dotyczy)

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

### 2. Metody pomocnicze
Stwórz metody pomocnicze dla powtarzających się operacji:

```csharp
/// <summary>
/// Wykonuje operację ADC (Add with Carry).
/// </summary>
/// <param name="value">Wartość do dodania.</param>
private void Adc(byte value)
{
    int result = A + value + (GetFlag(FlagC) ? 1 : 0);
    
    // Sprawdź Carry
    SetFlag(FlagC, result > 0xFF);
    
    // Sprawdź Overflow
    bool signA = (A & 0x80) != 0;
    bool signValue = (value & 0x80) != 0;
    bool signResult = (result & 0x80) != 0;
    SetFlag(FlagV, signA == signValue && signA != signResult);
    
    // Ustaw A i flagi N, Z
    A = (byte)result;
    SetNZ(A);
}
```

---

## 🎯 Tryby adresowania

### 1. Lista trybów adresowania
| Tryb | Opis | Liczba bajtów | Liczba cykli |
|------|------|----------------|---------------|
| Implied | Brak operandu | 1 | 2 |
| Immediate | Wartość bezpośrednia | 2 | 2 |
| Zero Page | Adres 8-bitowy | 2 | 3 |
| Zero Page,X | Adres 8-bitowy + X | 2 | 4 |
| Zero Page,Y | Adres 8-bitowy + Y | 2 | 4 |
| Absolute | Adres 16-bitowy | 3 | 4 |
| Absolute,X | Adres 16-bitowy + X | 3 | 4 (+1 przy page crossing) |
| Absolute,Y | Adres 16-bitowy + Y | 3 | 4 (+1 przy page crossing) |
| (Indirect,X) | Pośredni przez (zp,X) | 2 | 6 |
| (Indirect),Y | Pośredni przez zp z Y | 2 | 5 (+1 przy page crossing) |
| Relative | Adres względny (dla skoków) | 2 | 2 (+1 jeśli skok, +2 jeśli page crossing) |
| Indirect | Pośredni (JMP) | 3 | 5 |

### 2. Implementacja trybów adresowania
Każdy tryb adresowania powinien mieć swoją metodę pobierania operandu:

```csharp
// Immediate
private byte FetchImmediate()
{
    PC++;
    return _memoryBus.Read(PC);
}

// Zero Page
private byte FetchZeroPage()
{
    PC++;
    return _memoryBus.Read(_memoryBus.Read(PC));
}

// Zero Page,X
private byte FetchZeroPageX()
{
    PC++;
    byte address = (byte)(_memoryBus.Read(PC) + X);
    return _memoryBus.Read(address);
}

// Absolute
private byte FetchAbsolute()
{
    PC++;
    byte low = _memoryBus.Read(PC);
    PC++;
    byte high = _memoryBus.Read(PC);
    ushort address = (ushort)((high << 8) | low);
    return _memoryBus.Read(address);
}

// Absolute,X
private byte FetchAbsoluteX()
{
    PC++;
    byte low = _memoryBus.Read(PC);
    PC++;
    byte high = _memoryBus.Read(PC);
    ushort address = (ushort)((high << 8) | low);
    address += X;
    
    // Page crossing - dodatkowy cykl
    if ((address & 0xFF00) != ((address - X) & 0xFF00))
    {
        // Dodatkowy cykl
    }
    
    return _memoryBus.Read(address);
}
```

### 3. Page Crossing
Dodatkowy cykl jest potrzebny, gdy:
- **Absolute,X**: Adres + X przekracza granicę strony ($XX00-$XXFF)
- **Absolute,Y**: Adres + Y przekracza granicę strony
- **(Indirect),Y**: Adres pośredni + Y przekracza granicę strony

**Przykład sprawdzania page crossing:**
```csharp
bool pageCrossed = ((address & 0xFF00) != ((address - offset) & 0xFF00));
if (pageCrossed)
{
    // Dodaj dodatkowy cykl
}
```

---

## 🔤 Implementacja flag

### 1. Flagi i ich znaczenie
| Flaga | Bit | Opis |
|-------|-----|------|
| C (Carry) | 0 | Przeniesienie/pożyczka |
| Z (Zero) | 1 | Wynik zero |
| I (Interrupt) | 2 | Blokada przerwań |
| D (Decimal) | 3 | Tryb BCD |
| B (Break) | 4 | Instrukcja BRK |
| U (Unused) | 5 | Nieużywany (zawsze 1) |
| V (Overflow) | 6 | Przepełnienie |
| N (Negative) | 7 | Wynik ujemny |

### 2. Metody do obsługi flag
```csharp
// Ustawienie flagi
private void SetFlag(byte flag, bool value)
{
    if (value)
        P |= flag;
    else
        P &= (byte)~flag;
}

// Pobranie flagi
private bool GetFlag(byte flag)
{
    return (P & flag) != 0;
}

// Ustawienie flag N i Z
private void SetNZ(byte value)
{
    SetFlag(FlagN, (value & 0x80) != 0);
    SetFlag(FlagZ, value == 0);
}
```

### 3. Wpływ instrukcji na flagi
| Instrukcja | C | Z | I | D | B | U | V | N |
|------------|---|---|---|---|---|---|---|---|
| ADC | ✅ | ✅ | - | - | - | - | ✅ | ✅ |
| SBC | ✅ | ✅ | - | - | - | - | ✅ | ✅ |
| AND | - | ✅ | - | - | - | - | - | ✅ |
| OR | - | ✅ | - | - | - | - | - | ✅ |
| EOR | - | ✅ | - | - | - | - | - | ✅ |
| INC | - | ✅ | - | - | - | - | - | ✅ |
| DEC | - | ✅ | - | - | - | - | - | ✅ |
| ASL | ✅ | ✅ | - | - | - | - | - | ✅ |
| LSR | ✅ | ✅ | - | - | - | - | - | ✅ |
| ROL | ✅ | ✅ | - | - | - | - | - | ✅ |
| ROR | ✅ | ✅ | - | - | - | - | - | ✅ |
| TAX | - | ✅ | - | - | - | - | - | ✅ |
| TAY | - | ✅ | - | - | - | - | - | ✅ |
| TXA | - | ✅ | - | - | - | - | - | ✅ |
| TYA | - | ✅ | - | - | - | - | - | ✅ |
| TSX | - | ✅ | - | - | - | - | - | ✅ |
| TXS | - | - | - | - | - | - | - | - |
| CLC | ✅ | - | - | - | - | - | - | - |
| SEC | ✅ | - | - | - | - | - | - | - |
| CLI | - | - | ✅ | - | - | - | - | - |
| SEI | - | - | ✅ | - | - | - | - | - |
| CLD | - | - | - | ✅ | - | - | - | - |
| SED | - | - | - | ✅ | - | - | - | - |
| CLV | - | - | - | - | - | - | ✅ | - |

---

## 📊 Timing i cykle

### 1. Model instruction-stepped
- Każda instrukcja wykonuje się w **jednym wywołaniu** `Execute()`
- Liczba cykli jest symulowana wewnątrz metody

### 2. Model cycle-stepped (docelowy)
- Każdy cykl jest symulowany oddzielnie
- `Tick()` wykonuje jeden cykl
- Instrukcje są podzielone na etapy (fetch, read, execute, etc.)

### 3. Liczba cykli dla poszczególnych instrukcji
| Instrukcja | Tryb | Cykle | Uwagi |
|------------|------|-------|-------|
| TAX | Implied | 2 | - |
| LDA | Immediate | 2 | - |
| LDA | Zero Page | 3 | - |
| LDA | Zero Page,X | 4 | - |
| LDA | Absolute | 4 | - |
| LDA | Absolute,X | 4 (+1) | +1 przy page crossing |
| ADC | Immediate | 2 | - |
| ADC | Zero Page | 3 | - |
| JMP | Absolute | 3 | - |
| JMP | Indirect | 5 | - |
| BRK | Implied | 7 | - |

---

## 🔧 Implementacja poszczególnych grup instrukcji

### 1. Instrukcje Load/Store (LDA, LDX, LDY, STA, STX, STY)
**Wspólne cechy:**
- Ładują lub zapisują wartości do/ze rejestrów
- Ustawiają flagi N i Z (oprócz STA, STX, STY)

**Przykład LDA:**
```csharp
public void LdaImm()
{
    A = FetchImmediate();
    SetNZ(A);
}

public void LdaZp()
{
    A = FetchZeroPage();
    SetNZ(A);
}
```

### 2. Instrukcje Transfer (TAX, TAY, TSX, TXA, TXS, TYA)
**Wspólne cechy:**
- Kopiują wartości między rejestrami
- Wszystkie w trybie Implied
- 2 cykle
- Ustawiają flagi N i Z (oprócz TXS)

**Przykład:**
```csharp
public void Tax()
{
    X = A;
    SetNZ(X);
}

public void Txs()
{
    SP = X;
    // TXS nie ustawia flag
}
```

### 3. Instrukcje Flag Set/Clear (CLC, SEC, CLD, SED, CLI, SEI, CLV)
**Wspólne cechy:**
- Ustawiają lub czyszczą pojedynczą flagę
- Wszystkie w trybie Implied
- 2 cykle
- Nie wpływają na inne flagi

**Przykład:**
```csharp
public void Clc()
{
    SetFlag(FlagC, false);
}

public void Sec()
{
    SetFlag(FlagC, true);
}
```

### 4. Instrukcje Arytmetyczne (ADC, SBC)
**Wspólne cechy:**
- Wykonują operacje arytmetyczne z uwzględnieniem flagi Carry
- Ustawiają flagi C, V, N, Z

**ADC:**
```csharp
private void Adc(byte value)
{
    int result = A + value + (GetFlag(FlagC) ? 1 : 0);
    
    // Carry
    SetFlag(FlagC, result > 0xFF);
    
    // Overflow
    bool signA = (A & 0x80) != 0;
    bool signValue = (value & 0x80) != 0;
    bool signResult = (result & 0x80) != 0;
    SetFlag(FlagV, signA == signValue && signA != signResult);
    
    // Ustaw A i flagi N, Z
    A = (byte)result;
    SetNZ(A);
}
```

**SBC:**
```csharp
private void Sbc(byte value)
{
    int result = A - value - (GetFlag(FlagC) ? 0 : 1);
    
    // Carry (borrow)
    SetFlag(FlagC, result >= 0);
    
    // Overflow
    bool signA = (A & 0x80) != 0;
    bool signValue = (value & 0x80) != 0;
    bool signResult = (result & 0x80) != 0;
    SetFlag(FlagV, signA != signValue && signA != signResult);
    
    // Ustaw A i flagi N, Z
    A = (byte)result;
    SetNZ(A);
}
```

### 5. Instrukcje Logiczne (AND, OR, EOR)
**Wspólne cechy:**
- Wykonują operacje bitowe
- Ustawiają flagi N i Z

**Przykład AND:**
```csharp
private void And(byte value)
{
    A &= value;
    SetNZ(A);
}
```

### 6. Instrukcje Porównania (CMP, CPX, CPY)
**Wspólne cechy:**
- Porównują rejestr z wartością
- Ustawiają flagi C, N, Z

**Przykład CMP:**
```csharp
private void Cmp(byte value)
{
    int result = A - value;
    
    // Carry (A >= value)
    SetFlag(FlagC, result >= 0);
    
    // Ustaw flagi N, Z
    SetNZ((byte)result);
}
```

### 7. Instrukcje Inkrementacji/Dekrementacji (INC, DEC, INX, DEX, INY, DEY)
**Wspólne cechy:**
- Zwiększają lub zmniejszają wartość o 1
- Ustawiają flagi N i Z

**Przykład INC:**
```csharp
public void IncZp()
{
    byte address = FetchZeroPageAddress();
    byte value = _memoryBus.Read(address);
    value++;
    _memoryBus.Write(address, value);
    SetNZ(value);
}
```

### 8. Instrukcje Shift/Rotate (ASL, LSR, ROL, ROR)
**Wspólne cechy:**
- Przesuwają lub obracają bity
- Ustawiają flagi C, N, Z

**Przykład ASL:**
```csharp
private void Asl(ref byte value)
{
    // Przesuń w lewo
    SetFlag(FlagC, (value & 0x80) != 0);
    value <<= 1;
    SetNZ(value);
}

public void AslZp()
{
    byte address = FetchZeroPageAddress();
    byte value = _memoryBus.Read(address);
    Asl(ref value);
    _memoryBus.Write(address, value);
}
```

### 9. Instrukcje Skoku (JMP, JSR, RTS, RTI)
**Wspólne cechy:**
- Zmieniają przepływ wykonania
- Różne liczby cykli

**Przykład JMP Absolute:**
```csharp
public void JmpAbs()
{
    ushort address = FetchAbsoluteAddress();
    PC = address;
}
```

### 10. Instrukcje Stosu (PHA, PHP, PLA, PLP)
**Wspólne cechy:**
- Operują na stosie
- Ustawiają flagi (oprócz PHA, PHP)

**Przykład PHA:**
```csharp
public void Pha()
{
    _memoryBus.Write((ushort)(0x0100 + SP), A);
    SP--;
}
```

---

## 🚫 Zakazane praktyki

1. **Niedokładne implementacje** - Każda instrukcja **MUSI** być zgodna ze specyfikacją
2. **Błędny timing** - Liczba cykli **MUSI** być poprawna
3. **Błędne flagi** - Wpływ na flagi **MUSI** być dokładny
4. **Brak obsługi trybów adresowania** - Wszystkie tryby **MUSZĄ** być zaimplementowane
5. **Duplikacja kodu** - Używaj metod pomocniczych
6. **Magiczne liczby** - Używaj stałych dla opcode'ów i flag
7. **Brak komentarzy** - Każda publiczna metoda **MUSI** mieć komentarz XML

---

## 🔍 Weryfikacja implementacji

Przed commitowaniem:
1. Sprawdź, czy instrukcja jest zaimplementowana zgodnie ze specyfikacją
2. Sprawdź, czy timing jest poprawny
3. Sprawdź, czy flagi są ustawiane poprawnie
4. Uruchom testy jednostkowe
5. Uruchom `dotnet build` - **0 ostrzeżeń**

---

## 📌 Źródła referencyjne

1. **Official 6502 documentation**
2. **nestest** (NES test ROM)
3. **Klaus Dormann Functional Test Suite**
4. **Wolfgang Lorenz Test Suite**
5. **perfect6502**
6. **Visual6502.org**

---

## 📌 Podsumowanie

| Zasada | Wymaganie |
|--------|------------|
| **Dokładność** | Zgodność ze specyfikacją MOS 6502 |
| **Timing** | Poprawna liczba cykli |
| **Flagi** | Poprawny wpływ na flagi |
| **Tryby adresowania** | Wszystkie tryby zaimplementowane |
| **Komentarze** | XML dla publicznych metod |
| **Testy** | 100% pokrycie |
| **Page crossing** | Dodatkowy cykl przy przekroczeniu strony |
