# Emulacja grafiki i dźwięku na PC

Data: 2026-05-17  
Repozytorium: `prachwal/6502`  
Status: dokument architektoniczny / roadmapa implementacyjna  
Zakres: video, audio, terminal tekstowy, adaptery frontendowe, scheduler i synchronizacja z PC

---

## 1. Cel

Celem jest zdefiniowanie sposobu emulacji grafiki i dźwięku w projekcie bez wiązania rdzenia emulatora z konkretnym UI.

Najważniejsza zasada:

```text
układ video/audio jest częścią Core emulatora
renderowanie/odtwarzanie jest częścią frontendu PC
```

Nie należy implementować układów tak, aby zależały bezpośrednio od Avalonia, WPF, Blazor, NAudio albo innego hosta.

---

## 2. Docelowy podział warstw

```text
Emulator Core:
  CPU
  RuntimeBus
  RAM / ROM
  VideoDevice
  AudioDevice
  KeyboardDevice
  TimerDevice
  ClockScheduler

Runtime buffers:
  VideoFrame
  TextScreenSnapshot
  AudioRingBuffer

Frontend PC:
  Avalonia / WPF / Blazor / Terminal
  IVideoOutput
  IAudioOutput
  IInputSource
```

Frontend ma tylko:

- pokazać gotową klatkę,
- odtworzyć bufor próbek,
- przekazać wejście klawiatury/joysticka/myszy do urządzeń wejścia.

---

## 3. Dlaczego tak

Bez rozdzielenia Core i frontendu powstaną problemy:

| Problem | Skutek |
|---|---|
| `VideoDevice` zależy od WPF/Avalonia | brak testów headless i brak Blazor/TUI |
| `AudioDevice` wywołuje API audio bezpośrednio | trzaski, brak synchronizacji, trudne testy |
| UI aktualizowane przy każdym zapisie VRAM | bardzo słaba wydajność |
| audio odtwarzane próbkami pojedynczo | niestabilne audio |
| CPU zna ekran albo audio | brak modularności maszyn |

Poprawny model:

```text
CPU -> RuntimeBus -> Video/Audio devices -> buffers -> frontend adapter
```

---

## 4. Kontrakty video

### 4.1. `IVideoDevice`

```csharp
public interface IVideoDevice : IDevice, ICycleDevice
{
    VideoTiming Timing { get; }
    bool IsFrameReady { get; }

    VideoFrame GetFrame();
    void ClearFrameReady();
}
```

### 4.2. `VideoTiming`

```csharp
public sealed record VideoTiming(
    int Width,
    int Height,
    double RefreshRateHz,
    long PixelClockHz);
```

### 4.3. `VideoFrame`

```csharp
public sealed record VideoFrame(
    int Width,
    int Height,
    PixelFormat PixelFormat,
    ReadOnlyMemory<byte> Pixels,
    long FrameNumber);
```

### 4.4. `PixelFormat`

```csharp
public enum PixelFormat
{
    Rgba32,
    Bgra32,
    Indexed8,
    Monochrome1
}
```

### 4.5. `IVideoOutput`

```csharp
public interface IVideoOutput
{
    void Present(VideoFrame frame);
}
```

---

## 5. Kontrakty tekstowe

Dla Apple-1, UART, TTY, CP/M i prostych SBC pełny framebuffer nie jest konieczny.

### 5.1. `ITextDisplayDevice`

```csharp
public interface ITextDisplayDevice : IDevice
{
    TextScreenSnapshot GetSnapshot();
}
```

### 5.2. `TextScreenSnapshot`

```csharp
public sealed record TextScreenSnapshot(
    int Columns,
    int Rows,
    IReadOnlyList<char> Characters,
    IReadOnlyList<byte> Attributes,
    long Revision);
```

### 5.3. Zastosowanie

| Maszyna | Tryb |
|---|---|
| Apple-1 | terminal znakowy przez PIA |
| custom 6502 SBC | UART/TTY |
| CP/M Z80 | terminal znakowy |
| PET MVP | tekstowy ekran znakowy |

