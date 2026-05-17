# Dokumentacja projektowa — Symulator procesora MOS 6502 w .NET

## Spis treści

1. [Cel projektu](#1-cel-projektu)
2. [Architektura procesora](#2-architektura-procesora)
   - 2.1. Rejestry
   - 2.2. Rejestr Statusowy (Flag Register)
   - 2.3. Mapa pamięci
   - 2.4. Stos
   - 2.5. Wektory systemowe
   - 2.6. Kolejność bajtów (Little-Endian)
   - 2.7. Tryb BCD
   - 2.8. Inicjalizacja i reset
3. [Tryby adresowania](#3-tryby-adresowania)
4. [Pełna lista instrukcji](#4-pełna-lista-instrukcji)
   - 4.1. Legenda do tabel instrukcji
   - 4.2. Transfer danych (Load/Store/Transfer)
   - 4.3. Arytmetyka (ADC, SBC)
   - 4.4. Inkrementacja / Dekrementacja
   - 4.5. Przesunięcia i rotacje
   - 4.6. Operacje logiczne
   - 4.7. Porównania i test bitów
   - 4.8. Skoki i rozgałęzienia
   - 4.9. Stos
   - 4.10. Flagi (Set/Clear)
   - 4.11. Przerwania
   - 4.12. Inne (NOP)
5. [Nieudokumentowane instrukcje (NMOS 6502)](#5-nieudokumentowane-instrukcje-nmos-6502)
6. [Cykle i timing](#6-cykle-i-timing)
   - 6.1. Podstawowe informacje
   - 6.2. Przekraczanie granicy strony (page boundary crossing)
   - 6.3. Dodatkowe cykle w instrukcjach R-M-W
   - 6.4. Cykle poszczególnych instrukcji — tabela zbiorcza
7. [Przerwania](#7-przerwania)
   - 7.1. Rodzaje przerwań
   - 7.2. Wektory przerwań
   - 7.3. Sekwencja obsługi IRQ/NMI (sprzętowego)
   - 7.4. Instrukcja BRK (software interrupt)
   - 7.5. RESET
   - 7.6. Kolejność priorytetów
   - 7.7. Timing przerwań
   - 7.8. Interrupt hijacking (NMI podczas IRQ)
   - 7.9. Flaga I (Interrupt Disable)
   - 7.10. RTI (Return from Interrupt)
8. [Sekwencja RESET (start procesora)](#8-sekwencja-reset-start-procesora)
9. [Read-Modify-Write (R-M-W) i podwójny zapis](#9-read-modify-write-r-m-w-i-podwójny-zapis)
10. [Quirki i znane zachowania NMOS 6502](#10-quirki-i-znane-zachowania-nmos-6502)
11. [Kompletna mapa opkodów (tabela $00–$FF)](#11-kompletna-mapa-opkodów-tabela-00ff)
12. [Testy zgodności](#12-testy-zgodności)
    - 12.1. nestest (NEStest)
    - 12.2. Klaus Dormann 6502 Functional Test Suite
    - 12.3. Wolfgang Lorenz Test Suite
    - 12.4. perfect6502
    - 12.5. Testy BCD (decimal mode)
    - 12.6. Strategia implementacji testów
13. [Wymagania i struktura projektu .NET](#13-wymagania-i-struktura-projektu-net)
    - 13.1. Struktura rozwiązania
    - 13.2. Interfejs IMemoryBus
    - 13.3. Klasa Cpu6502 — pola
    - 13.4. Metoda Tick() — szkielet pętli cyklowej
    - 13.5. Wykonywanie instrukcji — szkielet switch-case
    - 13.6. Obsługa przerwań w Tick()
    - 13.7. Tryby adresowania — implementacja
14. [Materiały źródłowe](#14-materiały-źródłowe)

---

## 1. Cel projektu

Celem jest stworzenie **dokładnego binarnie (cycle-accurate) symulatora procesora MOS 6502** w języku C# przy użyciu platformy .NET. Symulator musi być w stanie:

- Wykonać każdą udokumentowaną instrukcję 6502 z poprawną liczbą cykli.
- Wykonać wszystkie **nieudokumentowane (illegal) opkody** NMOS 6502.
- Poprawnie obsłużyć przerwania (IRQ, NMI, RESET, BRK) z uwzględnieniem dokładnego timingu.
- Zaliczyć **test zgodności binarnych** takich jak: `nestest`, `Klaus Dormann Functional Test Suite`, `Wolfgang Lorenz Test Suite`, `perfect6502`.
- Architektura symulatora ma być **cycle-stepped** — każda metoda `Tick()` wykonuje dokładnie **jeden cykl zegara** procesora.

### Poziom zgodności: **Cycle-accurate**

- Każda instrukcja wykonuje się w dokładnie takiej liczbie cykli jak na prawdziwym sprzęcie.
- Przekroczenie granicy strony (page cross) dodaje +1 cykl w odpowiednich instrukcjach.
- Branch-not-taken = 2 cykle, branch-taken (ta sama strona) = 3 cykle, branch-taken (inna strona) = 4 cykle.
- Przerwania są sprawdzane na przedostatnim cyklu instrukcji.

---

## 2. Architektura procesora

### 2.1. Rejestry

| Rejestr | Rozmiar | Opis |
|---------|---------|------|
| **A** (Accumulator) | 8 bitów | Główny rejestr arytmetyczny. Wyniki operacji ALU lądują tutaj. |
| **X** (X Index) | 8 bitów | Rejestr indeksowy X. Używany do adresowania indeksowanego, może być inkrementowany/dekrementowany. |
| **Y** (Y Index) | 8 bitów | Rejestr indeksowy Y. Używany do adresowania indeksowanego. |
| **PC** (Program Counter) | 16 bitów | Wskaźnik bieżącej instrukcji. Jedyny 16-bitowy rejestr. |
| **S / SP** (Stack Pointer) | 8 bitów | Wskaźnik stosu (offset od $0100). Stos rośnie w dół. Wartość początkowa: **$FD** (po resecie SP zmniejsza się o 3 do $FD). |
| **P** (Processor Status Register) | 8 bitów | Rejestr flag. |

### 2.2. Rejestr Statusowy (P — Flag Register)

```
Bit:   7    6    5    4    3    2    1    0
Flag:  N    V    —    B    D    I    Z    C
       |    |    |    |    |    |    |    +-- Carry (C)
       |    |    |    |    |    |    +------- Zero (Z)
       |    |    |    |    |    +------------ Interrupt Disable (I)
       |    |    |    |    +----------------- Decimal Mode (D)
       |    |    |    +---------------------- Break (B)
       |    |    +--------------------------- (Unused) — zawsze 1 przy push, ignorowany przy pull
       |    +-------------------------------- Overflow (V)
       +------------------------------------- Negative (N)
```

| Flaga | Bit | Nazwa | Opis |
|-------|-----|-------|------|
| **C** | 0 | Carry | Przeniesienie/pożyczka w operacjach arytmetycznych. Bit wypychany przy shift/rotate. |
| **Z** | 1 | Zero | Ustawiana, gdy wynik operacji = 0. |
| **I** | 2 | Interrupt Disable | Blokuje przerwania IRQ. Nie dotyczy NMI ani RESET. Ustawiana automatycznie przy wejściu w interrupt handler. |
| **D** | 3 | Decimal | Włącza tryb BCD dla ADC i SBC. |
| **B** | 4 | Break | **Nie jest to fizyczna flaga w rejestrze!** Pojawia się tylko na stosie: =1 gdy push przez BRK/PHP, =0 gdy push przez IRQ/NMI sprzętowe. Ignorowany przy PLP/RTI. |
| **—** | 5 | Unused | Zawsze 1 na stosie. |
| **V** | 6 | Overflow | Nadmiar w arytmetyce ze znakiem. Ustawiany też przez BIT (bit 6 operandu). |
| **N** | 7 | Negative | Bit znaku (bit 7 wyniku). Ustawiany też przez BIT (bit 7 operandu). |

#### Zasady ustawiania flag

- **N, Z** — ustawiane zawsze przy: załadowaniu do rejestru (LDA, LDX, LDY, TAX, TAY, TSX, TXA, TYA, PLA), operacjach logicznych, inkrementacji/dekrementacji, shift/rotate, operacjach arytmetycznych.
- **C** — ustawiany przy: ADC (carry out), SBC (borrow), CMP/CPX/CPY (jak carry przy odejmowaniu), shift/rotate (wypchnięty bit).
- **V** — ustawiany przy: ADC, SBC (signed overflow), BIT (bit 6 operandu).
- **B, — (bit 5)** — tylko na stosie, nie w fizycznym rejestrze.

#### Uwaga: Flagi przy CMP/CPX/CPY

```
Relacja          Z   C   N
A < M            0   0   bit7(A-M)
A == M           1   1   0
A >= M           0/1 1   bit7(A-M)
```

---

### 2.3. Mapa pamięci

16-bitowa przestrzeń adresowa: **64 KB** ($0000–$FFFF).

| Zakres | Nazwa | Opis |
|--------|-------|------|
| $0000–$00FF | **Zero Page** | Strona zerowa — szybszy dostęp (adresowanie 1-bajtowe, -1 cykl). |
| $0100–$01FF | **Stack** | Stos procesora (256 bajtów). Rośnie od $01FF w dół. |
| $0200–$FFFF | **General Purpose** | Pamięć ogólnego przeznaczenia. Część zakresu może być zmapowana na I/O (memory-mapped I/O). |

### 2.4. Stos

- LIFO (Last In, First Out).
- Rośnie **w dół**: PUSH zapisuje bajt pod adresem $0100+SP, potem dekrementuje SP. PULL inkrementuje SP, potem czyta z $0100+SP.
- SP po resecie = **$FD** (czyli pierwszy PUSH trafi pod $01FD).
- Stos NIE ma detekcji przepełnienia — SP po prostu zawija się (wrap-around).

### 2.5. Wektory systemowe

| Adres | Przeznaczenie | Używany przez |
|-------|---------------|---------------|
| **$FFFA–$FFFB** | NMI vector | NMI |
| **$FFFC–$FFFD** | RESET vector | RESET |
| **$FFFE–$FFFF** | IRQ/BRK vector | IRQ, BRK |

Wektory są 16-bitowe, little-endian (low-byte pod pierwszym adresem, high-byte pod drugim).

### 2.6. Kolejność bajtów (Little-Endian)

Wszystkie 16-bitowe wartości w pamięci są przechowywane w formacie **little-endian**:

- Adres $1000: low-byte
- Adres $1001: high-byte

Przykład: `JMP ($1234)` — pobiera low-byte z $1234 i high-byte z $1235.

### 2.7. Tryb BCD (Decimal Mode)

- Gdy flaga D = 1, instrukcje **ADC** i **SBC** działają w trybie BCD.
- W trybie BCD każdy nibble (4 bity) reprezentuje cyfrę dziesiętną 0–9.
- Przykład: $09 + $01 = $10 (w BCD), a nie $0A (w trybie binarnym).
- Flagi N, V, Z są ustawiane **przed korekcją BCD** (na podstawie wyniku binarnego).
- Flaga C po korekcji BCD odzwierciedla przeniesienie dziesiętne.
- **Ważne**: Flagę V po ADC/SBC w trybie BCD można pominąć w pierwszej wersji — wiele testów (nestest) jej nie sprawdza.

### 2.8. Inicjalizacja i reset

Po resecie:

- **SP** = $FD (efektywnie, po 3 pseudo-pushach z $00 → zawija do $FD)
- **I** = 1 (interrupts disabled)
- **D** = 0
- Pozostałe flagi: **niezdefiniowane**
- **A, X, Y**: **niezdefiniowane**
- **PC** = wartość z wektora RESET ($FFFC–$FFFD)
- Sekwencja startowa: 7 cykli

---

## 3. Tryby adresowania

| Skrót | Nazwa | Format asemblera | Opis |
|-------|-------|------------------|------|
| **impl** | Implied | `CLC` | Operand wynika z instrukcji. 1-bajtowa. |
| **A** | Accumulator | `ASL A` | Operandem jest akumulator. 1-bajtowa. |
| **#** | Immediate | `LDA #$10` | Operand jest stałą z drugiego bajtu. 2-bajtowa. |
| **zp** | Zero Page | `LDA $10` | Adres w zero page ($00xx). 2-bajtowa. |
| **zp,X** | Zero Page, X | `LDA $10,X` | ($00LL + X) & $FF — bez carry. 2-bajtowa. |
| **zp,Y** | Zero Page, Y | `LDX $10,Y` | ($00LL + Y) & $FF — bez carry. 2-bajtowa. |
| **abs** | Absolute | `LDA $1234` | Pełny 16-bitowy adres. 3-bajtowa. |
| **abs,X** | Absolute, X | `LDA $1234,X` | Adres + X, z carry (może przekroczyć stronę). 3-bajtowa. |
| **abs,Y** | Absolute, Y | `LDA $1234,Y` | Adres + Y, z carry (może przekroczyć stronę). 3-bajtowa. |
| **(zp,X)** | Pre-indexed Indirect (X-indexed, indirect) | `LDA ($10,X)` | ($0010 + X) → pobiera 16-bit wskaźnik z zero page. 2-bajtowa. |
| **(zp),Y** | Post-indexed Indirect (Indirect, Y-indexed) | `LDA ($10),Y` | Pobiera 16-bit wskaźnik z ($0010), dodaje Y. Może przekroczyć stronę. 2-bajtowa. |
| **rel** | Relative | `BEQ $+5` | PC + signed offset (8-bit, -128..+127). 2-bajtowa. |
| **(abs)** | Absolute Indirect | `JMP ($1234)` | Pobiera 16-bit wskaźnik z adresu absolutnego. Tylko dla JMP. 3-bajtowa. |

### Uwagi o indirect JMP

Na **oryginalnym NMOS 6502**: przy odczycie wskaźnika nie ma carry na high-byte. `JMP ($12FF)` czyta low-byte z $12FF i high-byte z **$1200** (nie $1300!). Jest to **bug sprzętowy**. Na CMOS (65C02) jest to naprawione (+1 cykl na page boundary).

### Przekraczanie strony (Page Boundary Crossing)

Dotyczy trybów: **abs,X**, **abs,Y**, **(zp),Y**.

- Jeśli dodanie indeksu do adresu bazowego zmienia high-byte (przekracza granicę strony 256-bajtowej), dodawany jest **+1 cykl**.
- Dla instrukcji **zapisujących** (STA, STX, STY, …) w trybach abs,X i abs,Y **zawsze** jest dodatkowy cykl (bez względu na przekroczenie strony). Zobacz tabelę cykli.

---

## 4. Pełna lista instrukcji (udokumentowanych)

### 4.1. Legenda do tabel

- **Cykl**: Podstawowa liczba cykli bez page-cross penalty.
- **+p**: +1 cykl jeśli przekroczona granica strony (dla abs,X / abs,Y / (zp),Y).
- **+b**: +1 cykl jeśli branch taken na tej samej stronie, +2 jeśli branch taken na innej stronie.
- **(N,V,Z,C)**: + = flaga modyfikowana, - = niezmieniona, 1 = ustawiana na 1, 0 = zerowana, M6/M7 = pobierana z bitu pamięci.

---

### 4.2. Transfer danych (Load/Store/Transfer)

#### LDA — Load Accumulator

```
A ← M
Flagi: N, Z
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $A9 | 2 | 2 |
| zp | $A5 | 2 | 3 |
| zp,X | $B5 | 2 | 4 |
| abs | $AD | 3 | 4 |
| abs,X | $BD | 3 | 4+p |
| abs,Y | $B9 | 3 | 4+p |
| (zp,X) | $A1 | 2 | 6 |
| (zp),Y | $B1 | 2 | 5+p |

#### LDX — Load X Register

```
X ← M
Flagi: N, Z
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $A2 | 2 | 2 |
| zp | $A6 | 2 | 3 |
| zp,Y | $B6 | 2 | 4 |
| abs | $AE | 3 | 4 |
| abs,Y | $BE | 3 | 4+p |

#### LDY — Load Y Register

```
Y ← M
Flagi: N, Z
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $A0 | 2 | 2 |
| zp | $A4 | 2 | 3 |
| zp,X | $B4 | 2 | 4 |
| abs | $AC | 3 | 4 |
| abs,X | $BC | 3 | 4+p |

#### STA — Store Accumulator

```
M ← A
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $85 | 2 | 3 |
| zp,X | $95 | 2 | 4 |
| abs | $8D | 3 | 4 |
| abs,X | $9D | 3 | 5 |
| abs,Y | $99 | 3 | 5 |
| (zp,X) | $81 | 2 | 6 |
| (zp),Y | $91 | 2 | 6 |

#### STX — Store X Register

```
M ← X
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $86 | 2 | 3 |
| zp,Y | $96 | 2 | 4 |
| abs | $8E | 3 | 4 |

#### STY — Store Y Register

```
M ← Y
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $84 | 2 | 3 |
| zp,X | $94 | 2 | 4 |
| abs | $8C | 3 | 4 |

#### Transfer między rejestrami

| Instrukcja | Opis | Opcode | Bajty | Cykle | Flagi |
|------------|------|--------|-------|-------|-------|
| **TAX** | X ← A | $AA | 1 | 2 | N, Z |
| **TAY** | Y ← A | $A8 | 1 | 2 | N, Z |
| **TSX** | X ← SP | $BA | 1 | 2 | N, Z |
| **TXA** | A ← X | $8A | 1 | 2 | N, Z |
| **TXS** | SP ← X | $9A | 1 | 2 | brak |
| **TYA** | A ← Y | $98 | 1 | 2 | N, Z |

---

### 4.3. Arytmetyka (ADC, SBC)

#### ADC — Add with Carry

```
A ← A + M + C
Flagi: N, V, Z, C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $69 | 2 | 2 |
| zp | $65 | 2 | 3 |
| zp,X | $75 | 2 | 4 |
| abs | $6D | 3 | 4 |
| abs,X | $7D | 3 | 4+p |
| abs,Y | $79 | 3 | 4+p |
| (zp,X) | $61 | 2 | 6 |
| (zp),Y | $71 | 2 | 5+p |

#### SBC — Subtract with Carry (Borrow)

```
A ← A - M - (1 - C)   =  A - M - ~C
Flagi: N, V, Z, C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $E9 | 2 | 2 |
| zp | $E5 | 2 | 3 |
| zp,X | $F5 | 2 | 4 |
| abs | $ED | 3 | 4 |
| abs,X | $FD | 3 | 4+p |
| abs,Y | $F9 | 3 | 4+p |
| (zp,X) | $E1 | 2 | 6 |
| (zp),Y | $F1 | 2 | 5+p |

#### Algorytm dla ADC w trybie binarnym

```csharp
ushort sum = (ushort)(A + M + (C ? 1 : 0));
C = sum > 0xFF;
Z = (sum & 0xFF) == 0;
V = ((A ^ sum) & (M ^ sum) & 0x80) != 0;  // signed overflow
N = (sum & 0x80) != 0;
A = (byte)(sum & 0xFF);
```

#### Algorytm dla SBC w trybie binarnym

```csharp
// SBC = A - M - ~C = A + (~M) + C
ushort sum = (ushort)(A + (byte)(~M) + (C ? 1 : 0));
C = sum > 0xFF;
Z = (sum & 0xFF) == 0;
V = ((A ^ sum) & ((byte)(~M) ^ sum) & 0x80) != 0;
N = (sum & 0x80) != 0;
A = (byte)(sum & 0xFF);
```

---

### 4.4. Inkrementacja / Dekrementacja

| Instrukcja | Opis | Tryb | Opcode | Bajty | Cykle | Flagi |
|------------|------|------|--------|-------|-------|-------|
| **INC** | M ← M + 1 | zp | $E6 | 2 | 5 | N, Z |
| | | zp,X | $F6 | 2 | 6 | |
| | | abs | $EE | 3 | 6 | |
| | | abs,X | $FE | 3 | 7 | |
| **INX** | X ← X + 1 | impl | $E8 | 1 | 2 | N, Z |
| **INY** | Y ← Y + 1 | impl | $C8 | 1 | 2 | N, Z |
| **DEC** | M ← M − 1 | zp | $C6 | 2 | 5 | N, Z |
| | | zp,X | $D6 | 2 | 6 | |
| | | abs | $CE | 3 | 6 | |
| | | abs,X | $DE | 3 | 7 | |
| **DEX** | X ← X − 1 | impl | $CA | 1 | 2 | N, Z |
| **DEY** | Y ← Y − 1 | impl | $88 | 1 | 2 | N, Z |

---

### 4.5. Przesunięcia i rotacje

Wszystkie shift/rotate: **N, Z, C** modyfikowane. V — niezmienione (N, Z, C = +++, V = ---).

#### ASL — Arithmetic Shift Left

```
C ← [7 6 5 4 3 2 1 0] ← 0
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| A | $0A | 1 | 2 |
| zp | $06 | 2 | 5 |
| zp,X | $16 | 2 | 6 |
| abs | $0E | 3 | 6 |
| abs,X | $1E | 3 | 7 |

#### LSR — Logical Shift Right

```
0 → [7 6 5 4 3 2 1 0] → C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| A | $4A | 1 | 2 |
| zp | $46 | 2 | 5 |
| zp,X | $56 | 2 | 6 |
| abs | $4E | 3 | 6 |
| abs,X | $5E | 3 | 7 |

#### ROL — Rotate Left

```
C ← [7 6 5 4 3 2 1 0] ← C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| A | $2A | 1 | 2 |
| zp | $26 | 2 | 5 |
| zp,X | $36 | 2 | 6 |
| abs | $2E | 3 | 6 |
| abs,X | $3E | 3 | 7 |

#### ROR — Rotate Right

```
C → [7 6 5 4 3 2 1 0] → C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| A | $6A | 1 | 2 |
| zp | $66 | 2 | 5 |
| zp,X | $76 | 2 | 6 |
| abs | $6E | 3 | 6 |
| abs,X | $7E | 3 | 7 |

---

### 4.6. Operacje logiczne

Wszystkie: **N, Z** modyfikowane (++), **C, V** niezmienione (-).

#### AND — Logical AND

```
A ← A & M
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $29 | 2 | 2 |
| zp | $25 | 2 | 3 |
| zp,X | $35 | 2 | 4 |
| abs | $2D | 3 | 4 |
| abs,X | $3D | 3 | 4+p |
| abs,Y | $39 | 3 | 4+p |
| (zp,X) | $21 | 2 | 6 |
| (zp),Y | $31 | 2 | 5+p |

#### ORA — Logical OR

```
A ← A | M
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $09 | 2 | 2 |
| zp | $05 | 2 | 3 |
| zp,X | $15 | 2 | 4 |
| abs | $0D | 3 | 4 |
| abs,X | $1D | 3 | 4+p |
| abs,Y | $19 | 3 | 4+p |
| (zp,X) | $01 | 2 | 6 |
| (zp),Y | $11 | 2 | 5+p |

#### EOR — Exclusive OR (XOR)

```
A ← A ^ M
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $49 | 2 | 2 |
| zp | $45 | 2 | 3 |
| zp,X | $55 | 2 | 4 |
| abs | $4D | 3 | 4 |
| abs,X | $5D | 3 | 4+p |
| abs,Y | $59 | 3 | 4+p |
| (zp,X) | $41 | 2 | 6 |
| (zp),Y | $51 | 2 | 5+p |

---

### 4.7. Porównania i test bitów

#### CMP — Compare Accumulator

```
A - M  (wynik wpływa na flagi, ale A niezmienione)
Flagi: N, Z, C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $C9 | 2 | 2 |
| zp | $C5 | 2 | 3 |
| zp,X | $D5 | 2 | 4 |
| abs | $CD | 3 | 4 |
| abs,X | $DD | 3 | 4+p |
| abs,Y | $D9 | 3 | 4+p |
| (zp,X) | $C1 | 2 | 6 |
| (zp),Y | $D1 | 2 | 5+p |

#### CPX — Compare X Register

```
X - M  (flagi, X niezmienione)
Flagi: N, Z, C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $E0 | 2 | 2 |
| zp | $E4 | 2 | 3 |
| abs | $EC | 3 | 4 |

#### CPY — Compare Y Register

```
Y - M  (flagi, Y niezmienione)
Flagi: N, Z, C
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| # | $C0 | 2 | 2 |
| zp | $C4 | 2 | 3 |
| abs | $CC | 3 | 4 |

#### BIT — Bit Test

```
A & M → Z
bit7(M) → N
bit6(M) → V
Flagi: N=M7, V=M6, Z=(A&M)==0
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| zp | $24 | 2 | 3 |
| abs | $2C | 3 | 4 |

---

### 4.8. Skoki i rozgałęzienia

#### JMP — Jump

```
PC ← addr
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| abs | $4C | 3 | 3 |
| (abs) | $6C | 3 | 5 |

#### JSR — Jump to Subroutine

```
Push PC+2 (HB, potem LB); PC ← addr
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| abs | $20 | 3 | 6 |

#### RTS — Return from Subroutine

```
Pull PC (LB, potem HB); PC ← PC+1
Flagi: brak
```

| Tryb | Opcode | Bajty | Cykle |
|------|--------|-------|-------|
| impl | $60 | 1 | 6 |

#### Branch Instructions (8 instrukcji)

Wszystkie branch: 2 bajty (opcode + signed offset). Flagi bez zmian.

- Branch **NOT taken**: 2 cykle
- Branch **taken, same page**: 3 cykle (+1)
- Branch **taken, different page**: 4 cykle (+2)

| Instrukcja | Opcode | Warunek |
|------------|--------|---------|
| **BCC** | $90 | C = 0 |
| **BCS** | $B0 | C = 1 |
| **BEQ** | $F0 | Z = 1 |
| **BMI** | $30 | N = 1 |
| **BNE** | $D0 | Z = 0 |
| **BPL** | $10 | N = 0 |
| **BVC** | $50 | V = 0 |
| **BVS** | $70 | V = 1 |

---

### 4.9. Stos

| Instrukcja | Opis | Tryb | Opcode | Bajty | Cykle | Flagi |
|------------|------|------|--------|-------|-------|-------|
| **PHA** | Push A | impl | $48 | 1 | 3 | brak |
| **PHP** | Push P (z B=1, bit5=1) | impl | $08 | 1 | 3 | brak |
| **PLA** | Pull A | impl | $68 | 1 | 4 | N, Z |
| **PLP** | Pull P (B i bit5 ignorowane) | impl | $28 | 1 | 4 | zależne od wartości |

---

### 4.10. Flagi (Set/Clear)

| Instrukcja | Opis | Tryb | Opcode | Bajty | Cykle | Efekt |
|------------|------|------|--------|-------|-------|-------|
| **CLC** | Clear Carry | impl | $18 | 1 | 2 | C=0 |
| **SEC** | Set Carry | impl | $38 | 1 | 2 | C=1 |
| **CLD** | Clear Decimal | impl | $D8 | 1 | 2 | D=0 |
| **SED** | Set Decimal | impl | $F8 | 1 | 2 | D=1 |
| **CLI** | Clear Interrupt Disable | impl | $58 | 1 | 2 | I=0 |
| **SEI** | Set Interrupt Disable | impl | $78 | 1 | 2 | I=1 |
| **CLV** | Clear Overflow | impl | $B8 | 1 | 2 | V=0 |

---

### 4.11. Przerwania

| Instrukcja | Opis | Tryb | Opcode | Bajty | Cykle |
|------------|------|------|--------|-------|-------|
| **BRK** | Break (software interrupt) | impl | $00 | 1 | 7 |
| **RTI** | Return from Interrupt | impl | $40 | 1 | 6 |

---

### 4.12. Inne (NOP)

| Instrukcja | Opis | Tryb | Opcode | Bajty | Cykle |
|------------|------|------|--------|-------|-------|
| **NOP** | No Operation | impl | $EA | 1 | 2 |

---

## 5. Nieudokumentowane instrukcje (NMOS 6502)

Niektóre z tych instrukcji są **niestabilne** — ich działanie zależy od temperatury, serii układu, itp. Dla celów testów zgodności (np. Wolfgang Lorenz) muszą być zaimplementowane.

### Stabilne nieudokumentowane opkody

| Mnemonic | Opis | Opcode | Bajty | Cykle |
|----------|------|--------|-------|-------|
| **DCP** (DCM) | DEC + CMP: M ← M-1; A-M → flags | $C7/$D7/$CF/$DF/$DB/$C3/$D3 | 2-3 | 5-8 |
| **ISC** (ISB/INS) | INC + SBC: M ← M+1; A ← A-M-~C | $E7/$F7/$EF/$FF/$FB/$E3/$F3 | 2-3 | 5-8 |
| **LAX** | LDA + LDX: A ← X ← M | $A7/$B7/$AF/$BF/$A3/$B3 | 2-3 | 3-6 |
| **RLA** | ROL + AND: M ← ROL; A ← A&M | $27/$37/$2F/$3F/$3B/$23/$33 | 2-3 | 5-8 |
| **RRA** | ROR + ADC: M ← ROR; A ← A+M+C | $67/$77/$6F/$7F/$7B/$63/$73 | 2-3 | 5-8 |
| **SLO** (ASO) | ASL + ORA: M ← ASL; A ← A\|M | $07/$17/$0F/$1F/$1B/$03/$13 | 2-3 | 5-8 |
| **SRE** (LSE) | LSR + EOR: M ← LSR; A ← A^M | $47/$57/$4F/$5F/$5B/$43/$53 | 2-3 | 5-8 |
| **SAX** (AXS/AAX) | M ← A & X | $87/$97/$8F/$83 | 2-3 | 3-6 |
| **ANC** | AND + set C as ASL | $0B/$2B | 2 | 2 |
| **ALR** (ASR) | AND + LSR | $4B | 2 | 2 |
| **ARR** | AND + ROR (V z adder) | $6B | 2 | 2 |
| **SBX** (AXS/SAX) | X ← (A&X) - M | $CB | 2 | 2 |
| **LAS** (LAR) | A,X,SP ← M & SP | $BB | 3 | 4+p |

### Niestabilne opkody (używać tylko jeśli wymagają tego testy)

| Mnemonic | Opis | Opcode | Uwagi |
|----------|------|--------|-------|
| **ANE** (XAA) | A ← (A OR CONST) & X & M | $8B | Stała magiczna (zwykle $FF, $00, $EE) |
| **LXA** (LAX imm.) | A←X←(A OR CONST) & M | $AB | Jak ANE |
| **SHA** (AHX/AXA) | M ← A & X & (H+1) | $9F/$93 | |
| **SHX** (A11/SXA/XAS) | M ← X & (H+1) | $9E | |
| **SHY** (A11/SYA/SAY) | M ← Y & (H+1) | $9C | |
| **TAS** (XAS/SHS) | SP ← A & X; M ← SP & (H+1) | $9B | |
| **USBC** (SBC) | SBC + NOP | $EB | Zachowuje się jak SBC # |

### NOP-y nieudokumentowane

Opkody: $04, $44, $64, $14, $34, $54, $74, $0C, $1C, $3C, $5C, $7C, $DC, $FC, $80, $82, $89, $C2, $E2 — wykonują odczyt (niektóre z dodatkowymi efektami). Dla zgodności z testami Wolfgang Lorenz należy je zaimplementować.

### Instrukcje "zabijające" (KIL/JAM/HLT)

Opkody: $02, $12, $22, $32, $42, $52, $62, $72, $92, $B2, $D2, $F2 — zatrzymują procesor (w nieskończonej pętli, zwiększając PC bez końca? — zależnie od implementacji).

---

## 6. Cykle i timing

### 6.1. Podstawowe informacje

Każdy cykl procesora to jeden odczyt lub zapis pamięci.

- Prędkość oryginalnego 6502: **1 MHz** (dla NMOS), do 14 MHz dla W65C02S.
- Każdy cykl to dostęp do pamięci (odczyt lub zapis).
- W symulatorze cycle-stepped, każda metoda `Tick()` wykonuje 1 cykl.

### 6.2. Przekraczanie granicy strony

- Strona = 256 bajtów (high-byte adresu stały).
- Tryby: **abs,X**, **abs,Y**, **(zp),Y** — jeśli dodanie indeksu zmienia high-byte, +1 cykl.
- Tryby: **zp,X**, **zp,Y**, **(zp,X)** — **nie** dodają cyklu (zawijają w zero page).
- Dla instrukcji **store** (STA, STX, STY, SAX, SHA) w trybach abs,X i abs,Y — liczba cykli to zawsze **5** (dla abs,X/Y) niezależnie od page crossing.

### 6.3. Dodatkowe cykle w instrukcjach R-M-W

Instrukcje Read-Modify-Write (ASL, LSR, ROL, ROR, INC, DEC, DCP, ISC, RLA, RRA, SLO, SRE) na adresach absolutnych wykonują:

1. Odczyt wartości
2. Zapis niezmodyfikowanej wartości (NMOS quirk)
3. Modyfikacja
4. Zapis zmodyfikowanej wartości

W NMOS 6502 zapis podwójny ma znaczenie dla urządzeń I/O — należy go emulować.

### 6.4. Cykle poszczególnych instrukcji — tabela podsumowująca

| Instrukcja | Tryby | Cykle (bez penalty) |
|------------|-------|---------------------|
| ADC | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| AND | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| ASL | A, zp, zp,X, abs, abs,X | 2,5,6,6,7 |
| BCC/BCS/BEQ/... | rel | 2 (not taken), 3 (taken same page), 4 (taken diff page) |
| BIT | zp, abs | 3,4 |
| BRK | impl | 7 |
| CLC/CLD/CLI/CLV | impl | 2 |
| CMP | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| CPX | #, zp, abs | 2,3,4 |
| CPY | #, zp, abs | 2,3,4 |
| DEC | zp, zp,X, abs, abs,X | 5,6,6,7 |
| DEX/DEY | impl | 2 |
| EOR | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| INC | zp, zp,X, abs, abs,X | 5,6,6,7 |
| INX/INY | impl | 2 |
| JMP | abs, (abs) | 3,5 |
| JSR | abs | 6 |
| LDA | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| LDX | #, zp, zp,Y, abs, abs,Y | 2,3,4,4,4+p |
| LDY | #, zp, zp,X, abs, abs,X | 2,3,4,4,4+p |
| LSR | A, zp, zp,X, abs, abs,X | 2,5,6,6,7 |
| NOP | impl | 2 |
| ORA | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| PHA | impl | 3 |
| PHP | impl | 3 |
| PLA | impl | 4 |
| PLP | impl | 4 |
| ROL | A, zp, zp,X, abs, abs,X | 2,5,6,6,7 |
| ROR | A, zp, zp,X, abs, abs,X | 2,5,6,6,7 |
| RTI | impl | 6 |
| RTS | impl | 6 |
| SBC | #, zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 2,3,4,4,4+p,4+p,6,5+p |
| SEC/SED/SEI | impl | 2 |
| STA | zp, zp,X, abs, abs,X, abs,Y, (zp,X), (zp),Y | 3,4,4,5,5,6,6 |
| STX | zp, zp,Y, abs | 3,4,4 |
| STY | zp, zp,X, abs | 3,4,4 |
| TAX/TAY/TSX/TXA/TXS/TYA | impl | 2 |

---

## 7. Przerwania

### 7.1. Rodzaje przerwań

| Typ | Źródło | Maskowalne | Wektor | Edge/Level |
|-----|--------|------------|--------|------------|
| **RESET** | Pin RES (low-active) | Nie | $FFFC | Level (low active) |
| **NMI** | Pin NMI (low-active) | Nie | $FFFA | Edge (negative edge) |
| **IRQ** | Pin IRQ (low-active) | Tak (flaga I) | $FFFE | Level (low active) |
| **BRK** | Instrukcja BRK ($00) | — | $FFFE | Software |

### 7.2. Wektory przerwań

| Adres | Wektor | Używany przez |
|-------|--------|---------------|
| $FFFA–$FFFB | NMI vector | NMI |
| $FFFC–$FFFD | RESET vector | RESET |
| $FFFE–$FFFF | IRQ/BRK vector | IRQ, BRK |

### 7.3. Sekwencja obsługi IRQ/NMI (sprzętowego)

Cykl po cyklu (identyczna jak BRK, ale z różnicami w B-flag i wektorze):

1. **Cykl 1**: Fetch opcode (zawsze $00 — wstrzyknięty przez logikę przerwań, NIE z pamięci) — PC nie zmieniane.
2. **Cykl 2**: Read z pamięci (discard / dummy read).
3. **Cykl 3**: Push PCH na stos ($0100+SP, potem SP--).
4. **Cykl 4**: Push PCL na stos ($0100+SP, potem SP--).
5. **Cykl 5**: Push P na stos ($0100+SP, potem SP--).
   - Dla IRQ/BRK: B-flag = 1 jeśli BRK, = 0 jeśli IRQ.
   - Dla NMI: B-flag = 0.
   - Bit 5 zawsze = 1.
6. **Cykl 6**: Fetch low-byte wektora:
   - IRQ/BRK: $FFFE
   - NMI: $FFFA
7. **Cykl 7**: Fetch high-byte wektora i załadowanie PC. Flaga I ← 1.

Łącznie: **7 cykli**.

### 7.4. Instrukcja BRK (software interrupt)

- Działa identycznie jak IRQ z tym że:
  - PC+2 jest pushowane (nie PC+1) — "breaking byte" po opcodzie jest pomijany.
  - B-flag = 1 na stosie.
  - Używa wektora $FFFE/$FFFF.
  - I flaga **nie** jest ustawiana automatycznie przez BRK (ustawiana jest tylko przy sprzętowym IRQ/NMI).

### 7.5. RESET

Szczegółowa sekwencja opisana w [rozdziale 8](#8-sekwencja-reset-start-procesora).

### 7.6. Kolejność priorytetów

1. **RESET** (najwyższy — natychmiastowy)
2. **NMI**
3. **IRQ**
4. **BRK** (najniższy — tylko jeśli żadne sprzętowe nie jest aktywne)

### 7.7. Timing przerwań

- Przerwania są **sprawdzane na przedostatnim cyklu instrukcji** (penultimate cycle).
- IRQ jest level-triggered: jeśli pin IRQ pozostaje niski w momencie sprawdzenia, przerwanie jest obsłużone.
- NMI jest edge-triggered: CPU wykrywa opadające zbocze na pinie NMI. Jeśli NMI zostanie wykryte, jest zatrzaskiwane wewnętrznie.
- Jeśli IRQ zostanie wykryte, ale zanim dojdzie do obsługi, pojawi się NMI — IRQ jest "hijackowane" i kończy się jako NMI (interrupt hijacking).

### 7.8. Interrupt hijacking (NMI podczas IRQ)

Jeśli w trakcie sekwencji obsługi IRQ (konkretnie — przed pobraniem wektora) zostanie wykryty NMI:

- Sekwencja IRQ jest kontynuowana, ale:
  - Wektor IRQ ($FFFE) jest zamieniany na NMI ($FFFA)
  - B-flag = 0 (jak przy NMI)
  - W efekcie obsłużone zostaje NMI zamiast IRQ

### 7.9. Flaga I (Interrupt Disable)

- Ustawiana automatycznie przy wejściu w handler IRQ/NMI.
- Blokuje IRQ, ale nie NMI.
- SEI ustawia I, CLI czyści I.
- Zmiana I działa z **opóźnieniem o 1 instrukcję** — CLI przed następną instrukcją nie włączy przerwań natychmiast.

### 7.10. RTI (Return from Interrupt)

Sekwencja (6 cykli):

1. Pull P ze stosu (SP++, read $0100+SP). B-flag i bit 5 są ignorowane.
2. Pull PCL (SP++, read $0100+SP).
3. Pull PCH (SP++, read $0100+SP).

---

## 8. Sekwencja RESET (start procesora)

Reset jest **level-triggered** przez pin RES (active low). Gdy pin RES idzie z low na high, procesor wykonuje sekwencję startową trwającą **7 cykli**.

### Przebieg cykl po cyklu

```
Cykl 0: RESET active → IR = 00 (wstrzyknięcie BRK)
Cykl 1: Dummy read — adres zależy od stanu rejestrów
Cykl 2: Dummy read
Cykl 3: Dummy read z $0100+SP (symulacja push PCH) — SP--
Cykl 4: Dummy read z $0100+SP (symulacja push PCL) — SP--
Cykl 5: Dummy read z $0100+SP (symulacja push P)  — SP--
Cykl 6: Fetch PCL z $FFFC
Cykl 7: Fetch PCH z $FFFD  →  PC załadowane
Cykl 8: Pierwsza prawdziwa instrukcja (fetch opcode)
```

### Stan po resecie

| Rejestr | Stan |
|---------|------|
| SP | $FD (zmniejszone z $00 przez 3 pseudo-pushy → wrap-around do $FD) |
| I | 1 |
| D | 0 |
| A, X, Y | Niezdefiniowane |
| P (pozostałe flagi) | Niezdefiniowane |
| PC | Wartość z wektora RESET ($FFFC–$FFFD) |

### Różnice między RESET a BRK/IRQ/NMI

- Cykle 3–5 wykonują **odczyt** zamiast zapisu — R/W pin pozostaje w stanie read.
- Wartości odczytane są ignorowane (discard).
- SP i tak jest dekrementowane.

W symulatorze: można po prostu dekrementować SP o 3 i ustawić I=1, D=0.

---

## 9. Read-Modify-Write (R-M-W) i podwójny zapis

Instrukcje R-M-W (ASL, LSR, ROL, ROR, INC, DEC, DCP, ISC, RLA, RRA, SLO, SRE) wykonują:

1. Odczyt bajtu z pamięci.
2. **Zapis niezmodyfikowanego bajtu** z powrotem pod ten sam adres.
3. Modyfikacja wartości w CPU.
4. Zapis zmodyfikowanego bajtu.

To zachowanie NMOS 6502 jest istotne dla urządzeń I/O, które reagują na każdy zapis. W symulatorze należy to odtworzyć: w odpowiednim cyklu wywołać `Write(addr, originalValue)`, a potem `Write(addr, modifiedValue)`.

---

## 10. Quirki i znane zachowania NMOS 6502

### JMP indirect page-crossing bug

`JMP ($xxFF)` — low-byte wskaźnika czytany z $xxFF, high-byte czytane z **$xx00** (nie $xx+1 00). To bug hardware'owy NMOS 6502. Naprawiony w CMOS (65C02).

### Branch wraparound

Branch z offsetem $00 przechodzi do następnej instrukcji (branch always taken). Branch z offsetem $80 cofa się o 128 bajtów.

### BRK vs IRQ na stosie

Jedyną różnicą między BRK a sprzętowym IRQ na stosie jest bit B w zapisanym P:

- BRK: B=1, bit5=1
- IRQ sprzętowe: B=0, bit5=1

### NMI edge detect

NMI wykrywa zbocze opadające. Jeśli NMI jest trzymane nisko przez dłuższy czas, pojedyncze NMI jest obsłużone tylko raz (aż do kolejnego zbocza).

### Interrupt podczas branch

Specjalny przypadek: jeśli branch jest taken, ale do innej strony, a IRQ pojawia się podczas cyklu 3 branche'a — może być obsłużone z 1-cyklowym opóźnieniem zamiast 2-cyklowym.

---

## 11. Kompletna mapa opkodów (tabela $00–$FF)

Poniżej pełna tabela 16×16 wszystkich 256 opkodów. Format: `MNEMONIC TRYB`.

```text
        x0          x1          x2          x3          x4          x5          x6          x7          x8          x9          xA          xB          xC          xD          xE          xF
0x   BRK impl   ORA (zp,X)   KIL        SLO (zp,X)   NOP zp     ORA zp      ASL zp      SLO zp      PHP impl    ORA #       ASL A       ANC #       NOP abs    ORA abs     ASL abs     SLO abs
1x   BPL rel    ORA (zp),Y   KIL        SLO (zp),Y   NOP zp,X   ORA zp,X    ASL zp,X    SLO zp,X    CLC impl    ORA abs,Y   NOP         SLO abs,Y   NOP abs,X  ORA abs,X   ASL abs,X   SLO abs,X
2x   JSR abs    AND (zp,X)   KIL        RLA (zp,X)   BIT zp     AND zp      ROL zp      RLA zp      PLP impl    AND #       ROL A       ANC #       BIT abs    AND abs     ROL abs     RLA abs
3x   BMI rel    AND (zp),Y   KIL        RLA (zp),Y   NOP zp,X   AND zp,X    ROL zp,X    RLA zp,X    SEC impl    AND abs,Y   NOP         RLA abs,Y   NOP abs,X  AND abs,X   ROL abs,X   RLA abs,X
4x   RTI impl   EOR (zp,X)   KIL        SRE (zp,X)   NOP zp     EOR zp      LSR zp      SRE zp      PHA impl    EOR #       LSR A       ALR #       JMP abs    EOR abs     LSR abs     SRE abs
5x   BVC rel    EOR (zp),Y   KIL        SRE (zp),Y   NOP zp,X   EOR zp,X    LSR zp,X    SRE zp,X    CLI impl    EOR abs,Y   NOP         SRE abs,Y   NOP abs,X  EOR abs,X   LSR abs,X   SRE abs,X
6x   RTS impl   ADC (zp,X)   KIL        RRA (zp,X)   NOP zp     ADC zp      ROR zp      RRA zp      PLA impl    ADC #       ROR A       ARR #       JMP (abs)  ADC abs     ROR abs     RRA abs
7x   BVS rel    ADC (zp),Y   KIL        RRA (zp),Y   NOP zp,X   ADC zp,X    ROR zp,X    RRA zp,X    SEI impl    ADC abs,Y   NOP         RRA abs,Y   NOP abs,X  ADC abs,X   ROR abs,X   RRA abs,X
8x   NOP #      STA (zp,X)   NOP #      SAX (zp,X)   STY zp     STA zp      STX zp      SAX zp      DEY impl    NOP #       TXA impl    ANE #       STY abs    STA abs     STX abs     SAX abs
9x   BCC rel    STA (zp),Y   KIL        SHA (zp),Y   STY zp,X   STA zp,X    STX zp,Y    SAX zp,Y    TYA impl    STA abs,Y   TXS impl    TAS abs,Y   SHY abs,X  STA abs,X   SHX abs,Y   SHA abs,Y
Ax   LDY #      LDA (zp,X)   LDX #      LAX (zp,X)   LDY zp     LDA zp      LDX zp      LAX zp      TAY impl    LDA #       TAX impl    LXA #       LDY abs    LDA abs     LDX abs     LAX abs
Bx   BCS rel    LDA (zp),Y   KIL        LAX (zp),Y   LDY zp,X   LDA zp,X    LDX zp,Y    LAX zp,Y    CLV impl    LDA abs,Y   TSX impl    LAS abs,Y   LDY abs,X  LDA abs,X   LDX abs,Y   LAX abs,Y
Cx   CPY #      CMP (zp,X)   NOP #      DCP (zp,X)   CPY zp     CMP zp      DEC zp      DCP zp      INY impl    CMP #       DEX impl    SBX #       CPY abs    CMP abs     DEC abs     DCP abs
Dx   BNE rel    CMP (zp),Y   KIL        DCP (zp),Y   NOP zp,X   CMP zp,X    DEC zp,X    DCP zp,X    CLD impl    CMP abs,Y   NOP         DCP abs,Y   NOP abs,X  CMP abs,X   DEC abs,X   DCP abs,X
Ex   CPX #      SBC (zp,X)   NOP #      ISC (zp,X)   CPX zp     SBC zp      INC zp      ISC zp      INX impl    SBC #       NOP impl    SBC #       CPX abs    SBC abs     INC abs     ISC abs
Fx   BEQ rel    SBC (zp),Y   KIL        ISC (zp),Y   NOP zp,X   SBC zp,X    INC zp,X    ISC zp,X    SED impl    SBC abs,Y   NOP         ISC abs,Y   NOP abs,X  SBC abs,X   INC abs,X   ISC abs,X
```

---

## 12. Testy zgodności

### 12.1. nestest (NEStest)

- Plik: `nestest.nes`
- Test bazuje na logach, które pokazują stan rejestrów po każdej instrukcji.
- Sprawdza tylko **udokumentowane instrukcje** (nie sprawdza illegal opcodes).
- **Nie testuje trybu BCD**.
- Polega na porównaniu logu — `nestest.log` zawiera oczekiwane wartości PC, A, X, Y, P, SP, CYC po każdej instrukcji.

#### Sposób testowania

1. Załadować ROM nestest.
2. Ustawić PC na $C000 (adres startowy nestest).
3. Wykonywać instrukcje i po każdej porównać stan rejestrów z logiem.
4. Jeśli PC = zawiera instrukcję w pętli na sukces lub porażkę — test zakończony.

### 12.2. Klaus Dormann 6502 Functional Test Suite

- Pliki: `6502_functional_test.bin` (lub warianty z różnymi adresami startowymi)
- Bardziej zaawansowany test niż nestest.
- Testuje wszystkie udokumentowane instrukcje.
- Zawiera **testy BCD** (ADC i SBC w trybie decimal).
- Testuje **wszystkie tryby adresowania**.
- Działa jako binarny ROM: nie wymaga logów — ładuje się do pamięci pod adresem $0000 lub $0400 i uruchamia.
- Sygnalizuje błędy przez zapis do specyficznych lokalizacji pamięci (np. $2000+).
- Sukces = program wchodzi w nieskończoną pętlę pod konkretnym adresem, lub wyświetla komunikat.

### 12.3. Wolfgang Lorenz Test Suite (C64)

- Bardzo zaawansowany test emulatora C64, zawiera testy CPU, CIA i VIC.
- Testuje wszystkie opkody (w tym nieudokumentowane).
- Testuje timing (dokładną liczbę cykli).
- Testuje przerwania, interrupt hijacking, branch quirki.
- Testuje dokładną interakcję między CPU a CIA/VIC.
- Format: obrazy d64, wymaga emulacji C64.

### 12.4. perfect6502

- Transistor-level symulacja 6502.
- Pozwala porównać stan emulatora po każdym cyklu z "prawdziwym" sprzętem.
- Można zintegrować jako test regresyjny.

### 12.5. Testy BCD (decimal mode)

- `6502_decimal_test` lub testy zawarte w Klaus i Wolfgang Lorenz.
- Testują zachowanie ADC/SBC w trybie BCD.
- Sprawdzają flagi N, V, Z, C po operacjach BCD.
- **Uwaga**: flaga V po ADC/SBC w trybie BCD może różnić się między implementacjami NMOS.

### 12.6. Strategia implementacji testów

**Kolejność wdrażania testów:**

1. **nestest** — pierwszy test do przejścia. Weryfikuje poprawność udokumentowanych instrukcji i trybów adresowania.
2. **Klaus Dormann Functional Test** (wersja bez BCD) — rozszerzona weryfikacja udokumentowanych instrukcji.
3. **Klaus Dormann Functional Test** (z BCD) — weryfikacja trybu decimal.
4. **Nieudokumentowane opkody** — implementacja i testowanie na podstawie Wolfgang Lorenz.
5. **Wolfgang Lorenz Test Suite** — pełna weryfikacja timingu, przerwań i nieudokumentowanych instrukcji.
6. **perfect6502** (opcjonalnie) — najdokładniejsza weryfikacja cycle-by-cycle.

#### Sposób integracji testów w .NET

```csharp
// Przykład ładowania ROM-u testowego
byte[] rom = File.ReadAllBytes("test.bin");

// Zapis ROM-u pod odpowiedni adres w pamięci
for (int i = 0; i < rom.Length; i++)
    memory[loadAddress + i] = rom[i];

// Ustawienie wektora RESET jeśli potrzebne
memory[0xFFFC] = (byte)(loadAddress & 0xFF);
memory[0xFFFD] = (byte)(loadAddress >> 8);

// Wykonanie sekwencji RESET
cpu.Reset();

// Wykonywanie cykl po cyklu
while (true)
{
    cpu.Tick();
    // Sprawdzanie warunków zakończenia testu...
}
```

---

## 13. Wymagania i struktura projektu .NET

### 13.1. Struktura rozwiązania

```
6502Emulator/
├── 6502Emulator.sln
├── src/
│   └── Cpu6502/
│       ├── Cpu6502.csproj
│       ├── Cpu6502.cs              // Główna klasa procesora
│       ├── IMemoryBus.cs           // Interfejs dostępu do pamięci
│       ├── OpcodeTable.cs          // Tablica dekodująca opkody
│       ├── AddressingModes.cs      // Implementacje trybów adresowania
│       └── CpuState.cs             // Stan procesora (struct/class)
└── tests/
    └── Cpu6502.Tests/
        ├── Cpu6502.Tests.csproj
        ├── TestRomLoader.cs        // Ładowanie ROM-ów testowych
        ├── NestestTest.cs          // Test zgodności z nestest
        └── KlausTest.cs            // Test zgodności z Klaus
```

### 13.2. Interfejs IMemoryBus

```csharp
public interface IMemoryBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}
```

Symulator procesora **nie implementuje własnej pamięci** — otrzymuje ją przez wstrzyknięcie zależności. To pozwala na mapowanie I/O, bank switching, mirroring itp. po stronie systemu.

### 13.3. Klasa Cpu6502 — pola

```csharp
public class Cpu6502
{
    // Rejestry
    private byte A;         // Accumulator
    private byte X;         // X Index
    private byte Y;         // Y Index
    private ushort PC;      // Program Counter
    private byte SP;        // Stack Pointer
    private byte P;         // Status Register (flags)

    // Wewnętrzne
    private byte IR;        // Instruction Register (z opcode + cycle counter)
    private bool Sync;      // SYNC flaga — początek nowej instrukcji
    private ulong Cycle;    // Licznik cykli (dla testów)

    // Interrupts
    private bool NMIPending;    // Zatrzaśnięty NMI
    private bool IRQPending;    // Stan pinu IRQ
    private bool NMIEdge;       // Wykryto opadające zbocze NMI
    private bool ResetActive;   // Pin RESET aktywny

    // Zależności
    private IMemoryBus memory;

    // Inicjalizacja
    public void Reset();
    public void Tick();
}
```

### 13.4. Metoda `Tick()` — szkielet pętli cyklowej

```csharp
public void Tick()
{
    // 1. Sprawdź SYNC — jeśli poprzedni cykl zakończył instrukcję
    if (Sync)
    {
        byte opcode = memory.Read(PC);
        // Połącz opcode z 3-bitowym licznikiem cykli
        IR = (byte)((opcode << 3) | 0);
        Sync = false;
    }

    // 2. Sprawdź przerwania PRZED wykonaniem instrukcji
    if (/* warunek przerwania spełniony */)
    {
        // Wstrzyknij BRK do IR z odpowiednimi flagami
        IR = (0x00 << 3) | 0;
        // Ustaw wewnętrzne flagi przerwania
    }

    // 3. Dekoduj i wykonaj jeden cykl
    switch (IR)
    {
        // case'y dla każdego opcode|cycle
    }

    // 4. Inkrementuj licznik cykli
    IR++;  // cycle counter w dolnych 3 bitach
    Cycle++;
}
```

### 13.5. Wykonywanie instrukcji — szkielet switch-case

Dla instrukcji cycle-stepped, każdy `case` odpowiada jednemu cyklowi instrukcji:

```csharp
// LDA immediate ($A9)
case (0xA9 << 3) | 0:
    // Cykl 1: Odczyt opcode (już wykonany przez SYNC)
    // PC++ na operand
    PC++;
    break;

case (0xA9 << 3) | 1:
    // Cykl 2: Odczyt operandu
    A = memory.Read(PC);
    SetNZ(A);
    PC++;
    Sync = true;  // koniec instrukcji
    break;

// LDA absolute ($AD)
case (0xAD << 3) | 0:
    PC++;
    break;

case (0xAD << 3) | 1:
    // Odczyt low-byte adresu
    addrL = memory.Read(PC++);
    break;

case (0xAD << 3) | 2:
    // Odczyt high-byte adresu
    addrH = memory.Read(PC++);
    break;

case (0xAD << 3) | 3:
    // Odczyt wartości pod adresem
    A = memory.Read((ushort)(addrH << 8 | addrL));
    SetNZ(A);
    Sync = true;
    break;
```

### 13.6. Obsługa przerwań w `Tick()`

```csharp
// Sprawdzanie przed wykonaniem instrukcji
if (Sync)  // Jesteśmy na początku nowej instrukcji
{
    if (ResetActive)
    {
        // RESET obsługiwany osobno (sekwencja 7 cykli)
        HandleReset();
        return;
    }

    if (NMIPending && NMIEdge)
    {
        // NMI wykryte
        InjectInterrupt(InterruptType.NMI);
    }
    else if (IRQPending && !FlagI)
    {
        // IRQ wykryte
        InjectInterrupt(InterruptType.IRQ);
    }
}

void InjectInterrupt(InterruptType type)
{
    // Wstrzyknięcie opcode 0x00 (BRK) do IR z flagami
    IR = 0x00 << 3;
    Sync = false;
    // Zapisanie typu przerwania do wewnętrznej zmiennej
    currentInterrupt = type;
    // Ustawienie odpowiedniego wektora
}
```

### 13.7. Tryby adresowania — implementacja

```csharp
// Przykładowe metody adresowania — każda ustawia addressBus i/lub zwraca wartość

ushort AddrZP() => memory.Read(PC++) & 0xFF;
ushort AddrZPX() => (memory.Read(PC++) + X) & 0xFF;
ushort AddrZPY() => (memory.Read(PC++) + Y) & 0xFF;

ushort AddrAbs()
{
    ushort lo = memory.Read(PC++);
    ushort hi = memory.Read(PC++);
    return (ushort)(hi << 8 | lo);
}

ushort AddrAbsX(out bool pageCrossed)
{
    ushort lo = memory.Read(PC++);
    ushort hi = memory.Read(PC++);
    ushort baseAddr = (ushort)(hi << 8 | lo);
    ushort addr = (ushort)(baseAddr + X);
    pageCrossed = (addr >> 8) != hi;
    return addr;
}

// Podobnie dla abs,Y, (zp,X), (zp),Y...
```

---

## 14. Materiały źródłowe

1. **mass:werk 6502 Instruction Set** — <https://www.masswerk.at/6502/6502_instruction_set.html>
2. **Wikibooks: 6502 Assembly** — <https://en.wikibooks.org/wiki/6502_Assembly>
3. **6502.org Tutorials** — <https://6502.org/tutorials/>
4. **pagetable.com c64ref** — <https://www.pagetable.com/c64ref/6502/>
5. **Visual 6502** — <http://visual6502.org/>
6. **Internals of BRK/IRQ/NMI/RESET** — <https://www.pagetable.com/?p=410>
7. **Cycle-stepped 6502 emulator (floooh)** — <https://floooh.github.io/2019/12/13/cycle-stepped-6502.html>
8. **NESdev Wiki — CPU interrupts** — <https://www.nesdev.org/wiki/CPU_interrupts>
9. **NESdev Wiki — 6502 Timing of Interrupt Handling** — <https://www.nesdev.org/wiki/Visual6502wiki/6502_Timing_of_Interrupt_Handling>
10. **6502 Programming Manual (MOS Technology)** — PDF datasheet
11. **Klaus Dormann 6502 Functional Test** — <https://github.com/Klaus2m5/6502_65C02_functional_tests>
12. **Wolfgang Lorenz Test Suite** — <https://github.com/mist64/cbmbus/tree/master/emulator_testbench>
13. **perfect6502** — <https://github.com/mist64/perfect6502>
14. **O2 — cycle-accurate 6502 emulator** — <https://github.com/ericssonpaul/O2>
15. **chips project (floooh)** — <https://github.com/floooh/chips>

---

*Dokumentacja przygotowana dla projektu symulatora procesora MOS 6502 w .NET. Wersja: 1.0, maj 2026.*
