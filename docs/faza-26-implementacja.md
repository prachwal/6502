# Faza 26 - Implementacja: Profile komputerów, fabryki i builder

| Właściwość | Wartość |
|------------|---------|
| **Status** | ✅ Zaimplementowana |
| **Data implementacji** | 2025-05-18 |
| **Zależności** | Fazy 24-25 |
| **Liczba testów** | 27 |

---

## Podsumowanie

Faza 26 zaimplementowała system profili komputerów, który umożliwia:
- Definiowanie komputerów jako danych (JSON)
- Rejestrację fabryk CPU i urządzeń
- Budowanie pełnych systemów emulowanych z profili
- Obsługę zarówno memory-mapped (6502) jak i port-mapped (Z80) I/O

---

## Zaimplementowane komponenty

### 1. Profile (src/Cpu6502/System/Profiles/)

#### AddressParser.cs
- Statyczna klasa do parsowania adresów w formacie hex (`0xD010`) i decimal (`53248`)
- Metody: `Parse()`, `ParseOrDefault()`, `TryParse()`

#### AddressSpaceProfile.cs
- Record definujący przestrzeń adresową CPU
- Pola: `MemoryAddressBits`, `PortAddressBits`, `HasSeparatePortSpace`, `DataBusBits`
- Statyczne instancje: `Mos6502`, `Z80`
- Walidacja profilu

#### CpuProfile.cs
- Record definujący profil CPU
- Pola: `Type`, `ClockHz`, `InitialPC`
- Statyczne instancje: `Mos6502Nmos`, `Z80`
- Walidacja profilu

#### MemoryRegionProfile.cs
- Record bazowy dla regionów pamięci
- Pola: `Id`, `Start`, `Size`
- Właściwości obliczane: `ParsedStart`, `ParsedSize`, `EndAddress`

#### RamRegionProfile.cs
- Record dla regionów RAM
- Dodatkowe pole: `FillValue` (domyślnie 0)

#### RomRegionProfile.cs
- Record dla regionów ROM
- Dodatkowe pola: `File`, `WritePolicy` (domyślnie `ThrowException`)

#### MemoryProfile.cs
- Record agregujący regiony RAM i ROM
- Walidacja: wykrywanie nakładających się regionów

#### DeviceMappingProfile.cs
- Record definujący mapowanie urządzenia
- Pola: `Kind` (Memory/Port), `BaseAddress`, `Size`
- Właściwości obliczane: `ParsedBaseAddress`, `ParsedSize`, `EndAddress`

#### DeviceProfile.cs
- Record definujący profil urządzenia
- Pola: `Id`, `Type`, `Mapping`, `Bindings`, `Options`
- Walidacja: sprawdza poprawność mapowania (port-mapped wymaga `HasSeparatePortSpace`)

#### ComputerProfile.cs
- Record głównego profilu komputera
- Pola: `Schema`, `Id`, `Name`, `Status`, `Cpu`, `AddressSpace`, `Memory`, `Devices`
- Stała: `SchemaV1 = "computer-profile/v1"`
- Walidacja: sprawdza wszystkie pod-profile i unikalność ID urządzeń

#### ComputerProfileLoader.cs
- Klasa ładująca profile z JSON
- Metody: `LoadFromString()`, `LoadFromFile()`
- Obsługa: `ProfileLoadOptions` (overrides ROM, base path)
- Wyjątki: `ComputerProfileValidationException`

### 2. Fabryki (src/Cpu6502/System/Factories/)

#### ICpuFactory.cs
- Interfejs fabryki CPU
- Metoda: `CreateCpu(CpuProfile, AddressSpaceDescriptor, IMemoryBus?)`

#### IDeviceFactory.cs
- Interfejs fabryki urządzeń
- Metoda: `CreateDevice(DeviceProfile, ISystemBus, ProfileLoadOptions?)`

