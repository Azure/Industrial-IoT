// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
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
    using Serilog;

    /// <summary>
    /// Creates PubSub encoded messages
    /// </summary>
    public class NetworkMessageEncoder : IMessageEncoder {

        /// <inheritdoc/>
        public uint NotificationsDroppedCount { get; private set; }

        /// <inheritdoc/>
        public uint NotificationsProcessedCount { get; private set; }

        /// <inheritdoc/>
        public uint MessagesProcessedCount { get; private set; }

        /// <inheritdoc/>
        public double AvgNotificationsPerMessage { get; private set; }

        /// <inheritdoc/>
        public double AvgMessageSize { get; private set; }

        /// <summary> Logger for reporting. </summary>
        private readonly ILogger _logger;

        /// <summary> Flag to determine if extra routing information is enabled </summary>
        private readonly bool _enableRoutingInfo;

        /// <summary>
        /// Create instance of NetworkMessageEncoder.
        /// </summary>
        /// <param name="logger"> Logger to be used for reporting. </param>
        /// <param name="engineConfig"> injected configuration. </param>
        public NetworkMessageEncoder(ILogger logger, IEngineConfiguration engineConfig) {
            _logger = logger;
            _enableRoutingInfo = engineConfig.EnableRoutingInfo.GetValueOrDefault(false);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize, bool useReversibleEncoding) {
            try {
                var resultJson = EncodeAsJson(messages, maxMessageSize, useReversibleEncoding);
                var resultUadp = EncodeAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult<IEnumerable<NetworkMessageModel>>(result.ToList());
            }
            catch (Exception e) {
                _logger.Error(e, "Failed to encode {numOfMessages} messages", messages.Count());
                return Task.FromResult(Enumerable.Empty<NetworkMessageModel>());
            }
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeBatchAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize, bool useReversibleEncoding) {
            try {
                var resultJson = EncodeBatchAsJson(messages, maxMessageSize, useReversibleEncoding);
                var resultUadp = EncodeBatchAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult<IEnumerable<NetworkMessageModel>>(result.ToList());
            }
            catch (Exception e) {
                _logger.Error(e, "Failed to encode {numOfMessages} messages", messages.Count());
                return Task.FromResult(Enumerable.Empty<NetworkMessageModel>());
            }
        }

        /// <summary>
        /// DataSetMessage to NetworkMessage Json batched encoding
        /// </summary>
        /// <param name="messages">Messages to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <param name="useReversibleEncoding">Flag for reversible encoding</param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize, bool useReversibleEncoding) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var compressedPayload = messages.Where(x => x.CompressedPayload).Any();
            var notifications = GetNetworkMessages(messages, MessageEncoding.Json, encodingContext);
            if (!notifications.Any()) {
                yield break;
            }
            var routingInfo = messages.FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 2; // array brackets
            var chunk = new Collection<NetworkMessage>();
            int notificationsPerMessage = 0;
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperWriter = new StringWriter();
                    var helperEncoder = new JsonEncoderEx(helperWriter, encodingContext) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = useReversibleEncoding
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();
                    var notificationSize = 0;
                    if (compressedPayload) {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(helperWriter.ToString()));
                        try {
                            notificationSize = Encoding.UTF8.GetByteCount(Convert.ToBase64String(stream.Zip()));
                        }
                        finally {
                            stream.Dispose();
                        }
                    }
                    else {
                        notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    }

                    var notificationsInBatch = notification.Messages.Sum(m => m.Payload.Count);
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount += (uint)notificationsInBatch;
                        _logger.Warning("Message too large, dropped {notificationsInBatch} values", notificationsInBatch);
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount += (uint)notificationsInBatch;
                            notificationsPerMessage += notificationsInBatch;
                            processing = current.MoveNext();
                            messageSize += notificationSize + (processing ? 1 : 0);
                        }
                    }
                }
                if (messageCompleted || (!processing && chunk.Count > 0)) {
                    var writer = new StringWriter();
                    var encoder = new JsonEncoderEx(writer, encodingContext,
                        JsonEncoderEx.JsonEncoding.Array) {
                        UseAdvancedEncoding = true,
                        UseUriEncoding = true,
                        UseReversibleEncoding = useReversibleEncoding
                    };
                    foreach (var element in chunk) {
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();

                    var body = Encoding.UTF8.GetBytes(writer.ToString());
                    if (compressedPayload) {
                        var stream = new MemoryStream(body);
                        try {
                            body = Encoding.UTF8.GetBytes(Convert.ToBase64String(stream.Zip()));
                        }
                        finally {
                            stream.Dispose();
                        }
                    }

                    var encoded = new NetworkMessageModel {
                        Body = body,
                        ContentEncoding = compressedPayload ? "gzip" : "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = compressedPayload ? ContentMimeType.Binary : ContentMimeType.UaJson,
                        MessageSchema = MessageSchemaTypes.NetworkMessageJson
                        RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        notificationsPerMessage) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 2;
                    notificationsPerMessage = 0;
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

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetNetworkMessages(messages, MessageEncoding.Uadp, encodingContext);
            if (!notifications.Any()) {
                yield break;
            }
            var routingInfo = messages.FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 4; // array length size
            var chunk = new Collection<NetworkMessage>();
            int notificationsPerMessage = 0;
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperEncoder = new BinaryEncoder(encodingContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    var notificationsInBatch = notification.Messages.Sum(m => m.Payload.Count);
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount += (uint)notificationsInBatch;
                        _logger.Warning("Message too large, dropped {notificationsInBatch} values", notificationsInBatch);
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount += (uint)notificationsInBatch;
                            notificationsPerMessage += notificationsInBatch;
                            processing = current.MoveNext();
                            messageSize += notificationSize;
                        }
                    }
                }
                if (messageCompleted || (!processing && chunk.Count > 0)) {
                    var encoder = new BinaryEncoder(encodingContext);
                    encoder.WriteBoolean(null, true); // is Batch
                    encoder.WriteEncodeableArray(null, chunk);
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Uadp,
                        MessageSchema = MessageSchemaTypes.NetworkMessageUadp,
                        RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        notificationsPerMessage) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 4;
                    notificationsPerMessage = 0;
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform json encoding
        /// </summary>
        /// <param name="messages">Messages to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <param name="useReversibleEncoding">Flag for reversible encoding</param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize, bool useReversibleEncoding) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetNetworkMessages(messages, MessageEncoding.Json, encodingContext);
            if (!notifications.Any()) {
                yield break;
            }
            var routingInfo = messages.FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            foreach (var networkMessage in notifications) {
                int notificationsPerMessage = networkMessage.Messages.Sum(m => m.Payload.Count);
                var writer = new StringWriter();
                var encoder = new JsonEncoderEx(writer, encodingContext) {
                    UseAdvancedEncoding = true,
                    UseUriEncoding = true,
                    UseReversibleEncoding = useReversibleEncoding
                };
                networkMessage.Encode(encoder);
                encoder.Close();
                var encoded = new NetworkMessageModel {
                    Body = Encoding.UTF8.GetBytes(writer.ToString()),
                    ContentEncoding = "utf-8",
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Json,
                    MessageSchema = MessageSchemaTypes.NetworkMessageJson,
                    RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount += (uint)notificationsPerMessage;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values", notificationsPerMessage);
                    continue;
                }
                NotificationsProcessedCount += (uint)notificationsPerMessage;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + notificationsPerMessage) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Perform uadp encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsUadp(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetNetworkMessages(messages, MessageEncoding.Uadp, encodingContext);
            if (!notifications.Any()) {
                yield break;
            }
            var routingInfo = messages.FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            foreach (var networkMessage in notifications) {
                int notificationsPerMessage = networkMessage.Messages.Sum(m => m.Payload.Count);
                var encoder = new BinaryEncoder(encodingContext);
                encoder.WriteBoolean(null, false); // is not Batch
                encoder.WriteEncodeable(null, networkMessage);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Uadp,
                    MessageSchema = MessageSchemaTypes.NetworkMessageUadp,
                    RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount += (uint)notificationsPerMessage;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values", notificationsPerMessage);
                    continue;
                }
                NotificationsProcessedCount += (uint)notificationsPerMessage;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + notificationsPerMessage) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="encoding"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessage> GetNetworkMessages(
            IEnumerable<DataSetMessageModel> messages, MessageEncoding encoding,
            IServiceMessageContext context) {
            if (context?.NamespaceUris == null) {
                // Declare all notifications in messages as dropped.
                int totalNotifications = messages.Sum(m => m?.Notifications?.Count() ?? 0);
                NotificationsDroppedCount += (uint)totalNotifications;
                _logger.Warning("Namespace is empty, dropped {totalNotifications} values", totalNotifications);
                yield break;
            }

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
                        DataSetWriterGroup = message.WriterGroup.WriterGroupId,
                        MessageId = message.SequenceNumber.ToString()
                    };
                    var notificationQueues = message.Notifications
                        .GroupBy(m => !string.IsNullOrEmpty(m.Id) ?
                                        m.Id :
                                        !string.IsNullOrEmpty(m.DisplayName) ?
                                            m.DisplayName :
                                            m.NodeId)
                        .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray()))
                        .ToArray();

                    while (notificationQueues.Any(q => q.Any())) {
                        var payload = notificationQueues
                            .Select(q => q.Any() ? q.Dequeue() : null)
                            .Where(s => s != null)
                            .ToDictionary(
                                //  Identifier to show for notification in payload of IoT Hub method
                                //  Prio 1: Id = DataSetFieldId - if already configured
                                //  Prio 2: Id = DisplayName - if already configured
                                //  Prio 3: NodeId as configured
                                s => !string.IsNullOrEmpty(s.Id) ?
                                        s.Id :
                                        !string.IsNullOrEmpty(s.DisplayName) ?
                                            s.DisplayName :
                                            s.NodeId,
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
