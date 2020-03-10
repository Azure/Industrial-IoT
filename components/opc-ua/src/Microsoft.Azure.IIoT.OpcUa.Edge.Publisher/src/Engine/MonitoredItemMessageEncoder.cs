// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Publisher monitored item message encoder
    /// </summary>
    public class MonitoredItemMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(DataSetMessageModel message) {
            switch (message.WriterGroup?.MessageType ?? MessageEncoding.Json) {
                case MessageEncoding.Json:
                    return Task.FromResult(EncodeAsJson(message));
                case MessageEncoding.Uadp:
                    return Task.FromResult(EncodeAsUadp(message));
                default:
                    return Task.FromException<IEnumerable<NetworkMessageModel>>(
                        new NotSupportedException("Type not supported"));
            }
        }

        /// <summary>
        /// Perform event to message encoding
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageModel> EncodeAsUadp(DataSetMessageModel message) {
            foreach (var notification in message.Notifications) {
                using (var encoder = new BinaryEncoder(message.ServiceMessageContext)) {
                    var value = new MonitoredItemMessage {
                        MessageContentMask = (message.Writer?.MessageSettings?
                            .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                                message.Writer?.DataSetFieldContentMask),
                        ApplicationUri = message.ApplicationUri,
                        SubscriptionId = message.SubscriptionId,
                        EndpointUrl = message.EndpointUrl,
                        ExtensionFields = message.Writer?.DataSet?.ExtensionFields,
                        NodeId = notification.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris),
                        Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                        Value = notification.Value,
                        DisplayName = notification.DisplayName
                    };
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
        /// <summary>
        /// Perform event to message encoding
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageModel> EncodeAsJson(DataSetMessageModel message) {
            foreach (var notification in message.Notifications) {
                using (var writer = new StringWriter()) {
                    var value = new MonitoredItemMessage {
                        MessageContentMask = (message.Writer?.MessageSettings?
                            .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                                message.Writer?.DataSetFieldContentMask),
                        ApplicationUri = message.ApplicationUri,
                        SubscriptionId = message.SubscriptionId,
                        EndpointUrl = message.EndpointUrl,
                        ExtensionFields = message.Writer?.DataSet?.ExtensionFields,
                        NodeId = notification.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris),
                        Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                        Value = notification.Value,
                        DisplayName = notification.DisplayName
                    };
                    using (var encoder = new JsonEncoderEx(writer, message.ServiceMessageContext) {
                        UseUriEncoding = true}){
                        value.Encode(encoder);
                    }
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(writer.ToString()),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Json,
                        MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson
                    };
                    yield return encoded;
                }
            }
        }
    }
}