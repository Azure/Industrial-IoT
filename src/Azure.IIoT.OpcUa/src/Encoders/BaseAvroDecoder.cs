// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Buffers.Binary;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Decodes objects from a Avro Binary encoded stream.
    /// </summary>
    public abstract class BaseAvroDecoder : IDecoder
    {
        /// <inheritdoc/>
        public virtual EncodingType EncodingType => (EncodingType)3;

        /// <inheritdoc/>
        public virtual IServiceMessageContext Context { get; }

        /// <summary>
        /// Create avro decoder
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        protected BaseAvroDecoder(Stream stream, IServiceMessageContext context)
        {
            _reader = new AvroBinaryReader(stream)
            {
                MaxBytesLength = context.MaxByteStringLength,
                MaxStringLength = context.MaxStringLength
            };
            Context = context;
            _nestingLevel = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
        }

        /// <inheritdoc/>
        public virtual void Close()
        {
            // Dispose
        }

        /// <inheritdoc/>
        public virtual void PushNamespace(string namespaceUri)
        {
            // not used in the binary encoding.
        }

        /// <inheritdoc/>
        public virtual void PopNamespace()
        {
            // not used in the binary encoding.
        }

        /// <inheritdoc/>
        public virtual void SetMappingTables(NamespaceTable? namespaceUris,
            StringTable? serverUris)
        {
            _namespaceMappings = null;

            if (namespaceUris != null && Context.NamespaceUris != null)
            {
                _namespaceMappings = Context.NamespaceUris.CreateMapping(
                    namespaceUris, false);
            }

            _serverMappings = null;

            if (serverUris != null && Context.ServerUris != null)
            {
                _serverMappings = Context.ServerUris.CreateMapping(
                    serverUris, false);
            }
        }

        /// <inheritdoc/>
        public virtual bool ReadBoolean(string? fieldName)
        {
            return _reader.ReadBoolean();
        }

        /// <inheritdoc/>
        public virtual sbyte ReadSByte(string? fieldName)
        {
            return (sbyte)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual byte ReadByte(string? fieldName)
        {
            return (byte)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual short ReadInt16(string? fieldName)
        {
            return (short)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual ushort ReadUInt16(string? fieldName)
        {
            return (ushort)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual int ReadInt32(string? fieldName)
        {
            return (int)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual uint ReadUInt32(string? fieldName)
        {
            return (uint)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual long ReadInt64(string? fieldName)
        {
            return _reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual ulong ReadUInt64(string? fieldName)
        {
            // ulong is a union of long and fixed (> long.max)
            var index = _reader.ReadInteger();
            if (index == 0)
            {
                return (ulong)_reader.ReadInteger();
            }

            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            _reader.ReadFixed(bytes);
            return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        }

        /// <inheritdoc/>
        public virtual float ReadFloat(string? fieldName)
        {
            return _reader.ReadFloat();
        }

        /// <inheritdoc/>
        public virtual double ReadDouble(string? fieldName)
        {
            return _reader.ReadDouble();
        }

        /// <inheritdoc/>
        public virtual Uuid ReadGuid(string? fieldName)
        {
            Span<byte> bytes = stackalloc byte[16];
            _reader.ReadFixed(bytes);
            return new Uuid(new Guid(bytes));
        }

        /// <inheritdoc/>
        public virtual string? ReadString(string? fieldName)
        {
            return _reader.ReadString();
        }

        /// <inheritdoc/>
        public virtual DateTime ReadDateTime(string? fieldName)
        {
            var ticks = _reader.ReadInteger();

            if (ticks >= (long.MaxValue - Opc.Ua.Utils.TimeBase.Ticks))
            {
                return DateTime.MaxValue;
            }

            ticks += Opc.Ua.Utils.TimeBase.Ticks;

            if (ticks >= DateTime.MaxValue.Ticks)
            {
                return DateTime.MaxValue;
            }

            if (ticks <= Opc.Ua.Utils.TimeBase.Ticks)
            {
                return DateTime.MinValue;
            }

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <inheritdoc/>
        public virtual byte[] ReadByteString(string? fieldName)
        {
            return _reader.ReadBytes();
        }

        /// <inheritdoc/>
        public virtual XmlElement ReadXmlElement(string? fieldName)
        {
            var xmlString = _reader.ReadString();
            var document = new XmlDocument();
            try
            {
                using (var stream = new StringReader(xmlString))
                using (var reader = XmlReader.Create(stream,
                    Opc.Ua.Utils.DefaultXmlReaderSettings()))
                {
                    document.Load(reader);
                }
            }
            catch (XmlException ex)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    ex, "Xml element invalid");
            }
            return document.DocumentElement!;
        }

        /// <inheritdoc/>
        public virtual NodeId ReadNodeId(string? fieldName)
        {
            //
            // Node id is a record, with namespace and union of
            // the id. The IdType value represents the union
            // discriminator.
            //
            // Node id is not nullable, i=0 is a null node id.
            //
            var namespaceUri = ReadString("Namespace");
            var namespaceIndex = string.IsNullOrEmpty(namespaceUri) ?
                (ushort)0 :
                Context.NamespaceUris.GetIndexOrAppend(namespaceUri);
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            return ReadNodeId((ushort)namespaceIndex);
        }

        /// <inheritdoc/>
        public virtual ExpandedNodeId ReadExpandedNodeId(string? fieldName)
        {
            //
            // Expanded Node id is a record extending NodeId.
            // Namespace, and union of via IdType which represents
            // the union discriminator.  After that we write the
            // namespace uri and then server index.
            //
            // ExpandedNode id is not nullable, i=0 is a null node id.
            //
            var namespaceUri = ReadString("Namespace");
            var namespaceIndex = string.IsNullOrEmpty(namespaceUri) ?
                (ushort)0 :
                Context.NamespaceUris.GetIndexOrAppend(namespaceUri);
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }

            var innerNodeId = ReadNodeId((ushort)namespaceIndex);
            var serverUri = ReadString("ServerUri");

            if (NodeId.IsNull(innerNodeId))
            {
                return ExpandedNodeId.Null;
            }

            var serverIndex = string.IsNullOrEmpty(serverUri) ? 0u :
                Context.ServerUris.GetIndexOrAppend(serverUri);
            if (_serverMappings != null &&
                _serverMappings.Length > serverIndex)
            {
                serverIndex = _serverMappings[serverIndex];
            }
            return new ExpandedNodeId(innerNodeId, namespaceUri, serverIndex);
        }

        /// <inheritdoc/>
        public virtual StatusCode ReadStatusCode(string? fieldName)
        {
            return (StatusCode)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public virtual DiagnosticInfo ReadDiagnosticInfo(string? fieldName)
        {
            return ReadDiagnosticInfo(fieldName, 0);
        }

        /// <inheritdoc/>
        public virtual QualifiedName ReadQualifiedName(string? fieldName)
        {
            var namespaceUri = ReadString("Namespace");
            var name = ReadString(nameof(QualifiedName.Name));

            var namespaceIndex = string.IsNullOrEmpty(namespaceUri) ?
                (ushort)0 :
                Context.NamespaceUris.GetIndexOrAppend(namespaceUri);
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            return new QualifiedName(name, namespaceIndex);
        }

        /// <inheritdoc/>
        public virtual LocalizedText ReadLocalizedText(string? fieldName)
        {
            return new LocalizedText(
                ReadString(nameof(LocalizedText.Locale)),
                ReadString(nameof(LocalizedText.Text)));
        }

        /// <inheritdoc/>
        public virtual Variant ReadVariant(string? fieldName)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                // Read Union discriminator for the variant
                var fieldId = ReadUnionSelector();
                if (fieldId < 0 || fieldId >= _variantUnionFieldIds.Length)
                {
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Cannot decode unknown union field.", fieldId);
                }
                var (valueRank, builtInType) = _variantUnionFieldIds[fieldId];
                return ReadVariantValue(builtInType, valueRank);
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <inheritdoc/>
        public virtual DataValue ReadDataValue(string? fieldName)
        {
            return new DataValue
            {
                WrappedValue = ReadVariant(null),
                StatusCode = ReadStatusCode(null),
                SourceTimestamp = ReadDateTime(null),
                SourcePicoseconds = ReadUInt16(null),
                ServerTimestamp = ReadDateTime(null),
                ServerPicoseconds = ReadUInt16(null)
            };
        }

        /// <inheritdoc/>
        public virtual DataSet ReadDataSet()
        {
            var fieldNames = Array.Empty<string>();
            var avroFieldContent = ReadUInt32(null);
            var dataSet = avroFieldContent == 0 ?
                new DataSet() :
                new DataSet((uint)DataSetFieldContentMask.RawData);

            if (avroFieldContent == 1) // Raw mode
            {
                //
                // Read array of raw variant
                //
                var variants = ReadVariantArray(null);
                if (variants == null && fieldNames.Length == 0)
                {
                    return dataSet;
                }
                if (variants == null || variants.Count != fieldNames.Length)
                {
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unexpected number of fields in data set");
                }
                for (var index = 0; index < fieldNames.Length; index++)
                {
                    dataSet.Add(fieldNames[index], new DataValue(variants[index]));
                }
            }
            else if (avroFieldContent == 0)
            {
                //
                // Read data values
                //
                var dataValues = ReadDataValueArray(null);
                if (dataValues == null && fieldNames.Length == 0)
                {
                    return dataSet;
                }
                if (dataValues == null || dataValues.Count != fieldNames.Length)
                {
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unexpected number of fields in data set");
                }
                for (var index = 0; index < fieldNames.Length; index++)
                {
                    dataSet.Add(fieldNames[index], dataValues[index]);
                }
            }
            return dataSet;
        }

        /// <inheritdoc/>
        public virtual ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            var unionId = ReadUnionSelector();
            if (unionId != 0)
            {
                return new ExtensionObject(ReadEncodeableInExtensionObject(unionId));
            }
            return ReadEncodedDataType(fieldName);
        }

        /// <inheritdoc/>
        public virtual IEncodeable ReadEncodeable(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            if (Activator.CreateInstance(systemType) is not IEncodeable encodeable)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadDecodingError,
                    Opc.Ua.Utils.Format("Cannot decode type '{0}'.", systemType.FullName));
            }

            if (encodeableTypeId != null)
            {
                // set type identifier for custom complex data types before decode.

                if (encodeable is IComplexTypeInstance complexTypeInstance)
                {
                    complexTypeInstance.TypeId = encodeableTypeId;
                }
            }

            CheckAndIncrementNestingLevel();

            try
            {
                encodeable.Decode(this);
            }
            finally
            {
                _nestingLevel--;
            }

            return encodeable;
        }

        /// <inheritdoc/>
        public virtual Enum ReadEnumerated(string? fieldName, Type enumType)
        {
            return (Enum)Enum.ToObject(enumType, _reader.ReadInteger());
        }

        /// <inheritdoc/>
        public virtual T ReadEnumerated<T>(string? fieldName) where T : Enum
        {
            return (T)ReadEnumerated(fieldName, typeof(T));
        }

        /// <inheritdoc/>
        public virtual BooleanCollection ReadBooleanArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadBoolean(null));
        }

        /// <inheritdoc/>
        public virtual SByteCollection ReadSByteArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadSByte(null));
        }

        /// <inheritdoc/>
        public virtual ByteCollection ReadByteArray(string? fieldName)
        {
            // TODO: Read fixed bytes instead
            return ReadArray(fieldName, () => ReadByte(null));
        }

        /// <inheritdoc/>
        public virtual Int16Collection ReadInt16Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadInt16(null));
        }

        /// <inheritdoc/>
        public virtual UInt16Collection ReadUInt16Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadUInt16(null));
        }

        /// <inheritdoc/>
        public virtual Int32Collection ReadInt32Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadInt32(null));
        }

        /// <inheritdoc/>
        public virtual UInt32Collection ReadUInt32Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadUInt32(null));
        }

        /// <inheritdoc/>
        public virtual Int64Collection ReadInt64Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadInt64(null));
        }

        /// <inheritdoc/>
        public virtual UInt64Collection ReadUInt64Array(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadUInt64(null));
        }

        /// <inheritdoc/>
        public virtual FloatCollection ReadFloatArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadFloat(null));
        }

        /// <inheritdoc/>
        public virtual DoubleCollection ReadDoubleArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadDouble(null));
        }

        /// <inheritdoc/>
        public virtual StringCollection ReadStringArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadString(null));
        }

        /// <inheritdoc/>
        public virtual DateTimeCollection ReadDateTimeArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadDateTime(null));
        }

        /// <inheritdoc/>
        public virtual UuidCollection ReadGuidArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadGuid(null));
        }

        /// <inheritdoc/>
        public virtual ByteStringCollection ReadByteStringArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadByteString(null));
        }

        /// <inheritdoc/>
        public virtual XmlElementCollection ReadXmlElementArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadXmlElement(null));
        }

        /// <inheritdoc/>
        public virtual NodeIdCollection ReadNodeIdArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadNodeId(null));
        }

        /// <inheritdoc/>
        public virtual ExpandedNodeIdCollection ReadExpandedNodeIdArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadExpandedNodeId(null));
        }

        /// <inheritdoc/>
        public virtual StatusCodeCollection ReadStatusCodeArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadStatusCode(null));
        }

        /// <inheritdoc/>
        public virtual DiagnosticInfoCollection ReadDiagnosticInfoArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadDiagnosticInfo(null));
        }

        /// <inheritdoc/>
        public virtual QualifiedNameCollection ReadQualifiedNameArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadQualifiedName(null));
        }

        /// <inheritdoc/>
        public virtual LocalizedTextCollection ReadLocalizedTextArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadLocalizedText(null));
        }

        /// <inheritdoc/>
        public virtual VariantCollection ReadVariantArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadVariant(null));
        }

        /// <inheritdoc/>
        public virtual DataValueCollection ReadDataValueArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadDataValue(null));
        }

        /// <inheritdoc/>
        public virtual ExtensionObjectCollection ReadExtensionObjectArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadExtensionObject(null));
        }

        /// <inheritdoc/>
        public virtual Array? ReadEncodeableArray(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            return ReadArray(fieldName, () => ReadEncodeable(null,
                systemType, encodeableTypeId), systemType);
        }

        /// <inheritdoc/>
        public virtual Array? ReadEnumeratedArray(string? fieldName, Type enumType)
        {
            return ReadArray(fieldName, () => ReadEnumerated(null,
                enumType), enumType);
        }

        /// <inheritdoc/>
        public virtual T[]? ReadEnumeratedArray<T>(string? fieldName) where T : Enum
        {
            return (T[]?)ReadEnumeratedArray(fieldName, typeof(T));
        }

        /// <inheritdoc/>
        public virtual Array? ReadArray(string? fieldName, int valueRank, BuiltInType builtInType,
            Type? systemType = null, ExpandedNodeId? encodeableTypeId = null)
        {
            if (valueRank == ValueRanks.OneDimension)
            {
                switch (builtInType)
                {
                    case BuiltInType.Boolean:
                        return ReadBooleanArray(fieldName)?.ToArray();
                    case BuiltInType.SByte:
                        return ReadSByteArray(fieldName)?.ToArray();
                    case BuiltInType.Byte:
                        return ReadByteArray(fieldName)?.ToArray();
                    case BuiltInType.Int16:
                        return ReadInt16Array(fieldName)?.ToArray();
                    case BuiltInType.UInt16:
                        return ReadUInt16Array(fieldName)?.ToArray();
                    case BuiltInType.Enumeration:
                        if (systemType != null && encodeableTypeId != null)
                        {
                            DetermineIEncodeableSystemType(ref systemType, encodeableTypeId);
                            if (systemType?.IsEnum == true)
                            {
                                return ReadEnumeratedArray(fieldName, systemType);
                            }
                        }
                        // if system type is not known or not an enum, fall back to Int32
                        goto case BuiltInType.Int32;
                    case BuiltInType.Int32:
                        return ReadInt32Array(fieldName)?.ToArray();
                    case BuiltInType.UInt32:
                        return ReadUInt32Array(fieldName)?.ToArray();
                    case BuiltInType.Int64:
                        return ReadInt64Array(fieldName)?.ToArray();
                    case BuiltInType.UInt64:
                        return ReadUInt64Array(fieldName)?.ToArray();
                    case BuiltInType.Float:
                        return ReadFloatArray(fieldName)?.ToArray();
                    case BuiltInType.Double:
                        return ReadDoubleArray(fieldName)?.ToArray();
                    case BuiltInType.String:
                        return ReadStringArray(fieldName)?.ToArray();
                    case BuiltInType.DateTime:
                        return ReadDateTimeArray(fieldName)?.ToArray();
                    case BuiltInType.Guid:
                        return ReadGuidArray(fieldName)?.ToArray();
                    case BuiltInType.ByteString:
                        return ReadByteStringArray(fieldName)?.ToArray();
                    case BuiltInType.XmlElement:
                        return ReadXmlElementArray(fieldName)?.ToArray();
                    case BuiltInType.NodeId:
                        return ReadNodeIdArray(fieldName)?.ToArray();
                    case BuiltInType.ExpandedNodeId:
                        return ReadExpandedNodeIdArray(fieldName)?.ToArray();
                    case BuiltInType.StatusCode:
                        return ReadStatusCodeArray(fieldName)?.ToArray();
                    case BuiltInType.QualifiedName:
                        return ReadQualifiedNameArray(fieldName)?.ToArray();
                    case BuiltInType.LocalizedText:
                        return ReadLocalizedTextArray(fieldName)?.ToArray();
                    case BuiltInType.DataValue:
                        return ReadDataValueArray(fieldName)?.ToArray();
                    case BuiltInType.Variant:
                        if (systemType != null && encodeableTypeId != null
                            && DetermineIEncodeableSystemType(ref systemType, encodeableTypeId))
                        {
                            return ReadEncodeableArray(fieldName, systemType, encodeableTypeId);
                        }
                        return ReadVariantArray(fieldName)?.ToArray();
                    case BuiltInType.ExtensionObject:
                        return ReadExtensionObjectArray(fieldName)?.ToArray();
                    case BuiltInType.DiagnosticInfo:
                        return ReadDiagnosticInfoArray(fieldName)?.ToArray();
                    default:
                        if (systemType != null && encodeableTypeId != null
                            && DetermineIEncodeableSystemType(ref systemType, encodeableTypeId))
                        {
                            return ReadEncodeableArray(fieldName, systemType, encodeableTypeId);
                        }
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                            "Cannot decode unknown type in Array object with BuiltInType: {0}.",
                            builtInType);
                }
            }

            // two or more dimensions
            if (valueRank >= ValueRanks.OneOrMoreDimensions)
            {
                // read dimensions array
                var dimensions = ReadInt32Array(null);
                if (dimensions?.Count > 0)
                {
                    //int length;
                    (_, var length) = Matrix.ValidateDimensions(false, dimensions, Context.MaxArrayLength);

                    // read the elements
                    Array? elements = null;
                    if (systemType != null && encodeableTypeId != null
                        && DetermineIEncodeableSystemType(ref systemType, encodeableTypeId))
                    {
                        elements = Array.CreateInstance(systemType, length);
                        for (var i = 0; i < length; i++)
                        {
                            var element = ReadEncodeable(null, systemType, encodeableTypeId);
                            elements.SetValue(Convert.ChangeType(element, systemType,
                                CultureInfo.InvariantCulture), i);
                        }
                    }

                    elements ??= ReadArrayElements(length, builtInType);

                    if (elements == null)
                    {
                        throw ServiceResultException.Create(
                            StatusCodes.BadDecodingError,
                            "Unexpected null Array for multidimensional matrix with {0} elements.", length);
                    }

                    if (builtInType == BuiltInType.Enumeration && systemType?.IsEnum == true)
                    {
                        var newElements = Array.CreateInstance(systemType, elements.Length);
                        var ii = 0;
                        foreach (var element in elements)
                        {
                            newElements.SetValue(Enum.ToObject(systemType, element), ii++);
                        }
                        elements = newElements;
                    }

                    return new Matrix(elements, builtInType, dimensions.ToArray()).ToArray();
                }
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Unexpected null or empty Dimensions for multidimensional matrix.");
            }
            return null;
        }

        /// <summary>
        /// Reads a DiagnosticInfo from the stream.
        /// Limits the InnerDiagnosticInfo nesting level.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="depth"></param>
        /// <exception cref="ServiceResultException"></exception>
        internal DiagnosticInfo ReadDiagnosticInfo(string? fieldName, int depth)
        {
            if (depth >= DiagnosticInfo.MaxInnerDepth)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of InnerDiagnosticInfo was exceeded");
            }
            CheckAndIncrementNestingLevel();
            try
            {
                return new DiagnosticInfo
                {
                    SymbolicId = ReadInt32(nameof(DiagnosticInfo.SymbolicId)),
                    NamespaceUri = ReadInt32(nameof(DiagnosticInfo.NamespaceUri)),
                    Locale = ReadInt32(nameof(DiagnosticInfo.Locale)),
                    LocalizedText = ReadInt32(nameof(DiagnosticInfo.LocalizedText)),
                    AdditionalInfo = ReadString(nameof(DiagnosticInfo.AdditionalInfo)),
                    InnerStatusCode = ReadStatusCode(nameof(DiagnosticInfo.InnerStatusCode)),
                    InnerDiagnosticInfo = ReadNullableDiagnosticInfo(
                        nameof(DiagnosticInfo.InnerDiagnosticInfo), depth + 1)
                };
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <summary>
        /// Reads a DiagnosticInfo from the stream.
        /// Limits the InnerDiagnosticInfo nesting level.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="depth"></param>
        private DiagnosticInfo? ReadNullableDiagnosticInfo(string? fieldName, int depth)
        {
            var unionId = _reader.ReadInteger();
            if (unionId == 0)
            {
                return default;
            }
            Debug.Assert(unionId == 1);
            return ReadDiagnosticInfo(fieldName, depth);
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public virtual T[] ReadArray<T>(string? fieldName, Func<T> reader)
        {
            var length = _reader.ReadInteger();
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < length)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "MaxArrayLength {0} < {1}",
                    Context.MaxArrayLength,
                    length);
            }
            return Enumerable.Range(0, (int)length)
                .Select(_ => reader())
                .ToArray();
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        protected virtual Array ReadArray(string? fieldName,
            Func<object> reader, Type type)
        {
            var length = _reader.ReadInteger();
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < length)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "MaxArrayLength {0} < {1}",
                    Context.MaxArrayLength,
                    length);
            }
            var array = Array.CreateInstance(type, length);
            for (var i = 0; i < length; i++)
            {
                array.SetValue(reader(), i);
            }
            return array;
        }

        /// <summary>
        /// Read union selector
        /// </summary>
        /// <returns></returns>
        protected virtual int ReadUnionSelector()
        {
            return (int)_reader.ReadInteger();
        }

        /// <summary>
        /// Get the system type from the type factory if not specified by caller.
        /// </summary>
        /// <param name="systemType">The reference to the system type, or null</param>
        /// <param name="encodeableTypeId">The encodeable type id of the system type.</param>
        /// <returns>If the system type is assignable to <see cref="IEncodeable"/> </returns>
        private bool DetermineIEncodeableSystemType(ref Type systemType,
            ExpandedNodeId encodeableTypeId)
        {
            if (encodeableTypeId != null && systemType == null)
            {
                systemType = Context.Factory.GetSystemType(encodeableTypeId);
            }
            return typeof(IEncodeable).IsAssignableFrom(systemType);
        }

        /// <summary>
        /// Reads and returns an array of elements of the specified length and builtInType
        /// </summary>
        /// <param name="length"></param>
        /// <param name="builtInType"></param>
        /// <exception cref="ServiceResultException"></exception>
        private Array? ReadArrayElements(int length, BuiltInType builtInType)
        {
            Array? array = null;
            switch (builtInType)
            {
                case BuiltInType.Boolean:
                    {
                        var values = new bool[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadBoolean(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.SByte:
                    {
                        var values = new sbyte[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadSByte(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Byte:
                    {
                        var values = new byte[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadByte(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Int16:
                    {
                        var values = new short[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadInt16(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.UInt16:
                    {
                        var values = new ushort[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadUInt16(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Int32:
                case BuiltInType.Enumeration:
                    {
                        var values = new int[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadInt32(null);
                        }
                        array = values;
                        break;
                    }

                case BuiltInType.UInt32:
                    {
                        var values = new uint[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadUInt32(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Int64:
                    {
                        var values = new long[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadInt64(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.UInt64:
                    {
                        var values = new ulong[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadUInt64(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Float:
                    {
                        var values = new float[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadFloat(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Double:
                    {
                        var values = new double[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadDouble(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.String:
                    {
                        var values = new string?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadString(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.DateTime:
                    {
                        var values = new DateTime[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadDateTime(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Guid:
                    {
                        var values = new Uuid[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadGuid(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.ByteString:
                    {
                        var values = new byte[length][];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadByteString(null)!;
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.XmlElement:
                    {
                        try
                        {
                            var values = new XmlElement?[length];

                            for (var ii = 0; ii < values.Length; ii++)
                            {
                                values[ii] = ReadXmlElement(null);
                            }

                            array = values;
                        }
                        catch (Exception ex)
                        {
                            Opc.Ua.Utils.LogError(ex, "Error reading array of XmlElement.");
                        }

                        break;
                    }

                case BuiltInType.NodeId:
                    {
                        var values = new NodeId?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadNodeId(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.ExpandedNodeId:
                    {
                        var values = new ExpandedNodeId?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadExpandedNodeId(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.StatusCode:
                    {
                        var values = new StatusCode[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadStatusCode(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.QualifiedName:
                    {
                        var values = new QualifiedName?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadQualifiedName(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.LocalizedText:
                    {
                        var values = new LocalizedText?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadLocalizedText(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.ExtensionObject:
                    {
                        var values = new ExtensionObject?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadExtensionObject(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.DataValue:
                    {
                        var values = new DataValue?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadDataValue(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.Variant:
                    {
                        var values = new Variant[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadVariant(null);
                        }

                        array = values;
                        break;
                    }

                case BuiltInType.DiagnosticInfo:
                    {
                        var values = new DiagnosticInfo?[length];

                        for (var ii = 0; ii < values.Length; ii++)
                        {
                            values[ii] = ReadDiagnosticInfo(null);
                        }

                        array = values;
                        break;
                    }
                default:
                    throw ServiceResultException.Create(
                            StatusCodes.BadDecodingError,
                            Opc.Ua.Utils.Format("Cannot decode unknown type in Variant object with BuiltInType: {0}.", builtInType));
            }

            return array;
        }

        /// <summary>
        /// Reads the length of an array.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private int ReadArrayLength()
        {
            var length = (int)_reader.ReadInteger();

            if (length < 0)
            {
                return -1;
            }

            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < length)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "MaxArrayLength {0} < {1}",
                    Context.MaxArrayLength,
                    length);
            }
            return length;
        }

        /// <summary>
        /// Read extension object
        /// </summary>
        /// <param name="unionId"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        protected virtual IEncodeable ReadEncodeableInExtensionObject(int unionId)
        {
            throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                "Cannot decode extensible object structures without schema");
        }

        /// <summary>
        /// Read an encoded extension object
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        protected virtual ExtensionObject ReadEncodedDataType(string? fieldName)
        {
            var typeId = ReadNodeId("TypeId");
            var body = ReadByteString("Body");

            // convert to absolute node id.
            var expandedTypeId = NodeId.ToExpandedNodeId(typeId, Context.NamespaceUris);
            if (!NodeId.IsNull(typeId) && NodeId.IsNull(expandedTypeId))
            {
                ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Cannot de-serialized extension objects if the NamespaceUri " +
                    "is not in the NamespaceTable: Type = {0}", typeId);
            }

            var systemType = Context.Factory.GetSystemType(expandedTypeId);
            if (systemType != null)
            {
                var encodeable = Activator.CreateInstance(systemType) as IEncodeable;
                // set type identifier for custom complex data types before decode.
                if (encodeable is IComplexTypeInstance complexTypeInstance)
                {
                    complexTypeInstance.TypeId = expandedTypeId;
                }

                if (encodeable != null)
                {
                    if (encodeable.XmlEncodingId == expandedTypeId)
                    {
                        var document = new XmlDocument
                        {
                            InnerXml = Encoding.UTF8.GetString(body)
                        };
                        using var decoder = new XmlDecoder(document.DocumentElement, Context);
                        encodeable.Decode(decoder);
                        return new ExtensionObject(expandedTypeId, encodeable);
                    }
                    else if (encodeable.BinaryEncodingId == expandedTypeId)
                    {
                        using var decoder = new BinaryDecoder(body, Context);
                        encodeable.Decode(decoder);
                        return new ExtensionObject(expandedTypeId, encodeable);
                    }
                    else if (encodeable is IJsonEncodeable je &&
                        je.JsonEncodingId == expandedTypeId)
                    {
                        using var stream = new MemoryStream(body);
                        using var decoder = new JsonDecoderEx(stream, Context);
                        encodeable.Decode(decoder);
                        return new ExtensionObject(expandedTypeId, encodeable);
                    }
                }
            }
            return new ExtensionObject(expandedTypeId, body);
        }

        /// <summary>
        /// Read variant body
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="valueRank"></param>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        protected virtual Variant ReadVariantValue(BuiltInType builtInType,
            int valueRank = ValueRanks.Scalar)
        {
            var value = new Variant();
            if (valueRank <= 0)
            {
                switch (builtInType)
                {
                    case BuiltInType.Null:
                        value.Value = null;
                        break;
                    case BuiltInType.Boolean:
                        value.Set(ReadBoolean(null));
                        break;
                    case BuiltInType.SByte:
                        value.Set(ReadSByte(null));
                        break;
                    case BuiltInType.Byte:
                        value.Set(ReadByte(null));
                        break;
                    case BuiltInType.Int16:
                        value.Set(ReadInt16(null));
                        break;
                    case BuiltInType.UInt16:
                        value.Set(ReadUInt16(null));
                        break;
                    case BuiltInType.Int32:
                    case BuiltInType.Enumeration:
                        value.Set(ReadInt32(null));
                        break;
                    case BuiltInType.UInt32:
                        value.Set(ReadUInt32(null));
                        break;
                    case BuiltInType.Int64:
                        value.Set(ReadInt64(null));
                        break;
                    case BuiltInType.UInt64:
                        value.Set(ReadUInt64(null));
                        break;
                    case BuiltInType.Float:
                        value.Set(ReadFloat(null));
                        break;
                    case BuiltInType.Double:
                        value.Set(ReadDouble(null));
                        break;
                    case BuiltInType.String:
                        value.Set(ReadString(null));
                        break;
                    case BuiltInType.DateTime:
                        value.Set(ReadDateTime(null));
                        break;
                    case BuiltInType.Guid:
                        value.Set(ReadGuid(null));
                        break;
                    case BuiltInType.ByteString:
                        value.Set(ReadByteString(null));
                        break;
                    case BuiltInType.XmlElement:
                        try
                        {
                            value.Set(ReadXmlElement(null));
                        }
                        catch
                        {
                            value.Set(StatusCodes.BadDecodingError);
                        }
                        break;
                    case BuiltInType.NodeId:
                        value.Set(ReadNodeId(null));
                        break;
                    case BuiltInType.ExpandedNodeId:
                        value.Set(ReadExpandedNodeId(null));
                        break;
                    case BuiltInType.StatusCode:
                        value.Set(ReadStatusCode(null));
                        break;
                    case BuiltInType.QualifiedName:
                        value.Set(ReadQualifiedName(null));
                        break;
                    case BuiltInType.LocalizedText:
                        value.Set(ReadLocalizedText(null));
                        break;
                    case BuiltInType.ExtensionObject:
                        value.Set(ReadExtensionObject(null));
                        break;
                    case BuiltInType.DataValue:
                        value.Set(ReadDataValue(null));
                        break;
                    default:
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                            "Cannot decode unknown type in Variant object (0x{0:X2}).",
                            builtInType);
                }
            }
            else
            {
                var dimensions = Array.Empty<int>();
                if (valueRank > 1)
                {
                    // Read dimensions
                    dimensions = ReadArray("Dimensions", () => ReadInt32(null));
                }
 
                // read the array length.
                var length = ReadArrayLength();
                var array = ReadArrayElements(length, builtInType);
                if (array == null)
                {
                    value = new Variant(StatusCodes.BadDecodingError);
                }
                else
                {
                    if (dimensions.Length > 1) // Value Rank > 1
                    {
                        (var valid, var matrixLength) = Matrix.ValidateDimensions(
                            dimensions, length, Context.MaxArrayLength);
                        if (!valid || (matrixLength != length))
                        {
                            throw new ServiceResultException(StatusCodes.BadDecodingError,
                                "ArrayDimensions does not match with the ArrayLength " +
                                "in Variant object.");
                        }
                        value = new Variant(new Matrix(array, builtInType, dimensions));
                    }
                    else
                    {
                        value = new Variant(array, new TypeInfo(builtInType, 1));
                    }
                }
            }
            return value;
        }

        // TODO: Decide whether the opc ua types are records with single field
        internal static ReadOnlySpan<(int, BuiltInType)> _variantUnionFieldIds
            => new (int, BuiltInType)[]
        {
            (ValueRanks.Scalar, BuiltInType.Null), // 0,
            (ValueRanks.Scalar, BuiltInType.Boolean), // 1,
            (ValueRanks.Scalar, BuiltInType.SByte), // 2,
            (ValueRanks.Scalar, BuiltInType.Byte), // 3,
            (ValueRanks.Scalar, BuiltInType.Int16), // 4,
            (ValueRanks.Scalar, BuiltInType.UInt16), // 5,
            (ValueRanks.Scalar, BuiltInType.Int32), // 6,
            (ValueRanks.Scalar, BuiltInType.UInt32), // 7,
            (ValueRanks.Scalar, BuiltInType.Int64), // 8,
            (ValueRanks.Scalar, BuiltInType.UInt64), // 8,
            (ValueRanks.Scalar, BuiltInType.Float), // 10,
            (ValueRanks.Scalar, BuiltInType.Double), // 11,
            (ValueRanks.Scalar, BuiltInType.String), // 12,
            (ValueRanks.Scalar, BuiltInType.DateTime), // 13,
            (ValueRanks.Scalar, BuiltInType.Guid), // 14,
            (ValueRanks.Scalar, BuiltInType.ByteString), // 15,
            (ValueRanks.Scalar, BuiltInType.XmlElement), // 16,
            (ValueRanks.Scalar, BuiltInType.NodeId), // 17,
            (ValueRanks.Scalar, BuiltInType.ExpandedNodeId), // 18,
            (ValueRanks.Scalar, BuiltInType.StatusCode), // 19,
            (ValueRanks.Scalar, BuiltInType.QualifiedName), // 20,
            (ValueRanks.Scalar, BuiltInType.LocalizedText), // 21,
            (ValueRanks.Scalar, BuiltInType.ExtensionObject), // 22,
            (ValueRanks.Scalar, BuiltInType.DataValue), // 23,
            (ValueRanks.Scalar, BuiltInType.DiagnosticInfo), // 24,
            (ValueRanks.Scalar, BuiltInType.Number), // 25,
            (ValueRanks.Scalar, BuiltInType.Integer), // 26,
            (ValueRanks.Scalar, BuiltInType.UInteger), // 27,
            (ValueRanks.Scalar, BuiltInType.Enumeration), // 28,
            (ValueRanks.OneDimension, BuiltInType.Boolean), // 29,
            (ValueRanks.OneDimension, BuiltInType.SByte), // 30,
            (ValueRanks.OneDimension, BuiltInType.Byte), // 31,
            (ValueRanks.OneDimension, BuiltInType.Int16), // 32,
            (ValueRanks.OneDimension, BuiltInType.UInt16), // 33,
            (ValueRanks.OneDimension, BuiltInType.Int32), // 34,
            (ValueRanks.OneDimension, BuiltInType.UInt32), // 35,
            (ValueRanks.OneDimension, BuiltInType.Int64), // 36,
            (ValueRanks.OneDimension, BuiltInType.UInt64), // 37,
            (ValueRanks.OneDimension, BuiltInType.Float), // 38,
            (ValueRanks.OneDimension, BuiltInType.Double), // 39,
            (ValueRanks.OneDimension, BuiltInType.String), // 40,
            (ValueRanks.OneDimension, BuiltInType.DateTime), // 41,
            (ValueRanks.OneDimension, BuiltInType.Guid), // 42,
            (ValueRanks.OneDimension, BuiltInType.ByteString), // 43,
            (ValueRanks.OneDimension, BuiltInType.XmlElement), // 44,
            (ValueRanks.OneDimension, BuiltInType.NodeId), // 45,
            (ValueRanks.OneDimension, BuiltInType.ExpandedNodeId), // 46,
            (ValueRanks.OneDimension, BuiltInType.StatusCode), // 47,
            (ValueRanks.OneDimension, BuiltInType.QualifiedName), // 48,
            (ValueRanks.OneDimension, BuiltInType.LocalizedText), // 49,
            (ValueRanks.OneDimension, BuiltInType.ExtensionObject), // 50,
            (ValueRanks.OneDimension, BuiltInType.DataValue), // 51,
            (ValueRanks.OneDimension, BuiltInType.Variant), // 52,
            (ValueRanks.OneDimension, BuiltInType.DiagnosticInfo), // 53,
            (ValueRanks.OneDimension, BuiltInType.Number), // 54,
            (ValueRanks.OneDimension, BuiltInType.Integer), // 55,
            (ValueRanks.OneDimension, BuiltInType.UInteger), // 56,
            (ValueRanks.OneDimension, BuiltInType.Enumeration), // 57
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Boolean), // 58,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.SByte), // 59,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Byte), // 60,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Int16), // 61,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.UInt16), // 62,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Int32), // 63,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.UInt32), // 64,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Int64), // 65,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.UInt64), // 66,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Float), // 67,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Double), // 68,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.String), // 69,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.DateTime), // 70,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Guid), // 71,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.ByteString), // 72,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.XmlElement), // 73,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.NodeId), // 74,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.ExpandedNodeId), // 75,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.StatusCode), // 76,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.QualifiedName), // 77,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.LocalizedText), // 78,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.ExtensionObject), // 79,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.DataValue), // 80,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Variant), // 81,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.DiagnosticInfo), // 82,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Number), // 83,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Integer), // 84,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.UInteger), // 85,
            (ValueRanks.OneOrMoreDimensions, BuiltInType.Enumeration) // 86
        };

        /// <summary>
        /// Read node id
        /// </summary>
        /// <param name="namespaceIndex"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private NodeId ReadNodeId(ushort namespaceIndex)
        {
            const string kIdentifierName = "Identifier";
            switch ((IdType)ReadUnionSelector()) // Union field id
            {
                case IdType.Numeric:
                    return new NodeId(ReadUInt32(kIdentifierName), namespaceIndex);
                case IdType.String:
                    return new NodeId(ReadString(kIdentifierName), namespaceIndex);
                case IdType.Guid:
                    return new NodeId(ReadGuid(kIdentifierName), namespaceIndex);
                case IdType.Opaque:
                    return new NodeId(ReadByteString(kIdentifierName), namespaceIndex);
                default:
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unknown node id type");
            }
        }

        /// <summary>
        /// Test and increment the nesting level.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private void CheckAndIncrementNestingLevel()
        {
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }
            _nestingLevel++;
        }

        private readonly AvroBinaryReader _reader;
        private ushort[]? _namespaceMappings;
        private ushort[]? _serverMappings;
        private uint _nestingLevel;
    }
}
