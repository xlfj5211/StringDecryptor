namespace StringDecryptor.Core;

/// <summary>
/// Dynamic CIL Emualtor.
/// </summary>
/// <typeparam name="T">Return Type.</typeparam>
internal class DynamicEmulator<T> {

    private readonly IDictionary<CilInstruction, Label> _labels = new Dictionary<CilInstruction, Label>();
    private readonly IDictionary<CilLocalVariable, LocalBuilder> _locals = new Dictionary<CilLocalVariable, LocalBuilder>();
    private readonly IDictionary<FieldDefinition, FieldBuilder> _fields = new Dictionary<FieldDefinition, FieldBuilder>();
    private readonly IList<CilInstruction> _instructions;
    private readonly IList<CilLocalVariable> _localVariables;
    private readonly IList<CilExceptionHandler> _exceptionHandlers;
    private readonly string _methodName;
    private readonly Type _returnType;
    private readonly ILGenerator _ilGenerator;
    private readonly AssemblyBuilder _assemblyBuilder;
    private readonly ModuleBuilder _moduleBuilder;
    private readonly TypeBuilder _typeBuilder;
    private readonly MethodBuilder _methodBuilder;
    private Type? _createdType;
    private MethodInfo? _createdMethod;

    private readonly IDictionary<short, OpCode> _opcodesCache = typeof(OpCodes).GetFields()
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    /// <summary>
    /// <see cref="DynamicEmulator{T}"/> Constructor.
    /// </summary>
    public DynamicEmulator(MethodDefinition methodDefinition) 
        : this(methodDefinition.CilMethodBody) { }
    /// <summary>
    /// <see cref="DynamicEmulator{T}"/> Constructor.
    /// </summary>
    public DynamicEmulator(CilMethodBody methodBody)
        : this(methodBody.Instructions, methodBody.LocalVariables, methodBody.ExceptionHandlers, methodBody.Owner.GetParametersTypes(), methodBody.Owner.Name, methodBody.InitializeLocals) { }
    /// <summary>
    /// <see cref="DynamicEmulator{T}"/> Constructor.
    /// </summary>
    public DynamicEmulator(IEnumerable<CilInstruction> instructions, IEnumerable<CilLocalVariable> localVariables, IEnumerable<CilExceptionHandler> exceptionHandlers, IEnumerable<Type> parameterTypes, string methodName, bool initializeVariables) {
        _instructions = instructions as IList<CilInstruction> ?? instructions.ToList();
        _localVariables = localVariables as IList<CilLocalVariable> ?? localVariables.ToList();
        _exceptionHandlers = exceptionHandlers as IList<CilExceptionHandler> ?? exceptionHandlers.ToList();
        _returnType = typeof(T);

        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("A"), AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule("M");

        _typeBuilder = _moduleBuilder.DefineType("T", TypeAttributes.Public | TypeAttributes.Class);
        _methodBuilder = _typeBuilder.DefineMethod(_methodName = methodName, MethodAttributes.Public | MethodAttributes.Static,
            _returnType, parameterTypes as Type[] ?? parameterTypes.ToArray());
        _ilGenerator = _methodBuilder.GetILGenerator();
        _methodBuilder.InitLocals = initializeVariables;
    }

    /// <summary>
    /// Gets CreatedType.
    /// </summary>
    public Type? CreatedType => _createdType;

    /// <summary>
    /// Invokes Hosted Method.
    /// </summary>
    /// <param name="args">Method Arguments.</param>
    /// <returns>Invocation Result.</returns>
    public T? Invoke(object?[]? args) =>
        (T?)(_createdMethod?.Invoke(null, args));
    
    /// <summary>
    /// Initialize Labels, Variables, Instructions.
    /// </summary>
    public void Initialize() {
        InitializeVariables();
        InitializeLabels();
        InitializeInstructions();
    }

    /// <summary>
    /// Hosts Dynamic Assembly.
    /// </summary>
    public void CreateType() {
        _createdType = _typeBuilder.CreateType();
        _createdMethod = _createdType?.GetMethod(_methodName);
    }

    /// <summary>
    /// Defines Dynamic Field.
    /// </summary>
    /// <param name="field">The Field.</param>
    public void DefineField(FieldDefinition field) =>
        _fields[field] = _typeBuilder.DefineField(field.Name, field.Signature.FieldType.ToTypeDefOrRef().GetReflectionType(), FieldAttributes.Public | FieldAttributes.Static);

    /// <summary>
    /// Sets Dynamic Field Value.
    /// </summary>
    /// <param name="field">The Field.</param>
    /// <param name="value">The Value.</param>
    public void SetFieldValue(FieldDefinition field, object value) =>
        _createdType?.GetField(field.Name)?.SetValue(null, value);

    /// <summary>
    /// Gets Dynamic Field Value.
    /// </summary>
    /// <param name="field">The Field.</param>
    public object? GetFieldValue(FieldDefinition field) =>
        _createdType?.GetField(field.Name)?.GetValue(null);

