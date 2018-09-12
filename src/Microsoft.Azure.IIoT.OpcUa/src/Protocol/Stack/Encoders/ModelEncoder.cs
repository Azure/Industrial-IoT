// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.IO;
    using System.Linq;
    using Microsoft.Azure.IIoT;

    /// <summary>
    /// Encoder wrapper to encode model
    /// </summary>
    public class ModelEncoder : IEncoder, IDisposable {

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="callback"></param>
        /// <param name="stream"></param>
        public ModelEncoder(Stream stream, string contentType,
            Action<ExpandedNodeId> callback) :
            this(stream, new ServiceMessageContext {
                NamespaceUris = new NamespaceTable(),
                ServerUris = new StringTable(),
                Factory = new EncodeableFactory(true)
            }, contentType, callback) {
        }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="callback"></param>
        /// <param name="context"></param>
        /// <param name="stream"></param>
        public ModelEncoder(Stream stream, ServiceMessageContext context,
            string contentType, Action<ExpandedNodeId> callback) :
            this(CreateEncoder(contentType, stream, context), callback) {
        }

        /// <summary>
        /// Create wrapper
        /// </summary>
        /// <param name="wrapped"></param>
        /// <param name="callback"></param>
        public ModelEncoder(IEncoder wrapped, Action<ExpandedNodeId> callback) {
            _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));
            _callback = callback;
        }

        /// <inheritdoc />
        public EncodingType EncodingType =>
            _wrapped.EncodingType;

        /// <inheritdoc />
        public ServiceMessageContext Context =>
            _wrapped.Context;

        /// <inheritdoc />
        public void SetMappingTables(NamespaceTable namespaceUris, StringTable serverUris) =>
            _wrapped.SetMappingTables(namespaceUris, serverUris);

        /// <inheritdoc />
        public void WriteNodeId(string fieldName, NodeId value) {
            _callback?.Invoke(value);
            _wrapped.WriteNodeId(fieldName, value);
        }

        /// <inheritdoc />
        public void WriteExpandedNodeId(string fieldName, ExpandedNodeId value) {
            _callback?.Invoke(value);
            _wrapped.WriteExpandedNodeId(fieldName, value);
        }

        /// <inheritdoc />
        public void WriteNodeIdArray(string fieldName, IList<NodeId> values) {
            foreach (var node in values) {
                _callback?.Invoke(node);
            }
            _wrapped.WriteNodeIdArray(fieldName, values);
        }

        /// <inheritdoc />
        public void WriteExpandedNodeIdArray(string fieldName,
            IList<ExpandedNodeId> values) {
            foreach(var node in values) {
                _callback?.Invoke(node);
            }
            _wrapped.WriteExpandedNodeIdArray(fieldName, values);
        }

        /// <inheritdoc />
        public void WriteEncodeable(string fieldName, IEncodeable value,
            Type systemType) => _wrapped.WriteEncodeable(
                fieldName, new EncodableWrapper(this, value), systemType);

        /// <inheritdoc />
        public void WriteEncodeableArray(string fieldName, IList<IEncodeable> values,
            Type systemType) => _wrapped.WriteEncodeableArray(fieldName, values
                .Select(s => (IEncodeable)new EncodableWrapper(this, s))
                .ToList(), systemType);

        /// <inheritdoc />
        public void PushNamespace(string namespaceUri) =>
            _wrapped.PushNamespace(namespaceUri);

        /// <inheritdoc />
        public void PopNamespace() =>
            _wrapped.PopNamespace();

        /// <inheritdoc />
        public void WriteBoolean(string fieldName, bool value) =>
            _wrapped.WriteBoolean(fieldName, value);

        /// <inheritdoc />
        public void WriteSByte(string fieldName, sbyte value) =>
            _wrapped.WriteSByte(fieldName, value);

        /// <inheritdoc />
        public void WriteByte(string fieldName, byte value) =>
            _wrapped.WriteByte(fieldName, value);

        /// <inheritdoc />
        public void WriteInt16(string fieldName, short value) =>
            _wrapped.WriteInt16(fieldName, value);

        /// <inheritdoc />
        public void WriteUInt16(string fieldName, ushort value) =>
            _wrapped.WriteUInt16(fieldName, value);

        /// <inheritdoc />
        public void WriteInt32(string fieldName, int value) =>
            _wrapped.WriteInt32(fieldName, value);

        /// <inheritdoc />
        public void WriteUInt32(string fieldName, uint value) =>
            _wrapped.WriteUInt32(fieldName, value);

        /// <inheritdoc />
        public void WriteInt64(string fieldName, long value) =>
            _wrapped.WriteInt64(fieldName, value);

        /// <inheritdoc />
        public void WriteUInt64(string fieldName, ulong value) =>
            _wrapped.WriteUInt64(fieldName, value);

        /// <inheritdoc />
        public void WriteFloat(string fieldName, float value) =>
            _wrapped.WriteFloat(fieldName, value);

        /// <inheritdoc />
        public void WriteDouble(string fieldName, double value) =>
            _wrapped.WriteDouble(fieldName, value);

        /// <inheritdoc />
        public void WriteString(string fieldName, string value) =>
            _wrapped.WriteString(fieldName, value);

        /// <inheritdoc />
        public void WriteDateTime(string fieldName, DateTime value) =>
            _wrapped.WriteDateTime(fieldName, value);

        /// <inheritdoc />
        public void WriteGuid(string fieldName, Uuid value) =>
            _wrapped.WriteGuid(fieldName, value);

        /// <inheritdoc />
        public void WriteGuid(string fieldName, Guid value) =>
            _wrapped.WriteGuid(fieldName, value);

        /// <inheritdoc />
        public void WriteByteString(string fieldName, byte[] value) =>
            _wrapped.WriteByteString(fieldName, value);

        /// <inheritdoc />
        public void WriteXmlElement(string fieldName, XmlElement value) =>
            _wrapped.WriteXmlElement(fieldName, value);

        /// <inheritdoc />
        public void WriteStatusCode(string fieldName, StatusCode value) =>
            _wrapped.WriteStatusCode(fieldName, value);

        /// <inheritdoc />
        public void WriteDiagnosticInfo(string fieldName, DiagnosticInfo value) =>
            _wrapped.WriteDiagnosticInfo(fieldName, value);

        /// <inheritdoc />
        public void WriteQualifiedName(string fieldName, QualifiedName value) =>
            _wrapped.WriteQualifiedName(fieldName, value);

        /// <inheritdoc />
        public void WriteLocalizedText(string fieldName, LocalizedText value) =>
            _wrapped.WriteLocalizedText(fieldName, value);

        /// <inheritdoc />
        public void WriteVariant(string fieldName, Variant value) =>
            _wrapped.WriteVariant(fieldName, value);

        /// <inheritdoc />
        public void WriteDataValue(string fieldName, DataValue value) =>
            _wrapped.WriteDataValue(fieldName, value);

        /// <inheritdoc />
        public void WriteExtensionObject(string fieldName, ExtensionObject value) =>
            _wrapped.WriteExtensionObject(fieldName, value);

        /// <inheritdoc />
        public void WriteEnumerated(string fieldName, Enum value) =>
            _wrapped.WriteEnumerated(fieldName, value);

        /// <inheritdoc />
        public void WriteBooleanArray(string fieldName, IList<bool> values) =>
            _wrapped.WriteBooleanArray(fieldName, values);

        /// <inheritdoc />
        public void WriteSByteArray(string fieldName, IList<sbyte> values) =>
            _wrapped.WriteSByteArray(fieldName, values);

        /// <inheritdoc />
        public void WriteByteArray(string fieldName, IList<byte> values) =>
            _wrapped.WriteByteArray(fieldName, values);

        /// <inheritdoc />
        public void WriteInt16Array(string fieldName, IList<short> values) =>
            _wrapped.WriteInt16Array(fieldName, values);

        /// <inheritdoc />
        public void WriteUInt16Array(string fieldName, IList<ushort> values) =>
            _wrapped.WriteUInt16Array(fieldName, values);

        /// <inheritdoc />
        public void WriteInt32Array(string fieldName, IList<int> values) =>
            _wrapped.WriteInt32Array(fieldName, values);

        /// <inheritdoc />
        public void WriteUInt32Array(string fieldName, IList<uint> values) =>
            _wrapped.WriteUInt32Array(fieldName, values);

        /// <inheritdoc />
        public void WriteInt64Array(string fieldName, IList<long> values) =>
            _wrapped.WriteInt64Array(fieldName, values);

        /// <inheritdoc />
        public void WriteUInt64Array(string fieldName, IList<ulong> values) =>
            _wrapped.WriteUInt64Array(fieldName, values);

        /// <inheritdoc />
        public void WriteFloatArray(string fieldName, IList<float> values) =>
            _wrapped.WriteFloatArray(fieldName, values);

        /// <inheritdoc />
        public void WriteDoubleArray(string fieldName, IList<double> values) =>
            _wrapped.WriteDoubleArray(fieldName, values);

        /// <inheritdoc />
        public void WriteStringArray(string fieldName, IList<string> values) =>
            _wrapped.WriteStringArray(fieldName, values);

        /// <inheritdoc />
        public void WriteDateTimeArray(string fieldName, IList<DateTime> values) =>
            _wrapped.WriteDateTimeArray(fieldName, values);

        /// <inheritdoc />
        public void WriteGuidArray(string fieldName, IList<Uuid> values) =>
            _wrapped.WriteGuidArray(fieldName, values);

        /// <inheritdoc />
        public void WriteGuidArray(string fieldName, IList<Guid> values) =>
            _wrapped.WriteGuidArray(fieldName, values);

        /// <inheritdoc />
        public void WriteByteStringArray(string fieldName, IList<byte[]> values) =>
            _wrapped.WriteByteStringArray(fieldName, values);

        /// <inheritdoc />
        public void WriteXmlElementArray(string fieldName, IList<XmlElement> values) =>
            _wrapped.WriteXmlElementArray(fieldName, values);

        /// <inheritdoc />
        public void WriteStatusCodeArray(string fieldName, IList<StatusCode> values) =>
            _wrapped.WriteStatusCodeArray(fieldName, values);

        /// <inheritdoc />
        public void WriteDiagnosticInfoArray(string fieldName, IList<DiagnosticInfo> values) =>
            _wrapped.WriteDiagnosticInfoArray(fieldName, values);

        /// <inheritdoc />
        public void WriteQualifiedNameArray(string fieldName, IList<QualifiedName> values) =>
            _wrapped.WriteQualifiedNameArray(fieldName, values);

        /// <inheritdoc />
        public void WriteLocalizedTextArray(string fieldName, IList<LocalizedText> values) =>
            _wrapped.WriteLocalizedTextArray(fieldName, values);

        /// <inheritdoc />
        public void WriteVariantArray(string fieldName, IList<Variant> values) =>
            _wrapped.WriteVariantArray(fieldName, values);

        /// <inheritdoc />
        public void WriteDataValueArray(string fieldName, IList<DataValue> values) =>
            _wrapped.WriteDataValueArray(fieldName, values);

        /// <inheritdoc />
        public void WriteExtensionObjectArray(string fieldName, IList<ExtensionObject> values) =>
            _wrapped.WriteExtensionObjectArray(fieldName, values);

        /// <inheritdoc />
        public void WriteEnumeratedArray(string fieldName, Array values,
            Type systemType) => _wrapped.WriteEnumeratedArray(fieldName, values, systemType);

        /// <summary>
        /// Dispose
        /// </summary>
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
        private static IEncoder CreateEncoder(string contentType, Stream stream,
            ServiceMessageContext context) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            switch (contentType.ToLowerInvariant()) {
                case ContentEncodings.MimeTypeUaJson:
                    return new JsonEncoderEx(context, new StreamWriter(stream));
                case ContentEncodings.MimeTypeUaBinary:
                    return new BinaryEncoder(stream,
                        context);
                case ContentEncodings.MimeTypeUaXml:
                    return new XmlEncoder((Type)null, XmlWriter.Create(stream),
                        context);
                default:
                    throw new ArgumentException(nameof(contentType));
            }
        }

        /// <summary>
        /// Wrap encodeable to support virtual calls
        /// </summary>
        private class EncodableWrapper : IEncodeable {

            public EncodableWrapper(IEncoder encoder, IEncodeable wrapped) {
                _encoder = encoder;
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
                throw new InvalidOperationException();

            /// <inheritdoc />
            public void Encode(IEncoder encoder) =>
                _wrapped.Encode(_encoder);

            /// <inheritdoc />
            public bool IsEqual(IEncodeable encodeable) =>
                _wrapped.IsEqual(encodeable);

            private readonly IEncoder _encoder;
            private readonly IEncodeable _wrapped;
        }

        private readonly IEncoder _wrapped;
        private readonly Action<ExpandedNodeId> _callback;
    }
}
