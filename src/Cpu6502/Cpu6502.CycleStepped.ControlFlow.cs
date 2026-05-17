namespace Cpu6502;

public partial class Cpu6502
{
    private bool ExecuteCycleControlFlow(ushort key)
    {
        switch (key)
        {
            case 0x00 << 3 | 0: Brk(); break;
            case 0x40 << 3 | 0: Rti(); break;
            case 0x4C << 3 | 0: JmpAbs(); break;
            case 0x6C << 3 | 0: JmpInd(); break;
            case 0x20 << 3 | 0: JsrAbs(); break;
            case 0x60 << 3 | 0: Rts(); break;
            case 0x24 << 3 | 0: BitZp(); break;
            case 0x2C << 3 | 0: BitAbs(); break;
            default: return false;
        }

        return true;
    }
}
