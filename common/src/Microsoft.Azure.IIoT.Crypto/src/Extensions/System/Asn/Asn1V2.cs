// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#define netcoreapp

namespace System.Security.Cryptography.Asn1 {

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;


    // ITU-T-REC.X.680-201508 sec 4.
    internal enum AsnEncodingRules {
        BER,
        CER,
        DER,
    }

    // Uses a masked overlay of the tag class encoding.
    // T-REC-X.690-201508 sec 8.1.2.2
    internal enum TagClass : byte {
        Universal = 0,
        Application = 0b0100_0000,
        ContextSpecific = 0b1000_0000,
        Private = 0b1100_0000,
    }

    // ITU-T-REC.X.680-201508 sec 8.6
    internal enum UniversalTagNumber {
        EndOfContents = 0,
        Boolean = 1,
        Integer = 2,
        BitString = 3,
        OctetString = 4,
        Null = 5,
        ObjectIdentifier = 6,
        ObjectDescriptor = 7,
        External = 8,
        InstanceOf = External,
        Real = 9,
        Enumerated = 10,
        Embedded = 11,
        UTF8String = 12,
        RelativeObjectIdentifier = 13,
        Time = 14,
        // 15 is reserved
        Sequence = 16,
        SequenceOf = Sequence,
        Set = 17,
        SetOf = Set,
        NumericString = 18,
        PrintableString = 19,
        TeletexString = 20,
        T61String = TeletexString,
        VideotexString = 21,
        IA5String = 22,
        UtcTime = 23,
        GeneralizedTime = 24,
        GraphicString = 25,
        VisibleString = 26,
        ISO646String = VisibleString,
        GeneralString = 27,
        UniversalString = 28,
        UnrestrictedCharacterString = 29,
        BMPString = 30,
        Date = 31,
        TimeOfDay = 32,
        DateTime = 33,
        Duration = 34,
        ObjectIdentifierIRI = 35,
        RelativeObjectIdentifierIRI = 36,
    }

    // Represents a BER-family encoded tag.
    // T-REC-X.690-201508 sec 8.1.2
    internal struct Asn1Tag : IEquatable<Asn1Tag> {
        private const byte kClassMask = 0b1100_0000;
        private const byte kConstructedMask = 0b0010_0000;
        private const byte kControlMask = kClassMask | kConstructedMask;
        private const byte kTagNumberMask = 0b0001_1111;

        internal static readonly Asn1Tag EndOfContents = new Asn1Tag(0, (int)UniversalTagNumber.EndOfContents);
        internal static readonly Asn1Tag Boolean = new Asn1Tag(0, (int)UniversalTagNumber.Boolean);
        internal static readonly Asn1Tag Integer = new Asn1Tag(0, (int)UniversalTagNumber.Integer);
        internal static readonly Asn1Tag PrimitiveBitString = new Asn1Tag(0, (int)UniversalTagNumber.BitString);
        internal static readonly Asn1Tag ConstructedBitString = new Asn1Tag(kConstructedMask, (int)UniversalTagNumber.BitString);
        internal static readonly Asn1Tag PrimitiveOctetString = new Asn1Tag(0, (int)UniversalTagNumber.OctetString);
        internal static readonly Asn1Tag ConstructedOctetString = new Asn1Tag(kConstructedMask, (int)UniversalTagNumber.OctetString);
        internal static readonly Asn1Tag Null = new Asn1Tag(0, (int)UniversalTagNumber.Null);
        internal static readonly Asn1Tag ObjectIdentifier = new Asn1Tag(0, (int)UniversalTagNumber.ObjectIdentifier);
        internal static readonly Asn1Tag Enumerated = new Asn1Tag(0, (int)UniversalTagNumber.Enumerated);
        internal static readonly Asn1Tag Sequence = new Asn1Tag(kConstructedMask, (int)UniversalTagNumber.Sequence);
        internal static readonly Asn1Tag SetOf = new Asn1Tag(kConstructedMask, (int)UniversalTagNumber.SetOf);
        internal static readonly Asn1Tag UtcTime = new Asn1Tag(0, (int)UniversalTagNumber.UtcTime);
        internal static readonly Asn1Tag GeneralizedTime = new Asn1Tag(0, (int)UniversalTagNumber.GeneralizedTime);

