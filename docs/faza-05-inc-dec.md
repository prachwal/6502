# Faza 5 — Inkrementacja / Dekrementacja (INC, DEC, INX, INY, DEX, DEY)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.4) |
| **Pokrycie całości** | 13% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 2–3h |
| **Data zakończenia** | 2026-05-16 |
| **Liczba testów** | 15 |

---

## Cel fazy

Implementacja INC, DEC (Read-Modify-Write na pamięci) oraz INX, INY, DEX, DEY (na rejestrach). Flagi N, Z.

---

## Co implementujemy

### INC — Increment Memory

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $E6 | 2 | 5 |
| zp,X | $F6 | 2 | 6 |
| abs | $EE | 3 | 6 |
| abs,X | $FE | 3 | 7 |

### DEC — Decrement Memory

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $C6 | 2 | 5 |
| zp,X | $D6 | 2 | 6 |
| abs | $CE | 3 | 6 |
| abs,X | $DE | 3 | 7 |

### INX, INY, DEX, DEY

| Instrukcja | Opcode | Opis | Flagi |
|------------|--------|------|-------|
| INX | $E8 | X ← X+1 | N, Z |
| INY | $C8 | Y ← Y+1 | N, Z |
| DEX | $CA | X ← X-1 | N, Z |
| DEY | $88 | Y ← Y-1 | N, Z |

### Pseudokod

```csharp
// INC (memory)
byte value = memory.Read(address);
byte result = (byte)(value + 1);
memory.Write(address, result);
SetNZ(result);

// DEC (memory)
byte value = memory.Read(address);
byte result = (byte)(value - 1);
memory.Write(address, result);
SetNZ(result);

// INX
X = (byte)(X + 1);
SetNZ(X);

// INY
Y = (byte)(Y + 1);
SetNZ(Y);

// DEX
X = (byte)(X - 1);
SetNZ(X);

// DEY
Y = (byte)(Y - 1);
SetNZ(Y);

// Helper:
void SetNZ(byte value)
{
    SetFlag(FlagZ, value == 0);
    SetFlag(FlagN, (value & 0x80) != 0);
}
```

### Uwaga R-M-W

INC i DEC na pamięci to instrukcje Read-Modify-Write. W NMOS 6502 następuje podwójny zapis (oryginalna wartość + zmodyfikowana). To będzie zaimplementowane w fazie 17 — na razie wystarczy poprawny odczyt i zapis końcowy.

---

## Co testujemy

| Test | Opis |
|------|------|
| **INC zp zwiększa pamięć** | Zapis $05 pod $10, INC $10 → $06, N=0, Z=0 |
| **INC $FF → $00** | Z wrap-around, Z=1, C bez zmian |
| **DEC zp zmniejsza pamięć** | $05 → $04 |
| **DEC $00 → $FF** | Wrap-around, N=1, Z=0 |
| **INX z X=$FF → X=$00** | Z=1 |
| **DEX z X=$01 → X=$00** | Z=1 |
| **INY/DEX/DEY** | Każdy tryb |
| **Flag C niezmieniona przez INC/DEC** | Ustaw C=1, wykonaj INC → C=1 |

### Test jednostkowy

```csharp
[Test]
public void INC_ZP_IncrementsMemory()
{
    var mem = new FlatMemory();
    mem.Write(0x10, 0x05);
    mem.Write(0x0200, 0xE6); // INC $10
    mem.Write(0x0201, 0x10);
    var cpu = new Cpu6502(mem);
    cpu.PC = 0x0200;
    cpu.ExecuteOne();
    Assert.AreEqual(0x06, mem.Read(0x10));
    Assert.IsFalse(cpu.GetFlag(FlagZ));
    Assert.IsFalse(cpu.GetFlag(FlagN));
}
```

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 4.4 | Inkrementacja/Dekrementacja — pełna tabela |

---

## Definition of Done

- [x] Wszystkie 10 opcode'ów zaimplementowanych
- [x] INC/DEC poprawnie modyfikują pamięć
- [x] Wrap-around $FF↔$00 działa
- [x] Flagi N, Z poprawnie ustawiane
- [x] 15 testów jednostkowych zielonych (97/97 łącznie)

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.IncDec.cs` | Implementacja 10 instrukcji (partial class) |
| `src/Cpu6502/Cpu6502.Constructor.cs` | Inicjalizacja opcode'ów w konstruktorze |
| `tests/Cpu6502.Tests/IncDecTests.cs` | 15 testów jednostkowych |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 97/97 (100%)

### Zaimplementowane instrukcje

| Opcode | Instrukcja | Opis | Cykle |
|--------|------------|------|-------|
| $E6 | INC zp | Memory ← Memory+1 | 5 |
| $F6 | INC zp,X | Memory ← Memory+1 | 6 |
| $EE | INC abs | Memory ← Memory+1 | 6 |
| $FE | INC abs,X | Memory ← Memory+1 | 7 |
| $C6 | DEC zp | Memory ← Memory-1 | 5 |
| $D6 | DEC zp,X | Memory ← Memory-1 | 6 |
| $CE | DEC abs | Memory ← Memory-1 | 6 |
| $DE | DEC abs,X | Memory ← Memory-1 | 7 |
| $E8 | INX | X ← X+1 | 2 |
| $C8 | INY | Y ← Y+1 | 2 |
| $CA | DEX | X ← X-1 | 2 |
| $88 | DEY | Y ← Y-1 | 2 |

---

## Pliki

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj |
| `tests/Cpu6502.Tests/IncDecTests.cs` | Utwórz |
