namespace Cpu6502;

public partial class Cpu6502
{
    private bool ExecuteCycleMathsStackBranches(ushort key)
    {
        switch (key)
        {
            case 0xE6 << 3 | 0: IncZp(); break;
            case 0xF6 << 3 | 0: IncZpX(); break;
            case 0xEE << 3 | 0: IncAbs(); break;
            case 0xFE << 3 | 0: IncAbsX(); break;
            case 0xC6 << 3 | 0: DecZp(); break;
            case 0xD6 << 3 | 0: DecZpX(); break;
            case 0xCE << 3 | 0: DecAbs(); break;
            case 0xDE << 3 | 0: DecAbsX(); break;
            case 0xE8 << 3 | 0: Inx(); break;
            case 0xC8 << 3 | 0: Iny(); break;
            case 0xCA << 3 | 0: Dex(); break;
            case 0x88 << 3 | 0: Dey(); break;
            case 0x0A << 3 | 0: AslAcc(); break;
            case 0x06 << 3 | 0: AslZp(); break;
            case 0x16 << 3 | 0: AslZpX(); break;
            case 0x0E << 3 | 0: AslAbs(); break;
            case 0x1E << 3 | 0: AslAbsX(); break;
            case 0x4A << 3 | 0: LsrAcc(); break;
            case 0x46 << 3 | 0: LsrZp(); break;
            case 0x56 << 3 | 0: LsrZpX(); break;
            case 0x4E << 3 | 0: LsrAbs(); break;
            case 0x5E << 3 | 0: LsrAbsX(); break;
            case 0x2A << 3 | 0: RolAcc(); break;
            case 0x26 << 3 | 0: RolZp(); break;
            case 0x36 << 3 | 0: RolZpX(); break;
            case 0x2E << 3 | 0: RolAbs(); break;
            case 0x3E << 3 | 0: RolAbsX(); break;
            case 0x6A << 3 | 0: RorAcc(); break;
            case 0x66 << 3 | 0: RorZp(); break;
            case 0x76 << 3 | 0: RorZpX(); break;
            case 0x6E << 3 | 0: RorAbs(); break;
            case 0x7E << 3 | 0: RorAbsX(); break;
            case 0x48 << 3 | 0: Pha(); break;
            case 0x08 << 3 | 0: Php(); break;
            case 0x68 << 3 | 0: Pla(); break;
            case 0x28 << 3 | 0: Plp(); break;
            default: return false;
        }

        return true;
    }
}
