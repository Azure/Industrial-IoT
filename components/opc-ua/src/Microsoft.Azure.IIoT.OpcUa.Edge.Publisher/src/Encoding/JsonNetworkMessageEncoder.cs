// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Encoding {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Messaging;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Opc.Ua.PubSub;
    using Opc.Ua.Encoders;

    /// <summary>
    /// Creates publish subscribe messages
    /// </summary>
    public class JsonNetworkMessageEncoder : IMessageEncoder {

        /// <summary>
        /// Create message encoder
        /// </summary>
        /// <param name="encodingConfiguration"></param>
        public JsonNetworkMessageEncoder(IPubSubEncodingConfig encodingConfiguration) {
            _encodingConfiguration = encodingConfiguration;
        }

        /// <inheritdoc/>
        public Task<IMessageData> Decode(string encodedMessage) {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<EncodedMessage> Encode(IMessageData message) {
            var value = message.Value as NetworkMessage;

            value.MessageContentMask = _encodingConfiguration.NetworkMessageContentMask;
            foreach (var m in value.Messages) {
                m.MessageContentMask = _encodingConfiguration.DataSetMessageContentMask;
                m.Payload.FieldContentMask = _encodingConfiguration.FieldContentMask;
            }

            using (var writer = new StringWriter()) {
                using (var encoder = new JsonEncoderEx(writer, value.MessageContext) {
                    // TODO: Configure encoding further
                    UseUriEncoding = false
                }) {
                    value.Encode(encoder);
                }
                return Task.FromResult(new EncodedMessage {
                    Body = writer.ToString(),
                    MessageId = value.MessageId,
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Json,
                    MessageSchema = MessageSchemaTypes.NetworkMessageJson
                });
            }
        }

        private readonly IPubSubEncodingConfig _encodingConfiguration;
    }
}