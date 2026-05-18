# Faza 27 - Implementacja: Abstrakcje terminala

| Właściwość | Wartość |
|------------|---------|
| **Status** | ✅ Zaimplementowana |
| **Data implementacji** | 2025-05-18 |
| **Zależności** | Żadne (niezależna od faz 24-26) |
| **Liczba testów** | 31 |
| **Liczba plików** | 5 |

---

## Podsumowanie

Faza 27 zaimplementowała **neutralne abstrakcje terminala**, które umożliwiają:
- Testowanie urządzeń I/O bez realnego frontendu
- Obsługę różnych encodingów tekstowych (Raw, ASCII Uppercase, Apple-1)
- Ponowne użycie tego samego `ITerminalLink` przez różne urządzenia (PIA, UART, ACIA)

---

## Zaimplementowane komponenty

### 1. Interfejsy (src/Cpu6502/System/Terminal/)

| Plik | Opis | Wiersze kodu |
|------|------|--------------|
| `IByteInput.cs` | Interfejs źródła bajtów wejściowych | 19 |
| `IByteOutput.cs` | Interfejs celu bajtów wyjściowych | 13 |
| `ITerminalLink.cs` | Interfejs łącza terminala (IByteInput + IByteOutput) | 13 |

#### IByteInput
```csharp
public interface IByteInput
{
    bool HasInput { get; }
    bool TryReadByte(out byte value);
}
```

#### IByteOutput
```csharp
public interface IByteOutput
{
    void WriteByte(byte value);
}
```

#### ITerminalLink
```csharp
public interface ITerminalLink : IByteInput, IByteOutput
{
}
```

### 2. Encoding tekstu (src/Cpu6502/System/Terminal/TerminalTextEncoding.cs)

Enum `TerminalTextEncoding` z metodami rozszerzającymi:
- `Encode(this TerminalTextEncoding, string text)` - kodowanie tekstu do bajtów
- `Decode(this TerminalTextEncoding, byte[] bytes)` - dekodowanie bajtów do tekstu

| Tryb | Opis | Zachowanie |
|------|------|------------|
| `RawBytes` | Surowy tryb bajtowy | Bez transformacji, ASCII 1:1 |
| `AsciiUppercase` | Tekst uppercase | Konwertuje litery na uppercase (ASCII) |
| `Apple1` | Kompatybilny z Apple-1 | Uppercase + CR jako koniec linii |

**Uwaga**: Encoding `Apple1` **nie ustawia bitu 7** - to odpowiedzialność bindingu PIA (Faza 28).

### 3. Implementacja buforowana (src/Cpu6502/System/Terminal/BufferedTerminalLink.cs)

Klasa `BufferedTerminalLink` implementuje `ITerminalLink` z:
- FIFO bufor wejścia (`Queue<byte>`)
- Lista bufora wyjścia (`List<byte>`)
- Metody zarządzania buforami

| Metoda | Opis |
|--------|------|
| `HasInput` | Czy dostępne wejście |
| `TryReadByte(out byte)` | Odczyt bajtu z wejścia (FIFO) |
| `WriteByte(byte)` | Zapis bajtu do wyjścia |
| `EnqueueInput(byte)` | Dodaj bajt do bufora wejścia |
| `EnqueueText(string, encoding)` | Dodaj tekst do bufora wejścia |
| `ReadAllOutputBytes()` | Odczyt i wyczyszczenie bufora wyjścia (bajty) |
| `ReadOutputText(encoding)` | Odczyt i wyczyszczenie bufora wyjścia (tekst) |
| `InputBufferSize` | Rozmiar bufora wejścia (dla testów) |
| `OutputBufferSize` | Rozmiar bufora wyjścia (dla testów) |
| `Clear()` | Wyczyszczenie obu buforów |

---

## Testy jednostkowe

Plik: `tests/Cpu6502.Tests/System/Faza27TerminalTests.cs`

| Kategoria | Liczba testów | Opis |
|-----------|---------------|------|
| HasInput | 3 | Testy stanu bufora wejścia |
| FIFO | 2 | Testy kolejności FIFO |
| Output | 3 | Testy bufora wyjścia |
| RawBytes encoding | 2 | Testy encodingu surowych bajtów |
| AsciiUppercase encoding | 5 | Testy konwersji uppercase |
| Apple1 encoding | 3 | Testy encodingu Apple-1 |
| EnqueueText | 3 | Testy wstawiania tekstu |
| Clear | 1 | Test czyszczenia buforów |
| Edge Cases | 5 | Testy brzegowe (null, empty) |
| Buffer Size | 2 | Testy rozmiaru buforów |
| Interface | 3 | Testy implementacji interfejsów |
| **Razem** | **31** | **100% pokrycie wymagań** |

