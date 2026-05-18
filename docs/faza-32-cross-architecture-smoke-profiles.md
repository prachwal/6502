# Faza 32 - Profile smoke dla wielu architektur

| Wlasciwosc | Wartosc |
|------------|---------|
| **Status** | [ ] Nie rozpoczęte |
| **Zakres** | Walidacja, ze architektura nie jest 6502-only |
| **Zaleznosci** | Fazy 24-31 |
| **Cel projektowy** | Przygotowac droge dla Z80/8080/6809 bez przepisywania runtime |

---

## Cel fazy

Dodac profile i testy smoke, ktore nie wymagaja pelnych rdzeni Z80/8080/6809, ale waliduja, ze profil, bus, porty i fabryki potrafia opisac maszyny innych architektur.

To jest faza architektoniczna, nie implementacja nowych CPU.

---

## Co implementujemy

- Fake/test CPU z osobna przestrzenia portow.
- Profil `test-z80-port-sbc` uzywajacy `mapping.kind = "port"`.
- Profil `test-6809-memory-mapped-sbc` uzywajacy memory-mapped I/O.
- Testy `ComputerBuilder`, ktore buduja te profile z fake CPU factory.
- Walidacje `AddressSpaceDescriptor` dla portow, braku portow i roznych data bus bits.

---

## Kolejnosc wykonania dla agenta

1. Dodaj testowy `FakePortCpuCore` tylko w testach.
2. Dodaj testowy `FakeMemoryMappedCpuCore` tylko w testach.
3. Dodaj profile smoke w `tests/Data/profiles` albo jako inline JSON.
4. Dodaj test port-mapped UART/fake device.
5. Dodaj test memory-mapped fake device dla CPU bez portow.
6. Nie dodawaj prawdziwego Z80 ani 6809.

---

## Testy wymagane

| Test | Wymaganie |
|------|-----------|
| `Build_Z80LikeProfile_WithPortDevice_Succeeds` | port space |
| `Build_6502LikeProfile_WithPortDevice_Fails` | walidacja |
| `Build_6809LikeProfile_WithMemoryMappedDevice_Succeeds` | memory-mapped |
| `RuntimeBus_PortRead_RoutesToPortDevice` | porty dzialaja |
| `RuntimeBus_MemoryRead_RoutesToMemoryMappedDevice` | pamiec dziala |

---

## Kryteria akceptacji

- Runtime nie ma juz zalozenia, ze kazdy komputer to 6502.
- Port-mapped I/O jest realnie testowane przez profil.
- Nowe CPU moga pozniej wejsc przez fabryke CPU, bez zmiany formatu profili.

