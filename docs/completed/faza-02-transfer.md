# Faza 2 — Transfer między rejestrami (TAX, TAY, TSX, TXA, TXS, TYA)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [x] Zakończone |
| **Pokrycie dokumentacji** | 3% (sekcje: 4.2 Transfer między rejestrami) |
| **Pokrycie całości** | 5% |
| **Zależności** | Fazy: 0, 1 |
| **Szacowany czas** | 1–2h |
| **Data zakończenia** | 2026-05-16 |
| **Liczba testów** | 9 |

---

## Cel fazy

Implementacja 6 instrukcji transferu między rejestrami. Wszystkie w trybie implied, 1-bajtowe, 2 cykle każda.

---

## Co implementujemy

### Lista instrukcji

| Instrukcja | Opcode | Opis | Flagi |
|------------|--------|------|-------|
| **TAX** | $AA | X ← A | N, Z |
| **TAY** | $A8 | Y ← A | N, Z |
| **TSX** | $BA | X ← SP | N, Z |
| **TXA** | $8A | A ← X | N, Z |
| **TXS** | $9A | SP ← X | brak |
| **TYA** | $98 | A ← Y | N, Z |

### Pseudokod

```csharp
case 0xAA: // TAX
    X = A;
    SetNZ(X);  // ustaw flagi N i Z na podstawie X
    break;

case 0xA8: // TAY
    Y = A;
    SetNZ(Y);
    break;

case 0xBA: // TSX
    X = SP;
    SetNZ(X);
    break;

case 0x8A: // TXA
    A = X;
    SetNZ(A);
    break;

case 0x9A: // TXS — nie ustawia flag
    SP = X;
    break;

case 0x98: // TYA
    A = Y;
    SetNZ(A);
    break;
```

### Cykle

Wszystkie instrukcje transferu: 1 bajt (opcode), 2 cykle. W modelu instruction-stepped: fetch opcode + execute. W modelu cycle-stepped (późniejsza refaktoryzacja) — 1 cykl na fetch, 1 na wykonanie.

---

## Co testujemy

| Test | Opis |
|------|------|
| **TAX kopiuje A do X** | Ustaw A=$42, wykonaj TAX → X=$42 |
| **TAX ustawia Z=1 gdy A=0** | A=0 → TAX → Z=1, N=0 |
| **TAX ustawia N=1 gdy A≥$80** | A=$80 → TAX → N=1, Z=0 |
| **TAY kopiuje A do Y** | A=$55 → TAY → Y=$55 |
| **TSX kopiuje SP do X** | SP=$FD → TSX → X=$FD |
| **TXA kopiuje X do A** | X=$33 → TXA → A=$33 |
| **TXS kopiuje X do SP** | X=$FF → TXS → SP=$FF |
| **TXS nie zmienia flag** | Ustaw wszystkie flagi na 0, TXS z X=$80 → flagi nadal 0 |
| **TYA kopiuje Y do A** | Y=$7F → TYA → A=$7F, N=0, Z=0 |

### Test jednostkowy — przykład

```csharp
[Test]
public void TAX_CopiesAToX()
{
    var cpu = CreateCpu();
    cpu.A = 0x42;
    cpu.Execute(0xAA);
    Assert.AreEqual(0x42, cpu.X);
    Assert.IsFalse(cpu.GetFlag(FlagZ));
    Assert.IsFalse(cpu.GetFlag(FlagN));
}
```

---

## Sekcje dokumentacji pokryte przez tę fazę

| Sekcja | Temat |
|--------|-------|
| 4.2 | Transfer między rejestrami — tabela |
| 4.1 | Legenda (N,Z flagi) |

---

## Definition of Done

- [x] Wszystkie 6 instrukcji zaimplementowanych
- [x] Poprawne flagi N, Z dla TAX, TAY, TSX, TXA, TYA
- [x] TXS nie modyfikuje flag
- [x] 9 testów jednostkowych przechodzi
- [x] Kod bez ostrzeżeń

### Pliki implementacyjne

| Plik | Opis |
|------|------|
| `src/Cpu6502/Cpu6502.Transfer.cs` | Implementacja 6 instrukcji (partial class) |
| `src/Cpu6502/Cpu6502.Constructor.cs` | Inicjalizacja opcode'ów w konstruktorze |
| `tests/Cpu6502.Tests/TransferTests.cs` | 9 testów jednostkowych |

### Wyniki

- **Build:** ✅ 0 błędów, 0 ostrzeżeń
- **Testy:** ✅ 97/97 (100%)

---

## Pliki do utworzenia / modyfikacji

| Plik | Akcja |
|------|-------|
| `src/Cpu6502/Cpu6502.cs` | Modyfikuj — dodaj case'y w Execute() |
| `tests/Cpu6502.Tests/TransferTests.cs` | Utwórz |
