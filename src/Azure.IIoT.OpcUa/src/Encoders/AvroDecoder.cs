// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Avro;
    using Opc.Ua;
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Collections.Generic;

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
                throw new DecodingException(
                    $"Schema {currentSchema.Fullname} is not " +
                    $"as expected {currentSchema.ToJson()}.", Schema.ToJson());
            }

            // Read type per schema
            var result = base.ReadDataValue(fieldName);
            ValidatedPop(currentSchema);
            return result;
        }

        /// <inheritdoc/>
        public override Variant ReadVariant(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadVariant(fieldName, currentSchema);
            ValidatedPop(currentSchema);
            return result;

            Variant ReadVariant(string? fieldName, Schema currentSchema)
            {
                var expectedType = _builtIns.GetSchemaForBuiltInType(
                    BuiltInType.Variant, SchemaRank.Scalar);
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    // Read as variant
                    return base.ReadVariant(fieldName);
                }

                // Alternatively the schema could be nullable
                if (currentSchema is UnionSchema u)
                {
                    // Read as nullable
                    return ReadWithSchema(u, () => ReadNullable(fieldName,
                        _ => ReadVariantValueWithSchema(u.Schemas[1])));
                }

                return ReadVariantValueWithSchema(currentSchema);

                Variant ReadVariantValueWithSchema(Schema currentSchema)
                {
                    // Read as built in type
                    if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                    {
                        return ReadWithSchema(currentSchema,
                            () => ReadVariantValue(builtInType, rank));
                    }

                    // Read as encodeable
                    var typeId = currentSchema.GetDataTypeId(Context);
                    var systemType = Context.Factory.GetSystemType(typeId);
                    if (systemType != null)
                    {
                        return new Variant(ReadWithSchema(currentSchema,
                            () => ReadEncodeable(null, systemType, typeId)));
                    }

                    throw new DecodingException(
                        $"Variant schema {currentSchema.ToJson()} of " +
                        $"field {fieldName ?? "unnamed"} is neither variant nor built " +
                        "in type schema.", Schema.ToJson());
                }
            }
        }

        /// <inheritdoc/>
        public override DataSet ReadDataSet(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadDataSet(currentSchema);
            ValidatedPop(currentSchema);
            return result;

            DataSet ReadDataSet(Schema currentSchema)
            {
                if (currentSchema is not RecordSchema r)
                {
                    throw new DecodingException(
                        $"Invalid schema {currentSchema.ToJson()}. " +
                        "Data sets must be records or maps.", Schema.ToJson());
                }
                var dataSet = new List<(string, DataValue?)>();
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
                    dataSet.Add((SchemaUtils.Unescape(field.Name), dataValue));
                }
                var dataSetFieldContentFlags = isRaw ? DataSetFieldContentFlags.RawData : 0;
                return new DataSet(dataSet, dataSetFieldContentFlags);
            }
        }

        /// <summary>
        /// Read data set field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private DataValue? ReadDataSetField(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadDataSetField(currentSchema);
            ValidatedPop(currentSchema);
            return result;

            DataValue? ReadDataSetFieldValue()
            {
                var isRaw = true;
                if (Current is RecordSchema fieldRecord &&
                    fieldRecord.IsDataValue())
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
                                throw new DecodingException(
                                    $"Unknown field {dvf.Name} in dataset field.", Schema.ToJson());
                        }
                    }
                    if (isRaw)
                    {
                        dataValue.StatusCode = (StatusCode)uint.MaxValue;
                    }
                    return dataValue;
                }

                // Read value as variant
                var value = ReadWithSchema(currentSchema, () => ReadVariant(null));
                if (value == Variant.Null)
                {
                    return null;
                }
                return new DataValue(value, (StatusCode)uint.MaxValue);
            }

            DataValue? ReadDataSetField(Schema currentSchema)
            {
                if (currentSchema is UnionSchema u)
                {
                    return ReadWithSchema(u, () => ReadNullable(null, _ =>
                    {
                        _schema.Push(((UnionSchema)Current).Schemas[1]);
                        var v = ReadDataSetFieldValue();
                        _schema.Pop();
                        return v!;
                    }));
                }
                return ReadDataSetFieldValue();
            }
        }

        /// <inheritdoc/>
        public override IEncodeable ReadEncodeable(string? fieldName, Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            if (Activator.CreateInstance(systemType) is not IEncodeable encodeable)
            {
                throw new DecodingException(
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
        public override int ReadEnumerated(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadEnumerated(fieldName, currentSchema);
            ValidatedPop(currentSchema);
            return result;

            int ReadEnumerated(string? fieldName, Schema currentSchema)
            {
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank) &&
                    rank == SchemaRank.Scalar &&
                    (builtInType == BuiltInType.Int32 || builtInType == BuiltInType.Enumeration))
                {
                    return ReadWithSchema(currentSchema,
                        () => base.ReadEnumerated(fieldName));
                }

                throw new EncodingException(
                    $"Invalid schema {currentSchema.ToJson()}. " +
                    "Enumerated values must be enums.", Schema.ToJson());
            }
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
            return ValidatedRead(fieldName, BuiltInType.ByteString,
               base.ReadByteArray, SchemaRank.Scalar);
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
            var result = ReadVariantArray(fieldName, currentSchema);
            ValidatedPop(currentSchema);
            return result;

            VariantCollection ReadVariantArray(string? fieldName, Schema currentSchema)
            {
                var expectedType = _builtIns.GetSchemaForBuiltInType(BuiltInType.Variant,
                    SchemaRank.Collection);
                // Write as variant collection
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    return base.ReadVariantArray(fieldName);
                }

                // Write as built in type
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                {
                    // When written in concise mode we get an array of bytes as byte string
                    if (builtInType == BuiltInType.ByteString && rank == SchemaRank.Scalar)
                    {
                        var result = ReadWithSchema(currentSchema,
                            () => ReadScalar(null, builtInType));
                        return (result.Value as byte[])?
                            .Select(o => new Variant(o))
                            .ToArray();
                    }
                    //
                    // Otherwise rank should be collection, and all values to write should be
                    // scalar and of the built in type
                    //
                    if (rank == SchemaRank.Collection)
                    {
                        var result = ReadWithSchema(currentSchema,
                            () => ReadArray(null, builtInType, null, null));
                        if (result == null || result.Length == 0)
                        {
                            return Array.Empty<Variant>();
                        }
                        return result.Cast<object?>()
                            .Select(o => new Variant(o))
                            .ToArray();
                    }

                    throw new DecodingException(
                        $"Wrong schema {currentSchema.ToJson()} " +
                        $"of field {fieldName ?? "unnamed"} to write variants.",
                        Schema.ToJson());
                }

                throw new DecodingException(
                    $"Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant collection " +
                    "nor built in type collection schema.", Schema.ToJson());
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
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadEnumeratedArray(fieldName, currentSchema);
            ValidatedPop(currentSchema);
            return result;

            Array? ReadEnumeratedArray(string? fieldName, Schema currentSchema)
            {
                if (currentSchema is ArraySchema a && a.ItemSchema is EnumSchema e)
                {
                    return ReadWithSchema(currentSchema,
                        () => base.ReadEnumeratedArray(fieldName, enumType));
                }
                else if (currentSchema.IsBuiltInType(out var builtInType, out var rank) &&
                    rank == SchemaRank.Collection &&
                    (builtInType == BuiltInType.Int32 || builtInType == BuiltInType.Enumeration))
                {
                    return base.ReadEnumeratedArray(fieldName, enumType);
                }
                else
                {
                    throw new EncodingException($"Invalid schema {currentSchema.ToJson()}. " +
                        "Enumerated values must be arrays of enums.", Schema.ToJson());
                }
            }
        }

        /// <inheritdoc/>
        public override int[] ReadEnumeratedArray(string? fieldName)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = ReadEnumeratedArray(fieldName, currentSchema);
            ValidatedPop(currentSchema);
            return result;

            int[] ReadEnumeratedArray(string? fieldName, Schema currentSchema)
            {
                if (currentSchema is ArraySchema a && a.ItemSchema is EnumSchema e)
                {
                    return ReadWithSchema(currentSchema,
                        () => base.ReadEnumeratedArray(fieldName));
                }
                else if (currentSchema.IsBuiltInType(out var builtInType, out var rank) &&
                    rank == SchemaRank.Collection &&
                    (builtInType == BuiltInType.Int32 || builtInType == BuiltInType.Enumeration))
                {
                    return base.ReadEnumeratedArray(fieldName);
                }
                else
                {
                    throw new EncodingException(
                        $"Invalid schema {currentSchema.ToJson()}. " +
                        "Enumerated values must be arrays of enums.", Schema.ToJson());
                }
            }
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
                    throw new DecodingException(
                        $"Union schema {Current.ToJson()} of nullable " +
                        $"field {fieldName ?? "unnamed"} does not match.", Schema.ToJson());
                }
                switch (id)
                {
                    case 0:
                        return ReadNull<T>(null);
                    case 1:
                        return reader(null);
                    default:
                        throw new DecodingException(
                            $"Unexpected union discriminator {id}.");
                }
            });
        }

        /// <inheritdoc/>
        public override Array? ReadArray(string? fieldName, int valueRank, BuiltInType builtInType,
            Type? systemType = null, ExpandedNodeId? encodeableTypeId = null)
        {
            var currentSchema = GetFieldSchema(null);
            if (!currentSchema.IsBuiltInType(out var expectedType, out var rank))
            {
                throw new DecodingException(
                    $"Schema {currentSchema.ToJson()} is not an array schema.",
                    Schema.ToJson());
            }
            if (rank != SchemaUtils.GetRank(valueRank) || builtInType != expectedType)
            {
                throw new DecodingException(
                    $"Schema {currentSchema.ToJson()} does not match expected rank and type.",
                    Schema.ToJson());
            }
            var result = ReadWithSchema(currentSchema,
                () => base.ReadArray(fieldName, valueRank, builtInType, systemType, encodeableTypeId));
            ValidatedPop(currentSchema);
            return result;
        }

        /// <inheritdoc/>
        protected override Array ReadArray(string? fieldName,
            Func<object> reader, Type type)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, reader, type));
        }

        /// <inheritdoc/>
        public override T[] ReadArray<T>(string? fieldName, Func<T> reader)
        {
            return ValidatedReadArray(() => base.ReadArray(fieldName, () => reader()));
        }

        /// <inheritdoc/>
        public override T ReadObject<T>(string? fieldName, Func<object?, T> reader)
        {
            var currentSchema = GetFieldSchema(fieldName);
            var result = reader(currentSchema);
            ValidatedPop(currentSchema);
            return result;
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
            var currentSchema = GetFieldSchema(fieldName);
            if (currentSchema is not UnionSchema)
            {
                throw new DecodingException(
                    $"Union field {fieldName ?? "unnamed"} must be a union " +
                    $"schema but is {currentSchema.ToJson()} schema.", Schema.ToJson());
            }
            return base.ReadUnion(fieldName, reader);
        }

        /// <inheritdoc/>
        protected override int StartUnion()
        {
            var index = base.StartUnion();
            _schema.ExpectUnionItem = u =>
            {
                if (index < u.Schemas.Count && index >= 0)
                {
                    return u.Schemas[index];
                }
                throw new DecodingException(
                    $"Union index {index} not found in union {u.ToJson()}.",
                    Schema.ToJson());
            };
            return index;
        }

        /// <inheritdoc/>
        protected override void EndUnion()
        {
            var unionSchema = _schema.Pop();
            if (unionSchema is not UnionSchema)
            {
                throw new DecodingException(
                    $"Expected union schema but got {unionSchema.ToJson()} after " +
                    "completing union.", Schema.ToJson());
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
                throw new DecodingException(
                    $"Schema {schema.ToJson()} does not reference a valid type " +
                    "id to look up system type.", Schema.ToJson());
            }

            var systemType = Context.Factory.GetSystemType(typeId);
            if (systemType == null)
            {
                throw new DecodingException(
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
        /// <exception cref="DecodingException"></exception>
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
        /// <exception cref="DecodingException"></exception>
        private T ValidatedRead<T>(string? fieldName, string expectedSchemaName,
            Func<string?, T> value, bool isFullName = true)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Should be the same
            var curName = isFullName ? currentSchema.Fullname : currentSchema.Name;
            if (curName != expectedSchemaName)
            {
                throw new DecodingException(
                    $"Schema {currentSchema.Fullname} is not as " +
                    $"expected {expectedSchemaName}.", Schema.ToJson());
            }

            // Read type per schema
            var result = value(fieldName);
            ValidatedPop(currentSchema);
            return result;
        }

        /// <summary>
        /// Validated array reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private T ValidatedReadArray<T>(Func<T> reader)
        {
            var currentSchema = GetFieldSchema(null);
            if (currentSchema is not ArraySchema)
            {
                throw new DecodingException(
                    $"Reading array field but schema {currentSchema.ToJson()} is not " +
                    "array schema.", Schema.ToJson());
            }

            var result = reader();
            ValidatedPop(currentSchema);
            return result;
        }

        /// <summary>
        /// Use specified schema by pushing it on top for reading
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schema"></param>
        /// <param name="reader"></param>
        private T ReadWithSchema<T>(Schema schema, Func<T> reader)
        {
            var top = schema.AsArray();
            _schema.Push(top);
            var result = reader();
            ValidatedPop(top);
            return result;
        }

        /// <summary>
        /// Validate pop from stack should be expected schema
        /// </summary>
        /// <param name="expectedSchema"></param>
        /// <exception cref="DecodingException"></exception>
        private void ValidatedPop(Schema expectedSchema)
        {
            // Pop array from stack
            var completedSchema = _schema.Pop();
            if (completedSchema != expectedSchema)
            {
                throw new DecodingException(
                    $"Failed to pop schema. Expected {expectedSchema.ToJson()} " +
                    $"but got {completedSchema.ToJson()}.", Schema.ToJson());
            }
        }

        /// <summary>
        /// Get next schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="DecodingException"></exception>
        private Schema GetFieldSchema(string? fieldName)
        {
            _schema.ExpectedFieldName = fieldName;
            var current = _schema.Current;
            if (!_schema.TryMoveNext())
            {
                throw new DecodingException(
                    $"No schema for field {fieldName ?? "unnamed"} " +
                    $"found in {current.ToJson()}.", Schema.ToJson());
            }
            return _schema.Current;
        }

        private readonly AvroBuiltInSchemas _builtIns = new();
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
