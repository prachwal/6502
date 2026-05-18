# Faza 29 - Apple-1 jako profil na generycznej PIA

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Pierwszy historyczny profil komputera |
| **Zaleznosci** | Fazy 24-28 |
| **Cel projektowy** | Apple-1 jako konfiguracja runtime, nie specjalny emulator |

---

## Cel fazy

Zbudowac profil Apple-1 uzywajacy `Mos682xPiaDevice` z presetem terminalowym. Profil ma uruchamiac WOZ Monitor, jesli ROM jest dostepny lokalnie, oraz miec testy bez realnej konsoli.

---

## Mapa pamieci

| Zakres | Typ | Opis |
|---|---|---|
| `$0000-$0FFF` | RAM | Minimalny RAM Apple-1 |
| `$D010-$D013` | PIA | MOS 6820/6821 z bindingiem terminalowym |
| `$FF00-$FFFF` | ROM | WOZ Monitor |

Opcjonalny BASIC pod `$E000-$EFFF` powinien byc osobnym profilem wariantowym, nie czescia MVP.

---

## Profil

Dodac `profiles/computers/apple-1.json`:

```json
{
  "schema": "computer-profile/v1",
  "id": "apple-1",
  "name": "Apple-1",
  "status": "partial",
  "cpu": { "type": "mos6502-nmos", "clockHz": 1023000 },
  "memory": {
    "ram": [{ "id": "ram0", "start": "0x0000", "size": "0x1000" }],
    "rom": [{ "id": "wozmon", "start": "0xFF00", "size": "0x0100", "file": "roms/apple-1/wozmon.bin", "optional": true }]
  },
  "devices": [
    {
      "id": "pia0",
      "type": "mos6821-pia",
      "mapping": { "kind": "memory", "baseAddress": "0xD010", "size": "0x0004" },
      "preset": "apple-1-terminal"
    }
  ]
}
```

---

## Zachowanie terminala Apple-1

Preset `apple-1-terminal` musi zapewnic:

- odczyt KBD z bitem 7 ustawionym dla gotowego znaku,
- status gotowosci klawiatury,
- zapis DSP po wyczyszczeniu bitu 7,
- status gotowosci display,
- brak zaleznosci od realnego UI.

Jesli szczegoly PIA nie sa jeszcze pelne, preset moze uzywac bindingu terminalowego, ale routing ma nadal przechodzic przez `Mos682xPiaDevice`.

---

## Kolejnosc wykonania dla agenta

1. Dodaj profil `profiles/computers/apple-1.json`.
2. Dodaj `roms/apple-1/README.md` z informacja, skad legalnie dostarczyc ROM.
3. Dodaj factory/binding `apple-1-terminal` do PIA.
4. Dodaj test budowania profilu bez ROM-u z `optional = true`.
5. Dodaj test budowania z testowym ROM-em wstrzyknietym przez `ProfileLoadOptions`.
6. Dodaj testy odczytu/zapisu terminala przez adresy `$D010-$D013`.
7. Dodaj smoke test WOZ Monitor tylko jako `[Explicit]` albo pomijany, jesli ROM nie istnieje.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `BuildApple1Profile_CreatesCpuRamRomAndPia` | profil buduje komputer |
| `Apple1Profile_MapsPiaAtD010ToD013` | zakres I/O |
| `Apple1Profile_RomIsReadOnly` | ROM read-only |
| `Apple1Terminal_ReadKbd_ReturnsHighBitSetInput` | KBD |
| `Apple1Terminal_ReadKbdCr_ReportsReady` | KBDCR |
| `Apple1Terminal_WriteDsp_ForwardsLow7Bits` | DSP |
| `Apple1WozMonSmoke_WhenRomPresent_ReachesInputLoop` | explicit/smoke |

---

## Poza zakresem

- Apple II.
- PET.
- Pelny waveform/timing PIA.
- Realny frontend UI.

---

## Kryteria akceptacji

- Apple-1 jest zbudowany przez `ComputerBuilder`.
- Nie istnieje klasa `Apple1Emulator` zawierajaca specjalna mape pamieci w kodzie.
- PIA w profilu jest generyczna i ma tylko preset/binding Apple-1.

