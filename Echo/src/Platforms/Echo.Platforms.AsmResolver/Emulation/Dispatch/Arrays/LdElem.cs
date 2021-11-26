using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Echo.Concrete.Emulation;
using Echo.Concrete.Values;
using Echo.Platforms.AsmResolver.Emulation.Values;
using Echo.Platforms.AsmResolver.Emulation.Values.Cli;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.Arrays
{
    /// <summary>
    /// Provides a handler for instructions with the <see cref="CilOpCodes.Ldelem"/> operation code.
    /// </summary>
    public class LdElem : LdElemBase
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<CilCode> SupportedOpCodes => new[]
        {
            CilCode.Ldelem
        };

        /// <inheritdoc />
        protected override ICliValue GetElementValue(CilExecutionContext context, CilInstruction instruction, IDotNetArrayValue array, int index)
        {
            var environment = context.GetService<ICilRuntimeEnvironment>();

            var type = (ITypeDescriptor) instruction.Operand;
            var typeLayout = environment.ValueFactory.GetTypeMemoryLayout(type);

            return array.LoadElement(index, typeLayout, environment.CliMarshaller);
        }

        /// <inheritdoc />
        protected override ICliValue GetUnknownElementValue(CilExecutionContext context, CilInstruction instruction)
        {
            var environment = context.GetService<ICilRuntimeEnvironment>();
            var type = (ITypeDefOrRef) instruction.Operand;
            return environment.CliMarshaller.ToCliValue(new UnknownValue(), type.ToTypeSignature());
        }
    }
}