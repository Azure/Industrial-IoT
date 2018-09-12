// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace MsgPack {
    using System;
    using System.Threading;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Collections;

    /// <summary>
    /// Default contract serializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReflectionSerializer<T> : Serializer<T> {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReflectionSerializer() {
            _members = new List<Member>(typeof(T).GetProperties().Length);
            // Find all properties that are part of the data contract
            foreach (var prop in typeof(T).GetRuntimeProperties()) {
                foreach (var attr in prop.CustomAttributes) {
                    if (attr.AttributeType != typeof(DataMemberAttribute)) {
                        continue;
                    }
                    string name = null;
                    var order = -1;
                    foreach (var arg in attr.NamedArguments) {
                        /**/ if (arg.MemberName.Equals("Name")) {
                            name = arg.TypedValue.Value.ToString();
                        }
                        else if (arg.MemberName.Equals("Order")) {
                            int.TryParse(arg.TypedValue.Value.ToString(), out order);
                        }
                        else {
                            continue;
                        }
                    }
                    if (order < 0) {
                        throw new FormatException(
                           $"Order field required on DataMemberAttribute for {prop.Name}");
                    }
                    _members.Add(new Member(order, name, prop));
                }
            }
            _members.Sort();
        }


        /// <summary>
        /// Read property values from stream
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override async Task<T> ReadAsync(Reader reader,
            SerializerContext context, CancellationToken ct) {
            var result = CreateInstance();

            // Read object header
            var members = await reader.ReadObjectHeaderAsync(ct).ConfigureAwait(false);
            if (members > _members.Count) {
                throw new FormatException("Too many members");
            }
            foreach (var item in _members) {
                if (members == 0) {
                    if (item != null) {
                        item.Prop.SetValue(result, null);
                        continue;
                    }
                    throw new FormatException("Not enough members");
                }
                var obj = await ReadAsync(reader,
                    item.Prop.PropertyType, context, ct).ConfigureAwait(false);
                if (item != null && obj != null) {
                    item.Prop.SetValue(result, obj);
                }
                --members;
            }
            return result;
        }

        /// <summary>
        /// Create instance of T
        /// </summary>
        /// <returns></returns>
        private static T CreateInstance() {

            // TODO: Create empty using Create constructor

            return (T)typeof(T).GetConstructor(_noType).Invoke(_noObj);
        }

        /// <summary>
        /// Write properties of this type using reflection
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public override async Task WriteAsync(Writer writer, T obj,
            SerializerContext context, CancellationToken ct) {

            if (obj == null) {
                await writer.WriteNilAsync(ct).ConfigureAwait(false);
                return;
            }

            // Write object header
            await writer.WriteObjectHeaderAsync(_members.Count, ct).ConfigureAwait(false);

            foreach (var item in _members) {
                if (item != null) {
                    await WriteAsync(writer, item.Prop.GetValue(obj),
                        item.Prop.PropertyType, context, ct).ConfigureAwait(false);
                }
                else {
                    await writer.WriteNilAsync(ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Non generic helper
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<object> ReadAsync(Reader reader, Type type,
            SerializerContext context, CancellationToken ct) {

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) {
                type = nullable;
            }

            if (type == typeof(string)) {
                return await reader.ReadStringAsync(ct).ConfigureAwait(false);
            }
            if (type == typeof(byte[])) {
                return await reader.ReadBinAsync(ct).ConfigureAwait(false);
            }

            if (type.GetTypeInfo().IsPrimitive) {
                if (type == typeof(bool)) {
                    return await reader.ReadBoolAsync(ct).ConfigureAwait(false);
                }

                if (type == typeof(uint)) {
                    return await reader.ReadUInt32Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(int)) {
                    return await reader.ReadInt32Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(byte)) {
                    return await reader.ReadUInt8Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(char)) {
                    return await reader.ReadCharAsync(ct).ConfigureAwait(false);
                }

                if (type == typeof(ulong)) {
                    return await reader.ReadUInt64Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(long)) {
                    return await reader.ReadInt64Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(sbyte)) {
                    return await reader.ReadInt8Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(ushort)) {
                    return await reader.ReadUInt16Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(short)) {
                    return await reader.ReadInt16Async(ct).ConfigureAwait(false);
                }

                if (type == typeof(double)) {
                    return await reader.ReadDoubleAsync(ct).ConfigureAwait(false);
                }

                if (type == typeof(float)) {
                    return await reader.ReadFloatAsync(ct).ConfigureAwait(false);
                }

                throw new FormatException($"Type {type} is primitive, but cannot read.");
            }

            if (type.GetTypeInfo().IsValueType) {
                var val = await reader.ReadInt64Async(ct).ConfigureAwait(false);
                return Enum.ToObject(type, val);
            }

            if (type.GetTypeInfo().IsArray) {
                var len = await reader.ReadArrayLengthAsync(ct).ConfigureAwait(false);
                var itemType = type.GetElementType();
                var array = Array.CreateInstance(itemType, len);
                for (var i = 0; i < len; i++) {
                    var o = await ReadAsync(
                        reader, itemType, context, ct).ConfigureAwait(false);
                    array.SetValue(o, i);
                }
                return array;
            }

            if (typeof(IList).IsAssignableFrom(type)) {
                var list = (IList)Activator.CreateInstance(type);
                Type itemType = null;
                if (!list.GetType().GetTypeInfo().IsGenericType) {
#if !TYPE_SUPPORT
                    throw new FormatException("non-generic collections not supported");
#else
                    // Would need to add type encoding
#endif
                }
                itemType = type.GetGenericArguments()[0];
                var len = await reader.ReadArrayLengthAsync(ct).ConfigureAwait(false);
                for (var i = 0; i < len; i++) {
                    var o = await ReadAsync(
                        reader, itemType, context, ct).ConfigureAwait(false);
                    list.Add(o);
                }
                return list;
            }

            if (typeof(IDictionary).IsAssignableFrom(type)) {
                var map = (IDictionary)Activator.CreateInstance(type);
                Type keyType = null;
                Type valueType = null;
                if (!map.GetType().GetTypeInfo().IsGenericType) {
#if !TYPE_SUPPORT
                    throw new FormatException("non-generic collections not supported");
#else
                    // Would need to add type encoding
#endif
                }
                keyType = type.GetGenericArguments()[0];
                valueType = type.GetGenericArguments()[1];

                var len = await reader.ReadMapLengthAsync(ct).ConfigureAwait(false);
                for (var i = 0; i < len; i++) {
                    var key = await ReadAsync(
                        reader, keyType, context, ct).ConfigureAwait(false);
                    var value = await ReadAsync(
                        reader, valueType, context, ct).ConfigureAwait(false);
                    map.Add(key, value);
                }
                return map;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type)) {
                var list = Activator.CreateInstance(type);
                var add = type.GetMethod("Add");
                if (add == null) {
                    throw new FormatException($"No adder for enumerable of type {type}");
                }
                var itemType = add.GetParameters()[0].ParameterType;
                var len = await reader.ReadArrayLengthAsync(ct).ConfigureAwait(false);
                for (var i = 0; i < len; i++) {
                    var o = await ReadAsync(
                        reader, itemType, context, ct).ConfigureAwait(false);
                    add.Invoke(list, new object[] { o });
                }
                return list;
            }
            var serializer = context.GetType().GetMethod("Get").
                MakeGenericMethod(type).Invoke(context, _noObj);
            return await ((Task<object>)serializer.GetType().GetMethod("GetAsync").Invoke(
                serializer, new object[] { reader, context, ct })).ConfigureAwait(false);
        }

        /// <summary>
        /// Non-generic helper
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="obj"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task WriteAsync(Writer writer, object obj, Type type,
            SerializerContext context, CancellationToken ct) {

            var nullable  = Nullable.GetUnderlyingType(type);
            if (nullable != null) {
                type = nullable;
            }

            /**/ if (type.GetTypeInfo().IsPrimitive ||
                type == typeof(string) ||
                type == typeof(byte[])) {
                await writer.WriteAsync(obj, type.GetTypeInfo(), ct).ConfigureAwait(false);
            }

            else if (type.GetTypeInfo().IsValueType) {
                type = Enum.GetUnderlyingType(type);
                await WriteAsync(writer, Convert.ChangeType(obj, type),
                    type, context, ct).ConfigureAwait(false);
            }

            else if (type.GetTypeInfo().IsArray) {
                await WriteAsync(writer, context, ct, (IEnumerable)obj,
                    ((object[])obj).Length).ConfigureAwait(false);
            }
            else if (typeof(IList).IsAssignableFrom(type)) {
                await WriteAsync(writer, context, ct, (IEnumerable)obj,
                    ((IList)obj).Count).ConfigureAwait(false);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type)) {
                var enumerable = (IEnumerable)obj;
                var len = 0;
                foreach (var item in enumerable) { len++; }
                await WriteAsync(writer, context, ct,
                    enumerable, len).ConfigureAwait(false);
            }

            else if (typeof(IDictionary).IsAssignableFrom(type)) {
                var map = (IDictionary)obj;
                await writer.WriteMapHeaderAsync(map.Count, ct).ConfigureAwait(false);
                if (!map.GetType().GetTypeInfo().IsGenericType) {
#if !TYPE_SUPPORT
                    throw new FormatException("non-generic collections not supported");
#else
                    IDictionaryEnumerator enumerator = map.GetEnumerator();
                    while (enumerator.MoveNext()) {
                        await WriteAsync(writer, enumerator.Key,
                            enumerator.Key.GetType(), context, ct).ConfigureAwait(false);
                        await WriteAsync(writer, enumerator.Value,
                            enumerator.Value.GetType(), context, ct).ConfigureAwait(false);
                    }
#endif
                }
                var keyType = map.GetType().GetGenericArguments()[0];
                var valueType = map.GetType().GetGenericArguments()[1];
                var enumerator = map.GetEnumerator();
                while (enumerator.MoveNext()) {
                    await WriteAsync(writer, enumerator.Key,
                        keyType, context, ct).ConfigureAwait(false);
                    await WriteAsync(writer, enumerator.Value,
                        valueType, context, ct).ConfigureAwait(false);
                }
            }

            else {
                var serializer = context.GetType().GetMethod("Get").
                    MakeGenericMethod(type).Invoke(context, _noObj);
                await ((Task)serializer.GetType().GetMethod("SetAsync").Invoke(
                    serializer, new object[] { writer, obj, context, ct })).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Helper to write out an enumerable
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="ct"></param>
        /// <param name="enumerable"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private async Task WriteAsync(Writer writer, SerializerContext context,
            CancellationToken ct, IEnumerable enumerable, int length) {
            await writer.WriteArrayHeaderAsync(length, ct).ConfigureAwait(false);
            if (!enumerable.GetType().GetTypeInfo().IsGenericType) {
#if !TYPE_SUPPORT
                throw new FormatException("non-generic collections not supported");
#else
                foreach (var item in enumerable) {
                    await WriteAsync(writer, item,
                        item.GetType(), context, ct).ConfigureAwait(false);
                }
#endif
            }
            var generic = enumerable.GetType().GetGenericArguments()[0];
            foreach (var item in enumerable) {
                await WriteAsync(writer,
                    item, generic, context, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Member reflection helper
        /// </summary>
        class Member : IComparable<Member> {
            /// <summary>
            /// Order of property
            /// </summary>
            public int Order { get; set; }

            /// <summary>
            /// Property info
            /// </summary>
            public PropertyInfo Prop { get; set; }

            /// <summary>
            /// Name of contract member
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="order"></param>
            /// <param name="name"></param>
            /// <param name="prop"></param>
            public Member(int order, string name, PropertyInfo prop) {
                Order = order;
                Name = name;
                Prop = prop;
            }

            public int CompareTo(Member other) =>
                other == null ? 1 : Order.CompareTo(other.Order);
        }

        private List<Member> _members;
        private static readonly Type[] _noType = new Type[0];
        private static readonly object[] _noObj = new object[0];
    }
}