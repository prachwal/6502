# TTY / Mainframe Link — plan koncepcyjny i implementacyjny

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument koncepcyjny / plan implementacyjny  
Zakres: terminal TTY, UART, połączenie z hostem udającym mainframe, TCP/Telnet/VT100 jako etapy późniejsze

---

## 1. Cel dokumentu

Celem jest zaprojektowanie warstwy, która pozwala komputerowi 6502 komunikować się przez prosty terminal TTY/UART z zewnętrznym lub wewnętrznym hostem udającym mainframe.

Komputer 6502 nie powinien wiedzieć, czy po drugiej stronie jest:

- lokalny fake mainframe,
- proces TCP,
- serwer Telnet,
- WebSocket,
- konsola,
- testowy bufor,
- później uproszczony host 3270-like.

Z perspektywy CPU i programu 6502 istnieje tylko UART memory-mapped.

---

## 2. Zasada architektoniczna

Warstwy powinny wyglądać tak:

```text
6502 program
   |
memory-mapped UART
   |
UartSimpleDevice
   |
ITerminalLink
   |
FakeMainframe / TCP / Telnet / WebSocket / Console
```

CPU nie zna sieci, terminala, mainframe ani protokołów. CPU wykonuje tylko odczyty i zapisy pod adresami UART.

---

## 3. Zakres MVP

MVP powinno obejmować:

- prosty UART memory-mapped,
- interfejs `ITerminalLink`,
- buforowany link testowy,
- fake mainframe jako maszyna stanów,
- obsługę podstawowego TTY:
  - CR,
  - LF,
  - CRLF,
  - Backspace `$08`,
  - DEL `$7F`,
  - printable ASCII `$20-$7E`,
  - echo opcjonalne.

MVP nie musi obejmować:

- prawdziwego Telnet negotiation,
- TN3270,
- pełnego VT100,
- emulacji IBM 3270,
- audio/modemu,
- dokładnego timingu UART.

---

## 4. Memory-mapped UART

### 4.1. Proponowana mapa

```text
$D000 UART_DATA
$D001 UART_STATUS
```

### 4.2. Status bits

```text
bit 0 = RX_READY
bit 1 = TX_READY
```

### 4.3. Zachowanie

| Operacja | Zachowanie |
|---|---|
| read `$D000` | pobiera bajt z hosta do komputera |
| write `$D000` | wysyła bajt z komputera do hosta |
| read `$D001` | zwraca status RX/TX |
| write `$D001` | w MVP ignorowane albo zapamiętywane diagnostycznie |

---

## 5. Interfejs `ITerminalLink`

### 5.1. Cel

`ITerminalLink` jest adapterem między UART a światem zewnętrznym. Dzięki temu `UartSimpleDevice` nie zależy od TCP, konsoli ani konkretnego hosta.

### 5.2. Proponowany interfejs

```csharp
public interface ITerminalLink
{
    bool HasInputForComputer { get; }
    bool TryReadForComputer(out byte value);
    void WriteFromComputer(byte value);
}
```

### 5.3. Semantyka

| Metoda | Znaczenie |
|---|---|
| `HasInputForComputer` | czy host ma bajt gotowy dla 6502 |
| `TryReadForComputer` | pobiera bajt host -> 6502 |
| `WriteFromComputer` | zapisuje bajt 6502 -> host |

Ważne: `HasInputForComputer` nie może zdejmować bajtu z kolejki.

---

## 6. `UartSimpleDevice`

### 6.1. Proponowany model

```csharp
public sealed class UartSimpleDevice : IMemoryMappedDevice
{
    private const byte RxReady = 0x01;
    private const byte TxReady = 0x02;

    private readonly ushort _baseAddress;
    private readonly ITerminalLink _terminalLink;

    public UartSimpleDevice(ushort baseAddress, ITerminalLink terminalLink)
    {
        _baseAddress = baseAddress;
        _terminalLink = terminalLink;
    }

    public bool Handles(ushort address)
        => address == _baseAddress || address == _baseAddress + 1;

    public byte Read(ushort address)
    {
        if (address == _baseAddress)
            return _terminalLink.TryReadForComputer(out var value) ? value : (byte)0;

        if (address == _baseAddress + 1)
            return (byte)((_terminalLink.HasInputForComputer ? RxReady : 0) | TxReady);

        return 0;
    }

    public void Write(ushort address, byte value)
    {
        if (address == _baseAddress)
            _terminalLink.WriteFromComputer(value);
    }
}
```

---

## 7. Implementacje `ITerminalLink`

### 7.1. `BufferedTerminalLink`

Testowa implementacja buforowa.

Zastosowania:

- testy jednostkowe UART,
- testy monitora ROM,
- testy programów 6502,
- symulacja wejścia/wyjścia bez konsoli.

