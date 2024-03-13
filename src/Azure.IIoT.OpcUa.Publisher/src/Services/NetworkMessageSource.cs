// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Services
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Encoders.PubSub;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Network message sink connected to the source. The sink consists of
    /// publish queue which is a dataflow engine to handle batching and
    /// encoding and other egress concerns.  The queues can be partitioned
    /// to handle multiple topics.
    /// </summary>
    public sealed class NetworkMessageSource : IEventConsumer, IDisposable,
        IAsyncDisposable
    {
        /// <summary>
        /// Create engine
        /// </summary>
        /// <param name="writerGroup"></param>
        /// <param name="eventSubscribers"></param>
        /// <param name="encoder"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        public NetworkMessageSource(WriterGroupModel writerGroup,
            IEnumerable<IEventSubscriber> eventSubscribers,
            IMessageEncoder encoder, IOptions<PublisherOptions> options,
            ILogger<NetworkMessageSource> logger, IMetricsContext metrics)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _options = options;

            _messageEncoder = encoder;
            _logger = logger;
            _cts = new CancellationTokenSource();

            // Reverse the registration to have highest prio first.
            var registered = eventSubscribers?.OfType<IEventClient>().Reverse().ToList()
                ?? throw new ArgumentNullException(nameof(eventSubscribers));
            if (registered.Count != 0)
            {
                _eventSubscriber = ((
                       registered.Find(e => e.Name.Equals(
                        writerGroup.Transport?.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    ?? registered.Find(e => e.Name.Equals(
                        options.Value.DefaultTransport?.ToString(),
                            StringComparison.OrdinalIgnoreCase))
                    ?? registered[0]) // TODO Add name to subscriber interface
                    as IEventSubscriber) ?? new NullSubscriber();
            }
            else
            {
                _eventSubscriber = new NullSubscriber();
            }

            InitializeMetrics();

            if (_eventSubscriber is NullSubscriber)
            {
                return;
            }

            _logger.LogInformation("Reader group {ReaderGroup} set up to receive notifications " +
                "from {Transport} with {HeaderLayout} layout and {MessageType} encoding...",
                writerGroup.Name ?? Constants.DefaultWriterGroupName,
                (_eventSubscriber as IEventClient)?.Name, writerGroup.HeaderLayoutUri ?? "unknown",
                writerGroup.MessageType ?? MessageEncoding.Json);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync().ConfigureAwait(false);
            try
            {
            }
            finally
            {
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                _meter.Dispose();
            }
            finally
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
                _cts.Dispose();
            }
        }

        /// <inheritdoc/>
        public async Task HandleAsync(string topic, ReadOnlySequence<byte> data, string contentType,
            IReadOnlyDictionary<string, string?> properties, IEventClient? responder,
            CancellationToken ct)
        {
            try
            {
                var context = new ServiceMessageContext();
                var pubSubMessage = PubSubMessage.Decode(data, contentType, context);
                if (pubSubMessage is not BaseNetworkMessage networkMessage)
                {
                    _logger.LogInformation("Received non network message.");
                    _errorCount++;
                    return;
                }
                foreach (var dataSetMessage in networkMessage.Messages)
                {
                    foreach (var datapoint in dataSetMessage.Payload)
                    {
                        var dataValue = datapoint.Value;

                        // Perform write
                        await Task.Delay(1, ct).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscriber network message handling failed - skip");
                _errorCount++;
            }
        }

        /// <summary>
        /// Dummy subscriber
        /// </summary>
        private class NullSubscriber : IEventSubscriber, IAsyncDisposable
        {
            /// <inheritdoc/>
            public string Name => "NULL";

            /// <inheritdoc/>
            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            /// <inheritdoc/>
            public ValueTask<IAsyncDisposable> SubscribeAsync(string topic,
                IEventConsumer consumer, CancellationToken ct)
            {
                return ValueTask.FromResult<IAsyncDisposable>(this);
            }
        }

        /// <summary>
        /// Create observable metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableCounter("iiot_edge_publisher_message_receive_failures",
                () => new Measurement<long>(_errorCount, _metrics.TagList),
                description: "Number of failures receiving a network message.");
        }

        private long _errorCount;
        private readonly IOptions<PublisherOptions> _options;
        private readonly IMessageEncoder _messageEncoder;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cts;
        private readonly IMetricsContext _metrics;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }
}
