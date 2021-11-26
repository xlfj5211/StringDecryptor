using System.Collections.Generic;
using AsmResolver.PE.DotNet.Cil;
using Echo.Concrete.Emulation;
using Echo.Concrete.Values.ValueType;
using Echo.Platforms.AsmResolver.Emulation.Values.Cli;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.Operators
{
    /// <summary>
    /// Provides a handler for instructions with the <see cref="CilOpCodes.Clt"/> or <see cref="CilOpCodes.Clt_Un"/>
    /// operation code.
    /// </summary>
    public class Clt : ComparisonOperator
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<CilCode> SupportedOpCodes => new[]
        {
            CilCode.Clt, CilCode.Clt_Un
        };

        /// <inheritdoc />
        protected override DispatchResult Execute(CilExecutionContext context, CilInstruction instruction, 
            FValue left, FValue right)
        {
            bool result = left.IsLessThan(right, instruction.OpCode.Code == CilCode.Clt_Un);
            return ConvertToI4AndReturnSuccess(context, result);
        }

        /// <inheritdoc />
        protected override DispatchResult Execute(CilExecutionContext context, CilInstruction instruction, 
            IntegerValue left, IntegerValue right)
        {
            var result = left.IsLessThan(right, instruction.OpCode.Code == CilCode.Clt);
            return ConvertToI4AndReturnSuccess(context, result);
        }

        /// <inheritdoc />
        protected override DispatchResult Execute(CilExecutionContext context, CilInstruction instruction, OValue left, OValue right)
        {
            var result = left.IsLessThan(right);
            return ConvertToI4AndReturnSuccess(context, result);
        }
        
    }
}