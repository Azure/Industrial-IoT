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

namespace Opc.Ua {
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.IO;
    using Newtonsoft.Json;
    using System.Collections;

    /// <summary>
    /// Reads objects from reader or string
    /// </summary>
    public class JsonDecoderEx : IDecoder, IDisposable {

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
        public JsonDecoderEx(ServiceMessageContext context, JsonTextReader reader) {
            Context = context;
            _nestingLevel = 0;
            _reader = reader;
            _root = ReadObject();
            _stack = new Stack<object>();
            _stack.Push(_root);
        }

        /// <summary>
        /// Decodes an object from a buffer.
        /// </summary>
        public IEncodeable DecodeMessage(Type expectedType) {
            var namespaceUris = ReadStringArray("NamespaceUris");
            var serverUris = ReadStringArray("ServerUris");

            if ((namespaceUris != null && namespaceUris.Count > 0) || (serverUris != null && serverUris.Count > 0)) {
                var namespaces = (namespaceUris == null || namespaceUris.Count == 0) ? Context.NamespaceUris : new NamespaceTable(namespaceUris);
                var servers = (serverUris == null || serverUris.Count == 0) ? Context.ServerUris : new StringTable(serverUris);

                SetMappingTables(namespaces, servers);
            }

            // read the node id.
            var typeId = ReadNodeId("TypeId");

            // convert to absolute node id.
            var absoluteId = NodeId.ToExpandedNodeId(typeId, Context.NamespaceUris);

            // lookup message type.
            var actualType = Context.Factory.GetSystemType(absoluteId);

            if (actualType == null) {
                throw new ServiceResultException(StatusCodes.BadEncodingError, Utils.Format("Cannot decode message with type id: {0}.", absoluteId));
            }

            // read the message.
            var message = ReadEncodeable("Body", actualType);

            // return the message.
            return message;
        }


        /// <summary>
        /// Initializes the tables used to map namespace and server uris during decoding.
        /// </summary>
        /// <param name="namespaceUris">The namespaces URIs referenced by the data being decoded.</param>
        /// <param name="serverUris">The server URIs referenced by the data being decoded.</param>
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

        /// <summary>
        /// Closes the stream used for reading.
        /// </summary>
        public void Close() {
            _reader.Close();
        }

        /// <summary>
        /// Closes the stream used for reading.
        /// </summary>
        public void Close(bool checkEof) {
            if (checkEof && _reader.TokenType != JsonToken.EndObject) {
                while (_reader.Read() && _reader.TokenType != JsonToken.EndObject) {
                }
            }

            _reader.Close();
        }

        private List<object> ReadArray() {
            var elements = new List<object>();

            while (_reader.Read() && _reader.TokenType != JsonToken.EndArray) {
                switch (_reader.TokenType) {
                    case JsonToken.Comment: {
                            break;
                        }

                    case JsonToken.Boolean:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.String:
                    case JsonToken.Bytes:
                    case JsonToken.Date: {
                            elements.Add(_reader.Value);
                            break;
                        }

                    case JsonToken.StartArray: {
                            elements.Add(ReadArray());
                            break;
                        }

                    case JsonToken.StartObject: {
                            elements.Add(ReadObject());
                            break;
                        }
                }
            }

            return elements;
        }

