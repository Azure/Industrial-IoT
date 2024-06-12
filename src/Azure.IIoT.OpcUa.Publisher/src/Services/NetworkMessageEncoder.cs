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
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Linq;
    using System.Text;

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
        public NetworkMessageEncoder(IOptions<PublisherOptions> options,
            IMetricsContext metrics, ILogger<NetworkMessageEncoder> logger)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            _logger = logger;
            _options = options;
            InitializeMetrics(metrics);
            _logNotifications = _options.Value.DebugLogEncodedNotifications == true;
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
                var chunkedMessages = new List<(IEvent, Action)>();
                foreach (var m in GetNetworkMessages(notifications, asBatch))
                {
                    if (m.encodingContext == null)
                    {
                        _logger.LogError(
                            "Missing service message context for network message - dropping notification.");
                        NotificationsDroppedCount++;
                        m.onSent();
                        continue;
                    }

                    var chunks = m.networkMessage.Encode(m.encodingContext, maxMessageSize);
                    var notificationsPerChunk = m.notificationsPerMessage / (double)chunks.Count;
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
                            .AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                m.networkMessage.MessageSchema)
                            .SetTimestamp(DateTime.UtcNow)
                            .SetContentEncoding(m.networkMessage.ContentEncoding)
                            .SetContentType(m.networkMessage.ContentType)
                            .SetTopic(m.topic)
                            .SetRetain(m.retain)
                            .SetQoS(m.qos)
                            .AddBuffers(chunks)
                            ;

                        if (m.ttl != default)
                        {
                            chunkedMessage = chunkedMessage
                                .SetTtl(m.ttl);
                        }

                        if (m.networkMessage is IEventSchema schema)
                        {
                            chunkedMessage = chunkedMessage.SetSchema(schema);
                        }

                        if (_options.Value.UseStandardsCompliantEncoding != true)
                        {
                            chunkedMessage = chunkedMessage
                                .AddProperty("$$ContentType", m.networkMessage.ContentType)
                                .AddProperty("$$ContentEncoding", m.networkMessage.ContentEncoding);
                        }
                        if (_options.Value.EnableDataSetRoutingInfo ?? false)
                        {
                            chunkedMessage.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                m.networkMessage.DataSetWriterGroup);
                        }

                        _logger.LogDebug(
                            "{Count} Notifications encoded into a network message (chunks:{Chunks})...",
                            m.notificationsPerMessage, validChunks);

                        chunkedMessages.Add((chunkedMessage, m.onSent));
                    }
                    else
                    {
                        // Nothing to send, complete here
                        m.onSent();
                    }

                    // We dropped a number of notifications but processed the remainder successfully
                    var tooBig = chunks.Count - validChunks;
                    NotificationsDroppedCount += tooBig;
                    if (m.notificationsPerMessage > tooBig)
                    {
                        NotificationsProcessedCount += m.notificationsPerMessage - tooBig;
                    }

                    //
                    // how many notifications per message resulted in how many buffers. We track the
                    // split size to provide users with an indication how many times chunks had to
                    // be created so they can configure publisher to improve performance.
                    //
                    if (m.notificationsPerMessage > 0 && m.notificationsPerMessage < validChunks)
                    {
                        var splitSize = validChunks / m.notificationsPerMessage;
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
        /// Encoded message
        /// </summary>
        /// <param name="notificationsPerMessage"></param>
        /// <param name="networkMessage"></param>
        /// <param name="topic"></param>
        /// <param name="retain"></param>
        /// <param name="ttl"></param>
        /// <param name="qos"></param>
        /// <param name="onSent"></param>
        /// <param name="encodingContext"></param>
        private record struct EncodedMessage(int notificationsPerMessage,
            PubSubMessage networkMessage, string topic, bool retain,
            TimeSpan ttl, QoS qos, Action onSent,
            IServiceMessageContext? encodingContext = null);

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        private List<EncodedMessage> GetNetworkMessages(
            IEnumerable<IOpcUaSubscriptionNotification> messages, bool isBatched)
        {
            var standardsCompliant = _options.Value.UseStandardsCompliantEncoding ?? false;
            var result = new List<EncodedMessage>();

            static QoS GetQos(WriterGroupMessageContext context, QoS? defaultQos)
            {
                return context.Qos ?? defaultQos ?? QoS.AtLeastOnce;
            }
            // Group messages by topic and qos, then writer group and then by dataset class id
            foreach (var topics in messages
                .Select(m => (Notification: m, Context: (m.Context as WriterGroupMessageContext)!))
                .Where(m => m.Context != null)
                .GroupBy(m => (m.Context!.Topic,
                    GetQos(m.Context, _options.Value.DefaultQualityOfService))))
            {
                var (topic, qos) = topics.Key;
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
                        var messageMask = writerGroup.MessageSettings.NetworkMessageContentMask;
                        var hasSamplesPayload = (messageMask & NetworkMessageContentFlags.MonitoredItemMessage) != 0;
                        if (hasSamplesPayload && !isBatched)
                        {
                            messageMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                        }
                        var namespaceFormat =
                            writerGroup.MessageSettings?.NamespaceFormat ??
                            // _options.Value.DefaultNamespaceFormat ?? // TODO: Fix tests
                            NamespaceFormat.Uri;
                        foreach (var dataSetClass in groups
                            .GroupBy(m => m.Context.Writer?.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty))
                        {
                            var dataSetClassId = dataSetClass.Key;
                            BaseNetworkMessage? currentMessage = null;
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

                                var dataSetMessageContentMask = Context.Writer.MessageSettings?.DataSetMessageContentMask;
                                var dataSetFieldContentMask = Context.Writer.DataSetFieldContentMask;

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
                                        var dataSetMessage = PubSubMessage.CreateDataSetMessage(encoding,
                                            GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                            Notification.MessageType, GetDataSetWriterName(Notification, Context),
                                            Notification.SubscriptionId, new DataSet(),
                                            dataSetMessageContentMask, standardsCompliant, Notification.MetaData);

                                        AddMessage(dataSetMessage);
                                        LogNotification(Notification, false);
                                        currentNotifications.Add(Notification);
                                        continue;
                                    }

                                    var notificationQueues = Notification.Notifications
                                        .GroupBy(m => m.DataSetFieldName)
                                        .Select(c => new Queue<MonitoredItemNotificationModel>(
                                            c
                                            .OrderBy(m => m.Value?.SourceTimestamp)
                                            .ToArray()))
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
                                            var dataSetMessage = PubSubMessage.CreateDataSetMessage(encoding,
                                                GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                                Notification.MessageType, GetDataSetWriterName(Notification, Context),
                                                Notification.SubscriptionId, new DataSet(orderedNotifications.ToDictionary(
                                                    s => s.DataSetFieldName!, s => s.Value), dataSetFieldContentMask),
                                                dataSetMessageContentMask, standardsCompliant, Notification.MetaData);

                                            AddMessage(dataSetMessage);
                                            LogNotification(Notification, false);
                                        }
                                        else
                                        {
                                            // Add monitored item message payload to network message to handle backcompat
                                            foreach (var itemNotifications in orderedNotifications
                                                .GroupBy(f => f.Id + f.MessageId))
                                            {
                                                var notificationsInGroup = itemNotifications.ToList();
                                                Debug.Assert(notificationsInGroup.Count != 0);
                                                //
                                                // Special monitored item handling for events and conditions. Collate all
                                                // values into a single key value data dictionary extension object value.
                                                // Regular notifications we send as single messages.
                                                //
                                                if (notificationsInGroup.Count > 1)
                                                {
                                                    if (Notification.MessageType == MessageType.Event ||
                                                        Notification.MessageType == MessageType.Condition)
                                                    {
                                                        Debug.Assert(notificationsInGroup
                                                            .Select(n => n.DataSetFieldName).Distinct().Count() == notificationsInGroup.Count,
                                                            "There should not be duplicates in fields in a group.");
                                                        Debug.Assert(notificationsInGroup
                                                            .All(n => n.SequenceNumber == notificationsInGroup[0].SequenceNumber),
                                                            "All notifications in the group should have the same sequence number.");

                                                        var eventNotification = notificationsInGroup[0] with
                                                        {
                                                            Value = new DataValue
                                                            {
                                                                Value = new EncodeableDictionary(notificationsInGroup
                                                                    .Select(n => new KeyDataValuePair(n.DataSetFieldName!, n.Value)))
                                                            },
                                                            DataSetFieldName = notificationsInGroup[0].DataSetName
                                                        };
                                                        notificationsInGroup = new List<MonitoredItemNotificationModel>
                                                        {
                                                            eventNotification
                                                        };
                                                    }
                                                    else if (_options.Value.RemoveDuplicatesFromBatch ?? false)
                                                    {
                                                        notificationsInGroup = notificationsInGroup
                                                            .OrderByDescending(k => k.Value?.SourceTimestamp) // Descend from latest
                                                            .DistinctBy(k => k.DataSetFieldName) // Only leave the latest values
                                                            .ToList();
                                                    }
                                                }
                                                foreach (var notification in notificationsInGroup)
                                                {
                                                    if (notification.DataSetFieldName != null)
                                                    {
                                                        var dataSetMessage = PubSubMessage.CreateMonitoredItemMessage(writerGroup.Name,
                                                            encoding, GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                                            Notification.MessageType, notification.NodeId, Notification.EndpointUrl,
                                                            Notification.ApplicationUri, new DataSet(notification.DataSetFieldName,
                                                                notification.Value, dataSetFieldContentMask), dataSetMessageContentMask,
                                                            standardsCompliant, Context.Writer.DataSet?.ExtensionFields);
                                                        AddMessage(dataSetMessage);
                                                        LogNotification(notification);
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
                                        currentMessage ??= PubSubMessage.CreateNetworkMessage(writerGroup.Name
                                            ?? Constants.DefaultWriterGroupName, encoding, messageMask,
                                            () => SequenceNumber.Increment16(ref _sequenceNumber),
                                            dataSetClassId, publisherId, namespaceFormat, standardsCompliant, isBatched);
                                        currentMessage.Messages.Add(dataSetMessage);

                                        var maxMessagesToPublish = writerGroup.MessageSettings?.MaxDataSetMessagesPerPublish ??
                                            _options.Value.DefaultMaxDataSetMessagesPerPublish;
                                        if (maxMessagesToPublish != null && currentMessage.Messages.Count >= maxMessagesToPublish)
                                        {
                                            result.Add(new EncodedMessage(currentNotifications.Count, currentMessage,
                                                topic, false, default, qos, () => currentNotifications.ForEach(n => n.Dispose()),
                                                Notification.ServiceMessageContext));
#if DEBUG
                                            currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                            currentMessage = null;
                                            currentNotifications = new List<IOpcUaSubscriptionNotification>();
                                        }
                                    }
                                }
                                else if (Notification.MetaData != null && !hasSamplesPayload)
                                {
                                    if (currentMessage?.Messages.Count > 0)
                                    {
                                        // Start a new message but first emit current
                                        result.Add(new EncodedMessage(currentNotifications.Count, currentMessage,
                                            topic, false, default, qos, () => currentNotifications.ForEach(n => n.Dispose()),
                                            Notification.ServiceMessageContext));
#if DEBUG
                                        currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                        currentMessage = null;
                                        currentNotifications = new List<IOpcUaSubscriptionNotification>();
                                    }

                                    var metadataMessage = PubSubMessage.CreateMetaDataMessage(
                                        writerGroup.Name ?? Constants.DefaultWriterGroupName, encoding, publisherId,
                                        GetDataSetWriterName(Notification, Context), Notification.SubscriptionId,
                                        Notification.MetaData, namespaceFormat, standardsCompliant);
                                    result.Add(new EncodedMessage(0, metadataMessage, topic, true,
                                        Context.Writer.MetaDataUpdateTime ?? default, qos, Notification.Dispose,
                                            Notification.ServiceMessageContext));
#if DEBUG
                                    Notification.MarkProcessed();
#endif
                                    LogNotification(Notification, false);
                                }
                            }
                            if (currentMessage?.Messages.Count > 0)
                            {
                                result.Add(new EncodedMessage(currentNotifications.Count, currentMessage, topic, false,
                                    default, qos, () => currentNotifications.ForEach(n => n.Dispose()),
                                    currentNotifications.LastOrDefault()?.ServiceMessageContext));
#if DEBUG
                                currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                            }
                            else
                            {
                                Debug.Assert(currentNotifications.Count == 0);
                            }

                            static string GetDataSetWriterName(IOpcUaSubscriptionNotification Notification,
                                WriterGroupMessageContext Context)
                            {
                                var dataSetWriterName = Context.Writer.DataSetWriterName
                                    ?? Constants.DefaultDataSetWriterName;
                                var dataSetName = Notification.DataSetName;
                                if (!string.IsNullOrWhiteSpace(dataSetName))
                                {
                                    return dataSetWriterName + "|" + dataSetName;
                                }
                                return dataSetWriterName;
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

        /// <summary>
        /// Drop and log messages
        /// </summary>
        /// <param name="messages"></param>
        private void Drop(IEnumerable<IOpcUaSubscriptionNotification> messages)
        {
            var totalNotifications = 0;
            foreach (var message in messages)
            {
                LogNotification(message, true);
                totalNotifications += message.Notifications?.Count ?? 0;
#if DEBUG
                message.MarkProcessed();
#endif
                message.Dispose();
            }

            if (totalNotifications > 0)
            {
                _logger.LogWarning("Dropped {TotalNotifications} values",
                    totalNotifications);
                NotificationsDroppedCount += totalNotifications;
            }
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dropped"></param>
        private void LogNotification(IOpcUaSubscriptionNotification args, bool dropped)
        {
            if (!_logNotifications)
            {
                return;
            }
            // Filter fields to log
            var notifications = Stringify(args.Notifications);
            if (!string.IsNullOrEmpty(notifications))
            {
                _logger.LogInformation(
                    "{Action}|{PublishTime:hh:mm:ss:ffffff}|#{Seq}:{PublishSeq}|{MessageType}|{Subscription}|{Items}",
                    dropped ? "!!!! Dropped !!!! " : "Encoded", args.PublishTimestamp, args.SequenceNumber,
                    args.PublishSequenceNumber?.ToString(CultureInfo.CurrentCulture) ?? "-", args.MessageType,
                    args.SubscriptionName, notifications);
            }
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        private void LogNotification(MonitoredItemNotificationModel args)
        {
            if (!_logNotifications)
            {
                return;
            }
            // Filter fields to log
            var notifications = Stringify(args.YieldReturn());
            if (!string.IsNullOrEmpty(notifications))
            {
                _logger.LogInformation("Sample|{Items}", notifications);
            }
        }

        private static string Stringify(IEnumerable<MonitoredItemNotificationModel> notifications)
        {
            var sb = new StringBuilder();
            foreach (var item in notifications
                .Where(n => (n.Flags & MonitoredItemSourceFlags.ModelChanges) == 0))
            {
                sb
                    .AppendLine()
                    .Append("   |")
                    .Append(item.Value?.ServerTimestamp
                        .ToString("hh:mm:ss:ffffff", CultureInfo.CurrentCulture))
                    .Append('|')
                    .Append(item.DataSetFieldName ?? item.DataSetName)
                    .Append('|')
                    .Append(item.Value?.SourceTimestamp
                        .ToString("hh:mm:ss:ffffff", CultureInfo.CurrentCulture))
                    .Append('|')
                    .Append(item.Value?.Value)
                    .Append('|')
                    .Append(item.Value?.StatusCode)
                    .Append('|')
                    ;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Create observable metric registrations
        /// </summary>
        /// <param name="metrics"></param>
        private void InitializeMetrics(IMetricsContext metrics)
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_encoded_notifications",
                () => new Measurement<long>(NotificationsProcessedCount, metrics.TagList),
                description: "Number of successfully processed subscription notifications " +
                "received from OPC client.");
            _meter.CreateObservableCounter("iiot_edge_publisher_dropped_notifications",
                () => new Measurement<long>(NotificationsDroppedCount, metrics.TagList),
                description: "Number of incoming subscription notifications that are too " +
                "big to be processed based " +
                "on the message size limits or other issues with the notification.");
            _meter.CreateObservableCounter("iiot_edge_publisher_processed_messages",
                () => new Measurement<long>(MessagesProcessedCount, metrics.TagList),
                description: "Number of successfully generated messages that are to be " +
                "sent using the message sender");
            _meter.CreateObservableGauge("iiot_edge_publisher_notifications_per_message_average",
                () => new Measurement<double>(AvgNotificationsPerMessage, metrics.TagList),
                description: "Average subscription notifications packed into a message");
            _meter.CreateObservableGauge("iiot_edge_publisher_encoded_message_size_average",
                () => new Measurement<double>(AvgMessageSize, metrics.TagList),
                description: "Average size of a message through the lifetime of the encoder.");
            _meter.CreateObservableGauge("iiot_edge_publisher_chunk_size_average",
                () => new Measurement<double>(AvgMessageSize / (4 * 1024), metrics.TagList),
                description: "IoT Hub chunk size average");
            _meter.CreateObservableGauge("iiot_edge_publisher_message_split_ratio_max",
                () => new Measurement<double>(MaxMessageSplitRatio, metrics.TagList),
                description: "The message split ration specifies into how many messages " +
                "a subscription notification had to be split. Less is better for " +
                "performance. If the number is large user should attempt to limit " +
                "the number of notifications in a message using configuration.");
        }

        private uint _sequenceNumber; // TODO: Use writer group context
        private readonly IOptions<PublisherOptions> _options;
        private readonly ILogger _logger;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly bool _logNotifications;
    }
}
