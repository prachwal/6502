# Modular Computer Composition Architecture

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument architektoniczny / plan implementacyjny  
Zakres: uniwersalny mechanizm składania komputerów z wymiennymi CPU, pamięcią, urządzeniami I/O, ROM-ami i frontendami

---

## 1. Cel

Celem jest zaprojektowanie mechanizmu, który pozwala składać różne komputery retro z wymiennych komponentów:

- CPU: `mos6502`, `mos6510`, `wdc65c02`, `ricoh2a03`, `z80`, później inne,
- RAM/ROM,
- magistrala systemowa,
- urządzenia memory-mapped,
- urządzenia port-mapped,
- źródła IRQ/NMI,
- urządzenia cyklowe,
- terminale i frontendy,
- profile komputerów jako dane konfiguracyjne.

Rdzeń CPU nie powinien znać nazw komputerów ani konkretnych układów. Komputer powinien być budowany przez `ComputerBuilder` na podstawie profilu.

Architektura ma rozdzielać:

```text
kompozycję maszyny: profile, fabryki, walidacja, czytelność
runtime maszyny: szybkie mapy pamięci/portów, scheduler, minimalny koszt hot path
```

Komentarz: sama modularność nie wystarczy do wydajnej emulacji. `ComputerBuilder` może używać interfejsów i list urządzeń, ale wynikowy `EmulatedComputer` powinien używać skompilowanych struktur runtime, żeby nie iterować po urządzeniach przy każdym odczycie pamięci albo zapisie portu.

---

## 2. Główna zasada

Nie budować emulatora jako jednej klasy typu `Apple1Emulator`, `PetEmulator`, `C64Emulator`.

Budować emulator jako:

```text
ComputerProfile
   |
ComputerBuilder
   |
EmulatedComputer
   |-- ICpuCore
   |-- RuntimeBus / ISystemBus
   |-- CompiledMemoryMap
   |-- CompiledPortMap
   |-- MemoryRegions
   |-- Devices
   |-- InterruptController
   |-- Clock/Scheduler
   |-- Frontend adapters
```

---

## 3. Podział odpowiedzialności

| Element | Odpowiedzialność |
|---|---|
| `ICpuCore` | wykonuje instrukcje CPU, zgłasza cykle i linie przerwań |
| `ISystemBus` | kontrakt pamięci i portów I/O widziany przez CPU |
| `RuntimeBus` | szybka implementacja busa używana po zbudowaniu maszyny |
| `CompiledMemoryMap` | szybki routing odczytów/zapisów pamięci |
| `CompiledPortMap` | szybki routing portów I/O dla Z80/8080 |
| `IMemoryMappedDevice` | urządzenie widoczne w przestrzeni pamięci |
| `IPortMappedDevice` | urządzenie widoczne w przestrzeni portów, np. Z80/8080 |
| `ICycleDevice` | urządzenie aktualizowane cyklowo |
| `IIrqSource` / `INmiSource` | źródło przerwań |
| `ComputerProfile` | dane konfiguracyjne maszyny |
| `ComputerBuilder` | składa maszynę z profilu i kompiluje mapy runtime |
| `DeviceFactory` | tworzy urządzenia po typie z profilu |
| `CpuFactory` | tworzy CPU po typie z profilu |
| `FrontendAdapter` | łączy terminal/ekran/audio z UI |

---

## 4. Kontrakty CPU

### 4.1. Minimalny kontrakt CPU

```csharp
public interface ICpuCore
{
    string Id { get; }
    string DisplayName { get; }

    void Reset();
    void StepInstruction();
    void Tick();

    CpuSnapshot GetSnapshot();
}
```

### 4.2. CPU z liniami przerwań

```csharp
public interface IInterruptibleCpuCore : ICpuCore
{
    void SetIrq(bool asserted);
    void SetNmi(bool asserted);
}
```

### 4.3. CPU z linią RDY/HALT

```csharp
public interface IStallableCpuCore : ICpuCore
{
    void SetReady(bool ready);
    void SetHalt(bool halted);
}
```

### 4.4. CPU z przestrzenią portów

Nie dodawać portów do samego CPU jako osobnych callbacków. CPU powinien korzystać z `ISystemBus`, który obsługuje zarówno pamięć, jak i porty.

---

## 5. Magistrala systemowa

### 5.1. Dlaczego nie tylko `IMemoryBus`

6502 używa głównie memory-mapped I/O. Z80 i 8080 mają osobną przestrzeń portów. 65C816 może mieć szerszą przestrzeń adresową. Dlatego docelowo potrzebny jest szerszy kontrakt.

