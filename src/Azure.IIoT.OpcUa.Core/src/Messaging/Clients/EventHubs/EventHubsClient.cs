// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.EventHubs
{
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event Hubs event client.
    /// </summary>
    public sealed class EventHubsClient : IEventClient, IDisposable, IAsyncDisposable
    {
        /// <inheritdoc/>
        public string Name => "EventHub";

        /// <inheritdoc/>
        public string Identity { get; }

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes
            => _options.Value.MaxEventPayloadSizeInBytes ?? 1024 * 1024;

        /// <summary>
        /// Create client.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="credential"></param>
        /// <param name="logger"></param>
        /// <param name="registry"></param>
        public EventHubsClient(IOptions<EventHubsClientOptions> options,
            ICredentialProvider credential, ILogger<EventHubsClient> logger,
            ISchemaRegistry? registry = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _schemaRegistry = registry;

            if (string.IsNullOrEmpty(_options.Value.ConnectionString) ||
                !ConnectionString.TryParse(_options.Value.ConnectionString, out var cs) ||
                string.IsNullOrEmpty(cs.Endpoint))
            {
                throw new InvalidConfigurationException(
                    "EventHub Connection string not configured.");
            }

            Identity = cs.Endpoint;
            _client = new EventHubProducerClient(_options.Value.ConnectionString);

            if (_schemaRegistry == null && options.Value.SchemaRegistry != null)
            {
                options.Value.SchemaRegistry.FullyQualifiedNamespace =
                    cs.Endpoint.Replace("sb://", string.Empty, StringComparison.Ordinal);

                _schemaRegistry = new SchemaGroup(options.Value.SchemaRegistry,
                    credential, _logger);
            }
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return new EventHubsEvent(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync().ConfigureAwait(false);
        }

        private sealed class EventHubsEvent : IEvent
        {
            /// <summary>
            /// Create event.
            /// </summary>
            /// <param name="outer"></param>
            public EventHubsEvent(EventHubsClient outer)
            {
                _outer = outer;
            }

            /// <inheritdoc/>
            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                _properties["specversion"] = "1.0";
                _properties["id"] = header.Id;
                _properties["source"] = header.Source.ToString();
                _properties["type"] = header.Type;
                if (header.Time != null)
                {
                    _properties["time"] = header.Time.ToString();
                }
                if (header.DataContentType != null)
                {
                    _properties["datacontenttype"] = header.DataContentType;
                }
                if (header.Subject != null)
                {
                    _properties["subject"] = header.Subject;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetQoS(QoS value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentType(string? value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _contentEncoding = value;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetSchema(IEventSchema schema)
            {
                if (schema.Type == ContentMimeType.AvroSchema)
                {
                    _schema = schema;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddProperty(string name, string? value)
            {
                _properties[name] = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                _buffers.AddRange(value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTopic(string? value)
            {
                _properties["deviceId"] = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTimestamp(DateTimeOffset value)
            {
                return this;
            }

            /// <inheritdoc/>
            public async ValueTask SendAsync(CancellationToken ct = default)
            {
                if (_buffers.Count == 0)
                {
                    return;
                }
                try
                {
                    if (_outer._schemaRegistry != null && _schema != null)
                    {
                        var retrievedSchemaId = await _outer._schemaRegistry.RegisterAsync(
                            _schema, ct).ConfigureAwait(false);

                        if (retrievedSchemaId != null)
                        {
                            _contentEncoding = $"{_contentEncoding}+{retrievedSchemaId}";
                        }
                    }

                    using var eventBatch = await Client.CreateBatchAsync(ct).ConfigureAwait(false);
                    foreach (var msg in _buffers)
                    {
                        eventBatch.TryAdd(CreateMessage(msg));
                    }
                    await Client.SendAsync(eventBatch, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.SendingMessageFailed(ex);
                    throw;
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _buffers.Clear();
            }

            private EventData CreateMessage(ReadOnlySequence<byte> buffer)
            {
                var message = !buffer.IsSingleSegment ?
                    new EventData(buffer.ToArray()) :
                    new EventData(buffer.First);
                message.ContentType = _contentEncoding;
                foreach (var item in _properties)
                {
                    if (item.Value == null)
                    {
                        message.Properties.Remove(item.Key);
                    }
                    else
                    {
                        message.Properties[item.Key] = item.Value;
                    }
                }
                return message;
            }

            private ILogger Logger => _outer._logger;
            private EventHubProducerClient Client => _outer._client;

            private readonly EventHubsClient _outer;
            private readonly Dictionary<string, string?> _properties = [];
            private readonly List<ReadOnlySequence<byte>> _buffers = [];
            private IEventSchema? _schema;
            private string? _contentEncoding;
        }

        private readonly EventHubProducerClient _client;
        private readonly IOptions<EventHubsClientOptions> _options;
        private readonly ISchemaRegistry? _schemaRegistry;
        private readonly ILogger _logger;
    }

    /// <summary>
    /// Source-generated logging for <see cref="EventHubsClient"/>.
    /// </summary>
    internal static partial class EventHubsClientLogging
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Trace,
            Message = "Sending message to to EventHub failed.")]
        public static partial void SendingMessageFailed(this ILogger logger, Exception ex);
    }
}

