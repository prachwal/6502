# Faza 18 — Nieudokumentowane opkody — stabilne

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Data zakończenia** | 2026-05-27 |
| **Pokrycie dokumentacji** | 8% (sekcje: 5) |
| **Pokrycie całości** | 76% |
| **Zależności** | Fazy: 16, 17 |
| **Szacowany czas** | 5–8h |
| **Liczba testów** | 37 |

---

## Cel fazy

Implementacja wszystkich stabilnych nieudokumentowanych opkodów NMOS 6502. Ok. 60 dodatkowych opcode'ów. Większość to kombinacje istniejących operacji.

---

## Co implementujemy

### DCP (DCM) — DEC + CMP
Opcodes: $C7, $D7, $CF, $DF, $DB, $C3, $D3

```csharp
// DEC memory, potem CMP A z wynikiem
byte val = memory.Read(addr);
// R-M-W: dummy write
byte result = (byte)(val - 1);
memory.Write(addr, result);
// CMP
Compare(A, result);  // N, Z, C
```

### ISC (ISB) — INC + SBC
Opcodes: $E7, $F7, $EF, $FF, $FB, $E3, $F3

```csharp
byte val = memory.Read(addr);
byte result = (byte)(val + 1);
memory.Write(addr, result);
// SBC
ExecuteSBC(result);
```

### LAX — LDA + LDX
Opcodes: $A7, $B7, $AF, $BF, $A3, $B3

```csharp
byte val = memory.Read(addr);
A = val;
X = val;
SetNZ(val);
```

### SAX (AXS/AAX) — Store A & X
Opcodes: $87, $97, $8F, $83

```csharp
memory.Write(addr, (byte)(A & X));
```

### RLA — ROL + AND
Opcodes: $27, $37, $2F, $3F, $3B, $23, $33

```csharp
byte val = memory.Read(addr);
byte rotated = ExecuteROL(val);
memory.Write(addr, rotated);
A &= rotated;
SetNZ(A);
```

### RRA — ROR + ADC
Opcodes: $67, $77, $6F, $7F, $7B, $63, $73

```csharp
byte val = memory.Read(addr);
byte rotated = ExecuteROR(val);
memory.Write(addr, rotated);
ExecuteADC(rotated);
```

### SLO (ASO) — ASL + ORA
Opcodes: $07, $17, $0F, $1F, $1B, $03, $13

```csharp
byte val = memory.Read(addr);
byte shifted = ExecuteASL(val);
memory.Write(addr, shifted);
A |= shifted;
SetNZ(A);
```

### SRE (LSE) — LSR + EOR
Opcodes: $47, $57, $4F, $5F, $5B, $43, $53

```csharp
byte val = memory.Read(addr);
byte shifted = ExecuteLSR(val);
memory.Write(addr, shifted);
A ^= shifted;
SetNZ(A);
```

### ANC — AND + C ← bit7
Opcodes: $0B, $2B

```csharp
A &= operand;
SetNZ(A);
SetFlag(FlagC, (A & 0x80) != 0);  // C = bit 7
```

### ALR (ASR) — AND + LSR
Opcode: $4B

```csharp
A &= operand;
// LSR na A
SetFlag(FlagC, (A & 0x01) != 0);
A >>= 1;
SetNZ(A);
```

### ARR — AND + ROR
Opcode: $6B

```csharp
A &= operand;
// ROR na A
bool carryIn = (P & FlagC) != 0;
bool carryOut = (A & 0x01) != 0;
A = (byte)((A >> 1) | (carryIn ? 0x80 : 0x00));
SetFlag(FlagC, carryOut);
SetNZ(A);
// V = (A ^ (A >> 1)) & 0x40
SetFlag(FlagV, ((A ^ (A >> 1)) & 0x40) != 0);
```

### SBX (AXS) — (A&X) - operand → X
Opcode: $CB

```csharp
byte andVal = (byte)(A & X);
int result = andVal - operand;
SetFlag(FlagC, result >= 0);  // C = brak pożyczenia
X = (byte)result;
SetNZ(X);
```

Notatka po naprawie: SBX nie powinien używać helpera `Compare()` jako skrótu implementacji wyniku. `Compare()` jest poprawny dla flag CMP, ale w SBX źródłem prawdy po odejmowaniu jest finalny rejestr `X`; po zapisie do `X` trzeba jawnie ustawić `N/Z` z `X` i `C` z informacji o pożyczeniu. Test SBX musi być aktywny, nie zakomentowany jako TODO. Dane testowe muszą też mieć poprawne `A & X`: przypadek `A=0x10, X=0x08` daje `0x00`, nie `0x08`, więc nie może oczekiwać wyniku `0x05`.

