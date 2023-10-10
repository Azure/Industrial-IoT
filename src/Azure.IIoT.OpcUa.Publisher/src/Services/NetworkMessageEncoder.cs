// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;

    /// <summary>
    /// Creates PubSub encoded messages
    /// </summary>
    public sealed class NetworkMessageEncoder : IMessageEncoder, IDisposable
    {
        /// <inheritdoc/>
        public long NotificationsDroppedCount { get; private set; }

        /// <inheritdoc/>
        public long NotificationsProcessedCount { get; private set; }

        /// <inheritdoc/>
        public long MessagesProcessedCount { get; private set; }

        /// <inheritdoc/>
        public double AvgNotificationsPerMessage { get; private set; }

        /// <inheritdoc/>
        public double AvgMessageSize { get; private set; }

        /// <inheritdoc/>
        public double MaxMessageSplitRatio { get; private set; }

        /// <summary>
        /// Create instance of NetworkMessageEncoder.
        /// </summary>
        /// <param name="options"> injected configuration. </param>
        /// <param name="metrics"> Metrics context </param>
        /// <param name="logger"> Logger to be used for reporting. </param>
        public NetworkMessageEncoder(IOptions<PublisherOptions> options, IMetricsContext metrics,
            ILogger<NetworkMessageEncoder> logger)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            _logger = logger;
            _options = options;
            InitializeMetrics(metrics);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _meter.Dispose();
        }

        /// <inheritdoc/>
        public IEnumerable<(IEvent Event, Action OnSent)> Encode(Func<IEvent> factory,
            IEnumerable<IOpcUaSubscriptionNotification> notifications, int maxMessageSize, bool asBatch)
        {
            try
            {
                //
                // by design all messages are generated in the same session context, therefore it is safe to
                // get the first message's context
                //
                var encodingContext = notifications
                    .FirstOrDefault(m => m.ServiceMessageContext != null)?.ServiceMessageContext;
                var chunkedMessages = new List<(IEvent, Action)>();
                if (encodingContext == null)
                {
                    // Drop all messages
                    Drop(notifications);
                    return chunkedMessages;
                }

                var networkMessages = GetNetworkMessages(notifications, asBatch);
                foreach (var (notificationsPerMessage, networkMessage, topic, retain, ttl, qos, onSent) in networkMessages)
                {
                    var chunks = networkMessage.Encode(encodingContext, maxMessageSize);
                    var notificationsPerChunk = notificationsPerMessage / (double)chunks.Count;
                    var validChunks = 0;
                    foreach (var body in chunks)
                    {
                        if (body.Length == 0)
                        {
                            //
                            // Failed to press a notification into message size limit
                            // This is somewhat correct as the smallest dropped chunk is
                            // a message containing only a single data set message which
                            // contains (parts) of a notification.
                            //
                            _logger.LogWarning("Resulting chunk is too large, dropped a notification.");
                            continue;
                        }
                        validChunks++;
                        AvgMessageSize = ((AvgMessageSize * MessagesProcessedCount) + body.Length) /
                            (MessagesProcessedCount + 1);
                        AvgNotificationsPerMessage = ((AvgNotificationsPerMessage * MessagesProcessedCount) +
                            notificationsPerChunk) / (MessagesProcessedCount + 1);
                        MessagesProcessedCount++;
                    }

                    if (validChunks > 0)
                    {
                        var chunkedMessage = factory()
                            .AddProperty(OpcUa.Constants.MessagePropertySchemaKey, networkMessage.MessageSchema)
                            .SetTimestamp(DateTime.UtcNow)
                            .SetContentEncoding(networkMessage.ContentEncoding)
                            .SetContentType(networkMessage.ContentType)
                            .SetTopic(topic)
                            .SetRetain(retain)
                            .SetTtl(ttl)
                            .SetQoS(qos)
                            .AddBuffers(chunks)
                            ;

                        if (_options.Value.UseStandardsCompliantEncoding != true)
                        {
                            chunkedMessage = chunkedMessage
                                .AddProperty("$$ContentType", networkMessage.ContentType)
                                .AddProperty("$$ContentEncoding", networkMessage.ContentEncoding);
                        }
                        if (_options.Value.EnableDataSetRoutingInfo ?? false)
                        {
                            chunkedMessage.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                networkMessage.DataSetWriterGroup);
                        }

                        _logger.LogDebug(
                            "{Count} Notifications encoded into a network message (chunks:{Chunks})...",
                            notificationsPerMessage, validChunks);

                        chunkedMessages.Add((chunkedMessage, onSent));
                    }
                    else
                    {
                        // Nothing to send, complete here
                        onSent();
                    }

                    // We dropped a number of notifications but processed the remainder successfully
                    var tooBig = chunks.Count - validChunks;
                    NotificationsDroppedCount += tooBig;
                    if (notificationsPerMessage > tooBig)
                    {
                        NotificationsProcessedCount += notificationsPerMessage - tooBig;
                    }

                    //
                    // how many notifications per message resulted in how many buffers. We track the
                    // split size to provide users with an indication how many times chunks had to
                    // be created so they can configure publisher to improve performance.
                    //
                    if (notificationsPerMessage > 0 && notificationsPerMessage < validChunks)
                    {
                        var splitSize = validChunks / notificationsPerMessage;
                        if (splitSize > MaxMessageSplitRatio)
                        {
                            MaxMessageSplitRatio = splitSize;
                        }
                    }
                }
                return chunkedMessages;
            }
            finally
            {
#if DEBUG
                notifications.ToList().ForEach(a => a.DebugAssertProcessed());
#endif
            }
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        private List<(int, PubSubMessage, string, bool, TimeSpan, QoS, Action)> GetNetworkMessages(
            IEnumerable<IOpcUaSubscriptionNotification> messages, bool isBatched)
        {
            var standardsCompliant = _options.Value.UseStandardsCompliantEncoding ?? false;
            var result = new List<(int, PubSubMessage, string, bool, TimeSpan, QoS, Action)>();
            // Group messages by publisher, then writer group and then by dataset class id
            foreach (var topics in messages
                .Select(m => (Notification: m, Context: (m.Context as WriterGroupMessageContext)!))
                .Where(m => m.Context != null)
                .GroupBy(m => m.Context!.Topic))
            {
                var topic = topics.Key;
                foreach (var publishers in topics.GroupBy(m => m.Context.PublisherId))
                {
                    var publisherId = publishers.Key;
                    foreach (var groups in publishers.GroupBy(m => m.Context.WriterGroup))
                    {
                        var writerGroup = groups.Key;
                        if (writerGroup?.MessageSettings == null)
                        {
                            // Must have a writer group
                            Drop(groups.Select(m => m.Notification));
                            continue;
                        }
                        var encoding = writerGroup.MessageType ?? MessageEncoding.Json;
                        var qos = writerGroup.QoS ?? _options.Value.DefaultQualityOfService ?? QoS.AtLeastOnce;
                        var messageMask = writerGroup.MessageSettings.NetworkMessageContentMask;
                        var hasSamplesPayload = (messageMask & NetworkMessageContentMask.MonitoredItemMessage) != 0;
                        if (hasSamplesPayload && !isBatched)
                        {
                            messageMask |= NetworkMessageContentMask.SingleDataSetMessage;
                        }
                        var networkMessageContentMask = messageMask.ToStackType(encoding);
                        foreach (var dataSetClass in groups
                            .GroupBy(m => m.Context.Writer?.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty))
                        {
                            var dataSetClassId = dataSetClass.Key;
                            var currentMessage = CreateMessage(writerGroup, encoding,
                                networkMessageContentMask, dataSetClassId, publisherId);
                            var currentNotifications = new List<IOpcUaSubscriptionNotification>();
                            foreach (var (Notification, Context) in dataSetClass)
                            {
                                if (Context.Writer == null ||
                                    (hasSamplesPayload && !encoding.HasFlag(MessageEncoding.Json)))
                                {
                                    // Must have a writer or if samples mode, must be json
                                    Drop(Notification.YieldReturn());
                                    continue;
                                }
                                var dataSetMessageContentMask =
                                    (Context.Writer.MessageSettings?.DataSetMessageContentMask).ToStackType(
                                        Context.Writer.DataSetFieldContentMask, encoding);
                                var dataSetFieldContentMask =
                                        Context.Writer.DataSetFieldContentMask.ToStackType();

                                if (Notification.MessageType != MessageType.Metadata)
                                {
                                    Debug.Assert(Notification.Notifications != null);
                                    if (Notification.MessageType == MessageType.KeepAlive)
                                    {
                                        Debug.Assert(Notification.Notifications.Count == 0);
                                        if (hasSamplesPayload)
                                        {
                                            Drop(Notification.YieldReturn());
                                            continue;
                                        }

                                        // Create regular data set messages
                                        BaseDataSetMessage dataSetMessage = encoding.HasFlag(MessageEncoding.Json)
                                            ? new JsonDataSetMessage
                                            {
                                                UseCompatibilityMode = !standardsCompliant,
                                                DataSetWriterName = Context.Writer.DataSetWriterName
                                                    ?? Constants.DefaultDataSetWriterName
                                            }
                                            : new UadpDataSetMessage();

                                        dataSetMessage.DataSetWriterId = Notification.SubscriptionId;
                                        dataSetMessage.MessageType = MessageType.KeepAlive;
                                        dataSetMessage.MetaDataVersion = Notification.MetaData?.ConfigurationVersion
                                            ?? kEmptyConfiguration;
                                        dataSetMessage.DataSetMessageContentMask = dataSetMessageContentMask;
                                        dataSetMessage.Timestamp = GetTimestamp(Notification);
                                        dataSetMessage.SequenceNumber = Context.NextWriterSequenceNumber();

                                        AddMessage(dataSetMessage);
                                        currentNotifications.Add(Notification);
                                        continue;
                                    }

                                    var notificationQueues = Notification.Notifications
                                        .GroupBy(m => m.DataSetFieldName)
                                        .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray()))
                                        .ToArray();
                                    var notComplete = notificationQueues.Any(q => q.Count > 0);
                                    if (!notComplete)
                                    {
                                        // Already completed so we cannot complete it as an encoded message.
                                        Drop(Notification.YieldReturn());
                                        continue;
                                    }
                                    while (notComplete)
                                    {
                                        var orderedNotifications = notificationQueues
                                            .Select(q => q.Count > 0 ? q.Dequeue() : null!)
                                            .Where(s => s?.DataSetFieldName != null)
                                            .ToList()
                                            ;
                                        notComplete = notificationQueues.Any(q => q.Count > 0);

                                        if (!hasSamplesPayload)
                                        {
                                            // Create regular data set messages
                                            BaseDataSetMessage dataSetMessage = encoding.HasFlag(MessageEncoding.Json)
                                                ? new JsonDataSetMessage
                                                {
                                                    UseCompatibilityMode = !standardsCompliant,
                                                    DataSetWriterName = Context.Writer.DataSetWriterName
                                                        ?? Constants.DefaultDataSetWriterName
                                                }
                                                : new UadpDataSetMessage();

                                            dataSetMessage.DataSetWriterId = Notification.SubscriptionId;
                                            dataSetMessage.MessageType = Notification.MessageType;
                                            dataSetMessage.MetaDataVersion = Notification.MetaData?.ConfigurationVersion
                                                ?? kEmptyConfiguration;
                                            dataSetMessage.DataSetMessageContentMask = dataSetMessageContentMask;
                                            dataSetMessage.Timestamp = GetTimestamp(Notification);
                                            dataSetMessage.SequenceNumber = Context.NextWriterSequenceNumber();
                                            dataSetMessage.Payload = new DataSet(orderedNotifications.ToDictionary(
                                                s => s.DataSetFieldName!, s => s.Value), (uint)dataSetFieldContentMask);

                                            AddMessage(dataSetMessage);
                                        }
                                        else
                                        {
                                            // Add monitored item message payload to network message to handle backcompat
                                            foreach (var itemNotifications in orderedNotifications.GroupBy(f => f.Id + f.MessageId))
                                            {
                                                var notificationsInGroup = itemNotifications.ToList();
                                                Debug.Assert(notificationsInGroup.Count != 0);
                                                //
                                                // Special monitored item handling for events and conditions. Collate all
                                                // values into a single key value data dictionary extension object value.
                                                // Regular notifications we send as single messages.
                                                //
                                                if (notificationsInGroup.Count > 1 &&
                                                    (Notification.MessageType == MessageType.Event ||
                                                     Notification.MessageType == MessageType.Condition))
                                                {
                                                    Debug.Assert(notificationsInGroup
                                                        .Select(n => n.DataSetFieldName).Distinct().Count() == notificationsInGroup.Count,
                                                        "There should not be duplicates in fields in a group.");
                                                    Debug.Assert(notificationsInGroup
                                                        .All(n => n.SequenceNumber == notificationsInGroup[0].SequenceNumber),
                                                        "All notifications in the group should have the same sequence number.");

                                                    var eventNotification = notificationsInGroup[0]; // No clone, mutate ok.
                                                    eventNotification.Value = new DataValue
                                                    {
                                                        Value = new EncodeableDictionary(notificationsInGroup
                                                            .Select(n => new KeyDataValuePair(n.DataSetFieldName!, n.Value)))
                                                    };
                                                    eventNotification.DataSetFieldName = notificationsInGroup[0].DataSetName;
                                                    notificationsInGroup = new List<MonitoredItemNotificationModel>
                                                    {
                                                        eventNotification
                                                    };
                                                }
                                                foreach (var notification in notificationsInGroup)
                                                {
                                                    if (notification?.DataSetFieldName != null)
                                                    {
                                                        _logger.LogTrace("Processing notification: {Notification}",
                                                            notification.ToString());
                                                        var dataSetMessage = new MonitoredItemMessage
                                                        {
                                                            UseCompatibilityMode = !standardsCompliant,
                                                            ApplicationUri = Notification.ApplicationUri,
                                                            EndpointUrl = Notification.EndpointUrl,
                                                            WriterGroupId = writerGroup.WriterGroupId,
                                                            NodeId = notification.NodeId,
                                                            MessageType = Notification.MessageType,
                                                            DataSetMessageContentMask = dataSetMessageContentMask,
                                                            Timestamp = GetTimestamp(Notification),
                                                            SequenceNumber = Context.NextWriterSequenceNumber(),
                                                            ExtensionFields = Context.Writer.DataSet?.ExtensionFields,
                                                            Payload = new DataSet(notification.DataSetFieldName,
                                                                notification.Value, (uint)dataSetFieldContentMask)
                                                        };
                                                        AddMessage(dataSetMessage);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    currentNotifications.Add(Notification);

                                    //
                                    // Add message and number of notifications processed count to method result.
                                    // Checks current length and splits if max items reached if configured.
                                    //
                                    void AddMessage(BaseDataSetMessage dataSetMessage)
                                    {
                                        currentMessage.Messages.Add(dataSetMessage);
                                        var maxMessagesToPublish = writerGroup.MessageSettings?.MaxDataSetMessagesPerPublish ??
                                            _options.Value.DefaultMaxDataSetMessagesPerPublish;
                                        if (maxMessagesToPublish != null && currentMessage.Messages.Count >= maxMessagesToPublish)
                                        {
                                            result.Add((currentNotifications.Count, currentMessage, topic, false, default, qos,
                                                () => currentNotifications.ForEach(n => n.Dispose())));
#if DEBUG
                                            currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                            currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                                dataSetClassId, publisherId);
                                            currentNotifications = new List<IOpcUaSubscriptionNotification>();
                                        }
                                    }
                                }
                                else if (Notification.MetaData != null && !hasSamplesPayload)
                                {
                                    if (currentMessage.Messages.Count > 0)
                                    {
                                        // Start a new message but first emit current
                                        result.Add((currentNotifications.Count, currentMessage, topic, false, default, qos,
                                            () => currentNotifications.ForEach(n => n.Dispose())));
#if DEBUG
                                        currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                        currentMessage = CreateMessage(writerGroup, encoding, networkMessageContentMask,
                                            dataSetClassId, publisherId);
                                        currentNotifications = new List<IOpcUaSubscriptionNotification>();
                                    }
                                    PubSubMessage metadataMessage = encoding.HasFlag(MessageEncoding.Json)
                                        ? new JsonMetaDataMessage
                                        {
                                            UseAdvancedEncoding = !standardsCompliant,
                                            UseGzipCompression = encoding.HasFlag(MessageEncoding.IsGzipCompressed),
                                            DataSetWriterId = Notification.SubscriptionId,
                                            MetaData = Notification.MetaData,
                                            MessageId = Guid.NewGuid().ToString(),
                                            DataSetWriterName = Context.Writer.DataSetWriterName ?? Constants.DefaultDataSetWriterName
                                        } : new UadpMetaDataMessage
                                        {
                                            DataSetWriterId = Notification.SubscriptionId,
                                            MetaData = Notification.MetaData
                                        };
                                    metadataMessage.PublisherId = publisherId;
                                    metadataMessage.DataSetWriterGroup = writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId;

                                    result.Add((0, metadataMessage, Context.MetaDataTopic, true,
                                        Context.Writer.MetaDataUpdateTime ?? default, QoS.AtLeastOnce, Notification.Dispose));
#if DEBUG
                                    Notification.MarkProcessed();
#endif
                                }
                            }
                            if (currentMessage.Messages.Count > 0)
                            {
                                result.Add((currentNotifications.Count, currentMessage, topic, false, default, qos,
                                    () => currentNotifications.ForEach(n => n.Dispose())));
#if DEBUG
                                currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                            }
                            else
                            {
                                Debug.Assert(currentNotifications.Count == 0);
                            }

                            BaseNetworkMessage CreateMessage(WriterGroupModel writerGroup, MessageEncoding encoding,
                                uint networkMessageContentMask, Guid dataSetClassId, string publisherId)
                            {
                                BaseNetworkMessage currentMessage = encoding.HasFlag(MessageEncoding.Json) ?
                                    new JsonNetworkMessage
                                    {
                                        UseAdvancedEncoding = !standardsCompliant,
                                        UseGzipCompression = encoding.HasFlag(MessageEncoding.IsGzipCompressed),
                                        UseArrayEnvelope = !standardsCompliant && isBatched,
                                        MessageId = () => Guid.NewGuid().ToString()
                                    } : new UadpNetworkMessage
                                    {
                                        //   WriterGroupId = writerGroup.Index,
                                        //   GroupVersion = writerGroup.Version,
                                        SequenceNumber = () => SequenceNumber.Increment16(ref _sequenceNumber),
                                        Timestamp = DateTime.UtcNow,
                                        PicoSeconds = 0
                                    };
                                currentMessage.NetworkMessageContentMask = networkMessageContentMask;
                                currentMessage.PublisherId = publisherId;
                                currentMessage.DataSetClassId = dataSetClassId;
                                currentMessage.DataSetWriterGroup = writerGroup.WriterGroupId ?? Constants.DefaultWriterGroupId;
                                return currentMessage;
                            }

                            DateTime? GetTimestamp(IOpcUaSubscriptionNotification Notification)
                            {
                                switch (_options.Value.MessageTimestamp)
                                {
                                    case MessageTimestamp.EncodingTimeUtc:
                                        return DateTime.UtcNow;
                                    case MessageTimestamp.PublishTime:
                                        return Notification.PublishTimestamp;
                                    default:
                                        return Notification.CreatedTimestamp;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void Drop(IEnumerable<IOpcUaSubscriptionNotification> messages)
        {
            var totalNotifications = 0;
            foreach (var message in messages)
            {
                totalNotifications += message.Notifications?.Count ?? 0;
#if DEBUG
                message.MarkProcessed();
#endif
                message.Dispose();
            }

            if (totalNotifications > 0)
            {
                _logger.LogWarning("Dropped {TotalNotifications} values", totalNotifications);
                NotificationsDroppedCount += totalNotifications;
            }
        }

        /// <summary>
        /// Create observable metric registrations
        /// </summary>
        /// <param name="metrics"></param>
        private void InitializeMetrics(IMetricsContext metrics)
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_encoded_notifications",
                () => new Measurement<long>(NotificationsProcessedCount, metrics.TagList), "Notifications",
                "Number of successfully processed subscription notifications received from OPC client.");
            _meter.CreateObservableCounter("iiot_edge_publisher_dropped_notifications",
                () => new Measurement<long>(NotificationsDroppedCount, metrics.TagList), "Notifications",
                "Number of incoming subscription notifications that are too big to be processed based " +
                "on the message size limits or other issues with the notification.");
            _meter.CreateObservableCounter("iiot_edge_publisher_processed_messages",
                () => new Measurement<long>(MessagesProcessedCount, metrics.TagList), "Messages",
                "Number of successfully generated messages that are to be sent using the message sender");
            _meter.CreateObservableGauge("iiot_edge_publisher_notifications_per_message_average",
                () => new Measurement<double>(AvgNotificationsPerMessage, metrics.TagList), "Notifications/Message",
                "Average subscription notifications packed into a message");
            _meter.CreateObservableGauge("iiot_edge_publisher_encoded_message_size_average",
                () => new Measurement<double>(AvgMessageSize, metrics.TagList), "Bytes",
                "Average size of a message through the lifetime of the encoder.");
            _meter.CreateObservableGauge("iiot_edge_publisher_chunk_size_average",
                () => new Measurement<double>(AvgMessageSize / (4 * 1024), metrics.TagList), "4kb Chunks",
                "IoT Hub chunk size average");
            _meter.CreateObservableGauge("iiot_edge_publisher_message_split_ratio_max",
                () => new Measurement<double>(MaxMessageSplitRatio, metrics.TagList), "Splits",
                "The message split ration specifies into how many messages a subscription notification had to be split. " +
                "Less is better for performance. If the number is large user should attempt to limit the number of " +
                "notifications in a message using configuration.");
        }

        private static readonly ConfigurationVersionDataType kEmptyConfiguration = new() { MajorVersion = 1u };
        private uint _sequenceNumber; // TODO: Use writer group context
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }
}