        private readonly byte _controlFlags;

        public TagClass TagClass => (TagClass)(_controlFlags & kClassMask);
        public bool IsConstructed => (_controlFlags & kConstructedMask) != 0;
        public int TagValue { get; }

        private Asn1Tag(byte controlFlags, int tagValue) {
            _controlFlags = (byte)(controlFlags & kControlMask);
            TagValue = tagValue;
        }

        public Asn1Tag(UniversalTagNumber universalTagNumber, bool isConstructed = false)
            : this(isConstructed ? kConstructedMask : (byte)0, (int)universalTagNumber) {
            // T-REC-X.680-201508 sec 8.6 (Table 1)
            const UniversalTagNumber ReservedIndex = (UniversalTagNumber)15;

            if (universalTagNumber < UniversalTagNumber.EndOfContents ||
                universalTagNumber > UniversalTagNumber.RelativeObjectIdentifierIRI ||
                universalTagNumber == ReservedIndex) {
                throw new ArgumentOutOfRangeException(nameof(universalTagNumber));
            }
        }

        public Asn1Tag(TagClass tagClass, int tagValue, bool isConstructed = false)
            : this((byte)((byte)tagClass | (isConstructed ? kConstructedMask : 0)), tagValue) {
            if (tagClass < TagClass.Universal || tagClass > TagClass.Private) {
                throw new ArgumentOutOfRangeException(nameof(tagClass));
            }

            if (tagValue < 0) {
                throw new ArgumentOutOfRangeException(nameof(tagValue));
            }
        }

        public Asn1Tag AsConstructed() {
            return new Asn1Tag((byte)(_controlFlags | kConstructedMask), TagValue);
        }

        public Asn1Tag AsPrimitive() {
            return new Asn1Tag((byte)(_controlFlags & ~kConstructedMask), TagValue);
        }

        public static bool TryParse(ReadOnlySpan<byte> source, out Asn1Tag tag, out int bytesRead) {
            tag = default;
            bytesRead = 0;

            if (source.IsEmpty) {
                return false;
            }

            var first = source[bytesRead];
            bytesRead++;
            var tagValue = (uint)(first & kTagNumberMask);

            if (tagValue == kTagNumberMask) {
                // Multi-byte encoding
                // T-REC-X.690-201508 sec 8.1.2.4
                const byte ContinuationFlag = 0x80;
                const byte ValueMask = ContinuationFlag - 1;

                tagValue = 0;
                byte current;

                do {
                    if (source.Length <= bytesRead) {
                        bytesRead = 0;
                        return false;
                    }

                    current = source[bytesRead];
                    var currentValue = (byte)(current & ValueMask);
                    bytesRead++;

                    // If TooBigToShift is shifted left 7, the content bit shifts out.
                    // So any value greater than or equal to this cannot be shifted without loss.
                    const int TooBigToShift = 0b00000010_00000000_00000000_00000000;

                    if (tagValue >= TooBigToShift) {
                        bytesRead = 0;
                        return false;
                    }

                    tagValue <<= 7;
                    tagValue |= currentValue;

                    // The first byte cannot have the value 0 (T-REC-X.690-201508 sec 8.1.2.4.2.c)
                    if (tagValue == 0) {
                        bytesRead = 0;
                        return false;
                    }
                }
                while ((current & ContinuationFlag) == ContinuationFlag);

                // This encoding is only valid for tag values greater than 30.
                // (T-REC-X.690-201508 sec 8.1.2.3, 8.1.2.4)
                if (tagValue <= 30) {
                    bytesRead = 0;
                    return false;
                }

                // There's not really any ambiguity, but prevent negative numbers from showing up.
                if (tagValue > int.MaxValue) {
                    bytesRead = 0;
                    return false;
                }
            }

            Debug.Assert(bytesRead > 0);
            tag = new Asn1Tag(first, (int)tagValue);
            return true;
        }

