﻿using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using Echo.Concrete.Emulation;
using Echo.Platforms.AsmResolver.Emulation.Values.Cli;

namespace Echo.Platforms.AsmResolver.Emulation.Dispatch.ObjectModel
{
    /// <summary>
    /// Provides a handler for instructions with the <see cref="CilOpCodes.Newobj"/> operation code.
    /// </summary>
    public class NewObj : FallThroughOpCodeHandler
    {
        /// <inheritdoc />
        public override IReadOnlyCollection<CilCode> SupportedOpCodes => new[]
        {
            CilCode.Newobj
        };

        /// <inheritdoc />
        public override DispatchResult Execute(CilExecutionContext context, CilInstruction instruction)
        {
            var environment = context.GetService<ICilRuntimeEnvironment>();
            var method = (IMethodDescriptor) instruction.Operand;
            
            //Allocate Object
            var type = method.DeclaringType.ToTypeSignature();
            var newValue = type.IsValueType
                ? environment.ValueFactory.CreateValue(type, true)
                : environment.ValueFactory.CreateObject(type, true);

            var cilValueObject = environment.CliMarshaller.ToCliValue(newValue, type);

            // Pop arguments.
            int argumentCount = environment.Architecture.GetStackPopCount(instruction);
            var arguments = context.ProgramState.Stack.Pop(argumentCount, true).ToList();

            arguments.Insert(0, type.IsValueType
                ? new OValue(cilValueObject, true, environment.Is32Bit)
                : cilValueObject);

            // Dispatch
            var methodDispatch = new MethodDevirtualizationResult(method);
            if (methodDispatch.Exception != null)
                return new DispatchResult(methodDispatch.Exception);

            // Invoke.
            var marshalledArguments = CallBase.MarshalMethodArguments(environment, arguments, method.Signature);
            var result = environment.MethodInvoker.Invoke(method, marshalledArguments);

            if (result == null)
                context.ProgramState.Stack.Push(cilValueObject);
            else
                context.ProgramState.Stack.Push(environment.CliMarshaller.ToCliValue(result, type));

            return base.Execute(context, instruction);
        }
    }
}