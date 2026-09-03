// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub/Edge transport backed by IoTHubby.
    /// </summary>
    public sealed class IoTEdgeTransport : IEventClient, IEventSubscriber,
        IRpcServer, IRpcClient, IProcessIdentity, IEventClientCapabilities,
        IAsyncDisposable
    {
        /// <inheritdoc/>
        public string Name => "IoTHub";

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes { get; } = (256 * 1024) - 4 * 1024;

        /// <inheritdoc/>
        public int MaxMethodPayloadSizeInBytes { get; } = 120 * 1024;

        /// <inheritdoc/>
        public EventClientCapabilities Capabilities =>
            EventClientCapabilities.Payload
            | EventClientCapabilities.Topic
            | EventClientCapabilities.ContentType
            | EventClientCapabilities.ContentEncoding
            | EventClientCapabilities.CustomProperties
            | EventClientCapabilities.CloudEvents
            | EventClientCapabilities.TransportSecurity
            | EventClientCapabilities.Authentication;

        /// <inheritdoc/>
        public string Identity => _client.Identity.ModuleId == null ?
            _client.Identity.DeviceId :
            $"{_client.Identity.DeviceId}/{_client.Identity.ModuleId}";

        /// <inheritdoc/>
        string IProcessIdentity.Identity =>
            _client.Identity.Gateway ?? _client.Identity.DeviceId;

        /// <inheritdoc/>
        public IEnumerable<IRpcHandler> Connected
        {
            get
            {
                lock (_handlers)
                {
                    return [.. _handlers];
                }
            }
        }

        /// <summary>
        /// Create transport.
        /// </summary>
        public IoTEdgeTransport(IoTEdgeModuleClient client,
            ILogger<IoTEdgeTransport> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _receiverCts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return new IoTEdgeEvent(this);
        }

        /// <inheritdoc/>
        public async ValueTask<IAsyncDisposable> SubscribeAsync(string topic,
            IEventConsumer consumer, CancellationToken ct = default)
        {
            if (!TopicFilter.IsValid(topic))
            {
                throw new ArgumentException("Invalid topic filter", nameof(topic));
            }
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(consumer);

            await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
            var subscription = new EventSubscription(topic, consumer, this);
            lock (_subscriptions)
            {
                _subscriptions.Add(subscription);
                _receiver ??= Task.Factory.StartNew(
                    () => ReceiveInputsAsync(_receiverCts.Token),
                    _receiverCts.Token, TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();
            }
            return subscription;
        }

        /// <inheritdoc/>
        public async ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler server,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentNullException.ThrowIfNull(server);

            await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
            lock (_handlers)
            {
                _handlers.Add(server);
            }
            await _client.SetMethodHandlerAsync(InvokeMethodAsync, ct)
                .ConfigureAwait(false);
            return new RpcSubscription(server, this);
        }

        /// <inheritdoc/>
        public void Start()
        {
            // Nothing to do.
        }

        /// <inheritdoc/>
        public ValueTask<ReadOnlySequence<byte>> CallAsync(string target, string method,
            ReadOnlySequence<byte> payload, string contentType, TimeSpan? timeout = null,
            CancellationToken ct = default)
        {
            _ = target;
            _ = method;
            _ = payload;
            _ = contentType;
            _ = timeout;
            _ = ct;
            throw new NotSupportedException(
                "IoTHubby 0.9.0 exposes device/module direct-method handling, " +
                "but not service-side direct-method invocation.");
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            try
            {
                await _receiverCts.CancelAsync().ConfigureAwait(false);
                if (_receiver != null)
                {
                    try { await _receiver.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                }
                await _client.SetMethodHandlerAsync(null).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.DisposeFailed(ex);
            }
            finally
            {
                _receiverCts.Dispose();
            }
        }

        private async ValueTask SendAsync(IoTEdgeEvent message, CancellationToken ct)
        {
            await _client.EnsureConnectedAsync(ct).ConfigureAwait(false);
            var telemetry = new TelemetryMessage(message.Payload)
            {
                ContentType = message.ContentType,
                ContentEncoding = message.ContentEncoding,
                CreationTimeUtc = message.Timestamp,
                QoS = message.QoS == QoS.AtMostOnce ?
                    IoTHubQoS.AtMostOnce : IoTHubQoS.AtLeastOnce
            };
            foreach (var property in message.Properties)
            {
                if (property.Value != null)
                {
                    telemetry.Properties[property.Key] = property.Value;
                }
            }
            if (string.IsNullOrEmpty(message.Topic))
            {
                await _client.SendTelemetryAsync(telemetry, ct).ConfigureAwait(false);
            }
            else
            {
                await _client.SendToOutputAsync(message.Topic, telemetry, ct)
                    .ConfigureAwait(false);
            }
        }

        private async Task ReceiveInputsAsync(CancellationToken ct)
        {
            try
            {
                await foreach (var message in _client
                    .ReceiveInputMessagesAsync(string.Empty, ct).ConfigureAwait(false))
                {
                    var topic = message.InputName ?? string.Empty;
                    EventSubscription[] subscriptions;
                    lock (_subscriptions)
                    {
                        subscriptions = [.. _subscriptions];
                    }
                    await Task.WhenAll(subscriptions
                        .Where(s => TopicFilter.Matches(topic, s.Topic))
                        .Select(s => s.Consumer.HandleAsync(topic, message.Payload,
                            message.ContentType ?? ContentMimeType.Binary,
                            MergeProperties(message), this, ct))).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.ReceiveFailed(ex);
            }
        }

        private async ValueTask<DirectMethodResponse> InvokeMethodAsync(
            DirectMethodRequest request, CancellationToken ct)
        {
            IRpcHandler[] handlers;
            lock (_handlers)
            {
                handlers = [.. _handlers];
            }
            foreach (var handler in handlers)
            {
                try
                {
                    var response = await handler.InvokeAsync(request.Name,
                        request.Payload, ContentMimeType.Json, ct).ConfigureAwait(false);
                    return DirectMethodResponse.FromSequence((int)HttpStatusCode.OK,
                        response);
                }
                catch (NotSupportedException)
                {
                }
                catch (MethodCallStatusException ex)
                {
                    return DirectMethodResponse.FromBytes(ex.Status, ex.Serialize());
                }
                catch (OperationCanceledException)
                {
                    return DirectMethodResponse.FromStatus((int)HttpStatusCode.RequestTimeout);
                }
                catch (Exception ex)
                {
                    var error = new MethodCallStatusException(
                        (int)HttpStatusCode.InternalServerError, ex, ex.Message);
                    return DirectMethodResponse.FromBytes(error.Status, error.Serialize());
                }
            }
            return DirectMethodResponse.FromStatus((int)HttpStatusCode.NotFound);
        }

        private async ValueTask RemoveAsync(EventSubscription subscription)
        {
            lock (_subscriptions)
            {
                _subscriptions.Remove(subscription);
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async ValueTask RemoveAsync(IRpcHandler handler)
        {
            lock (_handlers)
            {
                _handlers.Remove(handler);
            }
            if (_handlers.Count == 0)
            {
                await _client.SetMethodHandlerAsync(null).ConfigureAwait(false);
            }
        }

        private static IReadOnlyDictionary<string, string> MergeProperties(
            CloudToDeviceMessage message)
        {
            var properties = new Dictionary<string, string>(message.Properties,
                StringComparer.Ordinal);
            foreach (var property in message.SystemProperties)
            {
                properties[property.Key] = property.Value;
            }
            if (message.ContentEncoding != null)
            {
                properties["ContentEncoding"] = message.ContentEncoding;
            }
            return properties;
        }

        private sealed class IoTEdgeEvent : IEvent
        {
            public string? Topic { get; private set; }
            public DateTimeOffset? Timestamp { get; private set; }
            public string? ContentType { get; private set; }
            public string? ContentEncoding { get; private set; }
            public QoS QoS { get; private set; } = QoS.AtLeastOnce;
            public ReadOnlySequence<byte> Payload { get; private set; }
            public Dictionary<string, string?> Properties { get; } =
                new(StringComparer.Ordinal);

            public IoTEdgeEvent(IoTEdgeTransport outer)
            {
                _outer = outer;
            }

            public IEvent SetTopic(string? value)
            {
                Topic = value;
                return this;
            }

            public IEvent SetTimestamp(DateTimeOffset value)
            {
                Timestamp = value;
                return this;
            }

            public IEvent SetContentType(string? value)
            {
                ContentType = value;
                return this;
            }

            public IEvent SetContentEncoding(string? value)
            {
                ContentEncoding = value;
                return this;
            }

            public IEvent AsCloudEvent(CloudEventHeader header)
            {
                Properties["specversion"] = "1.0";
                Properties["id"] = header.Id;
                Properties["source"] = header.Source.ToString();
                Properties["type"] = header.Type;
                if (header.Time != null)
                {
                    Properties["time"] = header.Time.ToString();
                }
                if (header.DataContentType != null)
                {
                    Properties["datacontenttype"] = header.DataContentType;
                }
                if (header.Subject != null)
                {
                    Properties["subject"] = header.Subject;
                }
                return this;
            }

            public IEvent SetSchema(IEventSchema schema)
            {
                if (schema.Id != null)
                {
                    Properties["dataschema"] = schema.Id;
                }
                return this;
            }

            public IEvent AddProperty(string name, string? value)
            {
                Properties[name] = value;
                return this;
            }

            public IEvent SetRetain(bool value)
            {
                _ = value;
                return this;
            }

            public IEvent SetQoS(QoS value)
            {
                QoS = value;
                return this;
            }

            public IEvent SetTtl(TimeSpan value)
            {
                _ = value;
                return this;
            }

            public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
            {
                var buffers = value?.ToList() ?? [];
                if (buffers.Count == 1)
                {
                    Payload = buffers[0];
                    return this;
                }
                var length = checked((int)buffers.Sum(b => b.Length));
                var payload = new byte[length];
                var offset = 0;
                foreach (var buffer in buffers)
                {
                    foreach (var segment in buffer)
                    {
                        segment.Span.CopyTo(payload.AsSpan(offset));
                        offset += segment.Length;
                    }
                }
                Payload = new ReadOnlySequence<byte>(payload);
                return this;
            }

            public ValueTask SendAsync(CancellationToken ct = default)
            {
                return _outer.SendAsync(this, ct);
            }

            public void Dispose()
            {
            }

            private readonly IoTEdgeTransport _outer;
        }

        private sealed class EventSubscription : IAsyncDisposable
        {
            public string Topic { get; }
            public IEventConsumer Consumer { get; }

            public EventSubscription(string topic, IEventConsumer consumer,
                IoTEdgeTransport outer)
            {
                Topic = topic;
                Consumer = consumer;
                _outer = outer;
            }

            public ValueTask DisposeAsync()
            {
                return _outer.RemoveAsync(this);
            }

            private readonly IoTEdgeTransport _outer;
        }

        private sealed class RpcSubscription : IAsyncDisposable
        {
            public RpcSubscription(IRpcHandler handler, IoTEdgeTransport outer)
            {
                _handler = handler;
                _outer = outer;
            }

            public ValueTask DisposeAsync()
            {
                return _outer.RemoveAsync(_handler);
            }

            private readonly IRpcHandler _handler;
            private readonly IoTEdgeTransport _outer;
        }

        private readonly IoTEdgeModuleClient _client;
        private readonly ILogger<IoTEdgeTransport> _logger;
        private readonly List<EventSubscription> _subscriptions = [];
        private readonly List<IRpcHandler> _handlers = [];
        private readonly CancellationTokenSource _receiverCts;
        private Task? _receiver;
        private bool _disposed;
    }

    /// <summary>
    /// Source-generated logging for IoTEdgeTransport.
    /// </summary>
    internal static partial class IoTEdgeTransportLogging
    {
        private const int EventClass = 930;

        [LoggerMessage(EventId = EventClass + 0, Level = LogLevel.Error,
            Message = "IoT Edge input receiver failed.")]
        public static partial void ReceiveFailed(this ILogger logger, Exception ex);

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Debug,
            Message = "IoT Edge transport dispose failed.")]
        public static partial void DisposeFailed(this ILogger logger, Exception ex);
    }
}