    /// <summary>
    /// Appends CilInstruction Into IlGenerator.
    /// </summary>
    /// <param name="ilGenerator">The IlGenerator.</param>
    /// <param name="instruction">The CilInstruction.</param>
    /// <exception cref="NotSupportedException">Not Supported OpCode.</exception>
    void AppendInstruction(ILGenerator ilGenerator, CilInstruction instruction) {
        var opCode = _opcodesCache[(short)instruction.OpCode.Code];

        switch (opCode.OperandType) {
            case OperandType.InlineBrTarget:
                ilGenerator.Emit(opCode, _labels[_instructions.GetByOffset(((ICilLabel)instruction.Operand).Offset)]);
                break;
            case OperandType.InlineField:
                ilGenerator.Emit(opCode, _fields[(FieldDefinition)instruction.Operand]);
                break;
            case OperandType.InlineI:
                ilGenerator.Emit(opCode, instruction.GetLdcI4Constant());
                break;
            case OperandType.InlineI8:
                ilGenerator.Emit(opCode, (long)instruction.Operand);
                break;
            case OperandType.InlineMethod:
                var descriptor = (IMethodDefOrRef)instruction.Operand;
                if (descriptor.Name.Contains("ctor")) {
                    ilGenerator.Emit(opCode, descriptor.GetCorlibMethod<ConstructorInfo>()!);
                }
                else {
                    ilGenerator.Emit(opCode, descriptor.GetCorlibMethod<MethodInfo>()!);
                }
                break;
            case OperandType.InlineNone:
                ilGenerator.Emit(opCode);
                break;
            case OperandType.InlineR:
                ilGenerator.Emit(opCode, (double)instruction.Operand);
                break;
            case OperandType.ShortInlineR:
                ilGenerator.Emit(opCode, (float)instruction.Operand);
                break;
            case OperandType.InlineString:
                ilGenerator.Emit(opCode, instruction.Operand as string ?? string.Empty);
                break;
            case OperandType.InlineSwitch:
                ilGenerator.Emit(opCode, ((IList<ICilLabel>)instruction.Operand).Select(label => _labels[_instructions.GetByOffset(label.Offset)]).ToArray());
                break;
            case OperandType.InlineTok:
            case OperandType.InlineType:
                ilGenerator.Emit(opCode, ((ITypeDefOrRef)instruction.Operand).GetReflectionType());
                break;
            case OperandType.InlineVar:
                if (instruction.Operand is Parameter parameter) {
                    ilGenerator.Emit(opCode, parameter.MethodSignatureIndex);
                }
                else {
                    ilGenerator.Emit(opCode, _locals[(CilLocalVariable)instruction.Operand]);
                }
                break;
            default:
                throw new NotSupportedException(nameof(opCode.OperandType));
        }

    }

    /// <summary>
    /// Initialize Branch Labels.
    /// </summary>
    void InitializeLabels() {
        for (int i = 0; i < _instructions.Count; i++) {
            _labels[_instructions[i]] = _ilGenerator.DefineLabel();
        }
    }

    /// <summary>
    /// Initialize Instructions.
    /// </summary>
    void InitializeInstructions() {

        var labelIndex = (ICilLabel label) => _instructions.GetIndexByOffset(label.Offset);

        for (int i = 0; i < _instructions.Count; i++) {
            var instruction = _instructions[i];

            // ew.
            if (_exceptionHandlers.Where(handler => labelIndex(handler.HandlerStart) == i) is IEnumerable<CilExceptionHandler> handlerStart) {
                foreach (var handler in handlerStart) {
                    if (handler.HandlerType is CilExceptionHandlerType.Exception) {
                        _ilGenerator.BeginCatchBlock(handler.ExceptionType.GetReflectionType());
                    }
                }
            }

            if (_exceptionHandlers.Where(handler => labelIndex(handler.TryStart) == i) is IEnumerable<CilExceptionHandler> tryStart) {
                foreach (var handler in tryStart) {
                    _ilGenerator.BeginExceptionBlock();
                }
            }

            if (_exceptionHandlers.Where(handler => labelIndex(handler.HandlerEnd) == i) is IEnumerable<CilExceptionHandler> handlerEnd) {
                foreach (var handler in handlerEnd) {
                    _ilGenerator.EndExceptionBlock();
                }
            }

            if (_exceptionHandlers.Where(handler => labelIndex(handler.TryEnd) == i) is IEnumerable<CilExceptionHandler> tryEnd) {
                foreach (var handler in tryEnd) {
                    if (handler.HandlerType is CilExceptionHandlerType.Finally) {
                        _ilGenerator.BeginFinallyBlock();
                    }
                }
            }


            _ilGenerator.MarkLabel(_labels[instruction]);


            AppendInstruction(_ilGenerator, instruction);
        }
    }

    /// <summary>
    /// Initialize Local Variables.
    /// </summary>
    void InitializeVariables() {
        for (int i = 0; i < _localVariables.Count; i++) {
            var localVariable = _localVariables[i];
            _locals[localVariable] = _ilGenerator.DeclareLocal(localVariable.VariableType.ToTypeDefOrRef().GetReflectionType()); 
        }
    }
}