### 7.2. `FakeMainframeTerminalLink`

Lokalny host udający mainframe. Powinien działać jako prosta maszyna stanów tekstowych.

Przykładowy start:

```text
MAINFRAME/TTY READY
LOGIN: 
```

Przykładowe komendy:

```text
HELP
TIME
JOB <text>
STATUS
LOGOFF
```

### 7.3. `ConsoleTerminalLink`

Połączenie z lokalną konsolą hosta.

Zastosowania:

- ręczne testy TUI,
- debugowanie monitora ROM,
- proste uruchomienie bez sieci.

### 7.4. `TcpClientTerminalLink`

Emulator 6502 łączy się jako klient do zewnętrznego hosta.

Przykład:

```text
6502 emulator --tty-connect localhost:2323
```

### 7.5. `TcpServerTerminalLink`

Emulator wystawia port, do którego można podłączyć zewnętrzny terminal.

Przykład:

```text
telnet localhost 2323
```

### 7.6. `WebSocketTerminalLink`

Późniejsza integracja z frontendami webowymi, np. Blazor.

---

## 8. Fake mainframe jako maszyna stanów

### 8.1. Minimalne stany

```text
Boot
LoginPrompt
Ready
RunningJob
LoggedOff
```

### 8.2. Minimalny flow

```text
Boot -> LoginPrompt
LoginPrompt + user input -> Ready
Ready + HELP -> command list
Ready + TIME -> current time
Ready + JOB text -> job accepted
Ready + LOGOFF -> LoggedOff
```

### 8.3. Zachowanie TTY

- wejście kończy się na CR albo LF,
- host powinien tolerować CRLF,
- backspace usuwa znak z bufora linii,
- DEL działa jak backspace,
- komendy są case-insensitive,
- odpowiedzi kończą się CRLF.

---

## 9. TTY, VT100 i 3270-like

### 9.1. Etap 1 — plain TTY

Obsługiwane:

```text
CR
LF
CRLF
Backspace
DEL
Printable ASCII
```

### 9.2. Etap 2 — ANSI/VT100 subset

Później można dodać:

```text
ESC [ 2 J      clear screen
ESC [ H        cursor home
ESC [ row ; col H
ESC [ K        clear line
```

Wtedy host może sterować ekranem terminala.

### 9.3. Etap 3 — 3270-like

Nie implementować prawdziwego IBM 3270 na początku.

Można później zrobić tryb podobny koncepcyjnie:

```text
host wysyła formularz
terminal edytuje pola lokalnie
ENTER wysyła cały formularz
```

To powinno być osobnym planem, jeśli projekt pójdzie w stronę terminali blokowych.

---

## 10. Integracja z profilem komputera

Przykładowy wpis urządzenia w profilu:

```json
{
  "id": "uart0",
  "type": "uart-simple",
  "baseAddress": "0xD000",
  "statusAddress": "0xD001",
  "link": {
    "type": "fake-mainframe"
  }
}
```

Alternatywnie TCP client:

```json
{
  "id": "uart0",
  "type": "uart-simple",
  "baseAddress": "0xD000",
  "link": {
    "type": "tcp-client",
    "host": "localhost",
    "port": 2323
  }
}
```

Alternatywnie TCP server:

```json
{
  "id": "uart0",
  "type": "uart-simple",
  "baseAddress": "0xD000",
  "link": {
    "type": "tcp-server",
    "port": 2323
  }
}
```

---

## 11. Program 6502 widziany od strony UART

Program 6502 powinien widzieć tylko rejestry UART.

```asm
UART_DATA   = $D000
UART_STATUS = $D001

getc:
    lda UART_STATUS
    and #$01
    beq getc
    lda UART_DATA
    rts

putc:
    pha
wait_tx:
    lda UART_STATUS
    and #$02
    beq wait_tx
    pla
    sta UART_DATA
    rts
```

---

## 12. Plan testów jednostkowych

### 12.1. `UartSimpleDeviceTests`

| Test | Cel |
|---|---|
| `Status_WhenNoHostData_ReturnsTxReadyOnly` | brak RX, TX gotowy |
| `Status_WhenHostHasData_ReturnsRxReadyAndTxReady` | RX_READY ustawione |
| `ReadData_WhenHostHasData_ReturnsByte` | odczyt danych z hosta |
| `ReadData_WhenNoHostData_ReturnsZero` | brak danych zwraca jawne zero |
| `WriteData_SendsByteToTerminalLink` | zapis do DATA idzie do linku |
| `Handles_OnlyDataAndStatusAddresses` | urządzenie obsługuje tylko 2 adresy |

### 12.2. `BufferedTerminalLinkTests`

