# Faza 31 - API uruchamiania Apple-1 i test end-to-end

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Publiczne API hosta i smoke test historycznego komputera |
| **Zaleznosci** | Fazy 24-30 |
| **Cel projektowy** | Wygodne uruchamianie Apple-1 bez znajomosci mapy pamieci |

---

## Cel fazy

Dostarczyc cienkie API hosta dla Apple-1 oparte o profil i generyczne komponenty. To API ma byc wygodne dla testow, CLI i przyszlego UI, ale nie moze obchodzic `ComputerBuilder`, `RuntimeBus` ani PIA.

---

## Publiczne API

```csharp
public static class Apple1ComputerFactory
{
    public static EmulatedComputer Create(ITerminalLink terminal, Apple1Options? options = null);
}
```

```csharp
public sealed class Apple1Host
{
    public void Reset();
    public void TypeText(string text);
    public string ReadOutputText();
    public RunResult RunUntilOutput(string expectedText, long maxInstructions);
    public RunResult RunUntilInputWait(long maxInstructions);
}
```

`Apple1Host` jest adapterem wygody. Nie moze zawierac recznej implementacji mapy `$D010-$D013`.

---

## Zachowanie

- `Create()` laduje profil `apple-1`.
- Terminal jest przekazywany jako zasob hosta/factory context.
- ROM WOZ Monitor moze byc opcjonalny. Test end-to-end z realnym ROM-em ma byc `[Explicit]`, jesli ROM nie jest w repo.
- Bez ROM-u host nadal moze byc testowany programem syntetycznym zapisujacym do DSP i czytajacym KBD.

---

## Kolejnosc wykonania dla agenta

1. Dodaj `Apple1Options`.
2. Dodaj `Apple1ComputerFactory`.
3. Dodaj `Apple1Host`.
4. Dodaj syntetyczny program testowy w RAM/ROM testowym, ktory czyta KBD i pisze DSP.
5. Dodaj testy hosta bez realnego WOZ ROM.
6. Dodaj `[Explicit]` smoke test WOZ Monitor, jesli plik ROM istnieje lokalnie.
7. Nie dodawaj realnego UI.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `Apple1Factory_Create_ReturnsComputerWithTerminalPia` | factory uzywa profilu |
| `Apple1Host_TypeText_QueuesTerminalInput` | wejscie hosta |
| `Apple1Host_ReadOutputText_ReturnsTerminalOutput` | wyjscie hosta |
| `Apple1SyntheticProgram_EchoesCharacterThroughPia` | end-to-end bez ROM-u |
| `Apple1WozMonitor_WhenRomPresent_ShowsPrompt` | explicit |

---

## Kryteria akceptacji

- Apple-1 da sie uruchomic jedna metoda factory.
- Test syntetyczny przechodzi bez zewnetrznego ROM-u.
- WOZ Monitor smoke test jest dostepny, ale nie psuje CI przy braku ROM-u.

