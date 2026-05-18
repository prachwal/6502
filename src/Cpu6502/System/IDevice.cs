namespace Cpu6502.System;

/// <summary>
/// Bazowy interfejs urządzenia w systemie.
/// Każde urządzenie ma unikalny identyfikator.
/// </summary>
public interface IDevice
{
    /// <summary>Unikalny identyfikator urządzenia.</summary>
    string Id { get; }
}

/// <summary>
/// Interfejs urządzenia, które może być resetowane.
/// </summary>
public interface IResettableDevice : IDevice
{
    /// <summary>Resetuje urządzenie do stanu początkowego.</summary>
    void Reset();
}

/// <summary>
/// Interfejs urządzenia, które działa w cyklach.
/// </summary>
public interface ICycleDevice : IDevice
{
    /// <summary>Wykonuje określoną liczbę cykli.</summary>
    /// <param name="cycles">Liczba cykli do wykonania.</param>
    void Tick(long cycles);
}

/// <summary>
/// Interfejs urządzenia, które może generować sygnały CPU.
/// </summary>
public interface ICpuSignalSource : IDevice
{
    /// <summary>Sprawdza, czy sygnał jest aktywny.</summary>
    /// <param name="signal">Sygnał do sprawdzenia.</param>
    /// <returns>True, jeśli sygnał jest aktywny.</returns>
    bool IsAsserted(CpuSignal signal);
}

/// <summary>
/// Interfejs urządzenia, które może odbierać sygnały CPU.
/// </summary>
public interface ICpuSignalSink
{
    /// <summary>Ustawia stan sygnału.</summary>
    /// <param name="signal">Sygnał CPU.</param>
    /// <param name="asserted">Czy sygnał jest aktywny.</param>
    void SetSignal(CpuSignal signal, bool asserted);
}
