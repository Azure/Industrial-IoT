// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua;
    using System;
    using System.Xml;
    using System.IO;
    using Microsoft.Azure.IIoT;

    /// <summary>
    /// Encoder wrapper to encode model
    /// </summary>
    public class ModelDecoder : IDecoder, IDisposable {

        /// <inheritdoc />
        public EncodingType EncodingType => _wrapped.EncodingType;

        /// <inheritdoc />
        public ServiceMessageContext Context => _wrapped.Context;

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        public ModelDecoder(Stream stream, string contentType,
            ServiceMessageContext context = null) :
            this(CreateDecoder(contentType, stream, context)) {
        }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="wrapped"></param>
        public ModelDecoder(IDecoder wrapped) {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
        }

        /// <inheritdoc />
        public void SetMappingTables(NamespaceTable namespaceUris,
            StringTable serverUris) => _wrapped.SetMappingTables(namespaceUris, serverUris);

        /// <inheritdoc />
        public void PushNamespace(string namespaceUri) =>
            _wrapped.PushNamespace(namespaceUri);

        /// <inheritdoc />
        public void PopNamespace() =>
            _wrapped.PopNamespace();

        /// <inheritdoc />
        public bool ReadBoolean(string fieldName) =>
            _wrapped.ReadBoolean(fieldName);

        /// <inheritdoc />
        public sbyte ReadSByte(string fieldName) =>
            _wrapped.ReadSByte(fieldName);

        /// <inheritdoc />
        public byte ReadByte(string fieldName) =>
            _wrapped.ReadByte(fieldName);

        /// <inheritdoc />
        public short ReadInt16(string fieldName) =>
            _wrapped.ReadInt16(fieldName);

        /// <inheritdoc />
        public ushort ReadUInt16(string fieldName) =>
            _wrapped.ReadUInt16(fieldName);

        /// <inheritdoc />
        public int ReadInt32(string fieldName) =>
            _wrapped.ReadInt32(fieldName);

        /// <inheritdoc />
        public uint ReadUInt32(string fieldName) =>
            _wrapped.ReadUInt32(fieldName);

        /// <inheritdoc />
        public long ReadInt64(string fieldName) =>
            _wrapped.ReadInt64(fieldName);

        /// <inheritdoc />
        public ulong ReadUInt64(string fieldName) =>
            _wrapped.ReadUInt64(fieldName);

        /// <inheritdoc />
        public float ReadFloat(string fieldName) =>
            _wrapped.ReadFloat(fieldName);

        /// <inheritdoc />
        public double ReadDouble(string fieldName) =>
            _wrapped.ReadDouble(fieldName);

        /// <inheritdoc />
        public string ReadString(string fieldName) =>
            _wrapped.ReadString(fieldName);

        /// <inheritdoc />
        public DateTime ReadDateTime(string fieldName) =>
            _wrapped.ReadDateTime(fieldName);

        /// <inheritdoc />
        public Uuid ReadGuid(string fieldName) =>
            _wrapped.ReadGuid(fieldName);

        /// <inheritdoc />
        public byte[] ReadByteString(string fieldName) =>
            _wrapped.ReadByteString(fieldName);

        /// <inheritdoc />
        public XmlElement ReadXmlElement(string fieldName) =>
            _wrapped.ReadXmlElement(fieldName);

        /// <inheritdoc />
        public NodeId ReadNodeId(string fieldName) =>
            _wrapped.ReadNodeId(fieldName);

        /// <inheritdoc />
        public ExpandedNodeId ReadExpandedNodeId(string fieldName) =>
            _wrapped.ReadExpandedNodeId(fieldName);

        /// <inheritdoc />
        public StatusCode ReadStatusCode(string fieldName) =>
            _wrapped.ReadStatusCode(fieldName);

        /// <inheritdoc />
        public DiagnosticInfo ReadDiagnosticInfo(string fieldName) =>
            _wrapped.ReadDiagnosticInfo(fieldName);

        /// <inheritdoc />
        public QualifiedName ReadQualifiedName(string fieldName) =>
            _wrapped.ReadQualifiedName(fieldName);

        /// <inheritdoc />
        public LocalizedText ReadLocalizedText(string fieldName) =>
            _wrapped.ReadLocalizedText(fieldName);

        /// <inheritdoc />
        public Variant ReadVariant(string fieldName) =>
            _wrapped.ReadVariant(fieldName);

        /// <inheritdoc />
        public DataValue ReadDataValue(string fieldName) =>
            _wrapped.ReadDataValue(fieldName);

        /// <inheritdoc />
        public ExtensionObject ReadExtensionObject(string fieldName) =>
            _wrapped.ReadExtensionObject(fieldName);

        /// <inheritdoc />
        public IEncodeable ReadEncodeable(string fieldName, Type systemType) =>
            _wrapped.ReadEncodeable(fieldName, systemType);

        /// <inheritdoc />
        public Enum ReadEnumerated(string fieldName, Type enumType) =>
            _wrapped.ReadEnumerated(fieldName, enumType);

        /// <inheritdoc />
        public BooleanCollection ReadBooleanArray(string fieldName) =>
            _wrapped.ReadBooleanArray(fieldName);

        /// <inheritdoc />
        public SByteCollection ReadSByteArray(string fieldName) =>
            _wrapped.ReadSByteArray(fieldName);

        /// <inheritdoc />
        public ByteCollection ReadByteArray(string fieldName) =>
            _wrapped.ReadByteArray(fieldName);

        /// <inheritdoc />
        public Int16Collection ReadInt16Array(string fieldName) =>
            _wrapped.ReadInt16Array(fieldName);

        /// <inheritdoc />
        public UInt16Collection ReadUInt16Array(string fieldName) =>
            _wrapped.ReadUInt16Array(fieldName);

        /// <inheritdoc />
        public Int32Collection ReadInt32Array(string fieldName) =>
            _wrapped.ReadInt32Array(fieldName);

        /// <inheritdoc />
        public UInt32Collection ReadUInt32Array(string fieldName) =>
            _wrapped.ReadUInt32Array(fieldName);

        /// <inheritdoc />
        public Int64Collection ReadInt64Array(string fieldName) =>
            _wrapped.ReadInt64Array(fieldName);

        /// <inheritdoc />
        public UInt64Collection ReadUInt64Array(string fieldName) =>
            _wrapped.ReadUInt64Array(fieldName);

        /// <inheritdoc />
        public FloatCollection ReadFloatArray(string fieldName) =>
            _wrapped.ReadFloatArray(fieldName);

        /// <inheritdoc />
        public DoubleCollection ReadDoubleArray(string fieldName) =>
            _wrapped.ReadDoubleArray(fieldName);

        /// <inheritdoc />
        public StringCollection ReadStringArray(string fieldName) =>
            _wrapped.ReadStringArray(fieldName);

        /// <inheritdoc />
        public DateTimeCollection ReadDateTimeArray(string fieldName) =>
            _wrapped.ReadDateTimeArray(fieldName);

        /// <inheritdoc />
        public UuidCollection ReadGuidArray(string fieldName) =>
            _wrapped.ReadGuidArray(fieldName);

        /// <inheritdoc />
        public ByteStringCollection ReadByteStringArray(string fieldName) =>
            _wrapped.ReadByteStringArray(fieldName);

        /// <inheritdoc />
        public XmlElementCollection ReadXmlElementArray(string fieldName) =>
            _wrapped.ReadXmlElementArray(fieldName);

        /// <inheritdoc />
        public NodeIdCollection ReadNodeIdArray(string fieldName) =>
            _wrapped.ReadNodeIdArray(fieldName);

        /// <inheritdoc />
        public ExpandedNodeIdCollection ReadExpandedNodeIdArray(string fieldName) =>
            _wrapped.ReadExpandedNodeIdArray(fieldName);

        /// <inheritdoc />
        public StatusCodeCollection ReadStatusCodeArray(string fieldName) =>
            _wrapped.ReadStatusCodeArray(fieldName);

        /// <inheritdoc />
        public DiagnosticInfoCollection ReadDiagnosticInfoArray(string fieldName) =>
            _wrapped.ReadDiagnosticInfoArray(fieldName);

        /// <inheritdoc />
        public QualifiedNameCollection ReadQualifiedNameArray(string fieldName) =>
            _wrapped.ReadQualifiedNameArray(fieldName);

        /// <inheritdoc />
        public LocalizedTextCollection ReadLocalizedTextArray(string fieldName) =>
            _wrapped.ReadLocalizedTextArray(fieldName);

        /// <inheritdoc />
        public VariantCollection ReadVariantArray(string fieldName) =>
            _wrapped.ReadVariantArray(fieldName);

        /// <inheritdoc />
        public DataValueCollection ReadDataValueArray(string fieldName) =>
            _wrapped.ReadDataValueArray(fieldName);

        /// <inheritdoc />
        public ExtensionObjectCollection ReadExtensionObjectArray(string fieldName) =>
            _wrapped.ReadExtensionObjectArray(fieldName);

        /// <inheritdoc />
        public Array ReadEncodeableArray(string fieldName, Type systemType) =>
            _wrapped.ReadEncodeableArray(fieldName, systemType);

        /// <inheritdoc />
        public Array ReadEnumeratedArray(string fieldName, Type enumType) =>
            _wrapped.ReadEnumeratedArray(fieldName, enumType);

        /// <inheritdoc />
        public void Dispose() {
            if (_wrapped is IDisposable disposable) {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Create encoder for content type
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="stream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static IDecoder CreateDecoder(string contentType, Stream stream,
            ServiceMessageContext context) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            switch (contentType.ToLowerInvariant()) {
                case ContentEncodings.MimeTypeJson:
                case ContentEncodings.MimeTypeUaJson:
                    return new JsonDecoderEx(stream, context);
                case ContentEncodings.MimeTypeUaBinary:
                    return new BinaryDecoder(stream,
                        context ?? new ServiceMessageContext());
                case ContentEncodings.MimeTypeUaXml:
                    return new XmlDecoder((Type)null, XmlReader.Create(stream),
                        context ?? new ServiceMessageContext());
                default:
                    throw new ArgumentException(nameof(contentType));
            }
        }

        /// <summary>
        /// Wrap encodeable to support virtual calls
        /// </summary>
        private class EncodableWrapper : IEncodeable {

            public EncodableWrapper(IDecoder decoder, IEncodeable wrapped) {
                _decoder = decoder;
                _wrapped = wrapped;
            }

            /// <inheritdoc />
            public ExpandedNodeId TypeId =>
                _wrapped.TypeId;

            /// <inheritdoc />
            public ExpandedNodeId BinaryEncodingId =>
                _wrapped.BinaryEncodingId;

            /// <inheritdoc />
            public ExpandedNodeId XmlEncodingId =>
                _wrapped.XmlEncodingId;

            /// <inheritdoc />
            public void Decode(IDecoder decoder) =>
                _wrapped.Decode(_decoder);

            /// <inheritdoc />
            public void Encode(IEncoder encoder) =>
                throw new InvalidOperationException();

            /// <inheritdoc />
            public bool IsEqual(IEncodeable encodeable) =>
                _wrapped.IsEqual(encodeable);

            private readonly IDecoder _decoder;
            private readonly IEncodeable _wrapped;
        }

        private readonly IDecoder _wrapped;
    }
}
