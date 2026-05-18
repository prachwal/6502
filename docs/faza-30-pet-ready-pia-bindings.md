# Faza 30 - PET-ready PIA bindings i drugi profil walidacyjny

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Walidacja reuse PIA poza Apple-1 |
| **Zaleznosci** | Fazy 24-29 |
| **Cel projektowy** | Udowodnic, ze PIA nie jest hardcoded pod Apple-1 |

---

## Cel fazy

Dodac drugi, PET-like scenariusz uzycia `Mos682xPiaDevice`, z inna adresacja i innymi bindingami portow. To nie jest pelny Commodore PET, tylko celowy test architektury: ta sama PIA ma obsluzyc klawiature/matrix albo testowy rownolegly I/O w innej konfiguracji.

---

## Zakres medium

Implementowac:

- `KeyboardMatrix` jako neutralny model wierszy/kolumn.
- `PiaKeyboardMatrixBinding`, ktory laczy port PIA z matryca.
- PET-like profil testowy, np. `profiles/computers/pet-pia-smoke.json`.
- Mapowanie PIA pod inny adres niz `$D010`, np. `$E810-$E813` albo parametr z profilu.
- Testy, ktore odczytuja klawisz przez PIA po ustawieniu kolumn/wierszy.

Nie implementowac jeszcze:

- pelnego PET ROM,
- edytora ekranu,
- VIA,
- CRTC,
- video RAM renderer.

---

## Kontrakty pomocnicze

```csharp
public sealed class KeyboardMatrix
{
    public KeyboardMatrix(int rows, int columns);
    public void SetKey(int row, int column, bool pressed);
    public byte ReadRows(byte selectedColumnsMask);
}
```

```csharp
public sealed class PiaKeyboardMatrixBinding : IPiaPortBinding
{
    public byte ReadPins();
    public void WritePins(byte value, byte directionMask);
}
```

Konkretny uklad aktywnego zera/aktywnych jedynek ma byc parametrem bindingu.

---

## Kolejnosc wykonania dla agenta

1. Dodaj `KeyboardMatrix`.
2. Dodaj binding PIA dla keyboard matrix.
3. Dodaj drugi profil smoke z PIA pod innym adresem.
4. Dodaj testy bez ROM-u PET.
5. Upewnij sie, ze testy Apple-1 z fazy 29 nadal przechodza.
6. Nie dodawaj jeszcze `Mos6520PiaDevice`, chyba ze jako alias/factory do tego samego rdzenia.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `KeyboardMatrix_SetKey_ReadRowsReportsPressedKey` | matryca dziala |
| `PiaKeyboardBinding_WriteColumnSelect_ReadRows` | PIA steruje wyborem kolumn |
| `PetLikeProfile_MapsPiaAtConfiguredAddress` | adresacja nie jest Apple-1 |
| `Mos682xPia_SameDeviceSupportsApple1AndPetLikeProfiles` | reuse jednej klasy |
| `PetLikeProfile_DoesNotRequireWozMonitor` | brak zaleznosci od Apple-1 ROM |

---

## Kryteria akceptacji

- `Mos682xPiaDevice` jest uzyty przez Apple-1 i PET-like smoke profile.
- PET-like test ma inny adres bazowy niz `$D010`.
- Binding klawiatury jest parametryzowany i nie zna nazw PET w rdzeniu PIA.

