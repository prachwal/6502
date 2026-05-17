namespace Cpu6502;

public partial class Cpu6502
{
    private bool ExecuteCycleLoadStoreTransferFlags(byte opcode, byte cycle, ushort key)
    {
        switch (key)
        {
            case 0xA9 << 3 | 0: LdaImm(); _sync = true; break;
            case 0xA5 << 3 | 0: LdaZp(); _sync = true; break;
            case 0xB5 << 3 | 0: LdaZpX(); _sync = true; break;
            case 0xAD << 3 | 0: LdaAbs(); _sync = true; break;
            case 0xBD << 3 | 0: LdaAbsX(); _sync = true; break;
            case 0xB9 << 3 | 0: LdaAbsY(); _sync = true; break;
            case 0xA1 << 3 | 0: LdaIndX(); _sync = true; break;
            case 0xB1 << 3 | 0: LdaIndY(); _sync = true; break;
            case 0x85 << 3 | 0: StaZp(); _sync = true; break;
            case 0x95 << 3 | 0: StaZpX(); _sync = true; break;
            case 0x8D << 3 | 0: StaAbs(); _sync = true; break;
            case 0x9D << 3 | 0: StaAbsX(); _sync = true; break;
            case 0x99 << 3 | 0: StaAbsY(); _sync = true; break;
            case 0x81 << 3 | 0: StaIndX(); _sync = true; break;
            case 0x91 << 3 | 0: StaIndY(); _sync = true; break;
            case 0xA2 << 3 | 0: LdxImm(); _sync = true; break;
            case 0xA6 << 3 | 0: LdxZp(); _sync = true; break;
            case 0xB6 << 3 | 0: LdxZpY(); _sync = true; break;
            case 0xAE << 3 | 0: LdxAbs(); _sync = true; break;
            case 0xBE << 3 | 0: LdxAbsY(); _sync = true; break;
            case 0x86 << 3 | 0: StxZp(); _sync = true; break;
            case 0x96 << 3 | 0: StxZpY(); _sync = true; break;
            case 0x8E << 3 | 0: StxAbs(); _sync = true; break;
            case 0xA0 << 3 | 0: LdyImm(); _sync = true; break;
            case 0xA4 << 3 | 0: LdyZp(); _sync = true; break;
            case 0xB4 << 3 | 0: LdyZpX(); _sync = true; break;
            case 0xAC << 3 | 0: LdyAbs(); _sync = true; break;
            case 0xBC << 3 | 0: LdyAbsX(); _sync = true; break;
            case 0x84 << 3 | 0: StyZp(); _sync = true; break;
            case 0x94 << 3 | 0: StyZpX(); _sync = true; break;
            case 0x8C << 3 | 0: StyAbs(); _sync = true; break;
            case 0xEA << 3 | 0: Nop(); _sync = true; break;
            case 0xAA << 3 | 0: Tax(); _sync = true; break;
            case 0xA8 << 3 | 0: Tay(); _sync = true; break;
            case 0xBA << 3 | 0: Tsx(); _sync = true; break;
            case 0x8A << 3 | 0: Txa(); _sync = true; break;
            case 0x9A << 3 | 0: Txs(); _sync = true; break;
            case 0x98 << 3 | 0: Tya(); _sync = true; break;
            case 0x18 << 3 | 0: Clc(); _sync = true; break;
            case 0x38 << 3 | 0: Sec(); _sync = true; break;
            case 0xD8 << 3 | 0: Cld(); _sync = true; break;
            case 0xF8 << 3 | 0: Sed(); _sync = true; break;
            case 0x58 << 3 | 0: Cli(); _sync = true; break;
            case 0x78 << 3 | 0: Sei(); _sync = true; break;
            case 0xB8 << 3 | 0: Clv(); _sync = true; break;
            
            // LAS - Load A, X, SP from memory AND SP
            case 0xBB << 3 | 0: LasAbsY_Cycle0(); break;
            case 0xBB << 3 | 1: LasAbsY_Cycle1(); break;
            case 0xBB << 3 | 2: LasAbsY_Cycle2(); break;
            case 0xBB << 3 | 3: LasAbsY_Cycle3(); break;
            
            // LAX - LDA + LDX (Illegal Opcode)
            case 0xA7 << 3 | 0: LaxZp_Cycle0(); break;
            case 0xB7 << 3 | 0: LaxZpY_Cycle0(); break;
            case 0xAF << 3 | 0: LaxAbs_Cycle0(); break;
            case 0xBF << 3 | 0: LaxAbsY_Cycle0(); break;
            case 0xBF << 3 | 1: LaxAbsY_Cycle1(); break;
            case 0xA3 << 3 | 0: LaxIndX_Cycle0(); break;
            case 0xB3 << 3 | 0: LaxIndY_Cycle0(); break;
            case 0xB3 << 3 | 1: LaxIndY_Cycle1(); break;
            
            // SAX - Store A & X (Illegal Opcode)
            case 0x87 << 3 | 0: SaxZp_Cycle0(); break;
            case 0x97 << 3 | 0: SaxZpY_Cycle0(); break;
            case 0x8F << 3 | 0: SaxAbs_Cycle0(); break;
            case 0x83 << 3 | 0: SaxZpX_Cycle0(); break;
            
            default: return false;
        }

        return true;
    }
}
