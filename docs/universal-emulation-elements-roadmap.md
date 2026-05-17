# Uniwersalne elementy emulatora dla wielu CPU

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: uzupełnienie dokumentacji architektonicznej  
Zakres: wspólne elementy potrzebne do emulacji rodzin 6502, 8080/Z80, 6800/6809, 65C816, 68000 i procesorów pokrewnych

---

## 1. Cel

Ten dokument uzupełnia `docs/modular-computer-composition-architecture.md` o brakujące elementy uniwersalne. Celem jest uniknięcie architektury zbyt mocno przywiązanej do 6502.

Projekt powinien umożliwiać dodawanie kolejnych rdzeni CPU bez przebudowy profili, frontendów i urządzeń wspólnych.

---

## 2. Procesory objęte zakresem

| Rodzina | CPU | Uwagi |
|---|---|---|
| 6502 | MOS 6502, 6510, 65C02, 8502, Ricoh 2A03, 65C816 | główna linia projektu |
| Intel 8-bit | 8080, 8085 | osobna przestrzeń portów I/O |
| Zilog | Z80, Z180 | port I/O, przerwania IM 0/1/2, prefixy |
| Motorola 8-bit | 6800, 6802, 6809 | memory-mapped I/O, inne rejestry i przerwania |
| Motorola 16/32-bit | 68000 | szerszy bus, wyjątki, tryb supervisor/user |
| VM / edukacyjne | CHIP-8 | bardziej VM niż CPU, ale pasuje do `ICpuCore` |

---

## 3. Minimalny wspólny rdzeń architektury

| Element | Priorytet | Cel |
|---|---:|---|
| `ICpuCore` | P0 | wspólny kontrakt CPU |
| `ISystemBus` | P0 | pamięć + porty I/O |
| `IDevice` | P0 | wspólny marker urządzeń |
| `IMemoryMappedDevice` | P0 | urządzenia widoczne w pamięci |
| `IPortMappedDevice` | P0 | urządzenia widoczne w portach I/O |
| `ICycleDevice` | P0 | timery, video, audio, DMA |
| `IIrqSource` / `INmiSource` | P0 | źródła przerwań |
| `IResettableDevice` | P0 | reset urządzeń |
| `CpuSnapshot` | P0 | debug, UI, testy |
| `ComputerProfile` | P0 | konfiguracja runtime maszyny |
| `ComputerBuilder` | P0 | składanie maszyny |
| `CpuFactory` | P0 | wymienne rdzenie CPU |
| `DeviceFactory` | P0 | wymienne urządzenia |
| `SystemBus` | P0 | routing pamięci i portów |

---

## 4. Elementy wymagane dla dokładniejszej emulacji

| Element | Priorytet | Cel |
|---|---:|---|
| `ExecutionTrace` | P1 | trace instrukcji CPU |
| `MemoryAccessTrace` | P1 | trace odczytów/zapisów pamięci |
| `PortAccessTrace` | P1 | trace portów I/O dla Z80/8080 |
| `InterruptController` | P1 | agregacja IRQ/NMI/INT |
| `ClockScheduler` | P1 | taktowanie CPU i układów z różnymi zegarami |
| `AddressSpaceDescriptor` | P1 | opis szerokości adresów i portów |
| `CpuFeatureDescriptor` | P1 | opis cech CPU |
| `BusTransaction` | P1 | wspólny model operacji busa |
| `DmaController` | P2 | DMA, video stealing, cartridge DMA |
| `BankedMemoryController` | P2 | C64, NES, 65C816, Atari XL/XE |
| `ExceptionVectorController` | P2 | 68000, 65C816, Z80 IM2 |
| `PluginRegistry` | P3 | dynamiczne ładowanie CPU/urządzeń |

---

## 5. `AddressSpaceDescriptor`

### 5.1. Cel

Różne CPU mają różne przestrzenie adresowe. Nie należy zakładać stałego `ushort` w warstwie systemowej.

