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
        public bool PerformXmlSerialization { get; set; }

        /// <summary>
        /// Encode nodes as uri
        /// </summary>
        public bool UseNodeUriEncoding { get; set; }

        /// <summary>
        /// Create encoder
        /// </summary>
        public JsonEncoderEx(ServiceMessageContext context,
            TextWriter writer = null) {
            _namespaces = new Stack<string>();
            Context = context;
            if (writer == null) {
                _destination = new MemoryStream();
                writer = new StreamWriter(_destination, new UTF8Encoding(false));
            }
            _writer = new JsonTextWriter(writer) {
                AutoCompleteOnClose = true,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            _writer.WriteStartObject();
        }

        /// <summary>
        /// Completes writing and returns the XML text.
        /// </summary>
        public string CloseAndReturnText() {
            Close();
            if (_destination != null) {
                return Encoding.UTF8.GetString(_destination.ToArray());
            }
            return string.Empty;
        }

        /// <summary>
        /// Completes writing and returns the text length.
        /// </summary>
        public void Close() {
            if (_writer != null) {
                _writer.Close();
                _writer = null;
            }
        }

        /// <summary>
        /// Encodes a message with its header.
        /// </summary>
        public void EncodeMessage(IEncodeable message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }

            // convert the namespace uri to an index.
            var typeId = ExpandedNodeId.ToNodeId(message.TypeId, Context.NamespaceUris);

            // write the type id.
            WriteNodeId("TypeId", typeId);

            // write the message.
            WriteEncodeable("Body", message, message.GetType());
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
        public void WriteSByte(string fieldName, sbyte value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteByte(string fieldName, byte value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt16(string fieldName, short value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt16(string fieldName, ushort value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt32(string fieldName, int value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt32(string fieldName, uint value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteInt64(string fieldName, long value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteUInt64(string fieldName, ulong value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteString(string fieldName, string value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteBoolean(string fieldName, bool value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteFloat(string fieldName, float value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
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
        public void WriteDouble(string fieldName, double value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
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
        public void WriteDateTime(string fieldName, DateTime value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
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
        public void WriteGuid(string fieldName, Uuid value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            if (value == Uuid.Empty) {
                _writer.WriteNull();
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string fieldName, Guid value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            if (value == Guid.Empty) {
                _writer.WriteNull();
            }
            else {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByteString(string fieldName, byte[] value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
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
        public void WriteXmlElement(string fieldName, XmlElement value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            if (value == null) {
                _writer.WriteNull();
            }
            else if (PerformXmlSerialization) {
                _writer.WriteValue(value);
            }
            else {
                // Back compat to json encoding
                var xml = value.OuterXml;
                _writer.WriteValue(Encoding.UTF8.GetBytes(xml));
            }
        }

        /// <inheritdoc/>
        public void WriteNodeId(string fieldName, NodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding) {
                WriteString(fieldName, value.ToString());
            }
            else if (UseNodeUriEncoding) {
                WriteString(fieldName, value.AsString(Context));
            }
            else {
                // Back compat to json encoding
                PushStructure(fieldName);
                _writer.WritePropertyName("Id");
                _writer.WriteValue(new NodeId(value.Identifier, 0).ToString());
                WriteNamespaceIndex(value.NamespaceIndex);
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string fieldName, ExpandedNodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding) {
                WriteString(fieldName, value.ToString());
            }
            else if (UseNodeUriEncoding) {
                WriteString(fieldName, value.AsString(Context));
            }
            else {
                // Back compat to json encoding
                PushStructure(fieldName);
                _writer.WritePropertyName("Id");
                _writer.WriteValue(new NodeId(value.Identifier, 0).ToString());
                WriteNamespaceIndex(value.NamespaceIndex);
                WriteServerIndex(value.ServerIndex);
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string fieldName, StatusCode value) {
            if (value == StatusCodes.Good) {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding) {
                WriteUInt32(fieldName, value.Code);
            }
            else {
                PushStructure(fieldName);
                WriteUInt32("Code", value.Code);
                WriteString("Symbol", StatusCode.LookupSymbolicId(value.CodeBits));
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfo(string fieldName, DiagnosticInfo value) {
            if (value == null) {
                WriteNull(fieldName);
            }
            else {
                PushStructure(fieldName);
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
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteQualifiedName(string fieldName, QualifiedName value) {
            if (QualifiedName.IsNull(value)) {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding) {
                PushStructure(fieldName);
                WriteString("Name", value.Name);
                if (value.NamespaceIndex > 0) {
                    WriteUInt16("Uri", value.NamespaceIndex);
                }
                PopStructure();
            }
            else {
                PushStructure(fieldName);
                WriteString("Name", value.Name);
                WriteNamespaceIndex(value.NamespaceIndex);
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteLocalizedText(string fieldName, LocalizedText value) {
            if (LocalizedText.IsNullOrEmpty(value)) {
                WriteNull(fieldName);
            }
            else if (UseReversibleEncoding) {
                PushStructure(fieldName);
                WriteString("Text", value.Text);
                if (!string.IsNullOrEmpty(value.Locale)) {
                    WriteString("Locale", value.Locale);
                }
                PopStructure();
            }
            else {
                WriteString(fieldName, value.Text);
            }
        }

        /// <inheritdoc/>
        public void WriteVariant(string fieldName, Variant value) {
            if (Variant.Null == value) {
                WriteNull(fieldName);
            }
            else {
                var isNull = (value.TypeInfo == null ||
                    value.TypeInfo.BuiltInType == BuiltInType.Null ||
                    value.Value == null);

                if (UseReversibleEncoding && !isNull) {
                    PushStructure(fieldName);
                    WriteByte("Type", (byte)value.TypeInfo.BuiltInType);
                    fieldName = "Body";
                }

                if (!string.IsNullOrEmpty(fieldName)) {
                    _writer.WritePropertyName(fieldName);
                }
                WriteVariantContents(value.Value, value.TypeInfo);

                if (UseReversibleEncoding && !isNull) {
                    if (value.Value is Matrix matrix) {
                        WriteInt32Array("Dimensions", matrix.Dimensions);
                    }
                    PopStructure();
                }
            }
        }

        /// <inheritdoc/>
        public void WriteDataValue(string fieldName, DataValue value) {
            if (value == null) {
                WriteNull(fieldName);
            }
            else {
                PushStructure(fieldName);
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
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string fieldName, ExtensionObject value) {
            if (value == null || value.Encoding == ExtensionObjectEncoding.None) {
                WriteNull(fieldName);
            }
            else {
                PushStructure(fieldName);
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
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string fieldName, IEncodeable value, Type systemType) {
            if (value == null) {
                WriteNull(fieldName);
            }
            else {
                PushStructure(fieldName);
                if (value != null) {
                    value.Encode(this);
                }
                PopStructure();
            }
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string fieldName, Enum value) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
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
        public void WriteBooleanArray(string fieldName, IList<bool> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteSByteArray(string fieldName, IList<sbyte> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteByteArray(string fieldName, IList<byte> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt16Array(string fieldName, IList<short> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt16Array(string fieldName, IList<ushort> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt32Array(string fieldName, IList<int> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt32Array(string fieldName, IList<uint> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteInt64Array(string fieldName, IList<long> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteUInt64Array(string fieldName, IList<ulong> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteFloatArray(string fieldName, IList<float> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteDoubleArray(string fieldName, IList<double> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteStringArray(string fieldName, IList<string> values) =>
            WriteArray(fieldName, values, _writer.WriteValue);

        /// <inheritdoc/>
        public void WriteDateTimeArray(string fieldName, IList<DateTime> values) =>
            WriteArray(fieldName, values, v => WriteDateTime(null, v));

        /// <inheritdoc/>
        public void WriteGuidArray(string fieldName, IList<Uuid> values) =>
            WriteArray(fieldName, values, v => WriteGuid(null, v));

        /// <inheritdoc/>
        public void WriteGuidArray(string fieldName, IList<Guid> values) =>
            WriteArray(fieldName, values, v => WriteGuid(null, v));

        /// <inheritdoc/>
        public void WriteByteStringArray(string fieldName, IList<byte[]> values) =>
            WriteArray(fieldName, values, v => WriteByteString(null, v));

        /// <inheritdoc/>
        public void WriteXmlElementArray(string fieldName, IList<XmlElement> values) =>
            WriteArray(fieldName, values, v => WriteXmlElement(null, v));

        /// <inheritdoc/>
        public void WriteNodeIdArray(string fieldName, IList<NodeId> values) =>
            WriteArray(fieldName, values, v => WriteNodeId(null, v));

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string fieldName, IList<ExpandedNodeId> values) =>
            WriteArray(fieldName, values, v => WriteExpandedNodeId(null, v));

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string fieldName, IList<StatusCode> values) =>
            WriteArray(fieldName, values, v => WriteStatusCode(null, v));

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string fieldName, IList<DiagnosticInfo> values) =>
            WriteArray(fieldName, values, v => WriteDiagnosticInfo(null, v));

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string fieldName, IList<QualifiedName> values) =>
            WriteArray(fieldName, values, v => WriteQualifiedName(null, v));

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string fieldName, IList<LocalizedText> values) =>
            WriteArray(fieldName, values, v => WriteLocalizedText(null, v));

        /// <inheritdoc/>
        public void WriteVariantArray(string fieldName, IList<Variant> values) {
            WriteArray(fieldName, values, v => {
                if (v == Variant.Null) {
                    _writer.WriteNull();
                }
                else {
                    WriteVariant(null, v);
                }
            });
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string fieldName, IList<DataValue> values) =>
            WriteArray(fieldName, values, v => WriteDataValue(null, v));

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string fieldName, IList<ExtensionObject> values) =>
            WriteArray(fieldName, values, v => WriteExtensionObject(null, v));

        /// <inheritdoc/>
        public void WriteEncodeableArray(string fieldName, IList<IEncodeable> values,
            Type systemType) =>
            WriteArray(fieldName, values, v => WriteEncodeable(null, v, systemType));

        /// <inheritdoc/>
        public void WriteObjectArray(string fieldName, IList<object> values) {
            PushArray(fieldName, values?.Count ?? 0);
            if (values != null) {
                for (var ii = 0; ii < values.Count; ii++) {
                    WriteVariant("Variant", new Variant(values[ii]));
                }
            }
            PopArray();
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string fieldName, Array values, Type systemType) {
            if (values == null || values.Length == 0) {
                WriteNull(fieldName);
            }
            else {
                PushArray(fieldName, values.Length);
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
        private void WriteMatrix(string fieldName, Matrix value) {
            PushStructure(fieldName);
            WriteVariant("Matrix", new Variant(value.Elements,
                new TypeInfo(value.TypeInfo.BuiltInType, ValueRanks.OneDimension)));
            WriteInt32Array("Dimensions", value.Dimensions);
            PopStructure();
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
        /// <param name="fieldName"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        private void WriteArray<T>(string fieldName, IList<T> values,
            Action<T> writer) {
            if (values == null || values.Count == 0) {
                WriteNull(fieldName);
            }
            else {
                PushArray(fieldName, values.Count);
                foreach (var value in values) {
                    writer(value);
                }
                PopArray();
            }
        }

        /// <summary>
        /// Write null
        /// </summary>
        /// <param name="fieldName"></param>
        private void WriteNull(string fieldName) {
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteNull();
        }

        /// <summary>
        /// Push new object
        /// </summary>
        /// <param name="fieldName"></param>
        private void PushStructure(string fieldName) {
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(StatusCodes.BadEncodingLimitsExceeded,
                    $"Maximum nesting level of {Context.MaxEncodingNestingLevels} was exceeded");
            }
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteStartObject();
            _nestingLevel++;
        }

        /// <summary>
        /// Pop structure
        /// </summary>
        private void PopStructure() {
            _writer.WriteEndObject();
            _nestingLevel--;
        }

        /// <summary>
        /// Push new array
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="count"></param>
        private void PushArray(string fieldName, int count) {
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < count) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }
            if (!string.IsNullOrEmpty(fieldName)) {
                _writer.WritePropertyName(fieldName);
            }
            _writer.WriteStartArray();
        }

        /// <summary>
        /// Pop array
        /// </summary>
        private void PopArray() {
            _writer.WriteEndArray();
        }

        private MemoryStream _destination;
        private JsonTextWriter _writer;
        private readonly Stack<string> _namespaces;
        private ushort[] _namespaceMappings;
        private ushort[] _serverMappings;
        private uint _nestingLevel;
    }
}
