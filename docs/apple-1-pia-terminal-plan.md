# Apple-1 PIA Terminal Device - plan implementacji

## Status

Repozytorium nie zawiera jeszcze kompletnej implementacji PIA dla Apple-1. Aktualny kierunek nie zaklada jednorazowego adaptera Apple-1 jako glownego urzadzenia. Wlasciwa implementacja powinna zaczac sie od srednio-dokladnego, parametryzowanego `Mos682xPiaDevice`, ktory Apple-1 wykorzysta przez preset/binding terminalowy mapowany na `$D010-$D013`.

## Cel

Celem jest uruchomienie Apple-1 WOZ Monitor z poprawna obsluga klawiatury i wyjscia znakowego, ale bez zamykania architektury na Apple-1. Ten sam rdzen PIA powinien byc mozliwy do uzycia w profilach PET-like, SBC i innych komputerach z inna adresacja oraz innymi bindingami portow.

## Zakres adresow Apple-1

| Adres | Nazwa | Kierunek | Znaczenie |
|---|---:|---|---|
| `$D010` | `KBD` | read | Dane klawiatury. Znak z ustawionym bitem 7, gdy znak jest gotowy. |
| `$D011` | `KBDCR` | read/write | Rejestr kontrolny klawiatury/status gotowosci. |
| `$D012` | `DSP` | write | Dane wyswietlacza. Zapisany znak trafia do terminala. |
| `$D013` | `DSPCR` | read/write | Rejestr kontrolny wyswietlacza/status gotowosci. |

## Wariant medium reusable

Dodać generyczne urządzenie PIA:

```csharp
public sealed class Mos682xPiaDevice : IMemoryMappedDevice, IResettableDevice, ICpuSignalSource
{
    public uint StartAddress { get; }
    public uint Size => 4;

    public byte ReadMemory(uint address);
    public void WriteMemory(uint address, byte value);
    public void Reset();
}
```

Logika wymagana w wersji medium:

- `ORA/ORB` i `DDRA/DDRB`,
- `CRA/CRB` w zakresie wyboru DDR/data register przez bit 2,
- mieszanie odczytu portu: `(outputLatch & ddr) | (externalInput & ~ddr)`,
- callbacki/bindingi pinow zewnetrznych przez `IPiaPortBinding`,
- konfigurowalny layout rejestrow i adres bazowy,
- minimalne IRQ jako `ICpuSignalSource`,
- preset `apple-1-terminal`,
- drugi binding PET-like z inna adresacja, aby potwierdzic reuse.

Nie wymagamy w tej fazie pelnego handshake CA2/CB2 ani dokladnosci tranzystorowej.

## Terminal/link bajtowy

Aby nie wiazac PIA z konkretnym frontendem, terminal powinien byc neutralnym linkiem bajtowym:

```csharp
public interface ITerminalLink
{
    bool HasInput { get; }
    bool TryReadByte(out byte value);
    void WriteByte(byte value);
}
```

Implementacje frontendu moga mapowac ten interfejs na:

- TUI / terminal konsolowy,
- WPF,
- Avalonia,
- Blazor,
- testowy bufor wejscia/wyjscia.

## Testy jednostkowe

Przypadki wymagane dla PIA i presetow:

1. `WriteDdra_WhenCraSelectsDdr_StoresDirection`
2. `WritePortA_WhenCraSelectsData_UpdatesOutputLatch`
3. `ReadPortA_MergesOutputAndExternalInput`
4. `Device_WithBaseD010_MapsApple1Offsets`
5. `Device_WithDifferentBase_MapsSameRegisters`
6. `Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet`
7. `Apple1Preset_ReadKbdCr_WhenInputAvailable_ReturnsReadyStatus`
8. `Apple1Preset_WriteDsp_StripsHighBitBeforeOutput`
9. `PetLikeBinding_UsesSamePiaWithDifferentAddress`

Przykladowy kierunek testu:

```csharp
[Test]
public void Apple1Preset_ReadKbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
{
    var terminal = new BufferedTerminalLink();
    terminal.EnqueueText("A", TerminalTextEncoding.Apple1);
    var device = Mos682xPiaDeviceFactory.CreateApple1Terminal(0xD010, terminal);

    var value = device.ReadMemory(0xD010);

    Assert.That(value, Is.EqualTo(0xC1));
}
```

