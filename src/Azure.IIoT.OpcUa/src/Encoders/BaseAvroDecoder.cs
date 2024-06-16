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
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.IO;
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
        /// <param name="leaveOpen"></param>
        protected BaseAvroDecoder(Stream stream, IServiceMessageContext context,
            bool leaveOpen = false)
        {
            _reader = new AvroBinaryReader(stream, leaveOpen)
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
#if UUID_FIXED
            Span<byte> bytes = stackalloc byte[16];
            _reader.ReadFixed(bytes);
            return new Uuid(new Guid(bytes));
#else
            var uuid = _reader.ReadString();
            return new Uuid(Guid.Parse(uuid));
#endif
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
                throw new DecodingException("Xml element invalid", ex);
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

            var innerNodeId = ReadNodeId(namespaceIndex);
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
                return ReadUnion(fieldName, fieldId =>
                {
                    if (fieldId < 0 || fieldId >= _variantUnionFieldIds.Length)
                    {
                        throw new DecodingException(
                            $"Cannot decode unknown variant union field {fieldId}.");
                    }
                    var (valueRank, builtInType) = _variantUnionFieldIds[fieldId];
                    return ReadVariantValue(builtInType, valueRank);
                });
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
                WrappedValue = ReadVariant(nameof(DataValue.Value)),
                StatusCode = ReadStatusCode(nameof(DataValue.StatusCode)),
                SourceTimestamp = ReadDateTime(nameof(DataValue.SourceTimestamp)),
                SourcePicoseconds = ReadUInt16(nameof(DataValue.SourcePicoseconds)),
                ServerTimestamp = ReadDateTime(nameof(DataValue.ServerTimestamp)),
                ServerPicoseconds = ReadUInt16(nameof(DataValue.ServerPicoseconds))
            };
        }

        /// <inheritdoc/>
        public virtual T ReadObject<T>(string? fieldName, Func<object?, T> reader)
        {
            return reader(null);
        }

        /// <inheritdoc/>
        public virtual DataSet ReadDataSet(string? fieldName)
        {
            return ReadUnion(fieldName, avroFieldContent =>
            {
                var fieldNames = Array.Empty<string>();
                var dataSet = avroFieldContent == 0 ?
                    new DataSet() :
                    new DataSet(DataSetFieldContentFlags.RawData);

                if (avroFieldContent == 1) // Raw mode
                {
                    //
                    // Read map of raw variant
                    //
                    var variants = ReadVariantArray(null); // TODO: Read map
                    if (variants == null && fieldNames.Length == 0)
                    {
                        return dataSet;
                    }
                    if (variants == null || variants.Count != fieldNames.Length)
                    {
                        throw new DecodingException(
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
                    // Read map of data values
                    //
                    var dataValues = ReadDataValueArray(null); // TODO: Read map
                    if (dataValues == null && fieldNames.Length == 0)
                    {
                        return dataSet;
                    }
                    if (dataValues == null || dataValues.Count != fieldNames.Length)
                    {
                        throw new DecodingException(
                            "Unexpected number of fields in data set");
                    }
                    for (var index = 0; index < fieldNames.Length; index++)
                    {
                        dataSet.Add(fieldNames[index], dataValues[index]);
                    }
                }
                return dataSet;
            });
        }

        /// <inheritdoc/>
        public virtual ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            return ReadUnion(fieldName, unionId =>
            {
                if (unionId != 0)
                {
                    return new ExtensionObject(ReadEncodeableInExtensionObject(unionId));
                }
                return ReadEncodedDataType(fieldName);
            });
        }

        /// <inheritdoc/>
        public virtual IEncodeable ReadEncodeable(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            if (Activator.CreateInstance(systemType) is not IEncodeable encodeable)
            {
                throw new DecodingException(
                    $"Cannot decode type '{systemType.FullName}'.");
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
            return (Enum)Enum.ToObject(enumType, ReadEnumerated(fieldName));
        }

        /// <inheritdoc/>
        public virtual int ReadEnumerated(string? fieldName)
        {
            return (int)_reader.ReadInteger();
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
            return _reader.ReadBytes();
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
        public virtual int[] ReadEnumeratedArray(string? fieldName)
        {
            return ReadArray(fieldName, () => ReadEnumerated(null));
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
            var rank = SchemaUtils.GetRank(valueRank);
            if (rank == SchemaRank.Scalar)
            {
                return null;
            }
            var array = ReadArray(fieldName, builtInType, systemType, encodeableTypeId);
            if (array == null || rank == SchemaRank.Collection)
            {
                return array;
            }
            // read as matrix
            var dimensions = ReadInt32Array(null);
            if (dimensions?.Count > 0)
            {
                Matrix.ValidateDimensions(false, dimensions, Context.MaxArrayLength);
                return new Matrix(array, builtInType, dimensions.ToArray()).ToArray();
            }
            throw new DecodingException(
                "Unexpected null or empty Dimensions for multidimensional matrix.");
        }

        /// <summary>
        /// Reads a DiagnosticInfo from the stream.
        /// Limits the InnerDiagnosticInfo nesting level.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="depth"></param>
        /// <exception cref="DecodingException"></exception>
        protected virtual DiagnosticInfo ReadDiagnosticInfo(string? fieldName, int depth)
        {
            if (depth >= DiagnosticInfo.MaxInnerDepth)
            {
                throw new DecodingException(StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of InnerDiagnosticInfo was exceeded.");
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
                    InnerDiagnosticInfo = ReadNullable(nameof(DiagnosticInfo.InnerDiagnosticInfo),
                        f => ReadDiagnosticInfo(f, depth + 1))
                };
            }
            finally
            {
                _nestingLevel--;
            }
        }

        /// <summary>
        /// Read null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        public virtual T? ReadNull<T>(string? fieldName)
        {
            // Nothing to do
            return default;
        }

        /// <summary>
        /// Read nullable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        protected virtual T? ReadNullable<T>(string? fieldName, Func<string?, T> reader)
        {
            return ReadUnion(fieldName, id =>
            {
                switch (id)
                {
                    case 0:
                        return ReadNull<T>(fieldName);
                    case 1:
                        return reader(fieldName);
                    default:
                        throw new DecodingException(
                            $"Unexpected union discriminator {id}.");
                }
            });
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        public virtual T[] ReadArray<T>(string? fieldName, Func<T> reader)
        {
            return ReadArray(reader).ToArray();
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Array ReadArray(string? fieldName,
            Func<object> reader, Type type)
        {
            var result = ReadArray(reader);
            var array = Array.CreateInstance(type, result.Count);

            // TODO: Can we just cast to Array?
            for (var i = 0; i < result.Count; i++)
            {
                array.SetValue(result[i], i);
            }
            return array;
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private List<T> ReadArray<T>(Func<T> reader)
        {
            var result = new List<T>();
            var length = _reader.ReadInteger();
            do
            {
                var newLength = length + result.Count;
                if (newLength < 0 || newLength > int.MaxValue ||
                    (Context.MaxArrayLength > 0 && Context.MaxArrayLength < newLength))
                {
                    throw new DecodingException(StatusCodes.BadEncodingLimitsExceeded,
                        $"MaxArrayLength {Context.MaxArrayLength} < {length}.");
                }
                result.EnsureCapacity((int)newLength);
                for (var i = 0; i < length; i++)
                {
                    result.Add(reader());
                }

                // Either another length or last block indicator
                length = _reader.ReadInteger();
            }
            while (length > 0);
            return result;
        }

        /// <summary>
        /// Read union
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="reader"></param>
        public virtual T ReadUnion<T>(string? fieldName,
            Func<int, T> reader)
        {
            var id = StartUnion();
            var result = reader(id);
            EndUnion();
            return result;
        }

        /// <summary>
        /// Read union selector
        /// </summary>
        /// <returns></returns>
        protected virtual int StartUnion()
        {
            return (int)_reader.ReadInteger();
        }

        /// <summary>
        /// End union
        /// </summary>
        protected virtual void EndUnion()
        {
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
        /// Read extension object
        /// </summary>
        /// <param name="unionId"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        protected virtual IEncodeable ReadEncodeableInExtensionObject(int unionId)
        {
            throw new DecodingException(
                "Cannot decode extensible object structures without schema");
        }

        /// <summary>
        /// Read an encoded extension object
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        protected virtual ExtensionObject ReadEncodedDataType(string? fieldName)
        {
            var typeId = ReadNodeId("TypeId");
            var body = ReadByteString("Body");

            // convert to absolute node id.
            var expandedTypeId = NodeId.ToExpandedNodeId(typeId, Context.NamespaceUris);
            if (!NodeId.IsNull(typeId) && NodeId.IsNull(expandedTypeId))
            {
                throw new DecodingException(
                    "Cannot de-serialized extension objects if the NamespaceUri " +
                    $"is not in the NamespaceTable: Type = {typeId}");
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
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        protected virtual Variant ReadVariantValue(BuiltInType builtInType,
            SchemaRank valueRank = SchemaRank.Scalar)
        {
            if (valueRank == SchemaRank.Scalar)
            {
                return ReadScalar(null, builtInType);
            }

            // Read array
            var array = ReadArray(null, builtInType, null, null);
            if (array == null)
            {
                return new Variant(StatusCodes.BadDecodingError);
            }

            if (valueRank == SchemaRank.Collection)
            {
                return new Variant(array,
                    new TypeInfo(builtInType, ValueRanks.OneDimension));
            }

            // Read matrix
            var dimensions = ReadArray("Dimensions", () => ReadInt32(null));
            (var valid, var matrixLength) = Matrix.ValidateDimensions(
                dimensions, array.Length, Context.MaxArrayLength);
            if (!valid || (matrixLength != array.Length))
            {
                throw new DecodingException(
                    "ArrayDimensions does not match with the ArrayLength " +
                    "in Variant object.");
            }
            return new Variant(new Matrix(array, builtInType, dimensions));
        }

        /// <summary>
        /// Read scalar element
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        protected Variant ReadScalar(string? fieldName, BuiltInType builtInType)
        {
            var value = new Variant();
            switch (builtInType)
            {
                case BuiltInType.Null:
                    value.Value = ReadNull<object>(fieldName);
                    break;
                case BuiltInType.Boolean:
                    value.Set(ReadBoolean(fieldName));
                    break;
                case BuiltInType.SByte:
                    value.Set(ReadSByte(fieldName));
                    break;
                case BuiltInType.Byte:
                    value.Set(ReadByte(fieldName));
                    break;
                case BuiltInType.Int16:
                    value.Set(ReadInt16(fieldName));
                    break;
                case BuiltInType.UInt16:
                    value.Set(ReadUInt16(fieldName));
                    break;
                case BuiltInType.Int32:
                    value.Set(ReadInt32(fieldName));
                    break;
                case BuiltInType.Enumeration:
                    value.Set(ReadEnumerated(fieldName));
                    break;
                case BuiltInType.UInt32:
                    value.Set(ReadUInt32(fieldName));
                    break;
                case BuiltInType.Int64:
                    value.Set(ReadInt64(fieldName));
                    break;
                case BuiltInType.UInt64:
                    value.Set(ReadUInt64(fieldName));
                    break;
                case BuiltInType.Float:
                    value.Set(ReadFloat(fieldName));
                    break;
                case BuiltInType.Double:
                    value.Set(ReadDouble(fieldName));
                    break;
                case BuiltInType.String:
                    value.Set(ReadString(fieldName));
                    break;
                case BuiltInType.DateTime:
                    value.Set(ReadDateTime(fieldName));
                    break;
                case BuiltInType.Guid:
                    value.Set(ReadGuid(fieldName));
                    break;
                case BuiltInType.ByteString:
                    value.Set(ReadByteString(fieldName));
                    break;
                case BuiltInType.XmlElement:
                    try
                    {
                        value.Set(ReadXmlElement(fieldName));
                    }
                    catch
                    {
                        value.Set(StatusCodes.BadDecodingError);
                    }
                    break;
                case BuiltInType.NodeId:
                    value.Set(ReadNodeId(fieldName));
                    break;
                case BuiltInType.ExpandedNodeId:
                    value.Set(ReadExpandedNodeId(fieldName));
                    break;
                case BuiltInType.StatusCode:
                    value.Set(ReadStatusCode(fieldName));
                    break;
                case BuiltInType.QualifiedName:
                    value.Set(ReadQualifiedName(fieldName));
                    break;
                case BuiltInType.LocalizedText:
                    value.Set(ReadLocalizedText(fieldName));
                    break;
                case BuiltInType.ExtensionObject:
                    value.Set(ReadExtensionObject(fieldName));
                    break;
                case BuiltInType.DataValue:
                    value.Set(ReadDataValue(fieldName));
                    break;
                default:
                    throw new DecodingException(
                        $"Cannot decode unknown type in Variant object (0x{builtInType:X2}).");
            }
            return value;
        }

        /// <summary>
        /// Read array
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="systemType"></param>
        /// <param name="encodeableTypeId"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        protected Array? ReadArray(string? fieldName, BuiltInType builtInType,
            Type? systemType = null, ExpandedNodeId? encodeableTypeId = null)
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
                    return ReadEnumeratedArray(fieldName);
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
                    throw new DecodingException(
                        $"Cannot decode unknown type in Array object with BuiltInType: {builtInType}.");
            }
        }

        // TODO: Decide whether the opc ua types are records with single field
        internal static ReadOnlySpan<(SchemaRank, BuiltInType)> _variantUnionFieldIds
            => new (SchemaRank, BuiltInType)[]
        {
            (SchemaRank.Scalar, BuiltInType.Null),
            (SchemaRank.Scalar, BuiltInType.Boolean),
            (SchemaRank.Scalar, BuiltInType.SByte),
            (SchemaRank.Scalar, BuiltInType.Byte),
            (SchemaRank.Scalar, BuiltInType.Int16),
            (SchemaRank.Scalar, BuiltInType.UInt16),
            (SchemaRank.Scalar, BuiltInType.Int32),
            (SchemaRank.Scalar, BuiltInType.UInt32),
            (SchemaRank.Scalar, BuiltInType.Int64),
            (SchemaRank.Scalar, BuiltInType.UInt64),
            (SchemaRank.Scalar, BuiltInType.Float),
            (SchemaRank.Scalar, BuiltInType.Double),
            (SchemaRank.Scalar, BuiltInType.String),
            (SchemaRank.Scalar, BuiltInType.DateTime),
            (SchemaRank.Scalar, BuiltInType.Guid),
            (SchemaRank.Scalar, BuiltInType.ByteString),
            (SchemaRank.Scalar, BuiltInType.XmlElement),
            (SchemaRank.Scalar, BuiltInType.NodeId),
            (SchemaRank.Scalar, BuiltInType.ExpandedNodeId),
            (SchemaRank.Scalar, BuiltInType.StatusCode),
            (SchemaRank.Scalar, BuiltInType.QualifiedName),
            (SchemaRank.Scalar, BuiltInType.LocalizedText),
            (SchemaRank.Scalar, BuiltInType.ExtensionObject),
            (SchemaRank.Scalar, BuiltInType.DataValue),
            // (ValueRanks.Scalar, BuiltInType.Number),
            // (ValueRanks.Scalar, BuiltInType.Integer),
            // (ValueRanks.Scalar, BuiltInType.UInteger),
            (SchemaRank.Scalar, BuiltInType.Enumeration),
            (SchemaRank.Collection, BuiltInType.Boolean),
            (SchemaRank.Collection, BuiltInType.SByte),
            // (SchemaRank.Collection, BuiltInType.Byte),
            (SchemaRank.Collection, BuiltInType.Int16),
            (SchemaRank.Collection, BuiltInType.UInt16),
            (SchemaRank.Collection, BuiltInType.Int32),
            (SchemaRank.Collection, BuiltInType.UInt32),
            (SchemaRank.Collection, BuiltInType.Int64),
            (SchemaRank.Collection, BuiltInType.UInt64),
            (SchemaRank.Collection, BuiltInType.Float),
            (SchemaRank.Collection, BuiltInType.Double),
            (SchemaRank.Collection, BuiltInType.String),
            (SchemaRank.Collection, BuiltInType.DateTime),
            (SchemaRank.Collection, BuiltInType.Guid),
            (SchemaRank.Collection, BuiltInType.ByteString),
            (SchemaRank.Collection, BuiltInType.XmlElement),
            (SchemaRank.Collection, BuiltInType.NodeId),
            (SchemaRank.Collection, BuiltInType.ExpandedNodeId),
            (SchemaRank.Collection, BuiltInType.StatusCode),
            (SchemaRank.Collection, BuiltInType.QualifiedName),
            (SchemaRank.Collection, BuiltInType.LocalizedText),
            (SchemaRank.Collection, BuiltInType.ExtensionObject),
            (SchemaRank.Collection, BuiltInType.DataValue),
            (SchemaRank.Collection, BuiltInType.Variant),
            //(SchemaRank.Collection, BuiltInType.Number),
            //(SchemaRank.Collection, BuiltInType.Integer),
            //(SchemaRank.Collection, BuiltInType.UInteger),
            (SchemaRank.Collection, BuiltInType.Enumeration),
            (SchemaRank.Matrix, BuiltInType.Boolean),
            (SchemaRank.Matrix, BuiltInType.SByte),
            (SchemaRank.Matrix, BuiltInType.Byte),
            (SchemaRank.Matrix, BuiltInType.Int16),
            (SchemaRank.Matrix, BuiltInType.UInt16),
            (SchemaRank.Matrix, BuiltInType.Int32),
            (SchemaRank.Matrix, BuiltInType.UInt32),
            (SchemaRank.Matrix, BuiltInType.Int64),
            (SchemaRank.Matrix, BuiltInType.UInt64),
            (SchemaRank.Matrix, BuiltInType.Float),
            (SchemaRank.Matrix, BuiltInType.Double),
            (SchemaRank.Matrix, BuiltInType.String),
            (SchemaRank.Matrix, BuiltInType.DateTime),
            (SchemaRank.Matrix, BuiltInType.Guid),
            (SchemaRank.Matrix, BuiltInType.ByteString),
            (SchemaRank.Matrix, BuiltInType.XmlElement),
            (SchemaRank.Matrix, BuiltInType.NodeId),
            (SchemaRank.Matrix, BuiltInType.ExpandedNodeId),
            (SchemaRank.Matrix, BuiltInType.StatusCode),
            (SchemaRank.Matrix, BuiltInType.QualifiedName),
            (SchemaRank.Matrix, BuiltInType.LocalizedText),
            (SchemaRank.Matrix, BuiltInType.ExtensionObject),
            (SchemaRank.Matrix, BuiltInType.DataValue),
            (SchemaRank.Matrix, BuiltInType.Variant),
            //(SchemaRank.Matrix, BuiltInType.Number),
            //(SchemaRank.Matrix, BuiltInType.Integer),
            //(SchemaRank.Matrix, BuiltInType.UInteger),
            (SchemaRank.Matrix, BuiltInType.Enumeration)
        };

        /// <summary>
        /// Read node id
        /// </summary>
        /// <param name="namespaceIndex"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private NodeId ReadNodeId(ushort namespaceIndex)
        {
            const string kIdentifierName = "Identifier";
            return ReadUnion(kIdentifierName, idType =>
            {
                switch ((IdType)idType)
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
                        throw new DecodingException("Unknown node id type");
                }
            });
        }

        /// <summary>
        /// Test and increment the nesting level.
        /// </summary>
        /// <exception cref="DecodingException"></exception>
        private void CheckAndIncrementNestingLevel()
        {
            if (_nestingLevel > Context.MaxEncodingNestingLevels)
            {
                throw new DecodingException(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded.");
            }
            _nestingLevel++;
        }

        private readonly AvroBinaryReader _reader;
        private ushort[]? _namespaceMappings;
        private ushort[]? _serverMappings;
        private uint _nestingLevel;
    }
}
