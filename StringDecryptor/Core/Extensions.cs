namespace StringDecryptor.Core;

internal static class Extensions {

    /// <summary>
    /// Creates default(<paramref name="type"/>) Using Reflection Type.
    /// </summary>
    /// <param name="type">The Type.</param>
    /// <returns>New Initialized <paramref name="type"/> Object.</returns>
    public static object InitializeObject(this Type type) {
        var dynamicMethod = new DynamicMethod(string.Empty, type,
            Type.EmptyTypes, typeof(Extensions).Module, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var addressVariable = ilGenerator.DeclareLocal(type);
        ilGenerator.Emit(OpCodes.Ldloca, addressVariable);
        ilGenerator.Emit(OpCodes.Initobj, type);
        ilGenerator.Emit(OpCodes.Ldloc, addressVariable);
        ilGenerator.Emit(OpCodes.Ret);
        return dynamicMethod.Invoke(null, null)!;
    }

    /// <summary>
    /// Gets String From StringValue.
    /// </summary>
    /// <param name="stringValue">The StringValue.</param>
    /// <returns>String Referenced in StringValue.</returns>
    public static string GetStringValue(this StringValue stringValue) {

        char[] chars = new char[stringValue.Length];

        for (var i = 0; i < stringValue.Length; i++) {
            chars[i] = (char)stringValue.GetChar(i).I16;
        }

        return new string(chars);
    }

    /// <summary>
    /// Gets All StackDependencies in <paramref name="offset"/>.
    /// </summary>
    /// <param name="dataFlowGraph">The DataFlowGraph.</param>
    /// <param name="offset">CilInstruction Offset.</param>
    /// <returns>All Dependency Nodes Contents Recursively.</returns>
    public static IEnumerable<CilInstruction> GetStackDependencies(this DataFlowGraph<CilInstruction> dataFlowGraph, int offset) =>
        dataFlowGraph.Nodes[offset].GetOrderedDependencies(DependencyCollectionFlags.IncludeStackDependencies).Select(node => node.Contents);

    /// <summary>
    /// Nops The Instruction.
    /// </summary>
    /// <param name="instruction">The Instruction.</param>
    public static void Nop(this CilInstruction instruction) =>
        (instruction.OpCode, instruction.Operand) = (CilOpCodes.Nop, null);

    /// <summary>
    /// Nops The Instructions.
    /// </summary>
    /// <param name="instructions">The Instructions.</param>
    public static void Nop(this IEnumerable<CilInstruction> instructions) {
        foreach (var instruction in instructions) {
            instruction.Nop();
        }
    }

    /// <summary>
    /// Gets Reflection Type Representation From <see cref="ITypeDefOrRef"/>.
    /// </summary>
    /// <param name="type">The Type.</param>
    /// <returns>Reflection Type.</returns>
    public static Type GetReflectionType(this ITypeDefOrRef type) =>
        Type.GetType(type.FullName, false) ?? typeof(object);

    /// <summary>
    /// Gets Reflection Method Or Constructor From Mscorlib.
    /// </summary>
    /// <typeparam name="T">The MemberInfo Type.</typeparam>
    /// <param name="method">The Method Or Constructor.</param>
    /// <returns>Reflection Method Or Constructor.</returns>
    public static T? GetCorlibMethod<T>(this IMethodDefOrRef method) where T : MemberInfo {
#pragma warning disable CS8604   
#pragma warning disable CS8600   
        object? members = method.Name.Contains("ctor")
            ? typeof(int).Module.GetTypes().FirstOrDefault(x => x.FullName == method.DeclaringType.FullName)?.GetConstructors()
            : typeof(int).Module.GetTypes().FirstOrDefault(x => x.FullName == method.DeclaringType.FullName)?.GetMethods();

        if (method.Signature.ParameterTypes.Count <= 0) {
            return members is ConstructorInfo[] cctors
                ? (T)(object)cctors.FirstOrDefault(x => x.Name == method.Name)
                : (T)(object)((MethodInfo[])members).FirstOrDefault(x => x.Name == method.Name);
        }
        else {
            return members is ConstructorInfo[] cctors
                ? (T)(object)cctors.FirstOrDefault(x => x.Name == method.Name && x.GetParameters().IsEqual(method.GetParametersTypes()))
                : (T)(object)((MethodInfo[])members).FirstOrDefault(x => x.Name == method.Name && x.GetParameters().Length == method.Signature.ParameterTypes.Count && x.GetParameters().IsEqual(method.GetParametersTypes()));
        }
#pragma warning restore CS8600
#pragma warning restore CS8604
    }

    /// <summary>
    /// Gets Method Parameter Types in Reflection Types.
    /// </summary>
    /// <param name="method">The Method.</param>
    /// <returns>Method Reflection Parameter Types.</returns>
    public static Type[] GetParametersTypes(this IMethodDescriptor method) {
        var retArray = new Type[method.Signature.ParameterTypes.Count];

        for (int x = 0; x < retArray.Length; x++)
            retArray[x] = method.Signature.ParameterTypes[x].ToTypeDefOrRef().GetReflectionType();

        return retArray;
    }

    /// <summary>
    /// Custom Equality Method.
    /// </summary>
    /// <returns>Equal Or Not.</returns>
    public static bool IsEqual(this ParameterInfo[] left, Type[] right) {
        if (right.Length != left.Length)
            return false;
        for (int i = 0; i < right.Length; i++)
            if (left[i].ParameterType != right[i])
                return false;
        return true;
    }
}