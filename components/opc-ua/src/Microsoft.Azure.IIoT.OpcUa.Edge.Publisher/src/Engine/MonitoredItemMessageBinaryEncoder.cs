// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using System;
    using System.Threading.Tasks;
    using System.Collections.Generic;


    /// <summary>
    /// Publisher monitored item message encoder
    /// </summary>
    public class MonitoredItemMessageBinaryEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(DataSetMessageModel message) {
            return Task.FromResult(Encode(message));
        }

        /// <summary>
        /// Perform event to message encoding
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageModel> Encode(DataSetMessageModel message) {
            foreach (var notification in message.Notifications) {
                var value = new MonitoredItemMessage {
                    MessageContentMask = (message.Writer?.MessageSettings?
                        .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                            message.Writer?.DataSetFieldContentMask),
                    ApplicationUri = message.ApplicationUri,
                    SubscriptionId = message.SubscriptionId,
                    EndpointUrl = message.EndpointUrl,
                    ExtensionFields = message.Writer?.DataSet?.ExtensionFields,
                    NodeId = notification.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris),
                    Value = notification.Value,
                    DisplayName = notification.DisplayName
                };
                using (var encoder = new BinaryEncoder(message.ServiceMessageContext)) {
                    value.Encode(encoder);
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaBinary,
                        MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary
                    };
                    yield return encoded;
                }
            }
        }
    }
}