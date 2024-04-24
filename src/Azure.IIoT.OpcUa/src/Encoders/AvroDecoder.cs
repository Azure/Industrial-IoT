// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Avro;
    using Opc.Ua;
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Decodes objects from underlying decoder using a provided
    /// Avro schema. Validation errors throw.
    /// </summary>
    public sealed class AvroDecoder : BaseAvroDecoder
    {
        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Current => _schema.Current;

        /// <summary>
        /// Creates a decoder that decodes the data from the
        /// passed in stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="schema"></param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        /// <param name="leaveOpen"></param>
        public AvroDecoder(Stream stream, Schema schema,
            IServiceMessageContext context, bool leaveOpen = false) :
            base(stream, context, leaveOpen)
        {
            Schema = schema;
            _schema = new AvroSchemaTraverser(schema);
        }

        /// <inheritdoc/>
        public override bool ReadBoolean(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Boolean,
                base.ReadBoolean);
        }

        /// <inheritdoc/>
        public override sbyte ReadSByte(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.SByte,
                base.ReadSByte);
        }

        /// <inheritdoc/>
        public override byte ReadByte(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Byte,
                base.ReadByte);
        }

        /// <inheritdoc/>
        public override short ReadInt16(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int16,
                base.ReadInt16);
        }

        /// <inheritdoc/>
        public override ushort ReadUInt16(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt16,
                base.ReadUInt16);
        }

        /// <inheritdoc/>
        public override int ReadInt32(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int32,
                base.ReadInt32);
        }

        /// <inheritdoc/>
        public override uint ReadUInt32(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt32,
                base.ReadUInt32);
        }

        /// <inheritdoc/>
        public override long ReadInt64(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int64,
                base.ReadInt64);
        }

        /// <inheritdoc/>
        public override float ReadFloat(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Float,
                base.ReadFloat);
        }

        /// <inheritdoc/>
        public override double ReadDouble(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Double,
                base.ReadDouble);
        }

        /// <inheritdoc/>
        public override Uuid ReadGuid(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Guid,
                base.ReadGuid);
        }

        /// <inheritdoc/>
        public override string? ReadString(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.String,
                base.ReadString);
        }

        /// <inheritdoc/>
        public override ulong ReadUInt64(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt64,
                base.ReadUInt64);
        }

        /// <inheritdoc/>
        public override DateTime ReadDateTime(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DateTime,
                base.ReadDateTime);
        }

        /// <inheritdoc/>
        public override byte[] ReadByteString(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ByteString,
                base.ReadByteString);
        }

        /// <inheritdoc/>
        public override XmlElement ReadXmlElement(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.XmlElement,
                base.ReadXmlElement);
        }

        /// <inheritdoc/>
        public override StatusCode ReadStatusCode(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.StatusCode,
                base.ReadStatusCode);
        }

        /// <inheritdoc/>
        public override NodeId ReadNodeId(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
                base.ReadNodeId);
        }

        /// <inheritdoc/>
        public override ExpandedNodeId ReadExpandedNodeId(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExpandedNodeId,
                base.ReadExpandedNodeId);
        }

        /// <inheritdoc/>
        public override QualifiedName ReadQualifiedName(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.QualifiedName,
                base.ReadQualifiedName);
        }

        /// <inheritdoc/>
        public override LocalizedText ReadLocalizedText(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.LocalizedText,
                base.ReadLocalizedText);
        }

        /// <inheritdoc/>
        public override DataValue ReadDataValue(string? fieldName)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);
            // Should be data value compatible
            if (!currentSchema.IsDataValue())
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to decode. Schema {currentSchema.Fullname} is not " +
                    $"as expected {currentSchema.ToJson()}.\n{Schema.ToJson()}");
            }

            // Read type per schema
            var result = base.ReadDataValue(fieldName);

            // Pop the type from the stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
            }
            return result;
        }

        /// <inheritdoc/>
        public override Variant ReadVariant(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var expectedType = _builtIns.GetSchemaForBuiltInType(BuiltInType.Variant,
                SchemaRank.Scalar);
            try
            {
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    // Read as variant
                    return base.ReadVariant(fieldName);
                }

                // Alternatively the schema could be nullable
                if (currentSchema is UnionSchema u)
                {
                    // Read as nullable
                    _schema.Push(ArraySchema.Create(u));
                    var result = ReadNullable(fieldName, f => ReadVariant(f, u.Schemas[1]));
                    _schema.Pop();
                    return result;
                }

                return ReadVariant(fieldName, currentSchema);
            }
            finally
            {
                _schema.Pop();
            }

            Variant ReadVariant(string? fieldName, Schema currentSchema)
            {
                // Read as built in type
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                {
                    _schema.Push(ArraySchema.Create(currentSchema));
                    var value = ReadVariantValue(builtInType, rank);
                    _schema.Pop();
                    return value;
                }

                // Read as encodeable
                var typeId = currentSchema.GetDataTypeId(Context);
                var systemType = Context.Factory.GetSystemType(typeId);
                if (systemType != null)
                {
                    _schema.Push(ArraySchema.Create(currentSchema));
                    var value = ReadEncodeable(null, systemType, typeId);
                    _schema.Pop();
                    return new Variant(value);
                }

                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to decode. Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant nor built " +
                    $"in type schema. .\n{Schema.ToJson()}");
            }
        }

        /// <inheritdoc/>
        public override DataSet ReadDataSet(string? fieldName)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema r)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Invalid schema {schema.ToJson()}. " +
                    $"Data sets must be records or maps.\n{Schema.ToJson()}");
            }
            try
            {
                var dataSet = new DataSet();
                var isRaw = false;

                // Run through the fields and read either using variant or data values
                foreach (var field in r.Fields)
                {
                    var dataValue = ReadDataSetField(field.Name);
                    if (dataValue?.StatusCode == (StatusCode)uint.MaxValue)
                    {
                        isRaw = true;
                        dataValue.StatusCode = StatusCodes.Good;
                    }
                    dataSet.Add(SchemaUtils.Unescape(field.Name), dataValue);
                }
                if (isRaw)
                {
                    dataSet.DataSetFieldContentMask = (uint)DataSetFieldContentMask.RawData;
                }
                return dataSet;
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <summary>
        /// Read data set field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private DataValue? ReadDataSetField(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            try
            {
                if (currentSchema is UnionSchema u)
                {
                    _schema.Push(ArraySchema.Create(u));
                    var result = ReadNullable(null, _ =>
                    {
                        _schema.Push(((UnionSchema)Current).Schemas[1]);
                        var v = ReadDataSetFieldValue();
                        _schema.Pop();
                        return v!;
                    });
                    _schema.Pop();
                    return result;
                }
                return ReadDataSetFieldValue();
            }
            finally
            {
                _schema.Pop();
            }

            DataValue? ReadDataSetFieldValue()
            {
                var isRaw = true;
                if (Current is not RecordSchema fieldRecord)
                {
                    throw new ServiceResultException(StatusCodes.BadDecodingError,
                        $"Invalid schema {Current.ToJson()}." +
                        $"Data set fields must be records.\n{Schema.ToJson()}");
                }

                // The field is a record that should contain the data value fields
                if (fieldRecord.IsDataValue())
                {
                    var dataValue = new DataValue();
                    foreach (var dvf in fieldRecord.Fields)
                    {
                        switch (dvf.Name)
                        {
                            case nameof(DataValue.Value):
                                dataValue.Value =
                                    ReadVariant(nameof(DataValue.Value));
                                break;
                            case nameof(DataValue.SourceTimestamp):
                                isRaw = false;
                                dataValue.SourceTimestamp =
                                    ReadDateTime(nameof(DataValue.SourceTimestamp));
                                break;
                            case nameof(DataValue.SourcePicoseconds):
                                isRaw = false;
                                dataValue.SourcePicoseconds =
                                    ReadUInt16(nameof(DataValue.SourcePicoseconds));
                                break;
                            case nameof(DataValue.ServerTimestamp):
                                isRaw = false;
                                dataValue.ServerTimestamp =
                                    ReadDateTime(nameof(DataValue.ServerTimestamp));
                                break;
                            case nameof(DataValue.ServerPicoseconds):
                                isRaw = false;
                                dataValue.ServerPicoseconds =
                                    ReadUInt16(nameof(DataValue.ServerPicoseconds));
                                break;
                            case nameof(DataValue.StatusCode):
                                isRaw = false;
                                dataValue.StatusCode =
                                    ReadStatusCode(nameof(DataValue.StatusCode));
                                break;
                            default:
                                throw new ServiceResultException(StatusCodes.BadDecodingError,
                                    $"Unknown field {dvf.Name} in dataset field.\n{Schema.ToJson()}");
                        }
                    }
                    if (isRaw)
                    {
                        dataValue.StatusCode = (StatusCode)uint.MaxValue;
                    }
                    return dataValue;
                }

                // Read value as variant
                _schema.Push(ArraySchema.Create(fieldRecord));
                var value = ReadVariant(null);
                _schema.Pop();
                if (value == Variant.Null)
                {
                    return null;
                }
                return new DataValue(value, (StatusCode)uint.MaxValue);
            }
        }

        /// <inheritdoc/>
        public override IEncodeable ReadEncodeable(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            if (Activator.CreateInstance(systemType) is not IEncodeable encodeable)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Cannot decode type '{systemType.FullName}'.");
            }

            var fullName = encodeable.TypeId.GetFullName(systemType.Name, Context);
            return ValidatedRead(fieldName, fullName ?? systemType.Name,
                f => base.ReadEncodeable(f, systemType, encodeableTypeId), fullName != null);
        }

        /// <inheritdoc/>
        public override ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExtensionObject,
                base.ReadExtensionObject);
        }

        /// <inheritdoc/>
        protected override ExtensionObject ReadEncodedDataType(string? fieldName)
        {
            return ValidatedRead(fieldName, SchemaUtils.NamespaceZeroName + ".EncodedDataType",
                base.ReadEncodedDataType);
        }

        /// <inheritdoc/>
        public override Enum ReadEnumerated(string? fieldName, Type enumType)
        {
            return ValidatedRead(fieldName, BuiltInType.Enumeration,
                f => base.ReadEnumerated(f, enumType));
        }

        /// <inheritdoc/>
        public override BooleanCollection ReadBooleanArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Boolean,
               base.ReadBooleanArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override SByteCollection ReadSByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.SByte,
               base.ReadSByteArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override ByteCollection ReadByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Byte,
               base.ReadByteArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override Int16Collection ReadInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int16,
               base.ReadInt16Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override UInt16Collection ReadUInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt16,
              base.ReadUInt16Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override Int32Collection ReadInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int32,
               base.ReadInt32Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override UInt32Collection ReadUInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt32,
               base.ReadUInt32Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override Int64Collection ReadInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int64,
               base.ReadInt64Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override UInt64Collection ReadUInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt64,
               base.ReadUInt64Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override FloatCollection ReadFloatArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Float,
               base.ReadFloatArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override DoubleCollection ReadDoubleArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Double,
               base.ReadDoubleArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override StringCollection ReadStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.String,
               base.ReadStringArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override DateTimeCollection ReadDateTimeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DateTime,
               base.ReadDateTimeArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override UuidCollection ReadGuidArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Guid,
               base.ReadGuidArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override ByteStringCollection ReadByteStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ByteString,
               base.ReadByteStringArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override XmlElementCollection ReadXmlElementArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.XmlElement,
               base.ReadXmlElementArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override NodeIdCollection ReadNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
               base.ReadNodeIdArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override ExpandedNodeIdCollection ReadExpandedNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExpandedNodeId,
               base.ReadExpandedNodeIdArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override StatusCodeCollection ReadStatusCodeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.StatusCode,
               base.ReadStatusCodeArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override DiagnosticInfoCollection ReadDiagnosticInfoArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DiagnosticInfo,
               base.ReadDiagnosticInfoArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override QualifiedNameCollection ReadQualifiedNameArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.QualifiedName,
               base.ReadQualifiedNameArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override LocalizedTextCollection ReadLocalizedTextArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.LocalizedText,
               base.ReadLocalizedTextArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override VariantCollection ReadVariantArray(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var expectedType = _builtIns.GetSchemaForBuiltInType(BuiltInType.Variant,
                SchemaRank.Collection);
            try
            {
                // Write as variant collection
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    return base.ReadVariantArray(fieldName);
                }

                // Write as built in type
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                {
                    //
                    // Rank should be collection, and all values to write should be
                    // scalar and of the built in type
                    //
                    if (rank != SchemaRank.Collection)
                    {
                        throw new ServiceResultException(StatusCodes.BadDecodingError,
                            $"Failed to decode. Wrong schema {currentSchema.ToJson()} " +
                            $"of field {fieldName ?? "unnamed"} to write variants.\n" +
                            $"{Schema.ToJson()}");
                    }
                    _schema.Push(ArraySchema.Create(currentSchema));
                    var result = ReadArray(null, builtInType, null, null);
                    _schema.Pop();
                    if (result == null || result.Length == 0)
                    {
                        return Array.Empty<Variant>();
                    }
                    return result.Cast<object?>()
                        .Select(o => new Variant(o))
                        .ToArray();
                }

                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to decode. Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant collection " +
                    $"nor built in type collection schema. .\n{Schema.ToJson()}");
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <inheritdoc/>
        public override DataValueCollection ReadDataValueArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DataValue,
                base.ReadDataValueArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override ExtensionObjectCollection ReadExtensionObjectArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExtensionObject,
                base.ReadExtensionObjectArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override Array? ReadEncodeableArray(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId)
        {
            return base.ReadEncodeableArray(fieldName, systemType, encodeableTypeId);
        }

        /// <inheritdoc/>
        public override Array? ReadEnumeratedArray(string? fieldName, Type enumType)
        {
            return ValidatedRead(fieldName, BuiltInType.Enumeration,
                f => base.ReadEnumeratedArray(f, enumType), SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override T? ReadNull<T>(string? fieldName) where T : default
        {
            return ValidatedRead(fieldName, BuiltInType.Null,
                base.ReadNull<T>, SchemaRank.Scalar);
        }

        /// <inheritdoc/>
        protected override T? ReadNullable<T>(string? fieldName, Func<string?, T> reader)
            where T : default
        {
            return ReadUnion(fieldName, id =>
            {
                // Check the schema is a nullable union schema
                if (Current is not UnionSchema u ||
                    u.Count != 2 || u.Schemas[0].Tag != Schema.Type.Null)
                {
                    throw new ServiceResultException(StatusCodes.BadDecodingError,
                        $"Failed to decode. Union schema {Current.ToJson()} of nullable " +
                        $"field {fieldName ?? "unnamed"} does not match.\n{Schema.ToJson()}");
                }
                switch (id)
                {
                    case 0:
                        return ReadNull<T>(null);
                    case 1:
                        return reader(null);
                    default:
                        throw new ServiceResultException(StatusCodes.BadDecodingError,
                            $"Unexpected union discriminator {id}.");
                }
            });
        }

        /// <inheritdoc/>
        protected override Array ReadArray(string? fieldName,
            Func<object> reader, Type type)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, reader, type));
        }

        /// <inheritdoc/>
        public override Array? ReadArray(string? fieldName, int valueRank,
            BuiltInType builtInType, Type? systemType, ExpandedNodeId? encodeableTypeId)
        {
            return ValidatedReadArray(
                () => base.ReadArray(fieldName, valueRank, builtInType,
                    systemType, encodeableTypeId));
        }

        /// <inheritdoc/>
        public override T[] ReadArray<T>(string? fieldName, Func<T> reader)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, () => reader()));
        }

        /// <inheritdoc/>
        public override T ReadObject<T>(string? fieldName, Func<object?, T> reader)
        {
            var schema = GetFieldSchema(fieldName);
            try
            {
                return reader(schema);
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <inheritdoc/>
        public void Push(Schema schema)
        {
            _schema.Push(schema);
        }

        /// <inheritdoc/>
        protected override DiagnosticInfo ReadDiagnosticInfo(string? fieldName, int depth)
        {
            return ValidatedRead(fieldName, BuiltInType.DiagnosticInfo,
                f => base.ReadDiagnosticInfo(f, depth));
        }

        /// <inheritdoc/>
        public override T ReadUnion<T>(string? fieldName,
            Func<int, T> reader)
        {
            // Get the union schema
            var schema = GetFieldSchema(fieldName);
            if (schema is not UnionSchema)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Union field {fieldName ?? "unnamed"} must be a union " +
                    $"schema but is {schema.ToJson()} schema.\n{Schema.ToJson()}");
            }
            return base.ReadUnion(fieldName, reader);
        }

        /// <inheritdoc/>
        public override int StartUnion()
        {
            var index = base.StartUnion();
            _schema.ExpectUnionItem = u =>
            {
                if (index < u.Schemas.Count && index >= 0)
                {
                    return u.Schemas[index];
                }
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Union index {index} not found in union {u.ToJson()}\n{Schema.ToJson()}");
            };
            return index;
        }

        /// <inheritdoc/>
        public override void EndUnion()
        {
            var unionSchema = _schema.Pop();
            if (unionSchema is not UnionSchema)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Expected union schema but got {unionSchema.ToJson()} after " +
                    $"completing union.\n{Schema.ToJson()}");
            }
            base.EndUnion();
        }

        /// <inheritdoc/>
        protected override IEncodeable ReadEncodeableInExtensionObject(int unionId)
        {
            var schema = _schema.Current; // Selected through union id

            // Get the type id directly from the schema and load the system type
            var typeId = schema.GetDataTypeId(Context);
            if (NodeId.IsNull(typeId))
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Schema {schema.ToJson()} does not reference a valid type " +
                    $"id to look up system type.\n{Schema.ToJson()}");
            }

            var systemType = Context.Factory.GetSystemType(typeId);
            if (systemType == null)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"A system type for schema {schema} could not befound using " +
                    $"the typeid {typeId}.");
            }
            return ReadEncodeable(null, systemType, typeId);
        }

        /// <summary>
        /// Perform the read of the built in type after validating the
        /// operation against the schema of the field if there is a field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="value"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private T ValidatedRead<T>(string? fieldName, BuiltInType builtInType,
            Func<string?, T> value, SchemaRank valueRank = SchemaRank.Scalar)
        {
            // Get expected schema
            var expectedType = _builtIns.GetSchemaForBuiltInType(builtInType,
                valueRank);
            var expectedName = expectedType.Fullname;
            return ValidatedRead(fieldName, expectedName, value);
        }

        /// <summary>
        /// Validates reading a value against the schema
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="expectedSchemaName"></param>
        /// <param name="value"></param>
        /// <param name="isFullName"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private T ValidatedRead<T>(string? fieldName, string expectedSchemaName,
            Func<string?, T> value, bool isFullName = true)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Should be the same
            var curName = isFullName ? currentSchema.Fullname : currentSchema.Name;
            if (curName != expectedSchemaName)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to decode. Schema {currentSchema.Fullname} is not as " +
                    $"expected {expectedSchemaName}.\n{Schema.ToJson()}");
            }

            // Read type per schema
            var result = value(fieldName);

            // Pop the type from the stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
            }

            return result;
        }

        /// <summary>
        /// Validated array reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private T ValidatedReadArray<T>(Func<T> reader)
        {
            var currentSchema = GetFieldSchema(null);
            if (currentSchema is not ArraySchema arr)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Reading array field but schema {currentSchema.ToJson()} is not " +
                    $"array schema.\n{Schema.ToJson()}");
            }

            var result = reader();

            // Pop array from stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
            }
            return result;
        }

        /// <summary>
        /// Get next schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private Schema GetFieldSchema(string? fieldName)
        {
            _schema.ExpectedFieldName = fieldName;
            var current = _schema.Current;
            if (!_schema.TryMoveNext())
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    $"Failed to decode. No schema for field {fieldName ?? "unnamed"} " +
                    $"found in {current.ToJson()}.\n{Schema.ToJson()}");
            }
            return _schema.Current;
        }

        private readonly AvroBuiltInAvroSchemas _builtIns = new();
        private readonly AvroSchemaTraverser _schema;
    }

    /// <summary>
    /// Schemaless avro decoder
    /// </summary>
    internal sealed class SchemalessAvroDecoder : BaseAvroDecoder
    {
        /// <inheritdoc/>
        public SchemalessAvroDecoder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = false)
            : base(stream, context, leaveOpen)
        {
        }
    }
}
