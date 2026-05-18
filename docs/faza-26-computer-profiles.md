# Faza 26 - Profile komputerow, fabryki i builder

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [x] Zaimplementowana |
| **Zakres** | Konfiguracja maszyn jako dane |
| **Zaleznosci** | Fazy 24-25 |
| **Cel projektowy** | Budowac komputery z profili, nie z klas `Apple1Emulator` |

---

## Cel fazy

Dodac profile runtime dla komputerow i `ComputerBuilder`, ktory z profilu sklada CPU, RAM, ROM, urzadzenia, mapy pamieci, mapy portow i kontroler sygnalow.

Ta faza musi byc gotowa na rozne architektury: 6502/PET/Apple-1, 6510/C64, Z80/CP-M, 6809 i inne.

---

## Schemat profilu

Minimalny profil:

```json
{
  "schema": "computer-profile/v1",
  "id": "apple-1",
  "name": "Apple-1",
  "status": "planned",
  "cpu": {
    "type": "mos6502-nmos",
    "clockHz": 1023000
  },
  "addressSpace": {
    "memoryAddressBits": 16,
    "portAddressBits": 0,
    "hasSeparatePortSpace": false,
    "dataBusBits": 8
  },
  "memory": {
    "ram": [
      { "id": "ram0", "start": "0x0000", "size": "0x1000" }
    ],
    "rom": [
      { "id": "wozmon", "start": "0xFF00", "size": "0x0100", "file": "roms/apple-1/wozmon.bin" }
    ]
  },
  "devices": [
    {
      "id": "pia0",
      "type": "mos6821-pia",
      "mapping": { "kind": "memory", "baseAddress": "0xD010", "size": "0x0004" },
      "bindings": { "preset": "apple-1-terminal" }
    }
  ]
}
```

Profil Z80 musi moc uzyc `mapping.kind = "port"`.

---

## Komponenty do zaimplementowania

- `ComputerProfile`
- `CpuProfile`
- `AddressSpaceProfile`
- `MemoryProfile`
- `DeviceProfile`
- `DeviceMappingProfile`
- `ComputerProfileLoader`
- `ICpuFactory`
- `IDeviceFactory`
- `DeviceFactoryRegistry`
- `ComputerBuilder`
- `EmulatedComputer`

---

## Reguly walidacji

1. `schema` musi byc znane.
2. `id` nie moze byc pusty.
3. `cpu.type` musi miec zarejestrowana fabryke.
4. Kazdy `device.type` musi miec zarejestrowana fabryke.
5. `mapping.kind = "port"` wymaga `addressSpace.hasSeparatePortSpace = true`.
6. Konflikty RAM/ROM/device w pamieci sa bledem.
7. Konflikty portow sa bledem.
8. Plik ROM moze byc wymagany albo zastapiony testowym `byte[]` przez `ProfileLoadOptions`.
9. Profile runtime leza w `profiles/computers`, nie w `docs`.

---

## Kolejnosc wykonania dla agenta

1. Dodaj modele profili jako immutable record/class.
2. Dodaj parser liczb hex/dec (`0xD010`, `53248`, etc.).
3. Dodaj loader JSON oparty o `System.Text.Json`.
4. Dodaj rejestry fabryk CPU i urzadzen.
5. Dodaj `ComputerBuilder`, ktory buduje najpierw regiony pamieci, potem urzadzenia, potem mapy, potem CPU.
6. Dodaj testowy `FakeDeviceFactory`.
7. Dodaj profil `profiles/computers/minimal-6502-sbc.json` jako smoke profile bez Apple-1.
8. Nie dodawaj jeszcze PIA ani Apple-1 runtime.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `LoadProfile_ParsesHexAddresses` | parser adresow dziala |
| `Build_Minimal6502Profile_CreatesComputer` | minimalny profil buduje runtime |
| `Build_UnknownCpuType_ThrowsValidationError` | brak fabryki CPU daje blad |
| `Build_UnknownDeviceType_ThrowsValidationError` | brak fabryki urzadzenia daje blad |
| `Build_OverlappingMemoryRanges_ThrowsValidationError` | konflikt pamieci |
| `Build_PortDeviceOnCpuWithoutPorts_ThrowsValidationError` | porty wymagaja CPU z port space |
| `Build_Z80StylePortDevice_CompilesPortMap` | model portow jest gotowy |

---

## Poza zakresem

- Konkretny Apple-1.
- Konkretny PET.
- PIA/VIA/UART.
- UI/frontend.

---

## Kryteria akceptacji

- Builder potrafi zbudowac neutralny komputer 6502 z RAM/ROM.
- Profil Z80 z portami przechodzi walidacje struktury przy zarejestrowanych fake fabrykach.
- Bledy profilu sa czytelne i zawieraja `profileId`, `deviceId` lub zakres konfliktu.

