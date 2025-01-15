// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Xml;

    /// <summary>
    /// Encodes objects and inline builds the schema from it
    /// This type exists mainly for testing.
    /// </summary>
    public sealed class AvroSchemaBuilder : BaseAvroEncoder
    {
        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Schema => _schemas.Peek().Unwrap();

        /// <summary>
        /// Creates an encoder that writes to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the encoder writes.
        /// </param>
        /// <param name="context">The message context to use for the
        /// encoding.</param>
        /// <param name="leaveOpen">If the stream should be left open on
        /// dispose.</param>
        /// <param name="emitConciseSchemas">If the builder should avoid
        /// creating large union schemas</param>
        public AvroSchemaBuilder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = true,
            bool emitConciseSchemas = false) :
            base(stream, context, leaveOpen)
        {
            _emitConciseSchemas = emitConciseSchemas;
        }

        /// <inheritdoc/>
        public override void WriteBoolean(string? fieldName, bool value)
        {
            using var _ = Add(fieldName, BuiltInType.Boolean);
            base.WriteBoolean(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteSByte(string? fieldName, sbyte value)
        {
            using var _ = Add(fieldName, BuiltInType.SByte);
            base.WriteSByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByte(string? fieldName, byte value)
        {
            using var _ = Add(fieldName, BuiltInType.Byte);
            base.WriteByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt16(string? fieldName, short value)
        {
            using var _ = Add(fieldName, BuiltInType.Int16);
            base.WriteInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt16(string? fieldName, ushort value)
        {
            using var _ = Add(fieldName, BuiltInType.UInt16);
            base.WriteUInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt32(string? fieldName, int value)
        {
            using var _ = Add(fieldName, BuiltInType.Int32);
            base.WriteInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt32(string? fieldName, uint value)
        {
            using var _ = Add(fieldName, BuiltInType.UInt32);
            base.WriteUInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt64(string? fieldName, long value)
        {
            using var _ = Add(fieldName, BuiltInType.Int64);
            base.WriteInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt64(string? fieldName, ulong value)
        {
            using var _ = Add(fieldName, BuiltInType.UInt64);
            base.WriteUInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteFloat(string? fieldName, float value)
        {
            using var _ = Add(fieldName, BuiltInType.Float);
            base.WriteFloat(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDouble(string? fieldName, double value)
        {
            using var _ = Add(fieldName, BuiltInType.Double);
            base.WriteDouble(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteString(string? fieldName, string? value)
        {
            using var _ = Add(fieldName, BuiltInType.String);
            base.WriteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDateTime(string? fieldName, DateTime value)
        {
            using var _ = Add(fieldName, BuiltInType.DateTime);
            base.WriteDateTime(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Uuid value)
        {
            using var _ = Add(fieldName, BuiltInType.Guid);
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Guid value)
        {
            using var _ = Add(fieldName, BuiltInType.Guid);
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByteString(string? fieldName, byte[]? value)
        {
            using var _ = Add(fieldName, BuiltInType.ByteString);
            base.WriteByteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            using var _ = Add(fieldName, BuiltInType.XmlElement);
            base.WriteXmlElement(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteNodeId(string? fieldName, NodeId? value)
        {
            using var _ = Add(fieldName, BuiltInType.NodeId);
            base.WriteNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            using var _ = Add(fieldName, BuiltInType.ExpandedNodeId);
            base.WriteExpandedNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteStatusCode(string? fieldName, StatusCode value)
        {
            using var _ = Add(fieldName, BuiltInType.StatusCode);
            base.WriteStatusCode(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo? value)
        {
            using var _ = Add(fieldName, BuiltInType.DiagnosticInfo);
            base.WriteDiagnosticInfo(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedName(string? fieldName,
            QualifiedName? value)
        {
            using var _ = Add(fieldName, BuiltInType.QualifiedName);
            base.WriteQualifiedName(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedText(string? fieldName,
            LocalizedText? value)
        {
            using var _ = Add(fieldName, BuiltInType.LocalizedText);
            base.WriteLocalizedText(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteVariant(string? fieldName, Variant value)
        {
            if (_emitConciseSchemas && value != Variant.Null && !_skipInnerSchemas)
            {
                if (value.Value is ExtensionObject eo && eo.Body is IEncodeable e)
                {
                    WriteEncodeable(fieldName, e, e.GetType());
                    return;
                }

                var rank = SchemaUtils.GetRank(value.TypeInfo.ValueRank);
                using var __ = Add(fieldName, value.TypeInfo.BuiltInType,
                    SchemaUtils.GetRank(value.TypeInfo.ValueRank));
                WriteVariantValue(value, value.TypeInfo.BuiltInType, rank);
                return;
            }

            using var _ = Add(fieldName, BuiltInType.Variant);
            base.WriteVariant(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDataValue(string? fieldName,
            DataValue? value)
        {
            if (_emitConciseSchemas && value != null &&
                value.WrappedValue != Variant.Null)
            {
                using var __ = Record(fieldName,
                    value.WrappedValue.TypeInfo.BuiltInType + "DataValue");
                base.WriteDataValue(fieldName, value);
                return;
            }
            using var _ = Add(fieldName, BuiltInType.DataValue);
            base.WriteDataValue(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObject(string? fieldName,
            ExtensionObject? value)
        {
            using var _ = Add(fieldName, BuiltInType.ExtensionObject);
            base.WriteExtensionObject(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteEncodeable(string? fieldName,
            IEncodeable? value, Type? systemType)
        {
            var fullName = GetFullNameOfEncodeable(value, systemType,
                out var typeName, out var typeId);
            if (typeName == null)
            {
                throw new EncodingException(
                    "Failed to encode a encodeable without system type");
            }
            using var _ = Record(fieldName, fullName ?? typeName, typeId: typeId);
            base.WriteEncodeable(fieldName, value, systemType);
        }

        /// <inheritdoc/>
        public override void WriteObject(string? fieldName, string? typeName,
            Action writer)
        {
            using var _ = Record(fieldName, typeName ?? fieldName ?? "unknown");
            base.WriteObject(fieldName, typeName, writer);
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName, Enum? value)
        {
            using var _ = value == null ? Add(fieldName, BuiltInType.Enumeration) :
                Enumeration(fieldName, value.GetType());
            base.WriteEnumerated(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName, int value)
        {
            using var _ = Add(fieldName, BuiltInType.Enumeration);
            base.WriteEnumerated(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Boolean, SchemaRank.Collection);
            base.WriteBooleanArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            using var _ = Add(fieldName, BuiltInType.SByte, SchemaRank.Collection);
            base.WriteSByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            using var _ = Add(fieldName, BuiltInType.ByteString, SchemaRank.Scalar);
            base.WriteByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Int16, SchemaRank.Collection);
            base.WriteInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            using var _ = Add(fieldName, BuiltInType.UInt16, SchemaRank.Collection);
            base.WriteUInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Int32, SchemaRank.Collection);
            base.WriteInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            using var _ = Add(fieldName, BuiltInType.UInt32, SchemaRank.Collection);
            base.WriteUInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Int64, SchemaRank.Collection);
            base.WriteInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            using var _ = Add(fieldName, BuiltInType.UInt64, SchemaRank.Collection);
            base.WriteUInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Float, SchemaRank.Collection);
            base.WriteFloatArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Double, SchemaRank.Collection);
            base.WriteDoubleArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.String, SchemaRank.Collection);
            base.WriteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            using var _ = Add(fieldName, BuiltInType.DateTime, SchemaRank.Collection);
            base.WriteDateTimeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Guid, SchemaRank.Collection);
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            using var _ = Add(fieldName, BuiltInType.Guid, SchemaRank.Collection);
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.ByteString, SchemaRank.Collection);
            base.WriteByteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.XmlElement, SchemaRank.Collection);
            base.WriteXmlElementArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.NodeId, SchemaRank.Collection);
            base.WriteNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.ExpandedNodeId, SchemaRank.Collection);
            base.WriteExpandedNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            using var _ = Add(fieldName, BuiltInType.StatusCode, SchemaRank.Collection);
            base.WriteStatusCodeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.DiagnosticInfo, SchemaRank.Collection);
            base.WriteDiagnosticInfoArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.QualifiedName, SchemaRank.Collection);
            base.WriteQualifiedNameArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.LocalizedText, SchemaRank.Collection);
            base.WriteLocalizedTextArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            if (_emitConciseSchemas && values?.Count > 0 && !_skipInnerSchemas)
            {
                var typeInfo = values[0].TypeInfo;
                if (typeInfo.ValueRank == ValueRanks.Scalar &&
                    values.All(v => typeInfo.Equals(v.TypeInfo)))
                {
                    WriteArray(typeInfo.BuiltInType, values.Select(v => v.Value).ToArray());
                    return;
                }
            }
            using var _ = Add(fieldName, BuiltInType.Variant, SchemaRank.Collection);
            base.WriteVariantArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.DataValue, SchemaRank.Collection);
            base.WriteDataValueArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            using var _ = Add(fieldName, BuiltInType.ExtensionObject, SchemaRank.Collection);
            base.WriteExtensionObjectArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteEncodeableArray(string? fieldName,
            IList<IEncodeable>? values, Type? systemType)
        {
            base.WriteEncodeableArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumeratedArray(string? fieldName,
            Array? values, Type? systemType)
        {
            var enumType = values?.GetType().GetElementType() ?? systemType;
            using var _ = enumType == null ?
                Add(fieldName, BuiltInType.Enumeration, SchemaRank.Collection) :
                Enumeration(fieldName, enumType, SchemaRank.Collection);
            base.WriteEnumeratedArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumeratedArray(string? fieldName,
            int[] values, Type? enumType)
        {
            using var _ = Add(fieldName, BuiltInType.Enumeration, SchemaRank.Collection);
            base.WriteEnumeratedArray(fieldName, values, enumType);
        }

        /// <inheritdoc/>
        public override void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            using var _ = Add(fieldName, builtInType, SchemaUtils.GetRank(valueRank));
            base.WriteArray(fieldName, array, valueRank, builtInType);
        }

        /// <inheritdoc/>
        public override void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            using var _ = Record(fieldName, fieldName + nameof(DataSet));
            base.WriteDataSet(fieldName, dataSet);
        }

        /// <inheritdoc/>
        public override void WriteArray<T>(string? fieldName, IList<T>? values,
            Action<T> writer, string? typeName = null)
        {
            using var _ = Array(fieldName);
            base.WriteArray(fieldName, values, writer);
        }

        /// <inheritdoc/>
        protected override void WriteNullable<T>(string? fieldName, T? value,
            Action<string?, T> writer) where T : class
        {
            using var _ = Union(fieldName, true);
            base.WriteNullable(fieldName, value, writer);
            if (value == null)
            {
                //
                // We need to add the type to the schema even in case of
                // null value.
                // Try to be generic enough, but this will not work for
                // everything at this point. Need to update if tests fail.
                // Today we use this for DiagnosticInfo and DataValue
                //
                SchemaRank rank;
                var type = typeof(T);
                if (typeof(T).IsArray)
                {
                    rank = SchemaRank.Collection;
                    type = type.GetElementType();
                }
                else
                {
                    rank = SchemaRank.Scalar;
                }
                var builtInType = Enum.Parse<BuiltInType>(type!.Name);
                using var __ = Add(fieldName, builtInType, rank);
            }
        }

        /// <summary>
        /// Add field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRanks"></param>
        /// <exception cref="EncodingException"></exception>
        private IDisposable Add(string? fieldName, BuiltInType builtInType,
            SchemaRank valueRanks = SchemaRank.Scalar)
        {
            if (_skipInnerSchemas)
            {
                return Nothing.ToDo;
            }
            _skipInnerSchemas = true;
            var schema = _builtIns.GetSchemaForBuiltInType(builtInType,
                valueRanks);
            if (!_schemas.TryPeek(out var top))
            {
                _schemas.Push(schema.CreateRoot(fieldName));
            }
            else if (top is ArraySchema arr)
            {
                arr.ItemSchema = schema;
            }
            else if (top is UnionSchema u)
            {
                u.Schemas.Add(schema);
            }
            else if (top is RecordSchema r)
            {
                r.Fields.Add(new Field(schema, fieldName ?? kDefaultFieldName,
                    r.Fields.Count));
            }
            else
            {
                throw new EncodingException("No record schema to push to",
                    Schema.ToJson());
            }
            return new Skip(this);
        }

        /// <summary>
        /// Push enum schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="enumType"></param>
        /// <param name="rank"></param>
        /// <returns></returns>
        private IDisposable Enumeration(string? fieldName, Type enumType,
            SchemaRank rank = SchemaRank.Scalar)
        {
            if (_skipInnerSchemas)
            {
                return Nothing.ToDo;
            }
            // Get enum types from DataMemberAttribute
            var names = enumType.GetProperties()
                .Select(p => p.GetCustomAttribute<DataMemberAttribute>()!)
                .Where(a => a?.Name != null)
                .OrderBy(a => a.Order)
                .Select(a => a.Name!)
                .ToArray();
            if (names.Length == 0)
            {
                names = Enum.GetNames(enumType);
            }
            Schema schema = EnumSchema.Create(enumType.Name, names);
            if (rank == SchemaRank.Collection)
            {
                schema = schema.AsArray();
            }
            return PushSchema(fieldName, schema);
        }

        /// <summary>
        /// Push a new record schema as field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="typeName"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private IDisposable Record(string? fieldName, string typeName,
            ExpandedNodeId? typeId = null)
        {
            if (_skipInnerSchemas)
            {
                return Nothing.ToDo;
            }
            var schema = RecordSchema.Create(typeName, [],
                customProperties: AvroSchema.Properties(
                    typeId?.AsString(Context, NamespaceFormat.Uri)));
            return PushSchema(fieldName, schema);
        }

        /// <summary>
        /// Push a new array schema as field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        private IDisposable Array(string? fieldName)
        {
            if (_skipInnerSchemas)
            {
                return Nothing.ToDo;
            }
            return PushSchema(fieldName, AvroSchema.CreatePlaceHolder("Dummy", "").AsArray(
));
        }

        /// <summary>
        /// Push a union
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        private IDisposable Union(string? fieldName, bool nullable = false)
        {
            if (_skipInnerSchemas)
            {
                return Nothing.ToDo;
            }
            var schema = UnionSchema.Create([]);
            if (nullable)
            {
                schema.Schemas.Add(AvroSchema.Null);
            }
            return PushSchema(fieldName, schema);
        }

        /// <summary>
        /// Push schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private Pop PushSchema(string? fieldName, Schema schema)
        {
            if (!_schemas.TryPeek(out var top))
            {
                _schemas.Push(schema.CreateRoot(fieldName));
            }
            else if (top is ArraySchema arr)
            {
                arr.ItemSchema = schema;
            }
            else if (top is UnionSchema u)
            {
                u.Schemas.Add(schema);
            }
            else if (top is RecordSchema r)
            {
                r.Fields.Add(new Field(schema, fieldName ?? kDefaultFieldName,
                    r.Fields.Count));
            }
            else
            {
                throw new EncodingException("No record schema to push to.",
                    Schema.ToJson());
            }
            _schemas.Push(schema);
            return new Pop(this);
        }

        private sealed record Pop(AvroSchemaBuilder Outer) : IDisposable
        {
            public void Dispose()
            {
                Outer._schemas.Pop();
            }
        }

        private sealed record Skip(AvroSchemaBuilder Outer) : IDisposable
        {
            public void Dispose()
            {
                Outer._skipInnerSchemas = false;
            }
        }

        private sealed class Nothing : IDisposable
        {
            public static readonly Nothing ToDo = new();

            public void Dispose()
            {
            }
        }

        private readonly Stack<Schema> _schemas = new();
        private readonly AvroBuiltInSchemas _builtIns = new();
        private readonly bool _emitConciseSchemas;
        private bool _skipInnerSchemas;
    }
}
