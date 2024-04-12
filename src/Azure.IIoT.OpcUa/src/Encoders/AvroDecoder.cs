// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using Avro;
    using Opc.Ua;
    using System;
    using System.Diagnostics;
    using System.IO;
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
        public AvroDecoder(Stream stream, Schema schema,
            IServiceMessageContext context) :
            base(stream, context)
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
            return ValidatedRead(fieldName, BuiltInType.DataValue,
                base.ReadDataValue);
        }

        /// <inheritdoc/>
        public override Variant ReadVariant(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Variant,
                base.ReadVariant);
#if FALSE
            var schema = GetFieldSchema(fieldName);

            if (schema.IsBuiltInType(out var builtInType) &&
                builtInType == BuiltInType.Variant)
            {
                // If it is a original variant - we pass through or it is a built in type
                return base.ReadVariant(fieldName);
            }

            // Record? Matrix are arrays with array dimension field
            var isMatrix = false;
            if (schema is RecordSchema r)
            {
                if (r.Count == 2 &&
                    r.Fields[0].Schema is ArraySchema adims &&
                    adims.ItemSchema.Name == nameof(BuiltInType.UInt32))
                {
                    isMatrix = true;
                    schema = r.Fields[1].Schema;
                }
                else
                {
                    throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                        "A matrix must have 2 fields with a dimension and a body");
                }
            }

            var isArray = false;
            if (schema is ArraySchema a)
            {
                // Array
                schema = a.ItemSchema;
                isArray = true;
            }

            if (!schema.IsBuiltInType(out builtInType))
            {
                // Not a built in type
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Schema is not a built in schema");
            }

            return base.ReadVariantValue(builtInType, isArray, isMatrix);
