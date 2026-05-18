namespace Cpu6502.System;

/// <summary>
/// Interfejs dla śledzenia aktywności magistrali.
/// Pozwala na logowanie odczytów/zapisów do pamięci i portów.
/// </summary>
public interface IBusTracer
{
    /// <summary>
    /// Wywoływane przy odczycie z pamięci.
    /// </summary>
    /// <param name="address">Adres odczytu.</param>
    /// <param name="value">Odczytana wartość.</param>
    void OnReadMemory(uint address, byte value);

    /// <summary>
    /// Wywoływane przy zapisie do pamięci.
    /// </summary>
    /// <param name="address">Adres zapisu.</param>
    /// <param name="value">Zapisywana wartość.</param>
    void OnWriteMemory(uint address, byte value);

    /// <summary>
    /// Wywoływane przy odczycie z portu I/O.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <param name="value">Odczytana wartość.</param>
    void OnReadPort(uint port, byte value);

    /// <summary>
    /// Wywoływane przy zapisie do portu I/O.
    /// </summary>
    /// <param name="port">Numer portu.</param>
    /// <param name="value">Zapisywana wartość.</param>
    void OnWritePort(uint port, byte value);
}

/// <summary>
/// Domyślna, pusta implementacja IBusTracer.
/// Nie wykonuje żadnych operacji - używana gdy tracing jest wyłączony.
/// </summary>
public sealed class NullBusTracer : IBusTracer
{
    /// <summary>Instancja singleton.</summary>
    public static readonly NullBusTracer Instance = new();

    private NullBusTracer() { }

    /// <inheritdoc/>
    public void OnReadMemory(uint address, byte value) { }

    /// <inheritdoc/>
    public void OnWriteMemory(uint address, byte value) { }

    /// <inheritdoc/>
    public void OnReadPort(uint port, byte value) { }

    /// <inheritdoc/>
    public void OnWritePort(uint port, byte value) { }
}
