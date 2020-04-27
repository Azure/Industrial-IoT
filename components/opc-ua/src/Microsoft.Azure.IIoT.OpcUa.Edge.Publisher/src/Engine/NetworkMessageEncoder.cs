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
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Creates pub/sub encoded messages
    /// </summary>
    public class NetworkMessageEncoder : IMessageEncoder {

        /// <summary>
        /// Perform DataSetMessageModel to single message NetworkMessageModel
        /// </summary>
        /// <param name="messages"></param>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages) {
            try {
                var resultJson = EncodeAsJson(messages);
                var resultUadp = EncodeAsUadp(messages);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to batch NetworkMessageModel
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        public Task<IEnumerable<NetworkMessageModel>> EncodeBatchAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var resultJson = EncodeBatchAsJson(messages, maxMessageSize);
                var resultUadp = EncodeBatchAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <summary>
        /// DataSetMessage to NetworkMessage Json batched encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            var notifications = GetNetworkMessages(messages, MessageEncoding.Json);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 2; // array brackets
            maxMessageSize -= 2048; // reserve 2k for header
            var chunk = new Collection<NetworkMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperWriter = new StringWriter();
                    var helperEncoder = new JsonEncoderEx(helperWriter, encodingContext) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();
                    var notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    messageCompleted = maxMessageSize < (messageSize + notificationSize);

                    if (!messageCompleted) {
                        chunk.Add(notification);
                        processing = current.MoveNext();
                        messageSize += notificationSize + (processing ? 1 : 0);
                    }
                }
                if (!processing || messageCompleted) {
                    var writer = new StringWriter();
                    var encoder = new JsonEncoderEx(writer, encodingContext,
                        JsonEncoderEx.JsonEncoding.Array) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = false
                    };
                    foreach(var element in chunk) { 
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();
                    chunk.Clear();
                    messageSize = 2;
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(writer.ToString()),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaJson,
                        MessageSchema = MessageSchemaTypes.NetworkMessageJson
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// DataSetMessage to NetworkMessage binary batched encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsUadp(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            var notifications = GetNetworkMessages(messages, MessageEncoding.Uadp);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 4; // array length size
            maxMessageSize -= 2048; // reserve 2k for header
            var chunk = new Collection<NetworkMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperEncoder = new BinaryEncoder(encodingContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    messageCompleted = maxMessageSize < (messageSize + notificationSize);

                    if (!messageCompleted) {
                        chunk.Add(notification);
                        processing = current.MoveNext();
                        messageSize += notificationSize;
                    }
                }
                if (!processing || messageCompleted) {
                    var encoder = new BinaryEncoder(encodingContext);
                    encoder.WriteBoolean(null, true); // is Batch
                    encoder.WriteEncodeableArray(null, chunk);
                    chunk.Clear();
                    messageSize = 4;
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Uadp,
                        MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                    };
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform json encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<DataSetMessageModel> messages) {
            var notifications = GetNetworkMessages(messages, MessageEncoding.Json);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            foreach (var networkMessage in notifications) {
                var writer = new StringWriter();
                var encoder = new JsonEncoderEx(writer, encodingContext) {
                    UseAdvancedEncoding = true,
                    UseUriEncoding = true,
                    UseReversibleEncoding = false
                };
                networkMessage.Encode(encoder);
                encoder.Close();
                var encoded = new NetworkMessageModel {
                    Body = Encoding.UTF8.GetBytes(writer.ToString()),
                    ContentEncoding = "utf-8",
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Json,
                    MessageSchema = MessageSchemaTypes.NetworkMessageJson
                };
                yield return encoded;
            }
        }

        /// <summary>
        /// Perform uadp encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsUadp(
            IEnumerable<DataSetMessageModel> messages) {
            var notifications = GetNetworkMessages(messages, MessageEncoding.Uadp);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            foreach (var networkMessage in notifications) {
                var encoder = new BinaryEncoder(encodingContext);
                encoder.WriteBoolean(null, false); // is not Batch
                encoder.WriteEncodeable(null, networkMessage);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Uadp,
                    MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                };
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessage> GetNetworkMessages(
            IEnumerable<DataSetMessageModel> messages,
            MessageEncoding encoding) {

            // TODO: Honor single message
            // TODO: Group by writer
            foreach (var message in messages) {
                if (message.WriterGroup?.MessageType
                    .GetValueOrDefault(MessageEncoding.Json) == encoding) {
                    var networkMessage = new NetworkMessage() {
                        MessageContentMask = message.WriterGroup
                            .MessageSettings.NetworkMessageContentMask
                            .ToStackType(message.WriterGroup?.MessageType),
                        PublisherId = message.PublisherId,
                        DataSetClassId = message.Writer?.DataSet?
                            .DataSetMetaData?.DataSetClassId.ToString(),
                        MessageId = message.SequenceNumber.ToString()
                    };
                    var notificationQueues = message.Notifications.GroupBy(m => m.NodeId)
                        .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray())).ToArray();
                    while (notificationQueues.Where(q => q.Any()).Any()) {
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
}