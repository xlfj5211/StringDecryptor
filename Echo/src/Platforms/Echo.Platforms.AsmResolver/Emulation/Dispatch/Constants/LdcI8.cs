using System.Collections.Generic;
using AsmResolver.PE.DotNet.Cil;
using Echo.Concrete.Emulation;
using Echo.Concrete.Values.ValueType;
using Echo.Platforms.AsmResolver.Emulation.Values.Cli;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.Constants
{
    /// <summary>
    /// Provides a handler for instructions with the <see cref="CilOpCodes.Ldc_I8"/> operation code.
    /// </summary>
    public class LdcI8 : FallThroughOpCodeHandler
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<CilCode> SupportedOpCodes => new[]
        {
            CilCode.Ldc_I8
        };

        /// <inheritdoc />
        public override DispatchResult Execute(CilExecutionContext context, CilInstruction instruction)
        {
            context.ProgramState.Stack.Push(new I8Value((long) instruction.Operand));
            return base.Execute(context, instruction);
        }
    }
}