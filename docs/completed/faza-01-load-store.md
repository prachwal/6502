# Faza 1 — Load / Store (LDA, LDX, LDY, STA, STX, STY)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 100% (sekcje: 3.1 LDA, 3.2 LDX, 3.3 LDY, 3.4 STA, 3.5 STX, 3.6 STY, częściowo 2.1 tryby adresowania) |
| **Pokrycie całości** | 3% |
| **Zależności** | Fazy: 0 |
| **Szacowany czas** | 4–6 h |
| **Data zakończenia** | 2026-05-16 |

---

## Cel fazy

Zaimplementować wszystkie tryby adresowania dla sześciu podstawowych instrukcji ładowania i składowania rejestrów 6502 (LDA, LDX, LDY, STA, STX, STY) wraz z mechanizmem ustawiania flag N i Z. Jest to fundament całego emulatora — na tych instrukcjach oprze się większość kolejnych faz.

---

## Co implementujemy

### 1. Metody pomocnicze flag NZ

Przed implementacją instrukcji tworzymy dwie pomocnicze metody w klasie `Cpu`:

```csharp
private void SetNZ(byte value)
{
    N = (value & 0x80) != 0;   // bit 7 → flaga N (Negative)
    Z = value == 0;             // wartość 0 → flaga Z (Zero)
}
```

Metody `SetNZ` używamy dla wszystkich instrukcji load (LDA, LDX, LDY) oraz operacji logicznych w przyszłych fazach. Instrukcje store nie modyfikują flag — zapamiętujemy tę różnicę.

### 2. Implementacja trybów adresowania

Każdy tryb adresowania to osobna metoda zwracająca `(ushort address, byte value, int cycles)`. Dla instrukcji load potrzebujemy wartości spod adresu, dla store tylko adresu.

| Tryb | Mnemonik w asm | Opis | Cykle bazowe |
|------|---------------|------|-------------|
| Immediate | `#$nn` | Wartość bezpośrednio po opcodzie | 2 |
| Zero Page | `$nn` | Adres w zerowej stronie ($0000–$00FF) | 3 |
| Zero Page,X | `$nn,X` | Zero Page + rejestr X (wrap w obrębie strony) | 4 |
| Zero Page,Y | `$nn,Y` | Zero Page + rejestr Y (wrap w obrębie strony) | 4 |
| Absolute | `$nnnn` | Pełny 16-bitowy adres | 4 |
| Absolute,X | `$nnnn,X` | Absolutny + X (dodatkowy cykl przy przekroczeniu strony) | 4+ |
| Absolute,Y | `$nnnn,Y` | Absolutny + Y (dodatkowy cykl przy przekroczeniu strony) | 4+ |
| (Indirect,X) | `($nn,X)` | Pre-indeksowane pośrednie — Zero Page + X, odczyt adresu | 6 |
| (Indirect),Y | `($nn),Y` | Post-indeksowane pośrednie — odczyt z ZP, + Y | 5+ |

**Pseudokod metody dla Immediate:**

```csharp
private (ushort address, byte value, int cycles) Imm()
{
    byte value = Read(PC++);
    return (0, value, 2); // address nieistotny dla immediate
}
```

**Pseudokod metody dla Zero Page:**

```csharp
private (ushort address, byte value, int cycles) Zp()
{
    byte addr = Read(PC++);
    byte value = Read(addr);
    return (addr, value, 3);
}
```

**Pseudokod metody dla Absolute:**

```csharp
private (ushort address, byte value, int cycles) Abs()
{
    byte lo = Read(PC++);
    byte hi = Read(PC++);
    ushort addr = (ushort)(hi << 8 | lo);
    byte value = Read(addr);
    return (addr, value, 4);
}
```

**Pseudokod dla Absolute,X i Absolute,Y (z detekcją przekroczenia strony):**

```csharp
private (ushort addr, byte val, int cyc) AbsX()
{
    byte lo = Read(PC++);
    byte hi = Read(PC++);
    ushort base = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(base + X);
    int extra = ((base & 0xFF00) != (addr & 0xFF00)) ? 1 : 0;
    byte val = Read(addr);
    return (addr, val, 4 + extra);
}
```

**Pseudokod dla (Indirect,X) — pre-indeksowane pośrednie:**

```csharp
private (ushort addr, byte val, int cyc) IndX()
{
    byte zpp = Read(PC++);                     // bajt bazowy zero page
    byte lo = Read((byte)(zpp + X));            // low byte adresu z ZP+X
    byte hi = Read((byte)(zpp + X + 1));        // high byte z ZP+X+1  (wrap w ZP!)
    ushort addr = (ushort)(hi << 8 | lo);
    byte val = Read(addr);
    return (addr, val, 6);
}
```

