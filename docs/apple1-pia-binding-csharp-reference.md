# Apple-1 PIA Terminal Binding — Referencyjna Implementacja C#

> **Status**: Poprawiona wersja z obsługą flag statusowych (CRA.7 / CRB.7)
> **Zależności**: `ITerminalLink`, `IPiaPortBinding`, `IMemoryMappedDevice`, `IResettableDevice`, `ICpuSignalSource`
> **Kompatybilność**: WOZ Monitor, Apple-1, PET-like
> **Faza**: 28 (MOS 6820/6821 PIA medium implementation)

---

## 📋 Spis treści

1. [Interfejsy bazowe](#1-interfejsy-bazowe)
2. [Apple1TerminalBinding](#2-apple1terminalbinding)
3. [Mos682xPiaDevice](#3-mos682xpiadevice)
4. [Fabryka i inicjalizacja WOZ](#4-fabryka-i-inicjalizacja-woz)
5. [Klasy pomocnicze testowe](#5-klasy-pomocnicze-testowe)

---

## 1. Interfejsy bazowe

### 1.1 `ITerminalLink`
Neutralny interfejs dla połączenia z terminalem (Faza 27).

```csharp
public interface ITerminalLink
{
    bool HasInput { get; }
    bool TryReadByte(out byte value);
    void WriteByte(byte value);
}
```

### 1.2 `IPiaPortBinding` (rozszerzony)
Interfejs bindingu portu PIA z obsługą flag statusowych.

```csharp
public interface IPiaPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte directionMask);
    
    // Nowe właściwości dla flag statusowych
    bool HasInputReady { get; }      // → CRA.7 = 1 (znak gotowy)
    bool IsOutputReady { get; }      // → CRB.7 = 0 (gotowy na zapis)
}
```

---

## 2. `Apple1TerminalBinding`

Binding terminalowy dla Apple-1 PIA.

```csharp
/// <summary>
/// Binding terminalowy dla Apple-1 PIA.
/// 
/// Obsługuje:
/// - Port A (KBD): bit 7 = 1 gdy znak dostępny
/// - Port B (DSP): bity 0-6 do terminala, bit 7 ignorowany
/// - CRA.7: 1 = znak gotowy do odczytu (WOZ sprawdza LDA KBDCR / BPL)
/// - CRB.7: 0 = terminal gotowy na zapis (WOZ sprawdza BIT DSP / BPL)
/// </summary>
public sealed class Apple1TerminalBinding : IPiaPortBinding
{
    private readonly ITerminalLink _terminal;
    
    public Apple1TerminalBinding(ITerminalLink terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }
    
    public bool HasInputReady => _terminal.HasInput;
    
    public bool IsOutputReady => true; // Terminal buforowany — zawsze gotowy
    
    public byte ReadPins()
    {
        if (_terminal.HasInput && _terminal.TryReadByte(out byte value))
        {
            // WOZ Monitor oczekuje: bit 7 = 1, bity 0-6 = ASCII
            return (byte)(value | 0x80);
        }
        return 0;
    }
    
    public void WritePins(byte value, byte directionMask)
    {
        // Tylko bity 0-6 są output (DDRB = 0x7F w Apple-1)
        byte outputValue = (byte)(value & 0x7F);
        
        if (outputValue != 0 || directionMask != 0)
        {
            _terminal.WriteByte(outputValue);
        }
        // Bit 7 = input — ignorowany
    }
}
```

---

## 3. `Mos682xPiaDevice`

Generyczne urządzenie PIA (MOS 6820/6821) z obsługą flag statusowych.

```csharp
/// <summary>
/// Generyczne urządzenie PIA (MOS 6820/6821).
/// 
/// Adresacja:
/// - base+0: ORA lub DDRA (zależy od CRA.2)
/// - base+1: CRA
/// - base+2: ORB lub DDRB (zależy od CRB.2)
/// - base+3: CRB
/// 
/// Mieszanie odczytu: (outputLatch & DDR) | (externalInput & ~DDR)
/// </summary>
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    public uint StartAddress { get; }
    public uint Size => 4;
    
    // Rejestry PIA
    private byte _ddra;
    private byte _ddrb;
    private byte _cra;
    private byte _crb;
    private byte _outputLatchA; // ORA
    private byte _outputLatchB; // ORB
    
    private readonly IPiaPortBinding _portABinding;
    private readonly IPiaPortBinding _portBBinding;
    
    public Mos682xPiaDevice(uint baseAddress, IPiaPortBinding portA, IPiaPortBinding portB)
    {
        StartAddress = baseAddress;
        _portABinding = portA ?? throw new ArgumentNullException(nameof(portA));
        _portBBinding = portB ?? throw new ArgumentNullException(nameof(portB));
    }
    
    public byte ReadMemory(uint address)
    {
        var offset = address - StartAddress;
        return offset switch
        {
            0 => ReadPortDataA(),
            1 => ReadControlRegisterA(),
            2 => ReadPortDataB(),
            3 => ReadControlRegisterB(),
            _ => throw new InvalidOperationException()
        };
    }
    
    private byte ReadPortDataA()
    {
        // CRA.2 decyduje: 0=DDRA, 1=ORA
        if ((_cra & 0x04) == 0)
            return _ddra;
        else
            return (byte)((_outputLatchA & _ddra) | (_portABinding.ReadPins() & ~_ddra));
    }
    
    private byte ReadControlRegisterA()
    {
        byte cra = _cra;
        
        // Ustaw bit 7 jeśli binding portu A ma znak gotowy
        if (_portABinding.HasInputReady)
            cra |= 0x80;
        else
            cra &= 0x7F;
        
        return cra;
    }
    
    private byte ReadPortDataB()
    {
        // CRB.2 decyduje: 0=DDRB, 1=ORB
        if ((_crb & 0x04) == 0)
            return _ddrb;
        else
            return (byte)((_outputLatchB & _ddrb) | (_portBBinding.ReadPins() & ~_ddrb));
    }
    
    private byte ReadControlRegisterB()
    {
        byte crb = _crb;
        
        // WOZ Monitor: BIT DSP / BPL → czeka na ORB.7 = 0 (gotowy)
        // Dlatego IsOutputReady = true → CRB.7 = 0
        if (_portBBinding.IsOutputReady)
            crb &= 0x7F; // Wyczyść bit 7 (gotowy)
        else
            crb |= 0x80; // Ustaw bit 7 (zajęty)
        
        return crb;
    }
    
    public void WriteMemory(uint address, byte value)
    {
        var offset = address - StartAddress;
        switch (offset)
        {
            case 0:
                if ((_cra & 0x04) == 0) _ddra = value; // CRA.2=0 → DDRA
                else _outputLatchA = value;            // CRA.2=1 → ORA
                break;
            case 1: _cra = value; break;
            case 2:
                if ((_crb & 0x04) == 0) _ddrb = value; // CRB.2=0 → DDRB
                else _outputLatchB = value;            // CRB.2=1 → ORB
                break;
            case 3: _crb = value; break;
        }
    }
    
    public void Reset()
    {
        _ddra = _ddrb = 0;
        _cra = _crb = 0;
        _outputLatchA = _outputLatchB = 0;
    }
    
    // ICpuSignalSource - minimalna obsługa IRQ
    public bool IsAsserted(CpuSignal signal)
    {
        if (signal == CpuSignal.Irq)
        {
            bool irqA = (_cra & 0x01) != 0; // IRQA1
            bool irqB = (_crb & 0x01) != 0; // IRQB1
            return irqA || irqB;
        }
        return false;
    }
}
```

---

## 4. Fabryka i inicjalizacja WOZ

### 4.1 `Mos682xPiaDeviceFactory`

```csharp
public static class Mos682xPiaDeviceFactory
{
    /// <summary>
    /// Tworzy PIA skonfigurowane dla Apple-1 Terminal.
    /// 
    /// Inicjalizacja zgodnie z WOZ Monitor:
    /// 1. Ustaw CRB.2 = 0 (DDRB na offset 2)
    /// 2. Zapis DDRB = $7F (Port B: bity 0-6 = output, bit 7 = input)
    /// 3. Ustaw CRB = $A7 (CRB.2 = 1 → ORB na offset 2)
    /// 4. Ustaw CRA = $A7 (CRA.2 = 1 → ORA na offset 0)
    /// </summary>
    public static Mos682xPiaDevice CreateApple1Terminal(uint baseAddress, ITerminalLink terminal)
    {
        var portABinding = new Apple1TerminalBinding(terminal); // KBD (Port A)
        var portBBinding = new Apple1TerminalBinding(terminal); // DSP (Port B)
        
        var device = new Mos682xPiaDevice(baseAddress, portABinding, portBBinding);
        
        // Krok 1: CRB.2 = 0 (DDRB na offset 2)
        device.WriteMemory(baseAddress + 3, 0x00);
        
        // Krok 2: DDRB = $7F
        device.WriteMemory(baseAddress + 2, 0x7F);
        
        // Krok 3: CRB = $A7 (CRB.2 = 1 → ORB na offset 2)
        device.WriteMemory(baseAddress + 3, 0xA7);
        
        // Krok 4: CRA = $A7 (CRA.2 = 1 → ORA na offset 0)
        device.WriteMemory(baseAddress + 1, 0xA7);
        
        return device;
    }
}
```

### 4.2 Inicjalizacja WOZ Monitor (referencja)

```assembly
; WOZ Monitor initialization
RESET:
    CLD
    CLI
    LDY #$7F
    STY DSP           ; Store $7F to $D012 (DDRB when CRB.2=0, then ORB when CRB.2=1)
    LDA #$A7
    STA KBDCR         ; Store $A7 to $D011 (CRA)
    STA DSPCR         ; Store $A7 to $D013 (CRB)
```

---

## 5. Klasy pomocnicze testowe

### 5.1 `NullPiaPortBinding`

```csharp
/// <summary>Pusty binding do testów (zawsze zwraca 0).</summary>
public sealed class NullPiaPortBinding : IPiaPortBinding
{
    public bool HasInputReady => false;
    public bool IsOutputReady => true;
    public byte ReadPins() => 0;
    public void WritePins(byte value, byte directionMask) { }
}
```

### 5.2 `MockPiaPortBinding`

```csharp
/// <summary>Mock binding z konfigurowalnym wejściem.</summary>
public sealed class MockPiaPortBinding : IPiaPortBinding
{
    public bool HasInputReady => false;
    public bool IsOutputReady => true;
    public byte ExternalInput { get; set; }
    
    public byte ReadPins() => ExternalInput;
    public void WritePins(byte value, byte directionMask) { }
}
```

### 5.3 `BufferedTerminalLink` (przykład)

```csharp
/// <summary>
/// Buforowany terminal do testów jednostkowych.
/// </summary>
public sealed class BufferedTerminalLink : ITerminalLink
{
    private readonly Queue<byte> _inputBuffer = new Queue<byte>();
    private readonly List<byte> _outputBuffer = new List<byte>();
    
    public bool HasInput => _inputBuffer.Count > 0;
    
    public void EnqueueInput(byte value)
    {
        _inputBuffer.Enqueue(value);
    }
    
    public void EnqueueText(string text, TerminalTextEncoding encoding)
    {
        foreach (char c in text)
        {
            byte b = encoding switch
            {
                TerminalTextEncoding.RawBytes => (byte)c,
                TerminalTextEncoding.AsciiUppercase => (byte)char.ToUpper(c),
                TerminalTextEncoding.Apple1 => (byte)(char.ToUpper(c) | 0x80),
                _ => (byte)c
            };
            _inputBuffer.Enqueue(b);
        }
    }
    
    public bool TryReadByte(out byte value)
    {
        if (_inputBuffer.Count > 0)
        {
            value = _inputBuffer.Dequeue();
            return true;
        }
        value = 0;
        return false;
    }
    
    public void WriteByte(byte value)
    {
        _outputBuffer.Add(value);
    }
    
    public byte[] ReadAllOutputBytes()
    {
        var result = _outputBuffer.ToArray();
        _outputBuffer.Clear();
        return result;
    }
    
    public byte LastWrittenByte => _outputBuffer.Count > 0 ? _outputBuffer[^1] : (byte)0;
}

public enum TerminalTextEncoding
{
    RawBytes,
    AsciiUppercase,
    Apple1
}
```

---

## 6. Podsumowanie zmian vs oryginał

| Element | Oryginał (informacje.md) | Poprawka | Powód |
|---|---|---|---|
| `IPiaPortBinding` | `ReadPins()`, `WritePins()` | + `HasInputReady`, `IsOutputReady` | Obsługa flag CRA.7/CRB.7 |
| `Apple1TerminalBinding` | Zwraca `value | 0x80` | ✅ OK | Poprawne |
| `Mos682xPiaDevice` | Brak obsługi flag | + `ReadControlRegisterA/B()` | Ustawia CRA.7/CRB.7 z bindingów |
| WOZ Monitor | Sprawdza KBDCR.7 | ✅ Potwierdzone | `LDA KBDCR / BPL` |
| DSP.7 | Niezdefiniowane | **0 = gotowy** | `BIT DSP / BPL` (BPL = bit 7 = 0) |

---

## 7. Zgodność z WOZ Monitor

### 7.1 Pętla wejścia (NEXTCHAR)
```assembly
NEXTCHAR:
    LDA KBDCR     ; $D011 (CRA)
    BPL NEXTCHAR  ; Pętla jeśli bit 7 = 0
    LDA KBD       ; $D010 (ORA)
    ...
```
**Wymaganie**: CRA.7 = 1 gdy znak gotowy → `HasInputReady = true`

### 7.2 Pętla wyjścia (ECHO)
```assembly
ECHO:
    BIT DSP       ; $D012 (ORB)
    BPL ECHO      ; Pętla jeśli bit 7 = 0
    STA DSP       ; Zapis znaku
    ...
```
**Wymaganie**: ORB.7 = 0 (gotowy) → `IsOutputReady = true` → CRB.7 = 0

---

## 8. Historia zmian

| Wersja | Data | Autor | Zmiany |
|---|---|---|---|
| 1.0 | 2025-XX-XX | Mistral Vibe | Utworzenie referencyjnej implementacji z obsługą flag CRA.7/CRB.7 |

---

## 9. Zobacz także

- [apple-1-pia-terminal-plan.md](../apple-1-pia-terminal-plan.md) — Plan implementacji z suplementem
- [faza-28-mos682x-pia-medium.md](../faza-28-mos682x-pia-medium.md) — Specyfikacja fazy
- [informacje.md](../informacje.md) — Oryginalne podsumowanie
