namespace Cpu6502;

/// <summary>
/// Reprezentacja procesora MOS 6502.
/// </summary>
public partial class Cpu6502
{
    #region Placeholder methods (dla brakujących opcode'ów)

    /// <summary>
    /// BRK - Software Interrupt.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void Brk() => throw new NotImplementedException("BRK not implemented");

    /// <summary>
    /// ORA (ind,X) - OR with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraIndX() => throw new NotImplementedException("ORA (ind,X) not implemented");

    /// <summary>
    /// ORA zp - OR with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraZp() => throw new NotImplementedException("ORA zp not implemented");

    /// <summary>
    /// ORA zp,X - OR with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraZpX() => throw new NotImplementedException("ORA zp,X not implemented");

    /// <summary>
    /// ORA abs - OR with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbs() => throw new NotImplementedException("ORA abs not implemented");

    /// <summary>
    /// ORA abs,X - OR with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbsX() => throw new NotImplementedException("ORA abs,X not implemented");

    /// <summary>
    /// ORA abs,Y - OR with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraAbsY() => throw new NotImplementedException("ORA abs,Y not implemented");

    /// <summary>
    /// ORA (ind),Y - OR with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraIndY() => throw new NotImplementedException("ORA (ind),Y not implemented");

    /// <summary>
    /// ASL A - Arithmetic Shift Left, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AslAcc() => throw new NotImplementedException("ASL A not implemented");

    /// <summary>
    /// ORA #imm - OR with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void OraImm() => throw new NotImplementedException("ORA #imm not implemented");

    /// <summary>
    /// BPL rel - Branch if Plus (N=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BplRel() => throw new NotImplementedException("BPL rel not implemented");

    /// <summary>
    /// JSR abs - Jump to Subroutine, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void JsrAbs() => throw new NotImplementedException("JSR abs not implemented");

    /// <summary>
    /// AND (ind,X) - AND with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndIndX() => throw new NotImplementedException("AND (ind,X) not implemented");

    /// <summary>
    /// AND zp - AND with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndZp() => throw new NotImplementedException("AND zp not implemented");

    /// <summary>
    /// AND zp,X - AND with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndZpX() => throw new NotImplementedException("AND zp,X not implemented");

    /// <summary>
    /// AND abs - AND with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbs() => throw new NotImplementedException("AND abs not implemented");

    /// <summary>
    /// AND abs,X - AND with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbsX() => throw new NotImplementedException("AND abs,X not implemented");

    /// <summary>
    /// AND abs,Y - AND with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndAbsY() => throw new NotImplementedException("AND abs,Y not implemented");

    /// <summary>
    /// AND (ind),Y - AND with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndIndY() => throw new NotImplementedException("AND (ind),Y not implemented");

    /// <summary>
    /// ROL A - Rotate Left, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void RolAcc() => throw new NotImplementedException("ROL A not implemented");

    /// <summary>
    /// AND #imm - AND with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AndImm() => throw new NotImplementedException("AND #imm not implemented");

    /// <summary>
    /// BMI rel - Branch if Minus (N=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BmiRel() => throw new NotImplementedException("BMI rel not implemented");

    /// <summary>
    /// RTI - Return from Interrupt.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void Rti() => throw new NotImplementedException("RTI not implemented");

    /// <summary>
    /// EOR (ind,X) - Exclusive OR with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorIndX() => throw new NotImplementedException("EOR (ind,X) not implemented");

    /// <summary>
    /// EOR zp - Exclusive OR with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorZp() => throw new NotImplementedException("EOR zp not implemented");

    /// <summary>
    /// EOR zp,X - Exclusive OR with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorZpX() => throw new NotImplementedException("EOR zp,X not implemented");

    /// <summary>
    /// EOR abs - Exclusive OR with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbs() => throw new NotImplementedException("EOR abs not implemented");

    /// <summary>
    /// EOR abs,X - Exclusive OR with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbsX() => throw new NotImplementedException("EOR abs,X not implemented");

    /// <summary>
    /// EOR abs,Y - Exclusive OR with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorAbsY() => throw new NotImplementedException("EOR abs,Y not implemented");

    /// <summary>
    /// EOR (ind),Y - Exclusive OR with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorIndY() => throw new NotImplementedException("EOR (ind),Y not implemented");

    /// <summary>
    /// LSR A - Logical Shift Right, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void LsrAcc() => throw new NotImplementedException("LSR A not implemented");

    /// <summary>
    /// EOR #imm - Exclusive OR with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void EorImm() => throw new NotImplementedException("EOR #imm not implemented");

    /// <summary>
    /// BVC rel - Branch if Overflow Clear (V=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BvcRel() => throw new NotImplementedException("BVC rel not implemented");

    /// <summary>
    /// RTS - Return from Subroutine.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void Rts() => throw new NotImplementedException("RTS not implemented");

    /// <summary>
    /// ADC (ind,X) - Add with Carry, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcIndX() => throw new NotImplementedException("ADC (ind,X) not implemented");

    /// <summary>
    /// ADC zp - Add with Carry, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcZp() => throw new NotImplementedException("ADC zp not implemented");

