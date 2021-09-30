// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Opc.Ua;
    using System;
    using System.IO;

    /// <summary>
    /// Message serializer service implementation
    /// </summary>
    public class MessageSerializer : IMessageSerializer {

        /// <summary>
        /// Create codec
        /// </summary>
        /// <param name="context"></param>
        public MessageSerializer(IServiceMessageContext context = null) {
            _context = context ?? ServiceMessageContext.ThreadContext;
        }

        /// <inheritdoc/>
        public IEncodeable Decode(string contentType, Stream stream) {
            var decoder = CreateDecoder(contentType);
            return decoder(stream);
        }

        /// <inheritdoc/>
        public void Encode(string contentType, Stream stream,
            IEncodeable message) {
            var encoder = CreateEncoder(contentType);
            encoder(message, stream);
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private Func<Stream, IEncodeable> CreateDecoder(string contentType) {
            switch (contentType.ToLowerInvariant()) {
                case ContentMimeType.UaXml:
                    throw new NotSupportedException();
                case ContentMimeType.UaJson:
                case ContentMimeType.UaNonReversibleJson:
                case ContentMimeType.UaNonReversibleJsonReference:
                    throw new NotSupportedException();
                default:
                    return stream => DecodeBinaryMessage(stream, _context);
            }
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private Action<IEncodeable, Stream> CreateEncoder(string contentType) {
            switch (contentType.ToLowerInvariant()) {
                case ContentMimeType.UaJson:
                    throw new NotSupportedException();
                case ContentMimeType.UaNonReversibleJson:
                    throw new NotSupportedException();
                case ContentMimeType.UaJsonReference:
                    throw new NotSupportedException();
                case ContentMimeType.UaNonReversibleJsonReference:
                    throw new NotSupportedException();
                case ContentMimeType.UaXml:
                    throw new NotSupportedException();
                default:
                    return (encodeable, stream) => EncodeBinaryMessage(
                        encodeable, stream, _context);
            }
        }
        /// <summary>
        /// Encodes a message in a stream.
        /// </summary>
        private static void EncodeBinaryMessage(IEncodeable message, Stream stream,
            IServiceMessageContext context) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message));
            }
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            MemoryStream buffer = null;
            try {
                //
                // Binary encoder unfortunately seeks (to keep track of
                // position). Therefore we need to wrap a non seeking stream.
                //
                var output = stream.CanSeek ? stream : buffer = new MemoryStream();
                using (var encoder = new BinaryEncoder(output, context)) {
                    // convert the namespace uri to an index.
                    var typeId = ExpandedNodeId.ToNodeId(message.BinaryEncodingId,
                        context.NamespaceUris);
                    // write the type id.
                    encoder.WriteNodeId(null, typeId);
                    // write the message.
                    encoder.WriteEncodeable(null, message, message.GetType());
                }
            }
            finally {
                if (buffer != null) {
                    stream.Write(buffer.ToArray());
                    buffer.Dispose();
                }
            }
        }

        /// <summary>
        /// Decodes a message from a buffer.
        /// </summary>
        private static IEncodeable DecodeBinaryMessage(Stream stream,
            IServiceMessageContext context) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            MemoryStream buffer = null;
            try {
                //
                // Binary decoder unfortunately uses seeking (to keep track of
                // position).  Therefore we need to wrap a non seeking stream.
                //
                if (!stream.CanSeek) {
                    // Read entire buffer and pass as memory stream.
                    var segment = stream.ReadAsBuffer();
                    stream = buffer = new MemoryStream(segment.Array, 0,
                        segment.Count);
                }
                using (var decoder = new BinaryDecoder(stream, context)) {
                    // read the node id.
                    var typeId = decoder.ReadNodeId(null);
                    // convert to absolute node id.
                    var absoluteId = NodeId.ToExpandedNodeId(typeId,
                        context.NamespaceUris);
                    // lookup message type.
                    var actualType = context.Factory.GetSystemType(absoluteId);
                    if (actualType == null) {
                        throw new ServiceResultException(StatusCodes.BadEncodingError,
                            $"Cannot decode message with type id: {absoluteId}.");
                    }
                    // read the message.
                    return decoder.ReadEncodeable(null, actualType);
                }
            }
            finally {
                buffer?.Dispose();
            }
        }


        private readonly IServiceMessageContext _context;
    }
}