        public int CalculateEncodedSize() {
            const int SevenBits = 0b0111_1111;
            const int FourteenBits = 0b0011_1111_1111_1111;
            const int TwentyOneBits = 0b0001_1111_1111_1111_1111_1111;
            const int TwentyEightBits = 0b0000_1111_1111_1111_1111_1111_1111_1111;

            if (TagValue < kTagNumberMask) {
                return 1;
            }

            if (TagValue <= SevenBits) {
                return 2;
            }

            if (TagValue <= FourteenBits) {
                return 3;
            }

            if (TagValue <= TwentyOneBits) {
                return 4;
            }

            if (TagValue <= TwentyEightBits) {
                return 5;
            }

            return 6;
        }

        public bool TryWrite(Span<byte> destination, out int bytesWritten) {
            var spaceRequired = CalculateEncodedSize();

            if (destination.Length < spaceRequired) {
                bytesWritten = 0;
                return false;
            }

            if (spaceRequired == 1) {
                var value = (byte)(_controlFlags | TagValue);
                destination[0] = value;
                bytesWritten = 1;
                return true;
            }

            var firstByte = (byte)(_controlFlags | kTagNumberMask);
            destination[0] = firstByte;

            var remaining = TagValue;
            var idx = spaceRequired - 1;

            while (remaining > 0) {
                var segment = remaining & 0x7F;

                // The last byte doesn't get the marker, which we write first.
                if (remaining != TagValue) {
                    segment |= 0x80;
                }

                Debug.Assert(segment <= byte.MaxValue);
                destination[idx] = (byte)segment;
                remaining >>= 7;
                idx--;
            }

            Debug.Assert(idx == 0);
            bytesWritten = spaceRequired;
            return true;
        }

        public bool Equals(Asn1Tag other) {
            return _controlFlags == other._controlFlags && TagValue == other.TagValue;
        }

        public override bool Equals(object obj) {
            if (obj is null) {
                return false;
            }

            return obj is Asn1Tag && Equals((Asn1Tag)obj);
        }

        public override int GetHashCode() {
            // Most TagValue values will be in the 0-30 range,
            // the GetHashCode value only has collisions when TagValue is
            // between 2^29 and uint.MaxValue
            return (_controlFlags << 24) ^ TagValue;
        }

        public static bool operator ==(Asn1Tag left, Asn1Tag right) => left.Equals(right);

        public static bool operator !=(Asn1Tag left, Asn1Tag right) => !left.Equals(right);

        public override string ToString() {
            const string ConstructedPrefix = "Constructed ";
            string classAndValue;

            if (TagClass == TagClass.Universal) {
                classAndValue = ((UniversalTagNumber)TagValue).ToString();
            }
            else {
                classAndValue = TagClass + "-" + TagValue;
            }

            if (IsConstructed) {
                return ConstructedPrefix + classAndValue;
            }

            return classAndValue;
        }
    }

    internal static class AsnCharacterStringEncodings {
        private static readonly Encoding kUtf8Encoding = new UTF8Encoding(false, true);
        private static readonly Encoding kBmpEncoding = new BMPEncoding();
        private static readonly Encoding kIa5Encoding = new IA5Encoding();
        private static readonly Encoding kVisibleStringEncoding = new VisibleStringEncoding();
        private static readonly Encoding kPprintableStringEncoding = new PrintableStringEncoding();

        internal static Encoding GetEncoding(UniversalTagNumber encodingType) {
            switch (encodingType) {
                case UniversalTagNumber.UTF8String:
                    return kUtf8Encoding;
                case UniversalTagNumber.PrintableString:
                    return kPprintableStringEncoding;
                case UniversalTagNumber.IA5String:
                    return kIa5Encoding;
                case UniversalTagNumber.VisibleString:
                    return kVisibleStringEncoding;
                case UniversalTagNumber.BMPString:
                    return kBmpEncoding;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingType), encodingType, null);
            }
        }
    }

    internal abstract class SpanBasedEncoding : Encoding {
        protected SpanBasedEncoding()
            : base(0, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback) {
        }

        protected abstract int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write);
        protected abstract int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write);

        public override int GetByteCount(char[] chars, int index, int count) {
            return GetByteCount(new ReadOnlySpan<char>(chars, index, count));
        }

        public override unsafe int GetByteCount(char* chars, int count) {
            return GetByteCount(new ReadOnlySpan<char>(chars, count));
        }

        public override int GetByteCount(string s) {
            return GetByteCount(s.AsSpan());
        }

        public
