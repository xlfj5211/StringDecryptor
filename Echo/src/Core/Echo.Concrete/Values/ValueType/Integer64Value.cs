using System;
using System.Buffers.Binary;
using Echo.Core;
using Echo.Core.Emulation;

namespace Echo.Concrete.Values.ValueType
{
    /// <summary>
    /// Represents a (partially) known concrete 64 bit integral value.
    /// </summary>
    public class Integer64Value : IntegerValue
    {
        /// <summary>
        /// Wraps an unsigned 64 bit integer into a fully concrete and known instance of <see cref="Integer64Value"/>.
        /// </summary>
        /// <param name="value">The 64 bit integer to wrap.</param>
        /// <returns>The concrete 64 bit integer.</returns>
        public static implicit operator Integer64Value(ulong value)
        {
            return new Integer64Value(value);
        }

        /// <summary>
        /// Wraps a signed 64 bit integer into a fully concrete and known instance of <see cref="Integer64Value"/>.
        /// </summary>
        /// <param name="value">The 64 bit integer to wrap.</param>
        /// <returns>The concrete 64 bit integer.</returns>
        public static implicit operator Integer64Value(long value)
        {
            return new Integer64Value(value);
        }

        /// <summary>
        /// Parses a (partially) known bit string into an 64 bit integer.
        /// </summary>
        /// <param name="bitString">The bit string to parse.</param>
        /// <returns>The 64 bit integer.</returns>
        public static implicit operator Integer64Value(string bitString)
        {
            return new Integer64Value(bitString);
        }

        /// <summary>
        /// Represents the bitmask that is used for a fully known concrete 64 bit integral value. 
        /// </summary>
        public const ulong FullyKnownMask = 0xFFFFFFFF_FFFFFFFF;

        private ulong _value;

        /// <summary>
        /// Creates a new, fully known concrete 64 bit integral value.
        /// </summary>
        /// <param name="value">The raw 64 bit value.</param>
        public Integer64Value(ulong value)
            : this(value, FullyKnownMask)
        {
        }

        /// <summary>
        /// Creates a new, fully known concrete 64 bit integral value.
        /// </summary>
        /// <param name="value">The raw 64 bit value.</param>
        public Integer64Value(long value)
            : this(value, FullyKnownMask)
        {
        }

        /// <summary>
        /// Creates a new, partially known concrete 64 bit integral value.
        /// </summary>
        /// <param name="value">The raw 64 bit value.</param>
        /// <param name="mask">The bit mask indicating the bits that are known.</param>
        public Integer64Value(long value, ulong mask)
            : this(unchecked((ulong) value), mask)
        {
        }

        /// <summary>
        /// Creates a new, partially known concrete 64 bit integral value.
        /// </summary>
        /// <param name="value">The raw 64 bit value.</param>
        /// <param name="mask">The bit mask indicating the bits that are known.</param>
        public Integer64Value(ulong value, ulong mask)
        {
            _value = value;
            Mask = mask;
        }

        /// <summary>
        /// Parses a (partially) known bit string into an 64 bit integer.
        /// </summary>
        /// <param name="bitString">The bit string to parse.</param>
        public Integer64Value(string bitString)
        {
            SetBits(bitString);
        }

        /// <inheritdoc />
        public override bool IsKnown => Mask == FullyKnownMask;

        /// <inheritdoc />
        public override int Size => sizeof(ulong);

        /// <inheritdoc />
        public override Trilean IsZero
        {
            get
            {
                if (IsKnown)
                    return U64 == 0;
                return base.IsZero;
            }
        }

        /// <summary>
        /// Gets the signed representation of this 64 bit value.
        /// </summary>
        public long I64
        {
            get => unchecked((long) U64);
            set => U64 = unchecked((ulong) value);
        }

        /// <summary>
        /// Gets the unsigned representation of this 64 bit value.
        /// </summary>
        public ulong U64
        {
            get => _value & Mask;
            set => _value = value;
        }