Wszystkie testy przechodzą pomyślnie.

---

## Decyzje implementacyjne

### 1. Niezależność od frontendu
`ITerminalLink` nie zależy od:
- `Console` (TUI)
- WPF
- Avalonia
- Blazor
- Żadnej konkretnej technologii UI

### 2. FIFO (First-In-First-Out)
Bufor wejścia używa `Queue<byte>` - gwarantuje poprawną kolejność odczytu.

### 3. Thread-safety
- Bezpieczny dla **single reader / single writer**
- **Nie** obsługuje concurrent access (nie jest wymagane dla emulacji)
- Można rozszerzyć w przyszłości przy użyciu `ConcurrentQueue`

### 4. Encoding jako metody rozszerzające
Czystsza składnia:
```csharp
var bytes = TerminalTextEncoding.Apple1.Encode("HELLO");
var text = TerminalTextEncoding.Apple1.Decode(bytes);
```

### 5. Bit 7 nie jest obsługiwany w encodingu
- Encoding `Apple1` **nie** ustawia bitu 7
- To jest odpowiedzialność **bindingu PIA** (Faza 28)
- Terminal link przechowuje tylko bajty

### 6. Wyczyszczenie bufora
- `ReadAllOutputBytes()` i `ReadOutputText()` **czyścią** bufor wyjścia
- `Clear()` czyści **oba** bufory
- `TryReadByte()` czyści **tylko jeden** bajt

---

## Integracja z przyszłymi fazami

Faza 27 jest **wymagana** dla:

### Faza 28: MOS 6820/6821 PIA
```csharp
// Apple1TerminalBinding używa ITerminalLink
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    
    public byte ReadPins()
    {
        if (_terminal.HasInput && _terminal.TryReadByte(out byte value))
        {
            // Ustaw bit 7 = 1 (znak gotowy) - TO JEST ODPOWIEDZIALNOŚĆ BINDINGU
            return (byte)(value | 0x80);
        }
        return 0;
    }
    
    public void WritePins(byte value, byte directionMask)
    {
        // Pisze bity 0-6, ignoruje bit 7
        _terminal.WriteByte((byte)(value & 0x7F));
    }
}
```

### Faza 29: Profil Apple-1
```json
{
  "id": "terminal0",
  "type": "buffered-terminal"
}
```

### Faza 30: PET bindings
- Używa `ITerminalLink` dla matrycy klawiatury
- Ten sam interfejs co Apple-1

---

## Zmiany w plikach

### Nowe pliki
| Plik | Rozmiar | Typ |
|------|--------|-----|
| `src/Cpu6502/System/Terminal/IByteInput.cs` | 714 B | Interfejs |
| `src/Cpu6502/System/Terminal/IByteOutput.cs` | 383 B | Interfejs |
| `src/Cpu6502/System/Terminal/ITerminalLink.cs` | 421 B | Interfejs |
| `src/Cpu6502/System/Terminal/TerminalTextEncoding.cs` | 3368 B | Enum + metody |
| `src/Cpu6502/System/Terminal/BufferedTerminalLink.cs` | 3468 B | Klasa |
| `tests/Cpu6502.Tests/System/Faza27TerminalTests.cs` | 10438 B | Testy |

### Zmodyfikowane pliki
| Plik | Zmiana |
|------|--------|
| `docs/faza-27-terminal-abstractions.md` | Zaktualizowano status na ✅ Zaimplementowana |

### Nowe dokumenty
| Plik | Opis |
|------|------|
| `docs/faza-27-implementacja.md` | Dokumentacja implementacji |

---

## Weryfikacja

```bash
# Kompilacja
dotnet build

# Testy Fazy 27
dotnet test --filter "Faza27TerminalTests"

# Wynik: Passed! - Failed: 0, Passed: 31, Skipped: 0, Total: 31
```

---

## Kryteria akceptacji

- [x] Terminal jest testowalny bez realnego frontendu
- [x] Urządzenia I/O mogą korzystać z tego samego `ITerminalLink`
- [x] Kod nie zależy od `Console`, WPF, Avalonia ani Blazor
- [x] Wszystkie wymagane testy przechodzą (31 testów)
- [x] Dokumentacja XML na wszystkich publicznych członkach
- [x] Kod follows konwencji projektowych (PascalCase, itd.)
- [x] Faza jest niezależna od innych faz (24-26)

---

## Kolejne kroki

Po Fazie 27, następna jest:
- **Faza 28**: MOS 6820/6821 PIA medium implementation
  - `Mos682xPiaDevice` z `IPiaPortBinding`
  - `Apple1TerminalBinding` używający `ITerminalLink`
  - Preset `apple-1-terminal`
  - Drugi binding PET-like (walidacyjny)