### 5.2. `ISystemBus`

```csharp
public interface ISystemBus
{
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);

    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

### 5.3. Zachowanie dla CPU bez portów

Dla 6502:

- `ReadMemory` i `WriteMemory` są używane normalnie,
- `ReadPort` i `WritePort` mogą zwracać błąd, `0xFF` albo być nieużywane.

Dla Z80:

- `ReadMemory`/`WriteMemory` obsługują 64 KB pamięci,
- `ReadPort`/`WritePort` obsługują porty I/O.

---

## 6. Szybki runtime bus

### 6.1. Problem

Nie używać listy urządzeń jako jedynego mechanizmu odczytu/zapisu w runtime:

```csharp
foreach (var device in devices)
{
    if (device.HandlesMemory(address))
        return device.ReadMemory(address);
}
```

Taki kod jest prosty, ale za wolny dla hot path CPU. Jest akceptowalny w prototypie, walidatorze albo narzędziach diagnostycznych, ale nie jako docelowy runtime.

### 6.2. Rozwiązanie

`ComputerBuilder` powinien kompilować profil do szybkich map:

```text
Memory range/device definitions
  -> validation
  -> CompiledMemoryMap
  -> RuntimeBus
```

Dla portów:

```text
Port definitions
  -> validation
  -> CompiledPortMap
  -> RuntimeBus
```

### 6.3. Minimalny model

```csharp
public interface IMemoryPageHandler
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}

public sealed class CompiledMemoryMap
{
    private readonly IMemoryPageHandler[] _readPages = new IMemoryPageHandler[256];
    private readonly IMemoryPageHandler[] _writePages = new IMemoryPageHandler[256];

    public byte Read(ushort address)
        => _readPages[address >> 8].Read(address);

    public void Write(ushort address, byte value)
        => _writePages[address >> 8].Write(address, value);
}
```

### 6.4. Dlaczego strony 256 bajtów

Dla 64 KB przestrzeni adresowej daje to 256 wpisów. To wystarczy dla pierwszych CPU:

- 6502,
- 6510,
- 65C02,
- Ricoh 2A03,
- 8080,
- Z80,
- 6800,
- 6809.

Dla 65C816/68000 można później dodać większy albo wielopoziomowy model mapowania.

---

## 7. Urządzenia

### 7.1. Bazowy marker

```csharp
public interface IDevice
{
    string Id { get; }
}
```

### 7.2. Memory-mapped device

```csharp
public interface IMemoryMappedDevice : IDevice
{
    bool HandlesMemory(uint address);
    byte ReadMemory(uint address);
    void WriteMemory(uint address, byte value);
}
```

### 7.3. Port-mapped device

```csharp
public interface IPortMappedDevice : IDevice
{
    bool HandlesPort(uint port);
    byte ReadPort(uint port);
    void WritePort(uint port, byte value);
}
```

### 7.4. Cycle device

```csharp
public interface ICycleDevice : IDevice
{
    void Tick();
}
```

### 7.5. IRQ/NMI source

```csharp
public interface IIrqSource : IDevice
{
    bool IsIrqAsserted { get; }
}

public interface INmiSource : IDevice
{
    bool IsNmiAsserted { get; }
}
```

### 7.6. Resettable device

```csharp
public interface IResettableDevice : IDevice
{
    void Reset();
}
```

---

## 8. Pamięć

### 8.1. Region pamięci

```csharp
public sealed record MemoryRegion(
    string Id,
    uint Start,
    uint Size,
    MemoryRegionKind Kind,
    byte[] Data,
    bool ReadOnly);
