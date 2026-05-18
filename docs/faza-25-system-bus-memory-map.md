# Faza 25 - RuntimeBus, przestrzenie adresowe i skompilowana mapa

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [x] **Zaimplementowana** |
| **Zakres** | Szybka magistrala runtime |
| **Zaleznosci** | Faza 24 |
| **Cel projektowy** | Jedna infrastruktura dla memory-mapped i port-mapped I/O |

---

## Cel fazy

Zbudowac `RuntimeBus`, ktory obsluguje pamiec, porty I/O i urzadzenia bez iterowania po liscie urzadzen w hot path. To jest fundament dla Apple-1, PET, KIM-1, C64, ale rowniez dla Z80/8080 z osobna przestrzenia portow.

---

## Kontrakty do zaimplementowania

```csharp
public interface ISystemBus
{
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

```csharp
public interface IMemoryMappedDevice : IDevice
{
    uint StartAddress { get; }
    uint Size { get; }
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);
}
```

```csharp
public interface IPortMappedDevice : IDevice
{
    uint StartPort { get; }
    uint Size { get; }
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

```csharp
public enum AddressSpaceKind
{
    Memory,
    Port
}
```

---

## Struktury runtime

Minimalny wariant dla 8-bitowych maszyn:

- `CompiledMemoryMap` z handlerami stron 256 bajtow.
- `CompiledPortMap` z handlerami portow albo zakresow.
- `RamPageHandler`, `RomPageHandler`, `DevicePageHandler`, `UnmappedPageHandler`.
- `RuntimeBus : ISystemBus`.

Dla CPU z przestrzenia wieksza niz 64 KB w tej fazie wystarczy walidacja i czytelny blad: `AddressSpaceDescriptor.MemoryAddressBits > 16` nie jest jeszcze obslugiwane przez 8-bitowa skompilowana mape.

---

## Reguly mapowania

1. RAM i ROM sa regionami pamieci, nie urzadzeniami.
2. Urzadzenia memory-mapped sa mapowane w `AddressSpaceKind.Memory`.
3. Urzadzenia port-mapped sa mapowane w `AddressSpaceKind.Port`.
4. Konflikt dwoch zakresow tej samej przestrzeni jest bledem budowania.
5. Odczyt niezmapowanej pamieci domyslnie zwraca `0xFF`.
6. Zapis do niezmapowanej pamieci domyslnie jest ignorowany.
7. ROM jest read-only: zapis ignorowany albo blad wedlug opcji `RomWritePolicy`.
8. Trace busa jest opcjonalny i nie moze alokowac, gdy jest wylaczony.

---

## Kolejnosc wykonania dla agenta

1. Dodaj `ISystemBus`, `IMemoryMappedDevice`, `IPortMappedDevice`.
2. Dodaj `MemoryRegion` i `MemoryRegionKind`.
3. Dodaj handler stron i portow.
4. Dodaj `CompiledMemoryMapBuilder` z walidacja konfliktow.
5. Dodaj `CompiledPortMapBuilder`.
6. Dodaj `RuntimeBus`.
7. Dodaj adapter `IMemoryBus` dla obecnego `Cpu6502`, ktory przekierowuje `ushort` do `RuntimeBus.ReadMemory/WriteMemory`.
8. Dodaj testy.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `ReadMemory_FromRam_ReturnsWrittenByte` | RAM dziala |
| `WriteMemory_ToRom_IsIgnoredOrRejectedByPolicy` | ROM jest read-only |
| `ReadMemory_FromDevice_RoutesToDevice` | memory-mapped device dostaje odczyt |
| `WriteMemory_ToDevice_RoutesToDevice` | memory-mapped device dostaje zapis |
| `BuildMemoryMap_OverlappingRanges_Throws` | konflikt zakresow jest wykryty |
| `ReadMemory_Unmapped_ReturnsFF` | domysl dla pustej pamieci |
| `ReadPort_FromPortDevice_RoutesToDevice` | port-mapped device dziala |
| `BuildPortMap_OverlappingPorts_Throws` | konflikt portow jest wykryty |
| `Cpu6502MemoryBusAdapter_RoutesThroughRuntimeBus` | obecny CPU moze uzyc nowego busa |

---

## Poza zakresem

- Profile JSON.
- Fabryki urzadzen.
- Bank switching, DMA, contention.
- Pelna obsluga 24-bit/32-bit busa.

---

## Kryteria akceptacji

- Hot path odczytu pamieci nie iteruje po liscie urzadzen.
- Istnieje sciezka kompatybilna z obecnym `IMemoryBus`.
- Port-mapped I/O jest zaprojektowane i przetestowane, nawet jesli nie ma jeszcze CPU Z80.

