// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Azure.IIoT.OpcUa.Encoders.Avro;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Utils;
    using global::Avro;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Decodes objects from underlying decoder using a provided
    /// Avro schema. Validation errors throw.
    /// </summary>
    public sealed class AvroDeserializer : Opc.Ua.IDecoder
    {
        /// <inheritdoc/>
        public EncodingType EncodingType => _decoder.EncodingType;

        /// <inheritdoc/>
        public IServiceMessageContext Context => _decoder.Context;

        /// <inheritdoc/>
        public Schema Schema { get; }

        /// <summary>
        /// Create avro schema decoder
        /// </summary>
        /// <param name="decoder"></param>
        /// <param name="schema"></param>
        internal AvroDeserializer(AvroDecoder decoder, Schema schema)
        {
            Schema = schema;
            _schema = new AvroSchemaStack(schema);
            _decoder = decoder;

            // Point encodeable decoder to us
            _decoder.EncodeableDecoder = this;
        }

        /// <summary>
        /// Creates a decoder that decodes the data from the
        /// passed in stream.
        /// </summary>
        /// <param name="stream">The stream to which the
        /// encoder writes.</param>
        /// <param name="schema"></param>
        /// <param name="context">The message context to
        /// use for the encoding.</param>
        public AvroDeserializer(Stream stream, Schema schema,
            IServiceMessageContext context) :
            this(new AvroDecoder(stream, context), schema)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _decoder.Dispose();
        }

        /// <inheritdoc/>
        public void Close()
        {
            _decoder.Close();
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri)
        {
            _decoder.PushNamespace(namespaceUri);
        }

        /// <inheritdoc/>
        public void PopNamespace()
        {
            _decoder.PopNamespace();
        }

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable? namespaceUris,
            StringTable? serverUris)
        {
            _decoder.SetMappingTables(namespaceUris, serverUris);
        }

        /// <inheritdoc/>
        public bool ReadBoolean(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Boolean,
                _decoder.ReadBoolean);
        }

        /// <inheritdoc/>
        public sbyte ReadSByte(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.SByte,
                _decoder.ReadSByte);
        }

        /// <inheritdoc/>
        public byte ReadByte(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Byte,
                _decoder.ReadByte);
        }

        /// <inheritdoc/>
        public short ReadInt16(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int16,
                _decoder.ReadInt16);
        }

        /// <inheritdoc/>
        public ushort ReadUInt16(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt16,
                _decoder.ReadUInt16);
        }

        /// <inheritdoc/>
        public int ReadInt32(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int32,
                _decoder.ReadInt32);
        }

        /// <inheritdoc/>
        public uint ReadUInt32(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt32,
                _decoder.ReadUInt32);
        }

        /// <inheritdoc/>
        public long ReadInt64(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int64,
                _decoder.ReadUInt32);
        }

        /// <inheritdoc/>
        public float ReadFloat(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Float,
                _decoder.ReadFloat);
        }

        /// <inheritdoc/>
        public double ReadDouble(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Double,
                _decoder.ReadDouble);
        }

        /// <inheritdoc/>
        public Uuid ReadGuid(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Guid,
                _decoder.ReadGuid);
        }

        /// <inheritdoc/>
        public string? ReadString(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.String,
                _decoder.ReadString);
        }

        /// <inheritdoc/>
        public ulong ReadUInt64(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt64,
                _decoder.ReadUInt64);
        }

        /// <inheritdoc/>
        public DateTime ReadDateTime(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DateTime,
                _decoder.ReadDateTime);
        }

        /// <inheritdoc/>
        public byte[]? ReadByteString(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ByteString,
                _decoder.ReadByteString);
        }

        /// <inheritdoc/>
        public XmlElement? ReadXmlElement(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.XmlElement,
                _decoder.ReadXmlElement);
        }

        /// <inheritdoc/>
        public StatusCode ReadStatusCode(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.StatusCode,
                _decoder.ReadStatusCode);
        }

        /// <inheritdoc/>
        public NodeId ReadNodeId(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
                _decoder.ReadNodeId);
        }

        /// <inheritdoc/>
        public ExpandedNodeId ReadExpandedNodeId(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
                _decoder.ReadExpandedNodeId);
        }

        /// <inheritdoc/>
        public DiagnosticInfo? ReadDiagnosticInfo(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DiagnosticInfo,
               _decoder.ReadDiagnosticInfo);
        }

        /// <inheritdoc/>
        public QualifiedName? ReadQualifiedName(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.QualifiedName,
               _decoder.ReadQualifiedName);
        }

        /// <inheritdoc/>
        public LocalizedText? ReadLocalizedText(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.LocalizedText,
               _decoder.ReadLocalizedText);
        }

        /// <inheritdoc/>
        public DataValue? ReadDataValue(string? fieldName)
        {
            // TODO: we will have data value schemas where the 
            // Value is not a variant schema. Those are named
            // something like <TypeName>DataValue
            // Get current field schema

            var currentSchema = GetFieldSchema(fieldName);

            // The schema should be a data value schema

            return _decoder.ReadDataValue(fieldName);
        }

        /// <inheritdoc/>
        public Variant ReadVariant(string? fieldName)
        {
            var schema = GetFieldSchema(fieldName);

            if (schema.IsBuiltInType(out var builtInType) &&
                builtInType == BuiltInType.Variant)
            {
                // If it is a original variant - we pass through or it is a built in type
                return _decoder.ReadVariant(fieldName);
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

            return _decoder.ReadVariantValue(builtInType, isArray, isMatrix);
        }

        /// <inheritdoc/>
        public DataSet ReadDataSet(string? fieldName)
        {
            var schema = GetFieldSchema(fieldName);

            // Run through the fields and read either using variant or data values

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
        public IEncodeable ReadEncodeable(string? fieldName, System.Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema r)
            {
                // Should be a record
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Encodeable schema {0} should be a record.", schema);
            }
            return _decoder.ReadEncodeable(fieldName, systemType, encodeableTypeId);
        }

        /// <inheritdoc/>
        public ExtensionObject? ReadExtensionObject(string? fieldName)
        {
            //
            // Extension objects can be either fully encoded extension objects or
            // can be just the encodeable that is decoded from the schema that is
            // the current field schema.
            //
            var schema = GetFieldSchema(fieldName);

            // TODO: This could be a union of many structure types, get which
            // using the union index and so on!!

            if (schema.IsBuiltInType(out var builtInType))
            {
                if (builtInType == BuiltInType.ExtensionObject)
                {
                    // The schema is a extension object schema so we decode as such
                    return _decoder.ReadExtensionObject(fieldName);
                }

                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Schema {0} is a built in type but not an extension object.",
                    schema);

                // Decode as variant?
            }

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
            return new ExtensionObject(typeId,
                ReadEncodeable(fieldName, systemType, typeId));
        }

        /// <inheritdoc/>
        public Enum ReadEnumerated(string? fieldName, System.Type enumType)
        {
            // TODO:
            var schema = GetFieldSchema(fieldName);

            return _decoder.ReadEnumerated(fieldName, enumType);
        }

        /// <inheritdoc/>
        public BooleanCollection? ReadBooleanArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Boolean,
               _decoder.ReadBooleanArray, true);
        }

        /// <inheritdoc/>
        public SByteCollection? ReadSByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.SByte,
               _decoder.ReadSByteArray, true);
        }

        /// <inheritdoc/>
        public ByteCollection? ReadByteArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Byte,
               _decoder.ReadByteArray, true);
        }

        /// <inheritdoc/>
        public Int16Collection? ReadInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int16,
               _decoder.ReadInt16Array, true);
        }

        /// <inheritdoc/>
        public UInt16Collection? ReadUInt16Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt16,
              _decoder.ReadUInt16Array, true);
        }

        /// <inheritdoc/>
        public Int32Collection? ReadInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int32,
               _decoder.ReadInt32Array, true);
        }

        /// <inheritdoc/>
        public UInt32Collection? ReadUInt32Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt32,
               _decoder.ReadUInt32Array, true);
        }

        /// <inheritdoc/>
        public Int64Collection? ReadInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Int64,
               _decoder.ReadInt64Array, true);
        }

        /// <inheritdoc/>
        public UInt64Collection? ReadUInt64Array(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.UInt64,
               _decoder.ReadUInt64Array, true);
        }

        /// <inheritdoc/>
        public FloatCollection? ReadFloatArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Float,
               _decoder.ReadFloatArray, true);
        }

        /// <inheritdoc/>
        public DoubleCollection? ReadDoubleArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Double,
               _decoder.ReadDoubleArray, true);
        }

        /// <inheritdoc/>
        public StringCollection? ReadStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.String,
               _decoder.ReadStringArray, true);
        }

        /// <inheritdoc/>
        public DateTimeCollection? ReadDateTimeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DateTime,
               _decoder.ReadDateTimeArray, true);
        }

        /// <inheritdoc/>
        public UuidCollection? ReadGuidArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Guid,
               _decoder.ReadGuidArray, true);
        }

        /// <inheritdoc/>
        public ByteStringCollection? ReadByteStringArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ByteString,
               _decoder.ReadByteStringArray, true);
        }

        /// <inheritdoc/>
        public XmlElementCollection? ReadXmlElementArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.XmlElement,
               _decoder.ReadXmlElementArray, true);
        }

        /// <inheritdoc/>
        public NodeIdCollection? ReadNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.NodeId,
               _decoder.ReadNodeIdArray, true);
        }

        /// <inheritdoc/>
        public ExpandedNodeIdCollection? ReadExpandedNodeIdArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.ExpandedNodeId,
               _decoder.ReadExpandedNodeIdArray, true);
        }

        /// <inheritdoc/>
        public StatusCodeCollection? ReadStatusCodeArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.StatusCode,
               _decoder.ReadStatusCodeArray, true);
        }

        /// <inheritdoc/>
        public DiagnosticInfoCollection? ReadDiagnosticInfoArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.DiagnosticInfo,
               _decoder.ReadDiagnosticInfoArray, true);
        }

        /// <inheritdoc/>
        public QualifiedNameCollection? ReadQualifiedNameArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Boolean,
               _decoder.ReadQualifiedNameArray, true);
        }

        /// <inheritdoc/>
        public LocalizedTextCollection? ReadLocalizedTextArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.LocalizedText,
               _decoder.ReadLocalizedTextArray, true);
        }

        /// <inheritdoc/>
        public VariantCollection? ReadVariantArray(string? fieldName)
        {
            return ValidatedRead(fieldName, BuiltInType.Variant,
               _decoder.ReadVariantArray, true);
        }

        /// <inheritdoc/>
        public DataValueCollection? ReadDataValueArray(string? fieldName)
        {
            // TODO
            return ValidatedRead(fieldName, BuiltInType.DataValue,
               _decoder.ReadDataValueArray, true);
        }

        /// <inheritdoc/>
        public ExtensionObjectCollection? ReadExtensionObjectArray(string? fieldName)
        {
            // TODO
            return ValidatedRead(fieldName, BuiltInType.ExtensionObject,
               _decoder.ReadExtensionObjectArray, true);
        }

        /// <inheritdoc/>
        public Array? ReadEncodeableArray(string? fieldName, System.Type systemType,
            ExpandedNodeId? encodeableTypeId = null)
        {
            // TODO
            var schema = GetFieldSchema(fieldName);
            return _decoder.ReadEncodeableArray(fieldName, systemType,
                encodeableTypeId);
        }

        /// <inheritdoc/>
        public Array? ReadEnumeratedArray(string? fieldName, System.Type enumType)
        {
            // TODO
            var schema = GetFieldSchema(fieldName);
            return _decoder.ReadEnumeratedArray(fieldName, enumType);
        }

        /// <inheritdoc/>
        public Array? ReadArray(string? fieldName, int valueRank, BuiltInType builtInType,
            Type? systemType = null, ExpandedNodeId? encodeableTypeId = null)
        {
            var schema = GetFieldSchema(fieldName);
            // TODO
            return _decoder.ReadArray(fieldName, valueRank, builtInType,
                systemType, encodeableTypeId);
        }

        /// <inheritdoc/>
        public T[] ReadCollection<T>(string? fieldName, Func<T> reader)
        {
            var schema = GetFieldSchema(fieldName);
            // TODO
            return _decoder.ReadCollection<T>(reader);
        }

        /// <summary>
        /// Perform the read of the built in type after validating the
        /// operation against the schema of the field if there is a field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="value"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private T ValidatedRead<T>(string? fieldName, BuiltInType builtInType,
            Func<string?, T> value, bool array = false)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Get expected schema
            var expectedType = _builtIns.GetSchemaForBuiltInType(builtInType,
                array);

            // Should be the same
            if (!currentSchema.Equals(expectedType))
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Failed to decode. Schema {0} is not as expected {1}",
                    currentSchema, expectedType);
            }
            return value(fieldName);
        }

        /// <summary>
        /// Get next schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private Schema GetFieldSchema(string? fieldName)
        {
            if (!_schema.TryPop(fieldName, out var schema))
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError,
                    "Failed to decode. No schema for field {0]", fieldName);
            }
            return schema;
        }

        /// <summary>
        /// Allows a encoder or decoder to follow the schema
        /// </summary>
        /// <returns></returns>
        private sealed class AvroSchemaStack
        {
            /// <summary>
            /// Create traversal
            /// </summary>
            /// <param name="schema"></param>
            public AvroSchemaStack(Schema schema)
            {
                var list = new List<(string?, Schema)>();
                Flatten((null, schema), list);
                _schemas = new Queue<(string?, Schema)>(list);
            }

            /// <summary>
            /// Split
            /// </summary>
            /// <param name="original"></param>
            private AvroSchemaStack(AvroSchemaStack original)
            {
                _schemas = new Queue<(string?, Schema)>(
                    original._schemas.ToList());
            }

            /// <summary>
            /// Fork traversal to create a safe path
            /// </summary>
            /// <returns></returns>
            public AvroSchemaStack Fork()
            {
                return new AvroSchemaStack(this);
            }

            /// <summary>
            /// Flatten depth first like we will travers
            /// </summary>
            /// <param name="schema"></param>
            /// <param name="flat"></param>
            private static void Flatten((string?, Schema) schema, List<(string?, Schema)> flat)
            {
                flat.Add(schema);
                var (_, s) = schema;
                switch (s)
                {
                    case RecordSchema r:
                        foreach (var f in r.Fields)
                        {
                            Flatten((f.Name, f.Schema), flat);
                        }
                        break;
                    case MapSchema m:
                        Flatten((null, m.ValueSchema), flat);
                        break;
                }
            }

            /// <summary>
            /// Try get the next field schema
            /// </summary>
            /// <param name="fieldName"></param>
            /// <param name="schema"></param>
            /// <returns></returns>
            public bool TryPop(string? fieldName, [NotNullWhen(true)] out Schema? schema)
            {
                if (!_schemas.TryDequeue(out var s) ||
                    (fieldName != null && fieldName != s.Item1))
                {
                    schema = null;
                    return false;
                }
                schema = s.Item2;
                return true;
            }

            /// <summary>
            /// Try to peek the next schema and field value
            /// </summary>
            /// <param name="schema"></param>
            /// <param name="fieldName"></param>
            /// <returns></returns>
            public bool TryPeek(out Schema? schema, out string? fieldName)
            {
                var result = _schemas.TryPeek(out var s);
                fieldName = s.Item1;
                schema = s.Item2;
                return result;
            }

            /// <summary>
            /// Finalize
            /// </summary>
            public bool IsDone()
            {
                return _schemas.Count != 0;
            }

            private readonly Queue<(string?, Schema)> _schemas;
        }

        private readonly AvroBuiltInTypeSchemas _builtIns = new();
        private readonly AvroSchemaStack _schema;
        private readonly AvroDecoder _decoder;
    }
}
