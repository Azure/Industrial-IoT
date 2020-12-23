// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.MessagePack {
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Exceptions;
    using global::MessagePack;
    using global::MessagePack.Formatters;
    using global::MessagePack.Resolvers;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Numerics;
    using System.Text;
#if MessagePack2
    using MsgPack = global::MessagePack.MessagePackSerializer;
#else
    using MsgPack = global::MessagePack.MessagePackSerializer.NonGeneric;
    using MsgPackWriter = global::MessagePack.MessagePackBinary;
    using MessagePackSerializationException = System.Exception;
    using MessagePackSerializerOptions = global::MessagePack.IFormatterResolver;
#endif

    /// <summary>
    /// Message pack serializer
    /// </summary>
    public class MessagePackSerializer : IMessagePackSerializerOptionsProvider,
        IBinarySerializer {

        /// <inheritdoc/>
        public string MimeType => ContentMimeType.MsgPack;

        /// <inheritdoc/>
        public Encoding ContentEncoding => null;

        /// <inheritdoc/>
        public MessagePackSerializerOptions Options { get; }

        /// <inheritdoc/>
        public IEnumerable<IFormatterResolver> Resolvers { get; }

        /// <summary>
        /// Create serializer
        /// </summary>
        /// <param name="providers"></param>
        public MessagePackSerializer(
            IEnumerable<IMessagePackFormatterResolverProvider> providers = null) {
            // Create options
            var resolvers = new List<MessagePackSerializerOptions> {
                MessagePackVariantFormatterResolver.Instance,
                ExceptionFormatterResolver.Instance
            };
            if (providers != null) {
                foreach (var provider in providers) {
                    var providedResolvers = provider.GetResolvers();
                    if (providedResolvers != null) {
                        resolvers.AddRange(providedResolvers);
                    }
                }
            }
            resolvers.Add(StandardResolver.Instance);
            resolvers.Add(DynamicContractlessObjectResolver.Instance);
            Resolvers = resolvers;

#if MessagePack2
            Options = MessagePackSerializerOptions.Standard
                .WithSecurity(MessagePackSecurity.UntrustedData)
                .WithResolver(CompositeResolver.Create(Resolvers.ToArray()))
                ;
#else
            try {
                CompositeResolver.RegisterAndSetAsDefault(Resolvers.ToArray());
            }
            catch {
                // already initialized
            }
            Options = CompositeResolver.Instance;
#endif
        }

        /// <inheritdoc/>
        public object Deserialize(ReadOnlyMemory<byte> buffer, Type type) {
            try {
#if MessagePack2
                return MsgPack.Deserialize(type, buffer, Options);
#else
                return MsgPack.Deserialize(type, buffer.ToArray(), Options);
#endif
            }
            catch (MessagePackSerializationException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public void Serialize(IBufferWriter<byte> buffer, object o, SerializeOption format) {
            try {
#if MessagePack2
                MsgPack.Serialize(buffer, o, Options);
#else
                var b = MsgPack.Serialize(o?.GetType() ?? typeof(object), o, Options);
                buffer.Write(b);
#endif
            }
            catch (MessagePackSerializationException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public VariantValue Parse(ReadOnlyMemory<byte> buffer) {
            try {
#if MessagePack2
                var o = MsgPack.Deserialize(typeof(object), buffer, Options);
#else
                var o = MsgPack.Deserialize(typeof(object), buffer.ToArray(), Options);
#endif
                if (o is VariantValue v) {
                    return v;
                }
                return new MessagePackVariantValue(o, Options, false);
            }
            catch (MessagePackSerializationException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <inheritdoc/>
        public VariantValue FromObject(object o) {
            try {
                return new MessagePackVariantValue(o, Options, true);
            }
            catch (MessagePackSerializationException ex) {
                throw new SerializerException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Value wrapper
        /// </summary>
        internal class MessagePackVariantValue : VariantValue {

            /// <summary>
            /// Create value
            /// </summary>
            /// <param name="value"></param>
            /// <param name="serializer"></param>
            /// <param name="typed">Whether the object is the
            /// original type or the generated one</param>
            /// <param name="parentUpdate"></param>
            internal MessagePackVariantValue(object value,
                MessagePackSerializerOptions serializer, bool typed,
                Action<object> parentUpdate = null) {
                _options = serializer;
                _update = parentUpdate;
                _value = typed ? ToTypeLess(value) : value;
            }

            /// <inheritdoc/>
            protected override VariantValueType GetValueType() {
                if (_value == null) {
                    return VariantValueType.Null;
                }
                var type = GetRawValue().GetType();
                if (typeof(byte[]) == type ||
                    typeof(string) == type) {
                    return VariantValueType.Primitive;
                }
                if (type.IsArray ||
                    typeof(IList<object>).IsAssignableFrom(type) ||
                    typeof(IEnumerable<object>).IsAssignableFrom(type)) {
                    return VariantValueType.Values;
                }
                if (typeof(IDictionary<object, object>).IsAssignableFrom(type)) {
                    return VariantValueType.Object;
                }
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                    type = type.GetGenericArguments()[0];
                }
                if (typeof(bool) == type ||
                    typeof(Guid) == type ||
                    typeof(DateTime) == type ||
                    typeof(DateTimeOffset) == type ||
                    typeof(TimeSpan) == type ||
                    typeof(uint) == type ||
                    typeof(int) == type ||
                    typeof(ulong) == type ||
                    typeof(long) == type ||
                    typeof(sbyte) == type ||
                    typeof(byte) == type ||
                    typeof(ushort) == type ||
                    typeof(short) == type ||
                    typeof(char) == type ||
                    typeof(float) == type ||
                    typeof(double) == type ||
                    typeof(decimal) == type ||
                    typeof(BigInteger) == type) {
                    return VariantValueType.Primitive;
                }
                if (type.GetProperties().Length > 0) {
                    return VariantValueType.Object;
                }
                // TODO: Throw?
                return VariantValueType.Primitive;
            }

            /// <inheritdoc/>
            protected override object GetRawValue() {
                if (_value is Uri u) {
                    return u.ToString();
                }
                if (_value is object[] o && o.Length == 2 && o[0] is DateTime dt) {
                    // Datetime offset encoding convention
                    switch (o[1]) {
                        case uint _:
                        case int _:
                        case ulong _:
                        case long _:
                        case ushort _:
                        case short _:
                        case byte _:
                        case sbyte _:
                            var offset = Convert.ToInt64(o[1]);
                            if (offset == 0) {
                                return dt;
                            }
                            return new DateTimeOffset(dt, TimeSpan.FromTicks(offset));
                    }
                }
                return _value;
            }

            /// <inheritdoc/>
            protected override IEnumerable<string> GetObjectProperties() {
                if (_value is IDictionary<object, object> o) {
                    return o.Keys.Select(p => p.ToString());
                }
                return Enumerable.Empty<string>();
            }

            /// <inheritdoc/>
            protected override IEnumerable<VariantValue> GetArrayElements() {
                if (_value is IList<object> array) {
                    return array.Select(i =>
                        new MessagePackVariantValue(i, _options, false));
                }
                return Enumerable.Empty<VariantValue>();
            }

            /// <inheritdoc/>
            protected override int GetArrayCount() {
                if (_value is IList<object> array) {
                    return array.Count;
                }
                return 0;
            }

            /// <inheritdoc/>
            public override VariantValue Copy(bool shallow) {
                if (_value == null) {
                    return new MessagePackVariantValue(null, _options, false);
                }
                try {
                    return new MessagePackVariantValue(_value, _options, true);
                }
                catch (MessagePackSerializationException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            public override object ConvertTo(Type type) {
                if (_value == null) {
                    return null;
                }
                var valueType = _value.GetType();
                if (type.IsAssignableFrom(valueType)) {
                    return _value;
                }
                try {
#if MessagePack2
                    var mem = new ArrayBufferWriter<byte>();
                    MsgPack.Serialize(mem, _value, _options);
                    var buffer = buffer.WrittenMemory;
#else
                    var buffer = MsgPack.Serialize(
                        _value?.GetType() ?? typeof(object), _value, _options);
#endif
                    // Special case - convert byte array to buffer if not bin to begin.
                    if (type == typeof(byte[]) && valueType.IsArray) {
                        return ((IList<byte>)MsgPack.Deserialize(typeof(IList<byte>),
                            buffer, _options)).ToArray();
                    }
                    return MsgPack.Deserialize(type, buffer, _options);
                }
                catch (MessagePackSerializationException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            /// <inheritdoc/>
            public override bool TryGetProperty(string key, out VariantValue value,
                StringComparison compare) {
                if (_value is IDictionary<object, object> o) {
                    var success = o.FirstOrDefault(kv => key.Equals((string)kv.Key, compare));
                    if (success.Value != null) {
                        value = new MessagePackVariantValue(success.Value, _options, false,
                            v => o.AddOrUpdate(success.Key, v));
                        return true;
                    }
                }
                value = null;
                return false;
            }

            /// <inheritdoc/>
            public override bool TryGetElement(int index, out VariantValue value) {
                if (index >= 0 && _value is IList<object> o && index < o.Count) {
                    value = new MessagePackVariantValue(o[index], _options, false,
                        v => o[index] = v);
                    return true;
                }
                value = null;
                return false;
            }

            /// <inheritdoc/>
            protected override VariantValue AddProperty(string property) {
                if (_value is IDictionary<object, object> o) {
                    return new MessagePackVariantValue(null, _options, false,
                        v => o.AddOrUpdate(property, v));
                }
                throw new NotSupportedException("Not an object");
            }

            /// <inheritdoc/>
            public override void AssignValue(object value) {
                if (_update != null) {
                    _update(value);
                    _value = value;
                }
                throw new NotSupportedException("Not an object or array");
            }

            /// <inheritdoc/>
            protected override bool TryEqualsVariant(VariantValue v, out bool equality) {
                if (v is MessagePackVariantValue packed) {
                    equality = DeepEquals(_value, packed._value);
                    return true;
                }

                // Special comparison to timespan
                var type = GetValueType();
                if (v.IsTimeSpan) {
                    if (IsInteger || IsDecimal) {
                        equality = v.Equals((VariantValue)TimeSpan.FromTicks(
                            Convert.ToInt64(_value)));
                        return true;
                    }
                }
                return base.TryEqualsVariant(v, out equality);
            }

            /// <summary>
            /// Compare tokens in more consistent fashion
            /// </summary>
            /// <param name="t1"></param>
            /// <param name="t2"></param>
            /// <returns></returns>
            internal bool DeepEquals(object t1, object t2) {
                if (t1 == null || t2 == null) {
                    return t1 == t2;
                }

                // Test object equals
                if (t1 is IDictionary<object, object> o1 &&
                    t2 is IDictionary<object, object> o2) {
                    // Compare properties in order of key
                    var props1 = o1.OrderBy(k => k.Key).Select(k => k.Value);
                    var props2 = o2.OrderBy(k => k.Key).Select(k => k.Value);
                    return props1.SequenceEqual(props2,
                        Compare.Using<object>((x, y) => DeepEquals(x, y)));
                }

                // Test array
                if (t1 is object[] c1 && t2 is object[] c2) {
                    return c1.SequenceEqual(c2,
                        Compare.Using<object>((x, y) => DeepEquals(x, y)));
                }

                // Test array
                if (t1 is byte[] b1 && t2 is byte[] b2) {
                    return b1.SequenceEqual(b2);
                }

                // Test value equals
                if (t1.Equals(t2)) {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Convert to typeless object
            /// </summary>
            /// <param name="value"></param>
            /// <returns></returns>
            internal object ToTypeLess(object value) {
                if (value == null) {
                    return null;
                }
                try {
#if MessagePack2
                    var mem = new ArrayBufferWriter<byte>();
                    MsgPack.Serialize(mem, value, _options);
                    var buffer = mem.WrittenMemory;
#else
                    var buffer = MsgPack.Serialize(
                        value?.GetType() ?? typeof(object), value, _options);
#endif
                    return MsgPack.Deserialize(typeof(object), buffer, _options);
                }
                catch (MessagePackSerializationException ex) {
                    throw new SerializerException(ex.Message, ex);
                }
            }

            private readonly MessagePackSerializerOptions _options;
            private readonly Action<object> _update;
            internal object _value;
        }

        /// <summary>
        /// Message pack resolver
        /// </summary>
        private class MessagePackVariantFormatterResolver : MessagePackSerializerOptions {

            public static readonly MessagePackVariantFormatterResolver Instance =
                new MessagePackVariantFormatterResolver();

            /// <inheritdoc/>
            public IMessagePackFormatter<T> GetFormatter<T>() {
                if (typeof(VariantValue).IsAssignableFrom(typeof(T))) {
                    return (IMessagePackFormatter<T>)GetVariantFormatter(typeof(T));
                }
                return null;
            }

            /// <summary>
            /// Create Message pack variant formater of specifed type
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            internal IMessagePackFormatter GetVariantFormatter(Type type) {
                return _cache.GetOrAdd(type,
                    (IMessagePackFormatter)Activator.CreateInstance(
                        typeof(MessagePackVariantFormatter<>).MakeGenericType(type)));
            }

            /// <summary>
            /// Variant formatter
            /// </summary>
            private sealed class MessagePackVariantFormatter<T> : IMessagePackFormatter<T>
                where T : VariantValue {

#if MessagePack2
                /// <inheritdoc/>
                public void Serialize(ref MessagePackWriter writer, T value,
                    MessagePackSerializerOptions options) {
                    if (value is MessagePackVariantValue packed) {
                        MsgPack.Serialize(ref writer, packed._value, options);
                    }
                    else if (value is null) {
                            writer.WriteNil();
                    }
                    else if (value is VariantValue variant) {
                        if (variant.IsNull()) {
                            writer.WriteNil();
                        }
                        else if (variant.IsListOfValues) {
                            writer.WriteArrayHeader(variant.Count);
                            foreach (var item in variant.Values) {
                                MsgPack.Serialize(ref writer, item, options);
                            }
                        }
                        else if (variant.IsObject) {
                            // Serialize objects as key value pairs
                            var dict = variant.PropertyNames
                                .ToDictionary(k => k, k => variant[k]);
                            MsgPack.Serialize(ref writer, dict, options);
                        }
                        else if (variant.TryGetValue(out var primitive)) {
                            MsgPack.Serialize(ref writer, primitive, options);
                        }
                        else {
                            MsgPack.Serialize(ref writer, variant.Value, options);
                        }
                    }
                }
#else
                /// <inheritdoc/>
                public int Serialize(ref byte[] bytes, int offset, T value,
                    MessagePackSerializerOptions options) {
                    if (value is MessagePackVariantValue packed) {
                        return MsgPack.Serialize(packed._value?.GetType() ?? typeof(object),
                            ref bytes, offset, packed._value, options);
                    }
                    else if (value is null) {
                        return MsgPackWriter.WriteNil(ref bytes, offset);
                    }
                    else if (value is VariantValue variant) {
                        if (VariantValueEx.IsNull(variant)) {
                            return MsgPackWriter.WriteNil(ref bytes, offset);
                        }
                        else if (variant.IsListOfValues) {
                            var written = MsgPackWriter.WriteArrayHeader(
                                ref bytes, offset, variant.Count);
                            foreach (var item in variant.Values) {
                                written += MsgPack.Serialize(item?.GetType() ?? typeof(object),
                                    ref bytes, offset + written, item, options);
                            }
                            return written;
                        }
                        else if (variant.IsObject) {
                            // Serialize objects as key value pairs
                            var dict = variant.PropertyNames
                                .ToDictionary(k => k, k => variant[k]);
                            return MsgPack.Serialize(dict.GetType(), ref bytes,
                                offset, dict, options);
                        }
                        else if (variant.TryGetValue(out var primitive)) {
                            return MsgPack.Serialize(primitive?.GetType() ?? typeof(object),
                                ref bytes, offset, primitive, options);
                        }
                        else {
                            return MsgPack.Serialize(variant.Value?.GetType() ?? typeof(object),
                                ref bytes, offset, variant.Value, options);
                        }
                    }
                    else {
                        return offset;
                    }
                }
#endif

#if MessagePack2
                /// <inheritdoc/>
                public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    // Read variant from reader
                    var o = MsgPack.Deserialize<object>(ref reader, options);
                    return new MessagePackVariantValue(o, options, false) as T;
                }
#else
                /// <inheritdoc/>
                public T Deserialize(byte[] bytes, int offset, MessagePackSerializerOptions options,
                    out int readSize) {
                    var o = MsgPack.Deserialize(typeof(object), bytes, offset, options, out readSize);
                    if (o == null) {
                        return default;
                    }
                    return new MessagePackVariantValue(o, options, false) as T;
                }
#endif
            }

            private readonly ConcurrentDictionary<Type, IMessagePackFormatter> _cache =
                new ConcurrentDictionary<Type, IMessagePackFormatter>();
        }

        /// <summary>
        /// Exception resolver
        /// </summary>
        private class ExceptionFormatterResolver : MessagePackSerializerOptions {

            public static readonly ExceptionFormatterResolver Instance =
                new ExceptionFormatterResolver();

            /// <inheritdoc/>
            public IMessagePackFormatter<T> GetFormatter<T>() {
                if (typeof(Exception).IsAssignableFrom(typeof(T))) {
                    return (IMessagePackFormatter<T>)GetVariantFormatter(typeof(T));
                }
                return null;
            }

            /// <summary>
            /// Create Message pack variant formater of specifed type
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            internal IMessagePackFormatter GetVariantFormatter(Type type) {
                return _cache.GetOrAdd(type,
                    (IMessagePackFormatter)Activator.CreateInstance(
                        typeof(ExceptionFormatter<>).MakeGenericType(type)));
            }

            /// <summary>
            /// Variant formatter
            /// </summary>
            private sealed class ExceptionFormatter<T> : IMessagePackFormatter<T>
                where T : Exception, new() {

#if MessagePack2
                /// <inheritdoc/>
                public void Serialize(ref MessagePackWriter writer, T value,
                    MessagePackSerializerOptions options) {
                }
#else
                /// <inheritdoc/>
                public int Serialize(ref byte[] bytes, int offset, T value,
                    MessagePackSerializerOptions options) {
                    return offset;
                }
#endif

#if MessagePack2
                /// <inheritdoc/>
                public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    // Read variant from reader
                    return new T();
                }
#else
                /// <inheritdoc/>
                public T Deserialize(byte[] bytes, int offset, MessagePackSerializerOptions options,
                    out int readSize) {
                    readSize = 0;
                    return new T();
                }
#endif
            }

            private readonly ConcurrentDictionary<Type, IMessagePackFormatter> _cache =
                new ConcurrentDictionary<Type, IMessagePackFormatter>();
        }
    }
}