### 5.2. Proponowany model

```csharp
public sealed record AddressSpaceDescriptor(
    int MemoryAddressBits,
    int PortAddressBits,
    bool HasSeparatePortSpace,
    bool SupportsUnalignedAccess,
    int DataBusBits);
```

### 5.3. Przykłady

| CPU | Memory bits | Port bits | Separate port space | Data bus |
|---|---:|---:|---:|---:|
| 6502 | 16 | 0 | nie | 8 |
| 6510 | 16 | 0 | nie | 8 |
| 65C816 | 24 | 0 | nie | 8 |
| 8080 | 16 | 8 | tak | 8 |
| Z80 | 16 | 16 | tak | 8 |
| 6809 | 16 | 0 | nie | 8 |
| 68000 | 24 | 0 | nie | 16 |

---

## 6. `CpuFeatureDescriptor`

### 6.1. Cel

Profil CPU powinien jawnie opisywać cechy rdzenia, zamiast ukrywać je w implementacji.

### 6.2. Proponowany model

```csharp
public sealed record CpuFeatureDescriptor(
    string CpuType,
    AddressSpaceDescriptor AddressSpace,
    bool HasDecimalMode,
    bool HasSeparateIoPorts,
    bool HasNmi,
    bool HasMaskableInterrupt,
    bool HasHaltLine,
    bool HasReadyLine,
    bool SupportsDmaStall,
    bool HasBankRegisters,
    bool IsLittleEndian);
```

### 6.3. Przykłady decyzji

| CPU | Decyzja |
|---|---|
| Ricoh 2A03 | `HasDecimalMode = false` |
| 6510 | `HasBankRegisters = true` albo osobny CPU port device |
| Z80 | `HasSeparateIoPorts = true`, `HasHaltLine = true` |
| 68000 | `DataBusBits = 16`, wyjątki jako osobny etap |

---

## 7. `CpuSnapshot`

### 7.1. Cel

UI, testy i trace muszą mieć wspólny sposób pobierania stanu CPU, ale rejestry są różne dla każdej rodziny.

### 7.2. Proponowany model elastyczny

```csharp
public sealed record CpuSnapshot(
    string CpuType,
    ulong ProgramCounter,
    ulong StackPointer,
    IReadOnlyDictionary<string, ulong> Registers,
    IReadOnlyDictionary<string, bool> Flags,
    long CycleCount,
    long InstructionCount);
```

### 7.3. Przykłady rejestrów

| CPU | Rejestry w `Registers` |
|---|---|
| 6502 | `A`, `X`, `Y`, `P`, `SP`, `PC` |
| Z80 | `A`, `F`, `BC`, `DE`, `HL`, `IX`, `IY`, `SP`, `PC`, `I`, `R` |
| 8080 | `A`, `F`, `BC`, `DE`, `HL`, `SP`, `PC` |
| 6809 | `A`, `B`, `D`, `X`, `Y`, `U`, `S`, `DP`, `CC`, `PC` |
| 68000 | `D0-D7`, `A0-A7`, `SR`, `PC` |

---

## 8. Trace i diagnostyka

### 8.1. `ExecutionTraceEntry`

```csharp
public sealed record ExecutionTraceEntry(
    long InstructionIndex,
    long Cycle,
    string CpuType,
    ulong ProgramCounter,
    byte Opcode,
    string Mnemonic,
    IReadOnlyList<byte> Bytes,
    CpuSnapshot Before,
    CpuSnapshot After);
```

### 8.2. `BusTransaction`

```csharp
public sealed record BusTransaction(
    long Cycle,
    BusTransactionKind Kind,
    AddressSpaceKind AddressSpace,
    uint Address,
    uint Value,
    int WidthBits,
    string? DeviceId);
```

### 8.3. Enums

