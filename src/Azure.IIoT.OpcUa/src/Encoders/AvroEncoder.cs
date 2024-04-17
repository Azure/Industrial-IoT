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
    using System.Collections.Generic;
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
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to encode. Schema {currentSchema.Fullname} is not " +
                    $"as expected {currentSchema.ToJson()}.\n{Schema.ToJson()}");
            }

            // Write type per schema
            base.WriteDataValue(fieldName, value);

            // Pop the type from the stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
            }
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
                out var typeName);
            if (typeName == null)
            {
                // Perform unvalidated write. TODO: Throw?
                GetFieldSchema(fieldName);
                base.WriteEncodeable(fieldName, value, systemType);
                _schema.Pop();
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
            var expectedType = _builtIns.GetSchemaForBuiltInType(BuiltInType.Variant,
                SchemaRank.Scalar);
            try
            {
                // Write as variant
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    base.WriteVariant(fieldName, value);
                    return;
                }

                // Write as built in type
                if (currentSchema.IsBuiltInType(out var builtInType, out var rank))
                {
                    if (value.TypeInfo.BuiltInType != builtInType ||
                        SchemaUtils.GetRank(value.TypeInfo.ValueRank) != rank)
                    {
                        throw new ServiceResultException(StatusCodes.BadEncodingError,
                            $"Failed to encode. Wrong schema {currentSchema.ToJson()} " +
                            $"of field {fieldName ?? "unnamed"} for variant of type" +
                            $"{value.TypeInfo} .\n{Schema.ToJson()}");
                    }
                    _schema.Push(ArraySchema.Create(currentSchema));
                    WriteVariantValue(value);
                    _schema.Pop();
                    return;
                }

                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to encode. Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant nor built " +
                    $"in type schema. .\n{Schema.ToJson()}");
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <inheritdoc/>
        public override void WriteEnumerated(string? fieldName,
            Enum? value)
        {
            ValidatedWrite(fieldName, BuiltInType.Enumeration, value,
                base.WriteEnumerated);
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
            ValidatedWrite(fieldName, BuiltInType.Byte, values,
                base.WriteByteArray, SchemaRank.Collection);
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
            var expectedType = _builtIns.GetSchemaForBuiltInType(BuiltInType.Variant,
                SchemaRank.Collection);
            try
            {
                // Write as variant collection
                if (currentSchema.Fullname == expectedType.Fullname)
                {
                    base.WriteVariantArray(fieldName, values);
                    return;
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
                        throw new ServiceResultException(StatusCodes.BadEncodingError,
                            $"Failed to encode. Wrong schema {currentSchema.ToJson()} " +
                            $"of field {fieldName ?? "unnamed"} to write variants.\n" +
                            $"{Schema.ToJson()}");
                    }
                    _schema.Push(ArraySchema.Create(currentSchema));
                    WriteArray(builtInType, values?.Select(v => v.Value).ToArray()
                        ?? Array.Empty<object>());
                    _schema.Pop();
                    return;
                }

                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to encode. Variant schema {currentSchema.ToJson()} of " +
                    $"field {fieldName ?? "unnamed"} is neither variant collection " +
                    $"nor built in type collection schema. .\n{Schema.ToJson()}");
            }
            finally
            {
                _schema.Pop();
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
        public override void WriteEnumeratedArray(string? fieldName,
            Array? values, Type? systemType)
        {
            ValidatedWrite(fieldName, BuiltInType.Enumeration, values,
                (f, v) => base.WriteEnumeratedArray(f, v, systemType),
                SchemaRank.Collection);
        }

        /// <inheritdoc/>
        public override void WriteDataSet(string? fieldName, DataSet dataSet)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema r)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Invalid schema {schema.ToJson()}. " +
                    $"Data sets must be records or maps.\n{Schema.ToJson()}");
            }
            try
            {
                // Serialize the fields in the schema
                foreach (var field in r.Fields)
                {
                    var isVariant = field.Schema.IsBuiltInType(out var bt, out _)
                        && bt == BuiltInType.Variant;
                    if (!dataSet.TryGetValue(SchemaUtils.Unescape(field.Name),
                            out var dataValue)
                        || dataValue == null)
                    {
                        if (isVariant)
                        {
                            WriteVariant(field.Name, default);
                        }
                        else
                        {
                            WriteUnion(0);
                            WriteNull(field.Name, dataValue);
                        }
                    }
                    else
                    {
                        if (isVariant)
                        {
                            WriteVariant(field.Name, dataValue.WrappedValue);
                        }
                        else
                        {
                            WriteUnion(1);
                            WriteDataSetField(field.Name, dataValue);
                        }
                    }
                }
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <inheritdoc/>
        public override void WriteObject(string? fieldName, string? typeName, Action writer)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema r)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Objects must be records or maps.\n{Schema.ToJson()}");
            }
            if (typeName != null && r.Name != typeName)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Object has type {r.Name} but expected {typeName}\n{Schema.ToJson()}");
            }
            try
            {
                base.WriteObject(fieldName, typeName, writer);
            }
            finally
            {
                _schema.Pop();
            }
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

        /// <summary>
        /// Write data set field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private void WriteDataSetField(string? fieldName, DataValue value)
        {
            var schema = GetFieldSchema(fieldName);
            if (schema is not RecordSchema fieldRecord)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Invalid schema {schema.ToJson()}." +
                    $"Data set fields must be records.\n{Schema.ToJson()}");
            }

            // The field is a record that should contain the data value fields
            try
            {
                if (fieldRecord.IsDataValue())
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
                                throw new ServiceResultException(
                                    StatusCodes.BadEncodingError,
                                    $"Unknown field {dvf.Name} in dataset field.");
                        }
                    }
                    return;
                }

                // Write value as variant
                if (fieldRecord.IsBuiltInType(out var builtInType, out var rank) &&
                    builtInType != BuiltInType.DataValue)
                {
                    _schema.Push(ArraySchema.Create(fieldRecord));
                    WriteVariant(null, value.WrappedValue);
                    _schema.Pop();
                    return;
                }

                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Data set field {fieldName} must be a data value.\n{Schema.ToJson()}");
            }
            finally
            {
                _schema.Pop();
            }
        }

        /// <inheritdoc/>
        public override void WriteArray(string? fieldName, object array,
            int valueRank, BuiltInType builtInType)
        {
            ValidatedWriteArray(
                () => base.WriteArray(fieldName, array, valueRank, builtInType));
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
                base.WriteNull<T>, SchemaRank.Scalar);
        }

        /// <inheritdoc/>
        public override void WriteUnion(int index)
        {
            base.WriteUnion(index);
            _schema.ExpectUnionItem = u =>
            {
                if (index < u.Schemas.Count && index >= 0)
                {
                    return u.Schemas[index];
                }
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Union index {index} not found in union {u.ToJson()}\n{Schema.ToJson()}");
            };
            GetFieldSchema(null);
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
        /// <exception cref="ServiceResultException"></exception>
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
        /// <exception cref="ServiceResultException"></exception>
        private void ValidatedWrite<T>(string? fieldName, string expectedSchemaName,
            T value, Action<string?, T> writer, bool isFullName = true)
        {
            // Get current field schema
            var currentSchema = GetFieldSchema(fieldName);

            // Should be the same
            var curName = isFullName ? currentSchema.Fullname : currentSchema.Name;
            if (curName != expectedSchemaName)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to encode. Schema {currentSchema.Fullname} is not as " +
                    $"expected {expectedSchemaName}.\n{Schema.ToJson()}");
            }

            // Write type per schema
            writer(fieldName, value);

            // Pop the type from the stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
            }
        }

        /// <summary>
        /// Validated array writer
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private void ValidatedWriteArray(Action writer)
        {
            var currentSchema = GetFieldSchema(null);
            if (currentSchema is not ArraySchema arr)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Reading array field but schema {currentSchema.ToJson()} is not " +
                    $"array schema.\n{Schema.ToJson()}");
            }

            writer();

            // Pop array from stack
            var completedSchema = _schema.Pop();
            if (completedSchema != currentSchema)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to pop built in type.\n{Schema.ToJson()}");
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
            var current = _schema.Current;
            if (!_schema.TryMoveNext())
            {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Failed to decode. No schema for field {fieldName ?? "unnamed"} " +
                    $"found in {current.ToJson()}.\n{Schema.ToJson()}");
            }
            return _schema.Current;
        }

        private readonly AvroBuiltInAvroSchemas _builtIns = new();
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
