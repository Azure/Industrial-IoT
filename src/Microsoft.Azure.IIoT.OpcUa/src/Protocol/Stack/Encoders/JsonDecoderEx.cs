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
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections;
    using Opc.Ua.Extensions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Globalization;

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
        public sbyte ReadSByte(string property) => ReadInteger(property,
            v => (sbyte) (v < sbyte.MinValue || v > sbyte.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public byte ReadByte(string property) => ReadInteger(property,
            v => (byte) (v < byte.MinValue || v > byte.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public short ReadInt16(string property) => ReadInteger(property,
            v => (short) (v < short.MinValue || v > short.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public ushort ReadUInt16(string property) => ReadInteger(property,
            v => (ushort) (v < ushort.MinValue || v > ushort.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public int ReadInt32(string property) => ReadInteger(property,
            v => (int) (v < int.MinValue || v > int.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public uint ReadUInt32(string property) => ReadInteger(property,
            v => (uint) (v < uint.MinValue || v > uint.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public long ReadInt64(string property) => ReadInteger(property,
            v => v);

        /// <inheritdoc/>
        public ulong ReadUInt64(string property) => ReadInteger(property,
            v => (ulong)v);

        /// <inheritdoc/>
        public float ReadFloat(string property) => ReadDouble(property,
            v => (float) (v < float.MinValue || v > float.MaxValue ? 0 : v));

        /// <inheritdoc/>
        public double ReadDouble(string property) => ReadDouble(property,
            v => v);

        /// <inheritdoc/>
        public byte[] ReadByteString(string property) => ReadValue(property,
            t => Convert.FromBase64String((string)t));

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
            return ReadValue(property, t => {
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
            return ReadInteger(property, v =>
                (uint)(v < uint.MinValue || v > uint.MaxValue ? 0 : v));
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
                _stack.Push(o);
                var type = (BuiltInType)ReadByte("Type");
                var variant = ReadVariantBody("Body", type);
                _stack.Pop();
                return variant;
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
            if (token is JObject o) {
                ExtensionObject extensionObject = null;
                _stack.Push(o);

                var typeId = ReadNodeId("TypeId");
                var systemType = Context.Factory.GetSystemType(typeId);
                if (systemType != null) {
                    // We know this one - first try decode it as encodeable from json
                    var encodeable = ReadEncodeable("Body", systemType);
                    if (encodeable != null) {
                        extensionObject = new ExtensionObject(typeId, encodeable);
                    }
                }
                if (extensionObject == null) {
                    var encoding = ReadByte("Encoding");
                    switch ((ExtensionObjectEncoding)encoding) {
                        case ExtensionObjectEncoding.Xml:
                            var xml = ReadXmlElement("Body");
                            if (xml != null) {
                                extensionObject = new ExtensionObject(typeId, xml);
                            }
                            break;
                        case ExtensionObjectEncoding.Json:
                            if (TryGetToken("Body", out var j)) {
                                extensionObject = new ExtensionObject(typeId, j.ToString());
                            }
                            break;
                        case ExtensionObjectEncoding.Binary:
                            var bytes = ReadByteString("Body");
                            if (bytes != null) {
                                extensionObject = new ExtensionObject(typeId, bytes);
                            }
                            break;
                    }
                }
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
        public SByteCollection ReadSByteArray(string property) =>
            ReadArray(property, () => ReadSByte(null));

        /// <inheritdoc/>
        public ByteCollection ReadByteArray(string property) =>
            ReadArray(property, () => ReadByte(null));

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
        /// Read integer
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        private T ReadInteger<T>(string property, Func<long, T> convert) {
            if (!TryGetToken(property, out var token)) {
                return default(T);
            }
            return convert(token.ToObject<long>());
        }

        /// <summary>
        /// Read double
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="convert"></param>
        /// <returns></returns>
        private T ReadDouble<T>(string property, Func<double, T> convert) {
            if (!TryGetToken(property, out var token)) {
                return default(T);
            }
            return convert(token.ToObject<double>());
        }

        /// <summary>
        /// Convert a token to variant
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static Variant ReadVariantFromToken(JToken token) {
            try {
                switch (token.Type) {
                    case JTokenType.Integer:
                        return new Variant((long)token);
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
                        return new Variant(((JObject)token).ToObject<XmlElement>());
                    case JTokenType.Array:
                        return ReadVariantFromToken((JArray)token);
                    default:
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
        /// <returns></returns>
        private static Variant ReadVariantFromToken(JArray array) {
            if (array.Count == 0) {
                return Variant.Null; // Give up
            }
            try {
                switch (array[0].Type) {
                    case JTokenType.Integer:
                        return new Variant(array
                            .Select(t => (long)t)
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
                    case JTokenType.Object:
                        return new Variant(array
                            .Select(t => ((JObject)t).ToObject<XmlElement>())
                            .ToArray());
                    default:
                        return Variant.Null;
                }
            }
            catch {
                return Variant.Null; // Give up
            }
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
            if (token is JArray a) {
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
                case BuiltInType.Variant:
                    return new Variant(ReadVariant(property),
                        TypeInfo.Scalars.Variant);
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
                case BuiltInType.Variant:
                    return new Variant(ReadVariantArray(property),
                        TypeInfo.Arrays.Variant);
                default:
                    return Variant.Null;
            }
        }

        /// <summary>
        /// Read value with conversion fallback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private T ReadValue<T>(string property, Func<JToken, T> fallback)
            where T : class {
            if (!TryGetToken(property, out var token)) {
                return default(T);
            }
            var value = token.ToObject<T>();
            if (value != null) {
                return value;
            }
            try {
                return fallback(token);
            }
            catch {
                return default(T);
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
                if (!o.TryGetValue(property, out token)) {
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
