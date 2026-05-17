namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Obsługa przerwań

    /// <summary>
    /// BRK - Software Interrupt.
    /// Opcode: 0x00, Tryb: Implied, Cykle: 7
    /// </summary>
    private void Brk()
    {
        // BRK to 2-bajtowa instrukcja (opcode + signature byte)
        // PC wskazuje już na signature byte po fetchu — pomiń go
        _pc++;
        // Wstrzykuje przerwanie z B=1 (pushuje PC za signature byte)
        InjectInterrupt(InterruptType.BRK);
    }

    /// <summary>
    /// RTI - Return from Interrupt.
    /// Opcode: 0x40, Tryb: Implied, Cykle: 6
    /// </summary>
    private void Rti()
    {
        // Pull P (z flagami)
        _p = Pop();
        
        // Pull PCL i PCH
        byte lo = Pop();
        byte hi = Pop();
        _pc = (ushort)((hi << 8) | lo);
        
        // Ustaw sygnalizację pobrania nowego opcode
        _sync = true;

        // RTI może zmienić flagę I — opóźnij sprawdzenie IRQ o 1 instrukcję
        _interruptDelay = true;
    }

    /// <summary>
    /// Wstrzykuje sekwencję obsługi przerwania.
    /// </summary>
    /// <param name="type">Typ przerwania (IRQ, NMI, BRK).</param>
    private void InjectInterrupt(InterruptType type)
    {
        // Zapamiętaj bieżący PC (dla pushowania)
        ushort returnPC = _pc;

        // Push PCH i PCL
        Push((byte)(returnPC >> 8));
        Push((byte)(returnPC & 0xFF));

        // Push rejestru P z odpowiednimi flagami
        byte pushedP = _p;
        if (type == InterruptType.BRK)
        {
            pushedP |= FlagB;  // B=1 tylko dla BRK
        }
        else
        {
            pushedP &= unchecked((byte)~FlagB);  // B=0 dla IRQ/NMI
        }
        pushedP |= FlagU;  // bit5=1
        Push(pushedP);

        // Ustaw flagę I (wyłącz dalsze IRQ)
        SetFlag(FlagI, true);

        // Odczytaj wektor przerwania
        ushort vector;
        if (type == InterruptType.NMI)
        {
            vector = 0xFFFA;
        }
        else
        {
            vector = 0xFFFE;
        }

        byte lo = _memory.Read(vector);
        byte hi = _memory.Read((ushort)(vector + 1));
        _pc = (ushort)(hi << 8 | lo);

        // Ustaw sygnalizację pobrania nowego opcode
        _sync = true;
    }

    #endregion
}