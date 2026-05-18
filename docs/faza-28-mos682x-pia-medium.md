# Faza 28 - MOS 6820/6821 PIA medium implementation

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Generyczny rownolegly uklad I/O |
| **Zaleznosci** | Fazy 24-27 |
| **Cel projektowy** | Jeden PIA dla Apple-1, PET-like profili i SBC |

---

## Cel fazy

Zaimplementowac srednio-dokladny `Mos682xPiaDevice`, ktory nie jest pelnym modelem tranzystorowym, ale jest wystarczajaco parametryzowalny, aby obsluzyc:

- Apple-1 terminal pod `$D010-$D013`,
- PET/PET-like konfiguracje PIA z inna adresacja,
- proste SBC z portami rownoleglymi,
- testowe urzadzenia z callbackami pinow.

Apple-1 nie dostaje osobnego, jednorazowego adaptera jako glownej implementacji. Dostaje preset/binding nad generyczna PIA.

---

## Rejestry

Standardowy uklad 4 rejestrow:

```text
base + 0  Port A data albo DDRA, zależnie od CRA bit 2
base + 1  CRA
base + 2  Port B data albo DDRB, zależnie od CRB bit 2
base + 3  CRB
```

Warianty adresacji musza byc konfigurowalne:

| Profil | Offset Port A | Offset CRA | Offset Port B | Offset CRB |
|--------|---------------|------------|---------------|------------|
| Apple-1 | 0 | 1 | 2 | 3 |
| PET-like | konfigurowalne | konfigurowalne | konfigurowalne | konfigurowalne |

Nie zakladac na stale `$D010`.

---

## Zakres medium

Implementowac:

- ORA/ORB output latch.
- DDRA/DDRB.
- CRA/CRB w zakresie wyboru DDR/data przez bit 2.
- Mieszanie odczytu: `(outputLatch & ddr) | (externalInput & ~ddr)`.
- Callbacki/piny zewnetrzne dla portu A i B.
- Proste flagi IRQ CA1/CB1 jako zapamietane bity w CRA/CRB.
- `ICpuSignalSource` dla IRQ, nawet jesli Apple-1 go nie uzywa w MVP.
- `Reset()` ustawiajacy latch/DDR/control do stanu poczatkowego.

Odlozyc:

- Pelny handshake CA2/CB2.
- Dokladne timingi przejsc pinow.
- Edge/level IRQ w pelnej zgodnosci z datasheetem.
- Emulacje analogowych efektow ukladu.

---

## Konfiguracja

```csharp
public sealed record Mos682xPiaOptions(
    string Variant,
    uint BaseAddress,
    PiaRegisterLayout Layout,
    IPiaPortBinding PortA,
    IPiaPortBinding PortB,
    bool EnableIrq);
```

```csharp
public interface IPiaPortBinding
{
    byte ReadPins();
    void WritePins(byte value, byte directionMask);
}
```

Apple-1 terminal ma byc bindingiem:

- Port A albo B zgodnie z presetem czyta klawiature.
- Drugi port zapisuje display/terminal.
- Status rejestrow moze byc wystawiony przez control bits/preset.

PET bedzie mogl uzyc innych bindingow: keyboard matrix, IEEE, cassette, user port.

---

## Kolejnosc wykonania dla agenta

1. Dodaj `Mos682xPiaDevice` w `src/Cpu6502/Devices/Pia`.
2. Dodaj `PiaRegisterLayout`.
3. Dodaj `IPiaPortBinding`, `BufferedPiaPortBinding` i testowe bindingi.
4. Dodaj `Mos682xPiaDeviceFactory` dla profili.
5. Dodaj preset `apple-1-terminal`, ale tylko jako konfiguracje/binding.
6. Dodaj testy rejestrow PIA.
7. Dodaj testy z dwoma roznymi bazowymi adresami, zeby uniknac zakodowania Apple-1.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `WriteDdra_WhenCraSelectsDdr_StoresDirection` | DDRA |
| `WritePortA_WhenCraSelectsData_UpdatesOutputLatch` | ORA |
| `ReadPortA_MergesOutputAndExternalInput` | mieszanie pinow |
| `WriteDdrB_AndPortB_BehaveLikePortA` | port B |
| `ControlBit2_SelectsDdrOrDataRegister` | selektor DDR/data |
| `Reset_ClearsDirectionAndOutputLatches` | reset |
| `Device_WithBaseD010_MapsApple1Offsets` | Apple-1 adresacja |
| `Device_WithDifferentBase_MapsSameRegisters` | reuse dla innej maszyny |
| `Factory_CreatesMos6821FromProfile` | integracja z profilami |
| `IrqSource_WhenEnabledFlagSet_AssertsIrq` | minimalne IRQ |

---

## Poza zakresem

- Pelny PET.
- Pelny Apple-1 WOZ Monitor.
- VIA 6522.
- CIA 6526.
- Cyklowa dokladnosc PIA na poziomie perfect6502.

---

## Kryteria akceptacji

- Ta sama klasa PIA dziala z co najmniej dwoma bazowymi adresami.
- Apple-1 jest presetem/bindingiem, nie osobnym hardcoded urzadzeniem.
- Testy nie odwolują sie do stalej `$D010`, poza testem presetowym Apple-1.
