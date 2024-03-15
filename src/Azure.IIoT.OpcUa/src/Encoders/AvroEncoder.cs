// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using global::Avro;
    using global::Avro.Generic;
    using global::Avro.IO;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Encoders.Avro;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Encodes objects via Avro schema using underlying encoder.
    /// </summary>
    public sealed class AvroEncoder : IEncoder
    {
        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Schema { get; }

        /// <inheritdoc/>
        public EncodingType EncodingType => _encoder.EncodingType;

        /// <inheritdoc/>
        public bool UseReversibleEncoding => _encoder.UseReversibleEncoding;

        /// <inheritdoc/>
        public IServiceMessageContext Context => _encoder.Context;

        /// <summary>
        /// Creates an encoder that encodes the passed in information as
        /// per the provided schema. The encoder will throw if the info
        /// does not comply with the schema
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="schema"></param>
        internal AvroEncoder(AvroEncoderCore encoder, Schema schema)
        {
            _encoder = encoder;
            Schema = schema;

            // Point encodeable encoder to us
            _encoder.EncodeableEncoder = this;
        }

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
            this(new AvroEncoderCore(stream, context, leaveOpen), schema)
        {
        }

        /// <inheritdoc/>
        public int Close()
        {
            return _encoder.Close();
        }

        /// <inheritdoc/>
        public string? CloseAndReturnText()
        {
            return _encoder.CloseAndReturnText();
        }

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable? namespaceUris,
            StringTable? serverUris)
        {
            _encoder.SetMappingTables(namespaceUris, serverUris);
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri)
        {
            _encoder.PushNamespace(namespaceUri);
        }

        /// <inheritdoc/>
        public void PopNamespace()
        {
            _encoder.PopNamespace();
        }

        /// <inheritdoc/>
        public void WriteBoolean(string? fieldName, bool value)
        {
            _encoder.WriteBoolean(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteSByte(string? fieldName, sbyte value)
        {
            _encoder.WriteSByte(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteByte(string? fieldName, byte value)
        {
            _encoder.WriteByte(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteInt16(string? fieldName, short value)
        {
            _encoder.WriteInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteUInt16(string? fieldName, ushort value)
        {
            _encoder.WriteUInt16(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteInt32(string? fieldName, int value)
        {
            _encoder.WriteInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteUInt32(string? fieldName, uint value)
        {
            _encoder.WriteUInt32(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteInt64(string? fieldName, long value)
        {
            _encoder.WriteInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteUInt64(string? fieldName, ulong value)
        {
            _encoder.WriteUInt64(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteFloat(string? fieldName, float value)
        {
            _encoder.WriteFloat(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteDouble(string? fieldName, double value)
        {
            _encoder.WriteDouble(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteString(string? fieldName, string value)
        {
            _encoder.WriteString(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteDateTime(string? fieldName, DateTime value)
        {
            _encoder.WriteDateTime(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Uuid value)
        {
            _encoder.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Guid value)
        {
            _encoder.WriteGuid(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteByteString(string? fieldName, byte[]? value)
        {
            _encoder.WriteByteString(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            _encoder.WriteXmlElement(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteNodeId(string? fieldName, NodeId? value)
        {
            _encoder.WriteNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            _encoder.WriteExpandedNodeId(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string? fieldName, StatusCode value)
        {
            _encoder.WriteStatusCode(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo? value)
        {
            _encoder.WriteDiagnosticInfo(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteQualifiedName(string? fieldName,
            QualifiedName? value)
        {
            _encoder.WriteQualifiedName(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteLocalizedText(string? fieldName,
            LocalizedText? value)
        {
            _encoder.WriteLocalizedText(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteVariant(string? fieldName,
            Variant value)
        {
            _encoder.WriteVariant(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteDataValue(string? fieldName,
            DataValue? value)
        {
            _encoder.WriteDataValue(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string? fieldName,
            ExtensionObject value)
        {
            _encoder.WriteExtensionObject(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string? fieldName,
            IEncodeable value, Type systemType)
        {
            _encoder.WriteEncodeable(fieldName, value, systemType);
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string? fieldName,
            Enum value)
        {
            _encoder.WriteEnumerated(fieldName, value);
        }

        /// <inheritdoc/>
        public void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            _encoder.WriteBooleanArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            _encoder.WriteSByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            _encoder.WriteByteArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            _encoder.WriteInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            _encoder.WriteUInt16Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            _encoder.WriteInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            _encoder.WriteUInt32Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            _encoder.WriteInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            _encoder.WriteUInt64Array(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            _encoder.WriteFloatArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            _encoder.WriteDoubleArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            _encoder.WriteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteDateTimeArray(string? fieldName,
            IList<DateTime> values)
        {
            _encoder.WriteDateTimeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName,
            IList<Uuid> values)
        {
            _encoder.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName,
            IList<Guid> values)
        {
            _encoder.WriteGuidArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            _encoder.WriteByteStringArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            _encoder.WriteXmlElementArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            _encoder.WriteNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            _encoder.WriteExpandedNodeIdArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode> values)
        {
            _encoder.WriteStatusCodeArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            _encoder.WriteDiagnosticInfoArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            _encoder.WriteQualifiedNameArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            _encoder.WriteLocalizedTextArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteVariantArray(string? fieldName,
            IList<Variant> values)
        {
            _encoder.WriteVariantArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            _encoder.WriteDataValueArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            _encoder.WriteExtensionObjectArray(fieldName, values);
        }

        /// <inheritdoc/>
        public void WriteEncodeableArray(string? fieldName,
            IList<IEncodeable>? values, Type systemType)
        {
            _encoder.WriteEncodeableArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string? fieldName,
            Array values, Type systemType)
        {
            _encoder.WriteEnumeratedArray(fieldName, values, systemType);
        }

        /// <inheritdoc/>
        public void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            _encoder.WriteArray(fieldName, array, valueRank, builtInType);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _encoder.Dispose();
        }

        private readonly AvroEncoderCore _encoder;
    }
}