```csharp
public enum BusTransactionKind
{
    Read,
    Write,
    InterruptAcknowledge,
    DmaRead,
    DmaWrite
}

public enum AddressSpaceKind
{
    Memory,
    Port,
    InternalCpu
}
```

---

## 9. Przerwania i linie CPU

### 9.1. Problem

Różne CPU mają różne linie i tryby przerwań:

| CPU | Linie / tryby |
|---|---|
| 6502 | IRQ, NMI, RESET, RDY |
| Z80 | INT, NMI, RESET, BUSRQ/BUSACK, HALT, IM 0/1/2 |
| 8080 | INTR, RST vectors |
| 6809 | IRQ, FIRQ, NMI, RESET, HALT |
| 68000 | IPL0-IPL2, RESET, HALT, exceptions |

### 9.2. Minimalny model MVP

```csharp
public interface ICpuSignalSink
{
    void SetSignal(CpuSignal signal, bool asserted);
}

public enum CpuSignal
{
    Reset,
    Irq,
    Nmi,
    Int,
    Firq,
    Halt,
    Ready,
    BusRequest
}
```

### 9.3. Agregator

```csharp
public sealed class CpuSignalController
{
    public void SetSource(string sourceId, CpuSignal signal, bool asserted);
    public bool IsAsserted(CpuSignal signal);
}
```

Dla MVP można nadal mieć `IIrqSource` i `INmiSource`, ale docelowo `CpuSignalController` lepiej obsłuży Z80/6809/68000.

---

## 10. Port-mapped I/O

### 10.1. Wymagane dla

- Intel 8080,
- Intel 8085,
- Z80,
- Z180,
- wielu systemów CP/M.

### 10.2. Kontrakt

