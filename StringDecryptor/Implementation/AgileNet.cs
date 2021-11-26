namespace StringDecryptor.Implementation;

internal class AgileNet : IStringDecryptor {

    private const string HashtableSignature = "System.Collections.Hashtable";
    private const string ByteArraySignature = "System.Byte[]";
    private DynamicEmulator<string>? _decryptor;
    private MethodDefinition? _decryptorMethod;

    public DecoderType Type => DecoderType.AgileNet;

    public void Decrypt(Context context) {

        var moduleMethods = context.Module.GetAllTypes()
            .SelectMany(type => type.Methods)
            .Where(method => method.MethodBody is CilMethodBody);

        var decryptorTypes = _decryptorMethod!.GetParametersTypes();

        foreach (var method in moduleMethods) {
            var instructions = method.CilMethodBody.Instructions;

            for (int i = 0; i < instructions.Count; i++) { 
                var instruction = instructions[i];

                if (instruction.OpCode.Code is not CilCode.Call) continue;
                if (instruction.Operand != _decryptorMethod) continue;

                var stack = ((IStringDecryptor)this).GetInstructionStack(method.CilMethodBody, instruction);
                var args = ((IStringDecryptor)this).RemodelStackParameters(stack, decryptorTypes);

                instruction.OpCode = CilOpCodes.Ldstr;
                instruction.Operand = _decryptor!.Invoke(args);
            }

        }

    }

    public bool Initialize(Context context) {
        TypeDefinition? matchedType = null;

        foreach (var type in context.Module.GetAllTypes().Where(type => type.Namespace is null)) {

            if (type.Fields.Count is not 2) continue;

            if (type.Methods.Count is not 2) continue;
            if (type.Methods.Count(method => method.IsConstructor) is not 1) continue;
            if (type.Methods.Count(method => method.Signature.ReturnType.FullName is "System.String") is not 1) continue;

            if (type.Fields.FirstOrDefault(fld => fld.Signature.FieldType.FullName is HashtableSignature) is null) continue;
            if (type.Fields.FirstOrDefault(fld => fld.Signature.FieldType.FullName is ByteArraySignature) is null) continue;

            // More Checks Can Be Added...

            matchedType = type;
            break;
        }

        if (matchedType is null) {
            return false;
        }
        else {

            var hashtableField = matchedType.Fields.First(fld => fld.Signature.FieldType.FullName is HashtableSignature);
            var byteArrayField = matchedType.Fields.First(fld => fld.Signature.FieldType.FullName is ByteArraySignature);
            _decryptorMethod = matchedType.Methods.First(method => method.Signature.ReturnType.FullName is "System.String");

            var rvaField = matchedType.GetStaticConstructor().CilMethodBody.Instructions.First(i => i.OpCode.Code is CilCode.Ldtoken).Operand as FieldDefinition;

            byte[] keyValue = ((DataSegment)rvaField!.FieldRva).Data;

            _decryptorMethod.CilMethodBody.Instructions.ExpandMacros();
            _decryptorMethod.CilMethodBody.Instructions.CalculateOffsets();

            _decryptor = new DynamicEmulator<string>(_decryptorMethod);

            _decryptor.DefineField(hashtableField);
            _decryptor.DefineField(byteArrayField);

            _decryptor.Initialize();

            _decryptor.CreateType();

            _decryptor.SetFieldValue(hashtableField, new Hashtable());
            _decryptor.SetFieldValue(byteArrayField, keyValue);

            return true;
        }
    }
}