#endif
        }

        /// <inheritdoc/>
        public override DataSet ReadDataSet(string? fieldName)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema r)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Data sets must be records or maps.");
            }
            try
            {
                var dataSet = new DataSet();
                var isRaw = true;

                // Run through the fields and read either using variant or data values
                foreach (var field in r.Fields)
                {
                    var dataValue = ReadDataSetField(field.Name, ref isRaw);
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

        /// <inheritdoc/>
        public override IEncodeable ReadEncodeable(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            if (Activator.CreateInstance(systemType) is not IEncodeable encodeable)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadDecodingError,
                    Opc.Ua.Utils.Format("Cannot decode type '{0}'.", systemType.FullName));
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
               base.ReadBooleanArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override SByteCollection ReadSByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.SByte,
               base.ReadSByteArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override ByteCollection ReadByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Byte,
               base.ReadByteArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override Int16Collection ReadInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int16,
               base.ReadInt16Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override UInt16Collection ReadUInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt16,
              base.ReadUInt16Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override Int32Collection ReadInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int32,
               base.ReadInt32Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override UInt32Collection ReadUInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt32,
               base.ReadUInt32Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override Int64Collection ReadInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int64,
               base.ReadInt64Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override UInt64Collection ReadUInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt64,
               base.ReadUInt64Array, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override FloatCollection ReadFloatArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Float,
               base.ReadFloatArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override DoubleCollection ReadDoubleArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Double,
               base.ReadDoubleArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override StringCollection ReadStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.String,
               base.ReadStringArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override DateTimeCollection ReadDateTimeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DateTime,
               base.ReadDateTimeArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override UuidCollection ReadGuidArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Guid,
               base.ReadGuidArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override ByteStringCollection ReadByteStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ByteString,
               base.ReadByteStringArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override XmlElementCollection ReadXmlElementArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.XmlElement,
               base.ReadXmlElementArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override NodeIdCollection ReadNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
               base.ReadNodeIdArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override ExpandedNodeIdCollection ReadExpandedNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExpandedNodeId,
               base.ReadExpandedNodeIdArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override StatusCodeCollection ReadStatusCodeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.StatusCode,
               base.ReadStatusCodeArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override DiagnosticInfoCollection ReadDiagnosticInfoArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DiagnosticInfo,
               base.ReadDiagnosticInfoArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override QualifiedNameCollection ReadQualifiedNameArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.QualifiedName,
               base.ReadQualifiedNameArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override LocalizedTextCollection ReadLocalizedTextArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.LocalizedText,
               base.ReadLocalizedTextArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override VariantCollection ReadVariantArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Variant,
                base.ReadVariantArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override DataValueCollection ReadDataValueArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DataValue,
                base.ReadDataValueArray, ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override ExtensionObjectCollection ReadExtensionObjectArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExtensionObject,
                base.ReadExtensionObjectArray, ValueRanks.OneDimension);
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
                f => base.ReadEnumeratedArray(f, enumType), ValueRanks.OneDimension);
        }

        /// <inheritdoc/>
        public override T? ReadNull<T>(string? fieldName) where T : default
        {
            return ValidatedRead(fieldName, BuiltInType.Null,
                base.ReadNull<T>, ValueRanks.Scalar);
        }

        /// <inheritdoc/>
        protected override Array ReadArray(string? fieldName,
            Func<object> reader, Type type)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, reader, type));
        }

        /// <inheritdoc/>
        public override Array? ReadArray(string? fieldName, int valueRank,
            BuiltInType builtInType, Type? systemType,
            ExpandedNodeId? encodeableTypeId)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, valueRank, builtInType,
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
        protected override int ReadUnionSelector()
        {
            var index = base.ReadUnionSelector();
            _schema.ExpectUnionItem = u =>
            {
                if (index < u.Schemas.Count && index >= 0)
                {
                    return u.Schemas[index];
                }
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Union index read does not match schema union");
            };
            GetFieldSchema(null);
            return index;
        }

        /// <inheritdoc/>
        protected override IEncodeable ReadEncodeableInExtensionObject(int unionId)
        {
            var schema = _schema.Current; // Selected through union id

            // Get the type id directly from the schema and load the system type
            var typeId = schema.GetDataTypeId(Context);
            if (NodeId.IsNull(typeId))
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Schema {0} does not reference a valid type id to look up system type.",
                    schema);
            }

            var systemType = Context.Factory.GetSystemType(typeId);
            if (systemType == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "A system type for schema {0} could not befound using the typeid {1}.",
                    schema, typeId);
            }
            return ReadEncodeable(null, systemType, typeId);
        }

        /// <summary>
        /// Read data set field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="isRaw"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private DataValue? ReadDataSetField(string? fieldName, ref bool isRaw)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is UnionSchema)
            {
                var unionId = ReadUnionSelector();
                if (unionId == 0)
                {
                    return ReadNull<DataValue>(fieldName);
                }
                Debug.Assert(unionId == 1);
            }

            if (Current is RecordSchema fieldRecord)
            {
                // The field is a record that should contain the data value fields
                try
                {
                    if (fieldRecord.IsBuiltInType(out var builtInType) &&
                        builtInType != BuiltInType.DataValue)
                    {
                        // Read value as variant
                        var value = base.ReadVariant(null);
                        if (value == Variant.Null)
                        {
                            return null;
                        }
                        return new DataValue(value); // TODO
                    }

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
                                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                                    $"Unknown field {dvf.Name} in dataset field.");
                        }
                    }
                    return dataValue;
                }
                finally
                {
                    _schema.Pop();
                }
            }
            else
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Data set fields must be records.");
            }
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
            Func<string?, T> value, int valueRank = ValueRanks.Scalar)
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
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Failed to decode. Schema {0} is not as expected {1}",
                    currentSchema.Fullname, expectedSchemaName);
            }

            // Read type per schema
            var result = value(fieldName);

            // Pop the type from the stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Failed to pop built in type.");
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
            var schema = GetFieldSchema(null);
            if (schema is not ArraySchema arr)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Reading array field but schema is not array schema");
            }
            try
            {
                return reader();
            }
            finally
            {
                // Pop array from stack
                schema = _schema.Pop();
                Debug.Assert(schema == arr);
            }
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
            if (!_schema.TryMoveNext())
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Failed to decode. No schema for field {0}", fieldName ?? string.Empty);
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
            IServiceMessageContext context) : base(stream, context)
        {
        }
    }
}
