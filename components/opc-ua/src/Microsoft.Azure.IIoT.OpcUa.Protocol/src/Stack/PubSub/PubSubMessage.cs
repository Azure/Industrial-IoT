// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.PubSub {
    using Microsoft.Azure.IIoT;
    using Microsoft.IO;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Encodeable PubSub messages
    /// <see href="https://reference.opcfoundation.org/v104/Core/docs/Part14/7.2.3/"/>
    /// </summary>
    public abstract class PubSubMessage {

        /// <summary>
        /// Message schema
        /// </summary>
        public abstract string MessageSchema { get; }

        /// <summary>
        /// Content type
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Content encoding
        /// </summary>
        public abstract string ContentEncoding { get; }

        /// <summary>
        /// Publisher identifier
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Routing information stashed into the message
        /// </summary>
        public string RoutingInfo { get; set; }

        /// <summary>
        /// Memory stream manager
        /// </summary>
        protected static RecyclableMemoryStreamManager Memory { get; }
            = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Decode the network message from the wire representation
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reader"></param>
        /// <param name="resolver"></param>
        public abstract bool TryDecode(IServiceMessageContext context,
            Queue<byte[]> reader, IDataSetMetaDataResolver resolver = null);

        /// <summary>
        /// Encode the network message into network message chunks
        /// wire representation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="maxChunkSize"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public abstract IReadOnlyList<byte[]> Encode(IServiceMessageContext context,
            int maxChunkSize, IDataSetMetaDataResolver resolver = null);

        /// <summary>
        /// Decode pub sub messages from buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="messageSchema"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static PubSubMessage Decode(byte[] buffer, string contentType,
            IServiceMessageContext context, IDataSetMetaDataResolver resolver = null,
            string messageSchema = null) {
            var reader = new Queue<byte[]>();
            reader.Enqueue(buffer);
            return DecodeOne(reader, contentType, context, resolver, messageSchema);
        }

        /// <summary>
        /// Decode all from reader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="messageSchema"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IEnumerable<PubSubMessage> Decode(Queue<byte[]> reader, string contentType,
            IServiceMessageContext context, IDataSetMetaDataResolver resolver,
            string messageSchema = null) {
            while (true) {
                var message = DecodeOne(reader, contentType, context, resolver, messageSchema);
                if (message == null) {
                    yield break;
                }
                else {
                    yield return message;
                }
            }
        }

        /// <summary>
        /// Decode one pub sub messages from buffer
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="messageSchema"></param>
        /// <param name="contentType"></param>
        /// <param name="context"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        internal static PubSubMessage DecodeOne(Queue<byte[]> reader, string contentType,
            IServiceMessageContext context, IDataSetMetaDataResolver resolver,
            string messageSchema = null) {
            if (reader.Count == 0) {
                return null;
            }
            PubSubMessage message;
            switch (contentType.ToLowerInvariant()) {
                case ContentMimeType.JsonGzip:
                case ContentMimeType.Json:
                case ContentMimeType.UaJson:
                case ContentMimeType.UaLegacyPublisher:
                case ContentMimeType.UaNonReversibleJson:
                    message = new JsonNetworkMessage {
                        MessageSchemaToUse = messageSchema,
                        UseGzipCompression = contentType.Equals(
                            ContentMimeType.JsonGzip, StringComparison.OrdinalIgnoreCase)
                    };
                    if (message.TryDecode(context, reader, resolver)) {
                        return message;
                    }
                    if (reader.Count == 0) {
                        return null;
                    }
                    message = new JsonMetaDataMessage();
                    if (message.TryDecode(context, reader, resolver)) {
                        return message;
                    }
                    break;
                case ContentMimeType.Binary:
                case ContentMimeType.Uadp:
                    message = new UadpNetworkMessage();
                    if (message.TryDecode(context, reader, resolver)) {
                        return message;
                    }
                    if (reader.Count == 0) {
                        return null;
                    }
                    message = new UadpDiscoveryMessage();
                    if (message.TryDecode(context, reader, resolver)) {
                        return message;
                    }
                    break;
                default:
                    break;
            }
            // Failed
            return null;
        }

        /// <inheritdoc/>
        public override bool Equals(object value) {
            if (ReferenceEquals(this, value)) {
                return true;
            }
            if (!(value is PubSubMessage wrapper)) {
                return false;
            }
            if (!Utils.IsEqual(wrapper.PublisherId, PublisherId)) {
                return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hash = new HashCode();
            hash.Add(PublisherId);
            return hash.ToHashCode();
        }
    }
}