#### DeviceFactoryRegistry.cs
- Rejestr fabryk CPU i urządzeń
- Metody rejestracji: `RegisterCpuFactory()`, `RegisterDeviceFactory()`
- Metody tworzenia: `CreateCpu()`, `CreateDevice()`
- Właściwości: `RegisteredCpuTypes`, `RegisteredDeviceTypes`
- Statyczna instancja: `Default`
- Klasy wewnętrzne: `DelegateCpuFactory`, `DelegateDeviceFactory`

#### Mos6502CpuFactory.cs
- Fabryka CPU MOS 6502
- Stałe: `CpuTypeMos6502Nmos`, `CpuTypeMos6502`, `CpuTypeMos6510`

#### FakeDeviceFactory.cs
- Fabryka urządzeń do testów
- Tworzy: `FakeMemoryMappedDevice`, `FakePortMappedDevice`
- Śledzenie: `MemoryDevice`, `PortDevice`, `LastCreatedDevice`

### 3. Builder (src/Cpu6502/System/Builder/)

#### ComputerBuilder.cs
- Buduje komputer z profilu
- Metody: `Build(ComputerProfile, ProfileLoadOptions?)`
- Metody statyczne: `BuildFromProfile()`, `BuildFromJson()`, `BuildFromFile()`
- Wyjątki: `ComputerBuildException`

#### EmulatedComputer.cs
- Klasa reprezentująca zbudowany komputer
- Implementuje: `IDevice`
- Właściwości: `Id`, `Name`, `Cpu`, `Bus`, `Devices`
- Metody: `GetDevice()`, `GetDevice<T>()`, `GetDevices<T>()`
- Metody operacyjne: `Reset()`, `StepInstruction()`, `StepCycle()`, `ReadMemory()`, `WriteMemory()`, `ReadPort()`, `WritePort()`, `GetCpuSnapshot()`, `Run()`, `RunCycles()`

### 4. Profile (profiles/computers/)

#### minimal-6502-sbc.json
- Przykładowy profil minimalnego SBC z 6502
- 32KB RAM, 4KB ROM
- Brak urządzeń

---

## Testy (tests/Cpu6502.Tests/System/Faza26ComputerProfilesTests.cs)

### AddressParser (4 testy)
- `AddressParser_ParsesHexAddresses` - parsowanie hex
- `AddressParser_ParsesDecimalAddresses` - parsowanie decimal
- `AddressParser_ThrowsOnEmptyString` - obsługa pustego stringa
- `AddressParser_ThrowsOnInvalidFormat` - obsługa nieprawidłowego formatu

### Profile Models (6 testów)
- `AddressSpaceProfile_ValidatesMemoryBits` - walidacja poprawna
- `AddressSpaceProfile_ThrowsOnInvalidMemoryBits` - błędy walidacji
- `AddressSpaceProfile_ThrowsOnPortBitsWithoutSeparateSpace` - porty wymagają oddzielnej przestrzeni
- `CpuProfile_ValidatesTypeAndClock` - walidacja CPU
- `CpuProfile_ThrowsOnEmptyType` - puste pole Type
- `CpuProfile_ThrowsOnZeroClock` - zero ClockHz
- `MemoryRegionProfile_ParsesStartAndSize` - parsowanie regionów
- `MemoryProfile_DetectsOverlappingRegions` - wykrywanie nakładania

### ComputerProfileLoader (3 testy)
- `LoadProfile_ParsesMinimalProfile` - parsowanie pełnego profilu
- `LoadProfile_ParsesHexAddresses` - parsowanie adresów hex
- `LoadProfile_WithDevices_ParsesDeviceMapping` - parsowanie urządzeń

### ComputerBuilder (4 testy)
- `Build_Minimal6502Profile_CreatesComputer` - budowanie minimalnego komputera
- `Build_UnknownCpuType_ThrowsValidationError` - brak fabryki CPU
- `Build_UnknownDeviceType_ThrowsValidationError` - brak fabryki urządzenia
- `Build_OverlappingMemoryRanges_ThrowsValidationError` - nakładające się regiony pamięci

### Factory Registry (2 testy)
- `Registry_CreatesCpuWithRegisteredFactory` - tworzenie CPU z fabryki
- `Registry_ThrowsWhenCpuFactoryNotRegistered` - brak zarejestrowanej fabryki

