namespace Cpu6502;

public partial class Cpu6502
{
    private bool ExecuteCycleBranches(ushort key)
    {
        switch (key)
        {
            case 0x90 << 3 | 0: BccRel(); break;
            case 0xB0 << 3 | 0: BcsRel(); break;
            case 0xF0 << 3 | 0: BeqRel(); break;
            case 0x30 << 3 | 0: BmiRel(); break;
            case 0xD0 << 3 | 0: BneRel(); break;
            case 0x10 << 3 | 0: BplRel(); break;
            case 0x50 << 3 | 0: BvcRel(); break;
            case 0x70 << 3 | 0: BvsRel(); break;
            default: return false;
        }

        return true;
    }
}
