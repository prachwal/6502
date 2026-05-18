# Faza 25 — Implementacja RuntimeBus, CompiledMemoryMap i CompiledPortMap

| Właściwość | Wartość |
|------------|---------|
| **Status** | ✅ **Zaimplementowana** |
| **Data implementacji** | 2025-05-18 |
| **Czas implementacji** | ~3 godziny |
| **Liczba plików** | 13 (kontrakty, implementacje, handlery, testy) |
| **Liczba testów** | 33 |
| **Wszystkie testy** | ✅ **Przeszły** (330/331, 1 skipped) |

---

## 1. Podsumowanie implementacji

Faza 25 została **pełnić zaimplementowana** i przetestowana. Wszystkie kontrakty z dokumentacji (`faza-25-system-bus-memory-map.md`) zostały zrealizowane.

### 1.1 Nowe pliki

| Plik | Typ | Opis |
|------|-----|------|
| `src/Cpu6502/System/ISystemBus.cs` | Interfejsy | `ISystemBus`, `IMemoryMappedDevice`, `IPortMappedDevice` |
| `src/Cpu6502/System/AddressSpaceKind.cs` | Enum | `AddressSpaceKind` (Memory, Port) |
| `src/Cpu6502/System/RomWritePolicy.cs` | Enum | `RomWritePolicy` (Ignore, ThrowException, LogAndIgnore) |
| `src/Cpu6502/System/IBusTracer.cs` | Interfejs | `IBusTracer` + `NullBusTracer` |
| `src/Cpu6502/System/CompiledMemoryMap.cs` | Klasa | Mapa pamięci z 256B stronami |
| `src/Cpu6502/System/CompiledPortMap.cs` | Klasa | Mapa portów I/O |
| `src/Cpu6502/System/RuntimeBus.cs` | Klasa | Główna magistrala (ISystemBus + IMemoryBus) |
| `src/Cpu6502/System/PageHandlers/IPageHandler.cs` | Interfejs | Bazowy interfejs handlera strony |
| `src/Cpu6502/System/PageHandlers/RamPageHandler.cs` | Klasa | Handler strony RAM |
| `src/Cpu6502/System/PageHandlers/RomPageHandler.cs` | Klasa | Handler strony ROM + `RomWriteException` |
| `src/Cpu6502/System/PageHandlers/DevicePageHandler.cs` | Klasa | Handler strony urządzenia |
| `src/Cpu6502/System/PageHandlers/UnmappedPageHandler.cs` | Klasa | Handler strony niezmapowanej |
| `tests/Cpu6502.Tests/System/Faza25BusTests.cs` | Testy | 33 testy jednostkowe |

### 1.2 Zależności od Fazy 24

Faza 25 korzysta z kontraktów z Fazy 24:
- `IDevice` (bazowy interfejs urządzeń)
- `ICpuCore`, `AddressSpaceDescriptor` (wykorzystane w przyszłych fazach)

---

## 2. Zaimplementowane kontrakty

### 2.1 `ISystemBus`

```csharp
public interface ISystemBus
{
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

**Zastosowanie**: Unifikacja dostępu do pamięci i portów I/O dla różnych architektur CPU.

### 2.2 `IMemoryMappedDevice` i `IPortMappedDevice`

```csharp
public interface IMemoryMappedDevice : IDevice
{
    uint StartAddress { get; }
    uint Size { get; }
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);
}

