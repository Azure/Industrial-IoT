// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using System;
    using System.Buffers.Binary;
    using System.Collections.Frozen;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Encodes objects in a stream using Avro binary encoding.
    /// </summary>
    public abstract class BaseAvroEncoder : IEncoder
    {
        /// <inheritdoc/>
        public EncodingType EncodingType => (EncodingType)3;

        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <inheritdoc/>
        public bool UseReversibleEncoding => true;

        /// <summary>
        /// Creates an encoder that writes to the stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        /// <param name="leaveOpen">If the stream should
        /// be left open on dispose.</param>
        protected BaseAvroEncoder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = true)
        {
            _writer = new AvroBinaryWriter(stream, leaveOpen);
            Context = context;
            _nestingLevel = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
        public virtual void WriteBoolean(string? fieldName, bool value)
        {
            _writer.WriteBoolean(value);
        }

        /// <inheritdoc/>
        public virtual void WriteSByte(string? fieldName, sbyte value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteByte(string? fieldName, byte value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteInt16(string? fieldName, short value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteUInt16(string? fieldName, ushort value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteInt32(string? fieldName, int value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteUInt32(string? fieldName, uint value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteInt64(string? fieldName, long value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteUInt64(string? fieldName, ulong value)
        {
            // ulong is a union of long and fixed (> long.max)
            var unionSelector = value < long.MaxValue ? 0 : 1;
            _writer.WriteInteger(unionSelector);
            if (unionSelector == 0)
            {
                _writer.WriteInteger((long)value);
            }
            else
            {
                Span<byte> bytes = stackalloc byte[sizeof(ulong)];
                BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
                _writer.WriteFixed(bytes);
            }
        }

        /// <inheritdoc/>
        public virtual void WriteFloat(string? fieldName, float value)
        {
            _writer.WriteFloat(value);
        }

        /// <inheritdoc/>
        public virtual void WriteDouble(string? fieldName, double value)
        {
            _writer.WriteDouble(value);
        }

        /// <inheritdoc/>
        public virtual void WriteDateTime(string? fieldName, DateTime value)
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
        public virtual void WriteGuid(string? fieldName, Uuid value)
        {
#if UUID_FIXED
            _writer.WriteFixed(((Guid)value).ToByteArray());
#else
            _writer.WriteString(((Guid)value).ToString());
#endif
        }

        /// <inheritdoc/>
        public virtual void WriteGuid(string? fieldName, Guid value)
        {
#if UUID_FIXED
            _writer.WriteFixed(value.ToByteArray());
#else
            _writer.WriteString(value.ToString());
#endif
        }

        /// <inheritdoc/>
        public virtual void WriteString(string? fieldName, string? value)
        {
            if (value == null)
            {
                _writer.WriteInteger(0);
                return;
            }
            _writer.WriteString(value);
        }

        /// <inheritdoc/>
        public virtual void WriteByteString(string? fieldName, byte[]? value)
        {
            if (value == null)
            {
                _writer.WriteInteger(0);
                return;
            }

            if (Context.MaxByteStringLength > 0 &&
                Context.MaxByteStringLength < value.Length)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxByteStringLength {Context.MaxByteStringLength} < {value.Length}.");
            }
            _writer.WriteBytes(value);
        }

        /// <inheritdoc/>
        public virtual void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            _writer.WriteString(value?.OuterXml ?? string.Empty);
        }

        /// <inheritdoc/>
        public virtual void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            var fieldContentMask = dataSet.DataSetFieldContentMask;
            if ((fieldContentMask.HasFlag(DataSetFieldContentFlags.RawData)) ||
                fieldContentMask == 0)
            {
                foreach (var value in dataSet)
                {
                    WriteVariant(value.Key, value.Value?.WrappedValue ?? default);
                }
            }
            else
            {
                foreach (var value in dataSet)
                {
                    WriteNullable(value.Key, value.Value, WriteDataValue);
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteObject(string? fieldName, string? typeName, Action writer)
        {
            writer();
        }

        /// <inheritdoc/>
        public virtual void WriteNodeId(string? fieldName, NodeId? value)
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

            var namespaceUri = namespaceIndex == 0 ? null :
                Context.NamespaceUris.GetString(namespaceIndex);
            WriteNodeId(value, namespaceUri ?? string.Empty);
        }

        /// <summary>
        /// Write node id
        /// </summary>
        /// <param name="value"></param>
        /// <param name="namespaceUri"></param>
        private void WriteNodeId(NodeId? value, string namespaceUri)
        {
            const string kIdentifierName = "Identifier";
            WriteString("Namespace", namespaceUri);
            //  Numeric = 0,
            //  String = 1
            //  Guid = 2
            //  Opaque = 3
            var idUnionIndex = (int)(value?.IdType ?? IdType.Numeric);
            WriteUnion(kIdentifierName, idUnionIndex, id =>
            {
                switch ((IdType)id)
                {
                    case IdType.Numeric:
                        WriteUInt32(kIdentifierName, (uint)(value?.Identifier ?? 0u));
                        break;
                    case IdType.String:
                        WriteString(kIdentifierName, (string)value!.Identifier);
                        break;
                    case IdType.Guid:
                        WriteGuid(kIdentifierName, (Guid)value!.Identifier);
                        break;
                    case IdType.Opaque:
                        WriteByteString(kIdentifierName, (byte[])value!.Identifier);
                        break;
                }
            });
        }

        /// <inheritdoc/>
        public virtual void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            var serverIndex = value?.ServerIndex ?? 0;
            if (_serverMappings != null &&
                _serverMappings.Length > serverIndex)
            {
                serverIndex = _serverMappings[serverIndex];
            }
            var serverUri = serverIndex == 0 ? null :
                Context.ServerUris.GetString(serverIndex);

            var namespaceUri = value?.NamespaceUri;
            if (namespaceUri == null && value != null &&
                value.NamespaceIndex != 0)
            {
                var namespaceIndex = value.NamespaceIndex;
                if (_namespaceMappings != null &&
                    _namespaceMappings.Length > namespaceIndex)
                {
                    namespaceIndex = _namespaceMappings[namespaceIndex];
                }
                namespaceUri = namespaceIndex == 0 ? null :
                    Context.NamespaceUris.GetString(namespaceIndex);
            }

            //
            // Expanded Node id is a record extending NodeId.
            // Namespace, and union of via IdType which represents
            // the union discriminator.  After that we write the
            // namespace uri and then server index.
            //
            WriteNodeId(value.ToNodeId(Context.NamespaceUris, true),
                namespaceUri ?? string.Empty);
            WriteString("ServerUri", serverUri);
        }

        /// <inheritdoc/>
        public virtual void WriteStatusCode(string? fieldName, StatusCode value)
        {
            _writer.WriteInteger(value.Code);
        }

        /// <inheritdoc/>
        public virtual void WriteDiagnosticInfo(string? fieldName, DiagnosticInfo? value)
        {
            WriteDiagnosticInfo(fieldName, value ?? new DiagnosticInfo(), 0);
        }

        /// <inheritdoc/>
        public virtual void WriteQualifiedName(string? fieldName, QualifiedName? value)
        {
            var namespaceIndex = value?.NamespaceIndex ?? 0;
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }

            var namespaceUri = namespaceIndex == 0 ? null :
                Context.NamespaceUris.GetString(namespaceIndex);
            _writer.WriteString(namespaceUri ?? string.Empty);
            _writer.WriteString(value?.Name ?? string.Empty);
        }

        /// <inheritdoc/>
        public virtual void WriteLocalizedText(string? fieldName, LocalizedText? value)
        {
            _writer.WriteString(value?.Locale ?? string.Empty);
            _writer.WriteString(value?.Text ?? string.Empty);
        }

        /// <inheritdoc/>
        public virtual void WriteVariant(string? fieldName, Variant value)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                // Write union index
                int unionId;
                var rank = SchemaRank.Scalar;
                if (value.Value == null)
                {
                    unionId = 0;
                }
                else
                {
                    rank = SchemaUtils.GetRank(value.TypeInfo!.ValueRank);
                    unionId = ToUnionId(value.TypeInfo.BuiltInType, rank);
                }
                WriteUnion(fieldName, unionId, _ => WriteVariantValue(value,
                    value.TypeInfo?.BuiltInType ?? BuiltInType.Null, rank));
            }
            finally
            {
                _nestingLevel--;
            }

            static int ToUnionId(BuiltInType builtInType, SchemaRank valueRank)
            {
                if (!_variableUnionId.TryGetValue((valueRank, builtInType),
                    out var unionId))
                {
                    throw new EncodingException(
                        "Invalid built in type or value rank for variant");
                }
                return unionId;
            }
        }

        /// <inheritdoc/>
        public virtual void WriteDataValue(string? fieldName, DataValue? value)
        {
            // record of fields
            value ??= new DataValue();
            WriteVariant(nameof(value.Value), value.WrappedValue);
            WriteStatusCode(nameof(value.StatusCode), value.StatusCode);
            WriteDateTime(nameof(value.SourceTimestamp), value.SourceTimestamp);
            WriteUInt16(nameof(value.SourcePicoseconds), value.SourcePicoseconds);
            WriteDateTime(nameof(value.ServerTimestamp), value.ServerTimestamp);
            WriteUInt16(nameof(value.ServerPicoseconds), value.ServerPicoseconds);
        }

        /// <inheritdoc/>
        public virtual void WriteExtensionObject(string? fieldName, ExtensionObject? value)
        {
            value ??= new ExtensionObject();
            // Write a raw encoded data type of the schema union
            WriteUnion(fieldName, 0, _ => WriteEncodedDataType(fieldName, value));
        }

        /// <inheritdoc/>
        public void EncodeMessage(IEncodeable message)
        {
            message.Encode(this);
        }

        /// <inheritdoc/>
        public virtual void WriteEncodeable(string? fieldName, IEncodeable? value,
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
                value?.Encode(this);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <inheritdoc/>
        public virtual void WriteEnumerated(string? fieldName, Enum? value)
        {
            _writer.WriteInteger(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public virtual void WriteEnumerated(string? fieldName, int value)
        {
            _writer.WriteInteger(value);
        }

        /// <inheritdoc/>
        public virtual void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            WriteArray(fieldName, values, v => WriteBoolean(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            WriteArray(fieldName, values, v => WriteSByte(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            if (values == null || values.Count == 0)
            {
                _writer.WriteInteger(0);
                return;
            }

            if (Context.MaxByteStringLength > 0 &&
                Context.MaxByteStringLength < values.Count)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxByteStringLength {Context.MaxByteStringLength} < {values.Count}.");
            }
            // Byte array is written as byte string
            _writer.WriteBytes(values.ToArray());
        }

        /// <inheritdoc/>
        public virtual void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            WriteArray(fieldName, values, v => WriteInt16(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            WriteArray(fieldName, values, v => WriteUInt16(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            WriteArray(fieldName, values, v => WriteInt32(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            WriteArray(fieldName, values, v => WriteUInt32(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            WriteArray(fieldName, values, v => WriteInt64(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            WriteArray(fieldName, values, v => WriteUInt64(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            WriteArray(fieldName, values, v => WriteFloat(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            WriteArray(fieldName, values, v => WriteDouble(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            WriteArray(fieldName, values, v => WriteString(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            WriteArray(fieldName, values, v => WriteDateTime(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            WriteArray(fieldName, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            WriteArray(fieldName, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            WriteArray(fieldName, values, v => WriteByteString(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            WriteArray(fieldName, values, v => WriteXmlElement(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            WriteArray(fieldName, values, v => WriteNodeId(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            WriteArray(fieldName, values, v => WriteExpandedNodeId(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            WriteArray(fieldName, values, v => WriteStatusCode(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            WriteArray(fieldName, values, v => WriteDiagnosticInfo(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            WriteArray(fieldName, values, v => WriteQualifiedName(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            WriteArray(fieldName, values, v => WriteLocalizedText(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            WriteArray(fieldName, values, v => WriteVariant(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            WriteArray(fieldName, values, v => WriteDataValue(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            WriteArray(fieldName, values, v => WriteExtensionObject(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteEncodeableArray(string? fieldName,
            IList<IEncodeable>? values, Type? systemType)
        {
            WriteArray(fieldName, values, v => WriteEncodeable(null, v, systemType),
                systemType?.Name);
        }

        /// <inheritdoc/>
        public virtual void WriteEnumeratedArray(string? fieldName,
            Array? values, Type? systemType)
        {
            WriteArray(fieldName, values, v => WriteEnumerated(null, (Enum?)v));
        }

        /// <inheritdoc/>
        public virtual void WriteEnumeratedArray(string? fieldName,
            int[] values, Type? enumType)
        {
            WriteArray(fieldName, values, v => WriteEnumerated(null, v));
        }

        /// <inheritdoc/>
        public virtual void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            var rank = SchemaUtils.GetRank(valueRank);
            if (rank == SchemaRank.Collection)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        WriteArray(fieldName, (bool[])array, v => WriteBoolean(null, v));
                        break;
                    case BuiltInType.SByte:
                        WriteArray(fieldName, (sbyte[])array, v => WriteSByte(null, v));
                        break;
                    case BuiltInType.Byte:
                        WriteArray(fieldName, (byte[])array, v => WriteByte(null, v));
                        break;
                    case BuiltInType.Int16:
                        WriteArray(fieldName, (short[])array, v => WriteInt16(null, v));
                        break;
                    case BuiltInType.UInt16:
                        WriteArray(fieldName, (ushort[])array, v => WriteUInt16(null, v));
                        break;
                    case BuiltInType.Int32:
                        WriteArray(fieldName, (int[])array, v => WriteInt32(null, v));
                        break;
                    case BuiltInType.UInt32:
                        WriteArray(fieldName, (uint[])array, v => WriteUInt32(null, v));
                        break;
                    case BuiltInType.Int64:
                        WriteArray(fieldName, (long[])array, v => WriteInt64(null, v));
                        break;
                    case BuiltInType.UInt64:
                        WriteArray(fieldName, (ulong[])array, v => WriteUInt64(null, v));
                        break;
                    case BuiltInType.Float:
                        WriteArray(fieldName, (float[])array, v => WriteFloat(null, v));
                        break;
                    case BuiltInType.Double:
                        WriteArray(fieldName, (double[])array, v => WriteDouble(null, v));
                        break;
                    case BuiltInType.DateTime:
                        WriteArray(fieldName, (DateTime[])array, v => WriteDateTime(null, v));
                        break;
                    case BuiltInType.Guid:
                        WriteArray(fieldName, (Uuid[])array, v => WriteGuid(null, v));
                        break;
                    case BuiltInType.String:
                        WriteArray(fieldName, (string[])array, v => WriteString(null, v));
                        break;
                    case BuiltInType.ByteString:
                        WriteArray(fieldName, (byte[][])array, v => WriteByteString(null, v));
                        break;
                    case BuiltInType.QualifiedName:
                        WriteArray(fieldName, (QualifiedName[])array, v => WriteQualifiedName(null, v));
                        break;
                    case BuiltInType.LocalizedText:
                        WriteArray(fieldName, (LocalizedText[])array, v => WriteLocalizedText(null, v));
                        break;
                    case BuiltInType.NodeId:
                        WriteArray(fieldName, (NodeId[])array, v => WriteNodeId(null, v));
                        break;
                    case BuiltInType.ExpandedNodeId:
                        WriteArray(fieldName, (ExpandedNodeId[])array, v => WriteExpandedNodeId(null, v));
                        break;
                    case BuiltInType.StatusCode:
                        WriteArray(fieldName, (StatusCode[])array, v => WriteStatusCode(null, v));
                        break;
                    case BuiltInType.XmlElement:
                        WriteArray(fieldName, (XmlElement[])array, v => WriteXmlElement(null, v));
                        break;
                    case BuiltInType.Variant:
                        // try to write IEncodeable Array
                        if (array is IEncodeable[] encodeableArray)
                        {
                            WriteEncodeableArray(fieldName, encodeableArray,
                                array.GetType().GetElementType());
                            return;
                        }
                        WriteVariantArray(null, (Variant[])array);
                        break;
                    case BuiltInType.Enumeration:
                        if (array is Enum[] enums)
                        {
                            WriteArray(fieldName, enums, v => WriteEnumerated(null, v));
                            break;
                        }
                        else if (array is int[] values)
                        {
                            WriteArray(fieldName, values, v => WriteEnumerated(null, v));
                            break;
                        }
                        throw new EncodingException(
                            "Unexpected type encountered while encoding an Enumeration Array.");
                    case BuiltInType.ExtensionObject:
                        WriteArray(fieldName, (ExtensionObject[])array, v => WriteExtensionObject(null, v));
                        break;
                    case BuiltInType.DiagnosticInfo:
                        WriteArray(fieldName, (DiagnosticInfo[])array, v => WriteDiagnosticInfo(null, v));
                        break;
                    case BuiltInType.DataValue:
                        WriteArray(fieldName, (DataValue[])array, v => WriteDataValue(null, v));
                        break;
                    default:
                        // try to write IEncodeable Array
                        if (array is IEncodeable[] encodeableArray2)
                        {
                            WriteEncodeableArray(fieldName, encodeableArray2,
                                array.GetType().GetElementType());
                            return;
                        }
                        if (array == null)
                        {
                            // write zero dimension
                            WriteInt32(null, -1);
                            return;
                        }
                        throw new EncodingException(
                            "Unexpected type encountered while encoding an Array with " +
                            $"BuiltInType: {builtInType}");
                }
            }
            else if (rank == SchemaRank.Matrix)
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
                        WriteInt32("Dimensions", -1);
                        return;
                    }
                    matrix = new Matrix(multiArray, builtInType);
                }

                // Write the Dimensions
                WriteInt32Array(null, matrix.Dimensions);

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
                        }
                        {
                            if (matrix.Elements is int[] values)
                            {
                                for (var ii = 0; ii < values.Length; ii++)
                                {
                                    WriteEnumerated(null, values[ii]);
                                }
                                break;
                            }
                        }
                        throw new EncodingException(
                            "Unexpected type encountered while encoding an Enumeration Matrix.");
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
                            throw new EncodingException(
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
                            throw new EncodingException(
                                "Unexpected type encountered while encoding a Matrix " +
                                $"with BuiltInType: {matrix.TypeInfo.BuiltInType}");
                        }
                }
            }
        }

        /// <inheritdoc/>
        public virtual void WriteNull<T>(string? fieldName, T? value)
        {
        }

        /// <summary>
        /// Write nullable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        protected virtual void WriteNullable<T>(string? fieldName, T? value,
            Action<string?, T> writer) where T : class
        {
            // Union index, first is "null"
            WriteUnion(fieldName, value == null ? 0 : 1, id =>
            {
                switch (id)
                {
                    case 0:
                        WriteNull(null, value);
                        break;
                    default:
                        Debug.Assert(value != null);
                        writer(null, value);
                        break;
                }
            });
        }

        /// <summary>
        /// Write union
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="index"></param>
        /// <param name="writer"></param>
        public virtual void WriteUnion(string? fieldName, int index,
            Action<int> writer)
        {
            StartUnion(index);
            writer(index);
            EndUnion();
        }

        /// <summary>
        /// Start union
        /// </summary>
        /// <param name="index"></param>
        protected virtual void StartUnion(int index)
        {
            _writer.WriteInteger(index);
        }

        /// <inheritdoc/>
        protected virtual void EndUnion()
        {
        }

        /// <summary>
        /// Write encoded data type
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <exception cref="EncodingException"></exception>
        protected virtual void WriteEncodedDataType(string? fieldName, ExtensionObject value)
        {
            // write the type id.
            var typeId = value.TypeId;
            var body = value.Body;
            if (body is IEncodeable encodeable)
            {
                if (body is IJsonEncodeable je)
                {
                    typeId = je.JsonEncodingId;
                    body = encodeable.AsJson(Context);
                }
                else if (value.Encoding == ExtensionObjectEncoding.Xml)
                {
                    typeId = encodeable.XmlEncodingId;
                    body = encodeable.AsXmlElement(Context);
                }
                else
                {
                    typeId = encodeable.BinaryEncodingId;
                    body = encodeable.AsBinary(Context);
                }
            }

            var localTypeId = ExpandedNodeId.ToNodeId(typeId, Context.NamespaceUris);
            if (NodeId.IsNull(localTypeId) && !NodeId.IsNull(typeId))
            {
                if (value.Body is IEncodeable e)
                {
                    throw new EncodingException(
                        $"Cannot encode bodies of type '{e.GetType().FullName}' in " +
                        $"ExtensionObject unless the NamespaceUri ({typeId.NamespaceUri}) " +
                        "is in the encoder's NamespaceTable.");
                }
                localTypeId = NodeId.Null;
            }

            switch (body)
            {
                case byte[]:
                    break;
                case XmlElement xml:
                    body = Encoding.UTF8.GetBytes(
                        xml.OuterXml ?? string.Empty);
                    break;
                case string str:
                    body = Encoding.UTF8.GetBytes(str);
                    break;
                default:
                    Debug.Fail("Should not get here");
                    break;
            }

            // Write a raw encoded data type of the schema union
            WriteNodeId("TypeId", localTypeId);
            WriteByteString("Body", (byte[])body);
        }

        /// <summary>
        /// Writes a DiagnosticInfo to the stream.
        /// Ignores InnerDiagnosticInfo field if the nesting level
        /// <see cref="DiagnosticInfo.MaxInnerDepth"/> is exceeded.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="depth"></param>
        protected virtual void WriteDiagnosticInfoNullable(string? fieldName,
            DiagnosticInfo? value, int depth)
        {
            if (depth >= DiagnosticInfo.MaxInnerDepth)
            {
                value = null;
            }
            WriteNullable(fieldName, value, (f, v) => WriteDiagnosticInfo(f, v, depth));
        }

        /// <summary>
        /// Write as non nullable
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="depth"></param>
        protected virtual void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo value, int depth)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                WriteInt32(nameof(value.SymbolicId), value.SymbolicId);
                WriteInt32(nameof(value.NamespaceUri), value.NamespaceUri);
                WriteInt32(nameof(value.Locale), value.Locale);
                WriteInt32(nameof(value.LocalizedText), value.LocalizedText);
                WriteString(nameof(value.AdditionalInfo), value.AdditionalInfo);
                WriteStatusCode(nameof(value.InnerStatusCode), value.InnerStatusCode);
                WriteDiagnosticInfoNullable(nameof(value.InnerDiagnosticInfo),
                    value.InnerDiagnosticInfo, depth + 1);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <summary>
        /// Writes an Variant value to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        protected void WriteVariantValue(Variant value,
            BuiltInType builtInType, SchemaRank valueRank)
        {
            var valueToEncode = value.Value;

            if (valueToEncode == null)
            {
                WriteNull(builtInType, valueRank);
            }
            else if (valueRank == SchemaRank.Scalar)
            {
                WriteScalar(builtInType, valueToEncode);
            }
            else if (valueToEncode is not Matrix m)
            {
                Debug.Assert(valueRank == SchemaRank.Collection);
                WriteArray(builtInType, valueToEncode);
            }
            else
            {
                Debug.Assert(valueRank == SchemaRank.Matrix);
                WriteMatrix("Body", m);
            }
        }

        /// <summary>
        /// Lookup
        /// </summary>
        internal static readonly FrozenDictionary<(SchemaRank, BuiltInType), int> _variableUnionId =
            BaseAvroDecoder._variantUnionFieldIds
                .ToArray()
                .Select((f, i) => System.Collections.Generic.KeyValuePair.Create(f, i))
                .ToFrozenDictionary();

        /// <summary>
        /// Write null value of variant
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        protected virtual void WriteNull(BuiltInType builtInType, SchemaRank valueRank)
        {
            // Nothing to do
        }

        /// <summary>
        /// Write array of variant
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="valueToEncode"></param>
        /// <exception cref="EncodingException"></exception>
        protected virtual void WriteArray(BuiltInType builtInType, object valueToEncode)
        {
            static T[] Cast<T>(object objects)
            {
                if (objects is T[] r)
                {
                    return r;
                }
                if (objects is object[] o)
                {
                    return o.Cast<T>().ToArray();
                }
                if (objects is T t)
                {
                    return new[] { t };
                }
                return Array.Empty<T>();
            }
            switch (builtInType)
            {
                case BuiltInType.Boolean:
                    WriteBooleanArray(null, Cast<bool>(valueToEncode));
                    break;
                case BuiltInType.SByte:
                    WriteSByteArray(null, Cast<sbyte>(valueToEncode));
                    break;
                case BuiltInType.Byte:
                    WriteByteArray(null, Cast<byte>(valueToEncode));
                    break;
                case BuiltInType.Int16:
                    WriteInt16Array(null, Cast<short>(valueToEncode));
                    break;
                case BuiltInType.UInt16:
                    WriteUInt16Array(null, Cast<ushort>(valueToEncode));
                    break;
                case BuiltInType.Int32:
                    WriteInt32Array(null, Cast<int>(valueToEncode));
                    break;
                case BuiltInType.UInt32:
                    WriteUInt32Array(null, Cast<uint>(valueToEncode));
                    break;
                case BuiltInType.Int64:
                    WriteInt64Array(null, Cast<long>(valueToEncode));
                    break;
                case BuiltInType.UInt64:
                    WriteUInt64Array(null, Cast<ulong>(valueToEncode));
                    break;
                case BuiltInType.Float:
                    WriteFloatArray(null, Cast<float>(valueToEncode));
                    break;
                case BuiltInType.Double:
                    WriteDoubleArray(null, Cast<double>(valueToEncode));
                    break;
                case BuiltInType.String:
                    WriteStringArray(null, Cast<string>(valueToEncode));
                    break;
                case BuiltInType.DateTime:
                    WriteDateTimeArray(null, Cast<DateTime>(valueToEncode));
                    break;
                case BuiltInType.Guid:
                    WriteGuidArray(null, Cast<Uuid>(valueToEncode));
                    break;
                case BuiltInType.ByteString:
                    WriteByteStringArray(null, Cast<byte[]>(valueToEncode));
                    break;
                case BuiltInType.XmlElement:
                    WriteXmlElementArray(null, Cast<XmlElement>(valueToEncode));
                    break;
                case BuiltInType.NodeId:
                    WriteNodeIdArray(null, Cast<NodeId>(valueToEncode));
                    break;
                case BuiltInType.ExpandedNodeId:
                    WriteExpandedNodeIdArray(null, Cast<ExpandedNodeId>(valueToEncode));
                    break;
                case BuiltInType.StatusCode:
                    WriteStatusCodeArray(null, Cast<StatusCode>(valueToEncode));
                    break;
                case BuiltInType.QualifiedName:
                    WriteQualifiedNameArray(null, Cast<QualifiedName>(valueToEncode));
                    break;
                case BuiltInType.LocalizedText:
                    WriteLocalizedTextArray(null, Cast<LocalizedText>(valueToEncode));
                    break;
                case BuiltInType.ExtensionObject:
                    WriteExtensionObjectArray(null, Cast<ExtensionObject>(valueToEncode));
                    break;
                case BuiltInType.DataValue:
                    WriteDataValueArray(null, Cast<DataValue>(valueToEncode));
                    break;
                case BuiltInType.Enumeration:
                    // Check whether the value to encode is int array.
                    if (valueToEncode is int[] ints)
                    {
                        WriteEnumeratedArray(null, ints, null);
                        break;
                    }
                    var enums = Cast<Enum>(valueToEncode);
                    WriteEnumeratedArray(null, enums, null);
                    break;
                case BuiltInType.Variant:
                    if (valueToEncode is Variant[] variants)
                    {
                        WriteVariantArray(null, variants);
                        break;
                    }
                    if (valueToEncode is object[] objects)
                    {
                        WriteVariantArray(null, objects.Select(v => new Variant(v)).ToArray());
                        break;
                    }
                    throw new EncodingException(
                        $"Unexpected type encountered while encoding array: {valueToEncode.GetType()}");
                case BuiltInType.DiagnosticInfo:
                    WriteDiagnosticInfoArray(null, Cast<DiagnosticInfo>(valueToEncode));
                    break;
                default:
                    throw new EncodingException(
                        $"Unexpected type encountered while encoding a Variant: {builtInType}");
            }
        }

        /// <summary>
        /// Write scalar value
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="valueToEncode"></param>
        /// <exception cref="EncodingException"></exception>
        protected virtual void WriteScalar(BuiltInType builtInType, object valueToEncode)
        {
            switch (builtInType)
            {
                case BuiltInType.Null:
                    WriteNull(null, valueToEncode);
                    return;
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
                    WriteString(null, (string)valueToEncode);
                    return;
                case BuiltInType.DateTime:
                    WriteDateTime(null, (DateTime)valueToEncode);
                    return;
                case BuiltInType.Guid:
                    WriteGuid(null, (Uuid)valueToEncode);
                    return;
                case BuiltInType.ByteString:
                    WriteByteString(null, (byte[])valueToEncode);
                    return;
                case BuiltInType.XmlElement:
                    WriteXmlElement(null, (XmlElement)valueToEncode);
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
                    WriteQualifiedName(null, (QualifiedName)valueToEncode);
                    return;
                case BuiltInType.LocalizedText:
                    WriteLocalizedText(null, (LocalizedText)valueToEncode);
                    return;
                case BuiltInType.ExtensionObject:
                    WriteExtensionObject(null, (ExtensionObject)valueToEncode);
                    return;
                case BuiltInType.DataValue:
                    WriteDataValue(null, (DataValue)valueToEncode);
                    return;
                case BuiltInType.Enumeration:
                    WriteEnumerated(null, Convert.ToInt32(valueToEncode,
                        CultureInfo.InvariantCulture));
                    return;
                    //case BuiltInType.DiagnosticInfo:
                    //    WriteDiagnosticInfo((DiagnosticInfo)valueToEncode);
                    //    return;
            }

            throw new EncodingException(
                $"Unexpected type encountered while encoding a Variant: {builtInType}");
        }

        /// <inheritdoc/>
        public virtual void WriteArray<T>(string? fieldName,
            IList<T>? values, Action<T> writer, string? typeName = null)
        {
            if (values == null || values.Count == 0)
            {
                _writer.WriteInteger(0);
            }
            else if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < values.Count)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxArrayLength {Context.MaxArrayLength} < {values.Count}.");
            }
            else
            {
                _writer.WriteInteger(values.Count);
                foreach (var value in values)
                {
                    writer(value);
                }
            }
            // Array block ends with 0 length
            _writer.WriteInteger(0);
        }

        /// <inheritdoc/>
        protected virtual void WriteArray(string? fieldName, Array? values,
            Action<object?> writer)
        {
            if (values == null || values.Length == 0)
            {
                _writer.WriteInteger(0);
            }
            else if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < values.Length)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"MaxArrayLength {Context.MaxArrayLength} < {values.Length}.");
            }
            else
            {
                // write length
                _writer.WriteInteger(values.Length);

                for (var index = 0; index < values.Length; index++)
                {
                    writer(values.GetValue(index));
                }
            }
            // Array block ends with 0 length
            _writer.WriteInteger(0);
        }

        /// <inheritdoc/>
        protected virtual void WriteMatrix<T>(string? fieldName,
            IList<T>? values, int[] dimensions, Action<T> writer, string? typeName = null)
        {
            WriteArray(nameof(Matrix.Dimensions), dimensions,
                v => WriteInt32(null, v));
            WriteArray(kDefaultFieldName, values, writer, typeName);
        }

        /// <inheritdoc/>
        protected virtual void WriteMatrix(string? fieldName, Matrix matrix,
            string? typeName = null)
        {
            WriteArray(nameof(Matrix.Dimensions), matrix.Dimensions,
                v => WriteInt32(null, v));
            WriteArray(kDefaultFieldName, matrix.Elements,
                matrix.TypeInfo.ValueRank, matrix.TypeInfo.BuiltInType);
        }

        /// <summary>
        /// Detect full name of the encodeable object
        /// </summary>
        /// <param name="value"></param>
        /// <param name="systemType"></param>
        /// <param name="typeName"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        protected string? GetFullNameOfEncodeable(IEncodeable? value,
            Type? systemType, out string? typeName, out ExpandedNodeId? typeId)
        {
            typeName = systemType?.Name ?? value?.GetType().Name;
            typeId = value?.TypeId;
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            var fullName = typeId?.GetFullName(typeName, Context);
            if (string.IsNullOrEmpty(fullName) && systemType != null)
            {
                value = (IEncodeable?)Activator.CreateInstance(systemType);
                typeId ??= value?.TypeId;
                fullName = typeId?.GetFullName(typeName, Context);
            }
            return fullName;
        }

        /// <summary>
        /// Test and increment the nesting level.
        /// </summary>
        /// <exception cref="EncodingException"></exception>
        private void CheckAndIncrementNestingLevel()
        {
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw new EncodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded.");
            }
            _nestingLevel++;
        }

        /// <summary>
        /// Default name when field name is not provided
        /// </summary>
        protected const string kDefaultFieldName = "Value";

        private readonly AvroBinaryWriter _writer;
        private ushort[]? _namespaceMappings;
        private ushort[]? _serverMappings;
        private uint _nestingLevel;
    }
}
