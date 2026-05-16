# Checklista faz implementacji symulatora 6502

Legenda statusu: `[ ]` = nie rozpoczęte | `[~]` = w trakcie | `[x]` = zakończone

## Legenda wskaźników

| Wskaźnik | Opis |
|----------|------|
| **Pokrycie dokumentacji** | % dokumentacji głównej, który dana faza implementuje |
| **Pokrycie całości** | Skumulowany % pełnej specyfikacji 6502 (instrukcje + timing + przerwania + quirk) |

---

| # | Faza | Plik | Status | % pokrycia dok. | % pokrycia całości |
|---|------|------|--------|----------------:|-------------------:|
| 0 | Szkielet projektu i struktura .NET | [faza-00-szkielet.md](faza-00-szkielet.md) | [x] | 100% | 1% |
| 1 | Load / Store — LDA, LDX, LDY, STA, STX, STY | [faza-01-load-store.md](faza-01-load-store.md) | [x] | 100% | 3% |
| 2 | Transfer między rejestrami — TAX, TAY, TSX, TXA, TXS, TYA | [faza-02-transfer.md](faza-02-transfer.md) | [ ] | 3% | 5% |
| 3 | Flagi Set/Clear — CLC, SEC, CLD, SED, CLI, SEI, CLV | [faza-03-flags.md](faza-03-flags.md) | [ ] | 3% | 7% |
| 4 | Arytmetyka binarna — ADC, SBC (bez BCD) | [faza-04-arithmetic.md](faza-04-arithmetic.md) | [ ] | 5% | 10% |
| 5 | Inkrementacja / Dekrementacja — INC, DEC, INX, INY, DEX, DEY | [faza-05-inc-dec.md](faza-05-inc-dec.md) | [ ] | 3% | 13% |
| 6 | Porównania i BIT — CMP, CPX, CPY, BIT | [faza-06-compare-bit.md](faza-06-compare-bit.md) | [ ] | 3% | 16% |
| 7 | Operacje logiczne — AND, ORA, EOR | [faza-07-logic.md](faza-07-logic.md) | [ ] | 3% | 19% |
| 8 | Przesunięcia i rotacje — ASL, LSR, ROL, ROR | [faza-08-shift-rotate.md](faza-08-shift-rotate.md) | [ ] | 4% | 23% |
| 9 | Skoki i rozgałęzienia — JMP, JSR, RTS, BCC, BCS, BEQ, BMI, BNE, BPL, BVC, BVS | [faza-09-branch-jump.md](faza-09-branch-jump.md) | [ ] | 6% | 30% |
| 10 | Stos i NOP — PHA, PHP, PLA, PLP, NOP | [faza-10-stack-nop.md](faza-10-stack-nop.md) | [ ] | 3% | 33% |
| 11 | Przerwania software — BRK, RTI | [faza-11-brk-rti.md](faza-11-brk-rti.md) | [ ] | 3% | 37% |
| 12 | Pełne tryby adresowania + page crossing | [faza-12-addressing.md](faza-12-addressing.md) | [ ] | 5% | 42% |
| 13 | Tryb BCD — ADC/SBC decimal mode | [faza-13-bcd.md](faza-13-bcd.md) | [ ] | 3% | 46% |
| 14 | Sekwencja RESET | [faza-14-reset.md](faza-14-reset.md) | [ ] | 3% | 50% |
| 15 | Przerwania sprzętowe — IRQ, NMI (podstawowa obsługa) | [faza-15-irq-nmi.md](faza-15-irq-nmi.md) | [ ] | 5% | 56% |
| 16 | Architektura cycle-stepped — Tick() per cykl | [faza-16-cycle-stepped.md](faza-16-cycle-stepped.md) | [ ] | 6% | 63% |
| 17 | R-M-W double write + quirk JMP indirect | [faza-17-rmw-quirks.md](faza-17-rmw-quirks.md) | [ ] | 4% | 68% |
| 18 | Nieudokumentowane opkody — stabilne | [faza-18-illegal-stable.md](faza-18-illegal-stable.md) | [ ] | 8% | 76% |
| 19 | Nieudokumentowane opkody — niestabilne + NOP + KIL | [faza-19-illegal-unstable.md](faza-19-illegal-unstable.md) | [ ] | 6% | 83% |
| 20 | Test zgodności — nestest | [faza-20-nestest.md](faza-20-nestest.md) | [ ] | 4% | 88% |
| 21 | Test zgodności — Klaus Dormann Functional Test | [faza-21-klaus.md](faza-21-klaus.md) | [ ] | 4% | 93% |
| 22 | Test zgodności — Wolfgang Lorenz | [faza-22-wolfgang.md](faza-22-wolfgang.md) | [ ] | 5% | 97% |
| 23 | Test zgodności — perfect6502 (opcjonalnie) | [faza-23-perfect6502.md](faza-23-perfect6502.md) | [ ] | 3% | 100% |

---

## Postęp faz

```
Całkowity postęp:     2 / 24 faz (8%)

Fazy zakończone   [x]: 0, 1
Fazy w trakcie     [~]:
Fazy nie rozpoczęte [ ]: 2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23
```