    /// <summary>
    /// ADC zp,X - Add with Carry, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcZpX() => throw new NotImplementedException("ADC zp,X not implemented");

    /// <summary>
    /// ADC abs - Add with Carry, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcAbs() => throw new NotImplementedException("ADC abs not implemented");

    /// <summary>
    /// ADC abs,X - Add with Carry, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcAbsX() => throw new NotImplementedException("ADC abs,X not implemented");

    /// <summary>
    /// ADC abs,Y - Add with Carry, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcAbsY() => throw new NotImplementedException("ADC abs,Y not implemented");

    /// <summary>
    /// ADC (ind),Y - Add with Carry, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcIndY() => throw new NotImplementedException("ADC (ind),Y not implemented");

    /// <summary>
    /// ROR A - Rotate Right, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void RorAcc() => throw new NotImplementedException("ROR A not implemented");

    /// <summary>
    /// ADC #imm - Add with Carry, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void AdcImm() => throw new NotImplementedException("ADC #imm not implemented");

    /// <summary>
    /// NOP - No Operation.
    /// Wykonuje pustą operację (zlicza cykle).
    /// </summary>
    private void Nop() { /* no operation */ }

    /// <summary>
    /// BVS rel - Branch if Overflow Set (V=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BvsRel() => throw new NotImplementedException("BVS rel not implemented");

    /// <summary>
    /// BCC rel - Branch if Carry Clear (C=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BccRel() => throw new NotImplementedException("BCC rel not implemented");

    /// <summary>
    /// BCS rel - Branch if Carry Set (C=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BcsRel() => throw new NotImplementedException("BCS rel not implemented");

    /// <summary>
    /// CPY #imm - Compare Y Register, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CpyImm() => throw new NotImplementedException("CPY #imm not implemented");

    /// <summary>
    /// CMP (ind,X) - Compare with Accumulator, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpIndX() => throw new NotImplementedException("CMP (ind,X) not implemented");

    /// <summary>
    /// CMP zp - Compare with Accumulator, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpZp() => throw new NotImplementedException("CMP zp not implemented");

    /// <summary>
    /// CMP zp,X - Compare with Accumulator, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpZpX() => throw new NotImplementedException("CMP zp,X not implemented");

    /// <summary>
    /// CMP abs - Compare with Accumulator, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbs() => throw new NotImplementedException("CMP abs not implemented");

    /// <summary>
    /// CMP abs,X - Compare with Accumulator, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbsX() => throw new NotImplementedException("CMP abs,X not implemented");

    /// <summary>
    /// CMP abs,Y - Compare with Accumulator, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpAbsY() => throw new NotImplementedException("CMP abs,Y not implemented");

    /// <summary>
    /// CMP (ind),Y - Compare with Accumulator, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpIndY() => throw new NotImplementedException("CMP (ind),Y not implemented");

    /// <summary>
    /// DEC A - Decrement, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void DecAcc() => throw new NotImplementedException("DEC A not implemented");

    /// <summary>
    /// CMP #imm - Compare with Accumulator, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CmpImm() => throw new NotImplementedException("CMP #imm not implemented");

    /// <summary>
    /// BNE rel - Branch if Not Equal (Z=0).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BneRel() => throw new NotImplementedException("BNE rel not implemented");

    /// <summary>
    /// CPX #imm - Compare X Register, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void CpxImm() => throw new NotImplementedException("CPX #imm not implemented");

    /// <summary>
    /// SBC (ind,X) - Subtract with Carry, Indirect X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcIndX() => throw new NotImplementedException("SBC (ind,X) not implemented");

    /// <summary>
    /// SBC zp - Subtract with Carry, Zero Page.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcZp() => throw new NotImplementedException("SBC zp not implemented");

    /// <summary>
    /// SBC zp,X - Subtract with Carry, Zero Page X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcZpX() => throw new NotImplementedException("SBC zp,X not implemented");

    /// <summary>
    /// SBC abs - Subtract with Carry, Absolute.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcAbs() => throw new NotImplementedException("SBC abs not implemented");

    /// <summary>
    /// SBC abs,X - Subtract with Carry, Absolute X.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcAbsX() => throw new NotImplementedException("SBC abs,X not implemented");

    /// <summary>
    /// SBC abs,Y - Subtract with Carry, Absolute Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcAbsY() => throw new NotImplementedException("SBC abs,Y not implemented");

    /// <summary>
    /// SBC (ind),Y - Subtract with Carry, Indirect Y.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcIndY() => throw new NotImplementedException("SBC (ind),Y not implemented");

    /// <summary>
    /// INC A - Increment, Accumulator.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void IncAcc() => throw new NotImplementedException("INC A not implemented");

    /// <summary>
    /// SBC #imm - Subtract with Carry, Immediate.
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void SbcImm() => throw new NotImplementedException("SBC #imm not implemented");

    /// <summary>
    /// BEQ rel - Branch if Equal (Z=1).
    /// Obecnie niezaimplementowane.
    /// </summary>
    private void BeqRel() => throw new NotImplementedException("BEQ rel not implemented");

    #endregion
}