---

## 6. Kontrakty audio

### 6.1. `IAudioDevice`

```csharp
public interface IAudioDevice : IDevice, ICycleDevice
{
    AudioFormat Format { get; }
    int ReadSamples(Span<float> destination);
}
```

### 6.2. `AudioFormat`

```csharp
public sealed record AudioFormat(
    int SampleRate,
    int Channels);
```

### 6.3. `IAudioOutput`

```csharp
public interface IAudioOutput
{
    void Submit(ReadOnlySpan<float> samples, AudioFormat format);
}
```

### 6.4. `AudioRingBuffer`

```csharp
public sealed class AudioRingBuffer
{
    public int Write(ReadOnlySpan<float> samples);
    public int Read(Span<float> destination);
    public int AvailableSamples { get; }
    public int FreeSamples { get; }
}
```

---

## 7. Scheduler i synchronizacja

Grafika i dźwięk wymagają synchronizacji czasu.

### 7.1. MVP

W pierwszej wersji można użyć prostego modelu:

```text
computer.Tick():
  cpu.Tick()
  cycleDevices.Tick()
  interruptController.Update()
```

To wystarczy dla:

- Apple-1,
- TTY/UART,
- prostego SBC,
- prostego framebuffer device,
- PET MVP.

### 7.2. Model docelowy

Docelowo potrzebny jest scheduler z proporcjami zegarów:

```text
CPU clock
video clock
audio clock
timer clock
```

Kontrakt:

```csharp
public interface IClockedDevice : IDevice
{
    long ClockHz { get; }
    void Tick(long cycles);
}
```

### 7.3. Frame scheduler

```csharp
public sealed class FrameScheduler
{
    public void RunUntilFrameReady();
    public void RunForCpuCycles(long cpuCycles);
}
```

### 7.4. Audio sample accumulator

Dla 1 MHz CPU i 44100 Hz audio:

```text
1_000_000 / 44_100 = około 22.6757 cyklu CPU na próbkę
```

Potrzebny jest akumulator frakcyjny:

```text
sampleAccumulator += sampleRate
if sampleAccumulator >= cpuClock
  generate sample
  sampleAccumulator -= cpuClock
```

---

## 8. Typy grafiki według trudności

| Typ | Trudność | Przykłady |
|---|---:|---|
| terminal tekstowy | mała | Apple-1, UART, TTY, CP/M |
| prosty framebuffer | mała/średnia | custom SBC |
| character/tile video | średnia | PET, VIC-20, MSX tekstowo |
| CRTC text video | średnia | PET, BBC Micro, Amstrad CPC |
| ULA/VDP | średnia/trudna | ZX Spectrum, MSX |
| scanline/sprite PPU | trudna | NES, Game Boy |
| cycle-accurate video | trudna | C64 VIC-II, Atari ANTIC/GTIA |

---

## 9. Typy audio według trudności

| Typ | Trudność | Przykłady |
|---|---:|---|
| beeper | mała | custom SBC, ZX-like minimal |
| square wave timer | mała/średnia | proste układy dźwięku |
| AY-3-8910 / YM2149 | średnia | ZX 128, Amstrad CPC, MSX |
| NES APU | trudna | NES |
| POKEY | trudna | Atari 8-bit |
| SID | bardzo trudna | C64 |

---

## 10. Implementacja video etapami

### Etap V1 — terminal tekstowy

- [ ] `ITextDisplayDevice`
- [ ] `TextScreenSnapshot`
- [ ] terminal frontend adapter
- [ ] test snapshotu tekstowego

Zastosowanie:

- Apple-1,
- UART/TTY,
- CP/M,
- monitor ROM.

### Etap V2 — prosty framebuffer

- [ ] `IVideoDevice`
- [ ] `VideoFrame`
- [ ] `VideoTiming`
- [ ] `SimpleFramebufferDevice`
- [ ] renderer WPF/Avalonia przez `WriteableBitmap`
- [ ] renderer Blazor przez canvas później

