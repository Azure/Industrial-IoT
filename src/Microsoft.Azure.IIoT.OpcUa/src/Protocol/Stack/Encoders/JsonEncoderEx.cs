/* Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

namespace Opc.Ua.Encoders {
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.IO;
    using System.Globalization;
    using Newtonsoft.Json;

    /// <summary>
    /// Writes objects to a json
    /// </summary>
    public class JsonEncoderEx : IEncoder, IDisposable {

        /// <inheritdoc/>
        public EncodingType EncodingType => EncodingType.Json;

        /// <inheritdoc/>
        public ServiceMessageContext Context { get; }

        /// <summary>
        /// Whether to use reversible encoding or not
        /// </summary>
        public bool UseReversibleEncoding { get; set; } = true;

        /// <summary>
        /// Perform xml serialization to json
        /// </summary>
        public bool PerformXmlSerialization { get; set; } = true;

        /// <summary>
        /// Encode nodes as uri
        /// </summary>
        public bool UseNodeUriEncoding { get; set; } = true;

        /// <summary>
        /// Create encoder
        /// </summary>
        public JsonEncoderEx(ServiceMessageContext context, Stream stream) :
            this (context, new StreamWriter(stream, new UTF8Encoding(false))) {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        public JsonEncoderEx(ServiceMessageContext context, TextWriter writer) :
            this(context, new JsonTextWriter(writer) {
                AutoCompleteOnClose = true,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            }) {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        public JsonEncoderEx(ServiceMessageContext context, JsonWriter writer) {
            _namespaces = new Stack<string>();
            Context = context;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _writer.WriteStartObject();
        }

        /// <summary>
        /// Completes writing
        /// </summary>
        public void Close() {
            if (_writer != null) {
                _writer.WriteEndObject();
                _writer.Close();
                _writer = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose() => Close();

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable namespaceUris,
            StringTable serverUris) {
            _namespaceMappings = null;

            if (namespaceUris != null && Context.NamespaceUris != null) {
                _namespaceMappings = namespaceUris.CreateMapping(
                    Context.NamespaceUris, false);
            }
            _serverMappings = null;
            if (serverUris != null && Context.ServerUris != null) {
                _serverMappings = serverUris.CreateMapping(Context.ServerUris, false);
            }
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri) =>
            _namespaces.Push(namespaceUri);

        /// <inheritdoc/>
        public void PopNamespace() =>
            _namespaces.Pop();

        /// <inheritdoc/>
        public void WriteSByte(string property, sbyte value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteByte(string property, byte value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt16(string property, short value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt16(string property, ushort value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt32(string property, int value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt32(string property, uint value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt64(string property, long value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt64(string property, ulong value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteString(string property, string value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteBoolean(string property, bool value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteFloat(string property, float value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (float.IsNaN(value) || float.IsPositiveInfinity(value) ||
                float.IsNegativeInfinity(value)) {
                _writer.WriteValue("NaN");
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteDouble(string property, double value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (double.IsNaN(value) || double.IsPositiveInfinity(value) ||
                double.IsNegativeInfinity(value)) {
                _writer.WriteValue("NaN");
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteDateTime(string property, DateTime value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (value == DateTime.MinValue) {
                _writer.WriteNull();
            }
            else {
                _writer.WriteValue(XmlConvert.ToString(value,
                    XmlDateTimeSerializationMode.RoundtripKind));
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string property, Uuid value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (value == Uuid.Empty) {
                _writer.WriteNull();
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string property, Guid value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (value == Guid.Empty) {
                _writer.WriteNull();
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByteString(string property, byte[] value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (value == null || value.Length == 0) {
                _writer.WriteNull();
                return;
            }
            // check the length.
            if (Context.MaxByteStringLength > 0 &&
                Context.MaxByteStringLength < value.Length) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteXmlElement(string property, XmlElement value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            if (value == null) {
                _writer.WriteNull();
            }
            else if (PerformXmlSerialization) {
                var json = JsonConvertEx.SerializeObject(value);
                _writer.WriteRawValue(json);
            }
            else {
                // Back compat to json encoding
                var xml = value.OuterXml;
                _writer.WriteValue(Encoding.UTF8.GetBytes(xml));
            }
        }

        /// <inheritdoc/>
        public void WriteNodeId(string property, NodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(property);
            }
            else if (UseReversibleEncoding) {
                if (UseNodeUriEncoding) {
                    WriteString(property, value.AsString(Context));
                }
                else {
                    // Back compat to json encoding
                    WriteString(property, value.ToString());
                }
            }
            else {
                PushObject(property);
                _writer.WritePropertyName("Id");
                _writer.WriteValue(new NodeId(value.Identifier, 0).ToString());
                WriteNamespaceIndex(value.NamespaceIndex);
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string property, ExpandedNodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(property);
            }
            else if (UseReversibleEncoding) {
                if (UseNodeUriEncoding) {
                    WriteString(property, value.AsString(Context));
                }
                else {
                    // Back compat to json encoding
                    WriteString(property, value.ToString());
                }
            }
            else {
                PushObject(property);
                _writer.WritePropertyName("Id");
                _writer.WriteValue(new NodeId(value.Identifier, 0).ToString());
                WriteNamespaceIndex(value.NamespaceIndex);
                WriteServerIndex(value.ServerIndex);
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string property, StatusCode value) {
            if (value == StatusCodes.Good) {
                WriteNull(property);
            }
            else if (UseReversibleEncoding) {
                WriteUInt32(property, value.Code);
            }
            else {
                PushObject(property);
                WriteUInt32("Code", value.Code);
                WriteString("Symbol", StatusCode.LookupSymbolicId(value.CodeBits));
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfo(string property, DiagnosticInfo value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                if (value.SymbolicId >= 0) {
                    WriteInt32("SymbolicId", value.SymbolicId);
                }
                if (value.NamespaceUri >= 0) {
                    WriteInt32("NamespaceUri", value.NamespaceUri);
                }
                if (value.Locale >= 0) {
                    WriteInt32("Locale", value.Locale);
                }
                if (value.LocalizedText >= 0) {
                    WriteInt32("LocalizedText", value.LocalizedText);
                }
                if (value.AdditionalInfo != null) {
                    WriteString("AdditionalInfo", value.AdditionalInfo);
                }
                if (value.InnerStatusCode != StatusCodes.Good) {
                    WriteStatusCode("InnerStatusCode", value.InnerStatusCode);
                }
                if (value.InnerDiagnosticInfo != null) {
                    WriteDiagnosticInfo("InnerDiagnosticInfo", value.InnerDiagnosticInfo);
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteQualifiedName(string property, QualifiedName value) {
            if (QualifiedName.IsNull(value)) {
                WriteNull(property);
            }
            else if (UseReversibleEncoding) {
                PushObject(property);
                WriteString("Name", value.Name);
                if (value.NamespaceIndex > 0) {
                    WriteUInt16("Uri", value.NamespaceIndex);
                }
                PopObject();
            }
            else {
                PushObject(property);
                WriteString("Name", value.Name);
                WriteNamespaceIndex(value.NamespaceIndex);
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteLocalizedText(string property, LocalizedText value) {
            if (LocalizedText.IsNullOrEmpty(value)) {
                WriteNull(property);
            }
            else if (UseReversibleEncoding || value.Locale != null) {
                PushObject(property);
                WriteString("Text", value.Text);
                if (!string.IsNullOrEmpty(value.Locale)) {
                    WriteString("Locale", value.Locale);
                }
                PopObject();
            }
            else {
                WriteString(property, value.Text);
            }
        }

        /// <inheritdoc/>
        public void WriteVariant(string property, Variant value) {
            if (Variant.Null == value) {
                WriteNull(property);
            }
            else {
                var isNull = value.TypeInfo == null ||
                    value.TypeInfo.BuiltInType == BuiltInType.Null ||
                    value.Value == null;

                if (UseReversibleEncoding && !isNull) {
                    PushObject(property);
                    WriteByte("Type", (byte)value.TypeInfo.BuiltInType);
                    property = "Body";
                }

                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                WriteVariantContents(value.Value, value.TypeInfo);

                if (UseReversibleEncoding && !isNull) {
                    if (value.Value is Matrix matrix) {
                        WriteInt32Array("Dimensions", matrix.Dimensions);
                    }
                    PopObject();
                }
            }
        }

        /// <inheritdoc/>
        public void WriteDataValue(string property, DataValue value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                if (value != null) {
                    if (value.WrappedValue.TypeInfo != null &&
                        value.WrappedValue.TypeInfo.BuiltInType != BuiltInType.Null) {
                        WriteVariant("Value", value.WrappedValue);
                    }

                    if (value.StatusCode != StatusCodes.Good) {
                        WriteStatusCode("StatusCode", value.StatusCode);
                    }
                    if (value.SourceTimestamp != DateTime.MinValue) {
                        WriteDateTime("SourceTimestamp", value.SourceTimestamp);
                        if (value.SourcePicoseconds != 0) {
                            WriteUInt16("SourcePicoseconds", value.SourcePicoseconds);
                        }
                    }
                    if (value.ServerTimestamp != DateTime.MinValue) {
                        WriteDateTime("ServerTimestamp", value.ServerTimestamp);
                        if (value.ServerPicoseconds != 0) {
                            WriteUInt16("ServerPicoseconds", value.ServerPicoseconds);
                        }
                    }
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string property, ExtensionObject value) {
            if (value == null || value.Encoding == ExtensionObjectEncoding.None) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                if (value != null) {
                    if (value.Body is IEncodeable encodeable) {
                        if (UseReversibleEncoding) {
                            WriteExpandedNodeId("TypeId", encodeable.TypeId);
                            WriteEncodeable("Body", encodeable, null);
                        }
                        else {
                            encodeable.Encode(this);
                        }
                    }
                    else {
                        WriteExpandedNodeId("TypeId", value.TypeId);
                        if (value.Body != null) {
                            switch (value.Encoding) {
                                case ExtensionObjectEncoding.Xml:
                                    WriteXmlElement("Body", value.Body as XmlElement);
                                    break;
                                case ExtensionObjectEncoding.Json:
                                    if (value.Body is string json) {
                                        _writer.WriteRaw(json);
                                    }
                                    if (value.Body is byte[] buffer) {
                                        json = Encoding.UTF8.GetString(buffer);
                                        _writer.WriteRaw(json);
                                    }
                                    break;
                                case ExtensionObjectEncoding.Binary:
                                    WriteByteString("Body", value.Body as byte[]);
                                    break;
                            }
                        }
                    }
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string property, IEncodeable value, Type systemType) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                if (value != null) {
                    value.Encode(this);
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string property, Enum value) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            var numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (UseReversibleEncoding) {
                _writer.WriteValue(numeric);
            }
            else {
                _writer.WriteValue($"{value}_{numeric}");
            }
        }

        /// <inheritdoc/>
        public void WriteBooleanArray(string property, IList<bool> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteSByteArray(string property, IList<sbyte> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteByteArray(string property, IList<byte> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt16Array(string property, IList<short> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt16Array(string property, IList<ushort> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt32Array(string property, IList<int> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt32Array(string property, IList<uint> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt64Array(string property, IList<long> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt64Array(string property, IList<ulong> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteFloatArray(string property, IList<float> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteDoubleArray(string property, IList<double> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteStringArray(string property, IList<string> values) =>
            WriteArray(property, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteDateTimeArray(string property, IList<DateTime> values) =>
            WriteArray(property, values, v => WriteDateTime(null, v));

        /// <inheritdoc/>
        public void WriteGuidArray(string property, IList<Uuid> values) =>
            WriteArray(property, values, v => WriteGuid(null, v));

        /// <inheritdoc/>
        public void WriteGuidArray(string property, IList<Guid> values) =>
            WriteArray(property, values, v => WriteGuid(null, v));

        /// <inheritdoc/>
        public void WriteByteStringArray(string property, IList<byte[]> values) =>
            WriteArray(property, values, v => WriteByteString(null, v));

        /// <inheritdoc/>
        public void WriteXmlElementArray(string property, IList<XmlElement> values) =>
            WriteArray(property, values, v => WriteXmlElement(null, v));

        /// <inheritdoc/>
        public void WriteNodeIdArray(string property, IList<NodeId> values) =>
            WriteArray(property, values, v => WriteNodeId(null, v));

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string property, IList<ExpandedNodeId> values) =>
            WriteArray(property, values, v => WriteExpandedNodeId(null, v));

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string property, IList<StatusCode> values) =>
            WriteArray(property, values, v => WriteStatusCode(null, v));

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string property, IList<DiagnosticInfo> values) =>
            WriteArray(property, values, v => WriteDiagnosticInfo(null, v));

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string property, IList<QualifiedName> values) =>
            WriteArray(property, values, v => WriteQualifiedName(null, v));

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string property, IList<LocalizedText> values) =>
            WriteArray(property, values, v => WriteLocalizedText(null, v));

        /// <inheritdoc/>
        public void WriteVariantArray(string property, IList<Variant> values) {
            WriteArray(property, values, v => {
                if (v == Variant.Null) {
                    _writer.WriteNull();
                }
                else {
                    WriteVariant(null, v);
                }
            });
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string property, IList<DataValue> values) =>
            WriteArray(property, values, v => WriteDataValue(null, v));

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string property, IList<ExtensionObject> values) =>
            WriteArray(property, values, v => WriteExtensionObject(null, v));

        /// <inheritdoc/>
        public void WriteEncodeableArray(string property, IList<IEncodeable> values,
            Type systemType) =>
            WriteArray(property, values, v => WriteEncodeable(null, v, systemType));

        /// <inheritdoc/>
        public void WriteObjectArray(string property, IList<object> values) {
            PushArray(property, values?.Count ?? 0);
            if (values != null) {
                for (var ii = 0; ii < values.Count; ii++) {
                    WriteVariant("Variant", new Variant(values[ii]));
                }
            }
            PopArray();
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string property, Array values, Type systemType) {
            if (values == null || values.Length == 0) {
                WriteNull(property);
            }
            else {
                PushArray(property, values.Length);
                // encode each element in the array.
                foreach (Enum value in values) {
                    WriteEnumerated(null, value);
                }
                PopArray();
            }
        }

        /// <summary>
        /// Writes the contents of an Variant to the stream.
        /// </summary>
        private void WriteVariantContents(object value, TypeInfo typeInfo) {
            // check for null.
            if (value == null) {
                return;
            }
            // write scalar.
            if (typeInfo.ValueRank < 0) {
                switch (typeInfo.BuiltInType) {
                    case BuiltInType.Boolean:
                        WriteBoolean(null, (bool)value);
                        return;
                    case BuiltInType.SByte:
                        WriteSByte(null, (sbyte)value);
                        return;
                    case BuiltInType.Byte:
                        WriteByte(null, (byte)value);
                        return;
                    case BuiltInType.Int16:
                        WriteInt16(null, (short)value);
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16(null, (ushort)value);
                        return;
                    case BuiltInType.Int32:
                        WriteInt32(null, (int)value);
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32(null, (uint)value);
                        return;
                    case BuiltInType.Int64:
                        WriteInt64(null, (long)value);
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64(null, (ulong)value);
                        return;
                    case BuiltInType.Float:
                        WriteFloat(null, (float)value);
                        return;
                    case BuiltInType.Double:
                        WriteDouble(null, (double)value);
                        return;
                    case BuiltInType.String:
                        WriteString(null, (string)value);
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTime(null, (DateTime)value);
                        return;
                    case BuiltInType.Guid:
                        WriteGuid(null, (Uuid)value);
                        return;
                    case BuiltInType.ByteString:
                        WriteByteString(null, (byte[])value);
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElement(null, (XmlElement)value);
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeId(null, (NodeId)value);
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeId(null, (ExpandedNodeId)value);
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCode(null, (StatusCode)value);
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedName(null, (QualifiedName)value);
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedText(null, (LocalizedText)value);
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObject(null, (ExtensionObject)value);
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValue(null, (DataValue)value);
                        return;
                    case BuiltInType.Enumeration:
                        WriteInt32(null, (int)value);
                        return;
                }
            }

            // write array.
            else if (typeInfo.ValueRank <= 1) {
                switch (typeInfo.BuiltInType) {
                    case BuiltInType.Boolean:
                        WriteBooleanArray(null, (bool[])value);
                        return;
                    case BuiltInType.SByte:
                        WriteSByteArray(null, (sbyte[])value);
                        return;
                    case BuiltInType.Byte:
                        WriteByteArray(null, (byte[])value);
                        return;
                    case BuiltInType.Int16:
                        WriteInt16Array(null, (short[])value);
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16Array(null, (ushort[])value);
                        return;
                    case BuiltInType.Int32:
                        WriteInt32Array(null, (int[])value);
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32Array(null, (uint[])value);
                        return;
                    case BuiltInType.Int64:
                        WriteInt64Array(null, (long[])value);
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64Array(null, (ulong[])value);
                        return;
                    case BuiltInType.Float:
                        WriteFloatArray(null, (float[])value);
                        return;
                    case BuiltInType.Double:
                        WriteDoubleArray(null, (double[])value);
                        return;
                    case BuiltInType.String:
                        WriteStringArray(null, (string[])value);
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTimeArray(null, (DateTime[])value);
                        return;
                    case BuiltInType.Guid:
                        WriteGuidArray(null, (Uuid[])value);
                        return;
                    case BuiltInType.ByteString:
                        WriteByteStringArray(null, (byte[][])value);
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElementArray(null, (XmlElement[])value);
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeIdArray(null, (NodeId[])value);
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeIdArray(null, (ExpandedNodeId[])value);
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCodeArray(null, (StatusCode[])value);
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedNameArray(null, (QualifiedName[])value);
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedTextArray(null, (LocalizedText[])value);
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObjectArray(null, (ExtensionObject[])value);
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValueArray(null, (DataValue[])value);
                        return;

                    case BuiltInType.Enumeration:
                        var enums = value as Enum[];
                        var values = new string[enums.Length];

                        for (var ii = 0; ii < enums.Length; ii++) {
                            var text = enums[ii].ToString();
                            text += "_";
                            text += ((int)(object)enums[ii]).ToString(CultureInfo.InvariantCulture);
                            values[ii] = text;
                        }

                        WriteStringArray(null, values);
                        return;
                    case BuiltInType.Variant:
                        if (value is Variant[] variants) {
                            WriteVariantArray(null, variants);
                            return;
                        }
                        if (value is object[] objects) {
                            WriteObjectArray(null, objects);
                            return;
                        }
                        throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                            "Unexpected type encountered while encoding an array" +
                            $" of Variants:{value.GetType()}");
                }
            }

            // write matrix.
            else if (typeInfo.ValueRank > 1) {
                WriteMatrix(null, (Matrix)value);
                return;
            }

            // oops - should never happen.
            throw new ServiceResultException(StatusCodes.BadEncodingError,
                $"Type '{value.GetType().FullName}' is not allowed in an Variant.");
        }

        /// <summary>
        /// Writes an DataValue array to the stream.
        /// </summary>
        private void WriteMatrix(string property, Matrix value) {
            PushObject(property);
            WriteVariant("Matrix", new Variant(value.Elements,
                new TypeInfo(value.TypeInfo.BuiltInType, ValueRanks.OneDimension)));
            WriteInt32Array("Dimensions", value.Dimensions);
            PopObject();
        }

        /// <summary>
        /// Writes namespace
        /// </summary>
        /// <param name="namespaceIndex"></param>
        private void WriteNamespaceIndex(ushort namespaceIndex) {
            if (namespaceIndex > 1) {
                var uri = Context.NamespaceUris.GetString(namespaceIndex);
                if (!string.IsNullOrEmpty(uri)) {
                    WriteString("Uri", uri);
                    return;
                }
            }
            if (_namespaceMappings != null &&
                _namespaceMappings.Length > namespaceIndex) {
                namespaceIndex = _namespaceMappings[namespaceIndex];
            }
            if (namespaceIndex != 0) {
                WriteUInt32("Index", namespaceIndex);
            }
        }

        /// <summary>
        /// Write server uri
        /// </summary>
        /// <param name="serverIndex"></param>
        private void WriteServerIndex(uint serverIndex) {
            if (serverIndex > 1) {
                var uri = Context.ServerUris.GetString(serverIndex);
                if (!string.IsNullOrEmpty(uri)) {
                    WriteString("ServerUri", uri);
                    return;
                }
            }
            if (_serverMappings != null &&
                _serverMappings.Length > serverIndex) {
                serverIndex = _serverMappings[serverIndex];
            }
            if (serverIndex != 0) {
                WriteUInt32("ServerIndex", serverIndex);
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        private void WriteArray<T>(string property, IList<T> values,
            Action<T> writer) {
            if (values == null || values.Count == 0) {
                WriteNull(property);
            }
            else {
                PushArray(property, values.Count);
                foreach (var value in values) {
                    writer(value);
                }
                PopArray();
            }
        }

        /// <summary>
        /// Write null
        /// </summary>
        /// <param name="property"></param>
        private void WriteNull(string property) {
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteNull();
        }

        /// <summary>
        /// Push new object
        /// </summary>
        /// <param name="property"></param>
        private void PushObject(string property) {
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded");
            }
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteStartObject();
            _nestingLevel++;
        }

        /// <summary>
        /// Pop structure
        /// </summary>
        private void PopObject() {
            _writer.WriteEndObject();
            _nestingLevel--;
        }

        /// <summary>
        /// Push new array
        /// </summary>
        /// <param name="property"></param>
        /// <param name="count"></param>
        private void PushArray(string property, int count) {
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < count) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }
            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }
            _writer.WriteStartArray();
        }

        /// <summary>
        /// Pop array
        /// </summary>
        private void PopArray() {
            _writer.WriteEndArray();
        }

        private JsonWriter _writer;
        private readonly Stack<string> _namespaces;
        private ushort[] _namespaceMappings;
        private ushort[] _serverMappings;
        private uint _nestingLevel;
    }
}
