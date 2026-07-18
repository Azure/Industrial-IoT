// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using global::Dapr.Client;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Event client built on Dapr pub/sub.
    /// </summary>
    public sealed class DaprPubSubClient : IEventClient, IEventClientCapabilities,
        IDisposable
    {
        /// <inheritdoc/>
        public string Name => "Dapr";

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes { get; }

        /// <inheritdoc/>
        public string Identity => Guid.NewGuid().ToString();

        /// <inheritdoc/>
        public EventClientCapabilities Capabilities =>
            EventClientCapabilities.Payload
            | EventClientCapabilities.Topic
            | EventClientCapabilities.ContentType
            | EventClientCapabilities.TransportSecurity
            | EventClientCapabilities.Authentication;

        /// <summary>
        /// Create Dapr pub/sub client.
        /// </summary>
        /// <param name="options"></param>
        public DaprPubSubClient(IOptions<DaprOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _component = options.Value.PubSubComponent;
            _client = options.Value.CreateClient();
            _checkHealth = options.Value.CheckSideCarHealthBeforeAccess;
            MaxEventPayloadSizeInBytes =
                options.Value.MessageMaxBytes ?? 512 * 1024 * 1024;
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            return new DaprPubSubEvent(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client.Dispose();
        }

        private sealed class DaprPubSubEvent : IEvent
        {
            public DaprPubSubEvent(DaprPubSubClient outer)
            {
                _outer = outer;
            }

            /// <inheritdoc/>
            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetQoS(QoS value)
            {
                AddProperty("qos", ((int)value).ToString(CultureInfo.InvariantCulture));
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTopic(string? value)
            {
                _topic = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTimestamp(DateTimeOffset value)
            {
                AddProperty("TimeStamp", value.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentType(string? value)
            {
                _contentType = value;
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetContentEncoding(string? value)
            {
                AddProperty("ContentEncoding", value);
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetSchema(IEventSchema schema)
            {
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddProperty(string name, string? value)
            {
                if (value == null)
                {
                    _metadata.Remove(name);
                }
                else
                {
                    _metadata[name] = value;
                }
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetRetain(bool value)
            {
                AddProperty("retain", value ? "true" : "false");
                return this;
            }

            /// <inheritdoc/>
            public IEvent SetTtl(TimeSpan value)
            {
                AddProperty("ttlInSeconds",
                    value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
                return this;
            }

            /// <inheritdoc/>
            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                _buffers.AddRange(value);
                return this;
            }

            /// <inheritdoc/>
            public async ValueTask SendAsync(CancellationToken ct = default)
            {
                if (_buffers.Count == 0)
                {
                    return;
                }
                var topic = _topic;
                if (topic == null)
                {
                    throw new InvalidOperationException("Need a valid topic.");
                }

                var pubSubName = _outer._component;
                if (string.IsNullOrEmpty(pubSubName))
                {
                    var split = topic.IndexOf('/', StringComparison.Ordinal);
                    if (split == -1)
                    {
                        ThrowInvalidTopic();
                    }
                    pubSubName = topic[..split];
                    if (pubSubName.Length == 0)
                    {
                        ThrowInvalidTopic();
                    }

                    topic = topic[(split + 1)..];
                }

                if (_outer._checkHealth &&
                    !await _outer._client.CheckOutboundHealthAsync(ct).ConfigureAwait(false))
                {
                    throw new ExternalDependencyException(
                        "Failed to publish message. Dapr side car is in unhealthy state.");
                }

                foreach (var buffer in _buffers)
                {
                    await _outer._client.PublishByteEventAsync(pubSubName, topic,
                        buffer.IsSingleSegment ? buffer.First : buffer.ToArray(),
                        _contentType ?? "application/json", _metadata, ct).ConfigureAwait(false);
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _buffers.Clear();
            }

            private static void ThrowInvalidTopic()
            {
                throw new InvalidOperationException("Because no pub sub component was " +
                    "defined in the configuration options, the Topic must contain component " +
                    "name as first part of the path.");
            }

            private string? _topic;
            private string? _contentType;
            private readonly Dictionary<string, string> _metadata = [];
            private readonly List<ReadOnlySequence<byte>> _buffers = [];
            private readonly DaprPubSubClient _outer;
        }

        private readonly string? _component;
        private readonly bool _checkHealth;
        private readonly DaprClient _client;
    }
}
