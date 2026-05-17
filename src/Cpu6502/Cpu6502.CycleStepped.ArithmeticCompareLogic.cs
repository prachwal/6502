namespace Cpu6502;

public partial class Cpu6502
{
    private bool ExecuteCycleArithmeticCompareLogic(ushort key)
    {
        switch (key)
        {
            case 0x69 << 3 | 0: AdcImm(); _sync = true; break;
            case 0x65 << 3 | 0: AdcZp(); _sync = true; break;
            case 0x75 << 3 | 0: AdcZpX(); _sync = true; break;
            case 0x6D << 3 | 0: AdcAbs(); _sync = true; break;
            case 0x7D << 3 | 0: AdcAbsX(); _sync = true; break;
            case 0x79 << 3 | 0: AdcAbsY(); _sync = true; break;
            case 0x61 << 3 | 0: AdcIndX(); _sync = true; break;
            case 0x71 << 3 | 0: AdcIndY(); _sync = true; break;
            case 0xE9 << 3 | 0: SbcImm(); _sync = true; break;
            case 0xE5 << 3 | 0: SbcZp(); _sync = true; break;
            case 0xF5 << 3 | 0: SbcZpX(); _sync = true; break;
            case 0xED << 3 | 0: SbcAbs(); _sync = true; break;
            case 0xFD << 3 | 0: SbcAbsX(); _sync = true; break;
            case 0xF9 << 3 | 0: SbcAbsY(); _sync = true; break;
            case 0xE1 << 3 | 0: SbcIndX(); _sync = true; break;
            case 0xF1 << 3 | 0: SbcIndY(); _sync = true; break;
            case 0xC9 << 3 | 0: CmpImm(); _sync = true; break;
            case 0xC5 << 3 | 0: CmpZp(); _sync = true; break;
            case 0xD5 << 3 | 0: CmpZpX(); _sync = true; break;
            case 0xCD << 3 | 0: CmpAbs(); _sync = true; break;
            case 0xDD << 3 | 0: CmpAbsX(); _sync = true; break;
            case 0xD9 << 3 | 0: CmpAbsY(); _sync = true; break;
            case 0xC1 << 3 | 0: CmpIndX(); _sync = true; break;
            case 0xD1 << 3 | 0: CmpIndY(); _sync = true; break;
            case 0xE0 << 3 | 0: CpxImm(); _sync = true; break;
            case 0xE4 << 3 | 0: CpxZp(); _sync = true; break;
            case 0xEC << 3 | 0: CpxAbs(); _sync = true; break;
            case 0xC0 << 3 | 0: CpyImm(); _sync = true; break;
            case 0xC4 << 3 | 0: CpyZp(); _sync = true; break;
            case 0xCC << 3 | 0: CpyAbs(); _sync = true; break;
            case 0x29 << 3 | 0: AndImm(); _sync = true; break;
            case 0x25 << 3 | 0: AndZp(); _sync = true; break;
            case 0x35 << 3 | 0: AndZpX(); _sync = true; break;
            case 0x2D << 3 | 0: AndAbs(); _sync = true; break;
            case 0x3D << 3 | 0: AndAbsX(); _sync = true; break;
            case 0x39 << 3 | 0: AndAbsY(); _sync = true; break;
            case 0x21 << 3 | 0: AndIndX(); _sync = true; break;
            case 0x31 << 3 | 0: AndIndY(); _sync = true; break;
            case 0x09 << 3 | 0: OraImm(); _sync = true; break;
            case 0x05 << 3 | 0: OraZp(); _sync = true; break;
            case 0x15 << 3 | 0: OraZpX(); _sync = true; break;
            case 0x0D << 3 | 0: OraAbs(); _sync = true; break;
            case 0x1D << 3 | 0: OraAbsX(); _sync = true; break;
            case 0x19 << 3 | 0: OraAbsY(); _sync = true; break;
            case 0x01 << 3 | 0: OraIndX(); _sync = true; break;
            case 0x11 << 3 | 0: OraIndY(); _sync = true; break;
            case 0x49 << 3 | 0: EorImm(); _sync = true; break;
            case 0x45 << 3 | 0: EorZp(); _sync = true; break;
            case 0x55 << 3 | 0: EorZpX(); _sync = true; break;
            case 0x4D << 3 | 0: EorAbs(); _sync = true; break;
            case 0x5D << 3 | 0: EorAbsX(); _sync = true; break;
            case 0x59 << 3 | 0: EorAbsY(); _sync = true; break;
            case 0x41 << 3 | 0: EorIndX(); _sync = true; break;
            case 0x51 << 3 | 0: EorIndY(); _sync = true; break;
            default: return false;
        }

        return true;
    }
}
