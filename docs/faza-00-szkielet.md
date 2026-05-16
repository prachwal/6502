# Faza 0 — Szkielet projektu i struktura .NET

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 5% (rozdział 13: Wymagania i struktura projektu .NET) |
| **Pokrycie całości** | 1% |
| **Zależności** | Brak |
| **Szacowany czas** | 2–4h |

---

## Cel fazy

Stworzenie struktury projektu .NET, interfejsu pamięci, szkieletu klasy procesora oraz podstawowej maszyny stanów `Tick()`. Po tej fazie projekt się kompiluje, można utworzyć instancję procesora i wywołać `Tick()`, ale procesor nie wykonuje jeszcze żadnych instrukcji — zwraca wyjątek lub NOP.

---

## Co implementujemy

### 1. Struktura rozwiązania .NET

```
6502Emulator/
├── 6502Emulator.sln
├── src/
│   └── Cpu6502/
│       ├── Cpu6502.csproj
│       ├── Cpu6502.cs
│       ├── IMemoryBus.cs
│       └── CpuState.cs
└── tests/
    └── Cpu6502.Tests/
        ├── Cpu6502.Tests.csproj
        └── SkeletonTests.cs
```

### 2. Interfejs `IMemoryBus`

```csharp
public interface IMemoryBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
```

Procesor nie alokuje własnej pamięci. Otrzymuje `IMemoryBus` przez konstruktor (Dependency Injection). Pozwala to na mapowanie I/O, bank switching, mirroring adresów po stronie systemu.

### 3. Klasa `CpuState` (opcjonalnie jako osobny plik lub wewnętrzna struktura)

Pola stanu procesora:

| Pole | Typ | Opis |
|------|-----|------|
| `A` | `byte` | Accumulator |
| `X` | `byte` | X register |
| `Y` | `byte` | Y register |
| `PC` | `ushort` | Program Counter (16-bit) |
| `SP` | `byte` | Stack Pointer |
| `P` | `byte` | Processor Status Register (flags) |

### 4. Klasa `Cpu6502`

Pola:

```csharp
public class Cpu6502
{
    // Rejestry
    private byte A;
    private byte X;
    private byte Y;
    private ushort PC;
    private byte SP;
    private byte P;

    // Stan wewnętrzny
    private byte IR;          // Instruction Register = (opcode << 3) | cycleCounter
    private bool Sync;        // true = rozpoczęcie nowej instrukcji
    private ulong Cycle;      // licznik cykli

    // Zależności
    private readonly IMemoryBus memory;

    // Konstruktor
    public Cpu6502(IMemoryBus memoryBus);
}
```

### 5. Metody pomocnicze dla flag

```csharp
// Stałe bitowe dla rejestru P
private const byte FlagC = 0x01;  // Carry
private const byte FlagZ = 0x02;  // Zero
private const byte FlagI = 0x04;  // Interrupt Disable
private const byte FlagD = 0x08;  // Decimal
private const byte FlagB = 0x10;  // Break (tylko na stosie)
private const byte FlagU = 0x20;  // Unused (zawsze 1 na stosie)
private const byte FlagV = 0x40;  // Overflow
private const byte FlagN = 0x80;  // Negative

// Property / metody
bool GetFlag(byte flag) => (P & flag) != 0;
void SetFlag(byte flag, bool value) { if (value) P |= flag; else P &= (byte)~flag; }
```

### 6. Metoda `Tick()` — wersja szkieletowa

```csharp
public void Tick()
{
    // 1. Jeśli SYNC — pobierz opcode, zainicjuj IR
    if (Sync)
    {
        byte opcode = memory.Read(PC);
        IR = (byte)(opcode << 3);  // opcode przesunięty o 3, cycleCounter = 0
        Sync = false;
    }

    // 2. Wykonaj jeden cykl bieżącej instrukcji
    //    Na razie: NotImplementedException lub pułapka debug
    throw new NotImplementedException($"Opcode not implemented: ${(IR >> 3):X2}");

    // 3. Zwiększ licznik cykli
    // IR++;  // (to będzie w pełnej wersji)
    // Cycle++;
}
```