        /// <summary>
        /// Gets a value indicating which bits in the integer are known.
        /// If bit at location <c>i</c> equals 1, bit <c>i</c> in <see cref="I64"/> and <see cref="U64"/> is known,
        /// and unknown otherwise.  
        /// </summary>
        public ulong Mask
        {
            get;
            set;
        }

        /// <inheritdoc />
        public override void GetBits(Span<byte> buffer) => BinaryPrimitives.WriteUInt64LittleEndian(buffer, U64);

        /// <inheritdoc />
        public override Trilean GetBit(int index)
        {
            if (index < 0 || index >= 64)
                throw new ArgumentOutOfRangeException(nameof(index));
            
            return ((Mask >> index) & 1) == 1 
                ? ((U64 >> index) & 1) == 1 
                : Trilean.Unknown;
        }

        /// <inheritdoc />
        public override void SetBit(int index, Trilean value)
        {
            if (index < 0 || index >= 64)
                throw new ArgumentOutOfRangeException(nameof(index));

            ulong mask = 1ul << index;

            if (value.IsKnown)
            {
                Mask |= mask;
                U64 = (U64 & ~mask) | ((value.ToBooleanOrFalse() ? 1ul : 0ul) << index);
            }
            else
            {
                U64 = U64 & ~mask;
                Mask &= ~mask;
            }
        }

        /// <inheritdoc />
        public override void GetMask(Span<byte> buffer) => BinaryPrimitives.WriteUInt64LittleEndian(buffer, Mask);

        /// <inheritdoc />
        public override void SetBits(ReadOnlySpan<byte> bits, ReadOnlySpan<byte> mask)
        {
            if (bits.Length != 8 || mask.Length != 8)
                throw new ArgumentException("Number of bits is not 64.");

            U64 = BinaryPrimitives.ReadUInt64LittleEndian(bits);
            Mask = BinaryPrimitives.ReadUInt64LittleEndian(mask);
        }

        /// <inheritdoc />
        public override IValue Copy() => new Integer64Value(U64, Mask);

        /// <inheritdoc />
        public override void Not()
        {
            U64 = ~U64;
        }

        /// <inheritdoc />
        public override void And(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 = U64 & int64.U64;
            else
                base.And(other);
        }

        /// <inheritdoc />
        public override void Or(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 = U64 | int64.U64;
            else
                base.Or(other);
        }

        /// <inheritdoc />
        public override void Xor(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 = U64 ^ int64.U64;
            else
                base.Xor(other);
        }

        /// <inheritdoc />
        public override void Add(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 += int64.U64;
            else
                base.Add(other);
        }

        /// <inheritdoc />
        public override void Subtract(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 -= int64.U64;
            else
                base.Subtract(other);
        }

        /// <inheritdoc />
        public override void Multiply(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 *= int64.U64;
            else
                base.Multiply(other);
        }

        /// <inheritdoc />
        public override void Divide(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 /= int64.U64;
            else
                base.Divide(other);
        }

        /// <inheritdoc />
        public override void Remainder(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                U64 %= int64.U64;
            else
                base.Remainder(other);
        }

        /// <inheritdoc />
        public override Trilean IsEqualTo(IntegerValue other)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
                return U64 == int64.U64;
            else
                return base.IsEqualTo(other);
        }

        /// <inheritdoc />
        public override Trilean IsGreaterThan(IntegerValue other, bool signed)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
            {
                return signed
                    ? I64 > int64.I64
                    : U64 > int64.U64;
            }

            return base.IsGreaterThan(other, signed);
        }

        /// <inheritdoc />
        public override Trilean IsLessThan(IntegerValue other, bool signed)
        {
            if (IsKnown && other.IsKnown && other is Integer64Value int64)
            {
                return signed
                    ? I64 < int64.I64
                    : U64 < int64.U64;
            }

            return base.IsLessThan(other, signed);
        }

        /// <inheritdoc />
        public override void MarkFullyUnknown()
        {
            Mask = 0;
        }
    }
}