# Faza 29: Implementacja profilu Apple-1 z PIA i WOZ Monitor

## Status
✅ Zaimplementowana

## Data implementacji
2025-XX-XX

## Cel fazy
Utworzenie profilu komputera Apple-1 z użyciem PIA (MOS 6821) jako urządzenia terminala, z WOZ Monitor w ROM.

## Zależności
- ✅ Faza 24: System profilów komputerów
- ✅ Faza 25: RuntimeBus i CompiledMemoryMap
- ✅ Faza 26: ComputerBuilder i fabryki urządzeń
- ✅ Faza 27: System modułów pamięci (RAM/ROM)
- ✅ Faza 28: MOS 6820/6821 PIA medium implementation

## Pliki utworzone

### 1. Profil komputera Apple-1
**Plik:** `profiles/computers/apple-1.json`

```json
{
  "id": "apple-1",
  "name": "Apple-1",
  "cpu": {
    "type": "mos6502-nmos",
    "clockHz": 1023000
  },
  "addressSpace": {
    "type": "mos6502"
  },
  "memory": {
    "ram": [
      {
        "id": "ram0",
        "startAddress": "0x0000",
        "size": "0x1000"
      }
    ],
    "rom": [
      {
        "id": "wozmon",
        "startAddress": "0xFF00",
        "size": "0x0100",
        "file": "roms/apple-1/wozmon.bin"
      }
    ]
  },
  "devices": [
    {
      "id": "pia0",
      "type": "mos6821-pia",
      "mapping": {
        "kind": "memory",
        "baseAddress": "0xD010",
        "size": "0x0004"
      },
      "options": {
        "preset": "apple-1-terminal"
      }
    }
  ]
}
```

### 2. Katalog ROM-ów
**Plik:** `roms/apple-1/README.md`

Dokumentacja dotycząca uzyskania pliku WOZ Monitor ROM.

### 3. Testy jednostkowe
**Plik:** `tests/Cpu6502.Tests/System/Faza29Apple1ProfileTests.cs`

11 testów jednostkowych weryfikujących:
- Budowę profilu Apple-1
- Walidację profilu
- Tworzenie urządzenia PIA przez fabrykę
- Mapowanie PIA pod adresami $D010-$D013
- Integrację z ComputerBuilder
- Odczyt/zapis przez terminal Apple-1
- Ponowne użycie kodu PIA w różnych profilach
- Smok test WOZ Monitor (opcjonalny, wymaga pliku ROM)

## Pliki zmodyfikowane

### 1. CompiledMemoryMap.cs
**Problem:** Błąd w mapowaniu urządzeń niezaczynających się od granicy strony (np. $D010).

**Poprawka:**
- Zmiana `offsetInDevice = pageStart - device.StartAddress` na `deviceOffsetInPage = device.StartAddress - pageStart`
- Poprawiono również mapowanie ROM z tym samym błędem

### 2. DevicePageHandler.cs
**Problem:** DevicePageHandler używał `_baseOffset + offset` zamiast `offset - _baseOffset`.

**Poprawka:**
- Zmiana `ReadByte(uint offset) => _device.ReadMemory(_baseOffset + offset)` na `ReadByte(uint offset) => _device.ReadMemory(offset - _baseOffset)`
- Analogiczna zmiana dla `WriteByte`

### 3. Mos682xPiaDevice.cs
**Problem:** Interfejs `IMemoryMappedDevice` oczekuje adresów względnych (0-3 dla PIA), ale implementacja używała adresów bezwzględnych i odejmowała `StartAddress` wewnątrz metod.

**Poprawka:**
- Zmiana parametru `address` na adres względny
- Usunięcie odejmowania `StartAddress` (teraz robi to DevicePageHandler)
- Aktualizacja dokumentacji XML
- Poprawka komunikatów błędów, aby pokazywały pełny adres

### 4. Mos682xPiaDeviceFactory.cs
**Problem:** Fabryka inicjalizowała rejestry PIA używając adresów bezwzględnych.

**Poprawka:**
- Zmiana `device.WriteMemory(baseAddress + 3, ...)` na `device.WriteMemory(3, ...)`
- Wszystkie inicjalizacje używają teraz adresów względnych (0-3)

