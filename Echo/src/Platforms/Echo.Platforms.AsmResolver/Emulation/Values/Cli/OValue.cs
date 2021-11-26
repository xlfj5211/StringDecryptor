using System;
using Echo.Concrete.Values;
using Echo.Concrete.Values.ReferenceType;
using Echo.Core.Emulation;

namespace Echo.Platforms.AsmResolver.Emulation.Values.Cli
{
    /// <summary>
    /// Represents an object reference on the evaluation stack of the Common Language Infrastructure (CLI).
    /// </summary>
    public class OValue : ObjectReference, ICliValue
    {
        /// <summary>
        /// Creates a new null object reference value. 
        /// </summary>
        /// <param name="is32Bit">Indicates whether the reference to the object is 32 or 64 bits wide.</param>
        /// <returns>The null reference.</returns>
        public new static OValue Null(bool is32Bit) => new OValue(null, true, is32Bit);

        /// <summary>
        /// Creates a new object reference value.
        /// </summary>
        /// <param name="referencedObject">The referenced value.</param>
        /// <param name="isKnown">Indicates whether the value is known.</param>
        /// <param name="is32Bit">Indicates whether the reference to the object is 32 or 64 bits wide.</param>
        public OValue(IConcreteValue referencedObject, bool isKnown, bool is32Bit)
            : base(referencedObject, isKnown, is32Bit)
        {
        }
        
        /// <inheritdoc />
        public CliValueType CliValueType => CliValueType.O; 

        /// <inheritdoc />
        public override IValue Copy() => new OValue(ReferencedObject, IsKnown, Is32Bit);
        
        /// <inheritdoc />
        public NativeIntegerValue InterpretAsI(bool is32Bit)
        {
            var value = new NativeIntegerValue(0, is32Bit);
            if (!IsZero.ToBooleanOrFalse())
                value.MarkFullyUnknown();
            return value;
        }

        /// <inheritdoc />
        public NativeIntegerValue InterpretAsU(bool is32Bit)
        {
            var value = new NativeIntegerValue(0, is32Bit);
            if (!IsZero.ToBooleanOrFalse())
                value.MarkFullyUnknown();
            return value;
        }

        /// <inheritdoc />
        public I4Value InterpretAsI1() => new I4Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0xFFFFFF00);

        /// <inheritdoc />
        public I4Value InterpretAsU1() => new I4Value(0, !IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0xFFFFFF00);

        /// <inheritdoc />
        public I4Value InterpretAsI2() => new I4Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0xFFFF0000);

        /// <inheritdoc />
        public I4Value InterpretAsU2() => new I4Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0xFFFF0000);

        /// <inheritdoc />
        public I4Value InterpretAsI4() => new I4Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0x00000000);

        /// <inheritdoc />
        public I4Value InterpretAsU4() => new I4Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF : 0x00000000);

        /// <inheritdoc />
        public I8Value InterpretAsI8() => new I8Value(0, IsZero.ToBooleanOrFalse() ? 0xFFFFFFFF_FFFFFFFF : 0);

        /// <inheritdoc />
        public FValue InterpretAsR4() => new FValue(0); // TODO: return unknown float.

        /// <inheritdoc />
        public FValue InterpretAsR8() => new FValue(0); // TODO: return unknown float.

        /// <inheritdoc />
        public OValue InterpretAsRef(bool is32Bit) => this;

        /// <inheritdoc />
        public NativeIntegerValue ConvertToI(bool is32Bit, bool unsigned, out bool overflowed)
        {
            overflowed = false;
            return InterpretAsI(is32Bit);
        }

        /// <inheritdoc />
        public NativeIntegerValue ConvertToU(bool is32Bit, bool unsigned, out bool overflowed)
        {
            overflowed = false;
            return InterpretAsU(is32Bit);
        }

        /// <inheritdoc />
        public I4Value ConvertToI1(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I4Value ConvertToU1(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I4Value ConvertToI2(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I4Value ConvertToU2(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I4Value ConvertToI4(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I4Value ConvertToU4(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I8Value ConvertToI8(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public I8Value ConvertToU8(bool unsigned, out bool overflowed) => throw new InvalidCastException();

        /// <inheritdoc />
        public FValue ConvertToR4() => throw new InvalidCastException();

        /// <inheritdoc />
        public FValue ConvertToR8() => throw new InvalidCastException();

        /// <inheritdoc />
        public FValue ConvertToR() => throw new InvalidCastException();
        
        /// <inheritdoc />
        public override string ToString() => $"O ({ReferencedObject})";
    }
}