### EmulatedComputer (2 testy)
- `EmulatedComputer_ReadWriteMemory` - odczyt/zapis pamięci
- `EmulatedComputer_StepInstruction_Works` - wykonanie instrukcji

### FakeDeviceFactory (2 testy)
- `FakeDeviceFactory_CreatesMemoryMappedDevice` - tworzenie urządzenia memory-mapped
- `FakeDeviceFactory_CreatesPortMappedDevice` - tworzenie urządzenia port-mapped

### Z80-style Profiles (2 testy)
- `Build_Z80StylePortDevice_WithRegisteredFactory` - budowanie z port-mapped devices
- `Build_PortDeviceOnCpuWithoutPorts_ThrowsValidationError` - walidacja port/CPU compatibility

---

## Decyzje projektowe

### 1. Format profili
- JSON zamiast XML/YAML dla lepszej integracji z .NET
- Schema versioning (`computer-profile/v1`) dla przyszłych rozszerzeń
- Adresy w formacie hex (`0xD010`) zamiast decimal dla czytelności

### 2. Walidacja
- Walidacja na poziomie modeli (validate methods)
- Walidacja podczas ładowania (ComputerProfileLoader)
- Walidacja podczas budowania (ComputerBuilder)
- Czytelne wiadomości błędów z `profileId`

### 3. Fabryki
- Interfejsy `ICpuFactory` i `IDeviceFactory` dla extensibility
- `DeviceFactoryRegistry` jako singleton z możliwością tworzenia instancji
- Obsługa zarówno funkcji fabryk jak i klas fabryk
- Przekazywanie `IMemoryBus` do fabryki CPU (6502 wymaga bus)

### 4. Builder
- Oddzielenie budowania od profili
- `ComputerBuilder` przyjmuje `DeviceFactoryRegistry` w konstruktorze
- Metody statyczne dla prostych przypadków użycia
- `EmulatedComputer` jako immutable po zbudowaniu

### 5. Obsługa port-mapped I/O
- `AddressSpaceProfile.HasSeparatePortSpace` determinuje czy CPU obsługuje porty
- `DeviceMappingProfile.Kind` określ czy urządzenie jest memory-mapped czy port-mapped
- Walidacja: port-mapped device wymaga CPU z `HasSeparatePortSpace = true`
- `RuntimeBus` obsługuje zarówno memory jak i port space

---

## Integracja z poprzednimi fazami

### Faza 24 (Runtime Abstractions)
- `ICpuCore` - używane jako interfejs dla CPU zwracanego przez fabryki
- `AddressSpaceDescriptor` - używane do opisu przestrzeni adresowej
- `Cpu6502CoreAdapter` - adapter dla istniejących CPU 6502

### Faza 25 (System Bus)
- `ISystemBus` - używane przez urządzenia i ComputerBuilder
- `RuntimeBus` - główna implementacja magistrali
- `CompiledMemoryMap` - używana przez ComputerBuilder do mapowania pamięci
- `CompiledPortMap` - używana dla port-mapped devices
- `IMemoryBus` - interfejs dla CPU (6502 używa go w konstruktorze)
- `IMemoryMappedDevice`, `IPortMappedDevice` - interfejsy urządzeń

---

## Przykład użycia

```csharp
// 1. Zarejestruj fabryki
var registry = new DeviceFactoryRegistry();
registry.RegisterCpuFactory("mos6502-nmos", (profile, addr, bus) =>
    new Cpu6502CoreAdapter(new Cpu6502(bus), "mos6502-nmos"));

// 2. Załaduj profil
var loader = new ComputerProfileLoader();
var profile = loader.LoadFromFile("profiles/computers/minimal-6502-sbc.json");

// 3. Zbuduj komputer
var builder = new ComputerBuilder(registry);
var computer = builder.Build(profile);

// 4. Użyj komputera
computer.WriteMemory(0x0000, 0xA9); // LDA #imm
computer.WriteMemory(0x0001, 0x01);
computer.StepInstruction();

// 5. Pobierz snapshot
var snapshot = computer.GetCpuSnapshot();
Console.WriteLine($