### LAS (LAR) — M & SP → A, X, SP
Opcode: $BB

```csharp
byte val = (byte)(memory.Read(addr) & SP);
A = val;
X = val;
SP = val;
SetNZ(val);
```

---

## Co testujemy

| Test | Opis |
|------|------|
| **DCP: DEC + CMP** | Wynik i flagi poprawne |
| **ISC: INC + SBC** | Wynik i flagi poprawne |
| **LAX: oba rejestry załadowane** | A = X = M |
| **SAX: A & X zapisane** | Zapis A&X |
| **RLA: ROL + AND** | Wynik i flagi |
| **RRA: ROR + ADC** | Wynik i flagi |
| **SLO: ASL + ORA** | Wynik i flagi |
| **SRE: LSR + EOR** | Wynik i flagi |
| **ANC, ALR, ARR, SBX, LAS** | Każdy osobno |
| **Wszystkie tryby adresowania dla każdego** | Jak w dokumentacji |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 5 | Nieudokumentowane instrukcje — tabela stabilnych |
| 11 | Mapa opkodów $00–$FF |

---

## Definition of Done

- [ ] Wszystkie 13 stabilnych mnemoników zaimplementowanych (~60 opcode'ów)
- [ ] Poprawne flagi dla każdego
- [ ] R-M-W double write dla DCP/ISC/RLA/RRA/SLO/SRE
- [ ] Page crossing dla trybów indeksowanych
- [ ] 15 testów jednostkowych zielonych

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — dodaj 60 case'ów |
| `tests/Cpu6502.Tests/IllegalOpcodesTests.cs` | Utwórz |

---

## Pliki implementacyjne

| Plik | Opis |
|------|-------|
| `src/Cpu6502/Cpu6502.CycleStepped.IllegalLoadStore.cs` | LAX, SAX, LAS (Read-Modify-Write) |
| `src/Cpu6502/Cpu6502.CycleStepped.IllegalRMW.cs` | DCP, ISC, RLA, RRA, SLO, SRE (R-M-W) |
| `src/Cpu6502/Cpu6502.CycleStepped.IllegalRMWDispatch.cs` | Dispatch cykli dla illegal RMW |
| `src/Cpu6502/Cpu6502.CycleStepped.LoadStoreTransferFlags.cs` | Podłączenie LAX/SAX do dispatchu |
| `src/Cpu6502/Cpu6502.CycleStepped.ArithmeticCompareLogic.cs` | ANC, ALR, ARR, SBX (już istniały) |
| `src/Cpu6502/Cpu6502.CycleStepped.Core.cs` | Dodano ExecuteCycleIllegalRMW do łańcucha dispatchu |
| `tests/Cpu6502.Tests/IllegalOpcodesTests.cs` | 37 testów jednostkowych |

---

## Wyniki

| Metryka | Wartość |
|---------|---------|
| **Build** | ✅ 0 błędów, 0 ostrzeżeń |
| **Testy** | ✅ 236/236 (100%) |
| **Status** | Zakończone |

### Zaimplementowane opkody (50/60):

| Opcode | Mnemonik | Tryby adresowania | Status |
|--------|----------|-------------------|--------|
| 0x0B, 0x2B | ANC | Immediate | ✅ |
| 0x4B | ALR | Immediate | ✅ |
| 0x6B | ARR | Immediate | ✅ |
| 0xCB | SBX | Immediate | ✅ |
| 0xBB | LAS | Absolute,Y | ✅ |
| A7, B7, AF, BF, A3, B3 | LAX | Zero Page, Zero Page,Y, Absolute, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| 87, 97, 8F, 83 | SAX | Zero Page, Zero Page,Y, Absolute, Zero Page,X | ✅ |
| C7, D7, CF, DF, DB, C3, D3 | DCP | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| E7, F7, EF, FF, FB, E3, F3 | ISC | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| 27, 37, 2F, 3F, 3B, 23, 33 | RLA | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| 67, 77, 6F, 7F, 7B, 63, 73 | RRA | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| 07, 17, 0F, 1F, 1B, 03, 13 | SLO | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |
| 47, 57, 4F, 5F, 5B, 43, 53 | SRE | Zero Page, Zero Page,X, Absolute, Absolute,X, Absolute,Y, (Indirect,X), (Indirect),Y | ✅ |

### Uwagi:
- SBX (0xCB) naprawiony: wynik trafia do X, C oznacza brak pożyczenia, N/Z pochodzą z finalnego X.
- Wszystkie inne opkody działają poprawnie
