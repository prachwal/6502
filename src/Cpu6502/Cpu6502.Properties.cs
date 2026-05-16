namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Właściwości publiczne

    /// <summary>
    /// Zwraca aktualny stan rejestru statusu (P).
    /// </summary>
    public byte Status => _p;

    /// <summary>
    /// Processor Status Register - rejestr flag.
    /// </summary>
    public byte P
    {
        get => _p;
        set => _p = value;
    }

    /// <summary>
    /// Accumulator - główny rejestr arytmetyczny.
    /// </summary>
    public byte A 
    { 
        get => _a; 
        set => _a = value; 
    }

    /// <summary>
    /// Rejestr indeksowy X.
    /// </summary>
    public byte X 
    { 
        get => _x; 
        set => _x = value; 
    }

    /// <summary>
    /// Rejestr indeksowy Y.
    /// </summary>
    public byte Y 
    { 
        get => _y; 
        set => _y = value; 
    }

    /// <summary>
    /// Program Counter - wskaźnik bieżącej instrukcji.
    /// </summary>
    public ushort PC 
    { 
        get => _pc; 
        set => _pc = value; 
    }

    /// <summary>
    /// Stack Pointer - wskaźnik stosu.
    /// </summary>
    public byte SP 
    { 
        get => _sp; 
        set => _sp = value; 
    }

    #endregion
}
