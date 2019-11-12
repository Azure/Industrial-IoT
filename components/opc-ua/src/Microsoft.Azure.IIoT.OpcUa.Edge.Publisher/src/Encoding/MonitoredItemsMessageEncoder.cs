// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Encoding {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.Messaging;
    using Opc.Ua.Encoders;
    using Opc.Ua.PubSub;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher sample message encoder
    /// </summary>
    public class MonitoredItemsMessageEncoder : IMessageEncoder {

        /// <summary>
        /// Create encoder
        /// </summary>
        /// <param name="encodingConfiguration"></param>
        public MonitoredItemsMessageEncoder(IMonitoredItemEncodingConfig encodingConfiguration) {
            _encodingConfiguration = encodingConfiguration;
        }

        /// <inheritdoc/>
        public Task<IMessageData> Decode(string encodedMessage) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<EncodedMessage> Encode(IMessageData message) {
            var value = message.Value as MonitoredItemSample; // TODO nullcheck

            value.MessageContentMask = _encodingConfiguration.MessageContentMask;
            using (var writer = new StringWriter()) {
                using (var encoder = new JsonEncoderEx(writer, value.ServiceMessageContext) {
                    // TODO: Configure encoding further
                    UseUriEncoding = false
                }) {
                    value.Encode(encoder);
                }
                var encoded = new EncodedMessage {
                    Body = writer.ToString(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Json,
                    MessageId = message.Id,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                };
                return Task.FromResult(encoded);
            }
        }

        private readonly IMonitoredItemEncodingConfig _encodingConfiguration;
    }
}