Przykład profilu:

```json
{
  "id": "video0",
  "type": "framebuffer-simple",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0x8000",
    "size": "0x2000"
  },
  "video": {
    "width": 256,
    "height": 192,
    "pixelFormat": "indexed8",
    "refreshRateHz": 50
  }
}
```

### Etap V3 — character/tile video

- [ ] screen RAM,
- [ ] character ROM/RAM,
- [ ] color RAM,
- [ ] palette,
- [ ] dirty region tracking opcjonalnie.

### Etap V4 — PET CRTC/text video

- [ ] CRTC registers,
- [ ] character generator ROM,
- [ ] screen RAM,
- [ ] cursor/blink,
- [ ] podstawowy timing ramki.

### Etap V5 — trudniejsze układy

Kolejność zalecana:

1. ZX Spectrum ULA,
2. MSX VDP,
3. NES PPU,
4. C64 VIC-II,
5. Atari ANTIC/GTIA.

---

## 11. Implementacja audio etapami

### Etap A1 — pipeline audio

- [ ] `IAudioDevice`
- [ ] `AudioFormat`
- [ ] `AudioRingBuffer`
- [ ] `IAudioOutput`
- [ ] `NullAudioOutput` dla testów

### Etap A2 — beeper

- [ ] `BeeperDevice`
- [ ] rejestr enable/frequency albo prosty bit wyjścia,
- [ ] test generowania próbek,
- [ ] test braku próbek przy wyłączonym urządzeniu.

### Etap A3 — host audio

- [ ] Windows: NAudio jako najprostszy adapter,
- [ ] cross-platform później: SDL/PortAudio/OpenAL,
- [ ] buforowanie blokowe, nie pojedyncze próbki.

### Etap A4 — AY-3-8910 / YM2149

- [ ] 16 rejestrów,
- [ ] 3 kanały tone,
- [ ] noise generator,
- [ ] envelope,
- [ ] port-mapped adapter,
- [ ] memory-mapped adapter,
- [ ] testy rejestrów i podstawowych fal.

### Etap A5 — układy trudniejsze

- [ ] NES APU,
- [ ] POKEY,
- [ ] SID.

---

## 12. Adaptery PC

| Frontend | Video | Audio | Uwagi |
|---|---|---|---|
| Terminal/TUI | tekst/ANSI | brak albo beep | najlepsze dla Apple-1/TTY |
| WPF | `WriteableBitmap` | NAudio | dobre dla Windows |
| Avalonia | `WriteableBitmap` | NAudio/PortAudio/SDL | dobre jako frontend multiplatformowy |
| Blazor | canvas/ImageData | WebAudio przez JS interop | późniejszy etap |
| Testy | hash frame/snapshot | `NullAudioOutput`/buffer inspect | headless |

---

## 13. Zasady wydajności

1. Nie aktualizować UI przy każdym zapisie do VRAM.
2. Nie odtwarzać pojedynczych próbek przez API audio.
3. Nie logować każdego piksela ani każdej próbki w normalnym trybie.
4. `VideoDevice` powinien wystawiać gotową klatkę albo snapshot.
5. `AudioDevice` powinien pisać do bufora, a host audio pobierać bloki.
6. Trace video/audio musi być opcjonalny.
7. Framebuffer powinien używać ciągłego bufora bajtów.
8. Dla trybów indexed color paleta powinna być rozwiązywana przy generowaniu klatki albo w rendererze, ale decyzja musi być spójna per device.

---

## 14. Zmiany w `EmulatedComputer`

Docelowy model:

```csharp
public sealed class EmulatedComputer
{
    public required ICpuCore Cpu { get; init; }
    public required ISystemBus Bus { get; init; }
    public required IReadOnlyList<IDevice> Devices { get; init; }

    public IReadOnlyList<IVideoDevice> VideoDevices { get; init; } = [];
    public IReadOnlyList<IAudioDevice> AudioDevices { get; init; } = [];
    public IReadOnlyList<ITextDisplayDevice> TextDisplays { get; init; } = [];

    public void Tick();
    public void StepInstruction();
    public void RunFrame();
}
```