| Test | Cel |
|---|---|
| `EnqueueInput_MakesHasInputTrue` | wejście host -> komputer działa |
| `TryReadForComputer_DequeuesByte` | odczyt usuwa bajt |
| `WriteFromComputer_StoresOutputByte` | output komputera jest zapisany |
| `HasInput_DoesNotDequeue` | sprawdzenie statusu nie konsumuje danych |

### 12.3. `FakeMainframeTerminalLinkTests`

| Test | Cel |
|---|---|
| `OnBoot_PrintsLoginPrompt` | po starcie jest prompt |
| `HelpCommand_ReturnsCommandList` | HELP działa |
| `TimeCommand_ReturnsTimeResponse` | TIME działa |
| `JobCommand_ReturnsJobAccepted` | JOB działa |
| `UnknownCommand_ReturnsError` | nieznana komenda daje błąd |
| `Backspace_RemovesPreviousCharacter` | backspace edytuje linię |
| `Del_RemovesPreviousCharacter` | DEL działa jak backspace |
| `CrLf_IsTreatedAsSingleLineEnd` | CRLF nie wykonuje komendy dwa razy |

### 12.4. `TcpTerminalLinkTests`

Testy TCP powinny być integracyjne i mogą być oznaczone kategorią, np. `Integration`.

| Test | Cel |
|---|---|
| `TcpClient_CanReceiveBytesFromServer` | klient odbiera dane |
| `TcpClient_CanSendBytesToServer` | klient wysyła dane |
| `TcpServer_AcceptsClientAndReceivesBytes` | serwer przyjmuje klienta |
| `TcpServer_CanSendBytesToClient` | serwer wysyła dane |

---

## 13. Testy integracyjne z CPU

| Test | Cel |
|---|---|
| `CpuProgram_ReadsPromptFromFakeMainframe` | program 6502 odbiera prompt przez UART |
| `CpuProgram_SendsHelpCommand_ReceivesResponse` | program wysyła HELP i odbiera odpowiedź |
| `MonitorRom_CanUseUartBackedByFakeMainframe` | monitor ROM działa z linkiem hosta |
| `CustomSbcProfile_CreatesUartWithFakeMainframeLink` | profil tworzy UART i link |

---

## 14. Kolejność implementacji

### Etap 1 — warstwa buforowa i UART

- [ ] Dodać `ITerminalLink`.
- [ ] Dodać `BufferedTerminalLink`.
- [ ] Dodać albo ujednolicić `UartSimpleDevice`.
- [ ] Dodać testy jednostkowe UART/link.

### Etap 2 — fake mainframe

- [ ] Dodać `FakeMainframeTerminalLink`.
- [ ] Dodać prostą maszynę stanów.
- [ ] Dodać obsługę HELP/TIME/JOB/STATUS/LOGOFF.
- [ ] Dodać testy CR/LF/backspace/DEL.

### Etap 3 — integracja z profilem

- [ ] Rozszerzyć schemat profilu o `link` dla UART.
- [ ] Dodać `fake-mainframe` jako typ linku.
- [ ] Dodać profil testowy custom SBC z UART + fake mainframe.

### Etap 4 — TCP

- [ ] Dodać `TcpClientTerminalLink`.
- [ ] Dodać `TcpServerTerminalLink`.
- [ ] Dodać testy integracyjne TCP.

### Etap 5 — VT100 subset

- [ ] Dodać `AnsiTerminalParser`.
- [ ] Dodać `VirtualScreenBuffer`.
- [ ] Obsłużyć minimalne ESC sequences.

### Etap 6 — 3270-like eksperyment

- [ ] Osobny dokument planu.
- [ ] Tryb formularzowy.
- [ ] Host wysyła ekran, terminal odsyła pola.

---

## 15. Kryteria akceptacji

MVP można uznać za gotowe, gdy:

- [ ] UART ma rejestry DATA/STATUS,
- [ ] status RX_READY nie konsumuje danych,
- [ ] `ITerminalLink` oddziela UART od hosta,
- [ ] fake mainframe zwraca prompt po starcie,
- [ ] program 6502 może wysłać komendę i odebrać odpowiedź,
- [ ] testy jednostkowe pokrywają UART, bufor i fake mainframe,
- [ ] profil custom SBC potrafi zadeklarować UART z linkiem `fake-mainframe`,
- [ ] implementacja nie wymaga zmian w CPU.

---

## 16. Decyzja projektowa

Nie implementować mainframe w CPU ani w UART. Mainframe jest tylko jedną z implementacji `ITerminalLink`.

Dzięki temu ten sam komputer 6502 może być podłączony do:

- fake mainframe,
- lokalnej konsoli,
- TCP/Telnet,
- WebSocket,
- testowego bufora,
- później VT100/3270-like.