        private Dictionary<string, object> ReadObject() {
            var fields = new Dictionary<string, object>();

            while (_reader.Read() && _reader.TokenType != JsonToken.EndObject) {
                if (_reader.TokenType == JsonToken.PropertyName) {
                    var name = (string)_reader.Value;

                    if (_reader.Read() && _reader.TokenType != JsonToken.EndObject) {
                        switch (_reader.TokenType) {
                            case JsonToken.Comment: {
                                    break;
                                }

                            case JsonToken.Null:
                            case JsonToken.Date: {
                                    fields[name] = _reader.Value;
                                    break;
                                }

                            case JsonToken.Bytes:
                            case JsonToken.Boolean:
                            case JsonToken.Integer:
                            case JsonToken.Float:
                            case JsonToken.String: {
                                    fields[name] = _reader.Value;
                                    break;
                                }

                            case JsonToken.StartArray: {
                                    fields[name] = ReadArray();
                                    break;
                                }

                            case JsonToken.StartObject: {
                                    fields[name] = ReadObject();
                                    break;
                                }
                        }
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Reads the body extension object from the stream.
        /// </summary>
        public object ReadExtensionObjectBody(ExpandedNodeId typeId) {
            return null;
        }

        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_reader != null) {
                    _reader.Close();
                }
            }
        }

        /// <summary>
        /// The type of encoding being used.
        /// </summary>
        public EncodingType EncodingType => EncodingType.Json;

        /// <summary>
        /// The message context associated with the decoder.
        /// </summary>
        public ServiceMessageContext Context { get; }

        /// <summary>
        /// Pushes a namespace onto the namespace stack.
        /// </summary>
        public void PushNamespace(string namespaceUri) {
        }

        /// <summary>
        /// Pops a namespace from the namespace stack.
        /// </summary>
        public void PopNamespace() {
        }

        public bool ReadField(string fieldName, out object token) {
            token = null;

            if (string.IsNullOrEmpty(fieldName)) {
                token = _stack.Peek();
                return true;
            }

            if (!(_stack.Peek() is Dictionary<string, object> context) ||
                !context.TryGetValue(fieldName, out token)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads a boolean from the stream.
        /// </summary>
        public bool ReadBoolean(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return false;
            }

            var value = token as bool?;

            if (value == null) {
                return false;
            }

            return (bool)token;
        }

        /// <summary>
        /// Reads a sbyte from the stream.
        /// </summary>
        public sbyte ReadSByte(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {
                return 0;
            }

            if (value < sbyte.MinValue || value > sbyte.MaxValue) {
                return 0;
            }

            return (sbyte)value;
        }

        /// <summary>
        /// Reads a byte from the stream.
        /// </summary>
        public byte ReadByte(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {
                return 0;
            }

            if (value < byte.MinValue || value > byte.MaxValue) {
                return 0;
            }

            return (byte)value;
        }

        /// <summary>
        /// Reads a short from the stream.
        /// </summary>
        public short ReadInt16(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {
                return 0;
            }

             if (value < short.MinValue || value > short.MaxValue) {
                return 0;
            }

            return (short)value;
        }

        /// <summary>
        /// Reads a ushort from the stream.
        /// </summary>
        public ushort ReadUInt16(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {
                return 0;
            }

            if (value < ushort.MinValue || value > ushort.MaxValue) {
                return 0;
            }

            return (ushort)value;
        }

        /// <summary>
        /// Reads an int from the stream.
        /// </summary>
        public int ReadInt32(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {
                return 0;
            }

            if (value < int.MinValue || value > int.MaxValue) {
                return 0;
            }

            return (int)value;
        }

        /// <summary>
        /// Reads a uint from the stream.
        /// </summary>
        public uint ReadUInt32(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {

                if (!(token is string text) || !uint.TryParse(text, out var number)) {
                    return 0;
                }

                return number;
            }

            if (value < uint.MinValue || value > uint.MaxValue) {
                return 0;
            }

            return (uint)value;
        }

        /// <summary>
        /// Reads a long from the stream.
        /// </summary>
        public long ReadInt64(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {

                if (!(token is string text) || !long.TryParse(text, out var number)) {
                    return 0;
                }

                return number;
            }

            if (value < long.MinValue || value > long.MaxValue) {
                return 0;
            }

            return (long)value;
        }

        /// <summary>
        /// Reads a ulong from the stream.
        /// </summary>
        public ulong ReadUInt64(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as long?;

            if (value == null) {

                if (!(token is string text) || !ulong.TryParse(text, out var number)) {
                    return 0;
                }

                return number;
            }

            if (value < 0) {
                return 0;
            }

            return (ulong)value;
        }

        /// <summary>
        /// Reads a float from the stream.
        /// </summary>
        public float ReadFloat(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as double?;

            if (value == null) {

                if (!(token is string text) || !float.TryParse(text, out var number)) {
                    var integer = token as long?;

                    if (integer == null) {
                        return 0;
                    }

                    return (float)integer;
                }

                return number;
            }

            if (value < float.MinValue || value > float.MaxValue) {
                return 0;
            }

            return (float)value;
        }

        /// <summary>
        /// Reads a double from the stream.
        /// </summary>
        public double ReadDouble(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return 0;
            }

            var value = token as double?;

            if (value == null) {

                if (!(token is string text) || !double.TryParse(text, out var number)) {
                    var integer = token as long?;

                    if (integer == null) {
                        return 0;
                    }

                    return (double)integer;
                }

                return number;
            }

            return (double)value;
        }

        /// <summary>
        /// Reads a string from the stream.
        /// </summary>
        public string ReadString(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is string value)) {
                return null;
            }