## Integracja z profilem Apple-1

Profil Apple-1 powinien zawierac generyczna PIA mapowana na `$D010-$D013`.

Przykladowy wpis koncepcyjny:

```json
{
  "id": "pia0",
  "type": "mos6821-pia",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0xD010",
    "size": "0x0004"
  },
  "preset": "apple-1-terminal"
}
```

Loader profilu powinien utworzyc `Mos682xPiaDevice` na podstawie tego wpisu i podlaczyc go do magistrali pamieci. Preset `apple-1-terminal` konfiguruje bindingi, ale nie zmienia rdzenia PIA w urzadzenie jednorazowe.

## Reuse poza Apple-1

PIA musi przejsc test drugiego profilu walidacyjnego:

- inny adres bazowy niz `$D010`,
- inny binding portow,
- brak zaleznosci od WOZ Monitor,
- brak klas lub warunkow `if Apple1` w rdzeniu PIA.

Najblizszy drugi scenariusz to PET-like keyboard matrix, opisany w fazie 30.

## Kolejnosc prac

1. Dodać uniwersalne abstrakcje runtime i bus.
2. Dodać `ITerminalLink` oraz buforowany terminal testowy.
3. Dodać `Mos682xPiaDevice` medium implementation.
4. Dodać preset `apple-1-terminal`.
5. Dodać drugi binding PET-like, aby potwierdzic reuse.
6. Podlaczyc PIA do loadera profilu.
7. Uzupelnic `profiles/computers/apple-1.json` o urzadzenie `mos6821-pia` z presetem `apple-1-terminal`.
8. Uruchomic Apple-1 z WOZ Monitor i zweryfikowac prompt oraz echo znakow.

## Fazy implementacyjne

Pelna implementacja Apple-1 wymaga najpierw warstwy skladania komputerow i reusable PIA. Szczegolowe fazy sa opisane w osobnych plikach:

| Faza | Dokument | Zakres |
|---:|---|---|
| 24 | [`faza-24-runtime-abstractions.md`](faza-24-runtime-abstractions.md) | Uniwersalne abstrakcje runtime dla wielu CPU |
| 25 | [`faza-25-system-bus-memory-map.md`](faza-25-system-bus-memory-map.md) | `RuntimeBus`, memory map, port map, szybki routing |
| 26 | [`faza-26-computer-profiles.md`](faza-26-computer-profiles.md) | Profile JSON, loader, `ComputerBuilder`, rejestr fabryk |
| 27 | [`faza-27-terminal-abstractions.md`](faza-27-terminal-abstractions.md) | Terminal/link bajtowy niezalezny od frontendu |
| 28 | [`faza-28-mos682x-pia-medium.md`](faza-28-mos682x-pia-medium.md) | Generyczny MOS 6820/6821 PIA reusable dla Apple-1/PET/SBC |
| 29 | [`faza-29-apple1-profile-wozmon.md`](faza-29-apple1-profile-wozmon.md) | Apple-1 jako profil na generycznej PIA |
| 30 | [`faza-30-pet-ready-pia-bindings.md`](faza-30-pet-ready-pia-bindings.md) | PET-ready bindingi PIA i drugi profil walidacyjny |
| 31 | [`faza-31-apple1-runtime-api.md`](faza-31-apple1-runtime-api.md) | Publiczne API uruchamiania Apple-1 i test end-to-end |
| 32 | [`faza-32-cross-architecture-smoke-profiles.md`](faza-32-cross-architecture-smoke-profiles.md) | Profile smoke dla wielu architektur, port-mapped I/O |

## Kryteria akceptacji

- Apple-1 startuje bez bledu mapowania urzadzen.
- WOZ Monitor moze odczytac znak z klawiatury przez `$D010/$D011`.
- WOZ Monitor moze wypisac znak przez `$D012/$D013`.
- Testy jednostkowe pokrywaja srednia semantyke PIA oraz preset Apple-1.
- Ten sam rdzen PIA jest uzyty w drugim profilu walidacyjnym z inna adresacja.
- Implementacja nie zalezy bezposrednio od konkretnego frontendu.
