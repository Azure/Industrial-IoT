// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Selects and configures the event client a writer group publishes
    /// through, and resolves the limits that bound how much it publishes at a
    /// time.
    /// </summary>
    /// <remarks>
    /// This is how a writer group's <c>Transport</c> and
    /// <c>TransportConfiguration</c> become an actual client, including the
    /// factory-created client a group-specific configuration asks for and the
    /// scope that owns it. It was a nested type of the custom encoder's sink;
    /// the native PubSub egress resolves its client the same way, so it
    /// outlived the sink and now stands on its own.
    /// </remarks>
    internal record class WriterGroupTransportOptions : IDisposable
    {
        /// <summary>
        /// Event client selected
        /// </summary>
        public IEventClient EventClient { get; }

        /// <summary>
        /// Notifications per message
        /// </summary>
        public int MaxNotificationsPerMessage { get; }

        /// <summary>
        /// Max network messages
        /// </summary>
        public int MaxNetworkMessageSize { get; }

        /// <summary>
        /// Max publish queue size
        /// </summary>
        public int MaxPublishQueueSize { get; }

        /// <summary>
        /// Max batch trigger interval
        /// </summary>
        public TimeSpan BatchTriggerInterval { get; }

        /// <summary>
        /// Iot edge configured
        /// </summary>
        public bool IsIoTEdge
            => EventClient.Name.Equals(nameof(WriterGroupTransport.IoTHub),
                    StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Max publish queue partitions
        /// </summary>
        public int MaxPublishQueuePartitions { get; }

        /// <summary>
        /// Create null options
        /// </summary>
        public WriterGroupTransportOptions()
        {
            EventClient = new NullEventClient();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scope?.Dispose();
        }

        /// <summary>
        /// Create options
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="eventClients"></param>
        /// <param name="factories"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public WriterGroupTransportOptions(WriterGroupModel writerGroup,
            List<IEventClient> eventClients,
            Dictionary<string, IEventClientFactory> factories,
            IOptions<PublisherOptions> options, ILogger logger)
        {
            EventClient = eventClients
                    .Find(e => e.Name.Equals(writerGroup.Transport?.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                ?? eventClients
                    .Find(e => e.Name.Equals(options.Value.DefaultTransport?.ToString(),
                        StringComparison.OrdinalIgnoreCase))
                ?? eventClients[0];

            if (!string.IsNullOrEmpty(writerGroup.TransportConfiguration))
            {
                if (!factories.TryGetValue(EventClient.Name, out var factory))
                {
                    logger.CustomWriterGroupConfigurationCouldNotBeApplied(
                        EventClient.Name);
                }
                else
                {
                    // Create event client with configuration from factory.
                    try
                    {
                        _scope = factory.CreateEventClient(
                            writerGroup.TransportConfiguration, out var client);

                        EventClient = client;
                        logger.UsingTransportWithCustomWriterGroupConfiguration(
                            EventClient.Name);
                    }
                    catch (Exception e)
                    {
                        logger.CustomWriterGroupConfigurationCouldNotBeAppliedWithError(
                            EventClient.Name, e.Message);
                    }
                }
            }

            MaxNotificationsPerMessage = (int?)writerGroup.NotificationPublishThreshold
                ?? options.Value.BatchSize ?? 0;
            MaxNetworkMessageSize = (int?)writerGroup.MaxNetworkMessageSize
                ?? options.Value.MaxNetworkMessageSize ?? 0;

            if (MaxNetworkMessageSize <= 0)
            {
                MaxNetworkMessageSize = int.MaxValue;
            }
            if (MaxNetworkMessageSize > EventClient.MaxEventPayloadSizeInBytes)
            {
                MaxNetworkMessageSize = EventClient.MaxEventPayloadSizeInBytes;
            }

            BatchTriggerInterval = writerGroup.PublishingInterval
                ?? options.Value.BatchTriggerInterval ?? TimeSpan.Zero;
            //
            // If the max notification per message is 1 then there is no need to
            // have an interval publishing as the messages are emitted as soon
            // as they arrive anyway
            //
            if (MaxNotificationsPerMessage == 1)
            {
                BatchTriggerInterval = TimeSpan.Zero;
            }
            MaxPublishQueueSize = (int?)writerGroup.PublishQueueSize
                ?? options.Value.MaxNetworkMessageSendQueueSize ?? kMaxQueueSize;

            //
            // If undefined, set notification buffer to 1 if no publishing interval
            // otherwise queue as much as reasonable
            //
            if (MaxNotificationsPerMessage <= 0)
            {
                MaxNotificationsPerMessage = BatchTriggerInterval == TimeSpan.Zero ?
                    1 : MaxPublishQueueSize;
            }

            MaxPublishQueuePartitions = writerGroup.PublishQueuePartitions ??
                options.Value.DefaultWriterGroupPartitions ?? 0;
        }

        /// <summary>
        /// Log the transportation options
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="logger"></param>
        public void Log(WriterGroupModel writerGroup, ILogger logger)
        {
            var interval = BatchTriggerInterval == TimeSpan.Zero ?
                "as soon as they arrive" : $"every {BatchTriggerInterval} (hh:mm:ss)";
            var batching = MaxNotificationsPerMessage == 1 ?
                "and individually" :
                $"or when a batch of {MaxNotificationsPerMessage} notifications is ready";
            var maxSize = MaxNetworkMessageSize == int.MaxValue ?
                "unlimited size" : $"at most {MaxNetworkMessageSize / 1024} kb";

            logger.WriterGroupSetup(
                writerGroup.Name ?? Constants.DefaultWriterGroupName,
                interval,
                batching,
                maxSize,
                EventClient.Name,
                writerGroup.HeaderLayoutUri ?? "unknown",
                writerGroup.MessageType ?? MessageEncoding.Json,
                MaxPublishQueueSize);
        }

        /// <summary>
        /// With 256k limit this is 1 GB.
        /// TODO: Must be related to the actual limit size
        /// </summary>
        private const int kMaxQueueSize = 4096;
        private readonly IDisposable? _scope;
    }

    /// <summary>
    /// Source-generated logging definitions for the writer group transport
    /// </summary>
    internal static partial class WriterGroupTransportLogging
    {
        private const int EventClass = 200;

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Information,
            Message = "Writer group {WriterGroup} set up to publish notifications {Interval} {Batching} with {MaxSize} to" +
            " {Transport} with {HeaderLayout} layout and {MessageType} encoding (queuing at most {MaxQueueSize} subscription" +
            " notifications)...")]
        public static partial void WriterGroupSetup(this ILogger logger, string writerGroup, string interval,
            string batching, string maxSize, string transport, string headerLayout,
            MessageEncoding messageType, int maxQueueSize);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Information,
            Message = "Using transport {Transport} with custom writer group configuration.")]
        public static partial void UsingTransportWithCustomWriterGroupConfiguration(this ILogger logger,
            string transport);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Warning,
            Message = "Custom writer group configuration could not be applied to transport {Transport} " +
            "- using default.")]
        public static partial void CustomWriterGroupConfigurationCouldNotBeApplied(this ILogger logger,
            string transport);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Error,
            Message = "Custom writer group configuration could not be applied to transport {Transport} " +
            "due to bad configuration (Error: {Error}) - using default.")]
        public static partial void CustomWriterGroupConfigurationCouldNotBeAppliedWithError(this ILogger logger,
            string transport, string error);
    }
}