### 7. Metoda `Reset()`

Ustawia stan procesora jak po resecie sprzętowym:

```csharp
public void Reset(ushort resetVectorAddress = 0xFFFC)
{
    A = 0;
    X = 0;
    Y = 0;
    SP = 0xFD;          // po 3 pseudo-pushach
    P = FlagI | FlagU;  // I=1, unused=1
    // Pobranie wektora RESET
    byte lo = memory.Read(resetVectorAddress);
    byte hi = memory.Read((ushort)(resetVectorAddress + 1));
    PC = (ushort)(hi << 8 | lo);
    Sync = true;
    Cycle = 0;
}
```

### 8. Prosta implementacja pamięci na potrzeby testów

```csharp
// W projekcie testowym
public class FlatMemory : IMemoryBus
{
    private readonly byte[] ram = new byte[65536];

    public byte Read(ushort address) => ram[address];
    public void Write(ushort address, byte value) => ram[address] = value;

    // Metoda pomocnicza do ładowania ROM-u
    public void LoadRom(ushort startAddress, byte[] data)
    {
        Array.Copy(data, 0, ram, startAddress, data.Length);
    }
}
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **Utworzenie procesora** | `new Cpu6502(new FlatMemory())` nie rzuca wyjątku |
| **Reset ustawia rejestry** | Po `Reset()`: SP=$FD, I=1, PC=wartość z wektora |
| **Odczyt wektora RESET** | Ustaw $FFFC=$00, $FFFD=$C0 → PC=$C000 |
| **Tick rzuca wyjątek** | Przed implementacją instrukcji, `Tick()` rzuca `NotImplementedException` |
| **Odczyt/zapis przez FlatMemory** | Test podstawowego dostępu do pamięci |

### Test jednostkowy — przykład

```csharp
[Test]
public void Reset_LoadsVectorAndSetsSP()
{
    var mem = new FlatMemory();
    mem.Write(0xFFFC, 0x00);
    mem.Write(0xFFFD, 0xC0);
    var cpu = new Cpu6502(mem);

    cpu.Reset();

    Assert.AreEqual(0xC000, cpu.PC);
    Assert.AreEqual(0xFD, cpu.SP);
    Assert.IsTrue(cpu.GetFlag(FlagI));
}
```

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 13.1 | Struktura rozwiązania |
| 13.2 | Interfejs IMemoryBus |
| 13.3 | Klasa Cpu6502 — pola |
| 13.4 | Metoda Tick() — szkielet |
| 2.1 | Rejestry (A, X, Y, PC, SP, P) |
| 2.4 | Stos (adres bazowy $0100) |
| 2.8 | Inicjalizacja i reset |

---

## Definicja zakończenia (Definition of Done)

- [ ] Rozwiązanie .NET się kompiluje
- [ ] Projekt `Cpu6502` i `Cpu6502.Tests` istnieją
- [ ] Interfejs `IMemoryBus` zdefiniowany
- [ ] Klasa `Cpu6502` ma wszystkie pola rejestrowe i stan wewnętrzny
- [ ] `Reset()` poprawnie ustawia SP=$FD, I=1, PC z wektora $FFFC
- [ ] `FlatMemory` przechodzi test odczytu/zapisu
- [ ] Wszystkie testy jednostkowe zielone
- [ ] Kod nie zawiera ostrzeżeń kompilatora

---

## Pliki do utworzenia / modyfikacji

| Plik | Akcja |
|------|-------|
| `6502Emulator.sln` | Utwórz |
| `src/Cpu6502/Cpu6502.csproj` | Utwórz |
| `src/Cpu6502/IMemoryBus.cs` | Utwórz |
| `src/Cpu6502/Cpu6502.cs` | Utwórz |
| `tests/Cpu6502.Tests/Cpu6502.Tests.csproj` | Utwórz |
| `tests/Cpu6502.Tests/FlatMemory.cs` | Utwórz |
| `tests/Cpu6502.Tests/SkeletonTests.cs` | Utwórz |