#if netcoreapp
            override
#endif
        int GetByteCount(ReadOnlySpan<char> chars) {
            return GetBytes(chars, Span<byte>.Empty, false);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
            return GetBytes(
                new ReadOnlySpan<char>(chars, charIndex, charCount),
                new Span<byte>(bytes, byteIndex, bytes.Length - byteIndex),
                true);
        }

        public override unsafe int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) {
            return GetBytes(
                new ReadOnlySpan<char>(chars, charCount),
                new Span<byte>(bytes, byteCount),
                true);
        }

        public override int GetCharCount(byte[] bytes, int index, int count) {
            return GetCharCount(new ReadOnlySpan<byte>(bytes, index, count));
        }

        public override unsafe int GetCharCount(byte* bytes, int count) {
            return GetCharCount(new ReadOnlySpan<byte>(bytes, count));
        }

        public
#if netcoreapp
            override
#endif
        int GetCharCount(ReadOnlySpan<byte> bytes) {
            return GetChars(bytes, Span<char>.Empty, false);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            return GetChars(
                new ReadOnlySpan<byte>(bytes, byteIndex, byteCount),
                new Span<char>(chars, charIndex, chars.Length - charIndex),
                true);
        }

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount) {
            return GetChars(
                new ReadOnlySpan<byte>(bytes, byteCount),
                new Span<char>(chars, charCount),
                true);
        }
    }

    internal class IA5Encoding : RestrictedAsciiStringEncoding {
        // T-REC-X.680-201508 sec 41, Table 8.
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 001
        //   is ASCII 0x00 - 0x1F
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 006
        //   is ASCII 0x21 - 0x7E
        // Space is ASCII 0x20, delete is ASCII 0x7F.
        //
        // The net result is all of 7-bit ASCII
        internal IA5Encoding()
            : base(0x00, 0x7F) {
        }
    }

    internal class VisibleStringEncoding : RestrictedAsciiStringEncoding {
        // T-REC-X.680-201508 sec 41, Table 8.
        // ISO International Register of Coded Character Sets to be used with Escape Sequences 006
        //   is ASCII 0x21 - 0x7E
        // Space is ASCII 0x20.
        internal VisibleStringEncoding()
            : base(0x20, 0x7E) {
        }
    }

    internal class PrintableStringEncoding : RestrictedAsciiStringEncoding {
        // T-REC-X.680-201508 sec 41.4
        internal PrintableStringEncoding()
            : base("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 '()+,-./:=?") {
        }
    }

    internal abstract class RestrictedAsciiStringEncoding : SpanBasedEncoding {
        private readonly bool[] _isAllowed;

        protected RestrictedAsciiStringEncoding(byte minCharAllowed, byte maxCharAllowed) {
            Debug.Assert(minCharAllowed <= maxCharAllowed);
            Debug.Assert(maxCharAllowed <= 0x7F);

            var isAllowed = new bool[0x80];

            for (var charCode = minCharAllowed; charCode <= maxCharAllowed; charCode++) {
                isAllowed[charCode] = true;
            }

            _isAllowed = isAllowed;
        }

        protected RestrictedAsciiStringEncoding(IEnumerable<char> allowedChars) {
            var isAllowed = new bool[0x7F];

            foreach (var c in allowedChars) {
                if (c >= isAllowed.Length) {
                    throw new ArgumentOutOfRangeException(nameof(allowedChars));
                }

                Debug.Assert(isAllowed[c] == false);
                isAllowed[c] = true;
            }

            _isAllowed = isAllowed;
        }

        public override int GetMaxByteCount(int charCount) {
            return charCount;
        }

        public override int GetMaxCharCount(int byteCount) {
            return byteCount;
        }

        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write) {
            if (chars.IsEmpty) {
                return 0;
            }

            for (var i = 0; i < chars.Length; i++) {
                var c = chars[i];

                if (c >= (uint)_isAllowed.Length || !_isAllowed[c]) {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write) {
                    bytes[i] = (byte)c;
                }
            }

            return chars.Length;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write) {
            if (bytes.IsEmpty) {
                return 0;
            }

            for (var i = 0; i < bytes.Length; i++) {
                var b = bytes[i];

                if (b >= (uint)_isAllowed.Length || !_isAllowed[b]) {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        new[] { b },
                        i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write) {
                    chars[i] = (char)b;
                }
            }

            return bytes.Length;
        }
    }

    /// <summary>
    /// Big-Endian UCS-2 encoding (the same as UTF-16BE, but disallowing surrogate pairs to leave plane 0)
    /// </summary>
    // T-REC-X.690-201508 sec 8.23.8 says to see ISO/IEC 10646:2003 section 13.1.
    // ISO/IEC 10646:2003 sec 13.1 says each character is represented by "two octets".
    // ISO/IEC 10646:2003 sec 6.3 says that when serialized as octets to use big endian.
    internal class BMPEncoding : SpanBasedEncoding {
        protected override int GetBytes(ReadOnlySpan<char> chars, Span<byte> bytes, bool write) {
            if (chars.IsEmpty) {
                return 0;
            }

            var writeIdx = 0;

            for (var i = 0; i < chars.Length; i++) {
                var c = chars[i];

                if (char.IsSurrogate(c)) {
                    EncoderFallback.CreateFallbackBuffer().Fallback(c, i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                ushort val16 = c;

                if (write) {
                    bytes[writeIdx + 1] = (byte)val16;
                    bytes[writeIdx] = (byte)(val16 >> 8);
                }

                writeIdx += 2;
            }

            return writeIdx;
        }

        protected override int GetChars(ReadOnlySpan<byte> bytes, Span<char> chars, bool write) {
            if (bytes.IsEmpty) {
                return 0;
            }

            if (bytes.Length % 2 != 0) {
                DecoderFallback.CreateFallbackBuffer().Fallback(
                    bytes.Slice(bytes.Length - 1).ToArray(),
                    bytes.Length - 1);

                Debug.Fail("Fallback should have thrown");
                throw new CryptographicException();
            }

            var writeIdx = 0;

            for (var i = 0; i < bytes.Length; i += 2) {
                var val = (bytes[i] << 8) | bytes[i + 1];
                var c = (char)val;

                if (char.IsSurrogate(c)) {
                    DecoderFallback.CreateFallbackBuffer().Fallback(
                        bytes.Slice(i, 2).ToArray(),
                        i);

                    Debug.Fail("Fallback should have thrown");
                    throw new CryptographicException();
                }

                if (write) {
                    chars[writeIdx] = c;
                }

                writeIdx++;
            }

            return writeIdx;
        }

        public override int GetMaxByteCount(int charCount) {
            checked {
                return charCount * 2;
            }
        }

        public override int GetMaxCharCount(int byteCount) {
            return byteCount / 2;
        }
    }

    internal class SetOfValueComparer : IComparer<ReadOnlyMemory<byte>> {
        internal static SetOfValueComparer Instance { get; } = new SetOfValueComparer();

        public int Compare(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y) {
            var xSpan = x.Span;
            var ySpan = y.Span;

            var min = Math.Min(x.Length, y.Length);
            int diff;

            for (var i = 0; i < min; i++) {
                int xVal = xSpan[i];
                var yVal = ySpan[i];
                diff = xVal - yVal;

                if (diff != 0) {
                    return diff;
                }
            }

            // The sorting rules (T-REC-X.690-201508 sec 11.6) say that the shorter one
            // counts as if it are padded with as many 0x00s on the right as required for
            // comparison.
            //
            // But, since a shorter definite value will have already had the length bytes
            // compared, it was already different.  And a shorter indefinite value will
            // have hit end-of-contents, making it already different.
            //
            // This is here because the spec says it should be, but no values are known
            // which will make diff != 0.
            diff = x.Length - y.Length;

            if (diff != 0) {
                return diff;
            }

            return 0;
        }
    }
}
