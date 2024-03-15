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
    using System.Xml;

    /// <summary>
    /// Decodes objects from a Avro Binary encoded stream.
    /// </summary>
    internal sealed class AvroDecoderCore : IDecoder
    {
        /// <inheritdoc/>
        public EncodingType EncodingType => (EncodingType)3;

        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        /// <summary>
        /// Outer decoders need a way to get encodeables to
        /// use them.
        /// </summary>
        public IDecoder EncodeableDecoder { get; set; }

        /// <summary>
        /// Create avro decoder
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        public AvroDecoderCore(Stream stream, IServiceMessageContext context)
        {
            EncodeableDecoder = this;
            _reader = new AvroReader(stream)
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
            _reader.Dispose();
        }

        /// <inheritdoc/>
        public void Close()
        {
            // Dispose
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
        public void SetMappingTables(NamespaceTable? namespaceUris,
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
        public bool ReadBoolean(string? fieldName)
        {
            return _reader.ReadBoolean();
        }

        /// <inheritdoc/>
        public sbyte ReadSByte(string? fieldName)
        {
            return (sbyte)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public byte ReadByte(string? fieldName)
        {
            return (byte)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public short ReadInt16(string? fieldName)
        {
            return (short)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public ushort ReadUInt16(string? fieldName)
        {
            return (ushort)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public int ReadInt32(string? fieldName)
        {
            return (int)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public uint ReadUInt32(string? fieldName)
        {
            return (uint)_reader.ReadInteger();
        }

        /// <inheritdoc/>
        public long ReadInt64(string? fieldName)
        {
            return _reader.ReadInteger();
        }

        /// <inheritdoc/>
        public ulong ReadUInt64(string? fieldName)
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
        public float ReadFloat(string? fieldName)
        {
            return _reader.ReadFloat();
        }

        /// <inheritdoc/>
        public double ReadDouble(string? fieldName)
        {
            return _reader.ReadDouble();
        }

        /// <inheritdoc/>
        public Uuid ReadGuid(string? fieldName)
        {
            Span<byte> bytes = stackalloc byte[16];
            _reader.ReadFixed(bytes);
            return new Uuid(new Guid(bytes));
        }

        /// <inheritdoc/>
        public string? ReadString(string? fieldName)
        {
            return ReadNullable(_reader.ReadString);
        }

        /// <inheritdoc/>
        public DateTime ReadDateTime(string? fieldName)
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
        public byte[]? ReadByteString(string? fieldName)
        {
            return ReadNullable(_reader.ReadBytes);
        }

        /// <inheritdoc/>
        public XmlElement? ReadXmlElement(string? fieldName)
        {
            var xmlString = ReadString(fieldName);
            if (xmlString == null)
            {
                return null;
            }
            try
            {
                var document = new XmlDocument();
                using (var stream = new StringReader(xmlString))
                using (var reader = XmlReader.Create(stream,
                    Opc.Ua.Utils.DefaultXmlReaderSettings()))
                {
                    document.Load(reader);
                }
                return document.DocumentElement;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// Read non nullable xml element
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        internal XmlElement ReadXmlElement()
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
        public NodeId ReadNodeId(string? fieldName)
        {
            //
            // Node id is a record, with namespace and union of
            // the id. The IdType value represents the union
            // discriminator.
            //
            // Node id is not nullable, i=0 is a null node id.
            //

            var namespaceIndex = (ushort)_reader.ReadInteger();

            if (_namespaceMappings != null && _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }

            switch ((IdType)_reader.ReadInteger()) // Union field id
            {
                case IdType.Numeric:
                    return new NodeId(ReadUInt32(null), namespaceIndex);
                case IdType.String:
                    return new NodeId(_reader.ReadString(), namespaceIndex);
                case IdType.Guid:
                    return new NodeId(ReadGuid(null), namespaceIndex);
                case IdType.Opaque:
                    return new NodeId(_reader.ReadBytes(), namespaceIndex);
                default:
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unknown node id type");
            }
        }

        /// <inheritdoc/>
        public ExpandedNodeId ReadExpandedNodeId(string? fieldName)
        {
            //
            // Expanded Node id is a record extending NodeId.
            // Namespace, and union of via IdType which represents
            // the union discriminator.  After that we write the
            // namespace uri and then server index.
            //
            // ExpandedNode id is not nullable, i=0 is a null node id.
            //
            var innerNodeId = ReadNodeId(null);

            var nsUri = _reader.ReadString();
            var serverIndex = ReadUInt32(null);

            if (NodeId.IsNull(innerNodeId))
            {
                return ExpandedNodeId.Null;
            }

            if (_serverMappings != null && _serverMappings.Length > serverIndex)
            {
                serverIndex = _serverMappings[serverIndex];
            }
            return new ExpandedNodeId(innerNodeId, nsUri, serverIndex);
        }

        /// <inheritdoc/>
        public StatusCode ReadStatusCode(string? fieldName)
        {
            return (StatusCode)ReadUInt32(fieldName);
        }

        /// <inheritdoc/>
        public DiagnosticInfo? ReadDiagnosticInfo(string? fieldName)
        {
            return ReadNullableDiagnosticInfo(0);
        }

        /// <inheritdoc/>
        public QualifiedName? ReadQualifiedName(string? fieldName)
        {
            return ReadNullable(ReadQualifiedName);
        }

        /// <summary>
        /// Read non-nullable qualified name
        /// </summary>
        /// <returns></returns>
        internal QualifiedName ReadQualifiedName()
        {
            var namespaceIndex = ReadUInt16(null);
            var name = _reader.ReadString();
            if (_namespaceMappings != null && _namespaceMappings.Length > namespaceIndex)
            {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            return new QualifiedName(name, namespaceIndex);
        }

        /// <inheritdoc/>
        public LocalizedText? ReadLocalizedText(string? fieldName)
        {
            return ReadNullable(ReadLocalizedText);
        }

        /// <summary>
        /// Read non-nullable localized text
        /// </summary>
        /// <returns></returns>
        internal LocalizedText ReadLocalizedText()
        {
            return new LocalizedText(_reader.ReadString(), _reader.ReadString());
        }

        /// <inheritdoc/>
        public Variant ReadVariant(string? fieldName)
        {
            CheckAndIncrementNestingLevel();
            try
            {
                return ReadVariantValue();
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <inheritdoc/>
        public DataValue? ReadDataValue(string? fieldName)
        {
            return ReadNullable(ReadDataValue);
        }

        /// <inheritdoc/>
        public DataSet ReadDataSet()
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

        /// <summary>
        /// Read nullable value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal T? ReadNullable<T>(Func<T> reader)
        {
            var unionId = _reader.ReadInteger();
            if (unionId == 0)
            {
                return default;
            }
            Debug.Assert(unionId == 1);
            return reader();
        }

        /// <summary>
        /// Read non nullable data value
        /// </summary>
        /// <returns></returns>
        internal DataValue ReadDataValue()
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
        public ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            return ReadNullable(ReadExtensionObject);
        }

        /// <inheritdoc/>
        public IEncodeable ReadEncodeable(string? fieldName, System.Type systemType,
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
                encodeable.Decode(EncodeableDecoder);
            }
            finally
            {
                _nestingLevel--;
            }

            return encodeable;
        }

        /// <inheritdoc/>
        public Enum ReadEnumerated(string? fieldName, System.Type enumType)
        {
            return (Enum)Enum.ToObject(enumType, _reader.ReadInteger());
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal T[]? ReadNullableCollection<T>(Func<T> reader)
        {
            return ReadNullable(() => ReadCollection(reader));
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public T[] ReadCollection<T>(Func<T> reader)
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
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Array? ReadNullableCollection(Func<object> reader,
            System.Type type)
        {
            return ReadNullable(() => ReadCollection(reader, type));
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        internal Array ReadCollection(Func<object> reader, System.Type type)
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

        /// <inheritdoc/>
        public BooleanCollection? ReadBooleanArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadBoolean(null));
        }

        /// <inheritdoc/>
        public SByteCollection? ReadSByteArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadSByte(null));
        }

        /// <inheritdoc/>
        public ByteCollection? ReadByteArray(string? fieldName)
        {
            // TODO: Read fixed bytes instead
            return ReadNullableCollection(() => ReadByte(null));
        }

        /// <inheritdoc/>
        public Int16Collection? ReadInt16Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadInt16(null));
        }

        /// <inheritdoc/>
        public UInt16Collection? ReadUInt16Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadUInt16(null));
        }

        /// <inheritdoc/>
        public Int32Collection? ReadInt32Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadInt32(null));
        }

        /// <inheritdoc/>
        public UInt32Collection? ReadUInt32Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadUInt32(null));
        }

        /// <inheritdoc/>
        public Int64Collection? ReadInt64Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadInt64(null));
        }

        /// <inheritdoc/>
        public UInt64Collection? ReadUInt64Array(string? fieldName)
        {
            return ReadNullableCollection(() => ReadUInt64(null));
        }

        /// <inheritdoc/>
        public FloatCollection? ReadFloatArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadFloat(null));
        }

        /// <inheritdoc/>
        public DoubleCollection? ReadDoubleArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadDouble(null));
        }

        /// <inheritdoc/>
        public StringCollection? ReadStringArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadString(null));
        }

        /// <inheritdoc/>
        public DateTimeCollection? ReadDateTimeArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadDateTime(null));
        }

        /// <inheritdoc/>
        public UuidCollection? ReadGuidArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadGuid(null));
        }

        /// <inheritdoc/>
        public ByteStringCollection? ReadByteStringArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadByteString(null));
        }

        /// <inheritdoc/>
        public XmlElementCollection? ReadXmlElementArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadXmlElement(null));
        }

        /// <inheritdoc/>
        public NodeIdCollection? ReadNodeIdArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadNodeId(null));
        }

        /// <inheritdoc/>
        public ExpandedNodeIdCollection? ReadExpandedNodeIdArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadExpandedNodeId(null));
        }

        /// <inheritdoc/>
        public StatusCodeCollection? ReadStatusCodeArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadStatusCode(null));
        }

        /// <inheritdoc/>
        public DiagnosticInfoCollection? ReadDiagnosticInfoArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadDiagnosticInfo(null));
        }

        /// <inheritdoc/>
        public QualifiedNameCollection? ReadQualifiedNameArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadQualifiedName(null));
        }

        /// <inheritdoc/>
        public LocalizedTextCollection? ReadLocalizedTextArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadLocalizedText(null));
        }

        /// <inheritdoc/>
        public VariantCollection? ReadVariantArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadVariant(null));
        }

        /// <inheritdoc/>
        public DataValueCollection? ReadDataValueArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadDataValue(null));
        }

        /// <inheritdoc/>
        public ExtensionObjectCollection? ReadExtensionObjectArray(string? fieldName)
        {
            return ReadNullableCollection(() => ReadExtensionObject(null));
        }

        /// <inheritdoc/>
        public Array? ReadEncodeableArray(string? fieldName, System.Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            return ReadNullableCollection(() => ReadEncodeable(null,
                systemType, encodeableTypeId), systemType);
        }

        /// <inheritdoc/>
        public Array? ReadEnumeratedArray(string? fieldName, System.Type enumType)
        {
            return ReadNullableCollection(() => ReadEnumerated(null,
                enumType), enumType);
        }

        /// <inheritdoc/>
        public Array? ReadArray(
            string? fieldName,
            int valueRank,
            BuiltInType builtInType,
            Type? systemType = null,
            ExpandedNodeId? encodeableTypeId = null)
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
                        throw new ServiceResultException(
                            StatusCodes.BadDecodingError,
                            Opc.Ua.Utils.Format("Cannot decode unknown type in Array object with BuiltInType: {0}.", builtInType));
                }
            }

            // two or more dimensions
            if (valueRank >= ValueRanks.TwoDimensions)
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
                throw ServiceResultException.Create(
                    StatusCodes.BadDecodingError,
                    "Unexpected null or empty Dimensions for multidimensional matrix.");
            }
            return null;
        }

        /// <summary>
        /// Reads a DiagnosticInfo from the stream.
        /// Limits the InnerDiagnosticInfo nesting level.
        /// </summary>
        /// <param name="depth"></param>
        private DiagnosticInfo? ReadNullableDiagnosticInfo(int depth)
        {
            return ReadNullable(() => ReadDiagnosticInfo(depth));
        }

        /// <summary>
        /// Reads a DiagnosticInfo from the stream.
        /// Limits the InnerDiagnosticInfo nesting level.
        /// </summary>
        /// <param name="depth"></param>
        /// <exception cref="ServiceResultException"></exception>
        private DiagnosticInfo ReadDiagnosticInfo(int depth)
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
                    SymbolicId = ReadInt32(null),
                    NamespaceUri = ReadInt32(null),
                    Locale = ReadInt32(null),
                    LocalizedText = ReadInt32(null),
                    AdditionalInfo = _reader.ReadString(),
                    InnerStatusCode = ReadStatusCode(null),
                    InnerDiagnosticInfo = ReadNullableDiagnosticInfo(depth + 1)
                };
            }
            finally
            {
                _nestingLevel--;
            }
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
        /// Reads an extension object from the stream.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private ExtensionObject ReadExtensionObject()
        {
            // Extension objects are records of fields
            // 1. Encoding Node Id
            // 2. A union of
            //   1. null
            //   2. A encodeable type
            //   3. A record with
            //     1. ExtensionObjectEncoding type enum
            //     2. bytes that are either binary opc ua or xml/json utf 8

            // 1.
            var typeId = ReadNodeId(null);

            // convert to absolute node id.
            var expandedTypeId = NodeId.ToExpandedNodeId(typeId, Context.NamespaceUris);
            if (!NodeId.IsNull(typeId) && NodeId.IsNull(expandedTypeId))
            {
                ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Cannot de-serialized extension objects if the NamespaceUri " +
                    "is not in the NamespaceTable: Type = {0}", typeId);
            }

            // 2. Read union field.
            var unionId = _reader.ReadInteger();
            switch (unionId)
            {
                case 0:
                    // 2.1
                    return new ExtensionObject(expandedTypeId);
                case 1:
                    // 2.2
                    var systemType = Context.Factory.GetSystemType(expandedTypeId);
                    if (systemType != null)
                    {
                        var encodeable = Activator.CreateInstance(systemType) as IEncodeable;
                        // set type identifier for custom complex data types before decode.
                        if (encodeable is IComplexTypeInstance complexTypeInstance)
                        {
                            complexTypeInstance.TypeId = expandedTypeId;
                        }

                        // decode body.
                        if (encodeable != null)
                        {
                            encodeable.Decode(EncodeableDecoder);
                            return new ExtensionObject(expandedTypeId, encodeable);
                        }
                    }
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unknown extension object encodeable Type = {0}", expandedTypeId);
                case 2:
                    // 2.3
                    switch ((ExtensionObjectEncoding)_reader.ReadInteger())
                    {
                        case ExtensionObjectEncoding.Binary:
                            return new ExtensionObject(expandedTypeId, _reader.ReadBytes());
                        case ExtensionObjectEncoding.Xml:
                            return new ExtensionObject(expandedTypeId, ReadXmlElement());
                        case ExtensionObjectEncoding.Json:
                            return new ExtensionObject(expandedTypeId, _reader.ReadString());
                        default:
                            throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                                "Unknown encoding type in extension object");
                    }
                default:
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "Unknown extension object union field: Type = {0}", unionId);
            }
        }

        /// <summary>
        /// Reads an Variant from the stream.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private Variant ReadVariantValue()
        {
            //
            // Variant always starts with dimensions of the variant.
            // In case of a scalar value this will be an empty
            // array which consumes 1 byte (length 0 zig zag encoded).
            //
            var dimensions = ReadCollection(() => ReadInt32(null));

            // Read Union discriminator for the variant
            var fieldId = ReadInt32(null);
            if (fieldId < 0 || fieldId >= _variantUnionFieldIds.Length)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Cannot decode unknown union field.", fieldId);
            }
            var (isArray, builtInType) = _variantUnionFieldIds[fieldId];
            return ReadVariantValue(builtInType, false, isArray, false, dimensions);
        }

        /// <summary>
        /// Read variant body
        /// </summary>
        /// <param name="builtInType"></param>
        /// <param name="isNullable"></param>
        /// <param name="isArray"></param>
        /// <param name="isMatrix"></param>
        /// <param name="dimensions"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        public Variant ReadVariantValue(BuiltInType builtInType, bool isNullable = false,
            bool isArray = false, bool isMatrix = false, int[]? dimensions = null)
        {
            if (isNullable)
            {
                var unionId = _reader.ReadInteger();
                if (unionId == 0)
                {
                    builtInType = BuiltInType.Null;
                }
                Debug.Assert(unionId == 1); // Throw
            }

            if (isMatrix)
            {
                // Read dimensions
                dimensions = ReadCollection(() => ReadInt32(null));
                isArray = true;
            }

            var value = new Variant();
            if (!isArray)
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
                        value.Set(_reader.ReadString());
                        break;
                    case BuiltInType.DateTime:
                        value.Set(ReadDateTime(null));
                        break;
                    case BuiltInType.Guid:
                        value.Set(ReadGuid(null));
                        break;
                    case BuiltInType.ByteString:
                        value.Set(_reader.ReadBytes());
                        break;
                    case BuiltInType.XmlElement:
                        try
                        {
                            value.Set(ReadXmlElement());
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
                        value.Set(ReadQualifiedName());
                        break;
                    case BuiltInType.LocalizedText:
                        value.Set(ReadLocalizedText());
                        break;
                    case BuiltInType.ExtensionObject:
                        value.Set(ReadExtensionObject());
                        break;
                    case BuiltInType.DataValue:
                        value.Set(ReadDataValue());
                        break;
                    default:
                        throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                            "Cannot decode unknown type in Variant object (0x{0:X2}).",
                            builtInType);
                }
            }
            else
            {
                // read the array length.
                var length = ReadArrayLength();
                var array = ReadArrayElements(length, builtInType);
                if (array == null)
                {
                    value = new Variant(StatusCodes.BadDecodingError);
                }
                else
                {
                    if (dimensions?.Length > 1) // Value Rank > 1
                    {
                        (var valid, var matrixLength) = Matrix.ValidateDimensions(
                            dimensions, length, Context.MaxArrayLength);
                        if (!valid || (matrixLength != length))
                        {
                            throw new ServiceResultException(StatusCodes.BadDecodingError,
                                "ArrayDimensions does not match with the ArrayLength " +
                                "in Variant object.");
                        }
                        value = new Variant(new Matrix(array,
                            builtInType, dimensions));
                    }
                    else
                    {
                        value = new Variant(array,
                            new TypeInfo(builtInType, 1));
                    }
                }
            }

            return value;
        }

        // TODO: Decide whether the opc ua types are records with single field
        private static ReadOnlySpan<(bool, BuiltInType)> _variantUnionFieldIds => new (bool, BuiltInType)[]
        {
            (false, BuiltInType.Null), // 1,
            (false, BuiltInType.Boolean), // 1,
            (false, BuiltInType.SByte), // 2,
            (false, BuiltInType.Byte), // 3,
            (false, BuiltInType.Int16), // 4,
            (false, BuiltInType.UInt16), // 5,
            (false, BuiltInType.Int32), // 6,
            (false, BuiltInType.UInt32), // 7,
            (false, BuiltInType.Int64), // 8,
            (false, BuiltInType.UInt64), // 8,
            (false, BuiltInType.Float), // 10,
            (false, BuiltInType.Double), // 11,
            (false, BuiltInType.String), // 12,
            (false, BuiltInType.DateTime), // 13,
            (false, BuiltInType.Guid), // 14,
            (false, BuiltInType.ByteString), // 15,
            (false, BuiltInType.XmlElement), // 16,
            (false, BuiltInType.NodeId), // 17,
            (false, BuiltInType.ExpandedNodeId), // 18,
            (false, BuiltInType.StatusCode), // 19,
            (false, BuiltInType.QualifiedName), // 20,
            (false, BuiltInType.LocalizedText), // 21,
            (false, BuiltInType.ExtensionObject), // 22,
            (false, BuiltInType.DataValue), // 23,
            (false, BuiltInType.DiagnosticInfo), // 24,
            (false, BuiltInType.Number), // 25,
            (false, BuiltInType.Integer), // 26,
            (false, BuiltInType.UInteger), // 27,
            (false, BuiltInType.Enumeration), // 28,
            (true, BuiltInType.Boolean), // 29,
            (true, BuiltInType.SByte), // 30,
            (true, BuiltInType.Byte), // 31,
            (true, BuiltInType.Int16), // 32,
            (true, BuiltInType.UInt16), // 33,
            (true, BuiltInType.Int32), // 34,
            (true, BuiltInType.UInt32), // 35,
            (true, BuiltInType.Int64), // 36,
            (true, BuiltInType.UInt64), // 37,
            (true, BuiltInType.Float), // 38,
            (true, BuiltInType.Double), // 39,
            (true, BuiltInType.String), // 40,
            (true, BuiltInType.DateTime), // 41,
            (true, BuiltInType.Guid), // 42,
            (true, BuiltInType.ByteString), // 43,
            (true, BuiltInType.XmlElement), // 44,
            (true, BuiltInType.NodeId), // 45,
            (true, BuiltInType.ExpandedNodeId), // 46,
            (true, BuiltInType.StatusCode), // 47,
            (true, BuiltInType.QualifiedName), // 48,
            (true, BuiltInType.LocalizedText), // 49,
            (true, BuiltInType.ExtensionObject), // 50,
            (true, BuiltInType.DataValue), // 51,
            (true, BuiltInType.Variant), // 52,
            (true, BuiltInType.DiagnosticInfo), // 53,
            (true, BuiltInType.Number), // 54,
            (true, BuiltInType.Integer), // 55,
            (true, BuiltInType.UInteger), // 56,
            (true, BuiltInType.Enumeration) // 57
        };

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

        private readonly AvroReader _reader;
        private ushort[]? _namespaceMappings;
        private ushort[]? _serverMappings;
        private uint _nestingLevel;
    }
}
