# Faza 27 - Terminal, wejscie tekstowe i adaptery hosta

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | ✅ Zaimplementowana |
| **Data implementacji** | 2025-05-18 |
| **Zakres** | Frontend tekstowy i link bajtowy |
| **Zaleznosci** | Żadne (niezależna) |
| **Cel projektowy** | Jeden terminal dla Apple-1, UART/ACIA, SBC i testow |
| **Liczba testów** | 31 |

---

## Cel fazy

Dodac neutralny model terminala/linku bajtowego, ktory moga wykorzystac rozne urzadzenia:

- Apple-1 PIA terminal,
- `UartSimpleDevice`,
- MOS 6551 ACIA,
- Motorola 6850 ACIA,
- testowe SBC,
- przyszle hosty TUI/WPF/Avalonia/Blazor.

Nie wolno projektowac terminala jako `Console.ReadKey()` w urzadzeniu.

---

## Kontrakty

```csharp
public interface IByteInput
{
    bool HasInput { get; }
    bool TryReadByte(out byte value);
}
```

```csharp
public interface IByteOutput
{
    void WriteByte(byte value);
}
```

```csharp
public interface ITerminalLink : IByteInput, IByteOutput
{
}
```

```csharp
public sealed class BufferedTerminalLink : ITerminalLink
{
    public void EnqueueInput(byte value);
    public void EnqueueText(string text, TerminalTextEncoding encoding);
    public bool TryReadByte(out byte value);
    public void WriteByte(byte value);
    public byte[] ReadAllOutputBytes();
    public string ReadOutputText(TerminalTextEncoding encoding);
}
```

---

## Normalizacja tekstu

Dodac `TerminalTextEncoding` albo podobny model:

| Tryb | Zachowanie |
|------|------------|
| `RawBytes` | bez zmian |
| `AsciiUppercase` | zamienia litery na uppercase |
| `Apple1` | uppercase, CR jako koniec linii, opcjonalne ustawianie/ignorowanie bitu 7 po stronie adaptera |

Urzadzenie PIA odpowiada za bity statusowe Apple-1. Terminal link przechowuje bajty.

---

## Kolejnosc wykonania dla agenta

1. Dodaj interfejsy `IByteInput`, `IByteOutput`, `ITerminalLink`.
2. Dodaj `BufferedTerminalLink`.
3. Dodaj `TerminalTextEncoding` i funkcje konwersji.
4. Dodaj testy bez zadnych urzadzen I/O.
5. Nie dodawaj jeszcze PIA/UART.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `BufferedTerminal_WhenEmpty_HasNoInput` | pusty bufor |
| `BufferedTerminal_EnqueueInput_TryReadDequeuesByte` | FIFO wejscia |
| `BufferedTerminal_WriteByte_AppendsOutput` | zapis wyjscia |
| `BufferedTerminal_ReadAllOutputBytes_ClearsOutput` | odczyt bufora wyjscia |
| `AsciiUppercase_ConvertsLowercase` | normalizacja tekstu |
| `Apple1Encoding_ConvertsLineEndingToCarriageReturn` | CR dla Apple-1 |

---

## Poza zakresem

- Mapowanie rejestrow Apple-1.
- UART/ACIA.
- PIA/VIA.
- UI konsolowe/interaktywne.

---

## Kryteria akceptacji

- Terminal jest testowalny bez realnego frontendu.
- Urzadzenia I/O moga korzystac z tego samego `ITerminalLink`.
- Kod nie zalezy od `Console`, WPF, Avalonia ani Blazor.

