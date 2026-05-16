# Faza 23 — Test zgodności — perfect6502 (opcjonalnie)

| Właściwość | Wartość |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Pokrycie dokumentacji** | 3% (sekcje: 12.4) |
| **Pokrycie całości** | 100% |
| **Zależności** | Fazy: 1–22 |
| **Szacowany czas** | 4–8h |

---

## Cel fazy

Integracja z **perfect6502** — symulacją na poziomie tranzystorów — w celu weryfikacji cycle-by-cycle zgodności emulatora z prawdziwym sprzętem.

---

## Co implementujemy

### perfect6502 — opis

- Transistor-level symulacja 6502 (model z Visual6502)
- Implementacja w C (https://github.com/mist64/perfect6502)
- Dla każdej instrukcji dostarcza oczekiwany stan magistrali (address, data, R/W) w każdym cyklu
- Pozwala na porównanie cycle-by-cycle

### Podejścia do integracji

#### Opcja A: Generowanie test vectors offline

```csharp
// Wygeneruj pliki JSON z perfect6502 dla każdej instrukcji
// Wczytaj je w testach .NET
// Porównuj stan emulatora z oczekiwanym po każdym cyklu
```

#### Opcja B: P/Invoke do C library

```csharp
[DllImport("perfect6502.dll")]
static extern void perfect6502_reset();
[DllImport("perfect6502.dll")]
static extern ulong perfect6502_step();
[DllImport("perfect6502.dll")]
static extern ushort perfect6502_getAddress();
[DllImport("perfect6502.dll")]
static extern byte perfect6502_getData();
[DllImport("perfect6502.dll")]
static extern bool perfect6502_isWrite();
```

#### Opcja C: Port do C#

Przepisanie logiki perfect6502 w czystym C# (duży nakład pracy).

### Co porównujemy

Dla każdego cyklu każdej instrukcji:

```csharp
public class CycleByCycleVerifier
{
    public void VerifyInstruction(byte opcode, byte[] operands, byte[] initialRegs)
    {
        // 1. Ustaw oba "procesory" w tym samym stanie
        // 2. Wykonuj cykl po cyklu
        // 3. Porównaj:
        //    - Address bus (czy CPU czyta/zapisuje pod właściwy adres)
        //    - Data bus (czy wartość na szynie danych jest prawidłowa)
        //    - R/W (czy cykl jest odczytem czy zapisem)
        //    - Stan wewnętrzny po zakończeniu instrukcji
    }
}
```

### Minimalny zestaw do zweryfikowania

Jeśli pełna integracja jest zbyt kosztowna, zweryfikuj ręcznie najbardziej krytyczne instrukcje:
- **ADC/SBC** — z carry, overflow, BCD
- **Branch** — taken/not taken, page cross
- **JSR/RTS** — push/pull PC
- **BRK/IRQ/NMI** — sekwencje przerwań
- **R-M-W** — double write
- **JMP indirect** — page-cross bug

---

## Co testujemy

| Test | Opis |
|------|------|
| **Cycle-by-cycle dla LDA** | Każdy cykl — addr, data, R/W |
| **Cycle-by-cycle dla STA** | W tym dummy write |
| **Sekwencja RESET** | 7 cykli |
| **Sekwencja BRK** | 7 cykli |
| **Sekwencja IRQ** | 7 cykli |
| **Wszystkie udokumentowane instrukcje** | O ile zintegrowane |

---

## Sekcje dokumentacji

| Sekcja | Temat |
|--------|-------|
| 12.4 | perfect6502 |

---

## Definition of Done

- [ ] perfect6502 zintegrowany (poprzez test vectors, P/Invoke lub port)
- [ ] Cycle-by-cycle porównanie dla minimum 10 kluczowych instrukcji
- [ ] Zero niezgodności
- [ ] Wszystkie poprzednie testy nadal zielone

---

## Pliki

| Plik | Akcja |
|------|-------|
| `tests/Cpu6502.Tests/Perfect6502Tests.cs` | Utwórz |
| `tests/Cpu6502.Tests/Data/perfect6502_vectors/` | Dodaj |