public interface IPortMappedDevice : IDevice
{
    uint StartPort { get; }
    uint Size { get; }
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

**Zastosowanie**: Obsługa urządzeń mapowanych w pamięci (6502) i portach (Z80).

### 2.3 `AddressSpaceKind`

```csharp
public enum AddressSpaceKind
{
    Memory,  // Memory-mapped I/O
    Port     // Port-mapped I/O
}
```

### 2.4 `RomWritePolicy`

```csharp
public enum RomWritePolicy
{
    Ignore,           // Cicho ignoruj zapis
    ThrowException,   // Rzuć wyjątek (domyślne)
    LogAndIgnore      // Zaloguj i zignoruj
}
```

### 2.5 `IBusTracer`

```csharp
public interface IBusTracer
{
    void OnReadMemory(uint address, byte value);
    void OnWriteMemory(uint address, byte value);
    void OnReadPort(uint port, byte value);
    void OnWritePort(uint port, byte value);
}
```

**Implementacja domyślna**: `NullBusTracer` (pusta, zero overhead).

---

## 3. Implementacje

### 3.1 `CompiledMemoryMap`

**Architektura**:
- 256-bajtowe strony (optymalne dla 6502)
- Tablica `IPageHandler[]` dla szybkiego routingu
- Wsparcie dla RAM, ROM, urządzeń memory-mapped

**Metody mapowania**:
```csharp
void MapRam(uint startAddress, uint size, byte fillValue = 0);
void MapRom(uint startAddress, byte[] data, RomWritePolicy writePolicy, string? regionName);
void MapDevice(IMemoryMappedDevice device);
```

**Zalety**:
- **Szybkość**: O(1) dla odczytu/zapisu (brak iteracji po urządzeniach)
- **Elastyczność**: Obsługuje dowolną liczbę bitów adresowych (8-32)
- **Bezpieczeństwo**: Walidacja zakresów, ochrona przed overflow

### 3.2 `CompiledPortMap`

**Architektura**:
- Bezpośrednie mapowanie port → urządzenie
- Tablica `IPortMappedDevice?[]` dla szybkiego dostępu

**Metody**:
```csharp
void MapDevice(IPortMappedDevice device);
byte ReadPort(uint port);  // 0xFF dla niezmapowanego
void WritePort(uint port, byte value);  // Ignorowany dla niezmapowanego
```

### 3.3 `RuntimeBus`

**Integracja**:
- Implementuje zarówno `ISystemBus` jak i `IMemoryBus`
- Łączy `CompiledMemoryMap` i `CompiledPortMap`
- Obsługuje `IBusTracer` dla debugowania

**Dla 6502**:
- Porty I/O są **nieobsługiwane** (6502 używa memory-mapped I/O)
- `ReadPort`/`WritePort` rzucają `NotSupportedException`

**Dla Z80**:
- Możliwe użycie z `portSpaceBits = 8` (256 portów)
- Port-mapped devices działają normalnie

### 3.4 Handlery stron

| Handler | Opis |
|---------|------|
| `RamPageHandler` | RAM z pełnym dostępem R/W |
| `RomPageHandler` | ROM z polityką zapisu |
| `DevicePageHandler` | Delegacja do `IMemoryMappedDevice` |
| `UnmappedPageHandler` | Singleton, zwraca 0xFF / ignoruje zapis |

---

## 4. Decyzje architektoniczne

### 4.1 Rozmiar strony: 256 bajtów

**Uzasadnienie**:
- Optymalny kompromis między **wydajnością** a **dokładnością**
- 64KB pamięci = 256 stron (zrównoważona liczba wpisów)
- Compatybilne z **bank switching** (np. C64, NES)
- Minimalny waste (maksymalnie 255 bajtów na stronę)

### 4.2 `RuntimeBus` implementuje `IMemoryBus`

**Uzasadnienie**:
- Unikanie nadmiernej abstrakcji
- `IMemoryBus` i `ISystemBus` są komplementarne
- Naturalna integracja z istniejącym kodem 6502

### 4.3 `RomWritePolicy.ThrowException` jako domyślne

**Uzasadnienie**:
- Wykrywanie błędów w fazie development
- Świadomy wybór polityki przez programistę
- Możliwe przełączenie na `Ignore` dla emulacji sprzętu

### 4.4 `NotSupportedException` dla portów na 6502

**Uzasadnienie**:
- 6502 **nie ma** oddzielnej przestrzeni portów
- Jasne sygnalizowanie błędu projektowego
- Unikanie ukrytych błędów

### 4.5 `IBusTracer` zamiast callback/delegate

**Uzasadnienie**:
- Modularność i testowalność
- Łatwa wymiana implementacji (Console, File, Serilog)
- Mockowanie w testach jednostkowych
- Zero overhead z `NullBusTracer`

---

## 5. Testy jednostkowe

### 5.1 Lista testów (33 testy)

| Kategoria | Liczba | Opis |
|-----------|--------|------|
| **CompiledMemoryMap** | 12 | Tworzenie, RAM, ROM, urządzenia, walidacja |
| **CompiledPortMap** | 6 | Tworzenie, porty, urządzenia, walidacja |
| **RuntimeBus** | 8 | Integracja, IMemoryBus, ISystemBus, tracing |
| **Page Handlers** | 7 | Ram, Rom, Device, Unmapped |

### 5.2 Ważniejsze testy

```csharp
// RAM działa
[Test] ReadMemory_FromRam_ReturnsWrittenByte()

// ROM jest read-only (domyślnie)
[Test] WriteMemory_ToRom_WithThrowExceptionPolicy_Throws()

// Urządzenia memory-mapped działają
[Test] ReadMemory_FromDevice_RoutesToDevice()

// Porty I/O dla Z80
[Test] ReadPort_FromPortDevice_RoutesToDevice()

// Integracja z IMemoryBus
[Test] RuntimeBus_ReadWriteViaIMemoryBus_Works()

// Tracing działa
[Test] RuntimeBus_Tracer_RecordsReadMemory()
[Test] RuntimeBus_Tracer_RecordsWriteMemory()

// Porty nieobsługiwane dla 6502
[Test] RuntimeBus_ReadPort_For6502WithoutPorts_Throws()
```

### 5.3 Wyniki testów

```
Passed!  - Failed:     0, Passed:    33, Skipped:     0, Total:    33
Duration: 47 ms
```

**Wszystkie testy Fazy 25 przeszły!** ✅

### 5.4 Testy istniejących komponentów

Po implementacji Fazy 25, **wszystkie istniejące testy wciąż przechodzą**:

```
Passed!  - Failed:     0, Passed:   330, Skipped:     1, Total:   331
Duration: 25 s
```

**Wniosek**: Implementacja Fazy 25 **nie zepsuła** istniejących funkcjonalności.

---

## 6. Zgodność z dokumentacją

### 6.1 Porównanie z `faza-25-system-bus-memory-map.md`

| Element z dokumentacji | Status | Uwagi |
|------------------------|--------|-------|
| `ISystemBus` | ✅ | Zaimplementowany |
| `IMemoryMappedDevice` | ✅ | Zaimplementowany |
| `IPortMappedDevice` | ✅ | Zaimplementowany |
| `AddressSpaceKind` | ✅ | Zaimplementowany |
| `CompiledMemoryMap` | ✅ | 256B strony |
| `CompiledPortMap` | ✅ | Zaimplementowany |
| `RamPageHandler` | ✅ | Zaimplementowany |
| `RomPageHandler` | ✅ | Zaimplementowany |
| `DevicePageHandler` | ✅ | Zaimplementowany |
| `UnmappedPageHandler` | ✅ | Zaimplementowany |
| `RuntimeBus` | ✅ | + IMemoryBus |
| Testy kontraktów | ✅ | 33 testy |

### 6.2 Rozbieżności od dokumentacji

| Rozbieżność | Uzasadnienie |
|-------------|--------------|
| `RomWritePolicy` nie było w specyfikacji | Dodano dla elastyczności obsługi ROM |
| `IBusTracer` nie było w specyfikacji | Dodano dla debugowania i trace |
| `RuntimeBus` implementuje `IMemoryBus` | Decyzja architektoniczna (unikanie adaptera) |

---

## 7. Blokery i zależności

### 7.1 Zależności

Faza 25 **zależy** od:
- Faza 24 (`IDevice`, `AddressSpaceDescriptor`) ✅

### 7.2 Blokery dla innych faz

Faza 25 **jest wymagana** przez:
- Faza 26: `ComputerBuilder` używa `RuntimeBus`
- Faza 27: Brak bezpośrednich zależności
- Faza 28: `Mos682xPiaDevice` używa `IMemoryMappedDevice`

---

## 8. Podsumowanie i kolejne kroki

### 8.1 Co zostało zrobione ✅

- [x] Zaimplementowane wszystkie kontrakty z Fazy 25
- [x] Utworzony `CompiledMemoryMap` z 256B stronami
- [x] Utworzony `CompiledPortMap`
- [x] Utworzony `RuntimeBus` (ISystemBus + IMemoryBus)
- [x] Utworzone wszystkie handlery stron
- [x] Napisane 33 testy jednostkowe
- [x] Wszystkie testy przeszły (33/33)
- [x] Istniejące testy wciąż przechodzą (330/331)

### 8.2 Co pozostało do zrobienia

Faza 25 jest **kompletna**. Kolejne kroki:

1. **Faza 26**: `ComputerProfile`, `ComputerBuilder`, `DeviceFactoryRegistry`
2. **Faza 27**: `ITerminalLink`, `BufferedTerminalLink`
3. **Faza 28**: `Mos682xPiaDevice`

### 8.3 Pliki do zaktualizowania

- [ ] `docs/faza-25-system-bus-memory-map.md` — Zmienić status z `[ ]` na `[x]`
- [ ] `docs/checklista.md` — Zaznaczyć Fazę 25 jako ukończoną

---

## 9. Struktura plików

```
src/Cpu6502/
├── System/
│   ├── ISystemBus.cs              # ISystemBus, IMemoryMappedDevice, IPortMappedDevice
│   ├── AddressSpaceKind.cs       # AddressSpaceKind enum
│   ├── RomWritePolicy.cs          # RomWritePolicy enum
│   ├── IBusTracer.cs              # IBusTracer + NullBusTracer
│   ├── CompiledMemoryMap.cs       # Mapa pamięci (256B strony)
│   ├── CompiledPortMap.cs         # Mapa portów
│   ├── RuntimeBus.cs              # Główna magistrala
│   └── PageHandlers/
│       ├── IPageHandler.cs        # Interfejs handlera
│       ├── RamPageHandler.cs      # Handler RAM
│       ├── RomPageHandler.cs      # Handler ROM + RomWriteException
│       ├── DevicePageHandler.cs   # Handler urządzenia
│       └── UnmappedPageHandler.cs # Handler niezmapowanej pamięci
└── (istniejące pliki)

tests/Cpu6502.Tests/
└── System/
    └── Faza25BusTests.cs           # 33 testy jednostkowe
```

---

## 10. Historia zmian

| Data | Autor | Zmiana |
|------|-------|--------|
| 2025-05-18 | Mistral Vibe | Implementacja Fazy 25 |
| 2025-05-18 | Mistral Vibe | Utworzenie 33 testów jednostkowych |

---

## 11. Zobacz także

- [faza-25-system-bus-memory-map.md](faza-25-system-bus-memory-map.md) — Oryginalna specyfikacja
- [faza-24-implementacja.md](faza-24-implementacja.md) — Poprzednia faza
- [faza-26-computer-profiles.md](faza-26-computer-profiles.md) — Następna faza
- [checklista.md](checklista.md) — Ogólna lista zadań
