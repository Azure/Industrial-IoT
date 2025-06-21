// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.Schemas;
    using Azure.IIoT.OpcUa.Encoders.Schemas.Avro;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Encodes objects via Avro schema using underlying encoder.
    /// </summary>
    public sealed class AvroEncoder : BaseAvroEncoder
    {
        /// <summary>
        /// Schema to use
        /// </summary>
        public Schema Schema { get; }

        /// <summary>
        /// Current schema
        /// </summary>
        public Schema Current => _schema.Current;

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
            base(stream, context, leaveOpen)
        {
            Schema = schema;
            _schema = new AvroSchemaTraverser(schema);
        }

        /// <inheritdoc/>
        public override void WriteBoolean(string? fieldName, bool value)
        {
            ValidatedWrite(fieldName, BuiltInType.Boolean, value,
                base.WriteBoolean);
        }

        /// <inheritdoc/>
        public override void WriteSByte(string? fieldName, sbyte value)
        {
            ValidatedWrite(fieldName, BuiltInType.SByte, value,
                base.WriteSByte);
        }

        /// <inheritdoc/>
        public override void WriteByte(string? fieldName, byte value)
        {
            ValidatedWrite(fieldName, BuiltInType.Byte, value,
                base.WriteByte);
        }

        /// <inheritdoc/>
        public override void WriteInt16(string? fieldName, short value)
        {
            ValidatedWrite(fieldName, BuiltInType.Int16, value,
                base.WriteInt16);
        }

        /// <inheritdoc/>
        public override void WriteUInt16(string? fieldName, ushort value)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt16, value,
                base.WriteUInt16);
        }

        /// <inheritdoc/>
        public override void WriteInt32(string? fieldName, int value)
        {
            ValidatedWrite(fieldName, BuiltInType.Int32, value,
                base.WriteInt32);
        }

        /// <inheritdoc/>
        public override void WriteUInt32(string? fieldName, uint value)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt32, value,
                base.WriteUInt32);
        }

        /// <inheritdoc/>
        public override void WriteInt64(string? fieldName, long value)
        {
            ValidatedWrite(fieldName, BuiltInType.Int64, value,
                base.WriteInt64);
        }

        /// <inheritdoc/>
        public override void WriteUInt64(string? fieldName, ulong value)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt64, value,
                base.WriteUInt64);
        }

        /// <inheritdoc/>
        public override void WriteFloat(string? fieldName, float value)
        {
            ValidatedWrite(fieldName, BuiltInType.Float, value,
                base.WriteFloat);
        }

        /// <inheritdoc/>
        public override void WriteDouble(string? fieldName, double value)
        {
            ValidatedWrite(fieldName, BuiltInType.Double, value,
                base.WriteDouble);
        }

        /// <inheritdoc/>
        public override void WriteString(string? fieldName, string? value)
        {
            ValidatedWrite(fieldName, BuiltInType.String, value,
                base.WriteString);
        }

        /// <inheritdoc/>
        public override void WriteDateTime(string? fieldName, DateTime value)
        {
            ValidatedWrite(fieldName, BuiltInType.DateTime, value,
                base.WriteDateTime);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Uuid value)
        {
            ValidatedWrite(fieldName, BuiltInType.Guid, value,
                base.WriteGuid);
        }

        /// <inheritdoc/>
        public override void WriteGuid(string? fieldName, Guid value)
        {
            ValidatedWrite(fieldName, BuiltInType.Guid, value,
                base.WriteGuid);
        }

        /// <inheritdoc/>
        public override void WriteByteString(string? fieldName, byte[]? value)
        {
            ValidatedWrite(fieldName, BuiltInType.ByteString, value,
                base.WriteByteString);
        }

        /// <inheritdoc/>
        public override void WriteXmlElement(string? fieldName, XmlElement? value)
        {
            ValidatedWrite(fieldName, BuiltInType.XmlElement, value,
                base.WriteXmlElement);
        }

        /// <inheritdoc/>
        public override void WriteNodeId(string? fieldName, NodeId? value)
        {
            ValidatedWrite(fieldName, BuiltInType.NodeId, value,
                base.WriteNodeId);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeId(string? fieldName,
            ExpandedNodeId? value)
        {
            ValidatedWrite(fieldName, BuiltInType.ExpandedNodeId, value,
                base.WriteExpandedNodeId);
        }

        /// <inheritdoc/>
        public override void WriteStatusCode(string? fieldName, StatusCode value)
        {
            ValidatedWrite(fieldName, BuiltInType.StatusCode, value,
                base.WriteStatusCode);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedName(string? fieldName,
            QualifiedName? value)
        {
            ValidatedWrite(fieldName, BuiltInType.QualifiedName, value,
                base.WriteQualifiedName);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedText(string? fieldName,
            LocalizedText? value)
        {
            ValidatedWrite(fieldName, BuiltInType.LocalizedText, value,
                base.WriteLocalizedText);
        }

        /// <inheritdoc/>
        public override void WriteDataValue(string? fieldName, DataValue? value)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Should be the same
            if (!currentSchema.IsDataValue())
            {
                throw new EncodingException(
                    $"Schema {currentSchema.Fullname} is not " +
                    $"as expected {currentSchema.ToJson()}.", Schema.ToJson());
            }

            // Write type per schema
            base.WriteDataValue(fieldName, value);
            ValidatedPop(currentSchema);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObject(string? fieldName,
            ExtensionObject? value)
        {
            ValidatedWrite(fieldName, BuiltInType.ExtensionObject, value,
                base.WriteExtensionObject);
        }

        /// <inheritdoc/>
        public override void WriteEncodeable(string? fieldName,
            IEncodeable? value, Type? systemType)
        {
            var fullName = GetFullNameOfEncodeable(value, systemType,
                out var typeName, out var typeId);
            if (typeName == null)
            {
                // Perform unvalidated write. TODO: Throw?
                var currentSchema = GetFieldSchema(fieldName);
                base.WriteEncodeable(fieldName, value, systemType);
                ValidatedPop(currentSchema);
                return;
            }
            ValidatedWrite(fieldName, fullName ?? typeName,
                value, (f, v) => base.WriteEncodeable(f, v, systemType),
                fullName != null);
        }

        /// <inheritdoc/>
        public override void WriteVariant(string? fieldName, Variant value)
        {
            var currentSchema = GetFieldSchema(fieldName);
            WriteVariant(value, currentSchema);
            ValidatedPop(currentSchema);

            void WriteVariant(Variant value, Schema currentSchema)
            {
                var expectedType = _builtIns.GetSchemaForBuiltInType(
                                    BuiltInType.Variant, SchemaRank.Scalar);
                // Write as variant
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    base.WriteVariant(null, value);
                    return;
                }

                // Alternatively the schema could be nullable
                if (currentSchema is UnionSchema u)
                {
                    WriteWithSchema(u, () => WriteNullable(null, value.Value,
                        (_, _) => WriteVariantValueWithSchema(value, u.Schemas[1])));
                    return;
                }

                WriteVariantValueWithSchema(value, currentSchema);

                void WriteVariantValueWithSchema(Variant value, Schema currentSchema)
                {
                    // Write as built in type
                    if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                    {
                        if (value.TypeInfo != null)
                        {
                            if (SchemaUtils.GetRank(value.TypeInfo.ValueRank) != rank)
                            {
                                throw new EncodingException(
                                    $"Wrong schema {currentSchema.ToJson()} " +
                                    $"of field {fieldName ?? "unnamed"} for variant has wrong " +
                                    $"rank {rank} vs. {value.TypeInfo}.", Schema.ToJson());
                            }
                            if (builtInType == BuiltInType.Enumeration) // or int?
                            {
                                if (value.TypeInfo.BuiltInType != BuiltInType.Int32 &&
                                    value.TypeInfo.BuiltInType != BuiltInType.Enumeration)
                                {
                                    throw new EncodingException(
                                        $"Schema {currentSchema.ToJson()} " +
                                        $"of field {fieldName ?? "unnamed"} should be enumeration" +
                                        $"or int32 to support {value.TypeInfo}.", Schema.ToJson());
                                }
                            }
                            else if (value.TypeInfo.BuiltInType != builtInType)
                            {
                                throw new EncodingException(
                                    $"Wrong schema {currentSchema.ToJson()} " +
                                    $"of field {fieldName ?? "unnamed"} for variant of type" +
                                    $"{value.TypeInfo}.", Schema.ToJson());
                            }
                        }

                        WriteWithSchema(currentSchema,
                            () => WriteVariantValue(value, builtInType, rank));
                        return;
                    }

                    var typeId = currentSchema.GetDataTypeId(Context);
                    var encodeable = (value.Value as ExtensionObject)?.Body as IEncodeable;
                    var systemType = encodeable?.GetType()
                        ?? Context.Factory.GetSystemType(typeId);
                    if (systemType != null)
                    {
                        WriteWithSchema(currentSchema,
                            () => WriteEncodeable(null, encodeable, systemType));
                        return;
                    }

                    throw new EncodingException(
                        $"Variant schema {currentSchema.ToJson()} of " +
                        $"field {fieldName ?? "unnamed"} is neither variant nor built " +
                        "in type schema.", Schema.ToJson());
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName,
            Enum? value)
        {
            WriteEnumerated(fieldName, Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName, int value)
        {
            var currentSchema = GetFieldSchema(fieldName);
            WriteEnumeratedValue(fieldName, value, currentSchema);
            ValidatedPop(currentSchema);

            void WriteEnumeratedValue(string? fieldName, int value, Schema currentSchema)
            {
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank) &&
                    rank == SchemaRank.Scalar &&
                    (builtInType == BuiltInType.Int32 || builtInType == BuiltInType.Enumeration))
                {
                    WriteWithSchema(currentSchema, () => base.WriteEnumerated(fieldName, value));
                }
                else
                {
                    throw new EncodingException(
                        $"Invalid schema {currentSchema.ToJson()}. " +
                        "Enumerated values must be enums.", Schema.ToJson());
                }
            }
        }

        /// <inheritdoc/>
        public override void WriteBooleanArray(string? fieldName,
            IList<bool>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Boolean, values,
                base.WriteBooleanArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteSByteArray(string? fieldName,
            IList<sbyte>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.SByte, values,
                base.WriteSByteArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteByteArray(string? fieldName,
            IList<byte>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.ByteString, values,
                base.WriteByteArray, SchemaRank.Scalar);
        }

        /// <inheritdoc/>
        public override void WriteInt16Array(string? fieldName,
            IList<short>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Int16, values,
                base.WriteInt16Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteUInt16Array(string? fieldName,
            IList<ushort>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt16, values,
                base.WriteUInt16Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteInt32Array(string? fieldName,
            IList<int>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Int32, values,
                base.WriteInt32Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteUInt32Array(string? fieldName,
            IList<uint>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt32, values,
                base.WriteUInt32Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteInt64Array(string? fieldName,
            IList<long>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Int64, values,
                base.WriteInt64Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteUInt64Array(string? fieldName,
            IList<ulong>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.UInt64, values,
                base.WriteUInt64Array, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteFloatArray(string? fieldName,
            IList<float>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Float, values,
                base.WriteFloatArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteDoubleArray(string? fieldName,
            IList<double>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Double, values,
                base.WriteDoubleArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteStringArray(string? fieldName,
            IList<string?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.String, values,
                base.WriteStringArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteDateTimeArray(string? fieldName,
            IList<DateTime>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.DateTime, values,
                base.WriteDateTimeArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Uuid>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Guid, values,
                base.WriteGuidArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteGuidArray(string? fieldName,
            IList<Guid>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.Guid, values,
                base.WriteGuidArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteByteStringArray(string? fieldName,
            IList<byte[]?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.ByteString, values,
                base.WriteByteStringArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteXmlElementArray(string? fieldName,
            IList<XmlElement?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.XmlElement, values,
                base.WriteXmlElementArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteNodeIdArray(string? fieldName,
            IList<NodeId?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.NodeId, values,
                base.WriteNodeIdArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteExpandedNodeIdArray(string? fieldName,
            IList<ExpandedNodeId?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.ExpandedNodeId, values,
                base.WriteExpandedNodeIdArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteStatusCodeArray(string? fieldName,
            IList<StatusCode>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.StatusCode, values,
                base.WriteStatusCodeArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteDiagnosticInfoArray(string? fieldName,
            IList<DiagnosticInfo?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.DiagnosticInfo, values,
                base.WriteDiagnosticInfoArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteQualifiedNameArray(string? fieldName,
            IList<QualifiedName?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.QualifiedName, values,
                base.WriteQualifiedNameArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteLocalizedTextArray(string? fieldName,
            IList<LocalizedText?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.LocalizedText, values,
                base.WriteLocalizedTextArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteVariantArray(string? fieldName,
            IList<Variant>? values)
        {
            var currentSchema = GetFieldSchema(fieldName);
            WriteVariantArray(fieldName, values, currentSchema);
            ValidatedPop(currentSchema);

            void WriteVariantArray(string? fieldName, IList<Variant>? values,
                Schema currentSchema)
            {
                var expectedType = _builtIns.GetSchemaForBuiltInType(
                    BuiltInType.Variant, SchemaRank.Collection);
                // Write as variant collection
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    base.WriteVariantArray(fieldName, values);
                    return;
                }

                // Write as built in type
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                {
                    // When written in concise mode we get an array of bytes as byte string
                    if (builtInType == BuiltInType.ByteString && rank == SchemaRank.Scalar)
                    {
                        WriteWithSchema(currentSchema, () => WriteScalar(builtInType,
                            values?.Select(v => v.Value).Cast<byte>().ToArray()
                            ?? []));
                        return;
                    }
                    //
                    // Rank should be collection, and all values to write should be
                    // scalar and of the built in type
                    //
                    if (rank == SchemaRank.Collection)
                    {
                        WriteWithSchema(currentSchema, () => WriteArray(builtInType,
                            values?.Select(v => v.Value).ToArray()
                            ?? []));
                        return;
                    }

                    throw new EncodingException(
                        $"Wrong schema {currentSchema.ToJson()} " +
                        $"of field {fieldName ?? "unnamed"} to write variants.",
                        Schema.ToJson());
                }

                throw new EncodingException(
                    $"Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant collection " +
                    "nor built in type collection schema.", Schema.ToJson());
            }
        }

        /// <inheritdoc/>
        public override void WriteDataValueArray(string? fieldName,
            IList<DataValue?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.DataValue, values,
                base.WriteDataValueArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteExtensionObjectArray(string? fieldName,
            IList<ExtensionObject?>? values)
        {
            ValidatedWrite(fieldName, BuiltInType.ExtensionObject, values,
                base.WriteExtensionObjectArray, SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteEnumeratedArray(string? fieldName, Array? values,
            Type? systemType)
        {
            var ints = values == null ? [] : Enumerable
                .Range(0, values.GetLength(0))
                .Select(i => Convert.ToInt32((Enum?)values.GetValue(i),
                    CultureInfo.InvariantCulture))
                .ToArray();
            WriteEnumeratedArray(fieldName, ints, systemType);
        }

        /// <inheritdoc/>
        public override void WriteEnumeratedArray(string? fieldName, int[] values,
            Type? enumType)
        {
            var currentSchema = GetFieldSchema(fieldName);
            WriteEnumeratedArray(fieldName, currentSchema);
            ValidatedPop(currentSchema);

            void WriteEnumeratedArray(string? fieldName, Schema currentSchema)
            {
                if (currentSchema is ArraySchema a && a.ItemSchema is EnumSchema e)
                {
                    WriteWithSchema(currentSchema, () =>
                        base.WriteEnumeratedArray(fieldName, values, enumType));
                }
                else if (currentSchema.IsBuiltInType(out var builtInType, out var rank) &&
                    rank == SchemaRank.Collection &&
                    (builtInType == BuiltInType.Int32 || builtInType == BuiltInType.Enumeration))
                {
                    base.WriteEnumeratedArray(fieldName, values, enumType);
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
        public override void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            var currentSchema = GetFieldSchema(fieldName);
            WriteDataSet(dataSet, currentSchema);
            ValidatedPop(currentSchema);

            void WriteDataSet(DataSet dataSet, Schema currentSchema)
            {
                if (currentSchema is not RecordSchema r)
                {
                    throw new EncodingException(
                        $"Invalid schema {currentSchema.ToJson()}. " +
                        "Data sets must be records or maps.", Schema.ToJson());
                }

                // Serialize the fields in the schema
                var lookup = dataSet.DataSetFields.ToDictionary(f => f.Name, f => f.Value);
                foreach (var field in r.Fields)
                {
                    if (!lookup.TryGetValue(SchemaUtils.Unescape(field.Name),
                        out var dataValue))
                    {
                        dataValue = null;
                    }
                    WriteDataSetField(field.Name, dataValue, field.Schema);
                }
            }
        }

        /// <summary>
        /// Write data set field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="dataValue"></param>
        /// <param name="schema"></param>
        /// <exception cref="EncodingException"></exception>
        private void WriteDataSetField(string? fieldName, DataValue? dataValue,
            Schema schema)
        {
            if (schema is not UnionSchema u)
            {
                WriteVariant(fieldName, dataValue?.WrappedValue ?? default);
            }
            else if ((u.Schemas.Count > 1 && u.Schemas[1].IsDataValue())
                || dataValue == null)
            {
                WriteNullable(fieldName, dataValue, (_, v) =>
                {
                    var currentSchema = GetFieldSchema(fieldName);
                    WriteDataSetFieldValue(v, currentSchema);
                    ValidatedPop(currentSchema);
                });
            }
            else
            {
                WriteNullable(fieldName, dataValue.Value, (_, _) =>
                {
                    var currentSchema = GetFieldSchema(fieldName);
                    WriteDataSetFieldValue(dataValue, currentSchema);
                    ValidatedPop(currentSchema);
                });
            }

            void WriteDataSetFieldValue(DataValue value, Schema currentSchema)
            {
                // The field is a record that should contain the data value fields
                if (currentSchema is RecordSchema fieldRecord &&
                    fieldRecord.IsDataValue())
                {
                    foreach (var dvf in fieldRecord.Fields)
                    {
                        switch (dvf.Name)
                        {
                            case nameof(value.Value):
                                WriteVariant(nameof(value.Value),
                                    value.WrappedValue);
                                break;
                            case nameof(value.SourceTimestamp):
                                WriteDateTime(nameof(value.SourceTimestamp),
                                    value.SourceTimestamp);
                                break;
                            case nameof(value.SourcePicoseconds):
                                WriteUInt16(nameof(value.SourcePicoseconds),
                                    value.SourcePicoseconds);
                                break;
                            case nameof(value.ServerTimestamp):
                                WriteDateTime(nameof(value.ServerTimestamp),
                                    value.ServerTimestamp);
                                break;
                            case nameof(value.ServerPicoseconds):
                                WriteUInt16(nameof(value.ServerPicoseconds),
                                    value.ServerPicoseconds);
                                break;
                            case nameof(value.StatusCode):
                                WriteStatusCode(nameof(value.StatusCode),
                                    value.StatusCode);
                                break;
                            default:
                                throw new EncodingException(
                                    $"Unknown field {dvf.Name} in dataset field.");
                        }
                    }
                    return;
                }

                // Write value as variant
                WriteWithSchema(currentSchema, () => WriteVariant(null, value.WrappedValue));
            }
        }

        /// <inheritdoc/>
        public override void WriteObject(string? fieldName, string? typeName, Action writer)
        {
            var currentSchema = GetFieldSchema(fieldName);
            if (currentSchema is not RecordSchema r)
            {
                throw new EncodingException(
                    "Objects must be records or maps.", Schema.ToJson());
            }
            if (typeName != null && r.Name != typeName)
            {
                throw new EncodingException(
                    $"Object has type {r.Name} but expected {typeName}.", Schema.ToJson());
            }
            base.WriteObject(fieldName, typeName, writer);
            ValidatedPop(currentSchema);
        }

        /// <inheritdoc/>
        protected override void WriteDiagnosticInfo(string? fieldName,
            DiagnosticInfo value, int depth)
        {
            ValidatedWrite(fieldName, BuiltInType.DiagnosticInfo, value,
                (f, v) => base.WriteDiagnosticInfo(f, v, depth));
        }

        /// <inheritdoc/>
        protected override void WriteEncodedDataType(string? fieldName, ExtensionObject value)
        {
            ValidatedWrite(fieldName, SchemaUtils.NamespaceZeroName + ".EncodedDataType",
                value, base.WriteEncodedDataType);
        }

        /// <inheritdoc/>
        public override void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            var currentSchema = GetFieldSchema(null);
            if (!currentSchema.IsBuiltInType(out var expectedType, out var rank))
            {
                throw new EncodingException(
                    $"Schema {currentSchema.ToJson()} is not an array schema.",
                    Schema.ToJson());
            }
            if (rank != SchemaUtils.GetRank(valueRank) || builtInType != expectedType)
            {
                throw new EncodingException(
                    $"Schema {currentSchema.ToJson()} does not match expected rank and type.",
                    Schema.ToJson());
            }
            base.WriteArray(fieldName, array, valueRank, builtInType);
            ValidatedPop(currentSchema);
        }

        /// <inheritdoc/>
        public override void WriteArray<T>(string? fieldName, IList<T>? values,
            Action<T> writer, string? typeName = null)
        {
            ValidatedWriteArray(
                () => base.WriteArray(fieldName, values, writer, typeName));
        }

        /// <inheritdoc/>
        protected override void WriteArray(string? fieldName, Array? values,
            Action<object?> writer)
        {
            ValidatedWriteArray(
                () => base.WriteArray(fieldName, values, writer));
        }

        /// <inheritdoc/>
        public override void WriteNull<T>(string? fieldName, T? value) where T : default
        {
            ValidatedWrite(fieldName, BuiltInType.Null, value,
                base.WriteNull, SchemaRank.Scalar);
        }

        /// <inheritdoc/>
        protected override void WriteNull(BuiltInType builtInType, SchemaRank valueRank)
        {
            var currentSchema = GetFieldSchema(null);
            // Should be the same
            if (currentSchema.Tag != Schema.Type.Null &&
                (!currentSchema.IsBuiltInType(out _, out var rank) || rank != valueRank))
            {
                throw new EncodingException(
                   $"Schema {currentSchema.Fullname} is not expected variant null type.",
                   Schema.ToJson());
            }
            base.WriteNull(builtInType, valueRank);
            ValidatedPop(currentSchema);
        }

        /// <inheritdoc/>
        protected override void WriteNullable<T>(string? fieldName, T? value,
            Action<string?, T> writer) where T : class
        {
            base.WriteNullable(fieldName, value, (f, v) =>
            {
                // Check the schema is a nullable union schema
                if (Current is not UnionSchema u ||
                    u.Count != 2 || u.Schemas[0].Tag != Schema.Type.Null)
                {
                    throw new EncodingException(
                        $"Union schema {Current.ToJson()} of nullable " +
                        $"field {fieldName ?? "unnamed"} does not match.", Schema.ToJson());
                }
                writer(f, v);
            });
        }

        /// <inheritdoc/>
        public override void WriteUnion(string? fieldName, int index,
            Action<int> writer)
        {
            // Get the union schema
            var currentSchema = GetFieldSchema(fieldName);
            if (currentSchema is not UnionSchema)
            {
                throw new EncodingException(
                    $"Union field {fieldName ?? "unnamed"} must be a union " +
                    $"schema but is {currentSchema.ToJson()} schema.", Schema.ToJson());
            }
            base.WriteUnion(fieldName, index, writer);
        }

        /// <inheritdoc/>
        protected override void StartUnion(int index)
        {
            base.StartUnion(index);
            _schema.ExpectUnionItem = u =>
            {
                if (index < u.Schemas.Count && index >= 0)
                {
                    return u.Schemas[index];
                }
                throw new EncodingException(
                    $"Union index {index} not found in union {u.ToJson()}.",
                    Schema.ToJson());
            };
        }

        /// <inheritdoc/>
        protected override void EndUnion()
        {
            var unionSchema = _schema.Pop();
            if (unionSchema is not UnionSchema)
            {
                throw new EncodingException(
                    $"Expected union schema but got {unionSchema.ToJson()} after " +
                    "completing union.", Schema.ToJson());
            }
            base.EndUnion();
        }

        /// <summary>
        /// Perform the read of the built in type after validating the
        /// operation against the schema of the field if there is a field.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="builtInType"></param>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        /// <param name="valueRank"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private void ValidatedWrite<T>(string? fieldName, BuiltInType builtInType,
            T value, Action<string?, T> writer, SchemaRank valueRank = SchemaRank.Scalar)
        {
            // Get expected schema
            var expectedType = _builtIns.GetSchemaForBuiltInType(builtInType, valueRank);
            var expectedName = expectedType.Fullname;
            ValidatedWrite(fieldName, expectedName, value, writer);
        }

        /// <summary>
        /// Validates reading a value against the schema
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="expectedSchemaName"></param>
        /// <param name="value"></param>
        /// <param name="writer"></param>
        /// <param name="isFullName"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private void ValidatedWrite<T>(string? fieldName, string expectedSchemaName,
            T value, Action<string?, T> writer, bool isFullName = true)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Should be the same
            var curName = isFullName ? currentSchema.Fullname : currentSchema.Name;
            if (curName != expectedSchemaName)
            {
                throw new EncodingException(
                    $"Schema {currentSchema.Fullname} is not as " +
                    $"expected {expectedSchemaName}.", Schema.ToJson());
            }

            // Write type per schema
            writer(fieldName, value);
            ValidatedPop(currentSchema);
        }

        /// <summary>
        /// Validated array writer
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private void ValidatedWriteArray(Action writer)
        {
            var currentSchema = GetFieldSchema(null);
            if (currentSchema is not ArraySchema)
            {
                throw new EncodingException(
                    $"Writing array field but schema {currentSchema.ToJson()} is not " +
                    "array schema.", Schema.ToJson());
            }
            writer();
            ValidatedPop(currentSchema);
        }

        /// <summary>
        /// Use specified schema by pushing it on top for writing
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="writer"></param>
        private void WriteWithSchema(Schema schema, Action writer)
        {
            var top = schema.AsArray();
            _schema.Push(top);
            writer();
            ValidatedPop(top);
        }

        /// <summary>
        /// Validate pop from stack should be expected schema
        /// </summary>
        /// <param name="expectedSchema"></param>
        /// <exception cref="EncodingException"></exception>
        private void ValidatedPop(Schema expectedSchema)
        {
            // Pop array from stack
            var completedSchema = _schema.Pop();
            if (completedSchema != expectedSchema)
            {
                throw new EncodingException(
                    $"Failed to pop schema. Expected {expectedSchema.ToJson()} " +
                    $"but got {completedSchema.ToJson()}.", Schema.ToJson());
            }
        }

        /// <summary>
        /// Get next schema
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="EncodingException"></exception>
        private Schema GetFieldSchema(string? fieldName)
        {
            _schema.ExpectedFieldName = fieldName;
            var current = _schema.Current;
            if (!_schema.TryMoveNext())
            {
                throw new EncodingException(
                    $"No schema for field {fieldName ?? "unnamed"} " +
                    $"found in {current.ToJson()}.", Schema.ToJson());
            }
            return _schema.Current;
        }

        private readonly AvroBuiltInSchemas _builtIns = new();
        private readonly AvroSchemaTraverser _schema;
    }

    /// <summary>
    /// Schemaless encoder
    /// </summary>
    internal sealed class SchemalessAvroEncoder : BaseAvroEncoder
    {
        /// <inheritdoc/>
        public SchemalessAvroEncoder(Stream stream,
            IServiceMessageContext context, bool leaveOpen = true)
            : base(stream, context, leaveOpen)
        {
        }
    }
}
