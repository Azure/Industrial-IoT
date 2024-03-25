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
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Encodes objects via Avro schema using underlying encoder.
    /// </summary>
    public sealed class AvroEncoder : BaseAvroEncoder
    {
        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Creates an encoder that writes to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="schema"></param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        /// <param name="leaveOpen">If the stream should
        /// be left open on dispose.</param>
        public AvroEncoder(Stream stream, Schema schema,
            IServiceMessageContext context, bool leaveOpen = true) :
            base(stream, context, leaveOpen)
        {
            Schema = schema;
        }

        /// <inheritdoc/>
        public override void WriteBoolean(string? fieldName, bool value)
        {
            base.WriteBoolean(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteSByte(string? fieldName, sbyte value)
        {
            base.WriteSByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByte(string? fieldName, byte value)
        {
            base.WriteByte(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt16(string? fieldName, short value)
        {
            base.WriteInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt16(string? fieldName, ushort value)
        {
            base.WriteUInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt32(string? fieldName, int value)
        {
            base.WriteInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt32(string? fieldName, uint value)
        {
            base.WriteUInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteInt64(string? fieldName, long value)
        {
            base.WriteInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteUInt64(string? fieldName, ulong value)
        {
            base.WriteUInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteFloat(string? fieldName, float value)
        {
            base.WriteFloat(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDouble(string? fieldName, double value)
        {
            base.WriteDouble(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteString(string? fieldName, string? value)
        {
            base.WriteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDateTime(string? fieldName, DateTime value)
        {
            base.WriteDateTime(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Uuid value)
        {
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Guid value)
        {
            base.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteByteString(string? fieldName, byte[]? value)
        {
            base.WriteByteString(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            base.WriteXmlElement(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteNodeId(string? fieldName, NodeId? value)
        {
            base.WriteNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            base.WriteExpandedNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteStatusCode(string? fieldName, StatusCode value)
        {
            base.WriteStatusCode(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo? value)
        {
            base.WriteDiagnosticInfo(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedName(string? fieldName,
            QualifiedName? value)
        {
            base.WriteQualifiedName(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedText(string? fieldName,
            LocalizedText? value)
        {
            base.WriteLocalizedText(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteVariant(string? fieldName,
            Variant value)
        {
            base.WriteVariant(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteDataValue(string? fieldName,
            DataValue? value)
        {
            base.WriteDataValue(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObject(string? fieldName,
            ExtensionObject? value)
        {
            base.WriteExtensionObject(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteEncodeable(string? fieldName,
            IEncodeable? value, Type? systemType)
        {
            base.WriteEncodeable(fieldName, value, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName,
            Enum? value)
        {
            base.WriteEnumerated(fieldName, value);
        }

        /// <inheritdoc/>
        public override void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            base.WriteBooleanArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            base.WriteSByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            base.WriteByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            base.WriteInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            base.WriteUInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            base.WriteInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            base.WriteUInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            base.WriteInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            base.WriteUInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            base.WriteFloatArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            base.WriteDoubleArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            base.WriteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            base.WriteDateTimeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            base.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            base.WriteByteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            base.WriteXmlElementArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            base.WriteNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            base.WriteExpandedNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            base.WriteStatusCodeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            base.WriteDiagnosticInfoArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            base.WriteQualifiedNameArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            base.WriteLocalizedTextArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            base.WriteVariantArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            base.WriteDataValueArray(fieldName, values);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
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
            base.WriteEnumeratedArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public override void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            base.WriteArray(fieldName, array, valueRank, builtInType);
        }

        /// <inheritdoc/>
        public override void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            base.WriteDataSet(fieldName, dataSet);
        }

        /// <inheritdoc/>
        public override void WriteArray<T>(string? fieldName, IList<T>? values,
            Action<T> writer, string? typeName = null)
        {
            base.WriteArray(fieldName, values, writer, typeName);
        }

        /// <summary>
        /// Add field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="array"></param>
        private void AddBuiltInType(string? fieldName, BuiltInType builtInType,
             bool array)
        {
            var dataTypeId = new NodeId((uint)builtInType);
            if (!_schemas.ContainsKey(dataTypeId))
            {
                var schema = _builtIns.GetSchemaForBuiltInType(builtInType,
                    ValueRanks.OneDimension);
                _schemas.Add(dataTypeId, schema);
            }
        }

        private readonly NodeIdDictionary<Schema> _schemas = new();
        private readonly AvroBuiltInTypeSchemas _builtIns = new();
    }

    /// <summary>
    /// Schemaless encoder
    /// </summary>
    internal sealed class SchemalessAvroEncoder : BaseAvroEncoder
    {
        /// <inheritdoc/>
        public SchemalessAvroEncoder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = true)
            : base(stream, context, leaveOpen)
        {
        }
    }
}
