// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Discovery
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Messaging;
    using Furly.Extensions.Serializers;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery progress message sender
    /// </summary>
    public sealed class ProgressPublisher : ProgressLogger, IDisposable
    {
        /// <summary>
        /// Create listener
        /// </summary>
        /// <param name="events"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="timeProvider"></param>
        public ProgressPublisher(IEnumerable<IEventClient> events, IJsonSerializer serializer,
            IOptions<PublisherOptions> options, ILogger<ProgressPublisher> logger,
            TimeProvider? timeProvider = null)
            : base(logger, timeProvider)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options;

            if (_options.Value.AllowedEventAndDiagnosticsTransports.Count > 0)
            {
                var allowed = _options.Value.AllowedEventAndDiagnosticsTransports
                    .Select(t => t.ToString())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                events = events
                    .Where(e => allowed.Contains(e.Name))
                    .ToList();
            }

            _events = events.Reverse().ToList();
            _channel = Channel.CreateUnbounded<DiscoveryProgressModel>(
                new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false
                });
            _sender = Task.Factory.StartNew(SendProgressAsync,
                default, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        protected override void Send(DiscoveryProgressModel progress)
        {
            base.Send(progress);
            if (!_channel.Writer.TryWrite(progress))
            {
                _logger.CannotSendIfProgressPublisherDisposed();
                ObjectDisposedException.ThrowIf(true, this);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_sender.IsCompleted)
            {
                _channel.Writer.Complete();
                _sender.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Send progress
        /// </summary>
        /// <returns></returns>
        private async Task SendProgressAsync()
        {
            await foreach (var progress in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                var eventsTopic = _topicCache.GetOrAdd(
                    (progress.EventType.ToString(), progress.RequestId),
                id => new TopicBuilder(_options.Value, variables: new Dictionary<string, string>
                {
                    [PublisherConfig.EventNameVariableName] =
                        id.Item1 ?? Constants.DefaultEventName,
                    [PublisherConfig.EventContextVariableName] =
                        id.Item2 ?? Constants.DefaultContextName,
                    [PublisherConfig.EventSourceVariableName] =
                        "discovery",
                    [PublisherConfig.EncodingVariableName] =
                        MessageEncoding.Json.ToString()
                    // ...
                }).EventsTopic);

                await Task.WhenAll(_events.Select(SendOneEventAsync)).ConfigureAwait(false);

                async Task SendOneEventAsync(IEventClient events)
                {
                    try
                    {
                        await events.SendEventAsync(eventsTopic,
                            _serializer.SerializeToMemory(progress with { DiscovererId = events.Identity }),
                            _serializer.MimeType, Encoding.UTF8.WebName,
                            eventMessage =>
                            {
                                if (_options.Value.EnableCloudEvents == true)
                                {
                                    eventMessage = eventMessage.AsCloudEvent(new CloudEventHeader
                                    {
                                        Id = Guid.NewGuid().ToString("N"),
                                        Source = new Uri("urn:" + _options.Value.PublisherId),
                                        Subject = progress.EventType.ToString(),
                                        Type = MessageSchemaTypes.DiscoveryMessage,
                                    });
                                }
                                else
                                {
                                    eventMessage.AddProperty(OpcUa.Constants.MessagePropertySchemaKey,
                                       MessageSchemaTypes.DiscoveryMessage);
                                }
                            }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.FailedToSendDiscoveryProgress(ex);
                    }
                }
            }
        }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _serializer;
        private readonly Task _sender;
        private readonly Channel<DiscoveryProgressModel> _channel;
        private readonly IOptions<PublisherOptions> _options;
        private readonly ConcurrentDictionary<(string, string?), string> _topicCache = new();
        private readonly List<IEventClient> _events;
    }

    internal static partial class ProgressPublisherLogging
    {
        private const int EventClass = 60;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Cannot send if progress publisher is already disposed.")]
        public static partial void CannotSendIfProgressPublisherDisposed(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Error,
            Message = "Failed to send discovery progress.")]
        public static partial void FailedToSendDiscoveryProgress(this ILogger logger, Exception ex);
    }
}
