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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Reads objects from reader or string
    /// </summary>
    public class JsonDecoderEx : IDecoder, IDisposable {

        /// <inheritdoc/>
        public EncodingType EncodingType => EncodingType.Json;

        /// <inheritdoc/>
        public ServiceMessageContext Context { get; }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="json"></param>
        public JsonDecoderEx(ServiceMessageContext context, string json) :
            this(context, new JsonTextReader(new StringReader(json))) {
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        public JsonDecoderEx(ServiceMessageContext context, Stream stream) :
            this(context, new JsonTextReader(new StreamReader(stream))) {
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reader"></param>
        public JsonDecoderEx(ServiceMessageContext context, JsonReader reader) {
            Context = context;
            using (var loader = new JsonLoader(reader)) {
                var root = JToken.ReadFrom(loader,
                    new JsonLoadSettings {
                        CommentHandling = CommentHandling.Ignore,
                        LineInfoHandling = LineInfoHandling.Ignore
                    });
                // Need to parse the entire document - TODO: Handle arrays
                _stack.Push(root as JObject ?? throw new ArgumentException(nameof(reader)));
            }
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="context"></param>
        /// <param name="root"></param>
        public JsonDecoderEx(ServiceMessageContext context, JObject root) {
            Context = context;
            _stack.Push(root ?? throw new ArgumentException(nameof(root)));
        }

        /// <inheritdoc/>
        public void Dispose() {
            // No op
        }

        /// <inheritdoc/>
        public void PushNamespace(string namespaceUri) {
            // No op
        }

        /// <inheritdoc/>
        public void PopNamespace() {
            // No op
        }

        /// <inheritdoc/>
        public void SetMappingTables(NamespaceTable namespaceUris, StringTable serverUris) {
            _namespaceMappings = null;

            if (namespaceUris != null && Context.NamespaceUris != null) {
                _namespaceMappings = Context.NamespaceUris.CreateMapping(namespaceUris, false);
            }

            _serverMappings = null;

            if (serverUris != null && Context.ServerUris != null) {
                _serverMappings = Context.ServerUris.CreateMapping(serverUris, false);
            }
        }

        /// <inheritdoc/>
        public bool ReadBoolean(string property) =>
            TryGetToken(property, out var value) && (bool)value;

        /// <inheritdoc/>
        public sbyte ReadSByte(string property) => ReadValue<sbyte>(property,
            v => v < sbyte.MinValue || v > sbyte.MaxValue ? (sbyte)0 : v);

        /// <inheritdoc/>
        public byte ReadByte(string property) => ReadValue<byte>(property,
            v => (byte) (v < byte.MinValue || v > byte.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public short ReadInt16(string property) => ReadValue<short>(property,
            v => (short) (v < short.MinValue || v > short.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public ushort ReadUInt16(string property) => ReadValue<ushort>(property,
            v => (ushort) (v < ushort.MinValue || v > ushort.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public int ReadInt32(string property) => ReadValue<int>(property,
            v => v < int.MinValue || v > int.MaxValue ? 0 : v);

        /// <inheritdoc/>
        public uint ReadUInt32(string property) => ReadValue<uint>(property,
            v => v < uint.MinValue || v > uint.MaxValue ? 0 : v);

        /// <inheritdoc/>
        public long ReadInt64(string property) => ReadValue<long>(property,
            v => v);

        /// <inheritdoc/>
        public ulong ReadUInt64(string property) => ReadValue<ulong>(property,
            v => v);

        /// <inheritdoc/>
        public float ReadFloat(string property) => ReadValue<float>(property,
            v => v < float.MinValue || v > float.MaxValue ? 0 : v);

        /// <inheritdoc/>
        public double ReadDouble(string property) => ReadValue<double>(property,
            v => v);

        /// <inheritdoc/>
        public byte[] ReadByteString(string property) => TryReadValue(property,
            t => ((string)t).DecodeAsBase64());

        /// <inheritdoc/>
        public string ReadString(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            return (string)token;
        }

        /// <inheritdoc/>
        public Uuid ReadGuid(string property) {
            if (!TryGetToken(property, out var token)) {
                return Uuid.Empty;
            }
            switch (token.Type) {
                case JTokenType.String:
                    if (Guid.TryParse((string)token, out var guid)) {
                        return new Uuid(guid);
                    }
                    return new Uuid((string)token);
                case JTokenType.Guid:
                    return new Uuid((Guid)token);
                case JTokenType.Bytes:
                    var bytes = (byte[])token;
                    if (bytes.Length != 16) {
                        break;
                    }
                    return new Uuid(new Guid((byte[])token));
            }
            return Uuid.Empty;
        }

        /// <inheritdoc/>
        public DateTime ReadDateTime(string property) {
            if (!TryGetToken(property, out var token)) {
                return DateTime.MinValue;
            }
            if (token.Type == JTokenType.String) {
                return XmlConvert.ToDateTime((string)token,
                    XmlDateTimeSerializationMode.Utc);
            }
            var value = token.ToObject<DateTime?>();
            if (value != null) {
                return value.Value;
            }
            return DateTime.MinValue;
        }

        /// <inheritdoc/>
        public XmlElement ReadXmlElement(string property) {
            return TryReadValue(property, t => {
                var bytes = t.ToObject<byte[]>();
                if (bytes != null && bytes.Length > 0) {
                    var document = new XmlDocument {
                        InnerXml = Encoding.UTF8.GetString(bytes)
                    };
                    return document.DocumentElement;
                }
                return null;
            });
        }

        /// <inheritdoc/>
        public NodeId ReadNodeId(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                // Read non reversable encoding
                var id = ReadString("Id");
                var uri = ReadString("Uri");
                if (string.IsNullOrEmpty(uri)) {
                    var index = (ushort)ReadUInt32("Index");
                    uri = Context.NamespaceUris.GetString(index);
                }
                _stack.Pop();
                return NodeId.Parse(id);
            }
            if (token.Type == JTokenType.String) {
                var id = (string)token;
                var nodeId = id.ToNodeId(Context);
                if (!NodeId.IsNull(nodeId)) {
                    return nodeId;
                }
                return NodeId.Parse(id);
            }
            return null;
        }

        /// <inheritdoc/>
        public ExpandedNodeId ReadExpandedNodeId(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                // Read non reversable encoding
                var id = ReadString("Id");
                var uri = ReadString("Uri");
                if (string.IsNullOrEmpty(uri)) {
                    var index = (ushort)ReadUInt32("Index");
                    uri = Context.NamespaceUris.GetString(index);
                }
                var serverIndex = (ushort)ReadUInt32("ServerIndex");
                if (serverIndex == 0) {
                    var server = ReadString("ServerUri");
                    serverIndex = Context.NamespaceUris.GetIndexOrAppend(server);
                }
                _stack.Pop();
                return new ExpandedNodeId(NodeId.Parse(id), uri, serverIndex);
            }
            if (token.Type == JTokenType.String) {
                var id = (string)token;
                var nodeId = id.ToExpandedNodeId(Context);
                if (!NodeId.IsNull(nodeId)) {
                    return nodeId;
                }
                return ExpandedNodeId.Parse(id);
            }
            return null;
        }

        /// <inheritdoc/>
        public StatusCode ReadStatusCode(string property) {
            if (!TryGetToken(property, out var token)) {
                return 0;
            }
            if (token is JObject o) {
                _stack.Push(o);
                // Read non reversable encoding
                var code = new StatusCode(ReadUInt32("Code"));
                // var status = ReadString("Symbol");
                _stack.Pop();
                return code;
            }
            return ReadValue<uint>(property, v =>
                v < uint.MinValue || v > uint.MaxValue ? 0 : v);
        }

        /// <inheritdoc/>
        public DiagnosticInfo ReadDiagnosticInfo(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                var di = new DiagnosticInfo {
                    SymbolicId = ReadInt32(
                        "SymbolicId"),
                    NamespaceUri = ReadInt32(
                        "NamespaceUri"),
                    Locale = ReadInt32(
                        "Locale"),
                    LocalizedText = ReadInt32(
                        "LocalizedText"),
                    AdditionalInfo = ReadString(
                        "AdditionalInfo"),
                    InnerStatusCode = ReadStatusCode(
                        "InnerStatusCode"),
                    InnerDiagnosticInfo = ReadDiagnosticInfo(
                        "InnerDiagnosticInfo")
                };
                _stack.Pop();
                return di;
            }
            return null;
        }

        /// <inheritdoc/>
        public QualifiedName ReadQualifiedName(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                try {
                    var name = ReadString("Name");
                    if (string.IsNullOrEmpty(name)) {
                        return null;
                    }
                    var index = 0u;
                    if (TryGetToken("Uri", out var uri)) {
                        if (uri.Type == JTokenType.Integer) {
                            index = (uint)uri;
                        }
                        else if (uri.Type == JTokenType.String) {
                            // Reversible
                            index = Context.NamespaceUris
                                .GetIndexOrAppend((string)uri);
                        }
                        else {
                            // Bad uri
                            return null;
                        }
                    }
                    else {
                        index = ReadUInt32("Index");
                    }
                    return new QualifiedName(name, (ushort)index);
                }
                finally {
                    _stack.Pop();
                }
            }
            if (token.Type == JTokenType.String) {
                var id = (string)token;
                var qn = id.ToQualifiedName(Context);
                if (!QualifiedName.IsNull(qn)) {
                    return qn;
                }
                return QualifiedName.Parse(id);
            }
            return null;
        }

        /// <inheritdoc/>
        public LocalizedText ReadLocalizedText(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                var text = ReadString("Text");
                var locale = ReadString("Locale");
                _stack.Pop();
                return new LocalizedText(locale, text);
            }
            if (token.Type == JTokenType.String) {
                // Non reversible or locale was null
                var text = (string)token;
                if (!string.IsNullOrEmpty(text)) {
                    return new LocalizedText(text);
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public Variant ReadVariant(string property) {
            if (!TryGetToken(property, out var token)) {
                return Variant.Null;
            }
            if (token is JObject o) {
                return TryReadVariant(o, out var tmp);
            }
            return ReadVariantFromToken(token);
        }

        /// <inheritdoc/>
        public DataValue ReadDataValue(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o && o.ContainsKey("Value")) {
                _stack.Push(o);
                var dv = new DataValue {
                    WrappedValue = ReadVariant("Value"),
                    StatusCode = ReadStatusCode("StatusCode"),
                    SourceTimestamp = ReadDateTime("SourceTimestamp"),
                    SourcePicoseconds = ReadUInt16("SourcePicoseconds"),
                    ServerTimestamp = ReadDateTime("ServerTimestamp"),
                    ServerPicoseconds = ReadUInt16("ServerPicoseconds")
                };
                _stack.Pop();
                return dv;
            }
            var variant = ReadVariant(property);
            if (variant == Variant.Null) {
                return null;
            }
            return new DataValue(variant);
        }

        /// <inheritdoc/>
        public ExtensionObject ReadExtensionObject(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o && o.ContainsKey("Body")) {
                ExtensionObject extensionObject = null;
                _stack.Push(o);

                var typeId = ReadExpandedNodeId("TypeId");
                var encoding = ReadEncoding("Encoding");
                extensionObject = ReadExtensionObjectBody("Body",
                    encoding, typeId);

                _stack.Pop();
                return extensionObject;
            }
            return null;
        }

        /// <inheritdoc/>
        public IEncodeable ReadEncodeable(string property, Type systemType) {
            if (systemType == null) {
                throw new ArgumentNullException(nameof(systemType));
            }
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (!(Activator.CreateInstance(systemType) is IEncodeable value)) {
                return null;
            }
            if (token is JObject o) {
                _stack.Push(o);
                value.Decode(this);
                _stack.Pop();
                return value;
            }
            return null; // or value?
        }

        /// <inheritdoc/>
        public Enum ReadEnumerated(string property, Type enumType) {
            if (enumType == null) {
                throw new ArgumentNullException(nameof(enumType));
            }
            if (!TryGetToken(property, out var token)) {
                return (Enum)Enum.ToObject(enumType, 0); // or null?
            }
            if (token.Type == JTokenType.String) {
                var val = (string)token;
                var index = val.LastIndexOf('_');
                if (index != -1 && int.TryParse(val.Substring(index + 1),
                    out var numeric)) {
                    return (Enum)Enum.ToObject(enumType, numeric);
                }
                return (Enum)Enum.Parse(enumType, val, true);
            }
            if (token.Type == JTokenType.Integer) {
                return (Enum)Enum.ToObject(enumType, (int)token);
            }
            return null;
        }

        /// <inheritdoc/>
        public BooleanCollection ReadBooleanArray(string property) =>
            ReadArray(property, () => ReadBoolean(null));

        /// <inheritdoc/>
        public Int16Collection ReadInt16Array(string property) =>
            ReadArray(property, () => ReadInt16(null));

        /// <inheritdoc/>
        public UInt16Collection ReadUInt16Array(string property) =>
            ReadArray(property, () => ReadUInt16(null));

        /// <inheritdoc/>
        public Int32Collection ReadInt32Array(string property) =>
            ReadArray(property, () => ReadInt32(null));

        /// <inheritdoc/>
        public UInt32Collection ReadUInt32Array(string property) =>
            ReadArray(property, () => ReadUInt32(null));

        /// <inheritdoc/>
        public Int64Collection ReadInt64Array(string property) =>
            ReadArray(property, () => ReadInt64(null));

        /// <inheritdoc/>
        public UInt64Collection ReadUInt64Array(string property) =>
            ReadArray(property, () => ReadUInt64(null));

        /// <inheritdoc/>
        public FloatCollection ReadFloatArray(string property) =>
            ReadArray(property, () => ReadFloat(null));

        /// <inheritdoc/>
        public DoubleCollection ReadDoubleArray(string property) =>
            ReadArray(property, () => ReadDouble(null));

        /// <inheritdoc/>
        public StringCollection ReadStringArray(string property) =>
            ReadArray(property, () => ReadString(null));

        /// <inheritdoc/>
        public DateTimeCollection ReadDateTimeArray(string property) =>
            ReadArray(property, () => ReadDateTime(null));

        /// <inheritdoc/>
        public UuidCollection ReadGuidArray(string property) =>
            ReadArray(property, () => ReadGuid(null));

        /// <inheritdoc/>
        public ByteStringCollection ReadByteStringArray(string property) =>
            ReadArray(property, () => ReadByteString(null));

        /// <inheritdoc/>
        public XmlElementCollection ReadXmlElementArray(string property) =>
            ReadArray(property, () => ReadXmlElement(null));

        /// <inheritdoc/>
        public NodeIdCollection ReadNodeIdArray(string property) =>
            ReadArray(property, () => ReadNodeId(null));

        /// <inheritdoc/>
        public ExpandedNodeIdCollection ReadExpandedNodeIdArray(string property) =>
            ReadArray(property, () => ReadExpandedNodeId(null));

        /// <inheritdoc/>
        public StatusCodeCollection ReadStatusCodeArray(string property) =>
            ReadArray(property, () => ReadStatusCode(null));

        /// <inheritdoc/>
        public DiagnosticInfoCollection ReadDiagnosticInfoArray(string property) =>
            ReadArray(property, () => ReadDiagnosticInfo(null));

        /// <inheritdoc/>
        public QualifiedNameCollection ReadQualifiedNameArray(string property) =>
            ReadArray(property, () => ReadQualifiedName(null));

        /// <inheritdoc/>
        public LocalizedTextCollection ReadLocalizedTextArray(string property) =>
            ReadArray(property, () => ReadLocalizedText(null));

        /// <inheritdoc/>
        public VariantCollection ReadVariantArray(string property) =>
            ReadArray(property, () => ReadVariant(null));

        /// <inheritdoc/>
        public DataValueCollection ReadDataValueArray(string property) =>
            ReadArray(property, () => ReadDataValue(null));

        /// <inheritdoc/>
        public ExtensionObjectCollection ReadExtensionObjectArray(string property) =>
            ReadArray(property, () => ReadExtensionObject(null));

        /// <inheritdoc/>
        public ByteCollection ReadByteArray(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token.Type == JTokenType.Bytes ||
                token.Type == JTokenType.String) {
                return ((string)token).DecodeAsBase64();
            }
            if (token is JArray a) {
                return a.Select(t => ReadToken(t,
                    () => ReadByte(null))).ToArray();
            }
            return new ByteCollection {
                ReadToken(token, () => ReadByte(null))
            };
        }

        /// <inheritdoc/>
        public SByteCollection ReadSByteArray(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token.Type == JTokenType.Bytes ||
                token.Type == JTokenType.String) {
                return ((string)token).DecodeAsBase64()
                    .Select(b => (sbyte)b).ToArray();
            }
            if (token is JArray a) {
                return a.Select(t => ReadToken(t,
                    () => ReadSByte(null))).ToArray();
            }
            return new SByteCollection {
                ReadToken(token, () => ReadSByte(null))
            };
        }

        /// <inheritdoc/>
        public Array ReadEncodeableArray(string property, Type systemType) {
            var values = ReadArray(property, () => ReadEncodeable(null, systemType))?
                .ToList();
            if (values == null) {
                return null;
            }
            var array = Array.CreateInstance(systemType, values.Count);
            values.CopyTo((IEncodeable[])array);
            return array;
        }

        /// <inheritdoc/>
        public Array ReadEnumeratedArray(string property, Type enumType) {
            var values = ReadArray(property, () => ReadEnumerated(null, enumType))?
                .ToList();
            if (values == null) {
                return null;
            }
            var array = Array.CreateInstance(enumType, values.Count);
            values.CopyTo((Enum[])array);
            return array;
        }

        /// <summary>
        /// Read integers
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public VariantCollection ReadIntegerArray(string property) =>
            ReadArray(property, () => ReadInteger(null));

        /// <summary>
        /// Read integer variant value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Variant ReadInteger(string property) {
            if (!TryGetToken(property, out var token)) {
                return Variant.Null;
            }
            Variant number;
            if (token is JObject o) {
                number = TryReadVariant(o, out var tmp);
            }
            else {
                number = ReadVariantFromToken(token, false);
            }
            var builtInType = number.TypeInfo.BuiltInType;
            if ((builtInType >= BuiltInType.SByte &&
                 builtInType <= BuiltInType.UInt64) ||
                builtInType == BuiltInType.Integer) {
                return number;
            }
            else {
                // TODO Log or throw for bad type
            }
            return Variant.Null;
        }

        /// <summary>
        /// Read unsigned integers
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public VariantCollection ReadUIntegerArray(string property) =>
            ReadArray(property, () => ReadUInteger(null));

        /// <summary>
        /// Read unsigned integer variant value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Variant ReadUInteger(string property) {
            if (!TryGetToken(property, out var token)) {
                return Variant.Null;
            }
            Variant number;
            if (token is JObject o) {
                number = TryReadVariant(o, out var tmp);
            }
            else {
                number = ReadVariantFromToken(token, true);
            }
            var builtInType = number.TypeInfo.BuiltInType;
            if ((builtInType >= BuiltInType.Byte &&
                 builtInType <= BuiltInType.UInt64) ||
                builtInType == BuiltInType.UInteger) {
                return number;
            }
            else {
                // TODO Log or throw for bad type
            }
            return Variant.Null;
        }

        /// <summary>
        /// Read numeric values
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public VariantCollection ReadNumberArray(string property) =>
            ReadArray(property, () => ReadNumber(null));

        /// <summary>
        /// Read numeric variant value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public Variant ReadNumber(string property) {
            if (!TryGetToken(property, out var token)) {
                return Variant.Null;
            }
            Variant number;
            if (token is JObject o) {
                number = TryReadVariant(o, out var tmp);
            }
            else {
                number = ReadVariantFromToken(token);
            }
            if (TypeInfo.IsNumericType(number.TypeInfo.BuiltInType)) {
                return number;
            }
            else {
                // TODO Log or throw for bad type
            }
            return Variant.Null;
        }

        /// <summary>
        /// Read extension object body
        /// </summary>
        /// <param name="property"></param>
        /// <param name="encoding"></param>
        /// <param name="typeId"></param>
        /// <returns></returns>
        private ExtensionObject ReadExtensionObjectBody(string property,
            ExtensionObjectEncoding encoding, ExpandedNodeId typeId) {
            if (!TryGetToken(property, out var body)) {
                return null;
            }
            var nextEncoding = encoding;
            var systemType = Context.Factory.GetSystemType(typeId);
            while (true) {
                switch (nextEncoding) {
                    case ExtensionObjectEncoding.Json:
                        if (systemType != null) {
                            var encodeable = ReadEncodeable(property, systemType);
                            if (encodeable == null) {
                                break;
                            }
                            return new ExtensionObject(typeId, encodeable);
                        }
                        return new ExtensionObject(typeId, body.ToString());
                            // TODO Should be Jtoken
                    case ExtensionObjectEncoding.Binary:
                        var bytes = ReadByteString(property);
                        if (bytes == null) {
                            break;
                        }
                        if (systemType != null) {
                            var encodeable = bytes.ToEncodeable(typeId, Context);
                            if (encodeable == null) {
                                break;
                            }
                            return new ExtensionObject(typeId, encodeable);
                        }
                        return new ExtensionObject(typeId, bytes);
                    default:
                        nextEncoding = ExtensionObjectEncoding.Xml;
                        if (body.Type != JTokenType.Object) {
                            break;
                        }
                        XmlElement xml = null;
                        try {
                            xml = body.ToObject<XmlElement>();
                            if (xml == null) {
                                break;
                            }
                        }
                        catch {
                            break;
                        }
                        if (systemType != null) {
                            var encodeable = xml.ToEncodeable(typeId, Context);
                            if (encodeable == null) {
                                break;
                            }
                            return new ExtensionObject(typeId, encodeable);
                        }
                        return new ExtensionObject(typeId, xml);
                }
                if (encoding == nextEncoding) {
                    // Honor defined encoding
                    return null;
                }
                switch (nextEncoding) {
                    case ExtensionObjectEncoding.Json:
                        // Try xml next
                        nextEncoding = ExtensionObjectEncoding.Xml;
                        break;
                    case ExtensionObjectEncoding.Xml:
                        // Try binary next
                        nextEncoding = ExtensionObjectEncoding.Binary;
                        break;
                    default:
                        // Give up

                        // TODO Log or throw for bad type
                        return null;
                }
            }
        }

        /// <summary>
        /// Convert a token to variant
        /// </summary>
        /// <param name="token"></param>
        /// <param name="unsigned"></param>
        /// <returns></returns>
        private Variant ReadVariantFromToken(JToken token, bool unsigned = false) {
            try {
                switch (token.Type) {
                    case JTokenType.Integer:
                        return !unsigned ? new Variant((long)token) :
                            new Variant((ulong)token);
                    case JTokenType.Boolean:
                        return new Variant((bool)token);
                    case JTokenType.Bytes:
                        return new Variant((byte[])token);
                    case JTokenType.Date:
                        return new Variant((DateTime)token);
                    case JTokenType.TimeSpan:
                        return new Variant(((TimeSpan)token).TotalMilliseconds);
                    case JTokenType.Float:
                        return new Variant((double)token);
                    case JTokenType.Guid:
                        return new Variant((Guid)token);
                    case JTokenType.String:
                        return new Variant((string)token);
                    case JTokenType.Object:
                        var variant = TryReadVariant((JObject)token, out var found);
                        if (found) {
                            return variant;
                        }
                        try {
                            return new Variant(((JObject)token).ToObject<XmlElement>());
                        }
                        catch {
                            // TODO: Try to read other structures
                            // ...
                            //
                            return Variant.Null; // Give up
                        }
                    case JTokenType.Array:
                        return ReadVariantFromArray((JArray)token);
                    default:
                        // TODO Log or throw for bad type
                        return Variant.Null;
                }
            }
            catch {
                return Variant.Null; // Give up
            }
        }

        /// <summary>
        /// Read variant from token
        /// </summary>
        /// <param name="array"></param>
        /// <param name="unsigned">Force integers to be unsigned</param>
        /// <returns></returns>
        private Variant ReadVariantFromArray(JArray array, bool unsigned = false) {
            if (array.Count == 0) {
                return Variant.Null; // Give up
            }

            // Try to decode non reversible encoding first.
            var type = array[0].Type;
            if (type != JTokenType.Object &&
                array.All(j => j.Type == type)) {
                try {
                    switch (array[0].Type) {
                        case JTokenType.Integer:
                            return !unsigned ? new Variant(array
                                .Select(t => (long)t)
                                .ToArray()) : new Variant(array
                                .Select(t => (ulong)t)
                                .ToArray());
                        case JTokenType.Boolean:
                            return new Variant(array
                                .Select(t => (bool)t)
                                .ToArray());
                        case JTokenType.Bytes:
                            return new Variant(array
                                .Select(t => (byte[])t)
                                .ToArray());
                        case JTokenType.Date:
                            return new Variant(array
                                .Select(t => (DateTime)t)
                                .ToArray());
                        case JTokenType.TimeSpan:
                            return new Variant(array
                                .Select(t => ((TimeSpan)t).TotalMilliseconds)
                                .ToArray());
                        case JTokenType.Float:
                            return new Variant(array
                                .Select(t => (double)t)
                                .ToArray());
                        case JTokenType.Guid:
                            return new Variant(array
                                .Select(t => (Guid)t)
                                .ToArray());
                        case JTokenType.String:
                            return new Variant(array
                                .Select(t => (string)t)
                                .ToArray());
                    }
                }
                catch {
                    // TODO Log or throw for bad type
                    return Variant.Null; // Give up
                }
            }
            var result = array
                .Select(t => ReadVariantFromToken(t, unsigned))
                .ToArray();
            if (result
                .Where(v => v != Variant.Null)
                .All(v => v.TypeInfo.BuiltInType == result[0].TypeInfo.BuiltInType)) {
                // TODO: This needs tests as it should not work.
                return new Variant(result.Select(v => v.Value).ToArray());
            }
            return new Variant(result);
        }

        /// <summary>
        /// Read variant
        /// </summary>
        /// <param name="o"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        private Variant TryReadVariant(JObject o, out bool success) {
            Variant variant;
            _stack.Push(o);
            if (TryReadBuiltInType("Type", out var type)) {
                variant = ReadVariantBody("Body", type);
                success = true;
            }
            else if (TryReadBuiltInType("DataType", out type)) {
                variant = ReadVariantBody("Value", type);
                success = true;
            }
            else {
                variant = Variant.Null;
                success = false;
            }
            _stack.Pop();
            return variant;
        }

        /// <summary>
        /// Read variant body
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Variant ReadVariantBody(string property, BuiltInType type) {
            if (!TryGetToken(property, out var token)) {
                return Variant.Null;
            }
            if (token is JArray ||
                ((token.Type == JTokenType.Bytes || token.Type == JTokenType.String) &&
                 (type == BuiltInType.Byte || type == BuiltInType.SByte))) {

                // Body is array - read object dimensions if any
                var dimensions = ReadInt32Array("Dimensions");

                // Read body as array
                var array = ReadVariantArrayBody(property, type);

                if (array.Value is ICollection && dimensions != null &&
                    dimensions.Count > 1) {
                    array = new Variant(new Matrix((Array)array.Value,
                        type, dimensions.ToArray()));
                }
                return array;
            }
            switch (type) {
                case BuiltInType.Boolean:
                    return new Variant(ReadBoolean(property),
                        TypeInfo.Scalars.Boolean);
                case BuiltInType.SByte:
                    return new Variant(ReadSByte(property),
                        TypeInfo.Scalars.SByte);
                case BuiltInType.Byte:
                    return new Variant(ReadByte(property),
                        TypeInfo.Scalars.Byte);
                case BuiltInType.Int16:
                    return new Variant(ReadInt16(property),
                        TypeInfo.Scalars.Int16);
                case BuiltInType.UInt16:
                    return new Variant(ReadUInt16(property),
                        TypeInfo.Scalars.UInt16);
                case BuiltInType.Enumeration:
                case BuiltInType.Int32:
                    return new Variant(ReadInt32(property),
                        TypeInfo.Scalars.Int32);
                case BuiltInType.UInt32:
                    return new Variant(ReadUInt32(property),
                        TypeInfo.Scalars.UInt32);
                case BuiltInType.Int64:
                    return new Variant(ReadInt64(property),
                        TypeInfo.Scalars.Int64);
                case BuiltInType.UInt64:
                    return new Variant(ReadUInt64(property),
                        TypeInfo.Scalars.UInt64);
                case BuiltInType.Float:
                    return new Variant(ReadFloat(property),
                        TypeInfo.Scalars.Float);
                case BuiltInType.Double:
                    return new Variant(ReadDouble(property),
                        TypeInfo.Scalars.Double);
                case BuiltInType.String:
                    return new Variant(ReadString(property),
                        TypeInfo.Scalars.String);
                case BuiltInType.ByteString:
                    return new Variant(ReadByteString(property),
                        TypeInfo.Scalars.ByteString);
                case BuiltInType.DateTime:
                    return new Variant(ReadDateTime(property),
                        TypeInfo.Scalars.DateTime);
                case BuiltInType.Guid:
                    return new Variant(ReadGuid(property),
                        TypeInfo.Scalars.Guid);
                case BuiltInType.NodeId:
                    return new Variant(ReadNodeId(property),
                        TypeInfo.Scalars.NodeId);
                case BuiltInType.ExpandedNodeId:
                    return new Variant(ReadExpandedNodeId(property),
                        TypeInfo.Scalars.ExpandedNodeId);
                case BuiltInType.QualifiedName:
                    return new Variant(ReadQualifiedName(property),
                        TypeInfo.Scalars.QualifiedName);
                case BuiltInType.LocalizedText:
                    return new Variant(ReadLocalizedText(property),
                        TypeInfo.Scalars.LocalizedText);
                case BuiltInType.StatusCode:
                    return new Variant(ReadStatusCode(property),
                        TypeInfo.Scalars.StatusCode);
                case BuiltInType.XmlElement:
                    return new Variant(ReadXmlElement(property),
                        TypeInfo.Scalars.XmlElement);
                case BuiltInType.ExtensionObject:
                    return new Variant(ReadExtensionObject(property),
                        TypeInfo.Scalars.ExtensionObject);
                case BuiltInType.Number:
                case BuiltInType.UInteger:
                case BuiltInType.Integer:
                case BuiltInType.Variant:
                    return ReadVariant(property);
                default:
                    return Variant.Null;
            }
        }

        /// <summary>
        /// Read variant array
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Variant ReadVariantArrayBody(string property, BuiltInType type) {
            switch (type) {
                case BuiltInType.Boolean:
                    return new Variant(ReadBooleanArray(property),
                        TypeInfo.Arrays.Boolean);
                case BuiltInType.SByte:
                    return new Variant(ReadSByteArray(property),
                        TypeInfo.Arrays.SByte);
                case BuiltInType.Byte:
                    return new Variant(ReadByteArray(property),
                        TypeInfo.Arrays.Byte);
                case BuiltInType.Int16:
                    return new Variant(ReadInt16Array(property),
                        TypeInfo.Arrays.Int16);
                case BuiltInType.UInt16:
                    return new Variant(ReadUInt16Array(property),
                        TypeInfo.Arrays.UInt16);
                case BuiltInType.Enumeration:
                case BuiltInType.Int32:
                    return new Variant(ReadInt32Array(property),
                        TypeInfo.Arrays.Int32);
                case BuiltInType.UInt32:
                    return new Variant(ReadUInt32Array(property),
                        TypeInfo.Arrays.UInt32);
                case BuiltInType.Int64:
                    return new Variant(ReadInt64Array(property),
                        TypeInfo.Arrays.Int64);
                case BuiltInType.UInt64:
                    return new Variant(ReadUInt64Array(property),
                        TypeInfo.Arrays.UInt64);
                case BuiltInType.Float:
                    return new Variant(ReadFloatArray(property),
                        TypeInfo.Arrays.Float);
                case BuiltInType.Double:
                    return new Variant(ReadDoubleArray(property),
                        TypeInfo.Arrays.Double);
                case BuiltInType.String:
                    return new Variant(ReadStringArray(property),
                        TypeInfo.Arrays.String);
                case BuiltInType.ByteString:
                    return new Variant(ReadByteStringArray(property),
                        TypeInfo.Arrays.ByteString);
                case BuiltInType.DateTime:
                    return new Variant(ReadDateTimeArray(property),
                        TypeInfo.Arrays.DateTime);
                case BuiltInType.Guid:
                    return new Variant(ReadGuidArray(property),
                        TypeInfo.Arrays.Guid);
                case BuiltInType.NodeId:
                    return new Variant(ReadNodeIdArray(property),
                        TypeInfo.Arrays.NodeId);
                case BuiltInType.ExpandedNodeId:
                    return new Variant(ReadExpandedNodeIdArray(property),
                        TypeInfo.Arrays.ExpandedNodeId);
                case BuiltInType.QualifiedName:
                    return new Variant(ReadQualifiedNameArray(property),
                        TypeInfo.Arrays.QualifiedName);
                case BuiltInType.LocalizedText:
                    return new Variant(ReadLocalizedTextArray(property),
                        TypeInfo.Arrays.LocalizedText);
                case BuiltInType.StatusCode:
                    return new Variant(ReadStatusCodeArray(property),
                        TypeInfo.Arrays.StatusCode);
                case BuiltInType.XmlElement:
                    return new Variant(ReadXmlElementArray(property),
                        TypeInfo.Arrays.XmlElement);
                case BuiltInType.ExtensionObject:
                    return new Variant(ReadExtensionObjectArray(property),
                        TypeInfo.Arrays.ExtensionObject);
                case BuiltInType.UInteger:
                    return new Variant(ReadUIntegerArray(property),
                        TypeInfo.Arrays.Variant);
                case BuiltInType.Integer:
                    return new Variant(ReadIntegerArray(property),
                        TypeInfo.Arrays.Variant);
                case BuiltInType.Number:
                    return new Variant(ReadNumberArray(property),
                        TypeInfo.Arrays.Variant);
                case BuiltInType.Variant:
                    return new Variant(ReadVariantArray(property),
                        TypeInfo.Arrays.Variant);
                default:
                    return Variant.Null;
            }
        }

        /// <summary>
        /// Read value with check
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="check"></param>
        /// <returns></returns>
        private T ReadValue<T>(string property, Func<T, T> check) {
            if (!TryGetToken(property, out var token)) {
                return default(T);
            }
            try {
                return check(token.ToObject<T>());
            }
            catch {
                return default(T);
            }
        }

        /// <summary>
        /// Read value with conversion fallback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private T TryReadValue<T>(string property, Func<JToken, T> fallback)
            where T : class {
            if (!TryGetToken(property, out var token)) {
                return default(T);
            }
            try {
                var value = token.ToObject<T>();
                if (value != null) {
                    return value;
                }
                return fallback(token);
            }
            catch {
                return default(T);
            }
        }

        /// <summary>
        /// Read built in type value
        /// </summary>
        /// <returns></returns>
        private bool TryReadBuiltInType(string property, out BuiltInType type) {
            type = BuiltInType.Null;
            if (!TryGetToken(property, out var token)) {
                return false;
            }
            if (token.Type == JTokenType.String) {
                return Enum.TryParse((string)token, true, out type);
            }
            try {
                type = (BuiltInType)token.ToObject<byte>();
                return true;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Read encoding value
        /// </summary>
        /// <returns></returns>
        private ExtensionObjectEncoding ReadEncoding(string property) {
            if (!TryGetToken(property, out var token)) {
                return ExtensionObjectEncoding.None;
            }
            if (token.Type == JTokenType.String) {
                if (Enum.TryParse<ExtensionObjectEncoding>((string)token,
                    true, out var encoding)) {
                    return encoding;
                }
            }
            try {
                return (ExtensionObjectEncoding)token.ToObject<byte>();
            }
            catch {
                return ExtensionObjectEncoding.None;
            }
        }

        /// <summary>
        /// Read array using specified element reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private T[] ReadArray<T>(string property, Func<T> reader) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JArray a) {
                return a.Select(t => ReadToken(t, reader)).ToArray();
            }
            return ReadToken(token, reader).YieldReturn().ToArray();
        }

        /// <summary>
        /// Read token using a specified reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private T ReadToken<T>(JToken token, Func<T> reader) {
            try {
                _stack.Push(token);
                return reader();
            }
            finally {
                _stack.Pop();
            }
        }

        /// <summary>
        /// Try get top token or named token from object
        /// </summary>
        /// <param name="property"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool TryGetToken(string property, out JToken token) {
            var top = _stack.Peek();
            if (property == null) {
                token = top;
                return true;
            }
            if (top is JObject o) {
                if (!o.TryGetValue(property,
                    StringComparison.InvariantCultureIgnoreCase, out token)) {
                    return false;
                }
                switch (token.Type) {
                    case JTokenType.Comment:
                    case JTokenType.Constructor:
                    case JTokenType.None:
                    case JTokenType.Property:
                    case JTokenType.Raw:
                    case JTokenType.Undefined:
                    case JTokenType.Null:
                        return false;
                }
                return true;
            }
            throw new ServiceResultException(StatusCodes.BadDecodingError,
                "Expected object at top of stack");
        }

        /// <summary>
        /// Works around missing object endings, etc.
        /// </summary>
        private class JsonLoader : JsonReader, IDisposable {

            /// <inheritdoc/>
            public override string Path => _reader.Path;

            /// <inheritdoc/>
            public override object Value => _reader.Value;

            /// <inheritdoc/>
            public override JsonToken TokenType =>
               _eofDepth >= 0 ? JsonToken.EndObject : _reader.TokenType;

            /// <inheritdoc/>
            public override int Depth =>
                _eofDepth >= 0 ? --_eofDepth : _reader.Depth;

            /// <summary>
            /// Create loader
            /// </summary>
            /// <param name="reader"></param>
            public JsonLoader(JsonReader reader) {
                _reader = reader;
                _eofDepth = -1;
            }

            /// <inheritdoc/>
            public override bool Read() {
                if (!_reader.Read()) {
                    _eofDepth = Depth;
                    return true;
                }
                return true;
            }

            /// <inheritdoc/>
            public void Dispose() => _reader.Close();

            private readonly JsonReader _reader;
            private int _eofDepth;
        }

        private ushort[] _namespaceMappings;
        private ushort[] _serverMappings;
        private readonly Stack<JToken> _stack = new Stack<JToken>();
    }
}
