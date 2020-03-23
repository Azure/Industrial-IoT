// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Encoders;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates publish subscribe messages
    /// </summary>
    public class NetworkMessageEncoder : IMessageEncoder {

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
        /// Perform json encoding
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageModel> EncodeAsJson(DataSetMessageModel message) {
            foreach (var networkMessage in GetNetworkMessages(message.YieldReturn())) {
                using (var writer = new StringWriter()) {
                    using (var encoder = new JsonEncoderEx(writer, message.ServiceMessageContext) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    }) {
                        networkMessage.Encode(encoder);
                    }
                    var json = writer.ToString();
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(json),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Json,
                        MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.NetworkMessageJson
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform uadp encoding
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessageModel> EncodeAsUadp(DataSetMessageModel message) {
            foreach (var networkMessage in GetNetworkMessages(message.YieldReturn())) {
                using (var encoder = new BinaryEncoder(message.ServiceMessageContext)) {
                    networkMessage.Encode(encoder);
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Uadp,
                        MessageId = message.SequenceNumber.ToString(),
                        MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        public IEnumerable<NetworkMessage> GetNetworkMessages(IEnumerable<DataSetMessageModel> messages) {

            // TODO: Honor single message
            // TODO: Group by writer
            foreach (var message in messages) {
                var networkMessage = new NetworkMessage() {
                    MessageContentMask = message.WriterGroup.MessageSettings.NetworkMessageContentMask
                        .ToStackType(message.WriterGroup?.MessageType),
                    PublisherId = message.PublisherId,
                    DataSetClassId = message.Writer?.DataSet?.DataSetMetaData?.DataSetClassId.ToString(),
                    MessageId = message.SequenceNumber.ToString()
                };
                var notificationQueues = message.Notifications.GroupBy(m => m.NodeId)
                    .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray())).ToArray();
                while(notificationQueues.Where(q => q.Any()).Any()) {
                    var payload = notificationQueues
                        .Select(q => q.Any() ? q.Dequeue() : null)
                            .Where(s => s != null)
                                .ToDictionary(
                                    s => s.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris)
                                        .AsString(message.ServiceMessageContext),
                                    s => s.Value);
                    var dataSetMessage = new DataSetMessage() {
                        DataSetWriterId = message.Writer.DataSetWriterId,
                        MetaDataVersion = new ConfigurationVersionDataType {
                            MajorVersion = message.Writer?.DataSet?.DataSetMetaData?
                                .ConfigurationVersion?.MajorVersion ?? 1,
                            MinorVersion = message.Writer?.DataSet?.DataSetMetaData?
                                .ConfigurationVersion?.MinorVersion ?? 0
                        },
                        MessageContentMask = (message.Writer?.MessageSettings?.DataSetMessageContentMask)
                            .ToStackType(message.WriterGroup?.MessageType),
                        Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                        SequenceNumber = message.SequenceNumber,
                        Status = payload.Values.Any(s => StatusCode.IsNotGood(s.StatusCode)) ?
                            StatusCodes.Bad : StatusCodes.Good,
                        Payload = new DataSet(payload, (uint)message.Writer?.DataSetFieldContentMask.ToStackType())
                    };
                    networkMessage.Messages.Add(dataSetMessage);
                }
                yield return networkMessage;
            }
        }
    }
}