```

### 8.2. Rodzaje regionów

```csharp
public enum MemoryRegionKind
{
    Ram,
    Rom,
    Mirror,
    Unmapped
}
```

### 8.3. Reguły

- ROM jest read-only.
- RAM jest read/write.
- Mirror powinien wskazywać region źródłowy.
- Konflikty mapowania powinny być wykrywane przy budowaniu komputera.
- Dla systemów z bankowaniem pamięci potrzebny będzie później `IBankedMemoryController`.

---

## 9. Fabryki CPU i urządzeń

### 9.1. CPU factory

```csharp
public interface ICpuFactory
{
    string CpuType { get; }
    ICpuCore Create(CpuProfile profile, ISystemBus bus);
}
```

Przykłady `CpuType`:

```text
mos6502
mos6502-nmos
mos6510
wdc65c02
ricoh2a03
z80
intel8080
```

### 9.2. Device factory

```csharp
public interface IDeviceFactory
{
    string DeviceType { get; }
    IDevice Create(DeviceProfile profile, DeviceFactoryContext context);
}
```

Przykłady `DeviceType`:

```text
ram
rom
uart-simple
mos6821-pia
mos6520-pia
mos6522-via
mos6530-rriot
mos6551-acia
ay-3-8910
nes-controller-ports
```

### 9.3. Factory context

```csharp
public sealed class DeviceFactoryContext
{
    public required IServiceProvider Services { get; init; }
    public required IReadOnlyDictionary<string, object> SharedResources { get; init; }
    public required string MachineId { get; init; }
}
```

---

## 10. Profil komputera

### 10.1. Minimalny schemat logiczny

```json
{
  "schema": "computer-profile/v1",
  "id": "custom-6502-sbc",
  "name": "Custom 6502 SBC",
  "status": "planned",
  "cpu": {
    "type": "mos6502",
    "clockHz": 1000000
  },
  "memory": {
    "ram": [
      { "id": "ram0", "start": "0x0000", "size": "0x8000" }
    ],
    "rom": [
      { "id": "rom0", "start": "0xC000", "size": "0x4000", "file": "roms/custom-sbc/monitor.bin" }
    ]
  },
  "devices": [
    {
      "id": "uart0",
      "type": "uart-simple",
      "mapping": {
        "kind": "memory",
        "baseAddress": "0xD000",
        "size": "0x0002"
      },
      "link": {
        "type": "fake-mainframe"
      }
    }
  ],
  "vectors": {
    "reset": "0xFFFC",
    "irq": "0xFFFE",
    "nmi": "0xFFFA"
  }
}
```

### 10.2. Profil Z80

```json
{
  "schema": "computer-profile/v1",
  "id": "custom-z80-sbc",
  "name": "Custom Z80 SBC",
  "status": "planned",
  "cpu": {
    "type": "z80",
    "clockHz": 4000000
  },
  "memory": {
    "ram": [
      { "id": "ram0", "start": "0x0000", "size": "0x10000" }
    ]
  },
  "devices": [
    {
      "id": "uart0",
      "type": "uart-simple",
      "mapping": {
        "kind": "port",
        "basePort": "0x10",
        "size": "0x02"
      }
    },
    {
      "id": "ay0",
      "type": "ay-3-8910",
      "mapping": {
        "kind": "port",
        "addressPort": "0x20",
        "dataPort": "0x21"
      }
    }
  ]
}
```

---

## 11. `ComputerBuilder`

### 11.1. Odpowiedzialność

`ComputerBuilder` powinien:

1. wczytać profil,
2. zwalidować schemat,
3. utworzyć pamięć,
4. utworzyć urządzenia,
5. wykryć konflikty adresów/portów,
6. skompilować `CompiledMemoryMap`,
7. skompilować `CompiledPortMap`, jeśli CPU albo urządzenia używają portów,
8. utworzyć `RuntimeBus`,
9. utworzyć CPU,
10. podłączyć IRQ/NMI,
11. zwrócić `EmulatedComputer`.

### 11.2. Proponowany model

```csharp
public sealed class ComputerBuilder
{
    public EmulatedComputer Build(ComputerProfile profile);
}
```

### 11.3. Wynik budowania

```csharp
public sealed class EmulatedComputer
{
    public required string Id { get; init; }
    public required ICpuCore Cpu { get; init; }
    public required ISystemBus Bus { get; init; }
    public required IReadOnlyList<IDevice> Devices { get; init; }
    public required ComputerClock Clock { get; init; }

    public void Reset();
    public void Tick();
    public void StepInstruction();
}
```

---

## 12. Scheduler / zegar

### 12.1. MVP

W MVP można użyć prostego modelu:

```text
computer.Tick():
  cpu.Tick()
  each cycle device.Tick()
  update interrupt lines
