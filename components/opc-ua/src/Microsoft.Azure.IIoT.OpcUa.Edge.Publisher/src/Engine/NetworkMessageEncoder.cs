// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
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
    using System.Diagnostics;
    using System.Linq;

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

        /// <inheritdoc/>
        public double MaxMessageSplitRatio { get; private set; }

        /// <summary>
        /// Create instance of NetworkMessageEncoder.
        /// </summary>
        /// <param name="logger"> Logger to be used for reporting. </param>
        /// <param name="engineConfig"> injected configuration. </param>
        public NetworkMessageEncoder(ILogger logger, IEngineConfiguration engineConfig) {
            _logger = logger;
            _enableRoutingInfo = engineConfig.EnableRoutingInfo;
            _useStandardsCompliantEncoding = engineConfig.UseStandardsCompliantEncoding;
        }

        /// <inheritdoc/>
        public IEnumerable<NetworkMessageModel> Encode(IEnumerable<SubscriptionNotificationModel> messages,
            int maxMessageSize, bool asBatch) {

            //
            // by design all messages are generated in the same session context, therefore it is safe to
            // get the first message's context
            //
            var encodingContext = messages.FirstOrDefault(m => m.ServiceMessageContext != null)?.ServiceMessageContext;
            var chunkedMessages = new List<NetworkMessageModel>();
            if (encodingContext == null) {
                // Drop all messages
                Drop(messages);
                return chunkedMessages;
            }

            var networkMessages = GetNetworkMessages(messages, asBatch);
            foreach (var (notificationsPerMessage, networkMessage) in networkMessages) {
                var chunks = networkMessage.Encode(encodingContext, maxMessageSize);
                var tooBig = 0;
                var notificationsPerChunk = notificationsPerMessage / (double)chunks.Count;
                foreach (var body in chunks) {
                    if (body == null) {
                        //
                        // Failed to press a notification into message size limit
                        // This is somewhat correct as the smallest dropped chunk is
                        // a message containing only a single data set message which
                        // contains (parts) of a notification.
                        //
                        _logger.Warning("Resulting chunk is too large, dropped a notification.");
                        tooBig++;
                        continue;
                    }

                    chunkedMessages.Add(new NetworkMessageModel {
                        Timestamp = DateTime.UtcNow,
                        Body = body,
                        ContentEncoding = networkMessage.ContentEncoding,
                        ContentType = networkMessage.ContentType,
                        MessageSchema = networkMessage.MessageSchema,
                        RoutingInfo = networkMessage.RoutingInfo,
                    });

                    AvgMessageSize = (AvgMessageSize * MessagesProcessedCount + body.Length) /
                        (MessagesProcessedCount + 1);
                    AvgNotificationsPerMessage = (AvgNotificationsPerMessage * MessagesProcessedCount +
                        notificationsPerChunk) / (MessagesProcessedCount + 1);
                    MessagesProcessedCount++;
                }

                // We dropped a number of notifications but processed the remainder successfully
                NotificationsDroppedCount += (uint)tooBig;
                if (notificationsPerMessage > tooBig) {
                    NotificationsProcessedCount += (uint)(notificationsPerMessage - tooBig);
                }

                //
                // how many notifications per message resulted in how many buffers. We track the
                // split size to provide users with an indication how many times chunks had to
                // be created so they can configure publisher to improve performance.
                //
                if (notificationsPerMessage > 0 && notificationsPerMessage < chunkedMessages.Count) {
                    var splitSize = chunkedMessages.Count / notificationsPerMessage;
                    if (splitSize > MaxMessageSplitRatio) {
                        MaxMessageSplitRatio = splitSize;
                    }
                }
            }
            return chunkedMessages;
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        private List<(int, PubSubMessage)> GetNetworkMessages(IEnumerable<SubscriptionNotificationModel> messages,
            bool isBatched) {
            var result = new List<(int, PubSubMessage)>();
            // Group messages by publisher, then writer group and then by dataset class id
            foreach (var publishers in messages
                .Select(m => (Notification: m, Context: m.Context as WriterGroupMessageContext))
                .Where(m => m.Context != null)
                .GroupBy(m => m.Context.PublisherId)) {
                var publisherId = publishers.Key;
                foreach (var groups in publishers.GroupBy(m => m.Context.WriterGroup)) {
                    var writerGroup = groups.Key;
                    if (writerGroup?.MessageSettings == null) {
                        // Must have a writer group
                        Drop(groups.Select(m => m.Notification));
                        continue;
                    }
                    var encoding = writerGroup.MessageType ?? MessageEncoding.Json;
                    var messageMask = writerGroup.MessageSettings.NetworkMessageContentMask;
                    var hasSamplesPayload = (messageMask & NetworkMessageContentMask.MonitoredItemMessage) != 0;
                    if (hasSamplesPayload && !isBatched) {
                        messageMask |= NetworkMessageContentMask.SingleDataSetMessage;
                    }
                    var networkMessageContentMask = messageMask.ToStackType(encoding);
                    foreach (var dataSetClass in groups
                        .GroupBy(m => m.Context.Writer?.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty)) {

                        var dataSetClassId = dataSetClass.Key;
                        var currentMessage = CreateMessage(writerGroup, encoding,
                            networkMessageContentMask, dataSetClassId, publisherId);
                        var currentNotificationCount = 0;
                        foreach (var message in dataSetClass) {
                            if (message.Context.Writer == null ||
                                (hasSamplesPayload && !encoding.HasFlag(MessageEncoding.Json))) {
                                // Must have a writer or if samples mode, must be json
                                Drop(message.Notification.YieldReturn());
                                continue;
                            }
                            var dataSetMessageContentMask =
                                (message.Context.Writer.MessageSettings?.DataSetMessageContentMask).ToStackType(
                                    message.Context.Writer.DataSetFieldContentMask, encoding);
                            var dataSetFieldContentMask =
                                    message.Context.Writer.DataSetFieldContentMask.ToStackType();

                            if (message.Notification.MessageType != MessageType.Metadata) {
                                Debug.Assert(message.Notification.Notifications != null);
                                var notificationQueues = message.Notification.Notifications
                                    .GroupBy(m => m.DataSetFieldName)
                                    .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray()))
                                    .ToArray();

                                while (notificationQueues.Any(q => q.Count > 0)) {
                                    var orderedNotifications = notificationQueues
                                        .Select(q => q.Count > 0 ? q.Dequeue() : null)
                                        .Where(s => s != null);

                                    if (!hasSamplesPayload) {
                                        // Create regular data set messages
                                        BaseDataSetMessage dataSetMessage = encoding.HasFlag(MessageEncoding.Json)
                                            ? new JsonDataSetMessage {
                                                UseCompatibilityMode = !_useStandardsCompliantEncoding,
                                                DataSetWriterName = message.Context.Writer.DataSetWriterName
                                            }
                                            : new UadpDataSetMessage();

                                        dataSetMessage.DataSetWriterId = message.Notification.SubscriptionId;
                                        dataSetMessage.MessageType = message.Notification.MessageType;
                                        dataSetMessage.MetaDataVersion = message.Notification.MetaData?.ConfigurationVersion
                                            ?? kEmptyConfiguration;
                                        dataSetMessage.DataSetMessageContentMask = dataSetMessageContentMask;
                                        dataSetMessage.Timestamp = message.Notification.Timestamp;
                                        dataSetMessage.SequenceNumber = message.Context.SequenceNumber;
                                        dataSetMessage.Payload = new DataSet(orderedNotifications.ToDictionary(
                                            s => s.DataSetFieldName, s => s.Value), (uint)dataSetFieldContentMask);

                                        AddMessage(dataSetMessage);
                                    }
                                    else {
                                        // Add monitored item message payload to network message to handle backcompat
                                        foreach (var itemNotifications in orderedNotifications.GroupBy(f => f.Id + f.MessageId)) {
                                            var notificationsInGroup = itemNotifications.ToList();
                                            Debug.Assert(notificationsInGroup.Count != 0);
                                            //
                                            // Special monitored item handling for events and conditions. Collate all
                                            // values into a single key value data dictionary extension object value.
                                            // Regular notifications we send as single messages.
                                            //
                                            if (notificationsInGroup.Count > 1 &&
                                                (message.Notification.MessageType == MessageType.Event ||
                                                 message.Notification.MessageType == MessageType.Condition)) {
                                                Debug.Assert(notificationsInGroup
                                                    .Select(n => n.DataSetFieldName).Distinct().Count() == notificationsInGroup.Count,
                                                    "There should not be duplicates in fields in a group.");
                                                Debug.Assert(notificationsInGroup
                                                    .All(n => n.SequenceNumber == notificationsInGroup[0].SequenceNumber),
                                                    "All notifications in the group should have the same sequence number.");

                                                var eventNotification = notificationsInGroup[0]; // No clone, mutate ok.
                                                eventNotification.Value = new DataValue {
                                                    Value = new EncodeableDictionary(notificationsInGroup
                                                        .Select(n => new KeyDataValuePair(n.DataSetFieldName, n.Value)))
                                                };
                                                eventNotification.DataSetFieldName = notificationsInGroup[0].DisplayName;
                                                notificationsInGroup = new List<MonitoredItemNotificationModel> {
                                                    eventNotification
                                                };
                                            }
                                            foreach (var notification in notificationsInGroup) {
                                                var dataSetMessage = new MonitoredItemMessage {
                                                    UseCompatibilityMode = !_useStandardsCompliantEncoding,
                                                    ApplicationUri = message.Notification.ApplicationUri,
                                                    EndpointUrl = message.Notification.EndpointUrl,
                                                    NodeId = notification.NodeId,
                                                    MessageType = message.Notification.MessageType,
                                                    DataSetMessageContentMask = dataSetMessageContentMask,
                                                    Timestamp = message.Notification.Timestamp,
                                                    SequenceNumber = message.Context.SequenceNumber,
                                                    ExtensionFields = message.Context.Writer.DataSet.ExtensionFields,
                                                    Payload = new DataSet(notification.DataSetFieldName, notification.Value,
                                                        (uint)dataSetFieldContentMask)
                                                };
                                                AddMessage(dataSetMessage);
                                            }
                                        }
                                    }

                                    //
                                    // Add message and number of notifications processed count to method result.
                                    // Checks current length and splits if max items reached if configured.
                                    //
                                    void AddMessage(BaseDataSetMessage dataSetMessage) {
                                        currentMessage.Messages.Add(dataSetMessage);
                                        if (writerGroup.MessageSettings?.MaxMessagesPerPublish != null &&
                                            currentMessage.Messages.Count >= writerGroup.MessageSettings.MaxMessagesPerPublish) {
                                            result.Add((currentNotificationCount, currentMessage));
                                            currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                                dataSetClassId, publisherId);
                                            currentNotificationCount = 0;
                                        }
                                    }
                                }
                                currentNotificationCount++;
                            }
                            else if (message.Notification.MetaData != null && !hasSamplesPayload) {
                                if (currentMessage.Messages.Count > 0) {
                                    // Start a new message but first emit current
                                    result.Add((currentNotificationCount, currentMessage));
                                    currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                        dataSetClassId, publisherId);
                                    currentNotificationCount = 0;
                                }
                                PubSubMessage metadataMessage = encoding.HasFlag(MessageEncoding.Json)
                                    ? new JsonMetaDataMessage {
                                        UseAdvancedEncoding = !_useStandardsCompliantEncoding,
                                        UseGzipCompression = encoding.HasFlag(MessageEncoding.Gzip),
                                        DataSetWriterId = message.Notification.SubscriptionId,
                                        MetaData = message.Notification.MetaData,
                                        MessageId = Guid.NewGuid().ToString(),
                                        DataSetWriterGroup = writerGroup.WriterGroupId,
                                        DataSetWriterName = message.Context.Writer.DataSetWriterName
                                    } : new UadpMetaDataMessage {
                                        DataSetWriterId = message.Notification.SubscriptionId,
                                        MetaData = message.Notification.MetaData
                                    };
                                metadataMessage.PublisherId = publisherId;

                                // TODO: Overriding with metadata topic name
                                metadataMessage.RoutingInfo = _enableRoutingInfo ? writerGroup.WriterGroupId : null;
                                result.Add((0, metadataMessage));
                            }
                        }
                        if (currentMessage.Messages.Count > 0) {
                            result.Add((currentNotificationCount, currentMessage));
                        }

                        BaseNetworkMessage CreateMessage(WriterGroupModel writerGroup, MessageEncoding encoding,
                            uint networkMessageContentMask, Guid dataSetClassId, string publisherId) {
                            BaseNetworkMessage currentMessage = encoding.HasFlag(MessageEncoding.Json) ?
                                new JsonNetworkMessage {
                                UseAdvancedEncoding = !_useStandardsCompliantEncoding,
                                UseGzipCompression = encoding.HasFlag(MessageEncoding.Gzip),
                                UseArrayEnvelope = !_useStandardsCompliantEncoding && isBatched,
                                MessageId = () => Guid.NewGuid().ToString(),
                                DataSetWriterGroup = writerGroup.WriterGroupId
                            } : new UadpNetworkMessage {
                                //   WriterGroupId = writerGroup.Index,
                                //   GroupVersion = writerGroup.Version,
                                SequenceNumber = () => SequenceNumber.Increment16(ref _sequenceNumber),
                                Timestamp = DateTime.UtcNow,
                                PicoSeconds = 0
                            };
                            currentMessage.NetworkMessageContentMask = networkMessageContentMask;
                            currentMessage.PublisherId = publisherId;
                            currentMessage.DataSetClassId = dataSetClassId;
                            currentMessage.RoutingInfo = _enableRoutingInfo ? writerGroup.WriterGroupId : null;
                            return currentMessage;
                        }
                    }
                }
            }
            return result;
        }

        private void Drop(IEnumerable<SubscriptionNotificationModel> messages) {
            int totalNotifications = messages.Sum(m => m?.Notifications?.Count ?? 0);
            NotificationsDroppedCount += (uint)totalNotifications;
            _logger.Warning("Dropped {totalNotifications} values", totalNotifications);
        }

        private static readonly ConfigurationVersionDataType kEmptyConfiguration =
            new ConfigurationVersionDataType { MajorVersion = 1u };
        private readonly ILogger _logger;
        private readonly bool _enableRoutingInfo;
        private uint _sequenceNumber;
        private readonly bool _useStandardsCompliantEncoding;
    }
}
