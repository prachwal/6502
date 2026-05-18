# Faza 24 - Uniwersalne abstrakcje runtime i API CPU

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Fundament runtime dla wielu architektur |
| **Zaleznosci** | Fazy: 0-23 |
| **Cel projektowy** | Nie przywiazywac warstwy komputerow do MOS 6502 |

---

## Cel fazy

Zbudowac minimalny, ale wielokrotnego uzytku kontrakt runtime, ktory obsluzy nie tylko 6502/6510/65C02, ale tez Z80/8080, 6800/6809 i przyszle CPU z osobna przestrzenia portow albo szersza przestrzenia adresowa.

Ta faza nie implementuje Apple-1. Ona przygotowuje podloge, zeby Apple-1, PET, KIM-1, minimalny SBC, a pozniej Z80/CP-M mogly uzyc tych samych pojec: CPU, bus, urzadzenie, sygnal, snapshot.

---

## Decyzje architektoniczne

1. Publiczny runtime uzywa `uint` dla adresow. Adapter 6502 moze nadal konwertowac do `ushort`.
2. `Tick()` i `StepInstruction()` musza miec jawna semantyke. Nie wolno nazywac kroku instrukcji pojedynczym cyklem.
3. Linie CPU sa modelowane generycznie przez `CpuSignal`, a nie tylko przez `SetIRQ()` / `SetNMI()`.
4. Snapshot CPU jest slownikowy, bo rejestry Z80, 6809 i 68000 nie zmieszcza sie w `CpuState`.
5. Urzadzenia nie znaja klasy CPU. Moga wystawiac zrodla sygnalow albo byc odpytywane przez kontroler sygnalow.

---

## Kontrakty do zaimplementowania

```csharp
public interface ICpuCore
{
    string CpuType { get; }
    AddressSpaceDescriptor AddressSpace { get; }
    void Reset();
    void StepInstruction();
    void StepCycle();
    CpuSnapshot GetSnapshot();
}
```

```csharp
public interface ICpuSignalSink
{
    void SetSignal(CpuSignal signal, bool asserted);
}
```

```csharp
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

```csharp
public sealed record AddressSpaceDescriptor(
    int MemoryAddressBits,
    int PortAddressBits,
    bool HasSeparatePortSpace,
    int DataBusBits);
```

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

```csharp
public interface IDevice
{
    string Id { get; }
}

public interface IResettableDevice : IDevice
{
    void Reset();
}

public interface ICycleDevice : IDevice
{
    void Tick(long cycles);
}

public interface ICpuSignalSource : IDevice
{
    bool IsAsserted(CpuSignal signal);
}
```

---

## Adapter 6502

Dodac `Cpu6502CoreAdapter`, ktory opakowuje istniejacy `Cpu6502`:

- `StepInstruction()` deleguje do obecnego `Cpu6502.StepInstruction()`.
- `StepCycle()` na poczatku moze rzucac `NotSupportedException`, jezeli rdzen nadal wykonuje pelne instrukcje.
- `ICpuSignalSink.SetSignal(Irq, value)` mapuje na `SetIRQ(value)`.
- `ICpuSignalSink.SetSignal(Nmi, true/false)` mapuje na obecne zbocze NMI zgodnie z semantyka rdzenia.
- `GetSnapshot()` mapuje `A`, `X`, `Y`, `P`, `SP`, `PC`.

Nie przepisywac rdzenia CPU w tej fazie.

---

## Kolejnosc wykonania dla agenta

1. Utworz katalog `src/Cpu6502/System`.
2. Dodaj kontrakty bez logiki runtime.
3. Dodaj `Cpu6502CoreAdapter`.
4. Dodaj `CpuSignalController`, ktory zbiera sygnaly po `sourceId`.
5. Dodaj testy kontraktow i adaptera.
6. Nie ruszaj istniejacych testow CPU poza koniecznymi namespace/import.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `Cpu6502CoreAdapter_Reset_UsesWrappedCpu` | reset ustawia PC z wektora |
| `Cpu6502CoreAdapter_StepInstruction_ExecutesOneInstruction` | krok wykonuje jedna instrukcje |
| `Cpu6502CoreAdapter_GetSnapshot_Contains6502Registers` | snapshot zawiera `A`, `X`, `Y`, `P`, `SP`, `PC` |
| `CpuSignalController_SetSource_AggregatesSignals` | wiele zrodel moze utrzymywac jedna linie |
| `CpuSignalController_ClearOneSource_KeepsSignalWhenOtherSourceActive` | linia opada dopiero po zwolnieniu wszystkich zrodel |
| `Cpu6502CoreAdapter_SetIrq_MapsToCpu` | IRQ jest propagowane |
| `Cpu6502CoreAdapter_SetNmi_MapsEdgeCorrectly` | NMI zachowuje semantyke zbocza |

---

## Poza zakresem

- `SystemBus`, RAM/ROM, profile JSON.
- Apple-1, PET, PIA, UART.
- Pelny cycle-stepped refactor rdzenia CPU.
- Z80/8080 implementacja CPU.

---

## Kryteria akceptacji

- `dotnet test` przechodzi.
- Istnieje adapter `Cpu6502CoreAdapter`.
- Nowe kontrakty nie zawieraja typow specyficznych dla 6502 poza adapterem.
- API runtime pozwala opisac CPU z portami I/O i CPU bez portow.