```

### 12.2. Później

Dla C64, Atari, NES, Apple II i dokładnych układów wideo potrzebny będzie scheduler cyklowy z proporcjami zegarów:

```text
CPU clock
video clock
audio clock
timer clock
```

Nie blokować MVP pełnym cycle-accurate schedulerem.

---

## 13. Przerwania

### 13.1. MVP

Na początku wystarczy agregator:

```csharp
public sealed class InterruptController
{
    public bool IrqAsserted { get; }
    public bool NmiAsserted { get; }
}
```

Agregacja:

```text
IRQ = OR wszystkich IIrqSource
NMI = OR wszystkich INmiSource
```

### 13.2. Integracja z CPU

Jeżeli CPU implementuje `IInterruptibleCpuCore`, `EmulatedComputer.Tick()` ustawia linie IRQ/NMI przed tickiem albo po ticku zgodnie z decyzją implementacyjną.

---

## 14. Bankowanie pamięci

### 14.1. Nie w MVP

Bankowanie pamięci nie jest potrzebne do minimalnego SBC, Apple-1 ani prostych profili.

### 14.2. Potrzebne później

| System | Powód |
|---|---|
| C64 | port 6510 `$0000/$0001`, ROM/RAM/I/O banking |
| C128 | banki i MMU |
| NES | mappery cartridge |
| Atari XL/XE | bankowanie RAM/ROM |
| 65C816 systems | adresowanie 24-bit |

### 14.3. Proponowany kontrakt późniejszy

```csharp
public interface IBankedMemoryController : IDevice
{
    bool TryTranslate(uint logicalAddress, out uint physicalAddress);
}
```

---

## 15. Pluginy i dynamiczne ładowanie

### 15.1. MVP

Na początku rejestracja fabryk w kodzie:

```csharp
services.AddSingleton<ICpuFactory, Mos6502CpuFactory>();
services.AddSingleton<IDeviceFactory, UartSimpleDeviceFactory>();
```

### 15.2. Później

Dynamiczne ładowanie assembly:

```text
plugins/cpu/*.dll
plugins/devices/*.dll
```

Kontrakt pluginu:

```csharp
public interface IEmulatorPlugin
{
    void Register(IEmulatorPluginRegistry registry);
}
```

### 15.3. Rejestr pluginów

```csharp
public interface IEmulatorPluginRegistry
{
    void RegisterCpuFactory(ICpuFactory factory);
    void RegisterDeviceFactory(IDeviceFactory factory);
}
```

---

## 16. Frontendy

Frontendy nie powinny być zależne od CPU. Powinny komunikować się przez adaptery:

| Frontend | Adapter |
|---|---|
| Terminal/TUI | `ITerminalLink` |
| WPF | terminal/display/audio adapters |
| Avalonia | terminal/display/audio adapters |
| Blazor | WebSocket/SignalR adapters |
| Testy | buffered adapters |

Dla Apple-1:

```text
Apple1PiaTerminalDevice -> IApple1Terminal -> frontend
```

Dla custom SBC:

```text
UartSimpleDevice -> ITerminalLink -> frontend/host
```

---

## 17. Walidacja profilu

Walidator powinien sprawdzać:

- brak konfliktów adresów memory-mapped,
- brak konfliktów portów,
- istnienie ROM file albo jawny tryb placeholder,
- zgodność typu mapowania z CPU,
- wymagane pola `id`, `type`, `clockHz`,
- poprawność liczb hex,
- brak nieznanych typów urządzeń,
- czy CPU obsługuje wymagane przestrzenie I/O.

Przykłady błędów:

```text
Device uart0 maps memory range $D000-$D001, but it overlaps ay0.
Device ay0 uses port mapping, but CPU mos6502 profile does not use port I/O.
ROM rom0 file not found: roms/custom-sbc/monitor.bin.
Unknown CPU type: z180.
```

---

## 18. Testy

### 18.1. Unit tests

- `ComputerProfileParserTests`
- `ComputerProfileValidatorTests`
- `SystemBusTests`
- `RuntimeBusTests`
- `CompiledMemoryMapTests`
- `CompiledPortMapTests`
- `MemoryRegionTests`
- `DeviceFactoryRegistryTests`
- `CpuFactoryRegistryTests`
- `ComputerBuilderTests`

### 18.2. Integracyjne

- `Build_Custom6502Sbc_CreatesCpuMemoryAndUart`
- `Build_Apple1_CreatesCpuRomRamAndPiaTerminal`
- `Build_Z80Sbc_CreatesPortMappedUart`
- `Build_ProfileWithOverlappingMemory_ThrowsValidationError`
- `Build_ProfileWithUnknownDevice_ThrowsValidationError`
- `Build_Profile_CompilesMemoryMap`
- `Build_Profile_CompilesPortMap_WhenPortDevicesExist`
- `Tick_UpdatesCpuAndCycleDevices`
- `Tick_AggregatesIrqSources`

### 18.3. Testy zgodności architektonicznej

- CPU nie referencjonuje nazw komputerów.
- Urządzenia nie zależą od konkretnego frontendu.
- Profile runtime są oddzielone od dokumentów planów.
- Z80 nie wymaga zmian w `Cpu6502`.
- Runtime bus nie iteruje po wszystkich urządzeniach przy każdym dostępie do pamięci.

---

## 19. Kolejność implementacji

### Etap 1 — kontrakty i bus

- [ ] Dodać `ICpuCore`.
- [ ] Dodać `ISystemBus`.
- [ ] Dodać `IDevice`.
- [ ] Dodać `IMemoryMappedDevice`.
- [ ] Dodać `IPortMappedDevice`.
- [ ] Dodać `ICycleDevice`.
- [ ] Dodać `IIrqSource` / `INmiSource`.
- [ ] Dodać `SystemBus`.

### Etap 2 — szybkie mapy runtime

- [ ] Dodać `RuntimeBus`.
- [ ] Dodać `CompiledMemoryMap`.
- [ ] Dodać `CompiledPortMap`.
- [ ] Dodać `IMemoryPageHandler`.
- [ ] Dodać `IPortHandler`.
- [ ] Dodać `FastRamRegion`.
- [ ] Dodać `FastRomRegion`.
- [ ] Dodać `NullPageHandler`.

### Etap 3 — profile runtime

- [ ] Dodać `ComputerProfile`.
- [ ] Dodać parser JSON.
- [ ] Dodać walidator.
- [ ] Dodać model `MemoryRegion`.
- [ ] Dodać `ComputerBuilder`.
- [ ] Dodać kompilację profilu do map runtime.

### Etap 4 — migracja 6502

- [ ] Owinąć istniejący `Cpu6502` w `ICpuCore`.
- [ ] Usunąć zależności CPU od konkretnego profilu maszyny.
- [ ] Podłączyć CPU do `ISystemBus` / `RuntimeBus`.
- [ ] Dodać test minimalnego profilu 6502.

### Etap 5 — minimalny SBC

- [ ] Dodać `UartSimpleDevice`.
- [ ] Dodać `ITerminalLink`.
- [ ] Dodać profil `minimal-sbc-6502.json`.
- [ ] Dodać test programu UART echo.

### Etap 6 — Apple-1

- [ ] Dodać `Apple1PiaTerminalDevice` albo `Mos6821PiaDevice`.
- [ ] Dodać profil `apple-1.json`.
- [ ] Uruchomić WOZ Monitor.

### Etap 7 — Z80 jako proof of architecture

- [ ] Dodać pusty/stub `CpuZ80Core`.
- [ ] Dodać `IPortMappedDevice` w praktyce.
- [ ] Dodać profil `custom-z80-sbc.json`.
- [ ] Dodać port-mapped UART.

### Etap 8 — większe systemy

- [ ] PET.
- [ ] KIM-1.
- [ ] VIC-20.
- [ ] C64.
- [ ] NES.
- [ ] Atari.

---

## 20. Decyzje architektoniczne

1. CPU nie zna komputerów.
2. Komputer jest składany z profilu.
3. Urządzenia są fabrykowane po `type`.
4. 6502 używa memory-mapped I/O.
5. Z80 używa memory + port I/O.
6. `ISystemBus` jest wspólną abstrakcją.
7. `RuntimeBus` jest szybką implementacją runtime.
8. `ComputerBuilder` kompiluje profil do `CompiledMemoryMap` i `CompiledPortMap`.
9. Bankowanie pamięci jest etapem późniejszym.
10. Dynamiczne pluginy są etapem późniejszym; najpierw rejestracja fabryk w kodzie.
11. Frontend nie jest częścią CPU ani urządzeń.
12. Dokumenty planów nie są profilami runtime.
13. Nie iterować po wszystkich urządzeniach w hot path odczytu/zapisu.

---

## 21. Definition of Done

Mechanizm składania komputerów można uznać za gotowy w wersji MVP, gdy:

- [ ] istnieje `ICpuCore`,
- [ ] istnieje `ISystemBus`,
- [ ] istnieje `RuntimeBus`,
- [ ] istnieje `CompiledMemoryMap`,
- [ ] istnieje `CompiledPortMap`,
- [ ] istnieją kontrakty urządzeń memory/port/cycle/irq,
- [ ] istnieje `ComputerProfile`,
- [ ] istnieje `ComputerBuilder`,
- [ ] profil `minimal-sbc-6502` buduje działającą maszynę,
- [ ] UART jest urządzeniem podłączanym przez profil,
- [ ] CPU 6502 nie zna UART ani nazw komputerów,
- [ ] walidator wykrywa konflikty mapowania,
- [ ] testy integracyjne potwierdzają składanie przynajmniej jednej maszyny 6502,
- [ ] architektura pozwala dodać Z80 bez modyfikowania `Cpu6502`,
- [ ] runtime bus nie iteruje po wszystkich urządzeniach przy każdym dostępie do pamięci.