**Pseudokod dla (Indirect),Y — post-indeksowane pośrednie:**

```csharp
private (ushort addr, byte val, int cyc) IndY()
{
    byte zpp = Read(PC++);                     // bajt bazowy zero page
    byte lo = Read(zpp);                        // low byte adresu z ZP
    byte hi = Read((byte)(zpp + 1));            // high byte z ZP+1 (wrap!)
    ushort base = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(base + Y);
    int extra = ((base & 0xFF00) != (addr & 0xFF00)) ? 1 : 0;
    byte val = Read(addr);
    return (addr, val, 5 + extra);
}
```

### 3. Opcode table entries — Load instructions

| Mnemonik | Opcode (hex) | Tryb adresowania | Cykle |
|----------|-------------|-----------------|-------|
| LDA | $A9 | Immediate | 2 |
| LDA | $A5 | Zero Page | 3 |
| LDA | $B5 | Zero Page,X | 4 |
| LDA | $AD | Absolute | 4 |
| LDA | $BD | Absolute,X | 4+ |
| LDA | $B9 | Absolute,Y | 4+ |
| LDA | $A1 | (Indirect,X) | 6 |
| LDA | $B1 | (Indirect),Y | 5+ |
| LDX | $A2 | Immediate | 2 |
| LDX | $A6 | Zero Page | 3 |
| LDX | $B6 | Zero Page,Y | 4 |
| LDX | $AE | Absolute | 4 |
| LDX | $BE | Absolute,Y | 4+ |
| LDY | $A0 | Immediate | 2 |
| LDY | $A4 | Zero Page | 3 |
| LDY | $B4 | Zero Page,X | 4 |
| LDY | $AC | Absolute | 4 |
| LDY | $BC | Absolute,X | 4+ |

**Pseudokod execute dla LDA:**

```csharp
case 0xA9: // LDA Immediate
{
    A = Read(PC++);
    SetNZ(A);
    cycles = 2;
    break;
}
case 0xA5: // LDA Zero Page
{
    byte addr = Read(PC++);
    A = Read(addr);
    SetNZ(A);
    cycles = 3;
    break;
}
// ... podobnie dla pozostałych trybów
```

### 4. Opcode table entries — Store instructions

| Mnemonik | Opcode (hex) | Tryb adresowania | Cykle |
|----------|-------------|-----------------|-------|
| STA | $85 | Zero Page | 3 |
| STA | $95 | Zero Page,X | 4 |
| STA | $8D | Absolute | 4 |
| STA | $9D | Absolute,X | 5 |
| STA | $99 | Absolute,Y | 5 |
| STA | $81 | (Indirect,X) | 6 |
| STA | $91 | (Indirect),Y | 6 |
| STX | $86 | Zero Page | 3 |
| STX | $96 | Zero Page,Y | 4 |
| STX | $8E | Absolute | 4 |
| STY | $84 | Zero Page | 3 |
| STY | $94 | Zero Page,X | 4 |
| STY | $8C | Absolute | 4 |

**Pseudokod execute dla STA (bez modyfikacji flag!):**

```csharp
case 0x85: // STA Zero Page
{
    byte addr = Read(PC++);
    Write(addr, A);
    cycles = 3;
    break;
}
case 0x8D: // STA Absolute
{
    byte lo = Read(PC++);
    byte hi = Read(PC++);
    ushort addr = (ushort)(hi << 8 | lo);
    Write(addr, A);
    cycles = 4;
    break;
}
```

**Uwaga:** STA Absolute,X (opcode $9D) i Absolute,Y ($99) zawsze wykonują pełne 5 cykli (Read-Modify-Write wymusza odczyt przed zapisem), nie stosujemy optymalizacji pomijania zapisu.

---

## Co testujemy

