# Apple-1 PIA Terminal Device — plan implementacji

## Status

Na ten moment repozytorium nie zawiera kompletnej implementacji PIA dla Apple-1. Wymagany jest co najmniej uproszczony adapter terminala Apple-1 mapowany na zakres `$D010-$D013`, a docelowo generyczna implementacja MOS 6820/6821 PIA.

## Cel

Celem jest uruchomienie Apple-1 WOZ Monitor z poprawną obsługą klawiatury i wyjścia znakowego. Minimalna implementacja nie musi od razu emulować pełnej semantyki układu MOS 6820/6821, ale musi zachować kompatybilność adresową i zachowanie oczekiwane przez monitor Apple-1.

## Zakres adresów Apple-1

| Adres | Nazwa | Kierunek | Znaczenie |
|---|---:|---|---|
| `$D010` | `KBD` | read | Dane klawiatury. Znak z ustawionym bitem 7, gdy znak jest gotowy. |
| `$D011` | `KBDCR` | read/write | Rejestr kontrolny klawiatury. W uproszczeniu może zwracać stan gotowości. |
| `$D012` | `DSP` | write | Dane wyświetlacza. Zapisany znak trafia do terminala. |
| `$D013` | `DSPCR` | read/write | Rejestr kontrolny wyświetlacza. W uproszczeniu może zwracać gotowość wyświetlacza. |

## Wariant minimalny

Dodać urządzenie specyficzne dla Apple-1:

```csharp
public sealed class Apple1PiaTerminalDevice : IMemoryMappedDevice
{
    public ushort StartAddress => 0xD010;
    public ushort EndAddress => 0xD013;

    public byte Read(ushort address);
    public void Write(ushort address, byte value);
}
```

Minimalna logika:

- `Read(0xD010)` zwraca kolejny znak z bufora klawiatury z ustawionym bitem 7.
- `Read(0xD011)` zwraca status klawiatury, np. bit gotowości, jeżeli bufor nie jest pusty.
- `Write(0xD012, value)` przekazuje znak do terminala po wyczyszczeniu bitu 7, jeżeli jest ustawiony.
- `Read(0xD013)` zwraca gotowość wyświetlacza.
- `Write(0xD011, value)` i `Write(0xD013, value)` mogą na początku zapamiętywać wartość, bez pełnej emulacji linii kontrolnych.

## Interfejs terminala

Aby nie wiązać PIA z konkretnym frontendem, dodać mały port terminalowy:

```csharp
public interface IApple1Terminal
{
    bool HasInput { get; }
    byte ReadInput();
    void WriteOutput(byte value);
}
```

Implementacje frontendu mogą mapować ten interfejs na:

- TUI / terminal konsolowy,
- WPF,
- Avalonia,
- Blazor,
- testowy bufor wejścia/wyjścia.

## Testy jednostkowe

Dodać testy MSTest + FluentAssertions.

Przypadki minimalne:

1. `Read_Kbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet`
2. `Read_KbdCr_WhenInputAvailable_ReturnsReadyStatus`
3. `Read_KbdCr_WhenNoInput_ReturnsNotReadyStatus`
4. `Write_Dsp_WritesCharacterToTerminal`
5. `Write_Dsp_StripsHighBitBeforeOutput`
6. `Device_AddressRange_IsD010ToD013`

Przykładowy kierunek testu:

```csharp
[TestMethod]
public void Read_Kbd_WhenInputAvailable_ReturnsCharacterWithHighBitSet()
{
    var terminal = new FakeApple1Terminal("A");
    var device = new Apple1PiaTerminalDevice(terminal);

    var value = device.Read(0xD010);

    value.Should().Be(0xC1);
}
```

## Integracja z profilem Apple-1

Profil Apple-1 powinien zawierać urządzenie mapowane na `$D010-$D013`.

Przykładowy wpis koncepcyjny:

```json
{
  "type": "apple1-pia-terminal",
  "start": "0xD010",
  "size": "0x0004"
}
```

Loader profilu powinien umieć utworzyć `Apple1PiaTerminalDevice` na podstawie tego wpisu i podłączyć go do magistrali pamięci.

## Etap docelowy: MOS 6821 PIA

Po uruchomieniu Apple-1 warto wydzielić pełniejszy układ:

```csharp
public sealed class Mos6821PiaDevice : IMemoryMappedDevice, IIrqSource
{
    // Port A/B
    // DDR A/B
    // CRA/CRB
    // CA1/CA2/CB1/CB2
}
```

Apple-1 może wtedy używać konfiguracji PIA zamiast dedykowanego uproszczenia. Nie należy jednak blokować uruchomienia WOZ Monitor do czasu pełnej emulacji MOS 6821.

## Kolejność prac

1. Dodać `IApple1Terminal`.
2. Dodać `Apple1PiaTerminalDevice`.
3. Dodać testowy terminal buforowy do testów.
4. Dodać testy MSTest/Moq/FluentAssertions.
5. Podłączyć urządzenie do loadera profilu.
6. Uzupełnić `profiles/apple-1.json` o urządzenie `apple1-pia-terminal`.
7. Uruchomić Apple-1 z WOZ Monitor i zweryfikować prompt oraz echo znaków.
8. Dopiero potem rozważyć pełny `Mos6821PiaDevice`.

## Kryteria akceptacji

- Apple-1 startuje bez błędu mapowania urządzeń.
- WOZ Monitor może odczytać znak z klawiatury przez `$D010/$D011`.
- WOZ Monitor może wypisać znak przez `$D012/$D013`.
- Testy jednostkowe pokrywają minimalną semantykę urządzenia.
- Implementacja nie zależy bezpośrednio od konkretnego frontendu.
