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
    using Serilog;

    /// <summary>
    /// Creates PubSub encoded messages
    /// </summary>
    public class NetworkMessageEncoder : IMessageEncoder {
        /// <inheritdoc/>
        public uint NotificationsDroppedCount { get; private set; }

        /// <inheritdoc/>
        public ulong NotificationsProcessedCount { get; private set; }

        /// <inheritdoc/>
        public ulong MessagesProcessedCount { get; private set; }

        /// <inheritdoc/>
        public double AvgNotificationsPerMessage { get; private set; }

        /// <inheritdoc/>
        public double AvgMessageSize { get; private set; }

        /// <inheritdoc/>
        private readonly ILogger _logger;

        /// <summary>
        /// We have to lookup payload identifier per notitication, because we have to handle huge amount of
        /// notifications a cache is useful
        /// </summary>
        /// <remarks>
        /// Clearing the cache is not necessary in standalone mode, each modification to published_nodes.json
        /// will create new instance of NetworkMessageEncoder.
        ///
        /// Currently orchestrated mode don't support PubSub format, therefor the cache don't need to be cleaned
        /// This need to be rechecked, once orchestrated mode support PubSub
        /// </remarks>
        private readonly IDictionary<string, string> _knownPayloadIdentifiers;

        /// <inheritdoc/>
        public NetworkMessageEncoder(ILogger logger) {
            _logger = logger;
            _knownPayloadIdentifiers = new Dictionary<string, string>(5000);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<NetworkMessageModel>> EncodeAsync(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {
            try {
                var resultJson = EncodeAsJson(messages, maxMessageSize);
                var resultUadp = EncodeAsUadp(messages, maxMessageSize);
                var result = resultJson.Concat(resultUadp);
                return Task.FromResult(result);
            }
            catch (Exception e) {
                return Task.FromException<IEnumerable<NetworkMessageModel>>(e);
            }
        }

        /// <inheritdoc/>
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

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetNetworkMessages(messages, MessageEncoding.Json, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
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
                        UseReversibleEncoding = false
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();
                    var notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    notificationsPerMessage = notification.Messages.Sum(m => m.Payload.Count);
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount += (uint)notificationsPerMessage;
                        _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount += (ulong)notificationsPerMessage;
                            processing = current.MoveNext();
                            messageSize += notificationSize + (processing ? 1 : 0);
                        }
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
                    foreach (var element in chunk) {
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(writer.ToString()),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.UaJson,
                        MessageSchema = MessageSchemaTypes.NetworkMessageJson
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
            if (notifications.Count() == 0) {
                yield break;
            }
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
                    notificationsPerMessage = notification.Messages.Sum(m => m.Payload.Count);
                    if (notificationSize > maxMessageSize) {
                        // Message too large, drop it.
                        NotificationsDroppedCount += (uint)notificationsPerMessage;
                        _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount += (ulong)notificationsPerMessage;
                            processing = current.MoveNext();
                            messageSize += notificationSize;
                        }
                    }
                }
                if (!processing || messageCompleted) {
                    var encoder = new BinaryEncoder(encodingContext);
                    encoder.WriteBoolean(null, true); // is Batch
                    encoder.WriteEncodeableArray(null, chunk);
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Uadp,
                        MessageSchema = MessageSchemaTypes.NetworkMessageUadp
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
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetNetworkMessages(messages, MessageEncoding.Json, encodingContext);
            if (notifications.Count() == 0) {
                yield break;
            }
            foreach (var networkMessage in notifications) {
                int notificationsPerMessage = networkMessage.Messages.Sum(m => m.Payload.Count);
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
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount += (uint)notificationsPerMessage;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                    yield break;
                }
                NotificationsProcessedCount += (ulong)notificationsPerMessage;
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
            if (notifications.Count() == 0) {
                yield break;
            }

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
                    MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // Message too large, drop it.
                    NotificationsDroppedCount += (uint)notificationsPerMessage;
                    _logger.Warning("Message too large, dropped {notificationsPerMessage} values");
                    yield break;
                }
                NotificationsProcessedCount += (ulong)notificationsPerMessage;
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
            ServiceMessageContext context) {
            if (context?.NamespaceUris == null) {
                // Declare all notifications in messages as dropped.
                int totalNotifications = messages.Sum(m => m?.Notifications?.Count() ?? 0);
                NotificationsDroppedCount += (uint)totalNotifications;
                _logger.Warning("Namespace is empty, dropped {totalNotifications} values");
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
                    var notificationQueues = message.Notifications.GroupBy(m => m.NodeId)
                        .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray())).ToArray();
                    while (notificationQueues.Where(q => q.Any()).Any()) {
                        var payload = notificationQueues
                            .Select(q => q.Any() ? q.Dequeue() : null)
                                .Where(s => s != null)
                                    .ToDictionary(
                                        s => GetPayloadIdentifier(s, message,context),
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

        /// <summary>
        ///  Reads to identifier to show for notification in payload of IoT Hub method
        ///  Prio 1: DataSetFieldId (need to be read from message)
        ///  Prio 2: DisplayName - nothing to do, because notification.Id already contains DisplayName
        ///  Prio 3: ExpandedNodeId
        /// </summary>
        /// <param name="notification">Notification, were ID need to be looked up for</param>
        /// <param name="message">subscription notification message, containing notifications</param>
        /// <param name="context">service context</param>
        /// <returns>identifier of payload element</returns>
        private string GetPayloadIdentifier(MonitoredItemNotificationModel notification, DataSetMessageModel message, ServiceMessageContext context) {
            if (notification is null) {
                throw new ArgumentNullException(nameof(notification));
            }

            if (message is null) {
                throw new ArgumentNullException(nameof(message));
            }

            if (context is null) {
                throw new ArgumentNullException(nameof(context));
            }

            if (_knownPayloadIdentifiers.TryGetValue(notification.NodeId.ToString(), out var knownPayloadIdentifier)) {
                if (!string.IsNullOrEmpty(knownPayloadIdentifier)) {
                    return knownPayloadIdentifier;
                }
            }
            else { //do the long running lookup as less as possible
                foreach (var dataSetWriter in message.WriterGroup.DataSetWriters) {
                    foreach (var publishedVariableData in dataSetWriter.DataSet.DataSetSource.PublishedVariables.PublishedData) {
                        if (publishedVariableData.PublishedVariableNodeId == notification.NodeId &&
                            publishedVariableData.Id != notification.NodeId) {
                            _knownPayloadIdentifiers[notification.NodeId.ToString()] = publishedVariableData.Id;
                            return publishedVariableData.Id;
                        } else {
                            _knownPayloadIdentifiers[notification.NodeId.ToString()] = string.Empty;
                        }
                    }
                }
            }

            return !string.IsNullOrEmpty(notification.Id)
                    ? notification.Id
                    : notification.NodeId.ToExpandedNodeId(context.NamespaceUris)
                        .AsString(message.ServiceMessageContext);
        }
    }
}