| # | Nazwa testu | Opis |
|---|------------|------|
| T1.1 | `LdaImmediate_LoadsA` | LDA #$42 → A = $42 |
| T1.2 | `LdaImmediate_SetsNZ_Zero` | LDA #$00 → Z=1, N=0 |
| T1.3 | `LdaImmediate_SetsNZ_Negative` | LDA #$80 → Z=0, N=1 |
| T1.4 | `LdaZeroPage_LoadsFromMemory` | LDA $10 (gdzie [$10]=$55) → A=$55 |
| T1.5 | `LdaZeroPageX_WrapsInPage` | LDA $FF,X (X=2) → odczyt z $01 |
| T1.6 | `LdaAbsolute_LoadsFromFullAddress` | LDA $1234 → A = [$1234] |
| T1.7 | `LdaAbsoluteX_CrossesPage` | LDA $12FF,X (X=2) → A=[$1301], dodatkowy cykl |
| T1.8 | `LdaIndirectX_PreIndexed` | LDA ($20,X) — adres pobrany z ($20+X) i ($20+X+1) |
| T1.9 | `LdaIndirectY_PostIndexed` | LDA ($20),Y — adres z ($20)/($21) + Y |
| T1.10 | `LdxImmediate_LoadsX` | LDX #$7F → X = $7F |
| T1.11 | `LdyZeroPage_LoadsY` | LDY $30 → Y = [$30] |
| T1.12 | `StaZeroPage_StoresA` | STA $50 (A=$AB) → [$50] = $AB |
| T1.13 | `StaAbsolute_StoresToFullAddress` | STA $2000 → [$2000] = A |
| T1.14 | `StaDoesNotAffectFlags` | STA $40 → N i Z sprzed instrukcji niezmienione |
| T1.15 | `StxAbsolute_StoresX` | STX $3000 → [$3000] = X |
| T1.16 | `StyZeroPage_StoresY` | STY $05 → [$05] = Y |
| T1.17 | `AllLdaModes_PcAdvancesCorrectly` | PC po każdej LDA wskazuje za instrukcję |
| T1.18 | `LdaIndirectX_ZeroPageWrap` | LDA ($FF,X) gdy X=2 → low z $01, high z $02 |
| T1.19 | `LdaIndirectY_ZeroPageWrap` | LDA ($FF),Y → low z $FF, high z $00 |
| T1.20 | `StaWritesCorrectAddr_AllModes` | Weryfikacja adresu docelowego dla każdego trybu STA |

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja dokumentacji | Co pokrywamy |
|-------------------|-------------|
| 3.1 LDA — Load Accumulator | Pełna implementacja, wszystkie 8 trybów |
| 3.2 LDX — Load X Register | Pełna implementacja, wszystkie 5 trybów |
| 3.3 LDY — Load Y Register | Pełna implementacja, wszystkie 5 trybów |
| 3.4 STA — Store Accumulator | Pełna implementacja, wszystkie 7 trybów |
| 3.5 STX — Store X Register | Pełna implementacja, wszystkie 3 tryby |
| 3.6 STY — Store Y Register | Pełna implementacja, wszystkie 3 tryby |
| 2.1 Tryby adresowania | Immediate, ZP, ZP,X, ZP,Y, Abs, Abs,X, Abs,Y, (Ind,X), (Ind),Y |

---

## Definition of Done

- [x] Metoda `SetNZ(byte value)` zaimplementowana i przetestowana
- [x] Wszystkie tryby adresowania dla LDA działają (8 opcode'ów)
- [x] Wszystkie tryby adresowania dla LDX działają (5 opcode'ów)
- [x] Wszystkie tryby adresowania dla LDY działają (5 opcode'ów)
- [x] Wszystkie tryby adresowania dla STA działają (7 opcode'ów)
- [x] Wszystkie tryby adresowania dla STX działają (3 opcode'y)
- [x] Wszystkie tryby adresowania dla STY działają (3 opcode'y)
- [x] Flagi N i Z ustawiane poprawnie dla load, niezmieniane dla store
- [x] Wrap-around w Zero Page działa poprawnie (adresowanie modulo 256)
- [x] Detekcja przekroczenia strony dla Absolute,X/Y i (Indirect),Y (dodatkowy cykl)
- [x] Wszystkie testy T1.1–T1.20 przechodzą (40 testów, wszystkie zielone)
- [x] Kod skompilowany bez warningów

---

## Pliki do utworzenia / modyfikacji

| Plik | Operacja | Opis |
|------|---------|------|
| `src/Cpu.Core.cs` | Modyfikacja | Główna pętla execute, metoda `SetNZ`, implementacje trybów adresowania |
| `src/Cpu.Opcodes.cs` | Modyfikacja | Opcode table — wpisy dla LDA/LDX/LDY/STA/STX/STY |
| `src/Bus.cs` | Modyfikacja (jeśli konieczne) | Metody `Read(ushort)` i `Write(ushort, byte)` — pamięć i mirroring |
| `tests/LoadStoreTests.cs` | Utworzenie | Testy jednostkowe dla wszystkich instrukcji load/store (T1.1–T1.20) |
| `tests/AddressingModesTests.cs` | Utworzenie | Testy jednostkowe samych trybów adresowania (izolowane) |
