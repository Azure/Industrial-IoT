// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Avro;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using global::Avro;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
        public Schema Schema => _schemas.Peek();

        /// <summary>
        /// Creates an encoder that writes to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        /// <param name="leaveOpen">If the stream should
        /// be left open on dispose.</param>
        public AvroSchemaBuilder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = true) :
            base(stream, context, leaveOpen)
        {
        }

        /// <inheritdoc/>
        public override void WriteBoolean(string? fieldName, bool value)
        {
            Add(fieldName, BuiltInType.Boolean);
            base.WriteBoolean(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteSByte(string? fieldName, sbyte value)
        {
            Add(fieldName, BuiltInType.SByte);
            base.WriteSByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByte(string? fieldName, byte value)
        {
            Add(fieldName, BuiltInType.Byte);
            base.WriteByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt16(string? fieldName, short value)
        {
            Add(fieldName, BuiltInType.Int16);
            base.WriteInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt16(string? fieldName, ushort value)
        {
            Add(fieldName, BuiltInType.UInt16);
            base.WriteUInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt32(string? fieldName, int value)
        {
            Add(fieldName, BuiltInType.Int32);
            base.WriteInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt32(string? fieldName, uint value)
        {
            Add(fieldName, BuiltInType.UInt32);
            base.WriteUInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt64(string? fieldName, long value)
        {
            Add(fieldName, BuiltInType.Int64);
            base.WriteInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt64(string? fieldName, ulong value)
        {
            Add(fieldName, BuiltInType.UInt64);
            base.WriteUInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteFloat(string? fieldName, float value)
        {
            Add(fieldName, BuiltInType.Float);
            base.WriteFloat(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDouble(string? fieldName, double value)
        {
            Add(fieldName, BuiltInType.Double);
            base.WriteDouble(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteString(string? fieldName, string? value)
        {
            Add(fieldName, BuiltInType.String);
            base.WriteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDateTime(string? fieldName, DateTime value)
        {
            Add(fieldName, BuiltInType.DateTime);
            base.WriteDateTime(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Uuid value)
        {
            Add(fieldName, BuiltInType.Guid);
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Guid value)
        {
            Add(fieldName, BuiltInType.Guid);
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByteString(string? fieldName, byte[]? value)
        {
            Add(fieldName, BuiltInType.ByteString);
            base.WriteByteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            Add(fieldName, BuiltInType.XmlElement);
            base.WriteXmlElement(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteNodeId(string? fieldName, NodeId? value)
        {
            Add(fieldName, BuiltInType.NodeId);
            base.WriteNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            Add(fieldName, BuiltInType.ExpandedNodeId);
            base.WriteExpandedNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteStatusCode(string? fieldName, StatusCode value)
        {
            Add(fieldName, BuiltInType.StatusCode);
            base.WriteStatusCode(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo? value)
        {
            Add(fieldName, BuiltInType.DiagnosticInfo);
            base.WriteDiagnosticInfo(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedName(string? fieldName,
            QualifiedName? value)
        {
            Add(fieldName, BuiltInType.QualifiedName);
            base.WriteQualifiedName(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedText(string? fieldName,
            LocalizedText? value)
        {
            Add(fieldName, BuiltInType.LocalizedText);
            base.WriteLocalizedText(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteVariant(string? fieldName,
            Variant value)
        {
            Add(fieldName, BuiltInType.Variant);
            base.WriteVariant(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDataValue(string? fieldName,
            DataValue? value)
        {
            Add(fieldName, BuiltInType.DataValue);
            base.WriteDataValue(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObject(string? fieldName,
            ExtensionObject? value)
        {
            Add(fieldName, BuiltInType.ExtensionObject);
            base.WriteExtensionObject(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteEncodeable(string? fieldName,
            IEncodeable? value, Type? systemType)
        {
            using var _ = Push(fieldName,
                value?.GetType().Name ?? systemType?.Name ?? "unknwon");
            base.WriteEncodeable(fieldName, value, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName,
            Enum? value)
        {
            Add(fieldName, BuiltInType.Enumeration);
            base.WriteEnumerated(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            Add(fieldName, BuiltInType.Boolean, ValueRanks.OneDimension);
            base.WriteBooleanArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            Add(fieldName, BuiltInType.SByte, ValueRanks.OneDimension);
            base.WriteSByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            Add(fieldName, BuiltInType.Byte, ValueRanks.OneDimension);
            base.WriteByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            Add(fieldName, BuiltInType.Int16, ValueRanks.OneDimension);
            base.WriteInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            Add(fieldName, BuiltInType.UInt16, ValueRanks.OneDimension);
            base.WriteUInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            Add(fieldName, BuiltInType.Int32, ValueRanks.OneDimension);
            base.WriteInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            Add(fieldName, BuiltInType.UInt32, ValueRanks.OneDimension);
            base.WriteUInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            Add(fieldName, BuiltInType.Int64, ValueRanks.OneDimension);
            base.WriteInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            Add(fieldName, BuiltInType.UInt64, ValueRanks.OneDimension);
            base.WriteUInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            Add(fieldName, BuiltInType.Float, ValueRanks.OneDimension);
            base.WriteFloatArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            Add(fieldName, BuiltInType.Double, ValueRanks.OneDimension);
            base.WriteDoubleArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            Add(fieldName, BuiltInType.String, ValueRanks.OneDimension);
            base.WriteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            Add(fieldName, BuiltInType.DateTime, ValueRanks.OneDimension);
            base.WriteDateTimeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            Add(fieldName, BuiltInType.Guid, ValueRanks.OneDimension);
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            Add(fieldName, BuiltInType.Guid, ValueRanks.OneDimension);
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            Add(fieldName, BuiltInType.ByteString, ValueRanks.OneDimension);
            base.WriteByteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            Add(fieldName, BuiltInType.XmlElement, ValueRanks.OneDimension);
            base.WriteXmlElementArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            Add(fieldName, BuiltInType.NodeId, ValueRanks.OneDimension);
            base.WriteNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            Add(fieldName, BuiltInType.ExpandedNodeId, ValueRanks.OneDimension);
            base.WriteExpandedNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            Add(fieldName, BuiltInType.StatusCode, ValueRanks.OneDimension);
            base.WriteStatusCodeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            Add(fieldName, BuiltInType.DiagnosticInfo, ValueRanks.OneDimension);
            base.WriteDiagnosticInfoArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            Add(fieldName, BuiltInType.QualifiedName, ValueRanks.OneDimension);
            base.WriteQualifiedNameArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            Add(fieldName, BuiltInType.LocalizedText, ValueRanks.OneDimension);
            base.WriteLocalizedTextArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            Add(fieldName, BuiltInType.Variant, ValueRanks.OneDimension);
            base.WriteVariantArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            Add(fieldName, BuiltInType.DataValue, ValueRanks.OneDimension);
            base.WriteDataValueArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            Add(fieldName, BuiltInType.ExtensionObject, ValueRanks.OneDimension);
            base.WriteExtensionObjectArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteEncodeableArray(string? fieldName,
            IList<IEncodeable>? values, Type? systemType)
        {
            using var _ = Push(fieldName, values?.FirstOrDefault()?.GetType().Name
                ?? systemType?.Name ?? "unknown", true);
            base.WriteEncodeableArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumeratedArray(string? fieldName,
            Array? values, Type? systemType)
        {
            Add(fieldName, BuiltInType.Enumeration, ValueRanks.OneDimension);
            base.WriteEnumeratedArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public override void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            Add(fieldName, builtInType, valueRank);
            base.WriteArray(fieldName, array, valueRank, builtInType);
        }

        /// <inheritdoc/>
        public override void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            using var _ = Push(fieldName, fieldName + typeof(DataSet).Name);
            base.WriteDataSet(fieldName, dataSet);
        }

        /// <inheritdoc/>
        public override void WriteArray<T>(string? fieldName, IList<T>? values,
            Action<T> writer)
        {
            using var _ = Push(fieldName, typeof(T).Name, true);
            base.WriteArray(fieldName, values, writer);
        }

        /// <summary>
        /// Add field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRanks"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void Add(string? fieldName, BuiltInType builtInType,
            int valueRanks = ValueRanks.Scalar)
        {
            var schema = _builtIns.GetSchemaForBuiltInType(builtInType,
                valueRanks);
            if (!_schemas.TryPeek(out var top))
            {
                schema = CreateRootSchema(fieldName, schema);
                _schemas.Push(schema);
            }
            else if (top is ArraySchema arr)
            {
                arr.ItemSchema = schema;
            }
            else if (top is RecordSchema r)
            {
                r.Fields.Add(new Field(schema, fieldName ?? kDefaultFieldName, 
                    r.Fields.Count));
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                    "No record schema to push to");
            }
        }

        /// <summary>
        /// Push a new record schema as field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="typeName"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private Pop Push(string? fieldName, string typeName, bool array = false)
        {
            var schema = array ? (Schema)ArraySchema.Create(
                PlaceHolderSchema.Create("Dummy", "")) :
                RecordSchema.Create(typeName, new List<Field>());
            if (!_schemas.TryPeek(out var top))
            {
                _schemas.Push(CreateRootSchema(fieldName, schema));
            }
            else if (top is ArraySchema arr)
            {
                arr.ItemSchema = schema;
            }
            else if (top is RecordSchema r)
            {
                r.Fields.Add(new Field(schema, fieldName ?? kDefaultFieldName,
                    r.Fields.Count));
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                    "No record schema to push to");
            }
            _schemas.Push(schema);
            return new Pop(this);
        }

        private static RecordSchema CreateRootSchema(string? fieldName, Schema schema)
        {
            return RecordSchema.Create("Root", new List<Field>
            {
                new (schema, fieldName ?? kDefaultFieldName, 0)
            });
        }

        private sealed record Pop(AvroSchemaBuilder outer) : IDisposable
        {
            public void Dispose()
            {
                outer._schemas.Pop();
            }
        }

        private readonly Stack<Schema> _schemas = new();
        private readonly AvroBuiltInTypeSchemas _builtIns
            = AvroBuiltInTypeSchemas.Default;
    }
}
