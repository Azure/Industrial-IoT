// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml;

    // The converters below are intentionally reflection based to preserve the
    // data contract wire format. AOT / trim hardening is a later migration phase.
#pragma warning disable IL2026, IL2055, IL2070, IL2071, IL2072, IL2075, IL2090, IL3050
#pragma warning disable CA1852

    /// <summary>
    /// System.Text.Json converters ported from the former Furly default json
    /// serializer so that the wire format (in particular <see cref="DataContractAttribute"/>
    /// / <see cref="DataMemberAttribute"/> based property naming and ordering used by the
    /// API models) is preserved. These converters are reflection based and therefore
    /// not Native-AOT / trim safe; they are only reachable from the reflection based
    /// <see cref="Json"/> helper whose entry points are annotated accordingly. AOT
    /// hardening of the serialization pipeline is a later migration phase.
    /// </summary>
    internal static class JsonConverters
    {
        /// <summary>
        /// Get generic interface (ported from Furly TypeEx).
        /// </summary>
        /// <param name="type"></param>
        /// <param name="genericItfType"></param>
        /// <exception cref="ArgumentException"></exception>
        public static Type? GetCompatibleGenericInterface(this Type type,
            Type genericItfType)
        {
            if (!genericItfType.IsGenericType ||
                !genericItfType.IsInterface ||
                genericItfType != genericItfType.GetGenericTypeDefinition())
            {
                throw new ArgumentException(
                    "Argument must be a generic interface type" +
                    $" which {genericItfType.Name} is not.");
            }
            var check = type;
            if (check.IsGenericType)
            {
                check = check.GetGenericTypeDefinition();
            }
            if (check == genericItfType)
            {
                return type;
            }
            foreach (var itfOfType in type.GetInterfaces())
            {
                if (itfOfType.IsGenericType)
                {
                    var genericItf = itfOfType.GetGenericTypeDefinition();
                    if (genericItf == genericItfType)
                    {
                        return itfOfType;
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Converts objects with <see cref="DataContractAttribute"/> honoring the
    /// <see cref="DataMemberAttribute"/> name / emit default semantics.
    /// </summary>
    internal sealed class DataContractObjectConverter : JsonConverterFactory
    {
        /// <inheritdoc/>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override bool CanConvert(Type typeToConvert)
        {
            var dca = typeToConvert.GetCustomAttribute<DataContractAttribute>(true);
            if (dca == null)
            {
                return false;
            }
            var constructors = typeToConvert.GetConstructors();
            if (constructors.Length != 0 && !constructors
                .Any(c => c.GetParameters().Length == 0))
            {
                // No support for parameter based construction at this point.
                return false;
            }
            // If data member attribute is being used
            return typeToConvert.GetProperties()
                .Any(p => p.CanWrite && !p.IsSpecialName &&
                    p.GetCustomAttribute<DataMemberAttribute>() != null);
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2072",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override JsonConverter? CreateConverter(Type typeToConvert,
            JsonSerializerOptions options)
        {
            var ct = typeof(DataContractObjectConverterOfT<>)
                .MakeGenericType(typeToConvert);
            return (JsonConverter?)Activator.CreateInstance(ct, []);
        }

        /// <summary>
        /// Actual converter of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public class DataContractObjectConverterOfT<T> : JsonConverter<T>
            where T : new()
        {
            /// <inheritdoc/>
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }
                var o = new T();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return o;
                    }
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        if (propertyName == null)
                        {
                            throw new JsonException();
                        }
                        reader.Read();
                        ReadFn? setter;
                        if (options.PropertyNameCaseInsensitive)
                        {
                            if (!kReadersInsensitive.TryGetValue(
                                propertyName.ToUpperInvariant(), out setter))
                            {
                                throw new JsonException(
                                   $"No case insensitive reader for {propertyName}");
                            }
                        }
                        else if (!kReaders.TryGetValue(propertyName, out setter))
                        {
                            throw new JsonException($"No reader for {propertyName}");
                        }
                        setter(ref reader, o, options);
                    }
                }
                return o;
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T value,
                JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                foreach (var write in kWriters)
                {
                    write(value, writer, options);
                }
                writer.WriteEndObject();
            }

            private delegate void WriteFn(object? o, Utf8JsonWriter writer,
                JsonSerializerOptions options);

            private delegate void ReadFn(ref Utf8JsonReader reader, object? o,
                JsonSerializerOptions options);

            /// <summary>
            /// Gather type information
            /// </summary>
            [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
                Justification = "Reflection based serializer, hardened in a later phase.")]
            [UnconditionalSuppressMessage("Trimming", "IL2026",
                Justification = "Reflection based serializer, hardened in a later phase.")]
            [UnconditionalSuppressMessage("Trimming", "IL2075",
                Justification = "Reflection based serializer, hardened in a later phase.")]
            static DataContractObjectConverterOfT()
            {
                kReaders = typeof(T).GetProperties()
                    .Where(p => p.CanWrite && !p.IsSpecialName &&
                        p.GetCustomAttribute<DataMemberAttribute>() != null)
                    .Select(p =>
                    {
                        var dma = p.GetCustomAttribute<DataMemberAttribute>();
                        var name = dma?.Name ?? p.Name;
                        void Read(ref Utf8JsonReader reader, object? o,
                            JsonSerializerOptions options)
                        {
                            var typeToRead = p.GetSetMethod()?
                                .GetParameters()[0].ParameterType;
                            var v = JsonSerializer.Deserialize(
                                ref reader, typeToRead ?? typeof(object), options);
                            try
                            {
                                p.SetValue(o, v);
                            }
                            catch (Exception ex)
                            {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
                                throw new JsonException(ex.Message, ex);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
                            }
                        }
                        ReadFn read = Read;
                        return (name, read);
                    })
                    .ToDictionary(p => p.name, v => v.read);

                kReadersInsensitive = kReaders
                    .ToDictionary(p => p.Key.ToUpperInvariant(), kv => kv.Value);

                kWriters = typeof(T).GetProperties()
                    .Where(p => p.CanRead && !p.IsSpecialName &&
                        p.GetCustomAttribute<DataMemberAttribute>() != null)
                    .Select(p =>
                    {
                        var dma = p.GetCustomAttribute<DataMemberAttribute>();
                        var name = JsonEncodedText.Encode(dma?.Name ?? p.Name);
                        var emitDefault = dma?.EmitDefaultValue != false;
                        var typeToWrite = p.GetGetMethod()?.ReturnType;
                        var defaultValue = typeToWrite?.IsValueType ?? false ?
                            Activator.CreateInstance(typeToWrite) : null;
                        void Write(object? o, Utf8JsonWriter writer,
                            JsonSerializerOptions options)
                        {
                            object? v;
                            try
                            {
                                v = p.GetValue(o);
                            }
                            catch
                            {
                                v = defaultValue;
                            }
                            if (emitDefault || !IsEqual(defaultValue, v))
                            {
                                writer.WritePropertyName(name);
                                JsonSerializer.Serialize(writer, v,
                                    typeToWrite ?? v?.GetType() ?? typeof(object),
                                    options);
                            }
                        }
                        return (WriteFn)Write;
                    })
                    .Where(p => p != null)
                    .ToList();
            }

            private static bool IsEqual(object? defaultValue, object? v)
            {
                if (v == defaultValue)
                {
                    return true;
                }
                if (v is null || defaultValue is null)
                {
                    return false;
                }
                return v.Equals(defaultValue);
            }

            private static readonly Dictionary<string, ReadFn> kReaders;
            private static readonly Dictionary<string, ReadFn> kReadersInsensitive;
            private static readonly List<WriteFn> kWriters;
        }
    }

    /// <summary>
    /// Converts enums with <see cref="DataContractAttribute"/> honoring
    /// <see cref="EnumMemberAttribute"/> values.
    /// </summary>
    internal sealed class DataContractEnumConverter : JsonConverterFactory
    {
        /// <summary>
        /// Create converter
        /// </summary>
        /// <param name="namingPolicy"></param>
        /// <param name="allowIntValues"></param>
        public DataContractEnumConverter(JsonNamingPolicy namingPolicy,
            bool allowIntValues)
        {
            _namingPolicy = namingPolicy;
            _fallback = new JsonStringEnumConverter(namingPolicy, allowIntValues);
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsEnum)
            {
                return false;
            }
            var dca = typeToConvert.GetCustomAttribute<DataContractAttribute>(true);
            if (dca == null)
            {
                return false;
            }
            // If enum member attribute used
            return typeToConvert.GetMembers()
                .Any(p => p.GetCustomAttribute<EnumMemberAttribute>() != null);
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override JsonConverter? CreateConverter(Type typeToConvert,
            JsonSerializerOptions options)
        {
            var ct = typeof(DataContractEnumConverterOfT<>)
                .MakeGenericType(typeToConvert);
            return (JsonConverter?)Activator.CreateInstance(ct, [
                _fallback.CreateConverter(typeToConvert, options),
                this
            ]);
        }

        /// <summary>
        /// Actual converter of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DataContractEnumConverterOfT<T> : JsonConverter<T>
            where T : struct, Enum
        {
            /// <summary>
            /// Create converter
            /// </summary>
            /// <param name="fallback"></param>
            /// <param name="outer"></param>
            public DataContractEnumConverterOfT(JsonConverter<T>? fallback,
                DataContractEnumConverter outer)
            {
                _fallback = fallback;
                _outer = outer;
            }

            /// <inheritdoc/>
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                var token = reader.TokenType;
                if (token == JsonTokenType.String)
                {
                    var enumString = FormatStringToEnumValue(reader.GetString());
                    if (enumString == null)
                    {
                        throw new JsonException();
                    }
                    if (!Enum.TryParse<T>(enumString, ignoreCase: true, out var value))
                    {
                        throw new JsonException();
                    }
                    return value;
                }
                if (_fallback == null)
                {
                    throw new JsonException("Not supported");
                }
                return _fallback.Read(ref reader, typeToConvert, options);
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T value,
                JsonSerializerOptions options)
            {
                var key = ConvertToUInt64(value);
                if (kCache.TryGetValue(key, out var formatted))
                {
                    writer.WriteStringValue(formatted);
                    return;
                }

                var enumString = FormatEnumValueToString(value.ToString(), options);
                if (enumString != null)
                {
                    formatted = JsonEncodedText.Encode(enumString, options.Encoder);
                    writer.WriteStringValue(formatted);
                    kCache.TryAdd(key, formatted);
                    return;
                }

                if (_fallback == null)
                {
                    throw new JsonException("Not supported");
                }
                _fallback.Write(writer, value, options);
            }

            private static string? FormatStringToEnumValue(string? value)
            {
                if (value == null)
                {
                    return null;
                }
                if (!value.Contains(kSeperator, StringComparison.Ordinal))
                {
                    return Convert(value);
                }
                var enumValues = value.Split(kSeperator, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < enumValues.Length; i++)
                {
                    enumValues[i] = Convert(enumValues[i]);
                }
                return string.Join(kSeperator, enumValues);
                static string Convert(string value)
                {
                    if (kValueToMember.TryGetValue(value.ToUpperInvariant(), out var actual))
                    {
                        value = actual;
                    }
                    return value;
                }
            }

            private string? FormatEnumValueToString(string? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    return null;
                }
                var namingPolicy = _outer._namingPolicy ?? options.PropertyNamingPolicy;
                if (!value.Contains(kSeperator, StringComparison.Ordinal))
                {
                    return Convert(value, namingPolicy);
                }
                var enumValues = value.Split(kSeperator, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < enumValues.Length; i++)
                {
                    enumValues[i] = Convert(enumValues[i], namingPolicy);
                }
                return string.Join(kSeperator, enumValues);
                static string Convert(string value, JsonNamingPolicy? policy)
                {
                    // When an explicit [EnumMember(Value=...)] is declared, emit it
                    // verbatim to match the Newtonsoft StringEnumConverter wire format
                    // (the naming policy only applies to members without EnumMember).
                    if (kMemberToValue.TryGetValue(value, out var actual))
                    {
                        return actual;
                    }
                    return policy != null ? policy.ConvertName(value) : value;
                }
            }

            /// <summary>
            /// Gather type information
            /// </summary>
            [UnconditionalSuppressMessage("Trimming", "IL2090",
                Justification = "Reflection based serializer, hardened in a later phase.")]
            [UnconditionalSuppressMessage("Trimming", "IL2075",
                Justification = "Reflection based serializer, hardened in a later phase.")]
            static DataContractEnumConverterOfT()
            {
                kTypeCode = Type.GetTypeCode(typeof(T));
                kMemberToValue = typeof(T).GetMembers()
                    .Where(p => p.GetCustomAttribute<EnumMemberAttribute>() != null)
                    .ToDictionary(m => m.Name,
                        p => p.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? p.Name);
                kValueToMember = kMemberToValue
                    .ToDictionary(k => k.Value.ToUpperInvariant(), v => v.Key);
            }

            private static ulong ConvertToUInt64(object value)
            {
                System.Diagnostics.Debug.Assert(value is T);
                return kTypeCode switch
                {
                    TypeCode.Int32 => (ulong)(int)value,
                    TypeCode.UInt32 => (uint)value,
                    TypeCode.UInt64 => (ulong)value,
                    TypeCode.Int64 => (ulong)(long)value,
                    TypeCode.SByte => (ulong)(sbyte)value,
                    TypeCode.Byte => (byte)value,
                    TypeCode.Int16 => (ulong)(short)value,
                    TypeCode.UInt16 => (ushort)value,
                    _ => throw new InvalidOperationException(),
                };
            }

            private const string kSeperator = ", ";
            private static readonly Dictionary<string, string> kValueToMember;
            private static readonly Dictionary<string, string> kMemberToValue;
            private static readonly ConcurrentDictionary<ulong, JsonEncodedText> kCache = new();
            private static readonly TypeCode kTypeCode;
            private readonly JsonConverter<T>? _fallback;
            private readonly DataContractEnumConverter _outer;
        }
        private readonly JsonStringEnumConverter _fallback;
        private readonly JsonNamingPolicy _namingPolicy;
    }

    /// <summary>
    /// Read only set converter
    /// </summary>
    internal sealed class ReadOnlySetConverter : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            var type = typeToConvert.GetCompatibleGenericInterface(typeof(IReadOnlySet<>));
            return type != null;
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override JsonConverter? CreateConverter(Type typeToConvert,
            JsonSerializerOptions options)
        {
            var type = typeToConvert.GetCompatibleGenericInterface(typeof(IReadOnlySet<>));
            System.Diagnostics.Debug.Assert(type != null);
            var ct = typeof(ReadOnlySetConverterOfT<,>)
                .MakeGenericType(typeToConvert, type.GetGenericArguments()[0]);
            return (JsonConverter?)Activator.CreateInstance(ct, []);
        }

        /// <summary>
        /// Actual converter of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TElement"></typeparam>
        public class ReadOnlySetConverterOfT<T, TElement> : JsonConverter<T?>
        {
            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T? value,
                JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, (IEnumerable<TElement?>?)value, options);
            }

            /// <inheritdoc/>
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                var set = JsonSerializer.Deserialize<TElement?[]?>(ref reader, options);
                if (set != null)
                {
                    return (T?)(IReadOnlySet<TElement?>?)new HashSet<TElement?>(set);
                }
                return default;
            }
        }
    }

    /// <summary>
    /// Byte array converter allowing list of integers
    /// </summary>
    internal sealed class ByteArrayConverter : JsonConverter<byte[]>
    {
        /// <inheritdoc/>
        public override byte[]? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = JsonSerializer.Deserialize<List<byte>>(ref reader, options);
                return list?.ToArray();
            }
            return reader.GetBytesFromBase64();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer,
            byte[]? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteBase64StringValue(value);
            }
        }
    }

    /// <summary>
    /// Xml element converter
    /// </summary>
    internal sealed class XmlElementConverter : JsonConverter<XmlElement>
    {
        /// <inheritdoc/>
        public override XmlElement? Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                var encoded = reader.GetBytesFromBase64();
                var xml = Encoding.UTF8.GetString(encoded);
                if (xml == null)
                {
                    return null;
                }
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return doc.DocumentElement;
            }
            throw new JsonException();
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer,
            XmlElement? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                var encoded = Encoding.UTF8.GetBytes(value.OuterXml);
                writer.WriteBase64StringValue(encoded);
            }
        }
    }

    /// <summary>
    /// Big integer converter
    /// </summary>
    internal sealed class BigIntegerConverter : JsonConverter<BigInteger>
    {
        /// <inheritdoc/>
        public override BigInteger Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is not JsonTokenType.Number and
                not JsonTokenType.String)
            {
                throw new JsonException();
            }
            using var doc = JsonDocument.ParseValue(ref reader);
            var txt = doc.RootElement.GetRawText();
            if (reader.TokenType == JsonTokenType.String &&
                txt.Length >= 2 && txt[0] == '"' && txt[^1] == '"')
            {
                // Trim quotes
                txt = txt[1..^1].Trim();
            }
            return BigInteger.Parse(txt, NumberFormatInfo.InvariantInfo);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, BigInteger value,
            JsonSerializerOptions options)
        {
            var s = value.ToString(NumberFormatInfo.InvariantInfo);
            using var doc = JsonDocument.Parse(s);
            doc.WriteTo(writer);
        }
    }

    /// <summary>
    /// Matrix converter
    /// </summary>
    internal sealed class MatrixConverter : JsonConverterFactory
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert.IsArray && typeToConvert.GetArrayRank() > 1)
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2055",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2072",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public override JsonConverter? CreateConverter(Type typeToConvert,
            JsonSerializerOptions options)
        {
            var ct = typeof(MatrixConverterOfT<,>).MakeGenericType(
                typeToConvert, typeToConvert.GetElementType()!);
            return (JsonConverter?)Activator.CreateInstance(ct, []);
        }

        /// <summary>
        /// Actual converter of T where T is the array and E is the element type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="E"></typeparam>
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Reflection based serializer, hardened in a later phase.")]
        public class MatrixConverterOfT<T, E> : JsonConverter<T?> where T : class
        {
            /// <inheritdoc/>
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    // Expected to be at beginning of array or null
                    throw new JsonException("Expected beginning of matrix array.");
                }

                var lengths = new int[typeToConvert.GetArrayRank()];
                var slices = ReadDimension(0, ref reader, typeToConvert, lengths, options);
                if (slices is not Array from)
                {
                    throw new JsonException();
                }
                var to = Array.CreateInstance(typeof(E), lengths);
                Array.Clear(lengths);
                CopyTo(from, to, lengths, 0);
                return to as T;
            }

            private static object? ReadDimension(int dimension,
                ref Utf8JsonReader reader, Type typeToConvert, int[] lengths,
                JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }

                if (dimension == lengths.Length - 1)
                {
                    // Last dimension - read the array slice
                    var result = JsonSerializer.Deserialize(ref reader,
                        typeof(E).MakeArrayType(), options);
                    if (result is E[] element && element.Length > lengths[dimension])
                    {
                        lengths[dimension] = element.Length;
                    }
                    return result;
                }

                var list = new List<object?>();
                while (true)
                {
                    if (!reader.Read())
                    {
                        throw new JsonException("Failed to read");
                    }
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        // we have read the last item of the array
                        break;
                    }

                    // Now at start of array of next dimension
                    var result = ReadDimension(dimension + 1, ref reader,
                        typeToConvert, lengths, options);

                    list.Add(result);
                }
                if (list.Count > lengths[dimension])
                {
                    lengths[dimension] = list.Count;
                }
                return list.ToArray(); // Slice
            }

            /// <inheritdoc/>
            public override void Write(Utf8JsonWriter writer, T? value,
                JsonSerializerOptions options)
            {
                if (value is Array a)
                {
                    var indices = new int[a.Rank];
                    WriteDimension(0, writer, a, indices, options);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }

            private static void WriteDimension(int dimension, Utf8JsonWriter writer,
                Array array, int[] indices, JsonSerializerOptions options)
            {
                if (dimension == indices.Length - 1)
                {
                    // Write the innermost slice element by element so that value
                    // element types (for example byte) are emitted as JSON numbers
                    // rather than being routed through element-array converters such
                    // as the base64 byte[] converter. This matches the nested number
                    // array wire format produced by the legacy serializer.
                    writer.WriteStartArray();
                    foreach (var element in Slice(array, indices))
                    {
                        JsonSerializer.Serialize(writer, element, options);
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    writer.WriteStartArray();
                    for (var index = 0; index < array.GetLength(dimension); index++)
                    {
                        indices[dimension] = index;
                        WriteDimension(dimension + 1, writer, array, indices, options);
                    }
                    writer.WriteEndArray();
                }
                static IEnumerable<E?> Slice(Array array, int[] indices)
                {
                    for (var index = 0; index < array.GetLength(indices.Length - 1); index++)
                    {
                        indices[^1] = index;
                        yield return (E?)array.GetValue(indices);
                    }
                }
            }

            private static void CopyTo(Array slice, Array array, int[] indices, int dimension)
            {
                indices[dimension] = 0;
                foreach (var item in slice)
                {
                    if (item is Array inner)
                    {
                        CopyTo(inner, array, indices, dimension + 1);
                    }
                    else
                    {
                        if (dimension != indices.Length - 1)
                        {
                            throw new JsonException();
                        }
                        array.SetValue(item, indices);
                    }
                    indices[dimension]++;
                }
            }
        }
    }
}