```csharp
public interface IPortMappedDevice : IDevice
{
    bool HandlesPort(uint port);
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

### 10.3. Trace portów

Każdy odczyt/zapis portu powinien generować `BusTransaction` z `AddressSpaceKind.Port`.

---

## 11. Szersze magistrale i dostęp wielobajtowy

### 11.1. Problem

68000 ma 16-bitową magistralę danych i operacje byte/word/long. 6502 i Z80 są 8-bitowe.

### 11.2. MVP

Na początku `ISystemBus` może zostać bajtowy:

```csharp
byte ReadMemory(uint address);
void WriteMemory(uint address, byte value);
```

### 11.3. Rozszerzenie późniejsze

```csharp
uint ReadMemory(uint address, int widthBits);
void WriteMemory(uint address, uint value, int widthBits);
```

Nie wdrażać tego przed 68000/16-bit bus.

---

## 12. DMA i zatrzymywanie CPU

### 12.1. Wymagane dla

| System | Powód |
|---|---|
| NES | OAM DMA |
| Atari 8-bit | ANTIC DMA / CPU stealing |
| C64 | VIC-II badlines / bus sharing |
| Z80 systems | BUSRQ/BUSACK |
| 68000 systems | DMA kontrolerów zewnętrznych |

### 12.2. Kontrakt późniejszy

```csharp
public interface IDmaDevice : IDevice
{
    bool IsDmaActive { get; }
    void TickDma(ISystemBus bus);
}
```

### 12.3. Linia zatrzymania CPU

```csharp
public interface ICpuStallController
{
    bool ShouldStallCpu { get; }
}
```

---

## 13. Bankowanie pamięci i MMU

### 13.1. Wymagane dla

| System | Mechanizm |
|---|---|
| C64 | port 6510 `$0000/$0001`, ROM/RAM/I/O banking |
| NES | cartridge mappers |
| 65C816 | banki 24-bit |
| Atari XL/XE | bankowanie RAM/ROM |
| Z180 | MMU |
| 68000 systems | późniejsze MMU opcjonalnie |

### 13.2. Proponowany model

```csharp
public interface IAddressTranslator : IDevice
{
    bool TryTranslate(AddressSpaceKind space, uint logicalAddress, out uint physicalAddress);
}
```

Dla prostych maszyn nie używać translatora.

---

## 14. Profile CPU

### 14.1. Cel

CPU powinno mieć własny profil, który można referencjonować z profilu komputera.

### 14.2. Przykład

```json
{
  "schema": "cpu-profile/v1",
  "type": "z80",
  "displayName": "Zilog Z80",
  "addressSpace": {
    "memoryAddressBits": 16,
    "portAddressBits": 16,
    "hasSeparatePortSpace": true,
    "dataBusBits": 8
  },
  "features": {
    "hasDecimalMode": false,
    "hasNmi": true,
    "hasMaskableInterrupt": true,
    "hasHaltLine": true,
    "hasReadyLine": false
  }
}
```

---

## 15. Aktualizacja kolejności implementacji

### Etap 1 — minimalny multi-CPU foundation

- [ ] `ICpuCore`
- [ ] `ISystemBus`
- [ ] `IDevice`
- [ ] `IMemoryMappedDevice`
- [ ] `IPortMappedDevice`
- [ ] `ICycleDevice`
- [ ] `IResettableDevice`
- [ ] `CpuSnapshot`
- [ ] `AddressSpaceDescriptor`
- [ ] `CpuFeatureDescriptor`

### Etap 2 — bus i trace

- [ ] `SystemBus`
- [ ] `BusTransaction`
- [ ] `MemoryAccessTrace`
- [ ] `PortAccessTrace`
- [ ] walidacja konfliktów mapowania

### Etap 3 — profile i builder

- [ ] `ComputerProfile`
- [ ] `CpuProfile`
- [ ] `DeviceProfile`
- [ ] `ComputerBuilder`
- [ ] `CpuFactoryRegistry`
- [ ] `DeviceFactoryRegistry`

### Etap 4 — 6502 jako pierwszy adapter

- [ ] `Cpu6502Core : ICpuCore`
- [ ] adapter obecnego busa do `ISystemBus`
- [ ] profil `minimal-sbc-6502`
- [ ] `UartSimpleDevice`

### Etap 5 — Z80 proof of architecture

- [ ] stub `CpuZ80Core`
- [ ] port-mapped UART
- [ ] profil `custom-z80-sbc`
- [ ] test, że Z80 używa portów bez zmian w 6502

### Etap 6 — rozszerzenia późniejsze

- [ ] `CpuSignalController`
- [ ] `ClockScheduler`
- [ ] `IAddressTranslator`
- [ ] `IDmaDevice`
- [ ] szerokości busa większe niż 8 bitów

---

## 16. Decyzje

1. W warstwie systemowej używać `uint` dla adresów, nie `ushort`.
2. W MVP bus może być bajtowy.
3. Port I/O musi być częścią `ISystemBus` od początku.
4. `CpuSnapshot` musi być słownikowy/elastyczny, nie 6502-specific.
5. `IIrqSource`/`INmiSource` są wystarczające dla 6502, ale docelowo potrzebny będzie `CpuSignalController`.
6. DMA, bankowanie i 16-bit bus nie są MVP, ale kontrakty powinny być przewidziane.
7. Z80 powinien być pierwszym testem, że architektura nie jest przywiązana do 6502.

---

## 17. Definition of Done

Ten obszar można uznać za ujęty w dokumentacji, gdy:

- [ ] `modular-computer-composition-architecture.md` odwołuje się do tego dokumentu,
- [ ] `planning-documents-index.md` zawiera ten dokument,
- [ ] roadmapa implementacji zawiera `AddressSpaceDescriptor`, `CpuFeatureDescriptor`, `CpuSnapshot`, trace i sygnały CPU,
- [ ] najbliższy krok implementacyjny obejmuje port-mapped I/O od początku,
- [ ] dokumentacja jasno wskazuje, że 6502 jest pierwszym rdzeniem, ale nie jedynym docelowym CPU.