            if (Context.MaxStringLength > 0 && Context.MaxStringLength < value.Length) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }

            return value;
        }

        /// <summary>
        /// Reads a UTC date/time from the stream.
        /// </summary>
        public DateTime ReadDateTime(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return DateTime.MinValue;
            }

            var value = token as DateTime?;

            if (value != null) {
                return value.Value;
            }

            if (token is string text) {
                return XmlConvert.ToDateTime(text, XmlDateTimeSerializationMode.Utc);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Reads a GUID from the stream.
        /// </summary>
        public Uuid ReadGuid(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return Uuid.Empty;
            }


            if (!(token is string value)) {
                return Uuid.Empty;
            }

            return new Uuid(value);
        }

        /// <summary>
        /// Reads a byte string from the stream.
        /// </summary>
        public byte[] ReadByteString(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is string value)) {
                return null;
            }

            var bytes = Convert.FromBase64String(value);

            if (Context.MaxByteStringLength > 0 && Context.MaxByteStringLength < bytes.Length) {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }

            return bytes;
        }

        /// <summary>
        /// Reads an XmlElement from the stream.
        /// </summary>
        public XmlElement ReadXmlElement(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is string value)) {
                return null;
            }

            var bytes = Convert.FromBase64String(value);

            if (bytes != null && bytes.Length > 0) {
                var document = new XmlDocument {
                    InnerXml = Encoding.UTF8.GetString(bytes)
                };
                return document.DocumentElement;
            }

            return null;
        }

        /// <summary>
        /// Reads an NodeId from the stream.
        /// </summary>
        public NodeId ReadNodeId(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }

            if (!(token is string value)) {
                return null;
            }

            return NodeId.Parse(value);
        }

        /// <summary>
        /// Reads an ExpandedNodeId from the stream.
        /// </summary>
        public ExpandedNodeId ReadExpandedNodeId(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is string value)) {
                return null;
            }

            return ExpandedNodeId.Parse(value);
        }

        /// <summary>
        /// Reads an StatusCode from the stream.
        /// </summary>
        public StatusCode ReadStatusCode(string fieldName) {
            return ReadUInt32(fieldName);
        }

        /// <summary>
        /// Reads an DiagnosticInfo from the stream.
        /// </summary>
        public DiagnosticInfo ReadDiagnosticInfo(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is Dictionary<string, object> value)) {
                return null;
            }

            // check the nesting level for avoiding a stack overflow.
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }

            try {
                _nestingLevel++;
                _stack.Push(value);

                var di = new DiagnosticInfo();

                if (value.ContainsKey("SymbolicId")) {
                    di.SymbolicId = ReadInt32("SymbolicId");
                }

                if (value.ContainsKey("NamespaceUri")) {
                    di.NamespaceUri = ReadInt32("NamespaceUri");
                }

                if (value.ContainsKey("Locale")) {
                    di.Locale = ReadInt32("Locale");
                }

                if (value.ContainsKey("LocalizedText")) {
                    di.LocalizedText = ReadInt32("LocalizedText");
                }

                if (value.ContainsKey("AdditionalInfo")) {
                    di.AdditionalInfo = ReadString("AdditionalInfo");
                }

                if (value.ContainsKey("InnerStatusCode")) {
                    di.InnerStatusCode = ReadStatusCode("InnerStatusCode");
                }

                if (value.ContainsKey("InnerDiagnosticInfo")) {
                    di.InnerDiagnosticInfo = ReadDiagnosticInfo("InnerDiagnosticInfo");
                }

                return di;
            }
            finally {
                _nestingLevel--;
                _stack.Pop();
            }
        }

        /// <summary>
        /// Reads an QualifiedName from the stream.
        /// </summary>
        public QualifiedName ReadQualifiedName(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }

            if (!(token is Dictionary<string, object> value)) {
                if (token is string name) {
                    return new QualifiedName(name, 0);
                }
                return null;
            }

            try {
                _stack.Push(value);

                var name = ReadString("Name");
                var namespaceIndex = ReadUInt16("Uri");

                return new QualifiedName(name, namespaceIndex);
            }
            finally {
                _stack.Pop();
            }
        }

        /// <summary>
        /// Reads an LocalizedText from the stream.
        /// </summary>
        public LocalizedText ReadLocalizedText(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }

            string locale = null;
            string text = null;

            if (!(token is Dictionary<string, object> value)) {
                text = token as string;

                if (text != null) {
                    return new LocalizedText(text);
                }

                return null;
            }

            try {
                _stack.Push(value);

                if (value.ContainsKey("Locale")) {
                    locale = ReadString("Locale");
                }

                if (value.ContainsKey("Text")) {
                    text = ReadString("Text");
                }
            }
            finally {
                _stack.Pop();
            }

            return new LocalizedText(locale, text);
        }

        private Variant ReadVariantBody(string fieldName, BuiltInType type) {
            switch (type) {
                case BuiltInType.Boolean: { return new Variant(ReadBoolean(fieldName), TypeInfo.Scalars.Boolean); }
                case BuiltInType.SByte: { return new Variant(ReadSByte(fieldName), TypeInfo.Scalars.SByte); }
                case BuiltInType.Byte: { return new Variant(ReadByte(fieldName), TypeInfo.Scalars.Byte); }
                case BuiltInType.Int16: { return new Variant(ReadInt16(fieldName), TypeInfo.Scalars.Int16); }
                case BuiltInType.UInt16: { return new Variant(ReadUInt16(fieldName), TypeInfo.Scalars.UInt16); }
                case BuiltInType.Int32: { return new Variant(ReadInt32(fieldName), TypeInfo.Scalars.Int32); }
                case BuiltInType.UInt32: { return new Variant(ReadUInt32(fieldName), TypeInfo.Scalars.UInt32); }
                case BuiltInType.Int64: { return new Variant(ReadInt64(fieldName), TypeInfo.Scalars.Int64); }
                case BuiltInType.UInt64: { return new Variant(ReadUInt64(fieldName), TypeInfo.Scalars.UInt64); }
                case BuiltInType.Float: { return new Variant(ReadFloat(fieldName), TypeInfo.Scalars.Float); }
                case BuiltInType.Double: { return new Variant(ReadDouble(fieldName), TypeInfo.Scalars.Double); }
                case BuiltInType.String: { return new Variant(ReadString(fieldName), TypeInfo.Scalars.String); }
                case BuiltInType.ByteString: { return new Variant(ReadByteString(fieldName), TypeInfo.Scalars.ByteString); }
                case BuiltInType.DateTime: { return new Variant(ReadDateTime(fieldName), TypeInfo.Scalars.DateTime); }
                case BuiltInType.Guid: { return new Variant(ReadGuid(fieldName), TypeInfo.Scalars.Guid); }
                case BuiltInType.NodeId: { return new Variant(ReadNodeId(fieldName), TypeInfo.Scalars.NodeId); }
                case BuiltInType.ExpandedNodeId: { return new Variant(ReadExpandedNodeId(fieldName), TypeInfo.Scalars.ExpandedNodeId); }
                case BuiltInType.QualifiedName: { return new Variant(ReadQualifiedName(fieldName), TypeInfo.Scalars.QualifiedName); }
                case BuiltInType.LocalizedText: { return new Variant(ReadLocalizedText(fieldName), TypeInfo.Scalars.LocalizedText); }
                case BuiltInType.StatusCode: { return new Variant(ReadStatusCode(fieldName), TypeInfo.Scalars.StatusCode); }
                case BuiltInType.XmlElement: { return new Variant(ReadXmlElement(fieldName), TypeInfo.Scalars.XmlElement); }
                case BuiltInType.ExtensionObject: { return new Variant(ReadExtensionObject(fieldName), TypeInfo.Scalars.ExtensionObject); }
                case BuiltInType.Variant: { return new Variant(ReadVariant(fieldName), TypeInfo.Scalars.Variant); }
            }

            return Variant.Null;
        }

        private Variant ReadVariantArrayBody(string fieldName, BuiltInType type) {
            switch (type) {
                case BuiltInType.Boolean: { return new Variant(ReadBooleanArray(fieldName), TypeInfo.Arrays.Boolean); }
                case BuiltInType.SByte: { return new Variant(ReadSByteArray(fieldName), TypeInfo.Arrays.SByte); }
                case BuiltInType.Byte: { return new Variant(ReadByteArray(fieldName), TypeInfo.Arrays.Byte); }
                case BuiltInType.Int16: { return new Variant(ReadInt16Array(fieldName), TypeInfo.Arrays.Int16); }
                case BuiltInType.UInt16: { return new Variant(ReadUInt16Array(fieldName), TypeInfo.Arrays.UInt16); }
                case BuiltInType.Int32: { return new Variant(ReadInt32Array(fieldName), TypeInfo.Arrays.Int32); }
                case BuiltInType.UInt32: { return new Variant(ReadUInt32Array(fieldName), TypeInfo.Arrays.UInt32); }
                case BuiltInType.Int64: { return new Variant(ReadInt64Array(fieldName), TypeInfo.Arrays.Int64); }
                case BuiltInType.UInt64: { return new Variant(ReadUInt64Array(fieldName), TypeInfo.Arrays.UInt64); }
                case BuiltInType.Float: { return new Variant(ReadFloatArray(fieldName), TypeInfo.Arrays.Float); }
                case BuiltInType.Double: { return new Variant(ReadDoubleArray(fieldName), TypeInfo.Arrays.Double); }
                case BuiltInType.String: { return new Variant(ReadStringArray(fieldName), TypeInfo.Arrays.String); }
                case BuiltInType.ByteString: { return new Variant(ReadByteStringArray(fieldName), TypeInfo.Arrays.ByteString); }
                case BuiltInType.DateTime: { return new Variant(ReadDateTimeArray(fieldName), TypeInfo.Arrays.DateTime); }
                case BuiltInType.Guid: { return new Variant(ReadGuidArray(fieldName), TypeInfo.Arrays.Guid); }
                case BuiltInType.NodeId: { return new Variant(ReadNodeIdArray(fieldName), TypeInfo.Arrays.NodeId); }
                case BuiltInType.ExpandedNodeId: { return new Variant(ReadExpandedNodeIdArray(fieldName), TypeInfo.Arrays.ExpandedNodeId); }
                case BuiltInType.QualifiedName: { return new Variant(ReadQualifiedNameArray(fieldName), TypeInfo.Arrays.QualifiedName); }
                case BuiltInType.LocalizedText: { return new Variant(ReadLocalizedTextArray(fieldName), TypeInfo.Arrays.LocalizedText); }
                case BuiltInType.StatusCode: { return new Variant(ReadStatusCodeArray(fieldName), TypeInfo.Arrays.StatusCode); }
                case BuiltInType.XmlElement: { return new Variant(ReadXmlElementArray(fieldName), TypeInfo.Arrays.XmlElement); }
                case BuiltInType.ExtensionObject: { return new Variant(ReadExtensionObjectArray(fieldName), TypeInfo.Arrays.ExtensionObject); }
                case BuiltInType.Variant: { return new Variant(ReadVariantArray(fieldName), TypeInfo.Arrays.Variant); }
            }

            return Variant.Null;
        }

        /// <summary>
        /// Reads an Variant from the stream.
        /// </summary>
        public Variant ReadVariant(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return Variant.Null;
            }


            if (!(token is Dictionary<string, object> value)) {
                return Variant.Null;
            }

            // check the nesting level for avoiding a stack overflow.
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }
            try {
                _nestingLevel++;
                _stack.Push(value);

                var type = (BuiltInType)ReadByte("Type");

                var context = _stack.Peek() as Dictionary<string, object>;
                if (!context.TryGetValue("Body", out token)) {
                    return Variant.Null;
                }

                if (token is ICollection) {
                    var array = ReadVariantArrayBody("Body", type);
                    var dimensions = ReadInt32Array("Dimensions");

                    if (array.Value is ICollection && dimensions != null && dimensions.Count > 1) {
                        array = new Variant(new Matrix((Array)array.Value, type, dimensions.ToArray()));
                    }

                    return array;
                }
                else {
                    return ReadVariantBody("Body", type);
                }
            }
            finally {
                _nestingLevel--;
                _stack.Pop();
            }
        }

        /// <summary>
        /// Reads an DataValue from the stream.
        /// </summary>
        public DataValue ReadDataValue(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is Dictionary<string, object> value)) {
                return null;
            }

            var dv = new DataValue();

            try {
                _stack.Push(value);

                dv.WrappedValue = ReadVariant("Value");
                dv.StatusCode = ReadStatusCode("StatusCode");
                dv.SourceTimestamp = ReadDateTime("SourceTimestamp");
                dv.SourcePicoseconds = ReadUInt16("SourcePicoseconds");
                dv.ServerTimestamp = ReadDateTime("ServerTimestamp");
                dv.ServerPicoseconds = ReadUInt16("ServerPicoseconds");
            }
            finally {
                _stack.Pop();
            }

            return dv;
        }

        private void EncodeAsJson(JsonTextWriter writer, object value) {

            if (value is Dictionary<string, object> map) {
                EncodeAsJson(writer, map);
                return;
            }


            if (value is List<object> list) {
                writer.WriteStartArray();

                foreach (var element in list) {
                    EncodeAsJson(writer, element);
                }

                writer.WriteStartArray();
                return;
            }

            writer.WriteValue(value);
        }

        private void EncodeAsJson(JsonTextWriter writer, Dictionary<string, object> value) {
            writer.WriteStartObject();

            foreach (var field in value) {
                writer.WritePropertyName(field.Key);
                EncodeAsJson(writer, field.Value);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads an extension object from the stream.
        /// </summary>
        public ExtensionObject ReadExtensionObject(string fieldName) {

            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(token is Dictionary<string, object> value)) {
                return null;
            }

            try {
                _stack.Push(value);

                var typeId = ReadNodeId("TypeId");

                var absoluteId = NodeId.ToExpandedNodeId(typeId, Context.NamespaceUris);

                if (!NodeId.IsNull(typeId) && NodeId.IsNull(absoluteId)) {
                    Utils.Trace("Cannot de-serialized extension objects if the NamespaceUri is not in the NamespaceTable: Type = {0}", typeId);
                }

                var encoding = ReadByte("Encoding");

                if (encoding == 1) {
                    var bytes = ReadByteString("Body");
                    return new ExtensionObject(typeId, bytes);
                }

                if (encoding == 2) {
                    var xml = ReadXmlElement("Body");
                    return new ExtensionObject(typeId, xml);
                }

                var systemType = Context.Factory.GetSystemType(typeId);

                if (systemType != null) {
                    var encodeable = ReadEncodeable("Body", systemType);
                    return new ExtensionObject(typeId, encodeable);
                }

                var ostrm = new MemoryStream();

                using (var writer = new JsonTextWriter(new StreamWriter(ostrm))) {
                    EncodeAsJson(writer, token);
                }

                return new ExtensionObject(typeId, ostrm.ToArray());
            }
            finally {
                _stack.Pop();
            }
        }

        /// <summary>
        /// Reads an encodeable object from the stream.
        /// </summary>
        public IEncodeable ReadEncodeable(
            string fieldName,
            Type systemType) {
            if (systemType == null) {
                throw new ArgumentNullException(nameof(systemType));
            }


            if (!ReadField(fieldName, out var token)) {
                return null;
            }


            if (!(Activator.CreateInstance(systemType) is IEncodeable value)) {
                throw new ServiceResultException(StatusCodes.BadDecodingError, Utils.Format("Type does not support IEncodeable interface: '{0}'", systemType.FullName));
            }

            // check the nesting level for avoiding a stack overflow.
            if (_nestingLevel > Context.MaxEncodingNestingLevels) {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    Context.MaxEncodingNestingLevels);
            }

            _nestingLevel++;

            try {
                _stack.Push(token);

                value.Decode(this);
            }
            finally {
                _stack.Pop();
            }

            _nestingLevel--;

            return value;
        }

        /// <summary>
        ///  Reads an enumerated value from the stream.
        /// </summary>
        public Enum ReadEnumerated(string fieldName, Type enumType) {
            if (enumType == null) {
                throw new ArgumentNullException(nameof(enumType));
            }

            return (Enum)Enum.ToObject(enumType, ReadInt32(fieldName));
        }

        private bool ReadArrayField(string fieldName, out List<object> array) {
            object token = array = null;

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
        /// Reads a boolean array from the stream.
        /// </summary>
        public BooleanCollection ReadBooleanArray(string fieldName) {
            var values = new BooleanCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadBoolean(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a sbyte array from the stream.
        /// </summary>
        public SByteCollection ReadSByteArray(string fieldName) {
            var values = new SByteCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadSByte(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a byte array from the stream.
        /// </summary>
        public ByteCollection ReadByteArray(string fieldName) {
            var values = new ByteCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadByte(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a short array from the stream.
        /// </summary>
        public Int16Collection ReadInt16Array(string fieldName) {
            var values = new Int16Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadInt16(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a ushort array from the stream.
        /// </summary>
        public UInt16Collection ReadUInt16Array(string fieldName) {
            var values = new UInt16Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadUInt16(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a int array from the stream.
        /// </summary>
        public Int32Collection ReadInt32Array(string fieldName) {
            var values = new Int32Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadInt32(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a uint array from the stream.
        /// </summary>
        public UInt32Collection ReadUInt32Array(string fieldName) {
            var values = new UInt32Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadUInt32(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a long array from the stream.
        /// </summary>
        public Int64Collection ReadInt64Array(string fieldName) {
            var values = new Int64Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadInt64(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a ulong array from the stream.
        /// </summary>
        public UInt64Collection ReadUInt64Array(string fieldName) {
            var values = new UInt64Collection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadUInt64(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a float array from the stream.
        /// </summary>
        public FloatCollection ReadFloatArray(string fieldName) {
            var values = new FloatCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadFloat(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a double array from the stream.
        /// </summary>
        public DoubleCollection ReadDoubleArray(string fieldName) {
            var values = new DoubleCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadDouble(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a string array from the stream.
        /// </summary>
        public StringCollection ReadStringArray(string fieldName) {
            var values = new StringCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadString(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a UTC date/time array from the stream.
        /// </summary>
        public DateTimeCollection ReadDateTimeArray(string fieldName) {
            var values = new DateTimeCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    values.Add(ReadDateTime(null));
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a GUID array from the stream.
        /// </summary>
        public UuidCollection ReadGuidArray(string fieldName) {
            var values = new UuidCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadGuid(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a byte string array from the stream.
        /// </summary>
        public ByteStringCollection ReadByteStringArray(string fieldName) {
            var values = new ByteStringCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadByteString(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an XmlElement array from the stream.
        /// </summary>
        public XmlElementCollection ReadXmlElementArray(string fieldName) {
            var values = new XmlElementCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadXmlElement(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an NodeId array from the stream.
        /// </summary>
        public NodeIdCollection ReadNodeIdArray(string fieldName) {
            var values = new NodeIdCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadNodeId(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an ExpandedNodeId array from the stream.
        /// </summary>
        public ExpandedNodeIdCollection ReadExpandedNodeIdArray(string fieldName) {
            var values = new ExpandedNodeIdCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadExpandedNodeId(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an StatusCode array from the stream.
        /// </summary>
        public StatusCodeCollection ReadStatusCodeArray(string fieldName) {
            var values = new StatusCodeCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadStatusCode(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an DiagnosticInfo array from the stream.
        /// </summary>
        public DiagnosticInfoCollection ReadDiagnosticInfoArray(string fieldName) {
            var values = new DiagnosticInfoCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadDiagnosticInfo(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an QualifiedName array from the stream.
        /// </summary>
        public QualifiedNameCollection ReadQualifiedNameArray(string fieldName) {
            var values = new QualifiedNameCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadQualifiedName(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an LocalizedText array from the stream.
        /// </summary>
        public LocalizedTextCollection ReadLocalizedTextArray(string fieldName) {
            var values = new LocalizedTextCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadLocalizedText(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an Variant array from the stream.
        /// </summary>
        public VariantCollection ReadVariantArray(string fieldName) {
            var values = new VariantCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadVariant(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an DataValue array from the stream.
        /// </summary>
        public DataValueCollection ReadDataValueArray(string fieldName) {
            var values = new DataValueCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadDataValue(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an array of extension objects from the stream.
        /// </summary>
        public ExtensionObjectCollection ReadExtensionObjectArray(string fieldName) {
            var values = new ExtensionObjectCollection();


            if (!ReadArrayField(fieldName, out var token)) {
                return values;
            }

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadExtensionObject(null);
                    values.Add(element);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an encodeable object array from the stream.
        /// </summary>
        public Array ReadEncodeableArray(string fieldName, Type systemType) {
            if (systemType == null) {
                throw new ArgumentNullException(nameof(systemType));
            }


            if (!ReadArrayField(fieldName, out var token)) {
                return Array.CreateInstance(systemType, 0);
            }

            var values = Array.CreateInstance(systemType, token.Count);

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadEncodeable(null, systemType);
                    values.SetValue(element, ii);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an enumerated value array from the stream.
        /// </summary>
        public Array ReadEnumeratedArray(string fieldName, Type enumType) {
            if (enumType == null) {
                throw new ArgumentNullException(nameof(enumType));
            }


            if (!ReadArrayField(fieldName, out var token)) {
                return Array.CreateInstance(enumType, 0);
            }

            var values = Array.CreateInstance(enumType, token.Count);

            for (var ii = 0; ii < token.Count; ii++) {
                try {
                    _stack.Push(token[ii]);
                    var element = ReadEnumerated(null, enumType);
                    values.SetValue(element, ii);
                }
                finally {
                    _stack.Pop();
                }
            }

            return values;
        }

        private JsonTextReader _reader;
        private readonly Dictionary<string, object> _root;
        private Stack<object> _stack;
        private ushort[] _namespaceMappings;
        private ushort[] _serverMappings;
        private uint _nestingLevel;
    }
}
