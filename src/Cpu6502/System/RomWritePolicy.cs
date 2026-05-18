namespace Cpu6502.System;

/// <summary>
/// Polityka obsługi zapisu do obszaru ROM.
/// </summary>
public enum RomWritePolicy
{
    /// <summary>
    /// Zapis do ROM jest cicho ignorowany.
    /// Używane dla dokładnej emulacji sprzętu, gdzie zapis do ROM nie ma efektu.
    /// </summary>
    Ignore,

    /// <summary>
    /// Zapis do ROM powoduje rzucenie wyjątku.
    /// Domyślna polityka - ułatwia debugowanie przez wykrywanie błędów.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Zapis do ROM jest ignorowany, ale logowany.
    /// Używane w produkcji do monitorowania nieoczekiwanych operacji.
    /// </summary>
    LogAndIgnore
}
