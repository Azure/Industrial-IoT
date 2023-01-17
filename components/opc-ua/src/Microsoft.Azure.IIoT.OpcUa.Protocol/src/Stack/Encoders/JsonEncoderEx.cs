// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Opc.Ua.Extensions;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Writes objects to a json
    /// </summary>
    public class JsonEncoderEx : IEncoder, IDisposable {

        /// <inheritdoc/>
        public EncodingType EncodingType => EncodingType.Json;

        /// <inheritdoc/>
        public IServiceMessageContext Context { get; }

        private readonly bool _ownedWriter;

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
        public bool UseUriEncoding { get; set; } = true;

        /// <summary>
        /// Encode using microsoft variant
        /// </summary>
        public bool UseAdvancedEncoding { get; set; } = false;

        /// <summary>
        /// Ignore null values
        /// </summary>
        public bool IgnoreNullValues { get; set; } = false;

        /// <summary>
        /// Ignore default primitive values
        /// </summary>
        public bool IgnoreDefaultValues { get; set; } = false;

        /// <summary>
        /// State of the writer
        /// </summary>
        public enum JsonEncoding {

            /// <summary>
            /// Start writing object (default)
            /// </summary>
            Object,

            /// <summary>
            /// Start writing array
            /// </summary>
            Array,

            /// <summary>
            /// Assume object or array already written
            /// </summary>
            Token
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="formatting"></param>
        /// <param name="leaveOpen"></param>
        public JsonEncoderEx(Stream stream, IServiceMessageContext context = null,
            JsonEncoding encoding = JsonEncoding.Object,
            Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None,
            bool leaveOpen = true) :
            this(new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: leaveOpen),
                context, encoding, formatting, leaveOpen) {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="formatting"></param>
        /// <param name="leaveOpen"></param>
        public JsonEncoderEx(TextWriter writer, IServiceMessageContext context = null,
            JsonEncoding encoding = JsonEncoding.Object,
            Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None,
            bool leaveOpen = true) :
            this(new JsonTextWriter(writer) {
                AutoCompleteOnClose = true,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatFormatHandling = FloatFormatHandling.String,
                Formatting = formatting,
                CloseOutput = !leaveOpen,
            }, context, encoding, true) {
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        /// <param name="encoding"></param>
        /// <param name="ownedWriter"></param>
        public JsonEncoderEx(JsonWriter writer, IServiceMessageContext context = null,
            JsonEncoding encoding = JsonEncoding.Object, bool ownedWriter = false) {
            _namespaces = new Stack<string>();
            Context = context ?? new ServiceMessageContext();
            _ownedWriter = ownedWriter;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _encoding = encoding;
            switch (encoding) {
                case JsonEncoding.Object:
                    _writer.WriteStartObject();
                    break;
                case JsonEncoding.Array:
                    _writer.WriteStartArray();
                    break;
            }
        }

        /// <summary>
        /// Completes writing
        /// </summary>
        public void Close() {
            if (_writer != null) {
                switch (_encoding) {
                    case JsonEncoding.Object:
                        _writer.WriteEndObject();
                        break;
                    case JsonEncoding.Array:
                        _writer.WriteEndArray();
                        break;
                }

                _writer.Flush();
                if (_ownedWriter) {
                    _writer.Close();
                }
                _writer = null;
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            Close();
        }

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
        public void PushNamespace(string namespaceUri) {
            _namespaces.Push(namespaceUri);
        }

        /// <inheritdoc/>
        public void PopNamespace() {
            _namespaces.Pop();
        }

        /// <inheritdoc/>
        public void WriteSByte(string property, sbyte value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByte(string property, byte value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt16(string property, short value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteUInt16(string property, ushort value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt32(string property, int value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteUInt32(string property, uint value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteInt64(string property, long value) {
            if (PreWriteValue(property, value)) {
                if (UseAdvancedEncoding) {
                    _writer.WriteValue(value);
                }
                else {
                    _writer.WriteValue(value.ToString());
                }
            }
        }

        /// <inheritdoc/>
        public void WriteUInt64(string property, ulong value) {
            if (PreWriteValue(property, value)) {
                if (UseAdvancedEncoding) {
                    _writer.WriteValue(value);
                }
                else {
                    _writer.WriteValue(value.ToString());
                }
            }
        }

        /// <inheritdoc/>
        public void WriteBoolean(string property, bool value) {
            if (PreWriteValue(property, value)) {
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteString(string property, string value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteFloat(string property, float value) {
            if (!string.IsNullOrEmpty(property)) {
                if (IgnoreDefaultValues && Math.Abs(value) < float.Epsilon) {
                    return;
                }
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteDouble(string property, double value) {
            if (!string.IsNullOrEmpty(property)) {
                if (IgnoreDefaultValues && Math.Abs(value) < double.Epsilon) {
                    return;
                }
                _writer.WritePropertyName(property);
            }
            _writer.WriteValue(value);
        }

        /// <inheritdoc/>
        public void WriteDateTime(string property, DateTime value) {
            if (value == default) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                _writer.WriteValue(value.ToOpcUaJsonEncodedTime());
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string property, Uuid value) {
            if (value == default) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteGuid(string property, Guid value) {
            if (value == default) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteByteString(string property, byte[] value) {
            if (value == null || value.Length == 0) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                // check the length.
                if (Context.MaxByteStringLength > 0 &&
                    Context.MaxByteStringLength < value.Length) {
                    throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
                }
                _writer.WriteValue(value);
            }
        }

        /// <inheritdoc/>
        public void WriteXmlElement(string property, XmlElement value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                if (!string.IsNullOrEmpty(property)) {
                    _writer.WritePropertyName(property);
                }
                if (PerformXmlSerialization || UseAdvancedEncoding) {
                    var json = JsonConvert.SerializeObject(value);
                    _writer.WriteRawValue(json);
                }
                else {
                    // Back compat to json encoding
                    var xml = value.OuterXml;
                    _writer.WriteValue(Encoding.UTF8.GetBytes(xml));
                }
            }
        }

        /// <inheritdoc/>
        public void WriteNodeId(string property, NodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(property);
            }
            else if (UseAdvancedEncoding) {
                if (UseUriEncoding || UseReversibleEncoding) {
                    WriteString(property, value.AsString(Context));
                }
                else {
                    WriteString(property, value.ToString());
                }
            }
            else {
                PushObject(property);
                if (value.IdType != IdType.Numeric) {
                    WriteByte("IdType", (byte)value.IdType);
                }
                switch (value.IdType) {
                    case IdType.Numeric:
                        WriteUInt32("Id", (uint)value.Identifier);
                        break;
                    case IdType.String:
                        WriteString("Id", (string)value.Identifier);
                        break;
                    case IdType.Guid:
                        WriteGuid("Id", (Guid)value.Identifier);
                        break;
                    case IdType.Opaque:
                        WriteByteString("Id", (byte[])value.Identifier);
                        break;
                }
                switch (value.NamespaceIndex) {
                    case 0:
                        // default namespace - nothing to do
                        break;
                    case 1:
                        // always as integer
                        WriteUInt16("Namespace", value.NamespaceIndex);
                        break;
                    default:
                        var namespaceUri = Context.NamespaceUris.GetString(value.NamespaceIndex);
                        if (namespaceUri != null && !UseReversibleEncoding) {
                            WriteString("Namespace", namespaceUri);
                        }
                        else {
                            WriteUInt16("Namespace", value.NamespaceIndex);
                        }
                        break;
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeId(string property, ExpandedNodeId value) {
            if (NodeId.IsNull(value)) {
                WriteNull(property);
            }
            else if (UseAdvancedEncoding) {
                if (UseUriEncoding || UseReversibleEncoding) {
                    WriteString(property, value.AsString(Context));
                }
                else {
                    WriteString(property, value.ToString());
                }
            }
            else {
                PushObject(property);
                if (value.IdType != IdType.Numeric) {
                    WriteByte("IdType", (byte)value.IdType);
                }
                switch (value.IdType) {
                    case IdType.Numeric:
                        WriteUInt32("Id", (uint)value.Identifier);
                        break;
                    case IdType.String:
                        WriteString("Id", (string)value.Identifier);
                        break;
                    case IdType.Guid:
                        WriteGuid("Id", (Guid)value.Identifier);
                        break;
                    case IdType.Opaque:
                        WriteByteString("Id", (byte[])value.Identifier);
                        break;
                }
                switch (value.NamespaceIndex) {
                    case 0:
                        // default namespace - nothing to do
                        break;
                    case 1:
                        // namespace 1 always as integer
                        WriteUInt16("Namespace", value.NamespaceIndex);
                        break;
                    default:
                        var namespaceUri = UseReversibleEncoding ?
                            null : Context.NamespaceUris.GetString(value.NamespaceIndex);
                        if (namespaceUri != null) {
                            WriteString("Namespace", namespaceUri);
                        }
                        else {
                            WriteUInt16("Namespace", value.NamespaceIndex);
                        }
                        break;
                }
                if (value.ServerIndex != 0) {
                    var serverUri = Context.ServerUris.GetString(value.ServerIndex);
                    if (serverUri != null) {
                        WriteString("ServerUri", serverUri);
                    }
                    else {
                        WriteUInt32("ServerUri", value.ServerIndex);
                    }
                }
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteStatusCode(string property, StatusCode value) {
            if (value == StatusCodes.Good) {
                WriteNull(property);
            }
            else {
                var symbol = string.Empty;
                if (!UseReversibleEncoding || UseAdvancedEncoding) {
                    symbol = StatusCode.LookupSymbolicId(value.CodeBits);
                }
                if (!UseReversibleEncoding || !string.IsNullOrEmpty(symbol)) {
                    PushObject(property);
                    WriteString("Symbol", symbol);
                    WriteUInt32("Code", value.Code);
                    PopObject();
                }
                else {
                    WriteUInt32(property, value.Code);
                }
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
                if (UseUriEncoding && UseAdvancedEncoding) {
                    WriteString(property, value.AsString(Context));
                }
                else {
                    // Back compat to json encoding
                    PushObject(property);
                    WriteString("Name", value.Name);
                    if (value.NamespaceIndex > 0) {
                        WriteUInt16("Uri", value.NamespaceIndex);
                    }
                    PopObject();
                }
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
            else if (UseReversibleEncoding) {
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

            var variant = value;
            if (UseAdvancedEncoding &&
                value.Value is Variant[] vararray &&
                value.TypeInfo.ValueRank == 1 &&
                vararray.Length > 0) {

                var type = vararray[0].TypeInfo?.BuiltInType;
                var rank = vararray[0].TypeInfo?.ValueRank;

                // TODO fails when different ranks are in use in array
                if (vararray.All(v =>
                    v.TypeInfo?.BuiltInType == type)) {
                    // Demote and encode as simple array
                    variant = new TypeInfo(type ?? BuiltInType.Null, 1)
                        .CreateVariant(vararray
                            .Select(v => v.Value)
                            .ToArray());
                }
            }

            var valueRank = variant.TypeInfo?.ValueRank ?? -1;
            var builtInType = variant.TypeInfo?.BuiltInType ?? BuiltInType.Null;

            if (UseReversibleEncoding) {
                PushObject(property);
                WriteBuiltInType("Type", builtInType);
                property = "Body";
            }

            if (!string.IsNullOrEmpty(property)) {
                _writer.WritePropertyName(property);
            }

            WriteVariantContents(variant.Value, valueRank, builtInType);

            if (UseReversibleEncoding) {
                PopObject();
            }
        }

        /// <inheritdoc/>
        public void WriteDataValue(string property, DataValue value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                if (value.StatusCode != StatusCodes.Good ||
                    value.SourceTimestamp != DateTime.MinValue ||
                    value.ServerTimestamp != DateTime.MinValue ||
                    value.SourcePicoseconds != 0 ||
                    value.ServerPicoseconds != 0) {
                    PushObject(property);
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
                    PopObject();
                }
                else {
                    // raw value
                    if (value.WrappedValue.TypeInfo != null &&
                        value.WrappedValue.TypeInfo.BuiltInType != BuiltInType.Null) {
                        WriteVariant(property, value.WrappedValue);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void WriteExtensionObject(string property, ExtensionObject value) {
            if (value == null) {
                WriteNull(property);
                return;
            }

            var encoding = value.Encoding;
            var typeId = value.TypeId;
            var body = value.Body;

            if (body is IEncodeable encodeable) {
                if (NodeId.IsNull(typeId)) {
                    typeId = encodeable.TypeId;
                    value.TypeId = typeId; // Also fix the extension object
                }
                if (!UseReversibleEncoding) {
                    PushObject(property);
                    encodeable.Encode(this);
                    PopObject();
                    return;
                }
                if (UseAdvancedEncoding) {
                    encoding = ExtensionObjectEncoding.Json;
                }
                switch (encoding) {
                    case ExtensionObjectEncoding.Binary:
                        body = encodeable.AsBinary(Context);
                        break;
                    case ExtensionObjectEncoding.Json:
                        body = encodeable; // Encode as json down below.
                        break;
                    case ExtensionObjectEncoding.EncodeableObject:
                    case ExtensionObjectEncoding.None:
                    case ExtensionObjectEncoding.Xml:
                        // Force xml
                        encoding = ExtensionObjectEncoding.Xml;
                        body = encodeable.AsXmlElement(Context);
                        break;
                    default:
                        throw ServiceResultException.Create(
                            StatusCodes.BadEncodingError,
                            "Unexpected encoding encountered while " +
                                $"encoding ExtensionObject:{value.Encoding}");
                }
            }
            else {
                if (NodeId.IsNull(typeId) && !UseAdvancedEncoding) {
                    throw ServiceResultException.Create(
                        StatusCodes.BadEncodingError,
                        "Cannot encode extension object without type id.");
                }
            }

            PushObject(property);
            WriteExpandedNodeId("TypeId", typeId);
            if (body != null) {
                switch (encoding) {
                    case ExtensionObjectEncoding.Xml:
                        WriteEncoding("Encoding", encoding);
                        WriteXmlElement("Body", body as XmlElement);
                        break;
                    case ExtensionObjectEncoding.Json:
                        WriteEncoding("Encoding", encoding);
                        if (body is EncodeableJToken jt) {
                            // Write encodeable token as json raw
                            body = jt.JToken;
                        }
                        else if (body is IEncodeable o) {
                            PushObject("Body");
                            o.Encode(this);
                            PopObject();
                            break;
                        }
                        PushObject("Body");
                        _writer.WritePropertyName(nameof(EncodeableJToken.JToken));
                        switch (body) {
                            case JToken token:
                                _writer.WriteRaw(token.ToString());
                                break;
                            case string json:
                                _writer.WriteRaw(json);
                                break;
                            case byte[] buffer:
                                _writer.WriteValue(buffer);
                                break;
                            default:
                                throw ServiceResultException.Create(
                                    StatusCodes.BadEncodingError,
                                    "Unexpected value encountered while " +
                                        $"encoding body:{body}");
                        }
                        PopObject();
                        break;
                    case ExtensionObjectEncoding.Binary:
                        WriteByteString("Body", body as byte[]);
                        break;
                }
            }
            PopObject();
        }

        /// <inheritdoc/>
        public void WriteEncodeable(string property, IEncodeable value, Type systemType) {
            WriteObject(property, value, v => v.Encode(this));
        }

        /// <inheritdoc/>
        public void WriteEnumerated(string property, Enum value) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                var numeric = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                if (UseReversibleEncoding) {
                    if (PreWriteValue(property, numeric)) {
                        _writer.WriteValue(numeric);
                    }
                }
                else {
                    WriteString(property, $"{value}_{numeric}");
                }
            }
        }

        /// <inheritdoc/>
        public void WriteBooleanArray(string property, IList<bool> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteSByteArray(string property, IList<sbyte> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteByteArray(string property, IList<byte> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteInt16Array(string property, IList<short> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteUInt16Array(string property, IList<ushort> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteInt32Array(string property, IList<int> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteUInt32Array(string property, IList<uint> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteInt64Array(string property, IList<long> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteUInt64Array(string property, IList<ulong> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteFloatArray(string property, IList<float> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteDoubleArray(string property, IList<double> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteStringArray(string property, IList<string> values) {
            WriteArray(property, values, _writer.WriteValue);
        }

        /// <inheritdoc/>
        public void WriteStringDictionary(string property,
            IEnumerable<KeyValuePair<string, string>> values) {
            WriteDictionary(property, values, (k, v) => WriteString(k, v));
        }

        /// <inheritdoc/>
        public void WriteDateTimeArray(string property, IList<DateTime> values) {
            WriteArray(property, values, v => WriteDateTime(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string property, IList<Uuid> values) {
            WriteArray(property, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteGuidArray(string property, IList<Guid> values) {
            WriteArray(property, values, v => WriteGuid(null, v));
        }

        /// <inheritdoc/>
        public void WriteByteStringArray(string property, IList<byte[]> values) {
            WriteArray(property, values, v => WriteByteString(null, v));
        }

        /// <inheritdoc/>
        public void WriteXmlElementArray(string property, IList<XmlElement> values) {
            WriteArray(property, values, v => WriteXmlElement(null, v));
        }

        /// <inheritdoc/>
        public void WriteNodeIdArray(string property, IList<NodeId> values) {
            WriteArray(property, values, v => WriteNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteExpandedNodeIdArray(string property, IList<ExpandedNodeId> values) {
            WriteArray(property, values, v => WriteExpandedNodeId(null, v));
        }

        /// <inheritdoc/>
        public void WriteStatusCodeArray(string property, IList<StatusCode> values) {
            WriteArray(property, values, v => WriteStatusCode(null, v));
        }

        /// <inheritdoc/>
        public void WriteDiagnosticInfoArray(string property, IList<DiagnosticInfo> values) {
            WriteArray(property, values, v => WriteDiagnosticInfo(null, v));
        }

        /// <inheritdoc/>
        public void WriteQualifiedNameArray(string property, IList<QualifiedName> values) {
            WriteArray(property, values, v => WriteQualifiedName(null, v));
        }

        /// <inheritdoc/>
        public void WriteLocalizedTextArray(string property, IList<LocalizedText> values) {
            WriteArray(property, values, v => WriteLocalizedText(null, v));
        }

        /// <inheritdoc/>
        public void WriteVariantArray(string property, IList<Variant> values) {
            WriteArray(property, values, v => WriteVariant(null, v));
        }

        /// <inheritdoc/>
        public void WriteDataValueArray(string property, IList<DataValue> values) {
            WriteArray(property, values, v => WriteDataValue(null, v));
        }

        /// <inheritdoc/>
        public void WriteExtensionObjectArray(string property, IList<ExtensionObject> values) {
            WriteArray(property, values, v => WriteExtensionObject(null, v));
        }

        /// <inheritdoc/>
        public void WriteEncodeableArray(string property, IList<IEncodeable> values, Type systemType) {
            WriteArray(property, values, v => WriteEncodeable(null, v, systemType));
        }

        /// <inheritdoc/>
        public void WriteDataSet(string property, DataSet dataSet) {
            if (dataSet == null) {
                WriteNull(property);
                return;
            }
            var useUriEncoding = UseUriEncoding;
            var useReversibleEncoding = UseReversibleEncoding;
            try {
                var fieldContentMask = dataSet.DataSetFieldContentMask;
                if ((fieldContentMask & (uint)DataSetFieldContentMask.RawData) != 0) {
                    //
                    // If the DataSetFieldContentMask results in a RawData representation,
                    // the field value is a Variant encoded using the non-reversible OPC UA
                    // JSON Data Encoding defined in OPC 10000-6
                    //
                    UseUriEncoding = true;
                    UseReversibleEncoding = false;
                    WriteDictionary(property, dataSet, (k, v) => WriteVariant(k, v.WrappedValue));
                }
                else if (fieldContentMask == 0) {
                    //
                    // If the DataSetFieldContentMask results in a Variant representation,
                    // the field value is encoded as a Variant encoded using the reversible
                    // OPC UA JSON Data Encoding defined in OPC 10000-6.
                    //
                    UseUriEncoding = false;
                    UseReversibleEncoding = true;
                    WriteDictionary(property, dataSet, (k, v) => WriteVariant(k, v.WrappedValue));
                }
                else {
                    //
                    // If the DataSetFieldContentMask results in a DataValue representation,
                    // the field value is a DataValue encoded using the non-reversible OPC UA
                    // JSON Data Encoding or reversible depending on encoder configuration.
                    //
                    WriteDictionary(property, dataSet, (k, value) => {
                        PushObject(k);
                        try {
                            WriteVariant("Value", value.WrappedValue);
                            if ((fieldContentMask & (uint)DataSetFieldContentMask.StatusCode) != 0) {
                                WriteStatusCode("StatusCode", value.StatusCode);
                            }
                            if ((fieldContentMask & (uint)DataSetFieldContentMask.SourceTimestamp) != 0) {
                                WriteDateTime("SourceTimestamp", value.SourceTimestamp);
                                if ((fieldContentMask & (uint)DataSetFieldContentMask.SourcePicoSeconds) != 0) {
                                    WriteUInt16("SourcePicoseconds", value.SourcePicoseconds);
                                }
                            }
                            if ((fieldContentMask & (uint)DataSetFieldContentMask.ServerTimestamp) != 0) {
                                WriteDateTime("ServerTimestamp", value.ServerTimestamp);
                                if ((fieldContentMask & (uint)DataSetFieldContentMask.ServerPicoSeconds) != 0) {
                                    WriteUInt16("ServerPicoseconds", value.ServerPicoseconds);
                                }
                            }
                        }
                        finally {
                            PopObject();
                        }
                    });
                }
            }
            finally {
                UseUriEncoding = useUriEncoding;
                UseReversibleEncoding = useReversibleEncoding;
            }
        }

        /// <inheritdoc/>
        public void WriteObjectArray(string property, IList<object> values) {
            PushArray(property, values?.Count ?? 0);
            if (values != null) {
                foreach (var value in values) {
                    WriteVariant("Variant", new Variant(value));
                }
            }
            PopArray();
        }

        /// <inheritdoc/>
        public void WriteEnumeratedArray(string property, Array values, Type enumType) {
            if (values == null) {
                WriteNull(property);
            }
            else {
                PushArray(property, values.Length);
                // encode each element in the array.
                if (enumType.IsEnum) {
                    foreach (Enum value in values) {
                        WriteEnumerated(null, value);
                    }
                }
                else if (enumType == typeof(int)) {
                    foreach (int value in values) {
                        WriteInt32(null, value);
                    }
                }
                else {
                    throw new ArgumentException("Not an enum type", nameof(enumType));
                }
                PopArray();
            }
        }

        /// <summary>
        /// Writes the contents of an Variant to the stream.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="valueRank"></param>
        /// <param name="builtInType"></param>
        private void WriteVariantContents(object value, int valueRank,
            BuiltInType builtInType) {

            // Handle special value ranks
            if (valueRank <= -2 || valueRank == 0) {
                if (valueRank < -3) {
                    throw new ServiceResultException(StatusCodes.BadEncodingError,
                        $"Bad variant: Value rank '{valueRank}' is invalid.");
                }
                // Cannot deduce rank - write null
                if (value == null) {
                    WriteNull(null);
                    return;
                }
                // Handle one or more (0), Any (-2) or scalar or one dimension (-3)
                if (value.GetType().IsArray) {
                    var rank = value.GetType().GetArrayRank();
                    if (valueRank == -3 && rank != 1) {
                        throw new ServiceResultException(StatusCodes.BadEncodingError,
                            $"Bad variant: Scalar or one dimension with matrix value.");
                    }
                    // Write as array or matrix
                    valueRank = rank;
                }
                else {
                    if (valueRank == 0) {
                        throw new ServiceResultException(StatusCodes.BadEncodingError,
                            $"Bad variant: One or more dimension rank with scalar value.");
                    }
                    // Force write as scalar
                    valueRank = -1;
                }
            }

            // write scalar.
            if (valueRank == -1) {
                switch (builtInType) {
                    case BuiltInType.Null:
                        WriteNull(null);
                        return;
                    case BuiltInType.Boolean:
                        WriteBoolean(null, ToTypedScalar<bool>(value));
                        return;
                    case BuiltInType.SByte:
                        WriteSByte(null, ToTypedScalar<sbyte>(value));
                        return;
                    case BuiltInType.Byte:
                        WriteByte(null, ToTypedScalar<byte>(value));
                        return;
                    case BuiltInType.Int16:
                        WriteInt16(null, ToTypedScalar<short>(value));
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16(null, ToTypedScalar<ushort>(value));
                        return;
                    case BuiltInType.Int32:
                        WriteInt32(null, ToTypedScalar<int>(value));
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32(null, ToTypedScalar<uint>(value));
                        return;
                    case BuiltInType.Int64:
                        WriteInt64(null, ToTypedScalar<long>(value));
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64(null, ToTypedScalar<ulong>(value));
                        return;
                    case BuiltInType.Float:
                        WriteFloat(null, ToTypedScalar<float>(value));
                        return;
                    case BuiltInType.Double:
                        WriteDouble(null, ToTypedScalar<double>(value));
                        return;
                    case BuiltInType.String:
                        WriteString(null, ToTypedScalar<string>(value));
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTime(null, ToTypedScalar<DateTime>(value));
                        return;
                    case BuiltInType.Guid:
                        WriteGuid(null, ToTypedScalar<Uuid>(value));
                        return;
                    case BuiltInType.ByteString:
                        WriteByteString(null, ToTypedScalar<byte[]>(value ?? Array.Empty<byte>()));
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElement(null, ToTypedScalar<XmlElement>(value));
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeId(null, ToTypedScalar<NodeId>(value));
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeId(null, ToTypedScalar<ExpandedNodeId>(value));
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCode(null, ToTypedScalar<StatusCode>(value));
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedName(null, ToTypedScalar<QualifiedName>(value));
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedText(null, ToTypedScalar<LocalizedText>(value));
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObject(null, ToTypedScalar<ExtensionObject>(value));
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValue(null, ToTypedScalar<DataValue>(value));
                        return;
                    case BuiltInType.Enumeration:
                        WriteInt32(null, ToTypedScalar<int>(value));
                        return;
                    case BuiltInType.Number:
                    case BuiltInType.Integer:
                    case BuiltInType.UInteger:
                    case BuiltInType.Variant:
                        throw ServiceResultException.Create(StatusCodes.BadEncodingError,
                            "Bad variant: Unexpected type encountered while encoding " +
                            value.GetType());
                }
            }

            // write array.
            if (valueRank == 1) {
                switch (builtInType) {
                    case BuiltInType.Null:
                        WriteNull(null);
                        return;
                    case BuiltInType.Boolean:
                        WriteBooleanArray(null, ToTypedArray<bool>(value));
                        return;
                    case BuiltInType.SByte:
                        WriteSByteArray(null, ToTypedArray<sbyte>(value));
                        return;
                    case BuiltInType.Byte:
                        WriteByteArray(null, ToTypedArray<byte>(value));
                        return;
                    case BuiltInType.Int16:
                        WriteInt16Array(null, ToTypedArray<short>(value));
                        return;
                    case BuiltInType.UInt16:
                        WriteUInt16Array(null, ToTypedArray<ushort>(value));
                        return;
                    case BuiltInType.Int32:
                        WriteInt32Array(null, ToTypedArray<int>(value));
                        return;
                    case BuiltInType.UInt32:
                        WriteUInt32Array(null, ToTypedArray<uint>(value));
                        return;
                    case BuiltInType.Int64:
                        WriteInt64Array(null, ToTypedArray<long>(value));
                        return;
                    case BuiltInType.UInt64:
                        WriteUInt64Array(null, ToTypedArray<ulong>(value));
                        return;
                    case BuiltInType.Float:
                        WriteFloatArray(null, ToTypedArray<float>(value));
                        return;
                    case BuiltInType.Double:
                        WriteDoubleArray(null, ToTypedArray<double>(value));
                        return;
                    case BuiltInType.String:
                        WriteStringArray(null, ToTypedArray<string>(value));
                        return;
                    case BuiltInType.DateTime:
                        WriteDateTimeArray(null, ToTypedArray<DateTime>(value));
                        return;
                    case BuiltInType.Guid:
                        WriteGuidArray(null, ToTypedArray<Uuid>(value));
                        return;
                    case BuiltInType.ByteString:
                        WriteByteStringArray(null, ToTypedArray<byte[]>(value));
                        return;
                    case BuiltInType.XmlElement:
                        WriteXmlElementArray(null, ToTypedArray<XmlElement>(value));
                        return;
                    case BuiltInType.NodeId:
                        WriteNodeIdArray(null, ToTypedArray<NodeId>(value));
                        return;
                    case BuiltInType.ExpandedNodeId:
                        WriteExpandedNodeIdArray(null, ToTypedArray<ExpandedNodeId>(value));
                        return;
                    case BuiltInType.StatusCode:
                        WriteStatusCodeArray(null, ToTypedArray<StatusCode>(value));
                        return;
                    case BuiltInType.QualifiedName:
                        WriteQualifiedNameArray(null, ToTypedArray<QualifiedName>(value));
                        return;
                    case BuiltInType.LocalizedText:
                        WriteLocalizedTextArray(null, ToTypedArray<LocalizedText>(value));
                        return;
                    case BuiltInType.ExtensionObject:
                        WriteExtensionObjectArray(null, ToTypedArray<ExtensionObject>(value));
                        return;
                    case BuiltInType.DataValue:
                        WriteDataValueArray(null, ToTypedArray<DataValue>(value));
                        return;
                    case BuiltInType.Enumeration:
                        var enums = value as Enum[];
                        var values = new string[enums.Length];

                        for (var index = 0; index < enums.Length; index++) {
                            var text = enums[index].ToString();
                            text += "_";
                            text += ((int)(object)enums[index]).ToString(CultureInfo.InvariantCulture);
                            values[index] = text;
                        }

                        WriteStringArray(null, values);
                        return;
                    case BuiltInType.Number:
                    case BuiltInType.UInteger:
                    case BuiltInType.Integer:
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
                            "Bad variant: Unexpected type encountered while encoding an array" +
                            $" of Variants: {value.GetType()}");
                }
            }

            if (valueRank > 1) {
                // Write matrix
                if (value == null) {
                    WriteNull(null);
                    return;
                }

                // TODO: JSON array encoding only for
                // non reversible encoding, otherwise
                // flatten array and add Dimension.
                // if (!UseReversibleEncoding) {
                if (value is Matrix matrix) {
                    var index = 0;
                    WriteMatrix(matrix, 0, ref index, builtInType);
                    return;
                }
            }

            // Should never happen.
            throw new ServiceResultException(StatusCodes.BadEncodingError,
                $"Bad variant: Type '{value.GetType().FullName}' is not allowed in Variant.");
        }

        /// <summary>
        /// Cast to array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private IList<T> ToTypedArray<T>(object value) {
            if (value == null) {
                return default;
            }
            if (value is T[] t) {
                return t;
            }
            if (!(value is Array arr)) {
                return ToTypedScalar<T>(value).YieldReturn().ToArray();
            }
            if (arr.Length == 0) {
                return new List<T>();
            }
            if (arr.Length == 1) {
                value = arr.GetValue(0);
                if (value.GetType().IsArray) {
                    // Recursively unpack an array in array if needed
                    return ToTypedArray<T>(value);
                }
            }
            try {
                return arr.Cast<T>().ToArray();
            }
            catch (Exception ex) {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Bad variant: Value '{value}' of type '{value.GetType().FullName}' is not " +
                    $"a one dimensional array of type '{typeof(T).GetType().FullName}'.", ex);
            }
        }

        /// <summary>
        /// Cast to array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T ToTypedScalar<T>(object value) {
            try {
                if (value == null) {
                    return default;
                }
                if (value is T t) {
                    return t;
                }
                return (T)value;
            }
            catch (Exception ex) {
                throw new ServiceResultException(StatusCodes.BadEncodingError,
                    $"Bad variant: Value '{value}' of type '{value.GetType().FullName}' is not " +
                    $"a scalar of type '{typeof(T).GetType().FullName}'.", ex);
            }
        }

        /// <summary>
        /// Write multi dimensional array
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="dim"></param>
        /// <param name="index"></param>
        /// <param name="builtInType"></param>
        private void WriteMatrix(Matrix matrix, int dim, ref int index,
            BuiltInType builtInType) {
            var arrayLen = matrix.Dimensions[dim];
            if (dim == matrix.Dimensions.Length - 1) {
                // Create a slice of values for the top dimension
                var copy = Array.CreateInstance(
                    matrix.Elements.GetType().GetElementType(), arrayLen);
                Array.Copy(matrix.Elements, index, copy, 0, arrayLen);
                // Write slice as value rank
                WriteVariantContents(copy, 1, builtInType);
                index += arrayLen;
            }
            else {
                PushArray(null, arrayLen);
                for (var i = 0; i < arrayLen; i++) {
                    WriteMatrix(matrix, dim + 1, ref index, builtInType);
                }
                PopArray();
            }
        }

        /// <summary>
        /// Write multi dimensional array in structure.
        /// </summary>
        private void WriteStructureMatrix(
            string fieldName,
            Matrix matrix,
            int dim,
            ref int index,
            TypeInfo typeInfo) {
            // check the nesting level for avoiding a stack overflow.
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }

            _nestingLevel++;

            try {
                var arrayLen = matrix.Dimensions[dim];
                if (dim == matrix.Dimensions.Length - 1) {
                    // Create a slice of values for the top dimension
                    var copy = Array.CreateInstance(
                        matrix.Elements.GetType().GetElementType(), arrayLen);
                    Array.Copy(matrix.Elements, index, copy, 0, arrayLen);
                    // Write slice as value rank

                    WriteVariantContents(copy, 1, typeInfo.BuiltInType);
                    index += arrayLen;
                }
                else {
                    PushArray(fieldName, arrayLen);
                    for (var i = 0; i < arrayLen; i++) {
                        WriteStructureMatrix(null, matrix, dim + 1, ref index, typeInfo);
                    }
                    PopArray();
                }
            }
            finally {
                _nestingLevel--;
            }
        }


        /// <summary>
        /// Write type
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        private void WriteBuiltInType(string property, BuiltInType type) {
            if (UseAdvancedEncoding) {
                WriteString(property, type.ToString());
            }
            else {
                WriteByte(property, (byte)type);
            }
        }

        /// <summary>
        /// Write encoding
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        private void WriteEncoding(string property, ExtensionObjectEncoding type) {
            if (UseAdvancedEncoding) {
                WriteString(property, type.ToString());
            }
            else {
                WriteByte(property, (byte)type);
            }
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
        /// Encode an array according to its valueRank and BuiltInType
        /// </summary>
        public void WriteArray(string fieldName, object array, int valueRank, BuiltInType builtInType) {
            // write array.
            if (valueRank == ValueRanks.OneDimension) {
                switch (builtInType) {
                    case BuiltInType.Boolean: { WriteBooleanArray(fieldName, (bool[])array); return; }
                    case BuiltInType.SByte: { WriteSByteArray(fieldName, (sbyte[])array); return; }
                    case BuiltInType.Byte: { WriteByteArray(fieldName, (byte[])array); return; }
                    case BuiltInType.Int16: { WriteInt16Array(fieldName, (short[])array); return; }
                    case BuiltInType.UInt16: { WriteUInt16Array(fieldName, (ushort[])array); return; }
                    case BuiltInType.Int32: { WriteInt32Array(fieldName, (int[])array); return; }
                    case BuiltInType.UInt32: { WriteUInt32Array(fieldName, (uint[])array); return; }
                    case BuiltInType.Int64: { WriteInt64Array(fieldName, (long[])array); return; }
                    case BuiltInType.UInt64: { WriteUInt64Array(fieldName, (ulong[])array); return; }
                    case BuiltInType.Float: { WriteFloatArray(fieldName, (float[])array); return; }
                    case BuiltInType.Double: { WriteDoubleArray(fieldName, (double[])array); return; }
                    case BuiltInType.String: { WriteStringArray(fieldName, (string[])array); return; }
                    case BuiltInType.DateTime: { WriteDateTimeArray(fieldName, (DateTime[])array); return; }
                    case BuiltInType.Guid: { WriteGuidArray(fieldName, (Uuid[])array); return; }
                    case BuiltInType.ByteString: { WriteByteStringArray(fieldName, (byte[][])array); return; }
                    case BuiltInType.XmlElement: { WriteXmlElementArray(fieldName, (XmlElement[])array); return; }
                    case BuiltInType.NodeId: { WriteNodeIdArray(fieldName, (NodeId[])array); return; }
                    case BuiltInType.ExpandedNodeId: { WriteExpandedNodeIdArray(fieldName, (ExpandedNodeId[])array); return; }
                    case BuiltInType.StatusCode: { WriteStatusCodeArray(fieldName, (StatusCode[])array); return; }
                    case BuiltInType.QualifiedName: { WriteQualifiedNameArray(fieldName, (QualifiedName[])array); return; }
                    case BuiltInType.LocalizedText: { WriteLocalizedTextArray(fieldName, (LocalizedText[])array); return; }
                    case BuiltInType.ExtensionObject: { WriteExtensionObjectArray(fieldName, (ExtensionObject[])array); return; }
                    case BuiltInType.DataValue: { WriteDataValueArray(fieldName, (DataValue[])array); return; }
                    case BuiltInType.DiagnosticInfo: { WriteDiagnosticInfoArray(fieldName, (DiagnosticInfo[])array); return; }
                    case BuiltInType.Enumeration: {
                            Array enumArray = array as Array;
                            if (enumArray == null) {
                                throw ServiceResultException.Create(
                                    StatusCodes.BadEncodingError,
                                    "Unexpected non Array type encountered while encoding an array of enumeration.");
                            }
                            WriteEnumeratedArray(fieldName, enumArray, enumArray.GetType().GetElementType());
                            return;
                        }

                    case BuiltInType.Variant: {
                            Variant[] variants = array as Variant[];

                            if (variants != null) {
                                WriteVariantArray(fieldName, variants);
                                return;
                            }

                            object[] objects = array as object[];

                            if (objects != null) {
                                WriteObjectArray(fieldName, objects);
                                return;
                            }

                            throw ServiceResultException.Create(
                                StatusCodes.BadEncodingError,
                                "Unexpected type encountered while encoding an array of Variants: {0}",
                                array.GetType());
                        }
                }
            }
            // write matrix.
            else if (valueRank > ValueRanks.OneDimension) {
                Matrix matrix = array as Matrix;
                if (matrix != null) {
                    int index = 0;
                    WriteStructureMatrix(fieldName, matrix, 0, ref index, matrix.TypeInfo);

                    return;
                }
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        internal void WriteArray<T>(string property, IList<T> values,
            Action<T> writer) {
            if (values == null) {
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

        /// <inheritdoc/>
        internal void WriteObject<T>(string property, T value, Action<T> writer) {
            if (value == null) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                if (value != null) {
                    writer(value);
                }
                PopObject();
            }
        }

        /// <summary>
        /// Write array to stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        private void WriteDictionary<T>(string property,
            IEnumerable<KeyValuePair<string, T>> values, Action<string, T> writer) {
            if (values == null) {
                WriteNull(property);
            }
            else {
                PushObject(property);
                foreach (var value in values) {
                    writer(value.Key, value.Value);
                }
                PopObject();
            }
        }

        /// <summary>
        /// Check whether to write the simple value.  If so
        /// andthis is not called in the context of array
        /// write (property == null) write property.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        /// <returns>true if should write value.</returns>
        private bool PreWriteValue<T>(string property, T value) where T : struct {
            if (!string.IsNullOrEmpty(property)) {
                if (IgnoreDefaultValues && EqualityComparer<T>.Default.Equals(value, default)) {
                    return false;
                }
                _writer.WritePropertyName(property);
            }
            return true;
        }

        /// <summary>
        /// Write null
        /// </summary>
        /// <param name="property"></param>
        public void WriteNull(string property) {
            if (!string.IsNullOrEmpty(property)) {
                if (IgnoreNullValues || IgnoreDefaultValues) {
                    // only skip null if not in array context.
                    return;
                }
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
        private readonly JsonEncoding _encoding;
        private readonly Stack<string> _namespaces;
        private ushort[] _namespaceMappings;
        private ushort[] _serverMappings;
        private uint _nestingLevel;
    }
}
