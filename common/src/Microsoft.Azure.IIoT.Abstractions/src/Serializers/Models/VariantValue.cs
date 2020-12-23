// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Text;

    /// <summary>
    /// Represents primitive or structurally complex value
    /// </summary>
    public abstract class VariantValue : ICloneable, IConvertible, IComparable {

        /// <summary>
        /// Null constant
        /// </summary>
        public readonly static VariantValue Null =
            new PrimitiveValue(null, VariantValueType.Null);

        /// <inheritdoc/>
        public VariantValue this[string key] {
            get {
                if (!TryGetProperty(key, out var result)) {
                    result = AddProperty(key);
                }
                return result;
            }
        }

        /// <inheritdoc/>
        public VariantValue this[int index] {
            get {
                if (!TryGetElement(index, out var result)) {
                    // Fall back to use array elements
                    result = Values.Skip(index).FirstOrDefault();
                    if (result == null) {
                        throw new IndexOutOfRangeException($"{index} out of range");
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Property names of object
        /// </summary>
        public IEnumerable<string> PropertyNames {
            get {
                return GetObjectProperties();
            }
        }

        /// <summary>
        /// The primitive value
        /// </summary>
        public object Value {
            get {
                if (!TryGetValue(out var v)) {
                    return null;
                }
                return v;
            }
        }

        /// <summary>
        /// Array elements
        /// </summary>
        public IEnumerable<VariantValue> Values {
            get {
                if (TryGetBytes(out var bytes)) {
                    return bytes.Select(b => new PrimitiveValue(b));
                }
                return GetArrayElements();
            }
        }

        /// <summary>
        /// Length of array
        /// </summary>
        public int Count {
            get {
                if (TryGetBytes(out var bytes)) {
                    return bytes.Length;
                }
                return GetArrayCount();
            }
        }

        /// <summary>
        /// Value is a list
        /// </summary>
        public bool IsListOfValues =>
            GetValueType() == VariantValueType.Values;

        /// <summary>
        /// Value is a array - includes bytes
        /// </summary>
        public bool IsArray =>
            GetValueType() == VariantValueType.Values || IsBytes;

        /// <summary>
        /// Value is a object type
        /// </summary>
        public bool IsObject =>
            GetValueType() == VariantValueType.Object;

        /// <summary>
        /// Value is a null type
        /// </summary>
        internal bool IsNull =>
            GetValueType() == VariantValueType.Null;

        /// <summary>
        /// Value is a decimal type
        /// </summary>
        public bool IsDecimal =>
            TryGetDecimal(out _, false);

        /// <summary>
        /// Value is a integer type
        /// </summary>
        public bool IsInteger =>
            TryGetBigInteger(out _, false);

        /// <summary>
        /// Value is a int64 type
        /// </summary>
        public bool IsInt64 =>
            TryGetInt64(out _, false);

        /// <summary>
        /// Value is a uint64 type
        /// </summary>
        public bool IsUInt64 =>
            TryGetInt64(out _, false);

        /// <summary>
        /// Value is a double type
        /// </summary>
        public bool IsDouble =>
            TryGetDouble(out _, false);

        /// <summary>
        /// Value is a float type
        /// </summary>
        public bool IsFloat =>
            TryGetSingle(out _, false);

        /// <summary>
        /// Value is a duration type
        /// </summary>
        public bool IsTimeSpan =>
            TryGetTimeSpan(out _, false);

        /// <summary>
        /// Value is a date type
        /// </summary>
        public bool IsDateTime =>
            TryGetDateTime(out _, false);

        /// <summary>
        /// Value is a Guid type
        /// </summary>
        public bool IsGuid =>
            TryGetGuid(out _, false);

        /// <summary>
        /// Value is a boolean type
        /// </summary>
        public bool IsBoolean =>
            TryGetBoolean(out _, false);

        /// <summary>
        /// Value is a string type
        /// </summary>
        public bool IsString =>
            TryGetString(out _, true);

        /// <summary>
        /// Value is a bytes type
        /// </summary>
        public bool IsBytes =>
            TryGetBytes(out _, false);

        /// <inheritdoc/>
        public virtual TypeCode GetTypeCode() {
            if (VariantValueEx.IsNull(this)) {
                return TypeCode.Empty;
            }
            if (TryGetBoolean(out _, true)) {
                return TypeCode.Boolean;
            }
            if (TryGetSingle(out _, true)) {
                return TypeCode.Single;
            }
            if (TryGetDouble(out _, true)) {
                return TypeCode.Double;
            }
            if (TryGetBytes(out _, true)) {
                return TypeCode.String;
            }
            if (TryGetDecimal(out _, false)) {
                return TypeCode.Decimal;
            }
            if (IsObject) {
                return TypeCode.Object;
            }
            return TypeCode.String;
        }

        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider provider) {
            if (TryGetBoolean(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<bool>();
        }
        /// <inheritdoc/>
        public static explicit operator bool(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToBoolean(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator bool?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (bool?)null :
                value.ToBoolean(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(bool value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(bool? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public byte ToByte(IFormatProvider provider) {
            if (TryGetUInt64(out var value, false, provider)) {
                return (byte)value;
            }
            return ConvertTo<byte>();
        }
        /// <inheritdoc/>
        public static explicit operator byte(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToByte(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator byte?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (byte?)null :
                value.ToByte(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(byte value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(byte? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public char ToChar(IFormatProvider provider) {
            if (TryGetChar(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<char>();
        }
        /// <inheritdoc/>
        public static explicit operator char(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToChar(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator char?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (char?)null :
                value.ToChar(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(char value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(char? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider provider) {
            if (TryGetDateTime(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<DateTime>();
        }
        /// <inheritdoc/>
        public static explicit operator DateTime(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToDateTime(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator DateTime?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (DateTime?)null :
                value.ToDateTime(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(DateTime value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(DateTime? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public DateTimeOffset ToDateTimeOffset(IFormatProvider provider) {
            if (TryGetDateTimeOffset(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<DateTimeOffset>();
        }
        /// <inheritdoc/>
        public static explicit operator DateTimeOffset(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToDateTimeOffset(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator DateTimeOffset?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (DateTimeOffset?)null :
                value.ToDateTimeOffset(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(DateTimeOffset value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(DateTimeOffset? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider provider) {
            if (TryGetDecimal(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<decimal>();
        }
        /// <inheritdoc/>
        public static explicit operator decimal(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToDecimal(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator decimal?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (decimal?)null :
                value.ToDecimal(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(decimal value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(decimal? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public double ToDouble(IFormatProvider provider) {
            if (TryGetDouble(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<double>();
        }
        /// <inheritdoc/>
        public static explicit operator double(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToDouble(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator double?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (double?)null :
                value.ToDouble(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(double value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(double? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public short ToInt16(IFormatProvider provider) {
            if (TryGetInt64(out var value, false, provider)) {
                return (short)value;
            }
            return ConvertTo<short>();
        }
        /// <inheritdoc/>
        public static explicit operator short(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToInt16(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator short?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (short?)null :
                value.ToInt16(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(short value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(short? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public int ToInt32(IFormatProvider provider) {
            if (TryGetInt64(out var value, false, provider)) {
                return (int)value;
            }
            return ConvertTo<int>();
        }
        /// <inheritdoc/>
        public static explicit operator int(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToInt32(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator int?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (int?)null :
                value.ToInt32(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(int value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(int? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public long ToInt64(IFormatProvider provider) {
            if (TryGetInt64(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<long>();
        }
        /// <inheritdoc/>
        public static explicit operator long(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToInt64(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator long?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (long?)null :
                value.ToInt64(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(long value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(long? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider provider) {
            if (TryGetUInt64(out var value, false, provider)) {
                return (ushort)value;
            }
            return ConvertTo<ushort>();
        }
        /// <inheritdoc/>
        public static explicit operator ushort(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToUInt16(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator ushort?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (ushort?)null :
                value.ToUInt16(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(ushort value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(ushort? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider provider) {
            if (TryGetUInt64(out var value, false, provider)) {
                return (uint)value;
            }
            return ConvertTo<uint>();
        }
        /// <inheritdoc/>
        public static explicit operator uint(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToUInt32(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator uint?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (uint?)null :
                value.ToUInt32(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(uint value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(uint? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider provider) {
            if (TryGetUInt64(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<ulong>();
        }
        /// <inheritdoc/>
        public static explicit operator ulong(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToUInt64(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator ulong?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (ulong?)null :
                value.ToUInt64(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(ulong value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(ulong? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider provider) {
            if (TryGetInt64(out var value, false, provider)) {
                return (sbyte)value;
            }
            return ConvertTo<sbyte>();
        }
        /// <inheritdoc/>
        public static explicit operator sbyte(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToSByte(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator sbyte?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (sbyte?)null :
                value.ToSByte(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(sbyte value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(sbyte? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public float ToSingle(IFormatProvider provider) {
            if (TryGetSingle(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<float>();
        }
        /// <inheritdoc/>
        public static explicit operator float(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToSingle(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator float?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (float?)null :
                value.ToSingle(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(float value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(float? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public string ToString(IFormatProvider provider) {
            if (TryGetString(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<string>();
        }
        /// <inheritdoc/>
        public static explicit operator string(VariantValue value) =>
            VariantValueEx.IsNull(value) ? null :
                value.ToString(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(string value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public byte[] ToBytes(IFormatProvider provider) {
            if (TryGetBytes(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<byte[]>();
        }
        /// <inheritdoc/>
        public static explicit operator byte[](VariantValue value) =>
            VariantValueEx.IsNull(value) ? null :
                value.ToBytes(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(byte[] value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public Guid ToGuid(IFormatProvider provider) {
            if (TryGetGuid(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<Guid>();
        }
        /// <inheritdoc/>
        public static explicit operator Guid(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToGuid(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator Guid?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (Guid?)null :
                value.ToGuid(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(Guid value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(Guid? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public TimeSpan ToTimeSpan(IFormatProvider provider) {
            if (TryGetTimeSpan(out var value, false, provider)) {
                return value;
            }
            return ConvertTo<TimeSpan>();
        }
        /// <inheritdoc/>
        public static explicit operator TimeSpan(VariantValue value) =>
            VariantValueEx.IsNull(value) ? throw new NullReferenceException() :
                value.ToTimeSpan(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static explicit operator TimeSpan?(VariantValue value) =>
            VariantValueEx.IsNull(value) ? (TimeSpan?)null :
                value.ToTimeSpan(CultureInfo.InvariantCulture);
        /// <inheritdoc/>
        public static implicit operator VariantValue(TimeSpan value) =>
            new PrimitiveValue(value);
        /// <inheritdoc/>
        public static implicit operator VariantValue(TimeSpan? value) =>
            new PrimitiveValue(value);

        /// <inheritdoc/>
        public virtual object ToType(Type conversionType, IFormatProvider provider) {
            if (GetRawValue() == null || IsNull) {
                if (conversionType.IsValueType) {
                    return Activator.CreateInstance(conversionType);
                }
                return null;
            }
            if (conversionType.IsAssignableFrom(GetRawValue().GetType())) {
                return GetRawValue();
            }
            if (GetRawValue() is IConvertible c) {
                return c.ToType(conversionType,
                    provider ?? CultureInfo.InvariantCulture);
            }
            if (conversionType == typeof(byte)) {
                return ToByte(provider);
            }
            if (conversionType == typeof(float)) {
                return ToSingle(provider);
            }
            if (conversionType == typeof(double)) {
                return ToDouble(provider);
            }
            if (conversionType == typeof(sbyte)) {
                return ToSByte(provider);
            }
            if (conversionType == typeof(short)) {
                return ToInt16(provider);
            }
            if (conversionType == typeof(int)) {
                return ToInt32(provider);
            }
            if (conversionType == typeof(long)) {
                return ToInt64(provider);
            }
            if (conversionType == typeof(ushort)) {
                return ToUInt16(provider);
            }
            if (conversionType == typeof(uint)) {
                return ToUInt32(provider);
            }
            if (conversionType == typeof(ulong)) {
                return ToUInt64(provider);
            }
            if (conversionType == typeof(char)) {
                return ToChar(provider);
            }
            if (conversionType == typeof(string)) {
                return ToString(provider);
            }
            if (conversionType == typeof(Guid)) {
                return ToGuid(provider);
            }
            if (conversionType == typeof(bool)) {
                return ToBoolean(provider);
            }
            if (conversionType == typeof(byte[])) {
                return ToBytes(provider);
            }
            if (conversionType == typeof(decimal)) {
                return ToDecimal(provider);
            }
            if (conversionType == typeof(DateTimeOffset)) {
                return ToDateTimeOffset(provider);
            }
            if (conversionType == typeof(TimeSpan)) {
                return ToTimeSpan(provider);
            }
            if (conversionType == typeof(DateTime)) {
                return ToDateTime(provider);
            }
            return null;
        }

        /// <inheritdoc/>
        public static bool operator ==(VariantValue left, VariantValue right) =>
            Comparer.Equals(left, right);
        /// <inheritdoc/>
        public static bool operator !=(VariantValue left, VariantValue right) =>
            !Comparer.Equals(left, right);
        /// <inheritdoc/>
        public static bool operator >(VariantValue left, VariantValue right) =>
            Comparer.Compare(left, right) > 0;
        /// <inheritdoc/>
        public static bool operator <(VariantValue left, VariantValue right) =>
            Comparer.Compare(left, right) < 0;
        /// <inheritdoc/>
        public static bool operator >=(VariantValue left, VariantValue right) =>
            Comparer.Compare(left, right) >= 0;
        /// <inheritdoc/>
        public static bool operator <=(VariantValue left, VariantValue right) =>
            Comparer.Compare(left, right) <= 0;

        /// <summary>
        /// Equality helper for easier porting
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool DeepEquals(VariantValue x, VariantValue y) {
            return Comparer.Equals(x, y);
        }

        /// <inheritdoc/>
        public override bool Equals(object o) {
            if (o is VariantValue v) {
                return Comparer.Equals(this, v);
            }
            return VariantValueComparer.EqualValues(this, o);
        }

        /// <inheritdoc/>
        public int CompareTo(object o) {
            if (o is VariantValue v) {
                return Comparer.Compare(this, v);
            }
            return VariantValueComparer.CompareValues(this, o);
        }

        /// <inheritdoc/>
        public override string ToString() {
            static void AppendProperty(StringBuilder sb, string k, object p) {
                sb.AppendLine(",");
                sb.Append("    \"");
                sb.Append(k); sb.Append("\": "); sb.Append(p);
            }

            // Append stringified value
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("    \"Value\": ");
            AppendTo(sb);

            var raw = GetRawValue();
            if (raw != null) {
                sb.AppendLine(",");
                // Append raw value as string
                sb.Append("    \"Raw\": \"");
                if (raw is IFormattable f) {
                    sb.Append(f.ToString("G", CultureInfo.InvariantCulture));
                }
                else {
                    sb.Append(raw.ToString());
                }
                sb.Append("\"");
            }

            // Append tests to inspect value recognition
            AppendProperty(sb, nameof(IsObject), IsObject);
            AppendProperty(sb, nameof(IsArray), IsArray);

            if (GetValueType() == VariantValueType.Values) {
                sb.AppendLine(",");
                // Append raw value as string
                sb.Append("    \"Count\": ");
                sb.Append(Count);
            }

            AppendProperty(sb, nameof(IsBoolean), IsBoolean);
            AppendProperty(sb, nameof(IsString), IsString);
            AppendProperty(sb, nameof(IsBytes), IsBytes);
            AppendProperty(sb, nameof(IsDecimal), IsDecimal);
            AppendProperty(sb, nameof(IsDouble), IsDouble);
            AppendProperty(sb, nameof(IsFloat), IsFloat);
            AppendProperty(sb, nameof(IsInt64), IsInt64);
            AppendProperty(sb, nameof(IsUInt64), IsUInt64);
            AppendProperty(sb, nameof(IsInteger), IsInteger);
            AppendProperty(sb, nameof(IsGuid), IsGuid);
            AppendProperty(sb, nameof(IsDateTime), IsDateTime);
            AppendProperty(sb, nameof(IsTimeSpan), IsTimeSpan);
            sb.AppendLine();
            sb.Append("}");

            return sb.ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hc = new HashCode();
            GetDeepHashCode(ref hc);
            return hc.ToHashCode();
        }

        /// <inheritdoc/>
        public object Clone() {
            return Copy();
        }

        /// <summary>
        /// Convert to json
        /// </summary>
        /// <returns></returns>
        public string ToJson() {
            var sb = new StringBuilder();
            AppendTo(sb);
            return sb.ToString();
        }

        /// <summary>
        /// Convert value to typed value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ConvertTo<T>() {
            var typed = ConvertTo(typeof(T));
            return typed == null ? default : (T)typed;
        }

        /// <summary>
        /// Convert value to typed value
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract object ConvertTo(Type type);

        /// <summary>
        /// Update the value to the new value.
        /// </summary>
        /// <param name="value"></param>
        public abstract void AssignValue(object value);

        /// <summary>
        /// Clone this item or entire tree
        /// </summary>
        /// <returns></returns>
        public abstract VariantValue Copy(bool shallow = false);

        /// <summary>
        /// Get value for property
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public virtual bool TryGetProperty(string key, out VariantValue value,
            StringComparison compare = StringComparison.InvariantCultureIgnoreCase) {
            value = null;
            return false;
        }

        /// <summary>
        /// Get value from array index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool TryGetElement(int index, out VariantValue value) {
            value = null;
            return false;
        }

        /// <summary>
        /// Try get primitive value
        /// </summary>
        /// <param name="o"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetValue(out object o,
            IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    o = null;
                    return false;
                case bool _:
                case int _:
                case long _:
                case short _:
                case uint _:
                case ushort _:
                case sbyte _:
                case byte _:
                case char _:
                case ulong _:
                case float _:
                case double _:
                case decimal _:
                case BigInteger _:
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                case Guid _:
                case byte[] _:
                    o = raw;
                    return true;
            }
            // Parse string
            if (TryGetBoolean(out var b, true, provider)) {
                o = b;
                return true;
            }
            if (TryGetUInt64(out var ul, true, provider)) {
                o = ul;
                return true;
            }
            if (TryGetInt64(out var l, true, provider)) {
                o = l;
                return true;
            }
            if (TryGetSingle(out var f, true, provider)) {
                o = f;
                return true;
            }
            if (TryGetDouble(out var d, true, provider)) {
                o = d;
                return true;
            }
            if (TryGetDecimal(out var dec, true, provider)) {
                o = dec;
                return true;
            }
            if (TryGetBigInteger(out var bi, true, provider)) {
                o = bi;
                return true;
            }
            if (TryGetTimeSpan(out var ts, true, provider)) {
                o = ts;
                return true;
            }
            if (TryGetDateTime(out var dt, true, provider)) {
                o = dt;
                return true;
            }
            if (TryGetDateTimeOffset(out var dto, true, provider)) {
                o = dto;
                return true;
            }
            if (TryGetGuid(out var g, true, provider)) {
                o = g;
                return true;
            }
            if (TryGetString(out var s, true, provider)) {
                o = s;
                return true;
            }
            if (TryGetBytes(out var buffer, true, provider)) {
                o = buffer;
                return true;
            }
            o = null;
            return false;
        }

        /// <summary>
        /// Returns double value
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetDouble(out double o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0.0;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                case Guid _:
                    return false;
                case char _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case short _:
                case ushort _:
                case sbyte _:
                case byte _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToDouble(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case decimal _:
                    try {
                        o = Convert.ToDouble(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case BigInteger b:
                    if (strict) {
                        return false;
                    }
                    o = (double)b;
                    return b.Equals(o);
                case float f:
                    o = f;
                    return true;
                case double d:
                    o = d;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            var result = true;
            if (s == kDoubleMinValue) {
                o = double.MinValue;
            }
            else if (s == kDoubleMaxValue) {
                o = double.MaxValue;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.NaNSymbol) {
                o = double.NaN;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol) {
                o = double.PositiveInfinity;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol) {
                o = double.NegativeInfinity;
            }
            else {
                result = double.TryParse(s, NumberStyles.Float, provider, out o);
                // Since .net 3 infinite means overflow
                result = result && !double.IsInfinity(o);
            }
            return result;
        }
        private static readonly string kDoubleMinValue =
            double.MinValue.ToString(CultureInfo.InvariantCulture);
        private static readonly string kDoubleMaxValue =
            double.MaxValue.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns float value
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetSingle(out float o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0.0f;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                case char _:
                    return false;
                case Guid _:
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                    return false;
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case short _:
                case ushort _:
                case sbyte _:
                case byte _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToSingle(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case decimal _:
                    try {
                        o = Convert.ToSingle(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case BigInteger b:
                    if (strict) {
                        return false;
                    }
                    o = (float)b;
                    return b.Equals(o);
                case float f:
                    o = f;
                    return true;
                case double d:
                    if (d > float.MaxValue || d < float.MinValue) {
                        s = d.ToString("G9", provider);
                        break;
                    }
                    o = (float)d;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            var result = true;
                 if (s == kFloatMinValue) {
                o = float.MinValue;
            }
            else if (s == kFloatMaxValue) {
                o = float.MaxValue;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.NaNSymbol) {
                o = float.NaN;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol) {
                o = float.PositiveInfinity;
            }
            else if (s == CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol) {
                o = float.NegativeInfinity;
            }
            else {
                result = float.TryParse(s, NumberStyles.Float, provider, out o);
                // Since .net 3 infinite means overflow
                result = result && !float.IsInfinity(o);
            }
            return result;
        }
        private static readonly string kFloatMinValue =
            float.MinValue.ToString(CultureInfo.InvariantCulture);
        private static readonly string kFloatMaxValue =
            float.MaxValue.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns decimal value
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetDecimal(out decimal o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0m;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                    return false;
                case TimeSpan ts:
                    if (strict) {
                        return false;
                    }
                    o = Convert.ToDecimal(ts.Ticks, provider);
                    return true;
                case DateTime dt:
                    if (strict) {
                        return false;
                    }
                    o = Convert.ToDecimal(dt.Ticks, provider);
                    return true;
                case DateTimeOffset dto:
                    if (strict) {
                        return false;
                    }
                    o = Convert.ToDecimal(dto.UtcTicks, provider);
                    return true;
                case bool _:
                case char _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case short _:
                case ushort _:
                case sbyte _:
                case byte _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToDecimal(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case byte[] buf:
                    if (strict) {
                        return false;
                    }
                    try {
                        var b = new BigInteger(buf);
                        o = (decimal)b;
                        return true;
                    }
                    catch {
                        return false;
                    }
                case BigInteger b:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = (decimal)b;
                        return true;
                    }
                    catch {
                        return false;
                    }
                case float _:
                case double _:
                    try {
                        o = Convert.ToDecimal(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case decimal dec:
                    o = dec;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable f ? f.ToString("G", provider) : raw.ToString();
                    break;
            }
            return decimal.TryParse(s, strict ? NumberStyles.Float : NumberStyles.Any,
                provider, out o);
        }

        /// <summary>
        /// Returns signed signed integer
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetInt64(out long o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0L;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                case char _:
                    return false;
                case TimeSpan ts:
                    if (strict) {
                        return false;
                    }
                    o = ts.Ticks;
                    return true;
                case DateTime dt:
                    if (strict) {
                        return false;
                    }
                    o = dt.Ticks;
                    return true;
                case DateTimeOffset dto:
                    if (strict) {
                        return false;
                    }
                    o = dto.UtcTicks;
                    return true;
                case int _:
                case long _:
                case short _:
                case uint _:
                case ushort _:
                case sbyte _:
                case byte _:
                    o = Convert.ToInt64(raw, provider);
                    return true;
                case ulong v:
                    if (v > long.MaxValue) {
                        return false;
                    }
                    o = (long)v;
                    return true;
                case float _:
                case double _:
                case decimal _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToInt64(raw, provider);
                        return o.Equals(raw);
                    }
                    catch {
                        return false;
                    }
                case byte[] buf:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = (long)new BigInteger(buf);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case BigInteger b:
                    try {
                        o = (long)b;
                        return true;
                    }
                    catch {
                        return false;
                    }
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable f ?
                        f.ToString("G", provider) : raw.ToString();
                    break;
            }
            return long.TryParse(s,
                NumberStyles.Integer, provider, out o);
        }

        /// <summary>
        /// Returns signed signed integer
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetUInt64(out ulong o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0UL;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                case char _:
                    return false;
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                    return false;
                case int _:
                case long _:
                case short _:
                case sbyte _:
                    var signed = Convert.ToInt64(raw, provider);
                    if (signed < 0) {
                        return false;
                    }
                    o = Convert.ToUInt64(raw, provider);
                    return true;
                case uint _:
                case ushort _:
                case byte _:
                    o = Convert.ToUInt64(raw, provider);
                    return true;
                case ulong v:
                    o = v;
                    return true;
                case float _:
                case double _:
                case decimal _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToUInt64(raw, provider);
                        return o.Equals(raw);
                    }
                    catch {
                        return false;
                    }
                case byte[] buf:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = (ulong)new BigInteger(buf);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case BigInteger b:
                    try {
                        o = (ulong)b;
                        return true;
                    }
                    catch {
                        return false;
                    }
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable f ?
                        f.ToString("G", provider) : raw.ToString();
                    break;
            }
            return ulong.TryParse(s, NumberStyles.Integer, provider, out o);
        }

        /// <summary>
        /// Returns byte
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetByte(out byte o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = 0;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                case char _:
                    return false;
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                    return false;
                case int _:
                case long _:
                case short _:
                case sbyte _:
                case uint _:
                case ushort _:
                case ulong _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToByte(raw, provider);
                        return o.Equals(raw);
                    }
                    catch {
                        return false;
                    }

                case byte b:
                    o = b;
                    return true;
                case float _:
                case double _:
                case decimal _:
                case BigInteger b:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = (byte)b;
                        return true;
                    }
                    catch {
                        return false;
                    }
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable f ?
                        f.ToString("G", provider) : raw.ToString();
                    break;
            }
            return byte.TryParse(s, NumberStyles.Integer, provider, out o);
        }

        /// <summary>
        /// Returns character
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetChar(out char o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = char.MinValue;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                    return false;
                case char c:
                    o = c;
                    return true;
                case int _:
                case long _:
                case short _:
                case sbyte _:
                case uint _:
                case ushort _:
                case byte _:
                case ulong _:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = Convert.ToChar(raw, provider);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case float _:
                case double _:
                case decimal _:
                    return false;
                case BigInteger b:
                    return false;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable f ?
                        f.ToString("G", provider) : raw.ToString();
                    break;
            }
            if (char.TryParse(s, out o)) {
                return true;
            }
            if (s.Length == 1) {
                o = s[0];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get value as integer
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetBigInteger(out BigInteger o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = BigInteger.Zero;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                case char _:
                    return false;
                case TimeSpan _:
                case DateTime _:
                case DateTimeOffset _:
                    return false;
                case uint _:
                case ulong _:
                case ushort _:
                case byte _:
                    o = new BigInteger(Convert.ToUInt64(raw, provider));
                    return true;
                case int _:
                case long _:
                case short _:
                case sbyte _:
                    o = new BigInteger(Convert.ToInt64(raw, provider));
                    return true;
                case BigInteger b:
                    o = b;
                    return true;
                case byte[] buf:
                    if (strict) {
                        return false;
                    }
                    o = new BigInteger(buf);
                    return true;
                case decimal dec:
                    if (strict) {
                        return false;
                    }
                    o = new BigInteger(dec);
                    return decimal.Floor(dec).Equals(dec);
                case float f:
                    if (strict) {
                        return false;
                    }
                    if (float.IsNaN(f) || float.IsInfinity(f)) {
                        return false;
                    }
                    o = new BigInteger(f);
                    return Math.Floor(f).Equals(f);
                case double d:
                    if (strict) {
                        return false;
                    }
                    if (double.IsNaN(d) || double.IsInfinity(d)) {
                        return false;
                    }
                    o = new BigInteger(d);
                    return Math.Floor(d).Equals(d);
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return BigInteger.TryParse(s, NumberStyles.Integer,
                provider, out o);
        }

        /// <summary>
        /// Get value as timespan
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetTimeSpan(out TimeSpan o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = TimeSpan.MinValue;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                    return false;
                case long l:
                    if (strict) {
                        return false;
                    }
                    o = TimeSpan.FromTicks(l);
                    return true;
                case TimeSpan ts:
                    o = ts;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return TimeSpan.TryParse(s, provider, out o);
        }

        /// <summary>
        /// Get value as date
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected virtual bool TryGetDateTime(out DateTime o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = DateTime.MinValue;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                    return false;
                case DateTime dt:
                    o = dt;
                    return true;
                case long l:
                    if (strict) {
                        return false;
                    }
                    try {
                        o = DateTime.FromBinary(l);
                        return true;
                    }
                    catch {
                        return false;
                    }
                case DateTimeOffset dto:
                    o = dto.UtcDateTime;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return DateTime.TryParse(s, provider,
                DateTimeStyles.AdjustToUniversal, out o);
        }

        /// <summary>
        /// Get value as date time offset
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetDateTimeOffset(out DateTimeOffset o,
            bool strict = true, IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = DateTimeOffset.MinValue;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                    return false;
                case DateTime dt:
                    if (strict) {
                        return false;
                    }
                    o = dt;
                    return true;
                case long l:
                    if (strict) {
                        return false;
                    }
                    o = DateTimeOffset.FromUnixTimeMilliseconds(l);
                    return true;
                case DateTimeOffset dto:
                    o = dto;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return DateTimeOffset.TryParse(s, provider,
                DateTimeStyles.AdjustToUniversal, out o);
        }

        /// <summary>
        /// Get value as boolean
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetBoolean(out bool o, bool strict = true,
            IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = false;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid _:
                    return false;
                case bool b:
                    o = b;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return bool.TryParse(s, out o);
        }

        /// <summary>
        /// Get Value as guid
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetGuid(out Guid o, bool strict = true,
            IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = Guid.Empty;
            string s;
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid g:
                    o = g;
                    return true;
                case string str:
                    s = str;
                    break;
                default:
                    if (GetValueType() != VariantValueType.Primitive) {
                        return false;
                    }
                    s = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    break;
            }
            return Guid.TryParse(s, out o);
        }

        /// <summary>
        /// Get Value as a bytes type
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetBytes(out byte[] o, bool strict = true,
            IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = null;
            if (GetValueType() == VariantValueType.Values) {
                // Convert array to bytes
                var buffer = new List<byte>();
                foreach (var item in GetArrayElements()) {
                    if (!item.TryGetByte(out var b)) {
                        return false;
                    }
                    buffer.Add(b);
                }
                o = buffer.ToArray();
                return true;
            }
            if (GetValueType() == VariantValueType.Object) {
                return false;
            }
            var raw = GetRawValue();
            switch (raw) {
                case null:
                    return false;
                case Guid g:
                    if (strict) {
                        return false;
                    }
                    o = g.ToByteArray();
                    return true;
                case byte[] b:
                    o = b;
                    return true;
                case string s:
                    if (strict && s.Length == 0) {
                        return false;
                    }
                    try {
                        o = Convert.FromBase64String(s);
                        return true;
                    }
                    catch {
                        return false;
                    }
                default:
                    // Must be string or override
                    return false;
            }
        }

        /// <summary>
        /// Value is a string type
        /// </summary>
        /// <param name="o"></param>
        /// <param name="strict"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public virtual bool TryGetString(out string o, bool strict = true,
            IFormatProvider provider = null) {
            provider ??= CultureInfo.InvariantCulture;
            o = null;
            if (GetValueType() != VariantValueType.Primitive) {
                return false;
            }
            var raw = GetRawValue();
            switch (raw) {
                case string s:
                    o = s.ToString(provider);
                    return true;
                case Guid g:
                    if (strict) {
                        return false;
                    }
                    o = g.ToString();
                    return true;
                default:
                    if (strict) {
                        return false;
                    }
                    o = raw is IFormattable fmt ?
                        fmt.ToString("G", provider) : raw.ToString();
                    return true;
            }
        }

        /// <summary>
        /// Select value using path. Path can be either . seperated
        /// property names or contain index [] for array elements.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="compare"></param>
        /// <returns></returns>
        public virtual VariantValue GetByPath(string path,
            StringComparison compare = StringComparison.InvariantCultureIgnoreCase) {
            var leaf = this;
            var elements = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            foreach (var elem in elements) {
                var key = elem;
                var index = -1;
                var offset = elem.Split('[', StringSplitOptions.RemoveEmptyEntries);
                if (offset.Length > 1) {
                    var end = offset[1].IndexOf(']');
                    if (end != -1 &&
                        int.TryParse(offset[1].Substring(0, end), out index)) {
                        key = offset[0];
                    }
                    else {
                        index = -1;
                    }
                }
                if (!leaf.TryGetProperty(key, out leaf, compare)) {
                    return Null;
                }
                if (index != -1) {
                    if (!leaf.TryGetElement(index, out leaf)) {
                        return Null;
                    }
                }
            }
            return leaf;
        }

        /// <summary>
        /// Equality comparer
        /// </summary>
        public static VariantValueComparer Comparer =>
            new VariantValueComparer();

        /// <inheritdoc/>
        public class VariantValueComparer : IEqualityComparer<VariantValue>,
            IComparer<VariantValue> {

            /// <inheritdoc/>
            public bool Equals(VariantValue x, VariantValue y) {
                if (ReferenceEquals(x, y)) {
                    return true;
                }

                var yt = y?.GetValueType() ?? VariantValueType.Null;
                var xt = x?.GetValueType() ?? VariantValueType.Null;

                if (yt != xt) {
                    if (xt == VariantValueType.Null || yt == VariantValueType.Null) {
                        return false;
                    }
                    // Special case
                    if (xt == VariantValueType.Primitive || yt == VariantValueType.Primitive) {
                        if (xt == VariantValueType.Values || yt == VariantValueType.Values) {
                            // Compare as bytes
                            if (x.TryGetBytes(out var bufx) && y.TryGetBytes(out var bufy) &&
                                bufx.AsSpan().SequenceEqual(bufy)) {
                                return true;
                            }
                        }

                        // Values or object compare to string
                        if (xt == VariantValueType.Primitive && x.TryGetString(out var sx)) {
                            return y.ToJson() == sx;
                        }
                        if (yt == VariantValueType.Primitive && y.TryGetString(out var sy)) {
                            return x.ToJson() == sy;
                        }
                    }
                    return false;
                }

                if (xt == VariantValueType.Null) {
                    // If both null then they are the same
                    return true;
                }

                // Perform structural comparison
                if (xt == VariantValueType.Values) {
                    if (x.GetArrayElements().SequenceEqual(y.GetArrayElements(), Comparer)) {
                        return true;
                    }
                    return false;
                }

                if (xt == VariantValueType.Object) {
                    var px = x.PropertyNames.OrderBy(k => k).Select(k => x[k]);
                    var py = y.PropertyNames.OrderBy(k => k).Select(k => y[k]);
                    if (px.SequenceEqual(py, Comparer)) {
                        return true;
                    }
                    return false;
                }

                // Allow implementation to perform comparison first
                if (x.TryEqualsVariant(y, out var result) ||
                    y.TryEqualsVariant(x, out result)) {
                    if (result) {
                        return true;
                    }
                    return false;
                }

                // Compare floating point values
                if (x.TryGetSingle(out var fx) &&
                    y.TryGetSingle(out var fy) &&
                    fx == fy) {
                    return true;
                }
                if (x.TryGetDouble(out var dx) &&
                    y.TryGetDouble(out var dy) &&
                    dx == dy) {
                    return true;
                }

                // Compare numbers - includes dates and times
                if (x.TryGetDecimal(out var nx, false) &&
                    y.TryGetDecimal(out var ny, false) &&
                    nx == ny) {
                    return true;
                }

                // Compare bytes - includes empty strings
                if (x.TryGetBytes(out var bx, false) &&
                    y.TryGetBytes(out var by, false) &&
                    bx.AsSpan().SequenceEqual(by)) {
                    return true;
                }

                // Compare values
                if (y.TryGetValue(out var yv) && EqualValues(x, yv)) {
                    return true;
                }
                if (x.TryGetValue(out var xv) && EqualValues(y, xv)) {
                    return true;
                }
                return false;
            }

            /// <inheritdoc/>
            public int Compare(VariantValue x, VariantValue y) {

                var yt = y?.GetValueType() ?? VariantValueType.Null;
                var xt = x?.GetValueType() ?? VariantValueType.Null;

                if (yt != xt) {
                    if (xt != VariantValueType.Null && yt != VariantValueType.Null) {
                        // Special case compare
                        if (xt == VariantValueType.Primitive || yt == VariantValueType.Primitive) {
                            if (xt == VariantValueType.Values || yt == VariantValueType.Values) {
                                // Compare as bytes
                                if (x.TryGetBytes(out var bufx) && y.TryGetBytes(out var bufy)) {
                                    return Convert.ToBase64String(bufx)
                                        .CompareTo(Convert.ToBase64String(bufy));
                                }
                            }

                            // Values or object compare to string
                            if (xt == VariantValueType.Primitive && x.TryGetString(out var sx)) {
                                return sx.CompareTo(y.ToJson());
                            }
                            if (yt == VariantValueType.Primitive && y.TryGetString(out var sy)) {
                                return x.ToJson().CompareTo(sy);
                            }
                        }
                    }
                    return xt.CompareTo(yt);
                }

                // First compare values to see if they are the same
                if (Equals(x, y)) {
                    return 0;
                }

                // Allow implementation to perform comparison first
                if (x.TryCompareToValue(y, out var result)) {
                    return result < 0 ? -1 : result > 0 ? 1 : 0;
                }
                if (y.TryCompareToValue(x, out result)) {
                    return result > 0 ? -1 : result < 0 ? 1 : 0;
                }

                // Perform primitive value comparison
                if (xt == VariantValueType.Primitive) {

                    if (x.TryGetSingle(out var fx) &&
                        y.TryGetSingle(out var fy)) {
                        return fx.CompareTo(fy);
                    }
                    if (x.TryGetDouble(out var dx) &&
                        y.TryGetDouble(out var dy)) {
                        return dx.CompareTo(dy);
                    }
                    if (x.TryGetDecimal(out var decx, false) &&
                        y.TryGetDecimal(out var decy, false)) {
                        return decx.CompareTo(decy);
                    }
                    if (x.TryGetBigInteger(out var bix) &&
                        y.TryGetBigInteger(out var biy)) {
                        return bix.CompareTo(biy);
                    }
                    if (x.TryGetTimeSpan(out var tx) &&
                        y.TryGetTimeSpan(out var ty)) {
                        return tx.CompareTo(ty);
                    }
                    if (x.TryGetDateTimeOffset(out var dtox) &&
                        y.TryGetDateTimeOffset(out var dtoy)) {
                        return dtox.CompareTo(dtoy);
                    }
                    if (x.TryGetDateTime(out var dtx) &&
                        y.TryGetDateTime(out var dty)) {
                        return dtx.CompareTo(dty);
                    }
                    if (x.TryGetGuid(out var gx) &&
                        y.TryGetGuid(out var gy)) {
                        return gx.CompareTo(gy);
                    }
                    if (x.TryGetString(out var sx) &&
                        y.TryGetString(out var sy)) {
                        return sx.CompareTo(sy);
                    }
                }

                // Use string comparison
                var osx = x.ToJson().ToLowerInvariant();
                var osy = y.ToJson().ToLowerInvariant();

                return osx.CompareTo(osy);
            }

            /// <inheritdoc/>
            public int GetHashCode(VariantValue v) {
                return v?.GetHashCode() ?? 0;
            }

            /// <summary>
            /// Tries to compare equality of 2 values using convertible
            /// and comparable interfaces.
            /// </summary>
            /// <param name="v"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            internal static bool EqualValues(VariantValue v, object y) {

                // Allow implementation to perform comparison first
                if (v.TryEqualsValue(y, out var equality)) {
                    return equality;
                }

                if (v.GetValueType() != VariantValueType.Primitive) {
                    if (y is string s) {
                        return v.ToJson() == s;
                    }
                    if (y is byte[] boy && v.GetValueType() == VariantValueType.Values) {
                        if (v.TryGetBytes(out var box)) {
                            return box.AsSpan().SequenceEqual(boy);
                        }
                    }
                }

                if (!v.TryGetValue(out var x)) {
                    try {
                        x = v.ConvertTo(y.GetType());
                    }
                    catch {
                        return false;
                    }
                }

                if (ReferenceEquals(x, y)) {
                    return true;
                }

                if (x == null || y == null) {
                    return false;
                }

                if (y.Equals(x) || x.Equals(y)) {
                    return true;
                }

                if (x is byte[] bx && y is byte[] by) {
                    return bx.AsSpan().SequenceEqual(by);
                }

                if (x is IConvertible co1) {
                    try {
                        var compare = co1.ToType(y.GetType(),
                            CultureInfo.InvariantCulture);
                        if (!compare.Equals(y)) {
                            return false;
                        }
                        return true;
                    }
                    catch {
                    }
                }
                if (y is IConvertible co2) {
                    try {
                        var compare = co2.ToType(x.GetType(),
                            CultureInfo.InvariantCulture);
                        if (!compare.Equals(x)) {
                            return false;
                        }
                        return true;
                    }
                    catch {
                    }
                }

                // Compare values through comparison operation
                return TryCompare(v, y, out var result, false) && result == 0;
            }

            /// <summary>
            /// Compare value
            /// </summary>
            /// <param name="v"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            internal static int CompareValues(VariantValue v, object y) {
                if (TryCompare(v, y, out var result, true)) {
                    return result;
                }
                return -1;
            }

            /// <summary>
            /// Compare
            /// </summary>
            /// <param name="v"></param>
            /// <param name="y"></param>
            /// <param name="result"></param>
            /// <param name="stringCompare"></param>
            /// <returns></returns>
            private static bool TryCompare(VariantValue v, object y,
                out int result, bool stringCompare = false) {

                // Allow implementation to perform comparison first
                if (v.TryCompareToValue(y, out result)) {
                    return true;
                }

                if (v.GetValueType() != VariantValueType.Primitive) {
                    if (y is string s) {
                        result = v.ToJson().CompareTo(s);
                        return true;
                    }
                    if (y is byte[] boy && v.GetValueType() == VariantValueType.Values) {
                        if (v.TryGetBytes(out var box)) {
                            var box64 = Convert.ToBase64String(box);
                            var boy64 = Convert.ToBase64String(boy);
                            result = box64.CompareTo(boy64);
                            return true;
                        }
                    }
                }

                if (!v.TryGetValue(out var x)) {
                    // Throw if needed
                    x = v.ConvertTo(y.GetType());
                }

                if (TryCompare(x, y, out result)) {
                    result = result < 0 ? -1 : result > 0 ? 1 : 0;
                    return true;
                }
                if (TryCompare(y, x, out result)) {
                    result = result > 0 ? -1 : result < 0 ? 1 : 0;
                    return true;
                }

                if (!stringCompare) {
                    result = -1;
                    return false;
                }

                // Compare stringified version
                var s1 = x?.ToString() ?? "null";
                var s2 = y?.ToString() ?? "null";
                result = s1.CompareTo(s2);
                return true;
            }

            /// <summary>
            /// Compare
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="result"></param>
            /// <returns></returns>
            private static bool TryCompare(object x, object y, out int result) {
                if (x is IComparable cv1) {
                    try {
                        if (x.GetType() != y.GetType()) {
                            if (y is IConvertible c) {
                                y = c.ToType(x.GetType(), CultureInfo.InvariantCulture);
                            }
                        }
                        result = cv1.CompareTo(y);
                        return true;
                    }
                    catch {
                    }
                }
                result = -1;
                return false;
            }
        }

        /// <summary>
        /// Represents a primitive value for assignment purposes
        /// </summary>
        internal sealed class PrimitiveValue : VariantValue {

            /// <inheritdoc/>
            protected override VariantValueType GetValueType() {
                return _valueType;
            }

            /// <inheritdoc/>
            protected override IEnumerable<string> GetObjectProperties() {
                return Enumerable.Empty<string>();
            }

            /// <inheritdoc/>
            protected override object GetRawValue() {
                return _rawValue;
            }

            /// <inheritdoc/>
            protected override IEnumerable<VariantValue> GetArrayElements() {
                return Enumerable.Empty<VariantValue>();
            }

            /// <inheritdoc/>
            protected override int GetArrayCount() {
                return 0;
            }

            /// <summary>
            /// Clone
            /// </summary>
            /// <param name="value"></param>
            /// <param name="type"></param>
            internal PrimitiveValue(object value, VariantValueType type) {
                _rawValue = value;
                _valueType = value == null ? VariantValueType.Null : type;
            }

            /// <inheritdoc/>
            public PrimitiveValue(string value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(byte[] value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(bool value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(byte value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(sbyte value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(short value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(ushort value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(int value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(uint value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(long value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(ulong value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(float value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(double value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(decimal value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(Guid value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(DateTime value) :
                this(value.Kind == DateTimeKind.Local ?
                    value.ToUniversalTime() : value,
                    VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(DateTimeOffset value) :
                this(value.Offset != TimeSpan.Zero ?
                    value.ToUniversalTime() : value,
                    VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(TimeSpan value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(bool? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(byte? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(sbyte? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(short? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(ushort? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(int? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(uint? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(long? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(ulong? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(float? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(double? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(decimal? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(Guid? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(DateTime? value) :
                this(!value.HasValue ? value :
                    value.Value.Kind == DateTimeKind.Local ?
                    value.Value.ToUniversalTime() : value.Value,
                    VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(DateTimeOffset? value) :
                this(!value.HasValue ? value :
                    value.Value.Offset != TimeSpan.Zero ?
                    value.Value.ToUniversalTime() : value.Value,
                    VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public PrimitiveValue(TimeSpan? value) :
                this(value, VariantValueType.Primitive) {
            }

            /// <inheritdoc/>
            public override VariantValue Copy(bool shallow = false) {
                return new PrimitiveValue(_rawValue, GetValueType());
            }

            /// <inheritdoc/>
            public override object ConvertTo(Type conversionType) {
                if (_rawValue == null || IsNull) {
                    if (conversionType.IsValueType) {
                        return Activator.CreateInstance(conversionType);
                    }
                    return null;
                }
                if (conversionType.IsAssignableFrom(_rawValue.GetType())) {
                    return _rawValue;
                }
                if (_rawValue is IConvertible c) {
                    return c.ToType(conversionType, CultureInfo.InvariantCulture);
                }
                var converter = TypeDescriptor.GetConverter(conversionType);
                return converter.ConvertFrom(_rawValue);
            }

            /// <inheritdoc/>
            public override VariantValue GetByPath(string path,
                StringComparison compare) {
                return Null;
            }

            /// <inheritdoc/>
            public override void AssignValue(object value) {
                throw new NotSupportedException("Not an object");
            }

            /// <inheritdoc/>
            protected override VariantValue AddProperty(string property) {
                throw new NotSupportedException("Not an object");
            }

            private readonly VariantValueType _valueType;
            private readonly object _rawValue;
        }

        /// <summary>
        /// Create value which is set to null.
        /// </summary>
        /// <returns></returns>
        protected abstract VariantValue AddProperty(string property);

        /// <summary>
        /// Get type of value
        /// </summary>
        /// <returns></returns>
        protected abstract VariantValueType GetValueType();

        /// <summary>
        /// Provide raw value or null
        /// </summary>
        /// <returns></returns>
        protected abstract object GetRawValue();

        /// <summary>
        /// Values of array
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<VariantValue> GetArrayElements();

        /// <summary>
        /// Length of array
        /// </summary>
        /// <returns></returns>
        protected abstract int GetArrayCount();

        /// <summary>
        /// Property names of object
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<string> GetObjectProperties();

        /// <summary>
        /// Compare to a non variant value object, e.g. the value of
        /// another variant.  This can be overridden.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="equality"></param>
        /// <returns></returns>
        protected virtual bool TryEqualsValue(object o, out bool equality) {
            equality = false;
            return false;
        }

        /// <summary>
        /// Try to compare equality with another variant.
        /// The implementation should return false if comparison
        /// is not possible and must not call equality test
        /// with value itself.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="equality"></param>
        /// <returns></returns>
        protected virtual bool TryEqualsVariant(VariantValue v, out bool equality) {
            equality = false;
            return false;
        }

        /// <summary>
        /// Compare value
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual bool TryCompareToValue(object obj, out int result) {
            result = 0;
            return false;
        }

        /// <summary>
        /// Compare variant value
        /// </summary>
        /// <param name="v"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual bool TryCompareToVariantValue(VariantValue v,
            out int result) {
            result = 0;
            return false;
        }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <returns></returns>
        protected virtual void AppendTo(StringBuilder builder) {
            string s;
            switch (GetValueType()) {
                case VariantValueType.Null:
                    builder.Append("null");
                    return;
                case VariantValueType.Values:
                    var first = true;
                    builder.Append('[');
                    foreach (var value in GetArrayElements()) {
                        if (!first) {
                            builder.Append(',');
                        }
                        else {
                            first = false;
                        }
                        value.AppendTo(builder);
                    }
                    builder.Append(']');
                    return;
                case VariantValueType.Object:
                    var open = true;
                    builder.Append('{');
                    var p2 = PropertyNames.OrderBy(k => k);
                    foreach (var k in p2) {
                        if (!open) {
                            builder.Append(',');
                        }
                        else {
                            open = false;
                        }
                        builder.Append(k);
                        builder.Append(':');
                        this[k].AppendTo(builder);
                    }
                    builder.Append('}');
                    return;
            }
            if (!TryGetValue(out var raw)) {
                raw = GetRawValue();
            }
            switch (raw) {
                case string str:
                    s = str;
                    break;
                case byte[] b:
                    s = Convert.ToBase64String(b);
                    break;
                case Guid g:
                    s = g.ToString();
                    break;
                case DateTime dt:
                    s = dt.ToString("O", CultureInfo.InvariantCulture);
                    break;
                case DateTimeOffset dto:
                    s = dto.ToString("O", CultureInfo.InvariantCulture);
                    break;
                case TimeSpan ts:
                    s = ts.ToString("c", CultureInfo.InvariantCulture);
                    break;
                case BigInteger bi:
                    builder.Append(bi.ToString("R", CultureInfo.InvariantCulture));
                    return;
                case decimal d:
                    builder.Append(d.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case double d:
                    s = d.ToString("G17", CultureInfo.InvariantCulture);
                    if (double.IsNaN(d) || double.IsInfinity(d)) {
                        break;
                    }
                    builder.Append(s);
                    return;
                case float f:
                    s = f.ToString("G9", CultureInfo.InvariantCulture);
                    if (float.IsNaN(f) || float.IsInfinity(f)) {
                        break;
                    }
                    builder.Append(s);
                    return;
                case int i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case long i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case short i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case uint i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case ushort i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case sbyte i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case byte i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case ulong i:
                    builder.Append(i.ToString("G", CultureInfo.InvariantCulture));
                    return;
                case bool _:
                    builder.Append(GetRawValue().ToString().ToLowerInvariant());
                    return;
                case char _:
                default:
                    s = raw.ToString();
                    break;
            }
            builder.Append('"');
            builder.Append(s);
            builder.Append('"');
        }

        /// <summary>
        /// Create hash code for this or entire tree.
        /// </summary>
        /// <returns></returns>
        private void GetDeepHashCode(ref HashCode hc) {
            switch (GetValueType()) {
                case VariantValueType.Null:
                    hc.Add(GetValueType());
                    break;
                case VariantValueType.Primitive:
                    if (!TryGetValue(out var o)) {
                        o = GetRawValue();
                    }
                    if (o is byte[] b) {
                        o = Convert.ToBase64String(b);
                    }
                    hc.Add(o);
                    break;
                case VariantValueType.Values:
                    foreach (var value in GetArrayElements()) {
                        value.GetDeepHashCode(ref hc);
                    }
                    break;
                case VariantValueType.Object:
                    var p2 = PropertyNames.OrderBy(k => k);
                    foreach (var k in p2) {
                        hc.Add(k);
                        this[k].GetDeepHashCode(ref hc);
                    }
                    break;
                default:
                    hc.Add(GetRawValue());
                    break;
            }
        }
    }
}