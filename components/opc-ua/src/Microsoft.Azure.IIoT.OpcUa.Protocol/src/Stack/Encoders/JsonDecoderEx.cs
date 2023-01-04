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
    using System.Data;
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
        public IServiceMessageContext Context { get; }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <param name="useJsonLoader"></param>
        public JsonDecoderEx(Stream stream, IServiceMessageContext context = null,
            bool useJsonLoader = true) :
            this(new JsonTextReader(new StreamReader(stream)) {
                FloatParseHandling = FloatParseHandling.Double
            }, context, useJsonLoader) {
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        /// <param name="useJsonLoader"></param>
        internal JsonDecoderEx(JsonReader reader, IServiceMessageContext context,
            bool useJsonLoader = true) {
            Context = context ?? new ServiceMessageContext();
            _reader = !useJsonLoader ? reader : new JsonLoader(
                reader ?? throw new ArgumentException(nameof(reader)));
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_reader is JsonLoader loader) {
                loader.Dispose();
            }
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
            // No op
        }

        /// <inheritdoc/>
        public bool ReadBoolean(string property) {
            return TryGetToken(property, out var value) && (bool)value;
        }

        /// <inheritdoc/>
        public sbyte ReadSByte(string property) {
            return ReadValue<sbyte>(property,
                v => v < sbyte.MinValue || v > sbyte.MaxValue ? (sbyte)0 : v);
        }

        /// <inheritdoc/>
        public byte ReadByte(string property) {
            return ReadValue<byte>(property,
            v => (byte)(v < byte.MinValue || v > byte.MaxValue ? 0 : v));
        }

        /// <inheritdoc/>
        public short ReadInt16(string property) {
            return ReadValue<short>(property,
                v => (short)(v < short.MinValue || v > short.MaxValue ? 0 : v));
        }

        /// <inheritdoc/>
        public ushort ReadUInt16(string property) {
            return ReadValue<ushort>(property,
                v => (ushort)(v < ushort.MinValue || v > ushort.MaxValue ? 0 : v));
        }

        /// <inheritdoc/>
        public int ReadInt32(string property) {
            return ReadValue<int>(property,
                v => v < int.MinValue || v > int.MaxValue ? 0 : v);
        }

        /// <inheritdoc/>
        public uint ReadUInt32(string property) {
            return ReadValue<uint>(property,
                v => v < uint.MinValue || v > uint.MaxValue ? 0 : v);
        }

        /// <inheritdoc/>
        public long ReadInt64(string property) {
            return ReadValue<long>(property,
                v => v);
        }

        /// <inheritdoc/>
        public ulong ReadUInt64(string property) {
            return ReadValue<ulong>(property,
                v => v);
        }

        /// <inheritdoc/>
        public float ReadFloat(string property) {
            return ReadValue<float>(property,
                v => v < float.MinValue || v > float.MaxValue ? 0 : v);
        }

        /// <inheritdoc/>
        public double ReadDouble(string property) {
            return ReadValue<double>(property,
                v => v);
        }

        /// <inheritdoc/>
        public byte[] ReadByteString(string property) {
            return TryReadValue(property,
                t => ((string)t).DecodeAsBase64());
        }

        /// <inheritdoc/>
        public string ReadString(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token.Type == JTokenType.String) {
                return (string)token;
            }
            return token.ToString(); // Return json string of token.
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
                    XmlDateTimeSerializationMode.Utc).ToOpcUaUniversalTime();
            }
            var value = token.ToObject<DateTime?>();
            if (value != null) {
                return value.Value.ToOpcUaUniversalTime();
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
                ushort namespaceIndex = 0;
                NodeId nodeId = null;
                if (TryGetToken("Namespace", out var namespaceToken)) {
                    switch (namespaceToken.Type) {
                        case JTokenType.String:
                            var namespaceString = ReadString("Namespace");
                            if (namespaceString != null) {
                                namespaceIndex = Context.NamespaceUris.GetIndexOrAppend(namespaceString);
                            }
                            break;
                        case JTokenType.Integer:
                            namespaceIndex = ReadUInt16("Namespace");
                            break;
                    }
                }
                var idType = (IdType)ReadByte("IdType");
                switch (idType) {
                    case IdType.Numeric:
                        nodeId = new NodeId(ReadUInt32("Id"), namespaceIndex);
                        break;
                    case IdType.String:
                        nodeId = new NodeId(ReadString("Id"), namespaceIndex);
                        break;
                    case IdType.Guid:
                        nodeId = new NodeId(ReadGuid("Id"), namespaceIndex);
                        break;
                    case IdType.Opaque:
                        nodeId = new NodeId(ReadByteString("Id"), namespaceIndex);
                        break;
                }
                if (NodeId.IsNull(nodeId)) {
                    var id = ReadString("Id");
                    _stack.Pop();
                    nodeId = id.ToNodeId(Context);
                    if (!NodeId.IsNull(nodeId)) {
                        return nodeId;
                    }
                    return NodeId.Parse(id);
                }
                _stack.Pop();
                return nodeId;
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
                ushort namespaceIndex = 0;
                string namespaceUri = null;
                if (TryGetToken("Namespace", out var namespaceToken)) {
                    switch (namespaceToken.Type) {
                        case JTokenType.String:
                            namespaceUri = ReadString("Namespace");
                            if (namespaceUri != null) {
                                namespaceIndex = Context.NamespaceUris.GetIndexOrAppend(namespaceUri);
                            }
                            break;
                        case JTokenType.Integer:
                            namespaceIndex = ReadUInt16("Namespace");
                            break;
                    }
                }
                uint serverIndex = 0;
                if (TryGetToken("ServerUri", out var serverToken)) {
                    switch (serverToken.Type) {
                        case JTokenType.String:
                            var serverUri = ReadString("ServerUri");
                            if (serverUri != null) {
                                serverIndex = Context.ServerUris.GetIndexOrAppend(serverUri);
                            }
                            break;
                        case JTokenType.Integer:
                            serverIndex = ReadUInt32("ServerUri");
                            break;
                    }
                }
                var idType = (IdType)ReadByte("IdType");
                NodeId nodeId = null;
                switch (idType) {
                    case IdType.Numeric:
                        nodeId = new NodeId(ReadUInt32("Id"), namespaceIndex);
                        break;
                    case IdType.String:
                        nodeId = new NodeId(ReadString("Id"), namespaceIndex);
                        break;
                    case IdType.Guid:
                        nodeId = new NodeId(ReadGuid("Id"), namespaceIndex);
                        break;
                    case IdType.Opaque:
                        nodeId = new NodeId(ReadByteString("Id"), namespaceIndex);
                        break;
                }
                if (NodeId.IsNull(nodeId)) {
                    var id = ReadString("Id");
                    _stack.Pop();
                    var expandedNodeId = id.ToExpandedNodeId(Context);
                    if (!NodeId.IsNull(expandedNodeId)) {
                        return expandedNodeId;
                    }
                    return ExpandedNodeId.Parse(id);
                }
                _stack.Pop();
                return new ExpandedNodeId(nodeId, namespaceUri, serverIndex);
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
                var text = (string)token;
                if (!string.IsNullOrEmpty(text)) {
                    return text.ToLocalizedText();
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
            if (token is JObject o) {
                if (HasAnyOf(o, "Value", "StatusCode", "SourceTimestamp", "ServerTimestamp")) {
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
                var objectVariant = TryReadVariant(o, out var tmp);
                if (objectVariant != Variant.Null) {
                    return new DataValue(objectVariant);
                }
                return null;
            }
            var tokenVariant = ReadVariantFromToken(token);
            if (tokenVariant == Variant.Null) {
                return null;
            }
            return new DataValue(tokenVariant);
        }

        /// <inheritdoc/>
        public ExtensionObject ReadExtensionObject(string property) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JObject o && HasAnyOf(o, "Body", "TypeId")) {
                _stack.Push(o);

                var typeId = ReadExpandedNodeId("TypeId");
                var encoding = ReadEncoding("Encoding");
                var extensionObject = ReadExtensionObjectBody("Body",
                    encoding, typeId);

                _stack.Pop();
                return extensionObject;
            }
            return null;
        }

        /// <inheritdoc/>
        public IEncodeable ReadEncodeable(string property, Type systemType,
            ExpandedNodeId encodeableTypeId = null) {
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
            if (!enumType.IsEnum) {
                throw new ArgumentException("Not an enum type", nameof(enumType));
            }
            if (!TryGetToken(property, out var token)) {
                return (Enum)Enum.ToObject(enumType, 0); // or null?
            }
            if (token.Type == JTokenType.String) {
                var val = (string)token;
                var index = val.LastIndexOf('_');
                if (index != -1 && int.TryParse(val.AsSpan(index + 1),
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
        public Array ReadArray(string property, int valueRank, BuiltInType builtInType,
            Type systemType, ExpandedNodeId nodeId) {
            if (valueRank == ValueRanks.OneDimension) {
                switch (builtInType) {
                    case BuiltInType.Boolean:
                        return ReadBooleanArray(property).ToArray();
                    case BuiltInType.SByte:
                        return ReadSByteArray(property).ToArray();
                    case BuiltInType.Byte:
                        return ReadByteArray(property).ToArray();
                    case BuiltInType.Int16:
                        return ReadInt16Array(property).ToArray();
                    case BuiltInType.UInt16:
                        return ReadUInt16Array(property).ToArray();
                    case BuiltInType.Int32:
                        return ReadInt32Array(property).ToArray();
                    case BuiltInType.UInt32:
                        return ReadUInt32Array(property).ToArray();
                    case BuiltInType.Int64:
                        return ReadInt64Array(property).ToArray();
                    case BuiltInType.UInt64:
                        return ReadUInt64Array(property).ToArray();
                    case BuiltInType.Float:
                        return ReadFloatArray(property).ToArray();
                    case BuiltInType.Double:
                        return ReadDoubleArray(property).ToArray();
                    case BuiltInType.String:
                        return ReadStringArray(property).ToArray();
                    case BuiltInType.DateTime:
                        return ReadDateTimeArray(property).ToArray();
                    case BuiltInType.Guid:
                        return ReadGuidArray(property).ToArray();
                    case BuiltInType.ByteString:
                        return ReadByteStringArray(property).ToArray();
                    case BuiltInType.XmlElement:
                        return ReadXmlElementArray(property).ToArray();
                    case BuiltInType.NodeId:
                        return ReadNodeIdArray(property).ToArray();
                    case BuiltInType.ExpandedNodeId:
                        return ReadExpandedNodeIdArray(property).ToArray();
                    case BuiltInType.StatusCode:
                        return ReadStatusCodeArray(property).ToArray();
                    case BuiltInType.QualifiedName:
                        return ReadQualifiedNameArray(property).ToArray();
                    case BuiltInType.LocalizedText:
                        return ReadLocalizedTextArray(property).ToArray();
                    case BuiltInType.DataValue:
                        return ReadDataValueArray(property).ToArray();
                    case BuiltInType.Enumeration:
                        return ReadInt32Array(property).ToArray();
                    case BuiltInType.Variant:
                        return ReadVariantArray(property).ToArray();
                    case BuiltInType.ExtensionObject:
                        return ReadExtensionObjectArray(property).ToArray();
                    case BuiltInType.DiagnosticInfo:
                        return ReadDiagnosticInfoArray(property).ToArray();
                    default: {
                            throw new ServiceResultException(
                                StatusCodes.BadDecodingError,
                                Utils.Format("Cannot decode unknown type in Array object with BuiltInType: {0}.", builtInType));
                        }
                }
            }
            else if (valueRank > ValueRanks.OneDimension) {
                List<object> array;
                if (!ReadArrayField(property, out array)) {
                    return null;
                }
                List<object> elements = new List<object>();
                List<int> dimensions = new List<int>();
                ReadMatrixPart(property, array, builtInType, ref elements, ref dimensions, 0);

                switch (builtInType) {
                    case BuiltInType.Boolean:
                        return new Matrix(elements.Cast<bool>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.SByte:
                        return new Matrix(elements.Cast<sbyte>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Byte:
                        return new Matrix(elements.Cast<byte>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Int16:
                        return new Matrix(elements.Cast<Int16>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.UInt16:
                        return new Matrix(elements.Cast<UInt16>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Int32:
                        return new Matrix(elements.Cast<Int32>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.UInt32:
                        return new Matrix(elements.Cast<UInt32>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Int64:
                        return new Matrix(elements.Cast<Int64>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.UInt64:
                        return new Matrix(elements.Cast<UInt64>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Float:
                        return new Matrix(elements.Cast<float>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Double:
                        return new Matrix(elements.Cast<double>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.String:
                        return new Matrix(elements.Cast<string>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.DateTime:
                        return new Matrix(elements.Cast<DateTime>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Guid:
                        return new Matrix(elements.Cast<Uuid>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.ByteString:
                        return new Matrix(elements.Cast<byte[]>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.XmlElement:
                        return new Matrix(elements.Cast<XmlElement>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.NodeId:
                        return new Matrix(elements.Cast<NodeId>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.ExpandedNodeId:
                        return new Matrix(elements.Cast<ExpandedNodeId>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.StatusCode:
                        return new Matrix(elements.Cast<StatusCode>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.QualifiedName:
                        return new Matrix(elements.Cast<QualifiedName>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.LocalizedText:
                        return new Matrix(elements.Cast<LocalizedText>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.DataValue:
                        return new Matrix(elements.Cast<DataValue>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Enumeration:
                        return new Matrix(elements.Cast<Int32>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.Variant:
                        return new Matrix(elements.Cast<Variant>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.ExtensionObject:
                        return new Matrix(elements.Cast<ExtensionObject>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                    case BuiltInType.DiagnosticInfo:
                        return new Matrix(elements.Cast<DiagnosticInfo>().ToArray(), builtInType, dimensions.ToArray()).ToArray();
                }
            }
            return null;
        }

        /// <inheritdoc/>
        public BooleanCollection ReadBooleanArray(string property) {
            return ReadArray(property, () => ReadBoolean(null));
        }

        /// <inheritdoc/>
        public Int16Collection ReadInt16Array(string property) {
            return ReadArray(property, () => ReadInt16(null));
        }

        /// <inheritdoc/>
        public UInt16Collection ReadUInt16Array(string property) {
            return ReadArray(property, () => ReadUInt16(null));
        }

        /// <inheritdoc/>
        public Int32Collection ReadInt32Array(string property) {
            return ReadArray(property, () => ReadInt32(null));
        }

        /// <inheritdoc/>
        public UInt32Collection ReadUInt32Array(string property) {
            return ReadArray(property, () => ReadUInt32(null));
        }

        /// <inheritdoc/>
        public Int64Collection ReadInt64Array(string property) {
            return ReadArray(property, () => ReadInt64(null));
        }

        /// <inheritdoc/>
        public UInt64Collection ReadUInt64Array(string property) {
            return ReadArray(property, () => ReadUInt64(null));
        }

        /// <inheritdoc/>
        public FloatCollection ReadFloatArray(string property) {
            return ReadArray(property, () => ReadFloat(null));
        }

        /// <inheritdoc/>
        public DoubleCollection ReadDoubleArray(string property) {
            return ReadArray(property, () => ReadDouble(null));
        }

        /// <inheritdoc/>
        public StringCollection ReadStringArray(string property) {
            return ReadArray(property, () => ReadString(null));
        }

        /// <inheritdoc/>
        public IDictionary<string, string> ReadStringDictionary(string property) {
            return ReadDictionary(property, () => ReadString(null));
        }

        /// <inheritdoc/>
        public DateTimeCollection ReadDateTimeArray(string property) {
            return ReadArray(property, () => ReadDateTime(null));
        }

        /// <inheritdoc/>
        public UuidCollection ReadGuidArray(string property) {
            return ReadArray(property, () => ReadGuid(null));
        }

        /// <inheritdoc/>
        public ByteStringCollection ReadByteStringArray(string property) {
            return ReadArray(property, () => ReadByteString(null));
        }

        /// <inheritdoc/>
        public XmlElementCollection ReadXmlElementArray(string property) {
            return ReadArray(property, () => ReadXmlElement(null));
        }

        /// <inheritdoc/>
        public NodeIdCollection ReadNodeIdArray(string property) {
            return ReadArray(property, () => ReadNodeId(null));
        }

        /// <inheritdoc/>
        public ExpandedNodeIdCollection ReadExpandedNodeIdArray(string property) {
            return ReadArray(property, () => ReadExpandedNodeId(null));
        }

        /// <inheritdoc/>
        public StatusCodeCollection ReadStatusCodeArray(string property) {
            return ReadArray(property, () => ReadStatusCode(null));
        }

        /// <inheritdoc/>
        public DiagnosticInfoCollection ReadDiagnosticInfoArray(string property) {
            return ReadArray(property, () => ReadDiagnosticInfo(null));
        }

        /// <inheritdoc/>
        public QualifiedNameCollection ReadQualifiedNameArray(string property) {
            return ReadArray(property, () => ReadQualifiedName(null));
        }

        /// <inheritdoc/>
        public LocalizedTextCollection ReadLocalizedTextArray(string property) {
            return ReadArray(property, () => ReadLocalizedText(null));
        }

        /// <inheritdoc/>
        public VariantCollection ReadVariantArray(string property) {
            return ReadArray(property, () => ReadVariant(null));
        }

        /// <inheritdoc/>
        public DataValueCollection ReadDataValueArray(string property) {
            return ReadArray(property, () => ReadDataValue(null));
        }

        /// <inheritdoc/>
        public DataSet ReadDataSet(string property) {
            var fieldMask = 0u;
            bool couldBeRawData = false;
            var dictionary = ReadDictionary(property, () => {
                if (TryGetToken(property, out var token)) {
                    if (token is JObject o) {
                        if (HasAnyOf(o, "StatusCode")) {
                            fieldMask |= (uint)DataSetFieldContentMask.StatusCode;
                        }
                        if (HasAnyOf(o, "SourceTimestamp")) {
                            fieldMask |= (uint)DataSetFieldContentMask.SourceTimestamp;
                        }
                        if (HasAnyOf(o, "ServerTimestamp")) {
                            fieldMask |= (uint)DataSetFieldContentMask.ServerTimestamp;
                        }
                        if (HasAnyOf(o, "SourcePicoseconds")) {
                            fieldMask |= (uint)DataSetFieldContentMask.SourcePicoSeconds;
                        }
                        if (HasAnyOf(o, "ServerPicoseconds")) {
                            fieldMask |= (uint)DataSetFieldContentMask.ServerPicoSeconds;
                        }
                    }
                    else if (token is JValue) {
                        // Could be raw data
                        couldBeRawData = true;
                    }
                }
                return ReadDataValue(null);
            });
            return dictionary == null ? null : new DataSet(dictionary,
                fieldMask != 0 || !couldBeRawData ? fieldMask :
                    (uint)DataSetFieldContentMask.RawData);
        }

        /// <inheritdoc/>
        public ExtensionObjectCollection ReadExtensionObjectArray(string property) {
            return ReadArray(property, () => ReadExtensionObject(null));
        }

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
        public Array ReadEncodeableArray(string property, Type systemType, ExpandedNodeId encodeableTypeId = null) {
            var values = ReadArray(property, () => ReadEncodeable(null, systemType, encodeableTypeId))?
                .ToList();
            if (values == null) {
                return Array.CreateInstance(systemType, 0);
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
        public VariantCollection ReadIntegerArray(string property) {
            return ReadArray(property, () => ReadInteger(null));
        }

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
        public VariantCollection ReadUIntegerArray(string property) {
            return ReadArray(property, () => ReadUInteger(null));
        }

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
        public VariantCollection ReadNumberArray(string property) {
            return ReadArray(property, () => ReadNumber(null));
        }

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
        /// Read a dictionary
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="token">The returned object token of the field.</param>
        private bool ReadField(string fieldName, out object token) {
            token = null;

            if (string.IsNullOrEmpty(fieldName)) {
                token = _stack.Peek();
                return true;
            }

            var context = _stack.Peek().ToObject<Dictionary<string, object>>();
            if (context == null || !context.TryGetValue(fieldName, out token)) {
                return false;
            }

            return true;
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
                        //
                        // TODO:
                        // Need a wrapper for now. Remove once stack supports
                        // json extension objects.
                        //
                        var wrapper = new EncodeableJToken(body);
                        return new ExtensionObject(NodeId.IsNull(typeId) ?
                            wrapper.TypeId : typeId, wrapper);
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
                        return new ExtensionObject(NodeId.IsNull(typeId) ?
                            DataTypeIds.ByteString : typeId, bytes);
                    default:
                        nextEncoding = ExtensionObjectEncoding.Xml;
                        if (body.Type != JTokenType.Object) {
                            break;
                        }
                        XmlElement xml;
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
                        return new ExtensionObject(NodeId.IsNull(typeId) ?
                            DataTypeIds.XmlElement : typeId, xml);
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
                        try {
                            return !unsigned ? new Variant((long)token) :
                                new Variant((ulong)token);
                        }
                        catch (OverflowException) {
                            return new Variant((ulong)token);
                        }
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
            var dimensions = array.GetDimensions(out var type);
            if (dimensions.Length > 1) {
                var builtInType = BuiltInType.Variant;
                switch (type) {
                    case JTokenType.Integer:
                        builtInType = BuiltInType.Int64;
                        break;
                    case JTokenType.Boolean:
                        builtInType = BuiltInType.Boolean;
                        break;
                    case JTokenType.Bytes:
                        builtInType = BuiltInType.ByteString;
                        break;
                    case JTokenType.Date:
                        builtInType = BuiltInType.DateTime;
                        break;
                    case JTokenType.TimeSpan:
                        builtInType = BuiltInType.DateTime;
                        break;
                    case JTokenType.Float:
                        builtInType = BuiltInType.Double;
                        break;
                    case JTokenType.Guid:
                        builtInType = BuiltInType.Guid;
                        break;
                    case JTokenType.String:
                        builtInType = BuiltInType.String;
                        break;
                }
                return ReadVariantMatrixBody(array, dimensions, builtInType);
            }

            if (type != JTokenType.Object && array.All(j => j.Type == type)) {
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
            var validBuiltInType = result.FirstOrDefault(v => v.TypeInfo?.BuiltInType != null).TypeInfo?.BuiltInType;
            if (validBuiltInType == null) {
                return Variant.Null;
            }
            if (result
                .Where(v => v != Variant.Null)
                .All(v => v.TypeInfo.BuiltInType == validBuiltInType)) {
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
            if (token is JArray jarray) {
                // Check array dimensions
                var dimensions = jarray.GetDimensions(out _);
                if (dimensions.Length > 1) {
                    return ReadVariantMatrixBody(jarray, dimensions, type);
                }
                // Read body as array
                return ReadVariantArrayBody(property, type);
            }

            if ((token.Type == JTokenType.Bytes || token.Type == JTokenType.String) &&
                (type == BuiltInType.Byte || type == BuiltInType.SByte)) {
                // Read body as array
                return ReadVariantArrayBody(property, type);
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
        /// Read variant matrix
        /// </summary>
        /// <param name="array"></param>
        /// <param name="dimensions"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Variant ReadVariantMatrixBody(JArray array, int[] dimensions,
            BuiltInType type) {
            var length = 1;
            foreach (var dim in dimensions) {
                length *= dim;
            }
            var flatArray = TypeInfo.CreateArray(type, length);
            var index = 0;
            CopyToMatrixFlatArray(array, flatArray, ref index, type);
            if (index < length) {
                throw new ServiceResultException(StatusCodes.BadDecodingError,
                    "Read matrix is smaller than array dimensions.");
            }
            return new Variant(new Matrix(flatArray, type, dimensions));
        }

        /// <summary>
        /// Copy from array to flat matrix array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="target"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        private void CopyToMatrixFlatArray(JArray array, Array target, ref int index,
            BuiltInType type) {
            foreach (var item in array) {
                if (item is JArray next) {
                    // Recurse into inner array until we hit individual items
                    CopyToMatrixFlatArray(next, target, ref index, type);
                }
                else if (index < target.GetLength(0)) {
                    // Read item at top of stack
                    _stack.Push(item);
                    switch (type) {
                        case BuiltInType.Boolean:
                            target.SetValue(ReadBoolean(null), index++);
                            break;
                        case BuiltInType.SByte:
                            target.SetValue(ReadSByte(null), index++);
                            break;
                        case BuiltInType.Byte:
                            target.SetValue(ReadByte(null), index++);
                            break;
                        case BuiltInType.Int16:
                            target.SetValue(ReadInt16(null), index++);
                            break;
                        case BuiltInType.UInt16:
                            target.SetValue(ReadUInt16(null), index++);
                            break;
                        case BuiltInType.Enumeration:
                        case BuiltInType.Int32:
                            target.SetValue(ReadInt32(null), index++);
                            break;
                        case BuiltInType.UInt32:
                            target.SetValue(ReadUInt32(null), index++);
                            break;
                        case BuiltInType.Int64:
                            target.SetValue(ReadInt64(null), index++);
                            break;
                        case BuiltInType.UInt64:
                            target.SetValue(ReadUInt64(null), index++);
                            break;
                        case BuiltInType.Float:
                            target.SetValue(ReadFloat(null), index++);
                            break;
                        case BuiltInType.Double:
                            target.SetValue(ReadDouble(null), index++);
                            break;
                        case BuiltInType.String:
                            target.SetValue(ReadString(null), index++);
                            break;
                        case BuiltInType.ByteString:
                            target.SetValue(ReadByteString(null), index++);
                            break;
                        case BuiltInType.DateTime:
                            target.SetValue(ReadDateTime(null), index++);
                            break;
                        case BuiltInType.Guid:
                            target.SetValue(ReadGuid(null), index++);
                            break;
                        case BuiltInType.NodeId:
                            target.SetValue(ReadNodeId(null), index++);
                            break;
                        case BuiltInType.ExpandedNodeId:
                            target.SetValue(ReadExpandedNodeId(null), index++);
                            break;
                        case BuiltInType.QualifiedName:
                            target.SetValue(ReadQualifiedName(null), index++);
                            break;
                        case BuiltInType.LocalizedText:
                            target.SetValue(ReadLocalizedText(null), index++);
                            break;
                        case BuiltInType.StatusCode:
                            target.SetValue(ReadStatusCode(null), index++);
                            break;
                        case BuiltInType.XmlElement:
                            target.SetValue(ReadXmlElement(null), index++);
                            break;
                        case BuiltInType.ExtensionObject:
                            target.SetValue(ReadExtensionObject(null), index++);
                            break;
                        case BuiltInType.UInteger:
                            target.SetValue(ReadUInteger(null), index++);
                            break;
                        case BuiltInType.Integer:
                            target.SetValue(ReadInteger(null), index++);
                            break;
                        case BuiltInType.Number:
                            target.SetValue(ReadNumber(null), index++);
                            break;
                        case BuiltInType.Variant:
                            target.SetValue(ReadVariant(null), index++);
                            break;
                        default:
                            target.SetValue(null, index++);
                            break;
                    }
                    _stack.Pop();
                }
                else {
                    throw new ServiceResultException(StatusCodes.BadDecodingError,
                        "Read matrix is larger than array dimensions.");
                }
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
                return default;
            }
            try {
                return check(token.ToObject<T>());
            }
            catch {
                return default;
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
                return default;
            }
            try {
                var value = token.ToObject<T>();
                if (value != null) {
                    return value;
                }
                return fallback(token);
            }
            catch {
                return default;
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
        internal T[] ReadArray<T>(string property, Func<T> reader) {
            if (!TryGetToken(property, out var token)) {
                return null;
            }
            if (token is JArray a) {
                return a.Select(t => ReadToken(t, reader)).ToArray();
            }
            return ReadToken(token, reader).YieldReturn().ToArray();
        }

        /// <summary>
        /// Read dictionary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        private IDictionary<string, T> ReadDictionary<T>(string property,
            Func<T> reader) {
            if (!TryGetToken(property, out var token) || !(token is JObject o)) {
                return null;
            }
            var dictionary = new Dictionary<string, T>();
            foreach (var p in o.Properties()) {
                dictionary[p.Name] = ReadToken(p.Value, reader);
            }
            return dictionary;
        }

        /// <summary>
        /// Read token using a specified reader
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal T ReadToken<T>(JToken token, Func<T> reader) {
            try {
                _stack.Push(token);
                return reader();
            }
            finally {
                _stack.Pop();
            }
        }

        /// <summary>
        /// Test whether the object contains any of the the properties
        /// </summary>
        /// <param name="o"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        internal static bool HasAnyOf(JObject o, params string[] properties) {
            foreach (var property in properties) {
                if (o.TryGetValue(property,
                    StringComparison.InvariantCultureIgnoreCase, out _)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Try get top token or named token from object
        /// </summary>
        /// <param name="property"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal bool TryGetToken(string property, out JToken token) {
            JToken top;
            if (_stack.Count == 0) {
                top = ReadNextToken();
                //
                // Check whether we read a property from the top object.
                // If so, push the top object for reading.  Otherwise, we
                // are reading from an array of object so we do not push
                // which means our stack will reset to 0.
                //
                if (top != null && (property != null || !(_reader is JsonLoader))) {
                    _stack.Push(top);
                }
            }
            else {
                top = _stack.Peek();
            }
            if (top == null) {
                // Hit end of file.
                token = null;
                return false;
            }
            if (property == null) {
                // Read top token
                token = top;
                return true;
            }
            if (top is JObject o) {
                if (!o.TryGetValue(property, out token)) {
                    if (!o.TryGetValue(property,
                        StringComparison.InvariantCultureIgnoreCase, out token)) {
                        return false;
                    }
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
        /// Read next root token from reader
        /// </summary>
        /// <returns></returns>
        private JToken ReadNextToken() {
            if (_reader == null) {
                return null;
            }
            if (_reader.TokenType == JsonToken.EndObject &&
                string.IsNullOrEmpty(_reader.Path)) {
                return null;
            }
            if (_reader is JsonLoader loader) {
                loader.Reset();
            }

            var root = JToken.ReadFrom(_reader,
                new JsonLoadSettings {
                    CommentHandling = CommentHandling.Ignore,
                    LineInfoHandling = LineInfoHandling.Ignore
                });
            return root;
        }

        /// <summary>
        /// Read array field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="array"></param>
        /// <returns></returns>
        /// <exception cref="ServiceResultException"></exception>
        private bool ReadArrayField(string fieldName, out List<object> array) {
            array = null;
            object token;

            if (!ReadField(fieldName, out token)) {
                return false;
            }
            array = token as List<object>;
            if (array == null) {
                return false;
            }
            if (Context.MaxArrayLength > 0 && Context.MaxArrayLength < array.Count) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }
            return true;
        }

        /// <summary>
        /// Read the Matrix part (simple array or array of arrays)
        /// </summary>
        private void ReadMatrixPart(string fieldName, List<object> currentArray, BuiltInType builtInType, ref List<object> elements, ref List<int> dimensions, int level) {
            try {
                if (currentArray?.Count > 0) {
                    var hasInnerArray = false;
                    for (var ii = 0; ii < currentArray.Count; ii++) {
                        if (ii == 0 && dimensions.Count <= level) {
                            // remember dimension length
                            dimensions.Add(currentArray.Count);
                        }
                        if (currentArray[ii] is List<object>) {
                            hasInnerArray = true;

                            if (!TryGetToken(fieldName, out var token)) {
                                return;
                            }
                            _stack.Push(token);
                            ReadMatrixPart(null, currentArray[ii] as List<object>, builtInType, ref elements, ref dimensions, level + 1);
                            _stack.Pop();
                        }
                        else {
                            break; // do not continue reading array of array
                        }
                    }
                    if (!hasInnerArray) {
                        // read array from one dimension
                        var part = ReadArray(null, ValueRanks.OneDimension, builtInType, null, null) as System.Collections.IList;
                        if (part != null && part.Count > 0) {
                            // add part elements to final list
                            foreach (var item in part) {
                                elements.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw new ServiceResultException(ex.Message);
            }
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
            public override JsonToken TokenType {
                get {
                    if (_eofDepth >= 0) {
                        return JsonToken.EndObject;
                    }
                    if (_eos) {
                        return JsonToken.Null;
                    }
                    if (_reset) {
                        return JsonToken.None;
                    }
                    return _reader.TokenType;
                }
            }

            /// <inheritdoc/>
            public override int Depth {
                get {
                    if (_eofDepth >= 0) {
                        return --_eofDepth;
                    }
                    if (_reader.Depth > 0 && _inArray) {
                        return _reader.Depth - 1;
                    }
                    return _reader.Depth;
                }
            }

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

                // Handle streaming
                if (_reader.Depth == 0 &&
                   ((_inArray && _reader.TokenType == JsonToken.EndArray) ||
                   (!_inArray && _reader.TokenType == JsonToken.StartArray))) {
                    _inArray = !_inArray;
                    _eos |= !_inArray && _reset;
                    // Skip to start object
                    _reader.Read();
                }

                // Next token is start of object
                _reset = false;
                return true;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _reader.Close();
            }

            /// <summary>
            /// Reset loader
            /// </summary>
            public void Reset() {
                _reset = true;
            }

            private readonly JsonReader _reader;
            private int _eofDepth;
            private bool _inArray;
            private bool _reset;
            private bool _eos;
        }

        private readonly JsonReader _reader;
        private readonly Stack<JToken> _stack = new Stack<JToken>();
    }
}