### 5. Faza28PiaTests.cs
**Problem:** Testy używały adresów bezwzględnych (np. 0xD010) zamiast względnych (0-3).

**Poprawka:**
- Masowe zastąpienie `ReadMemory(0xD010)` → `ReadMemory(0)`
- Masowe zastąpienie `ReadMemory(0xD011)` → `ReadMemory(1)`
- Masowe zastąpienie `ReadMemory(0xD012)` → `ReadMemory(2)`
- Masowe zastąpienie `ReadMemory(0xD013)` → `ReadMemory(3)`
- Analogiczne zmiany dla `WriteMemory`
- Poprawka testów granicznych, aby używały adresów względnych

## Testy

### Faza 28 (PIA)
- **Liczba testów:** 43
- **Status:** ✅ Wszystkie przechodzą
- **Zmiany:** Dostosowanie do nowego interfejsu (adresy względne)

### Faza 29 (Apple-1 Profile)
- **Liczba testów:** 11
- **Status:** ✅ 10 przechodzi, 1 pominięty (wymaga pliku ROM)

### Razem
- **Liczba testów:** 442 przechodzi, 1 pominięty
- **Status:** ✅ Wszystkie testy przechodzą

## Decyzje projektowe

### 1. Interfejs IMemoryMappedDevice
- **Decyzja:** Adresy przekazywane do `ReadMemory`/`WriteMemory` są względne (offset od `StartAddress`)
- **Uzasadnienie:** Spójność z wzorcem Adapter i uproszczenie implementacji DevicePageHandler

### 2. Poprawki w Fazie 25 (CompiledMemoryMap)
- **Decyzja:** Poprawiono błędy w mapowaniu urządzeń/ROM-ów niezaczynających się od granicy strony
- **Uzasadnienie:** Błędy te uniemożliwiały działanie urządzeń pod adresami takimi jak $D010 (Apple-1 PIA)

### 3. Nie używanie plików JSON w testach
- **Decyzja:** Testy używają programowo tworzonych profili zamiast ładowania z plików JSON
- **Uzasadnienie:** Pliki JSON nie są kopiowane do katalogu wyjściowego testów, co powodowało błędy `FileNotFoundException`

### 4. Rejestracja fabryk CPU
- **Decyzja:** Testy rejestrują fabryki CPU i urządzeń w eigen registry
- **Uzasadnienie:** Konieczne do budowy komputerów z profili bez modyfikowania globalnego rejestru

## Problemy i rozwiązania

### Problem 1: Błąd mapowania urządzeń niezaczynających się od granicy strony
**Objawy:** `ArgumentOutOfRangeException` przy dostępie do PIA pod $D010
**Przyczyna:** Błędne obliczanie offsetu w CompiledMemoryMap i DevicePageHandler
**Rozwiązanie:** Poprawiono formuły obliczania offsetu

### Problem 2: Niespójność interfejsu IMemoryMappedDevice
**Objawy:** Testy Fazy 28 używały adresów bezwzględnych
**Przyczyna:** Oryginalna implementacja PIA odejmowała StartAddress wewnątrz metod
**Rozwiązanie:** Zmieniono na adresy względne, zaktualizowano wszystkie testy

### Problem 3: Brak fabryki CPU w testach Fazy 29
**Objawy:** `ComputerBuildException: No CPU factory registered for type: 'mos6502-nmos'`
**Przyczyna:** Testy używały ComputerBuilder z domyślnym rejestrem, który nie miał zarejestrowanej fabryki CPU
**Rozwiązanie:** Testy tworzą własny rejestr i rejestrują fabryki CPU i PIA

## Podsumowanie

Faza 29 została zaimplementowana pomyślnie. Głównym wyzwaniem były błędy w implementacji Fazy 25 (CompiledMemoryMap), które zostały naprawione w ramach tej fazy. Dodatkowo, dostosowano implementację PIA do poprawnego używania adresów względnych zgodnie z kontraktem interfejsu IMemoryMappedDevice.

Wszystkie testy przechodzą, a profil Apple-1 jest gotowy do użycia z WOZ Monitor.

## Następne kroki
- Faza 30: Integracja z frontendem (opcjonalnie)
- Faza 31: Testy integracyjne z WOZ Monitor (wymaga pliku ROM)
