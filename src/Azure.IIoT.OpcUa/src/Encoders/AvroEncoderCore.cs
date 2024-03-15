// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
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
    using System.Xml;

    /// <summary>
    /// Encodes objects in a stream using Avro binary encoding.
    /// </summary>
    internal sealed class AvroEncoderCore : IEncoder
    {
        /// <inheritdoc/>
        public EncodingType EncodingType => (EncodingType)3;

        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <inheritdoc/>
        public bool UseReversibleEncoding => true;

        /// <summary>
        /// Outer encoders need a way to get encodeables to use it.
        /// </summary>
        public IEncoder EncodeableEncoder { get; set; }

        /// <summary>
        /// Creates an encoder that writes to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        /// <param name="leaveOpen">If the stream should
        /// be left open on dispose.</param>
        public AvroEncoderCore(Stream stream, IServiceMessageContext context,
            bool leaveOpen = true)
        {
            EncodeableEncoder = this;
            _writer = new AvroWriter(stream);
            Context = context;
            _leaveOpen = leaveOpen;
            _nestingLevel = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _writer.Dispose();
            }
        }

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable? namespaceUris,
            StringTable? serverUris)
        {
            _namespaceMappings = null;

            if (namespaceUris != null &&
                Context.NamespaceUris != null)
            {
                _namespaceMappings = namespaceUris.CreateMapping(
                    Context.NamespaceUris, false);
            }

            _serverMappings = null;
            if (serverUris != null &&
                Context.ServerUris != null)
            {
                _serverMappings = serverUris.CreateMapping(
                    Context.ServerUris, false);
            }
        }

        /// <inheritdoc/>
        public string? CloseAndReturnText()
        {
            throw new NotSupportedException("Unsupported");
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri)
        {
            // not used in the binary encoding.
        }

        /// <inheritdoc/>
        public void PopNamespace()
        {
            // not used in the binary encoding.
        }

        /// <inheritdoc/>
        public int Close()
        {
            var position = (int)_writer.Stream.Position;
            _writer.Stream.Flush();
            return position;
        }

        /// <inheritdoc/>
        public void WriteBoolean(string? fieldName, bool value)
        {
            _writer.WriteBoolean(value);
        }

        /// <inheritdoc/>
        public void WriteSByte(string? fieldName, sbyte value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteByte(string? fieldName, byte value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteInt16(string? fieldName, short value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteUInt16(string? fieldName, ushort value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteInt32(string? fieldName, int value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteUInt32(string? fieldName, uint value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteInt64(string? fieldName, long value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public void WriteUInt64(string? fieldName, ulong value)
        {
            // ulong is a union of long and fixed (> long.max)
            if (value < long.MaxValue)
            {
                _writer.WriteInteger(0);
                _writer.WriteInteger((long)value);
            }
            else
            {
                _writer.WriteInteger(1);
                Span<byte> bytes = stackalloc byte[sizeof(ulong)];
                BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
                _writer.WriteFixed(bytes);
            }
        }

        /// <inheritdoc/>
        public void WriteFloat(string? fieldName, float value)
        {
            _writer.WriteFloat(value);
        }

        /// <inheritdoc/>
        public void WriteDouble(string? fieldName, double value)
        {
            _writer.WriteDouble(value);
        }

        /// <inheritdoc/>
        public void WriteDateTime(string? fieldName, DateTime value)
        {
            value = Opc.Ua.Utils.ToOpcUaUniversalTime(value);
            var ticks = value.Ticks;

            // check for max value.
            if (ticks >= DateTime.MaxValue.Ticks)
            {
                ticks = long.MaxValue;
            }
            // check for min value.
            else
            {
                ticks -= Opc.Ua.Utils.TimeBase.Ticks;

                if (ticks <= 0)
                {
                    ticks = 0;
                }
            }
            _writer.WriteInteger(ticks);
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Uuid value)
        {
            _writer.WriteFixed(((Guid)value).ToByteArray());
        }

        /// <inheritdoc/>
        public void WriteGuid(string? fieldName, Guid value)
        {
            _writer.WriteFixed(value.ToByteArray());
        }

        /// <summary>
        /// Write nullable which is a union object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="o"></param>
        /// <returns></returns>
        internal bool WriteNullable<T>([NotNullWhen(false)] T? o) where T : class
        {
            _writer.WriteInteger(o == null ? 0 : 1); // Union index, first is "null"
            return o == null;
        }

        /// <inheritdoc/>
        public void WriteString(string? fieldName, string? value)
        {
            // String is nullable
            if (WriteNullable(value))
            {
                return;
            }
            _writer.WriteString(value);
        }

        /// <inheritdoc/>
        public void WriteByteString(string? fieldName, byte[]? value)
        {
            // byte string is nullable
            if (WriteNullable(value))
            {
                return;
            }

            if (Context.MaxByteStringLength > 0 &&
                Context.MaxByteStringLength < value.Length)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "MaxByteStringLength {0} < {1}",
                    Context.MaxByteStringLength,
                    value.Length);
            }
            _writer.WriteBytes(value);
        }

        /// <inheritdoc/>
        public void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            // xml string is nullable
            if (WriteNullable(value))
            {
                return;
            }
            WriteXmlElement(value);
        }

        /// <summary>
        /// Write non nullable xml element
        /// </summary>
        /// <param name="value"></param>
        public void WriteXmlElement(XmlElement value)
        {
            _writer.WriteString(value.OuterXml);
        }

        /// <inheritdoc/>
        public void WriteDataSet(DataSet dataSet)
        {
            var fieldContentMask = dataSet.DataSetFieldContentMask;
            if ((fieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0 ||
                fieldContentMask == 0)
            {
                _writer.WriteInteger(1);

                //
                // Write array of raw variant
                //
                WriteArray(dataSet.Values
                    .Select(v => v?.WrappedValue ?? default)
                    .ToList(),
                        v => WriteVariant(null, v));
            }
            else
            {
                _writer.WriteInteger(0);

                //
                // Write array of data values
                //
                WriteArray(dataSet.Values
                    .ToList(),
                        v => WriteDataValue(null, v));
            }
        }

        /// <inheritdoc/>
        public void WriteNodeId(string? fieldName, NodeId? value)
        {
            //
            // Node id is a record, with namespace and union of
            // the id. The IdType value represents the union
            // discriminator.
            //
            // Node id is not nullable, i=0 is a null node id.
            //
            var namespaceIndex = value?.NamespaceIndex ?? 0;
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            _writer.WriteInteger(namespaceIndex);

            var idUnionIndex = value?.IdType ?? IdType.Numeric;
            //  Numeric = 0,
            //  String = 1
            //  Guid = 2
            //  Opaque = 3
            _writer.WriteInteger((int)idUnionIndex);
            switch (idUnionIndex)
            {
                case IdType.Numeric:
                    _writer.WriteInteger((uint)(value?.Identifier ?? 0u));
                    break;
                case IdType.String:
                    _writer.WriteString((string)value!.Identifier);
                    break;
                case IdType.Guid:
                    WriteGuid(null, (Guid)value!.Identifier);
                    break;
                case IdType.Opaque:
                    _writer.WriteBytes((byte[])value!.Identifier);
                    break;
            }
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            var serverIndex = value?.ServerIndex ?? 0;
            if (_serverMappings != null &&
                _serverMappings.Length > serverIndex)
            {
                serverIndex = _serverMappings[serverIndex];
            }

            //
            // Expanded Node id is a record extending NodeId.
            // Namespace, and union of via IdType which represents
            // the union discriminator.  After that we write the
            // namespace uri and then server index.
            //
            // ExpandedNode id is not nullable, i=0 is a null node id.
            //
            WriteNodeId(fieldName, value.ToNodeId(Context.NamespaceUris));
            _writer.WriteString(value?.NamespaceUri ?? string.Empty);
            _writer.WriteInteger(serverIndex);
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string? fieldName, StatusCode value)
        {
            _writer.WriteInteger(value.Code);
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfo(string? fieldName, DiagnosticInfo? value)
        {
            WriteDiagnosticInfoNullable(value, 0);
        }

        /// <inheritdoc/>
        public void WriteQualifiedName(string? fieldName, QualifiedName? value)
        {
            // Nullable qualified name
            if (WriteNullable(value))
            {
                return;
            }
            WriteQualifiedName(value);
        }

        /// <summary>
        /// Write non nullable qualified name
        /// </summary>
        /// <param name="value"></param>
        internal void WriteQualifiedName(QualifiedName value)
        {
            var namespaceIndex = value.NamespaceIndex;
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }

            _writer.WriteString(Context.NamespaceUris.GetString(namespaceIndex));
            _writer.WriteString(value.Name ?? string.Empty);
        }

        /// <inheritdoc/>
        public void WriteLocalizedText(string? fieldName, LocalizedText? value)
        {
            // Localized Text string is nullable
            if (WriteNullable(value))
            {
                return;
            }
            WriteLocalizedText(value);
        }

        /// <summary>
        /// Localized text
        /// </summary>
        /// <param name="value"></param>
        internal void WriteLocalizedText(LocalizedText value)
        {
            _writer.WriteString(value.Locale ?? string.Empty); // Nullable
            _writer.WriteString(value.Text ?? string.Empty); // Not nullable
        }

        /// <inheritdoc/>
        public void WriteVariant(string? fieldName, Variant value)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                WriteVariantValue(value);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <inheritdoc/>
        public void WriteDataValue(string? fieldName, DataValue? value)
        {
            // Data value is nullable
            if (WriteNullable(value))
            {
                return;
            }
            WriteDataValue(value);
        }

        /// <summary>
        /// Write non nullable data value
        /// </summary>
        /// <param name="value"></param>
        internal void WriteDataValue(DataValue value)
        {
            // record of fields
            WriteVariant(null, value.WrappedValue);
            WriteStatusCode(null, value.StatusCode);
            WriteDateTime(null, value.SourceTimestamp);
            _writer.WriteInteger(value.SourcePicoseconds);
            WriteDateTime(null, value.ServerTimestamp);
            _writer.WriteInteger(value.ServerPicoseconds);
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string? fieldName, ExtensionObject? value)
        {
            // Extension object is nullable
            if (WriteNullable(value))
            {
                return;
            }

            WriteExtensionObject(value);
        }

        /// <summary>
        /// Write extension object
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal void WriteExtensionObject(ExtensionObject value)
        {
            var encodeable = value.Body as IEncodeable;

            // write the type id.
            var typeId = value.TypeId;
            if (encodeable != null)
            {
                typeId = value.Encoding == ExtensionObjectEncoding.Xml
                    ? encodeable.XmlEncodingId : encodeable.BinaryEncodingId;
            }
            var localTypeId = ExpandedNodeId.ToNodeId(typeId, Context.NamespaceUris);
            if (NodeId.IsNull(localTypeId) && !NodeId.IsNull(typeId))
            {
                if (encodeable != null)
                {
                    throw ServiceResultException.Create(
                        StatusCodes.BadEncodingError,
    "Cannot encode bodies of type '{0}' in ExtensionObject unless the NamespaceUri ({1}) is in the encoder's NamespaceTable.",
                        encodeable.GetType().FullName,
                        typeId.NamespaceUri);
                }
                localTypeId = NodeId.Null;
            }

            // Extension objects are records of fields
            // 1. Encoding Node Id
            // 2. A union of
            //   1. null
            //   2. A encodeable type
            //   3. A record with
            //     1. ExtensionObjectEncoding type enum
            //     2. bytes that are either binary opc ua or xml/json utf 8

            // 1.
            WriteNodeId(null, localTypeId);

            // 2.1
            var body = value.Body;
            if (body is null)
            {
                _writer.WriteInteger(0);
                return;
            }

            // 2.2
            if (encodeable != null)
            {
                _writer.WriteInteger(1);
                encodeable.Encode(EncodeableEncoder);
                return;
            }

            // 2.3
            _writer.WriteInteger(2);
            // 2.3.1
            _writer.WriteInteger((int)value.Encoding);
            // 2.3.2
            switch (body)
            {
                case byte[] buffer:
                    // write binary bodies.
                    _writer.WriteBytes(buffer);
                    break;
                case XmlElement xml:
                    // write XML bodies.
                    WriteXmlElement(xml);
                    break;
                case string str:
                    // write json bodies.
                    _writer.WriteString(str);
                    break;
                default:
                    Debug.Fail("Should not get here");
                    break;
            }
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string? fieldName, IEncodeable? value,
            Type? systemType)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                // create a default object if a null object specified.
                if (value == null && systemType != null)
                {
                    value = Activator.CreateInstance(systemType) as IEncodeable;
                }
                // encode the object.
                value?.Encode(EncodeableEncoder);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string? fieldName, Enum? value)
        {
            _writer.WriteInteger(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            WriteNullableArray(values, v => WriteBoolean(null, v));
        }

        /// <inheritdoc/>
        public void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            WriteNullableArray(values, v => WriteSByte(null, v));
        }

        /// <inheritdoc/>
        public void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            WriteNullableArray(values, v => WriteByte(null, v));
        }

        /// <inheritdoc/>
        public void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            WriteNullableArray(values, v => WriteInt16(null, v));
        }

        /// <inheritdoc/>
        public void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            WriteNullableArray(values, v => WriteUInt16(null, v));
        }

        /// <inheritdoc/>
        public void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            WriteNullableArray(values, v => WriteInt32(null, v));
        }

        /// <inheritdoc/>
        public void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            WriteNullableArray(values, v => WriteUInt32(null, v));
        }

        /// <inheritdoc/>
        public void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            WriteNullableArray(values, v => WriteInt64(null, v));
        }

        /// <inheritdoc/>
        public void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            WriteNullableArray(values, v => WriteUInt64(null, v));
        }

        /// <inheritdoc/>
        public void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            WriteNullableArray(values, v => WriteFloat(null, v));
        }

        /// <inheritdoc/>
        public void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            WriteNullableArray(values, v => WriteDouble(null, v));
        }

        /// <inheritdoc/>
        public void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            WriteNullableArray(values, v => WriteString(null, v));
        }

        /// <inheritdoc/>
        public void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            WriteNullableArray(values, v => WriteDateTime(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            WriteNullableArray(values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            WriteNullableArray(values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            WriteNullableArray(values, v => WriteByteString(null, v));
        }

        /// <inheritdoc/>
        public void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            WriteNullableArray(values, v => WriteXmlElement(null, v));
        }

        /// <inheritdoc/>
        public void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            WriteNullableArray(values, v => WriteNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            WriteNullableArray(values, v => WriteExpandedNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            WriteNullableArray(values, v => WriteStatusCode(null, v));
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            WriteNullableArray(values, v => WriteDiagnosticInfo(null, v));
        }

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            WriteNullableArray(values, v => WriteQualifiedName(null, v));
        }

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            WriteNullableArray(values, v => WriteLocalizedText(null, v));
        }

        /// <inheritdoc/>
        public void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            WriteNullableArray(values, v => WriteVariant(null, v));
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            WriteNullableArray(values, v => WriteDataValue(null, v));
        }

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            WriteNullableArray(values, v => WriteExtensionObject(null, v));
        }

        /// <inheritdoc/>
        public void WriteEncodeableArray(string? fieldName,
            IList<IEncodeable>? values, Type? systemType)
        {
            WriteNullableArray(values, v => WriteEncodeable(null, v, systemType));
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string? fieldName,
            Array? values, Type systemType)
        {
            // Arrays are nullable
            if (WriteNullable(values))
            {
                return;
            }

            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < values.Length)
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingLimitsExceeded,
                    "MaxArrayLength {0} < {1}", Context.MaxArrayLength, values.Length);
            }

            // write length
            _writer.WriteInteger(values.Length);

            for (var index = 0; index < values.Length; index++)
            {
                WriteEnumerated(null, (Enum?)values.GetValue(index));
            }
        }

        /// <inheritdoc/>
        public void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            if (valueRank == ValueRanks.OneDimension)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        WriteArray((bool[])array, v => WriteBoolean(null, v));
                        break;
                    case BuiltInType.SByte:
                        WriteArray((sbyte[])array, v => WriteSByte(null, v));
                        break;
                    case BuiltInType.Byte:
                        WriteArray((byte[])array, v => WriteByte(null, v));
                        break;
                    case BuiltInType.Int16:
                        WriteArray((short[])array, v => WriteInt16(null, v));
                        break;
                    case BuiltInType.UInt16:
                        WriteArray((ushort[])array, v => WriteUInt16(null, v));
                        break;
                    case BuiltInType.Int32:
                        WriteArray((int[])array, v => WriteInt32(null, v));
                        break;
                    case BuiltInType.UInt32:
                        WriteArray((uint[])array, v => WriteUInt32(null, v));
                        break;
                    case BuiltInType.Int64:
                        WriteArray((long[])array, v => WriteInt64(null, v));
                        break;
                    case BuiltInType.UInt64:
                        WriteArray((ulong[])array, v => WriteUInt64(null, v));
                        break;
                    case BuiltInType.Float:
                        WriteArray((float[])array, v => WriteFloat(null, v));
                        break;
                    case BuiltInType.Double:
                        WriteArray((double[])array, v => WriteDouble(null, v));
                        break;
                    case BuiltInType.DateTime:
                        WriteArray((DateTime[])array, v => WriteDateTime(null, v));
                        break;
                    case BuiltInType.Guid:
                        WriteArray((Uuid[])array, v => WriteGuid(null, v));
                        break;
                    case BuiltInType.String:
                        WriteArray((string[])array, v => WriteString(null, v));
                        break;
                    case BuiltInType.ByteString:
                        WriteArray((byte[][])array, v => WriteByteString(null, v));
                        break;
                    case BuiltInType.QualifiedName:
                        WriteArray((QualifiedName[])array, v => WriteQualifiedName(null, v));
                        break;
                    case BuiltInType.LocalizedText:
                        WriteArray((LocalizedText[])array, v => WriteLocalizedText(null, v));
                        break;
                    case BuiltInType.NodeId:
                        WriteArray((NodeId[])array, v => WriteNodeId(null, v));
                        break;
                    case BuiltInType.ExpandedNodeId:
                        WriteArray((ExpandedNodeId[])array, v => WriteExpandedNodeId(null, v));
                        break;
                    case BuiltInType.StatusCode:
                        WriteArray((StatusCode[])array, v => WriteStatusCode(null, v));
                        break;
                    case BuiltInType.XmlElement:
                        WriteArray((XmlElement[])array, v => WriteXmlElement(null, v));
                        break;
                    case BuiltInType.Variant:
                        {
                            // try to write IEncodeable Array
                            if (array is IEncodeable[] encodeableArray)
                            {
                                WriteEncodeableArray(fieldName, encodeableArray,
                                    array.GetType().GetElementType());
                                return;
                            }
                            WriteVariantArray(null, (Variant[])array);
                            break;
                        }
                    case BuiltInType.Enumeration:
                        var ints = array as int[];
                        if (ints == null && array is Enum[] enums)
                        {
                            ints = new int[enums.Length];
                            for (var ii = 0; ii < enums.Length; ii++)
                            {
                                ints[ii] = Convert.ToInt32(enums[ii],
                                    CultureInfo.InvariantCulture);
                            }
                        }
                        if (ints != null)
                        {
                            WriteArray(ints, v => WriteInt32(null, v));
                        }
                        else
                        {
                            throw ServiceResultException.Create(
                                StatusCodes.BadEncodingError,
                                "Unexpected type encountered while encoding an Enumeration Array.");
                        }
                        break;
                    case BuiltInType.ExtensionObject:
                        WriteArray((ExtensionObject[])array, v => WriteExtensionObject(null, v));
                        break;
                    case BuiltInType.DiagnosticInfo:
                        WriteArray((DiagnosticInfo[])array, v => WriteDiagnosticInfo(null, v));
                        break;
                    case BuiltInType.DataValue:
                        WriteArray((DataValue[])array, v => WriteDataValue(null, v));
                        break;
                    default:
                        {
                            // try to write IEncodeable Array
                            if (array is IEncodeable[] encodeableArray)
                            {
                                WriteEncodeableArray(fieldName, encodeableArray,
                                    array.GetType().GetElementType());
                                return;
                            }
                            if (array == null)
                            {
                                // write zero dimension
                                WriteInt32(null, -1);
                                return;
                            }
                            throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                                "Unexpected type encountered while encoding an Array with BuiltInType: {0}",
                                builtInType);
                        }
                }
            }
            else if (valueRank > ValueRanks.OneDimension)
            {
                /* Multi-dimensional Arrays are encoded as an Int32 Array containing the dimensions followed by
                 * a list of all the values in the Array. The total number of values is equal to the
                 * product of the dimensions.
                 * The number of values is 0 if one or more dimension is less than or equal to 0.*/

                var matrix = array as Matrix;
                if (matrix == null)
                {
                    if (array is not Array multiArray || multiArray.Rank != valueRank)
                    {
                        // there is no Dimensions to write
                        WriteInt32(null, -1);
                        return;
                    }
                    matrix = new Matrix(multiArray, builtInType);
                }

                // Write the Dimensions
                WriteInt32Array(null, (int[])matrix.Dimensions);

                switch (matrix.TypeInfo.BuiltInType)
                {
                    case BuiltInType.Boolean:
                        {
                            var values = (bool[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteBoolean(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.SByte:
                        {
                            var values = (sbyte[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteSByte(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Byte:
                        {
                            var values = (byte[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteByte(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Int16:
                        {
                            var values = (short[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteInt16(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.UInt16:
                        {
                            var values = (ushort[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteUInt16(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Enumeration:
                        {
                            if (matrix.Elements is Enum[] values)
                            {
                                for (var ii = 0; ii < values.Length; ii++)
                                {
                                    WriteEnumerated(null, values[ii]);
                                }
                                break;
                            }
                            goto case BuiltInType.Int32;
                        }
                    case BuiltInType.Int32:
                        {
                            var values = (int[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteInt32(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.UInt32:
                        {
                            var values = (uint[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteUInt32(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Int64:
                        {
                            var values = (long[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteInt64(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.UInt64:
                        {
                            var values = (ulong[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteUInt64(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Float:
                        {
                            var values = (float[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteFloat(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Double:
                        {
                            var values = (double[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteDouble(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.String:
                        {
                            var values = (string[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteString(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.DateTime:
                        {
                            var values = (DateTime[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteDateTime(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Guid:
                        {
                            var values = (Uuid[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteGuid(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.ByteString:
                        {
                            var values = (byte[][])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteByteString(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.XmlElement:
                        {
                            var values = (XmlElement[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteXmlElement(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.NodeId:
                        {
                            var values = (NodeId[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteNodeId(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.ExpandedNodeId:
                        {
                            var values = (ExpandedNodeId[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteExpandedNodeId(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.StatusCode:
                        {
                            var values = (StatusCode[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteStatusCode(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.QualifiedName:
                        {
                            var values = (QualifiedName[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteQualifiedName(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.LocalizedText:
                        {
                            var values = (LocalizedText[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteLocalizedText(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.ExtensionObject:
                        {
                            var values = (ExtensionObject[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteExtensionObject(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.DataValue:
                        {
                            var values = (DataValue[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteDataValue(null, values[ii]);
                            }
                            break;
                        }
                    case BuiltInType.Variant:
                        {
                            if (matrix.Elements is Variant[] variants)
                            {
                                for (var ii = 0; ii < variants.Length; ii++)
                                {
                                    WriteVariant(null, variants[ii]);
                                }
                                break;
                            }

                            // try to write IEncodeable Array
                            if (matrix.Elements is IEncodeable[] encodeableArray)
                            {
                                for (var ii = 0; ii < encodeableArray.Length; ii++)
                                {
                                    WriteEncodeable(null, encodeableArray[ii], null);
                                }
                                break;
                            }

                            if (matrix.Elements is object[] objects)
                            {
                                for (var ii = 0; ii < objects.Length; ii++)
                                {
                                    WriteVariant(null, new Variant(objects[ii]));
                                }
                                break;
                            }
                            throw ServiceResultException.Create(
                                StatusCodes.BadEncodingError,
                                "Unexpected type encountered while encoding a Matrix.");
                        }
                    case BuiltInType.DiagnosticInfo:
                        {
                            var values = (DiagnosticInfo[])matrix.Elements;
                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                WriteDiagnosticInfo(null, values[ii]);
                            }
                            break;
                        }
                    default:
                        {
                            // try to write IEncodeable Array
                            if (matrix.Elements is IEncodeable[] encodeableArray)
                            {
                                for (var ii = 0; ii < encodeableArray.Length; ii++)
                                {
                                    WriteEncodeable(null, encodeableArray[ii], null);
                                }
                                break;
                            }
                            throw ServiceResultException.Create(
                                StatusCodes.BadEncodingError,
                                "Unexpected type encountered while encoding a Matrix with BuiltInType: {0}",
                                matrix.TypeInfo.BuiltInType);
                        }
                }
            }
        }

        /// <summary>
        /// Writes a DiagnosticInfo to the stream.
        /// Ignores InnerDiagnosticInfo field if the nesting level
        /// <see cref="DiagnosticInfo.MaxInnerDepth"/> is exceeded.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="depth"></param>
        private void WriteDiagnosticInfoNullable(DiagnosticInfo? value, int depth)
        {
            if (depth >= DiagnosticInfo.MaxInnerDepth)
            {
                value = null;
            }

            // Diagnostic info is always nullable
            if (WriteNullable(value))
            {
                return;
            }
            WriteDiagnosticInfo(value, depth);
        }

        /// <summary>
        /// Write as non nullable
        /// </summary>
        /// <param name="value"></param>
        /// <param name="depth"></param>
        private void WriteDiagnosticInfo(DiagnosticInfo value, int depth = 0)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                _writer.WriteInteger(value.SymbolicId);
                _writer.WriteInteger(value.NamespaceUri);
                _writer.WriteInteger(value.Locale);
                _writer.WriteInteger(value.LocalizedText);
                _writer.WriteString(value.AdditionalInfo ?? string.Empty);
                WriteStatusCode(null, value.InnerStatusCode);
                WriteDiagnosticInfoNullable(value.InnerDiagnosticInfo, depth + 1);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <summary>
        /// Writes an Variant to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="ServiceResultException"></exception>
        private void WriteVariantValue(Variant value)
        {
            // always write the dimensions of the variant.
            // In case of a scalar value this will be an empty
            // array which consumes 1 byte (length 0 zig zag encoded).
            Matrix? matrix = null;
            var valueToEncode = value.Value;
            if (value.TypeInfo?.ValueRank > 1)
            {
                matrix = (Matrix?)valueToEncode;
                valueToEncode = matrix?.Elements;
            }
            WriteArray(matrix?.Dimensions ?? Array.Empty<int>(),
                v => WriteInt32(null, v));

            // Shortcut here to write null
            if (valueToEncode == null)
            {
                _writer.WriteInteger(0);
                return;
            }

            if (value.TypeInfo!.ValueRank < 0)
            {
                // Write union discriminator for scalar
                _writer.WriteInteger(ToUnionId(value.TypeInfo.BuiltInType));
                switch (value.TypeInfo.BuiltInType)
                {
                    case BuiltInType.Boolean:
                        WriteBoolean(null, (bool)valueToEncode);
                        return;
                    case BuiltInType.SByte:
                        WriteSByte(null, (sbyte)valueToEncode);
                        return;
                    case BuiltInType.Byte:
                        WriteByte(null, (byte)valueToEncode);
                        return;
                    case BuiltInType.Int16:
                        WriteInt16(null, (short)valueToEncode);
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16(null, (ushort)valueToEncode);
                        return;
                    case BuiltInType.Int32:
                        WriteInt32(null, (int)valueToEncode);
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32(null, (uint)valueToEncode);
                        return;
                    case BuiltInType.Int64:
                        WriteInt64(null, (long)valueToEncode);
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64(null, (ulong)valueToEncode);
                        return;
                    case BuiltInType.Float:
                        WriteFloat(null, (float)valueToEncode);
                        return;
                    case BuiltInType.Double:
                        WriteDouble(null, (double)valueToEncode);
                        return;
                    case BuiltInType.String:
                        _writer.WriteString((string)valueToEncode);
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTime(null, (DateTime)valueToEncode);
                        return;
                    case BuiltInType.Guid:
                        WriteGuid(null, (Uuid)valueToEncode);
                        return;
                    case BuiltInType.ByteString:
                        _writer.WriteBytes((byte[])valueToEncode);
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElement((XmlElement)valueToEncode);
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeId(null, (NodeId)valueToEncode);
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeId(null, (ExpandedNodeId)valueToEncode);
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCode(null, (StatusCode)valueToEncode);
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedName((QualifiedName)valueToEncode);
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedText((LocalizedText)valueToEncode);
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObject((ExtensionObject)valueToEncode);
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValue((DataValue)valueToEncode);
                        return;
                    case BuiltInType.Enumeration:
                        WriteInt32(null, Convert.ToInt32(valueToEncode,
                            CultureInfo.InvariantCulture));
                        return;
                    case BuiltInType.DiagnosticInfo:
                        WriteDiagnosticInfo((DiagnosticInfo)valueToEncode);
                        return;
                }

                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingError,
                    "Unexpected type encountered while encoding a Variant: {0}",
                    value.TypeInfo.BuiltInType);
            }

            if (value.TypeInfo.ValueRank >= 0)
            {
                // Write union discriminator for array
                _writer.WriteInteger(ToUnionId(value.TypeInfo.BuiltInType, true));
                switch (value.TypeInfo.BuiltInType)
                {
                    case BuiltInType.Boolean:
                        WriteArray((bool[])valueToEncode, v => WriteBoolean(null, v));
                        break;
                    case BuiltInType.SByte:
                        WriteArray((sbyte[])valueToEncode, v => WriteSByte(null, v));
                        break;
                    case BuiltInType.Byte:
                        WriteArray((byte[])valueToEncode, v => WriteByte(null, v));
                        break;
                    case BuiltInType.Int16:
                        WriteArray((short[])valueToEncode, v => WriteInt16(null, v));
                        break;
                    case BuiltInType.UInt16:
                        WriteArray((ushort[])valueToEncode, v => WriteUInt16(null, v));
                        break;
                    case BuiltInType.Int32:
                        WriteArray((int[])valueToEncode, v => WriteInt32(null, v));
                        break;
                    case BuiltInType.UInt32:
                        WriteArray((uint[])valueToEncode, v => WriteUInt32(null, v));
                        break;
                    case BuiltInType.Int64:
                        WriteArray((long[])valueToEncode, v => WriteInt64(null, v));
                        break;
                    case BuiltInType.UInt64:
                        WriteArray((ulong[])valueToEncode, v => WriteUInt64(null, v));
                        break;
                    case BuiltInType.Float:
                        WriteArray((float[])valueToEncode, v => WriteFloat(null, v));
                        break;
                    case BuiltInType.Double:
                        WriteArray((double[])valueToEncode, v => WriteDouble(null, v));
                        break;
                    case BuiltInType.String:
                        WriteArray((string[])valueToEncode, v => WriteString(null, v));
                        break;
                    case BuiltInType.DateTime:
                        WriteArray((DateTime[])valueToEncode, v => WriteDateTime(null, v));
                        break;
                    case BuiltInType.Guid:
                        WriteArray((Uuid[])valueToEncode, v => WriteGuid(null, v));
                        break;
                    case BuiltInType.ByteString:
                        WriteArray((byte[][])valueToEncode, v => WriteByteString(null, v));
                        break;
                    case BuiltInType.XmlElement:
                        WriteArray((XmlElement[])valueToEncode, v => WriteXmlElement(null, v));
                        break;
                    case BuiltInType.NodeId:
                        WriteArray((NodeId[])valueToEncode, v => WriteNodeId(null, v));
                        break;
                    case BuiltInType.ExpandedNodeId:
                        WriteArray((ExpandedNodeId[])valueToEncode, v => WriteExpandedNodeId(null, v));
                        break;
                    case BuiltInType.StatusCode:
                        WriteArray((StatusCode[])valueToEncode, v => WriteStatusCode(null, v));
                        break;
                    case BuiltInType.QualifiedName:
                        WriteArray((QualifiedName[])valueToEncode, v => WriteQualifiedName(null, v));
                        break;
                    case BuiltInType.LocalizedText:
                        WriteArray((LocalizedText[])valueToEncode, v => WriteLocalizedText(null, v));
                        break;
                    case BuiltInType.ExtensionObject:
                        WriteArray((ExtensionObject[])valueToEncode, v => WriteExtensionObject(null, v));
                        break;
                    case BuiltInType.DataValue:
                        WriteArray((DataValue[])valueToEncode, v => WriteDataValue(null, v));
                        break;
                    case BuiltInType.Enumeration:
                        // Check whether the value to encode is int array.
                        if (valueToEncode is not int[] ints)
                        {
                            if (valueToEncode is not Enum[] enums)
                            {
                                throw new ServiceResultException(
                                    StatusCodes.BadEncodingError,
                                    Opc.Ua.Utils.Format(
                                        "Type '{0}' is not allowed in an Enumeration.",
                                        value.GetType().FullName));
                            }
                            ints = new int[enums.Length];
                            for (var ii = 0; ii < enums.Length; ii++)
                            {
                                ints[ii] = (int)(object)enums[ii];
                            }
                        }
                        WriteArray(ints, v => WriteInt32(null, v));
                        break;
                    case BuiltInType.Variant:
                        if (valueToEncode is Variant[] variants)
                        {
                            WriteArray(variants, v => WriteVariant(null, v));
                            break;
                        }
                        if (valueToEncode is object[] objects)
                        {
                            WriteArray(objects, v => WriteVariant(null, new Variant(v)));
                            break;
                        }
                        throw ServiceResultException.Create(
                            StatusCodes.BadEncodingError,
                            "Unexpected type encountered while encoding a Matrix: {0}",
                            valueToEncode.GetType());
                    case BuiltInType.DiagnosticInfo:
                        WriteArray((DiagnosticInfo[])valueToEncode,
                            v => WriteDiagnosticInfo(null, v));
                        break;
                    default:
                        throw ServiceResultException.Create(
                            StatusCodes.BadEncodingError,
                            "Unexpected type encountered while encoding a Variant: {0}",
                            value.TypeInfo.BuiltInType);
                }
            }
            static int ToUnionId(BuiltInType builtInType, bool isArray = false)
            {
                // TODO: Decide whether the opc ua types are records with single field
                return (isArray, builtInType) switch
                {
                    (false, BuiltInType.Null) => 0,
                    (false, BuiltInType.Boolean) => 1,
                    (false, BuiltInType.SByte) => 2,
                    (false, BuiltInType.Byte) => 3,
                    (false, BuiltInType.Int16) => 4,
                    (false, BuiltInType.UInt16) => 5,
                    (false, BuiltInType.Int32) => 6,
                    (false, BuiltInType.UInt32) => 7,
                    (false, BuiltInType.Int64) => 8,
                    (false, BuiltInType.UInt64) => 8,
                    (false, BuiltInType.Float) => 10,
                    (false, BuiltInType.Double) => 11,
                    (false, BuiltInType.String) => 12,
                    (false, BuiltInType.DateTime) => 13,
                    (false, BuiltInType.Guid) => 14,
                    (false, BuiltInType.ByteString) => 15,
                    (false, BuiltInType.XmlElement) => 16,
                    (false, BuiltInType.NodeId) => 17,
                    (false, BuiltInType.ExpandedNodeId) => 18,
                    (false, BuiltInType.StatusCode) => 19,
                    (false, BuiltInType.QualifiedName) => 20,
                    (false, BuiltInType.LocalizedText) => 21,
                    (false, BuiltInType.ExtensionObject) => 22,
                    (false, BuiltInType.DataValue) => 23,
                    (false, BuiltInType.DiagnosticInfo) => 24,
                    (false, BuiltInType.Number) => 25,
                    (false, BuiltInType.Integer) => 26,
                    (false, BuiltInType.UInteger) => 27,
                    (false, BuiltInType.Enumeration) => 28,
                    (true, BuiltInType.Boolean) => 29,
                    (true, BuiltInType.SByte) => 30,
                    (true, BuiltInType.Byte) => 31,
                    (true, BuiltInType.Int16) => 32,
                    (true, BuiltInType.UInt16) => 33,
                    (true, BuiltInType.Int32) => 34,
                    (true, BuiltInType.UInt32) => 35,
                    (true, BuiltInType.Int64) => 36,
                    (true, BuiltInType.UInt64) => 37,
                    (true, BuiltInType.Float) => 38,
                    (true, BuiltInType.Double) => 39,
                    (true, BuiltInType.String) => 40,
                    (true, BuiltInType.DateTime) => 41,
                    (true, BuiltInType.Guid) => 42,
                    (true, BuiltInType.ByteString) => 43,
                    (true, BuiltInType.XmlElement) => 44,
                    (true, BuiltInType.NodeId) => 45,
                    (true, BuiltInType.ExpandedNodeId) => 46,
                    (true, BuiltInType.StatusCode) => 47,
                    (true, BuiltInType.QualifiedName) => 48,
                    (true, BuiltInType.LocalizedText) => 49,
                    (true, BuiltInType.ExtensionObject) => 50,
                    (true, BuiltInType.DataValue) => 51,
                    (true, BuiltInType.Variant) => 52,
                    (true, BuiltInType.DiagnosticInfo) => 53,
                    (true, BuiltInType.Number) => 54,
                    (true, BuiltInType.Integer) => 55,
                    (true, BuiltInType.UInteger) => 56,
                    (true, BuiltInType.Enumeration) => 57,

                    _ => throw ServiceResultException.Create(
                        StatusCodes.BadEncodingError, "Type not allowed"),
                };
            }
        }

        /// <inheritdoc/>
        public void WriteNullableArray<T>(IList<T>? values, Action<T> writer)
        {
            // All arrays can be nullable
            if (WriteNullable(values))
            {
                return;
            }

            WriteArray(values, writer);
        }

        /// <inheritdoc/>
        public void WriteArray<T>(IList<T> values, Action<T> writer)
        {
            // Arrays are nullable, otherwise write length
            if (WriteArrayLength(values))
            {
                return;
            }

            // write contents.
            foreach (var value in values)
            {
                writer(value);
            }
        }

        /// <summary>
        /// Write the length of an array. Returns true if the
        /// array is empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <exception cref="ServiceResultException"></exception>
        private bool WriteArrayLength<T>(
            [NotNullWhen(false)] ICollection<T> values)
        {
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < values.Count)
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingLimitsExceeded,
                    "MaxArrayLength {0} < {1}", Context.MaxArrayLength, values.Count);
            }

            // Arrays are nullable, otherwise write length
            _writer.WriteInteger(values.Count);
            return values.Count == 0;
        }

        /// <summary>
        /// Test and increment the nesting level.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private void CheckAndIncrementNestingLevel()
        {
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw ServiceResultException.Create(StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }
            _nestingLevel++;
        }

        private readonly AvroWriter _writer;
        private readonly bool _leaveOpen;
        private ushort[]? _namespaceMappings;
        private ushort[]? _serverMappings;
        private uint _nestingLevel;
    }
}
