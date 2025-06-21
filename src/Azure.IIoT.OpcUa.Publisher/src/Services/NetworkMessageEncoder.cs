// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Encoders.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
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
        /// <param name="timeProvider">time to use for timestamps</param>
        public NetworkMessageEncoder(IOptions<PublisherOptions> options,
            IMetricsContext metrics, ILogger<NetworkMessageEncoder> logger,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            _logger = logger;
            _timeProvider = timeProvider ?? TimeProvider.System;
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
            IEnumerable<OpcUaSubscriptionNotification> notifications, int maxMessageSize, bool asBatch)
        {
            try
            {
                var chunkedMessages = new List<(IEvent, Action)>();
                foreach (var m in GetNetworkMessages(notifications, asBatch))
                {
                    if (m.EncodingContext == null)
                    {
                        _logger.MissingServiceMessageContext();
                        NotificationsDroppedCount++;
                        m.OnSentCallback();
                        continue;
                    }

                    var chunks = m.NetworkMessage.Encode(m.EncodingContext, maxMessageSize);
                    var notificationsPerChunk = m.NotificationsPerMessage / (double)chunks.Count;
                    var validChunks = 0;
                    foreach (var body in chunks)
                    {
                        if (body.Length == 0)
                        {
                            _logger.ChunkTooLarge();
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
                            .SetTimestamp(_timeProvider.GetUtcNow())
                            .SetContentEncoding(m.NetworkMessage.ContentEncoding)
                            .SetContentType(m.NetworkMessage.ContentType)
                            .SetTopic(m.Queue.QueueName)
                            .SetRetain(m.Queue.Retain ?? false)
                            .SetQoS(m.Queue.RequestedDeliveryGuarantee ?? QoS.AtLeastOnce)
                            .AddBuffers(chunks)
                            ;

                        if (m.Queue.Ttl.HasValue)
                        {
                            chunkedMessage = chunkedMessage
                                .SetTtl(m.Queue.Ttl.Value);
                        }
                        if (m.CloudEvent != null)
                        {
                            // Send as cloud event
                            chunkedMessage = chunkedMessage.AsCloudEvent(m.CloudEvent);
                        }
                        else
                        {
                            chunkedMessage = chunkedMessage
                                .AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                    m.NetworkMessage.MessageSchema);
                            if (_options.Value.EnableDataSetRoutingInfo ?? false)
                            {
                                chunkedMessage.AddProperty(OpcUa.Constants.MessagePropertyRoutingKey,
                                    m.NetworkMessage.DataSetWriterGroup);
                            }

                            if (_options.Value.UseStandardsCompliantEncoding != true)
                            {
                                chunkedMessage = chunkedMessage
                                    .AddProperty("$$ContentType", m.NetworkMessage.ContentType)
                                    .AddProperty("$$ContentEncoding", m.NetworkMessage.ContentEncoding);
                            }
                        }
                        if (m.Schema != null)
                        {
                            chunkedMessage = chunkedMessage.SetSchema(m.Schema);
                        }

                        _logger.NotificationsEncoded(m.NotificationsPerMessage, validChunks);

                        chunkedMessages.Add((chunkedMessage, m.OnSentCallback));
                    }
                    else
                    {
                        // Nothing to send, complete here
                        m.OnSentCallback();
                    }

                    // We dropped a number of notifications but processed the remainder successfully
                    var tooBig = chunks.Count - validChunks;
                    NotificationsDroppedCount += tooBig;
                    if (m.NotificationsPerMessage > tooBig)
                    {
                        NotificationsProcessedCount += m.NotificationsPerMessage - tooBig;
                    }

                    //
                    // how many notifications per message resulted in how many buffers. We track the
                    // split size to provide users with an indication how many times chunks had to
                    // be created so they can configure publisher to improve performance.
                    //
                    if (m.NotificationsPerMessage > 0 && m.NotificationsPerMessage < validChunks)
                    {
                        var splitSize = validChunks / m.NotificationsPerMessage;
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
        /// <param name="NotificationsPerMessage"></param>
        /// <param name="NetworkMessage"></param>
        /// <param name="Queue"></param>
        /// <param name="OnSentCallback"></param>
        /// <param name="Schema"></param>
        /// <param name="EncodingContext"></param>
        /// <param name="CloudEvent"></param>
        private record struct EncodedMessage(int NotificationsPerMessage,
            PubSubMessage NetworkMessage, PublishingQueueSettingsModel Queue,
            Action OnSentCallback, IEventSchema? Schema, CloudEventHeader? CloudEvent,
            IServiceMessageContext? EncodingContext = null);

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="isBatched"></param>
        /// <returns></returns>
        private List<EncodedMessage> GetNetworkMessages(
            IEnumerable<OpcUaSubscriptionNotification> messages, bool isBatched)
        {
            var standardsCompliant = _options.Value.UseStandardsCompliantEncoding ?? false;
            var result = new List<EncodedMessage>();

            static PublishingQueueSettingsModel GetQueue(DataSetWriterContext context,
                PublisherOptions options)
            {
                return new PublishingQueueSettingsModel
                {
                    RequestedDeliveryGuarantee = context.Qos,
                    Retain = context.Retain,
                    Ttl = context.Ttl,
                    QueueName = context.Topic
                };
            }
            // Group messages by topic and qos, then writer group and then by dataset class id
            foreach (var topics in messages
                .Select(m => (Notification: m, Context: (m.Context as DataSetWriterContext)!))
                .Where(m => m.Context != null)
                .GroupBy(m => GetQueue(m.Context, _options.Value)))
            {
                var queue = topics.Key;
                foreach (var publishers in topics.GroupBy(m => m.Context.PublisherId))
                {
                    var publisherId = publishers.Key;
                    foreach (var groups in publishers
                        .GroupBy(m => (m.Context.WriterGroup, m.Context.Schema, m.Context.CloudEvent)))
                    {
                        var writerGroup = groups.Key.WriterGroup;
                        var schema = groups.Key.Schema;
                        var cloudEvent = groups.Key.CloudEvent;

                        if (writerGroup?.MessageSettings == null)
                        {
                            // Must have a writer group
                            Drop(groups.Select(m => m.Notification));
                            continue;
                        }
                        var encoding = writerGroup.MessageType ?? MessageEncoding.Json;
                        var networkMessageContentMask =
                            writerGroup.MessageSettings.NetworkMessageContentMask;
                        var hasSamplesPayload =
                            (networkMessageContentMask & NetworkMessageContentFlags.MonitoredItemMessage) != 0;
                        if (hasSamplesPayload && !isBatched)
                        {
                            networkMessageContentMask |= NetworkMessageContentFlags.SingleDataSetMessage;
                        }
                        var namespaceFormat =
                            writerGroup.MessageSettings?.NamespaceFormat ??
                            _options.Value.DefaultNamespaceFormat ??
                            NamespaceFormat.Uri;
                        foreach (var dataSetClass in groups
                            .GroupBy(m => m.Context.Writer?.DataSet?.DataSetMetaData?.DataSetClassId ?? Guid.Empty))
                        {
                            var dataSetClassId = dataSetClass.Key;
                            BaseNetworkMessage? currentMessage = null;
                            var currentNotifications = new List<OpcUaSubscriptionNotification>();
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
                                    Context.Writer.MessageSettings?.DataSetMessageContentMask;
                                var dataSetFieldContentMask =
                                    Context.Writer.DataSetFieldContentMask;

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
                                        if (!PubSubMessage.TryCreateDataSetMessage(encoding,
                                            GetDataSetWriterName(Notification, Context),
                                            Context.DataSetWriterId, dataSetMessageContentMask,
                                            MessageType.KeepAlive,
#if KA_WITH_EX_FIELDS
                                            new DataSet(Context.ExtensionFields, dataSetFieldContentMask),
#else
                                            new DataSet(),
#endif
                                            GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                            standardsCompliant, Notification.EndpointUrl,
                                            Notification.ApplicationUri, Context.MetaData?.MetaData,
                                            out var dataSetMessage))
                                        {
                                            Drop(Notification.YieldReturn());
                                            continue;
                                        }

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
                                            if (!PubSubMessage.TryCreateDataSetMessage(encoding,
                                                GetDataSetWriterName(Notification, Context), Context.DataSetWriterId,
                                                dataSetMessageContentMask, Notification.MessageType,
                                                new DataSet(orderedNotifications
                                                    .Select(s => (s.DataSetFieldName!, s.Value))
                                                    .Concat(Context.ExtensionFields)
                                                    .ToList(), dataSetFieldContentMask),
                                                GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                                standardsCompliant, Notification.EndpointUrl, Notification.ApplicationUri,
                                                Context.MetaData?.MetaData, out var dataSetMessage))
                                            {
                                                Drop(Notification.YieldReturn());
                                                continue;
                                            }

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
                                                        var pruned = notificationsInGroup
                                                            .OrderByDescending(k => k.Value?.SourceTimestamp) // Descend from latest
                                                            .DistinctBy(k => k.DataSetFieldName) // Only leave the latest values
                                                            .ToList();
                                                        if (pruned.Count != notificationsInGroup.Count)
                                                        {
                                                            if (_logNotifications)
                                                            {
                                                                _logger.RemovedDuplicates(notificationsInGroup.Count - pruned.Count);
                                                            }
                                                            notificationsInGroup = pruned;
                                                        }
                                                    }
                                                }
                                                foreach (var notification in notificationsInGroup)
                                                {
                                                    if (notification.DataSetFieldName != null)
                                                    {
                                                        if (!PubSubMessage.TryCreateMonitoredItemMessage(encoding,
                                                            writerGroup.Name, dataSetMessageContentMask, Notification.MessageType,
                                                            GetTimestamp(Notification), Context.NextWriterSequenceNumber(),
                                                            new DataSet(notification.DataSetFieldName, notification.Value,
                                                                dataSetFieldContentMask),
                                                            notification.NodeId, Notification.EndpointUrl, Notification.ApplicationUri,
                                                            standardsCompliant, Context.Writer.DataSet?.ExtensionFields,
                                                            out var dataSetMessage))
                                                        {
                                                            LogNotification(notification, true);
                                                            continue;
                                                        }
                                                        AddMessage(dataSetMessage);
                                                        LogNotification(notification, false);
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
                                        if (currentMessage == null)
                                        {
                                            if (!PubSubMessage.TryCreateNetworkMessage(encoding, publisherId,
                                                writerGroup.Name ?? Constants.DefaultWriterGroupName, networkMessageContentMask,
                                                dataSetClassId, () => SequenceNumber.Increment16(ref _sequenceNumber),
                                                GetTimestamp(Notification) ?? _timeProvider.GetUtcNow(), namespaceFormat,
                                                standardsCompliant, isBatched, schema, out var message))
                                            {
                                                Drop(messages);
                                                return;
                                            }
                                            currentMessage = message;
                                        }
                                        currentMessage.Messages.Add(dataSetMessage);

                                        var maxMessagesToPublish = writerGroup.MessageSettings?.MaxDataSetMessagesPerPublish ??
                                            _options.Value.DefaultMaxDataSetMessagesPerPublish;
                                        if (maxMessagesToPublish != null && currentMessage.Messages.Count >= maxMessagesToPublish)
                                        {
                                            result.Add(new EncodedMessage(currentNotifications.Count, currentMessage,
                                                queue, () => currentNotifications.ForEach(n => n.Dispose()),
                                                schema, cloudEvent, Notification.ServiceMessageContext));
#if DEBUG
                                            currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                            currentMessage = null;
                                            currentNotifications = new List<OpcUaSubscriptionNotification>();
                                        }
                                    }
                                }
                                else if (Context.MetaData?.MetaData != null && !hasSamplesPayload)
                                {
                                    if (currentMessage?.Messages.Count > 0)
                                    {
                                        // Start a new message but first emit current
                                        result.Add(new EncodedMessage(currentNotifications.Count, currentMessage,
                                            queue, () => currentNotifications.ForEach(n => n.Dispose()),
                                            schema, cloudEvent, Notification.ServiceMessageContext));
#if DEBUG
                                        currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                                        currentMessage = null;
                                        currentNotifications = new List<OpcUaSubscriptionNotification>();
                                    }

                                    if (PubSubMessage.TryCreateMetaDataMessage(encoding, publisherId,
                                        writerGroup.Name ?? Constants.DefaultWriterGroupName,
                                        GetDataSetWriterName(Notification, Context), Context.DataSetWriterId,
                                        Context.MetaData.MetaData, namespaceFormat, standardsCompliant,
                                        out var metadataMessage))
                                    {
                                        result.Add(new EncodedMessage(0, metadataMessage, queue, Notification.Dispose,
                                                schema, cloudEvent, Notification.ServiceMessageContext));
                                    }
#if DEBUG
                                    Notification.MarkProcessed();
#endif
                                    LogNotification(Notification, false);
                                }
                            }
                            if (currentMessage?.Messages.Count > 0)
                            {
                                result.Add(new EncodedMessage(currentNotifications.Count, currentMessage, queue,
                                    () => currentNotifications.ForEach(n => n.Dispose()),
                                    schema, cloudEvent, currentNotifications.LastOrDefault()?.ServiceMessageContext));
#if DEBUG
                                currentNotifications.ForEach(n => n.MarkProcessed());
#endif
                            }
                            else
                            {
                                Debug.Assert(currentNotifications.Count == 0);
                            }

                            static string GetDataSetWriterName(OpcUaSubscriptionNotification Notification,
                                DataSetWriterContext Context)
                            {
                                var dataSetWriterName = Context.WriterName;
                                var eventTypeName = Notification.EventTypeName;
                                if (!string.IsNullOrWhiteSpace(eventTypeName))
                                {
                                    return dataSetWriterName + "|" + eventTypeName;
                                }
                                return dataSetWriterName;
                            }

                            DateTimeOffset? GetTimestamp(OpcUaSubscriptionNotification Notification)
                            {
                                switch (_options.Value.MessageTimestamp)
                                {
                                    case MessageTimestamp.EncodingTimeUtc:
                                        return _timeProvider.GetUtcNow();
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
        private void Drop(IEnumerable<OpcUaSubscriptionNotification> messages)
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
                _logger.DroppedValues(totalNotifications);
                NotificationsDroppedCount += totalNotifications;
            }
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dropped"></param>
        private void LogNotification(OpcUaSubscriptionNotification args, bool dropped)
        {
            if (!_logNotifications)
            {
                return;
            }
            // Filter fields to log
            var notifications = Stringify(args.Notifications);
            if (!string.IsNullOrEmpty(notifications))
            {
                _logger.NotificationInfo(
                    dropped ? "!!!! Dropped !!!! " : "Encoded", args.PublishTimestamp, args.SequenceNumber,
                    args.PublishSequenceNumber?.ToString(CultureInfo.CurrentCulture) ?? "-", args.MessageType,
                    args.EndpointUrl, notifications);
            }
        }

        /// <summary>
        /// Log notifications for debugging
        /// </summary>
        /// <param name="args"></param>
        /// <param name="dropped"></param>
        private void LogNotification(MonitoredItemNotificationModel args, bool dropped)
        {
            if (!_logNotifications)
            {
                return;
            }
            // Filter fields to log
            var notifications = Stringify(args.YieldReturn());
            if (!string.IsNullOrEmpty(notifications))
            {
                _logger.NotificationSample(
                    dropped ? "!!!! Dropped !!!! " : "Encoded", notifications);
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
        private readonly TimeProvider _timeProvider;
        private readonly Meter _meter = Diagnostics.NewMeter();
        private readonly bool _logNotifications;
    }

    /// <summary>
    /// Source-generated logging extensions for NetworkMessageEncoder
    /// </summary>
    internal static partial class NetworkMessageEncoderLogging
    {
        private const int EventClass = 170;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Missing service message context for network message - dropping notification.")]
        public static partial void MissingServiceMessageContext(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "Resulting chunk is too large, dropped a notification.")]
        public static partial void ChunkTooLarge(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "{Count} Notifications encoded into a network message (chunks:{Chunks})...")]
        public static partial void NotificationsEncoded(this ILogger logger, int count, int chunks);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Warning,
            Message = "Dropped {TotalNotifications} values")]
        public static partial void DroppedValues(this ILogger logger, int totalNotifications);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Information,
            Message = "{Action}|{PublishTime:hh:mm:ss:ffffff}|#{Seq}:{PublishSeq}|{MessageType}|{Endpoint}|{Items}")]
        public static partial void NotificationInfo(this ILogger logger, string action, DateTimeOffset? publishTime, uint seq, string publishSeq, MessageType messageType, string? endpoint, string items);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Information,
            Message = "{Action}|Sample|{Items}")]
        public static partial void NotificationSample(this ILogger logger, string action, string items);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Information,
            Message = "Removed {Count} duplicates from batch.")]
        public static partial void RemovedDuplicates(this ILogger logger, int count);
    }
}
