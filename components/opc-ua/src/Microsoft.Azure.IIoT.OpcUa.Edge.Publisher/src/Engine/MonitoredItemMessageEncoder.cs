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
    using Opc.Ua.PubSub;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Publisher monitored item message encoder
    /// </summary>
    public class MonitoredItemMessageEncoder : IMessageEncoder {

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

        /// <summary>
        /// Create instance of MonitoredItemMessageEncoder.
        /// </summary>
        /// <param name="logger"> Logger to be used for reporting. </param>
        /// <param name="standaloneConfig"> injected configuration. </param>
        /// <param name="engineConfig"> injected configuration. </param>
        public MonitoredItemMessageEncoder(ILogger logger,
            IStandaloneCliModelProvider standaloneConfig,
            IEngineConfiguration engineConfig) {
            _logger = logger;
            _jsonContentType = standaloneConfig.StandaloneCliModel.LegacyCompatibility
                ? ContentMimeType.UaLegacyPublisher
                : ContentMimeType.Json;
            _enableRoutingInfo = engineConfig.EnableRoutingInfo;
            _useAdvancedEncoding = !engineConfig.UseStandardsCompliantEncoding;
        }

        /// <summary>
        /// Create instance of MonitoredItemMessageEncoder.
        /// </summary>
        /// <param name="logger"> Logger to be used for reporting. </param>
        /// <param name="engineConfig"> injected configuration. </param>
        public MonitoredItemMessageEncoder(ILogger logger, IEngineConfiguration engineConfig) {
            _logger = logger;
            _jsonContentType = ContentMimeType.Json;
            _enableRoutingInfo = engineConfig.EnableRoutingInfo;
            _useAdvancedEncoding = !engineConfig.UseStandardsCompliantEncoding;
        }

        /// <inheritdoc/>
        public IEnumerable<NetworkMessageModel> Encode(
            IEnumerable<SubscriptionNotificationModel> messages, int maxMessageSize, bool asBatch) {
            // Deliberately cresh and log through caller
            if (!asBatch) {
                var resultJson = EncodeAsJson(messages, maxMessageSize);
                var resultBinary = EncodeAsBinary(messages, maxMessageSize);
                return resultJson.Concat(resultBinary);
            }
            else {
                var resultJson = EncodeBatchAsJson(messages, maxMessageSize);
                var resultBinary = EncodeBatchAsBinary(messages, maxMessageSize);
                return resultJson.Concat(resultBinary);
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to NetworkMessageModel batch using Json encoding
        /// </summary>
        /// <param name="messages">Messages to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsJson(
            IEnumerable<SubscriptionNotificationModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json, encodingContext);
            var routingInfo = messages.Select(m => m.Context).OfType<WriterGroupMessageContext>()
                .FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 2; // array brackets
            maxMessageSize -= 2048; // reserve 2k for header
            var chunk = new Collection<MonitoredItemMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperWriter = new StringWriter();
                    var helperEncoder = new JsonEncoderEx(helperWriter, encodingContext) {
                        UseAdvancedEncoding = _useAdvancedEncoding,
                        UseUriEncoding = _useAdvancedEncoding,
                        IgnoreDefaultValues = _useAdvancedEncoding,
                        IgnoreNullValues = true,
                        UseReversibleEncoding = (notification.MessageContentMask
                            & (uint)MonitoredItemMessageContentMask.ReversibleFieldEncoding) != 0,
                    };
                    notification.Encode(helperEncoder);
                    helperEncoder.Close();
                    var notificationSize = Encoding.UTF8.GetByteCount(helperWriter.ToString());
                    if (notificationSize > maxMessageSize) {
                        // we cannot handle this notification. Drop it.
                        // TODO Trace
                        NotificationsDroppedCount++;
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);
                        if (!messageCompleted) {
                            NotificationsProcessedCount++;
                            chunk.Add(notification);
                            processing = current.MoveNext();
                            messageSize += notificationSize + (processing ? 1 : 0);
                        }
                    }
                }
                if (messageCompleted || (!processing && chunk.Count > 0)) {
                    var writer = new StringWriter();
                    var encoder = new JsonEncoderEx(writer, encodingContext, JsonEncoderEx.JsonEncoding.Array) {
                        UseAdvancedEncoding = _useAdvancedEncoding,
                        UseUriEncoding = _useAdvancedEncoding,
                        IgnoreDefaultValues = _useAdvancedEncoding,
                        IgnoreNullValues = true,
                        UseReversibleEncoding = (notification.MessageContentMask
                            & (uint)MonitoredItemMessageContentMask.ReversibleFieldEncoding) != 0,
                    };
                    foreach (var element in chunk) {
                        encoder.WriteEncodeable(null, element);
                    }
                    encoder.Close();
                    var content = writer.ToString();
                    var encoded = new NetworkMessageModel {
                        Body = Encoding.UTF8.GetBytes(content),
                        ContentEncoding = "utf-8",
                        Timestamp = DateTime.UtcNow,
                        ContentType = _jsonContentType,
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson,
                        RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        chunk.Count) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 2;
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform DataSetMessageModel to batch NetworkMessageModel using binary encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeBatchAsBinary(
            IEnumerable<SubscriptionNotificationModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Binary, encodingContext);
            var routingInfo = messages.Select(m => m.Context).OfType<WriterGroupMessageContext>()
                .FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 4; // array length size
            maxMessageSize -= 2048; // reserve 2k for header
            var chunk = new Collection<MonitoredItemMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperEncoder = new BinaryEncoder(encodingContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    if (notificationSize > maxMessageSize) {
                        // we cannot handle this notification. Drop it.
                        // TODO Trace
                        NotificationsDroppedCount++;
                        processing = current.MoveNext();
                    }
                    else {
                        messageCompleted = maxMessageSize < (messageSize + notificationSize);

                        if (!messageCompleted) {
                            chunk.Add(notification);
                            NotificationsProcessedCount++;
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
                        ContentType = ContentMimeType.UaBinary,
                        MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary,
                        RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                    };
                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        chunk.Count) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                    chunk.Clear();
                    messageSize = 4;
                    yield return encoded;
                }
            }
        }

        /// <summary>
        /// Perform event to message Json encoding
        /// </summary>
        /// <param name="messages">Messages to encode</param>
        /// <param name="maxMessageSize">Maximum size of messages</param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsJson(
            IEnumerable<SubscriptionNotificationModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Json, encodingContext);
            var routingInfo = messages.Select(m => m.Context).OfType<WriterGroupMessageContext>()
                .FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            foreach (var networkMessage in notifications) {
                var writer = new StringWriter();
                var encoder = new JsonEncoderEx(writer, encodingContext) {
                    UseAdvancedEncoding = _useAdvancedEncoding,
                    UseUriEncoding = _useAdvancedEncoding,
                    IgnoreDefaultValues = _useAdvancedEncoding,
                    IgnoreNullValues = true,
                    UseReversibleEncoding = (networkMessage.MessageContentMask
                        & (uint)MonitoredItemMessageContentMask.ReversibleFieldEncoding) != 0,
                };
                networkMessage.Encode(encoder);
                encoder.Close();
                var encoded = new NetworkMessageModel {
                    Body = Encoding.UTF8.GetBytes(writer.ToString()),
                    ContentEncoding = "utf-8",
                    Timestamp = DateTime.UtcNow,
                    ContentType = _jsonContentType,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageJson,
                    RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // this message is too large to be processed. Drop it
                    NotificationsDroppedCount++;
                    _logger.Warning("Message too large, dropped 1 value.");
                    continue;
                }
                NotificationsProcessedCount++;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + 1) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Perform event to message binary encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="maxMessageSize"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessageModel> EncodeAsBinary(
            IEnumerable<SubscriptionNotificationModel> messages, int maxMessageSize) {

            // by design all messages are generated in the same session context,
            // therefore it is safe to get the first message's context
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)
                ?.ServiceMessageContext;
            var notifications = GetMonitoredItemMessages(messages, MessageEncoding.Binary, encodingContext).ToList();
            var routingInfo = messages.Select(m => m.Context).OfType<WriterGroupMessageContext>()
                .FirstOrDefault(m => m?.WriterGroup != null)?.WriterGroup.WriterGroupId;
            foreach (var networkMessage in notifications) {
                var encoder = new BinaryEncoder(encodingContext);
                encoder.WriteBoolean(null, false); // is not Batch
                encoder.WriteEncodeable(null, networkMessage);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.UaBinary,
                    MessageSchema = MessageSchemaTypes.MonitoredItemMessageBinary,
                    RoutingInfo = _enableRoutingInfo ? routingInfo : null,
                };
                if (encoded.Body.Length > maxMessageSize) {
                    // this message is too large to be processed. Drop it
                    NotificationsDroppedCount++;
                    _logger.Warning("Message too large, dropped 1 value.");
                    continue;
                }
                NotificationsProcessedCount++;
                AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + encoded.Body.Length) /
                    (MessagesProcessedCount + 1);
                AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount + 1) /
                    (MessagesProcessedCount + 1);
                MessagesProcessedCount++;
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce Monitored Item Messages from the data set message model for the specified encoding
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="encoding"></param>
        /// <param name="context"></param>
        private IEnumerable<MonitoredItemMessage> GetMonitoredItemMessages(
            IEnumerable<SubscriptionNotificationModel> messages, MessageEncoding encoding,
            IServiceMessageContext context) {
            if (context?.NamespaceUris == null) {
                // declare all notifications in messages dropped
                foreach (var message in messages) {
                    NotificationsDroppedCount += (uint)(message?.Notifications?.Count() ?? 0);
                }
                yield break;
            }

            // Filter metadata message == no notifications, we do not handle those in samples mode
            foreach (var message in messages.Where(m => m.Notifications != null)) {
                var writerGroupContext = message.Context as WriterGroupMessageContext;
                if (writerGroupContext?.WriterGroup?.MessageType?.HasFlag(encoding) ?? true) {

                    // Group by message id to collate event fields into a single key value pair dictionary view
                    foreach (var notification in message.Notifications.GroupBy(f => f.Id + f.MessageId)) {
                        var notificationsInGroup = notification.ToList();
                        if (notificationsInGroup.Count == 1) {
                            // This is a data change event
                            yield return CreateMessage(message, writerGroupContext,
                                notificationsInGroup[0], notificationsInGroup[0].Value);
                        }
                        else if (notificationsInGroup.Count > 1) {

                            Debug.Assert(notificationsInGroup
                                .Select(n => n.DataSetFieldName).Distinct().Count() == notificationsInGroup.Count,
                                "There should not be duplications in fields in the group.");
                            Debug.Assert(notificationsInGroup.All(n => n.SequenceNumber == notificationsInGroup[0].SequenceNumber),
                                "All notifications in the group should have the same sequence number.");

                            // Combine all event fields into a table representation
                            var dataValue = new DataValue {
                                Value = new EncodeableDictionary(notificationsInGroup
                                    .Select(n => new KeyDataValuePair {
                                        Key = n.DataSetFieldName,
                                        Value = n.Value
                                    })),
                                SourceTimestamp = notificationsInGroup[0].Value.SourceTimestamp,
                                SourcePicoseconds = notificationsInGroup[0].Value.SourcePicoseconds,
                                ServerTimestamp = notificationsInGroup[0].Value.ServerTimestamp,
                                ServerPicoseconds = notificationsInGroup[0].Value.ServerPicoseconds,
                                StatusCode = notificationsInGroup[0].Value.StatusCode
                            };
                            yield return CreateMessage(message, writerGroupContext, notificationsInGroup[0], dataValue);
                        }
                    }
                }
            }

            static MonitoredItemMessage CreateMessage(SubscriptionNotificationModel message,
                WriterGroupMessageContext context, MonitoredItemNotificationModel notification, DataValue value) {
                var sequenceNumber = notification.SequenceNumber.GetValueOrDefault(0);
                var result = new MonitoredItemMessage {
                    MessageContentMask = (context?.Writer?.MessageSettings?
                        .DataSetMessageContentMask).ToMonitoredItemMessageMask(
                            context?.Writer?.DataSetFieldContentMask),
                    ApplicationUri = message.ApplicationUri,
                    EndpointUrl = message.EndpointUrl,
                    ExtensionFields = context?.Writer?.DataSet?.ExtensionFields,
                    NodeId = notification.NodeId,
                    Timestamp = message.Timestamp,
                    Value = value,
                    DisplayName = notification.DisplayName,
                    SequenceNumber = sequenceNumber
                };
                // force published timestamp into to source timestamp for the legacy heartbeat compatibility
                if (notification.IsHeartbeat &&
                    ((result.MessageContentMask & (uint)MonitoredItemMessageContentMask.Timestamp) == 0) &&
                    ((result.MessageContentMask & (uint)MonitoredItemMessageContentMask.SourceTimestamp) != 0)) {
                    result.Value.SourceTimestamp = result.Timestamp;
                }
                return result;
            }
        }

        /// <summary> Logger for reporting. </summary>
        private readonly ILogger _logger;
        /// <summary> The ContentType to be used for json messages encoding. </summary>
        private readonly string _jsonContentType;
        /// <summary> Flag to determine if extra routing information is enabled </summary>
        private readonly bool _enableRoutingInfo;
        /// <summary> Flag to use reversible encoding for messages </summary>
        private readonly bool _useAdvancedEncoding;
    }
}
