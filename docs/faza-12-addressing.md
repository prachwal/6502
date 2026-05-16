# Faza 12 — Pełne tryby adresowania i page crossing

| Właściwość | Wartość |
|------------|---------|
| **Status** | [~] Częściowo zaimplementowane |
| **Pokrycie dokumentacji** | 5% (sekcje: 3, 6.2, 6.4) |
| **Pokrycie całości** | 42% |
| **Zależności** | Fazy: 0–11 |
| **Szacowany czas** | 4–6h |
| **Data rozpoczęcia** | 2026-05-17 |

---

## Cel fazy

Refaktoryzacja: wydzielenie trybów adresowania do osobnych metod. Prawidłowa obsługa page boundary crossing (+1 cykl). Ujednolicenie kodu wszystkich instrukcji.

---

## Co implementujemy

### Metody adresowania

```csharp
// Każda zwraca adres efektywny i out bool pageCrossed

// Immediate — zwraca PC (adres operandu) i auto-inkrementuje PC
ushort AddrImmediate() => PC++;

// Zero Page
ushort AddrZP()
{
    byte zp = memory.Read(PC++);
    return zp;  // adres = $00xx
}

// Zero Page, X — zawija w stronie
ushort AddrZPX() => (byte)(memory.Read(PC++) + X);

// Zero Page, Y
ushort AddrZPY() => (byte)(memory.Read(PC++) + Y);

// Absolute
ushort AddrAbs()
{
    byte lo = memory.Read(PC++);
    byte hi = memory.Read(PC++);
    return (ushort)(hi << 8 | lo);
}

// Absolute, X — może przekroczyć stronę
ushort AddrAbsX(out bool pageCrossed)
{
    byte lo = memory.Read(PC++);
    byte hi = memory.Read(PC++);
    ushort baseAddr = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(baseAddr + X);
    pageCrossed = (addr >> 8) != hi;
    return addr;
}

// Absolute, Y — może przekroczyć stronę
ushort AddrAbsY(out bool pageCrossed)
{
    byte lo = memory.Read(PC++);
    byte hi = memory.Read(PC++);
    ushort baseAddr = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(baseAddr + Y);
    pageCrossed = (addr >> 8) != hi;
    return addr;
}

// (zp,X) — pre-indexed indirect, zawija w zero page
ushort AddrIndX()
{
    byte zp = (byte)(memory.Read(PC++) + X);  // zawija w zeropage
    byte lo = memory.Read(zp);
    byte hi = memory.Read((byte)(zp + 1));    // zawija
    return (ushort)(hi << 8 | lo);
}

// (zp),Y — post-indexed indirect, może przekroczyć stronę
ushort AddrIndY(out bool pageCrossed)
{
    byte zp = memory.Read(PC++);
    byte lo = memory.Read(zp);
    byte hi = memory.Read((byte)(zp + 1));
    ushort baseAddr = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(baseAddr + Y);
    pageCrossed = (addr >> 8) != hi;
    return addr;
}
```

### Page crossing penalty

Dla instrukcji **odczytujących** (LDA, ADC, AND, ORA, EOR, CMP, SBC, LAX, itp.) w trybach `abs,X`, `abs,Y`, `(zp),Y`:

- Jeśli `pageCrossed == true`: **+1 cykl** do czasu wykonania.

Dla instrukcji **zapisujących** (STA, STX, STY, SAX, SHA, SHX, SHY) w trybach `abs,X` i `abs,Y`:

- **Zawsze dodatkowy cykl** (5 cykli zamiast 4 dla abs,X/Y), niezależnie od page crossing.

### Refaktoryzacja

Wszystkie instrukcje z faz 1–11 używają teraz metod adresowania. Kod każdej instrukcji staje się krótszy:

```csharp
case 0xA9: // LDA #
    A = memory.Read(AddrImmediate());
    SetNZ(A);
    break;

case 0xAD: // LDA abs
    A = memory.Read(AddrAbs());
    SetNZ(A);
    break;

case 0xBD: // LDA abs,X
    bool pageCrossed;
    ushort addr = AddrAbsX(out pageCrossed);
    A = memory.Read(addr);
    SetNZ(A);
    if (pageCrossed) cycles++;
    break;
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **AddrZP zwraca $00xx** | Adres < $0100 |
| **AddrZPX zawija** | X=$FF, zp=$80 → adres = $7F |
| **AddrAbsX pageCross=true** | base=$01FF, X=1 → pageCross |
| **AddrAbsX pageCross=false** | base=$0100, X=$7F → no cross |
| **AddrIndX zawija** | zp+1 w zero page |
| **AddrIndY pageCross** | base=$01FF, Y=2 → cross |
| **Wszystkie instrukcje nadal działają** | Pełny regression z faz 1–11 |
| **Page crossing dodaje cykl** | Sprawdź cycle count dla abs,X |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 3 | Tryby adresowania (wszystkie 13) |
| 6.2 | Page boundary crossing |
| 6.4 | Tabela cykli |

---

## Definition of Done

- [x] Wszystkie tryby adresowania wydzielone do metod (AddrImmediate, AddrZp, AddrZpX, AddrZpY, AddrAbs, AddrAbsX, AddrAbsY, AddrIndX, AddrIndY)
- [x] Helper methods for tuple-based addressing (Imm, Zp, ZpX, ZpY, Abs, AbsX, AbsY, IndX, IndY)
- [x] Page crossing detection implemented in AddrAbsX, AddrAbsY, AddrIndY
- [ ] Wszystkie instrukcje z faz 1–11 zrefaktoryzowane (częściowo - wymaga pełnej refaktoryzacji)
- [ ] Page crossing dodaje +1 cykl (wymaga implementacji w instrukcjach)
- [ ] Store w abs,X/abs,Y zawsze +1 cykl (wymaga implementacji)
- [x] Testy regresyjne zielone (173/173 z poprzednich faz)
- [x] 7 nowych testów dla adresowania (4 wymagają poprawy)

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.AddressingModes.cs` | Utworzono |
| `src/Cpu6502/Cpu6502.AddressingHelpers.cs` | Utworzono |
| `tests/Cpu6502.Tests/AddressingTests.cs` | Utworzono |

## Pliki implementacyjne

- `Cpu6502.AddressingModes.cs`: 9 metod adresowania (AddrImmediate, AddrZp, AddrZpX, AddrZpY, AddrAbs, AddrAbsX, AddrAbsY, AddrIndX, AddrIndY)
- `Cpu6502.AddressingHelpers.cs`: 9 helper methods (Imm, Zp, ZpX, ZpY, Abs, AbsX, AbsY, IndX, IndY) dla kompatybilności
- `AddressingTests.cs`: 7 testów jednostkowych (3 passing, 4 failing - wymagają pełnej refaktoryzacji instrukcji)

## Wyniki

- Build: ✅ 0 błędów, 7 ostrzeżeń (nullable references)
- Testy: ✅ 173/173 (100%) dla istniejących faz + ⚠️ 4/7 failing dla nowych testów adresowania
- Testy regresyjne: ✅ Wszystkie poprzednie instrukcje nadal działają

## Postęp

Faza 12 jest częściowo zaimplementowana:
- ✅ Nowe metody adresowania gotowe do użycia
- ✅ Helper methods dla kompatybilności z istniejącym kodem
- ✅ Wykrywanie page crossing zaimplementowane
- ⏳ Pełna refaktoryzacja instrukcji do zrobienia
- ⏳ Implementacja +1 cyklu dla page crossing do zrobienia
- ⏳ Testy adresowania wymagają poprawy po pełnej refaktoryzacji
