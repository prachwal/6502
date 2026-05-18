using System;
using System.Collections.Generic;

namespace Cpu6502.System;

/// <summary>
/// Kontroler sygnałów CPU.
/// Agreguje sygnały z wielu źródeł (urządzeń) i dostarcza je do odbiorców (CPU).
/// 
/// Jedna linia sygnałowa (np. IRQ) może być utrzymywana przez wiele źródeł.
/// Linia jest aktywna, dopóki co najmniej jedno źródło ją utrzymuje.
/// </summary>
public sealed class CpuSignalController : ICpuSignalSource
{
    private readonly Dictionary<CpuSignal, HashSet<string>> _sources = new();
    private readonly Dictionary<CpuSignal, int> _assertionCounts = new();
    
    /// <summary>
    /// Unikalny identyfikator kontrolera.
    /// </summary>
    public string Id { get; } = "cpu-signal-controller";
    
    /// <summary>
    /// Tworzy nowy kontroler sygnałów CPU.
    /// </summary>
    public CpuSignalController()
    {
        // Inicjalizuj liczniki dla wszystkich sygnałów
        foreach (CpuSignal signal in Enum.GetValues(typeof(CpuSignal)))
        {
            _sources[signal] = new HashSet<string>();
            _assertionCounts[signal] = 0;
        }
    }
    
    /// <summary>
    /// Rejestruje nowe źródło sygnałów.
    /// </summary>
    /// <param name="sourceId">Identyfikator źródła.</param>
    /// <param name="source">Źródło sygnałów.</param>
    public void RegisterSource(string sourceId, ICpuSignalSource source)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or whitespace", nameof(sourceId));
        
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        
        // Zarejestruj źródło
        // W przyszłości: moglibyśmy odpytywać źródło tutaj
        // Na razie po prostu zapamiętujemy, że źródło istnieje
    }
    
    /// <summary>
    /// Aktualizuje stan sygnału od konkretnego źródła.
    /// </summary>
    /// <param name="sourceId">Identyfikator źródła.</param>
    /// <param name="signal">Sygnał do zaktualizowania.</param>
    /// <param name="asserted">Czy sygnał jest aktywny.</param>
    public void UpdateSignal(string sourceId, CpuSignal signal, bool asserted)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or whitespace", nameof(sourceId));
        
        var sourcesForSignal = _sources[signal];
        var currentCount = _assertionCounts[signal];
        
        if (asserted)
        {
            // Dodaj źródło, jeśli nie jest już zarejestrowane
            if (sourcesForSignal.Add(sourceId))
            {
                _assertionCounts[signal] = currentCount + 1;
            }
        }
        else
        {
            // Usuń źródło
            if (sourcesForSignal.Remove(sourceId))
            {
                _assertionCounts[signal] = currentCount - 1;
            }
        }
    }
    
    /// <summary>
    /// Wyiąguj źródło sygnałów.
    /// </summary>
    /// <param name="sourceId">Identyfikator źródła.</param>
    public void UnregisterSource(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("Source ID cannot be null or whitespace", nameof(sourceId));
        
        // Usuń źródło ze wszystkich sygnałów
        foreach (var signal in _sources.Keys)
        {
            if (_sources[signal].Remove(sourceId))
            {
                _assertionCounts[signal]--;
            }
        }
    }
    
    /// <inheritdoc/>
    public bool IsAsserted(CpuSignal signal)
    {
        return _assertionCounts.TryGetValue(signal, out var count) && count > 0;
    }
    
    /// <summary>
    /// Zwraca listę źródeł aktywnie utrzymujących dany sygnał.
    /// </summary>
    /// <param name="signal">Sygnał do sprawdzenia.</param>
    /// <returns>Kolekcja identyfikatorów źródeł.</returns>
    public IReadOnlyCollection<string> GetAssertingSources(CpuSignal signal)
    {
        return _sources.TryGetValue(signal, out var sources) 
            ? sources.ToList().AsReadOnly() 
            : Array.Empty<string>();
    }
    
    /// <summary>
    /// Czyści wszystkie sygnały.
    /// </summary>
    public void ClearAll()
    {
        foreach (var signal in _sources.Keys)
        {
            _sources[signal].Clear();
            _assertionCounts[signal] = 0;
        }
    }
}
