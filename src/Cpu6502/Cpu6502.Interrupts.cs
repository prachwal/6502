// <copyright file="Cpu6502.Interrupts.cs" company="6502 Emulator">
// Copyright © 2026 6502 Emulator. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System;

namespace Cpu6502
{
    public partial class Cpu6502
    {
        /// <summary>
        /// BRK - Force Break (Software Interrupt)
        /// Pushes PC+2 and P (with B=1) onto stack, then jumps to IRQ vector
        /// </summary>
        private void Brk()
        {
            // BRK is a 2-byte instruction (opcode + signature byte)
            // We push PC+2 to skip both bytes
            ushort returnAddress = (ushort)(_pc + 1); // PC already points to next byte after opcode
            Push((byte)(returnAddress >> 8));    // Push PCH
            Push((byte)(returnAddress & 0xFF));  // Push PCL
            
            // Push P with B=1 and bit5=1 (NMOS behavior)
            Push((byte)(_p | FlagB | FlagU));
            
            // Set I flag (BRK does set interrupt disable flag)
            _p |= FlagI;
            
            // Jump to IRQ vector at $FFFE/$FFFF
            byte vectorLo = _memory.Read(0xFFFE);
            byte vectorHi = _memory.Read(0xFFFF);
            _pc = (ushort)((vectorHi << 8) | vectorLo);
        }

        /// <summary>
        /// RTI - Return from Interrupt
        /// Pulls P and PC from stack, restoring state before interrupt
        /// </summary>
        private void Rti()
        {
            // Pull P from stack (including B and bit5 for compatibility)
            _p = Pop();
            
            // Pull PC from stack (little-endian: PCL then PCH)
            byte pcl = Pop();
            byte pch = Pop();
            _pc = (ushort)((pch << 8) | pcl);
        }
    }
}