---

## 15. Zmiany w profilach

### 15.1. Video device

```json
{
  "id": "video0",
  "type": "framebuffer-simple",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0x8000",
    "size": "0x2000"
  },
  "video": {
    "width": 256,
    "height": 192,
    "pixelFormat": "indexed8",
    "refreshRateHz": 50
  }
}
```

### 15.2. Audio device

```json
{
  "id": "beeper0",
  "type": "beeper",
  "mapping": {
    "kind": "memory",
    "baseAddress": "0xD020",
    "size": "0x0002"
  },
  "audio": {
    "sampleRate": 44100,
    "channels": 1
  }
}
```

### 15.3. AY device

```json
{
  "id": "ay0",
  "type": "ay-3-8910",
  "mapping": {
    "kind": "port",
    "addressPort": "0x20",
    "dataPort": "0x21"
  },
  "audio": {
    "sampleRate": 44100,
    "channels": 1
  }
}
```

---

## 16. Testy

### 16.1. Unit tests

- `VideoFrameTests`
- `TextScreenSnapshotTests`
- `SimpleFramebufferDeviceTests`
- `AudioRingBufferTests`
- `BeeperDeviceTests`
- `Ay38910RegisterTests`
- `FrameSchedulerTests`
- `AudioSchedulerTests`

### 16.2. Integracyjne

- `Build_ProfileWithFramebuffer_CreatesVideoDevice`
- `Build_ProfileWithBeeper_CreatesAudioDevice`
- `RunFrame_ProducesVideoFrame`
- `RunFrame_DoesNotUpdateUiPerVramWrite`
- `AudioDevice_WritesSamplesToRingBuffer`
- `TextDisplay_ReturnsStableSnapshot`

### 16.3. Snapshot/golden tests

- hash klatki dla prostego framebuffer test pattern,
- snapshot tekstu dla terminala,
- próbki audio dla prostego beepera,
- rejestry AY po zapisach do portów.

---

## 17. Najlepsza kolejność dla projektu

```text
1. ITextDisplayDevice + TextScreenSnapshot
2. IVideoDevice + VideoFrame + SimpleFramebufferDevice
3. IAudioDevice + AudioRingBuffer + BeeperDevice
4. Adapter WPF/Avalonia dla VideoFrame
5. Adapter NAudio dla AudioRingBuffer
6. AY-3-8910
7. PET CRTC text video
8. ZX/MSX/NES/C64/Atari później
```

---

## 18. Decyzje

1. Video/audio są urządzeniami emulatora, nie elementami UI.
2. Frontend pokazuje `VideoFrame` i odtwarza próbki z bufora.
3. Apple-1/UART/TTY powinny zaczynać od modelu tekstowego, nie od framebuffer.
4. Prosty framebuffer jest pierwszym graficznym układem testowym.
5. Beeper jest pierwszym audio device.
6. AY-3-8910 jest pierwszym sensownym układem audio retro.
7. SID, POKEY, NES APU i VIC-II nie są MVP.
8. UI nie może być aktualizowane przy każdym zapisie VRAM.
9. Audio musi być buforowane blokowo.
10. Testy Core muszą działać bez UI i bez fizycznego audio output.

---

## 19. Definition of Done

Ten obszar można uznać za gotowy w dokumentacji, gdy:

- [ ] `planning-documents-index.md` zawiera ten dokument,
- [ ] roadmapa implementacji zawiera video/audio jako osobne urządzenia,
- [ ] profile wspierają urządzenia video/audio,
- [ ] `EmulatedComputer` ma model video/audio/text outputs,
- [ ] testy obejmują framebuffer, text snapshot i audio ring buffer,
- [ ] dokumentacja jasno mówi, że frontend nie jest częścią emulowanego układu.
