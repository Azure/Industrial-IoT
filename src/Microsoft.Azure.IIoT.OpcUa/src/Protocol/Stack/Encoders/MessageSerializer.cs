// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using System;
    using System.IO;
    using Microsoft.Azure.IIoT;

    /// <summary>
    /// Message serializer service implementation
    /// </summary>
    public class MessageSerializer : IMessageSerializer {

        /// <summary>
        /// Create codec
        /// </summary>
        /// <param name="context"></param>
        public MessageSerializer(ServiceMessageContext context = null) {
            _context = context ?? ServiceMessageContext.ThreadContext;
        }

        /// <inheritdoc/>
        public IEncodeable Decode(string contentType, Stream stream) {
            var decoder = CreateDecoder(contentType);
            return decoder(stream);
        }

        /// <inheritdoc/>
        public void Encode(string contentType, Stream stream,
            IEncodeable encodeable) {
            var encoder = CreateEncoder(contentType);
            encoder(encodeable, stream);
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private Func<Stream, IEncodeable> CreateDecoder(string contentType) {
            switch (contentType.ToLowerInvariant()) {
                case ContentEncodings.MimeTypeUaXml:
                    throw new NotSupportedException();
                case ContentEncodings.MimeTypeUaJson:
                case ContentEncodings.MimeTypeUaNonReversibleJson:
                case ContentEncodings.MimeTypeUaNonReversibleJsonReference:
                    throw new NotSupportedException();
                default:
                    return stream => BinaryDecoder.DecodeMessage(stream, null,
                        _context);
            }
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        private Action<IEncodeable, Stream> CreateEncoder(string contentType) {
            switch (contentType.ToLowerInvariant()) {
                case ContentEncodings.MimeTypeUaJson:
                    throw new NotSupportedException();
                case ContentEncodings.MimeTypeUaNonReversibleJson:
                    throw new NotSupportedException();
                case ContentEncodings.MimeTypeUaJsonReference:
                    throw new NotSupportedException();
                case ContentEncodings.MimeTypeUaNonReversibleJsonReference:
                    throw new NotSupportedException();
                case ContentEncodings.MimeTypeUaXml:
                    throw new NotSupportedException();
                default:
                    return (encodeable, stream) => BinaryEncoder.EncodeMessage(
                        encodeable, stream, _context);
            }
        }

        private readonly ServiceMessageContext _context;
    }
}
