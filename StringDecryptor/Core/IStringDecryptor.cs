namespace StringDecryptor.Core;

internal interface IStringDecryptor {

    /// <summary>
    /// Gets Decoder Type.
    /// </summary>
    DecoderType Type { get; }

    /// <summary>
    /// Executes <see cref="IStringDecryptor"/> Implementation.
    /// </summary>
    /// <param name="context">The Context.</param>
    void Decrypt(Context context);

    /// <summary>
    /// Initializes <see cref="IStringDecryptor"/> Implementation.
    /// </summary>
    /// <param name="context">The Context.</param>
    bool Initialize(Context context);

    /// <summary>
    /// Gets Instruction Stack After Emulating Its Dependencies.
    /// </summary>
    /// <param name="methodBody">The MethodBody.</param>
    /// <param name="callInstruction">The Instruction.</param>
    /// <returns>Instruction StackState.</returns>
    /// <exception cref="InvalidOperationException">When <paramref name="callInstruction"/> Doesn't Reference a Method.</exception>
    public virtual IStackState<ICliValue> GetInstructionStack(CilMethodBody methodBody, CilInstruction callInstruction) {

        var module = methodBody.Owner.Module;
        bool is32Bit = module.IsBit32Preferred || module.IsBit32Required;

        if (callInstruction.OpCode.OperandType is not CilOperandType.InlineMethod) {
            throw new InvalidOperationException($"{nameof(callInstruction)} Doesn't Reference a Method.");
        }

        methodBody.Instructions.ExpandMacros();
        methodBody.ConstructSymbolicFlowGraph(out var dataFlowGraph);

        var virtualMachine = new CilVirtualMachine(methodBody, is32Bit);
        var executionContext = new CilExecutionContext(virtualMachine, virtualMachine.CurrentState, default);

        var parameterExpression = dataFlowGraph.GetStackDependencies(callInstruction.Offset).ToList();

        // Removes Call Instruction So it Doesn't Get Emulated.
        parameterExpression.Remove(parameterExpression.Last());

        foreach (var instruction in parameterExpression) {
            if (instruction.OpCode.Code is CilCode.Ldtoken)
                virtualMachine.CurrentState.Stack.Push(OValue.Null(is32Bit));
            else
                virtualMachine.Dispatcher.Execute(executionContext, instruction);
        }

        parameterExpression.Nop();

        return virtualMachine.CurrentState.Stack;
    }

    /// <summary>
    /// Remodels StackState Values Into Reflection (Primitives) Usable Values.
    /// </summary>
    /// <param name="stack">Emulated <see cref="CilVirtualMachine"/> StackState.</param>
    /// <param name="parameterTypes">Decryption Method ParameterTypes.</param>
    /// <returns>Unique Remodeled Object Values.</returns>
    public virtual object[] RemodelStackParameters(IStackState<ICliValue> stack, IList<Type> parameterTypes) {

        object[] args = new object[stack.Size];

        if (parameterTypes.Count != args.Length) {
            throw new ArgumentException($"{nameof(parameterTypes)} Count Isn't Equal to Arguments Length.",
                nameof(parameterTypes));
        }

        int argIndex = 0;

        for (int i = args.Length - 1; i >= 0; i--) {

            var value = stack.Pop();
            Type valueType = parameterTypes[i];

            args[argIndex++] = value switch {
                OValue objValue when valueType == typeof(string) && objValue.ReferencedObject is StringValue stringValue => stringValue.GetStringValue(),
                I4Value i16Value when valueType == typeof(short) => (short)i16Value.I32,
                I4Value u16Value when valueType == typeof(ushort) => (ushort)u16Value.U32,
                I4Value i32Value when valueType == typeof(int) => i32Value.I32,
                I4Value u32Value when valueType == typeof(uint) => u32Value.U32,
                I8Value i8Value when valueType == typeof(long) => i8Value.I64,
                I8Value u8Value when valueType == typeof(ulong) => u8Value.U64,
                Float32Value r4Value when valueType == typeof(float) => r4Value.F32,
                Float64Value r8Value when valueType == typeof(double) => r8Value.F64,
                _ => valueType.InitializeObject(), /* Would be a fake value. */
            };
        }

        return args;
    }
}