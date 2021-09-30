// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Encoders {
    using System;
    using System.Xml;
    using System.IO;
    using Newtonsoft.Json;
    using Microsoft.Azure.IIoT;

    /// <summary>
    /// Type serializer service implementation
    /// </summary>
    public class TypeSerializer : ITypeSerializer {

        /// <summary>
        /// Create codec
        /// </summary>
        /// <param name="context"></param>
        public TypeSerializer(IServiceMessageContext context = null) {
            _context = context ?? ServiceMessageContext.GlobalContext;
        }

        /// <inheritdoc/>
        public T Decode<T>(string contentType, byte[] input,
            Func<IDecoder, T> reader) {
            using (var stream = new MemoryStream(input)) {
                IDecoder decoder = null;
                try {
                    decoder = CreateDecoder(contentType, stream);
                    return reader(decoder);
                }
                finally {
                    if (decoder is IDisposable dispose) {
                        dispose.Dispose();
                    }
                }
            }
        }

        /// <inheritdoc/>
        public byte[] Encode(string contentType, Action<IEncoder> writer) {
            using (var stream = new MemoryStream()) {
                IEncoder encoder = null;
                try {
                    encoder = CreateEncoder(contentType, stream);
                    writer(encoder);

                    // Dispose should flush
                    if (encoder is IDisposable dispose) {
                        dispose.Dispose();
                    }
                    return stream.ToArray();
                }
                catch {
                    if (encoder is IDisposable dispose) {
                        dispose.Dispose();
                    }
                    throw;
                }
            }
        }

        /// <summary>
        /// Create decoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private IDecoder CreateDecoder(string contentType, Stream stream) {
            switch (contentType.ToLowerInvariant()) {
                case ContentMimeType.UaJson:
                case ContentMimeType.UaNonReversibleJson:
                case ContentMimeType.UaNonReversibleJsonReference:
                    return new JsonDecoderEx(stream, _context);
                case ContentMimeType.UaBinary:
                    return new BinaryDecoder(stream, _context);
                case ContentMimeType.UaXml:
                    return new XmlDecoder(null, XmlReader.Create(stream), _context);
                case ContentMimeType.UaJsonReference:
                    return new JsonDecoder(null, new JsonTextReader(
                        new StreamReader(stream)), _context);
                default:
                    throw new ArgumentException(nameof(contentType));
            }
        }

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        private IEncoder CreateEncoder(string contentType, Stream stream) {
            switch (contentType.ToLowerInvariant()) {
                case ContentMimeType.UaJson:
                    return new JsonEncoderEx(stream, _context);
                case ContentMimeType.UaNonReversibleJson:
                    return new JsonEncoderEx(stream, _context) {
                        UseReversibleEncoding = false
                    };
                case ContentMimeType.UaJsonReference:
                    return new JsonEncoder(_context, true, new StreamWriter(stream));
                case ContentMimeType.UaNonReversibleJsonReference:
                    return new JsonEncoder(_context, false, new StreamWriter(stream));
                case ContentMimeType.UaBinary:
                    return new BinaryEncoder(stream, _context);
                case ContentMimeType.UaXml:
                    return new XmlEncoder(
                        new XmlQualifiedName("ua", Namespaces.OpcUaXsd),
                            XmlWriter.Create(stream), _context);
                default:
                    throw new ArgumentException(nameof(contentType));
            }
        }

        private readonly IServiceMessageContext _context;
    }
}
