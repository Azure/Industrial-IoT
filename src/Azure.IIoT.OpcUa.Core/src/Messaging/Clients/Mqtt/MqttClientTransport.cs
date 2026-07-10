// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using global::Mqtt.Client;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// Mqtt transport built on the <c>Mqtt.Client</c> library implementing the
    /// core event and rpc abstractions. Replaces the former Furly.Extensions.Mqtt
    /// client. The topic strings, quality of service and request/response wire
    /// protocol are preserved (see <see cref="MqttRpcProtocol"/>).
    /// </summary>
    public sealed class MqttClientTransport : IEventClient, IEventSubscriber,
        IRpcClient, IRpcServer, IMqttPublisher, IAsyncDisposable
    {
        /// <inheritdoc/>
        public string Name => "Mqtt";

        /// <inheritdoc/>
        public int MaxEventPayloadSizeInBytes => MaxMethodPayloadSizeInBytes;

        /// <inheritdoc/>
        public int MaxMethodPayloadSizeInBytes { get; }

        /// <inheritdoc/>
        public string Identity { get; }

        /// <inheritdoc/>
        public IEnumerable<IRpcHandler> Connected => _handlers.Values.Select(v => v.Item1);

        /// <summary>
        /// Create mqtt transport
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="loggerFactory"></param>
        public MqttClientTransport(IOptions<MqttOptions> options,
            ILogger<MqttClientTransport> logger, ILoggerFactory? loggerFactory = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var o = _options.Value;
            Identity = o.ClientId ?? Guid.NewGuid().ToString();
            _version = o.Protocol;
            _defaultQoS = o.QoS ?? QoS.AtMostOnce;
            MaxMethodPayloadSizeInBytes =
                Math.Max(o.MaxPayloadSize ?? int.MaxValue, 268435455); // 256 MB

            if (o.NumberOfClientPartitions is > 1)
            {
                _logger.PartitioningNotSupported(o.NumberOfClientPartitions.Value);
            }

            _cts = new CancellationTokenSource();
            _client = BuildClient(o, loggerFactory);
            _connected = ConnectWithRetryAsync(_cts.Token);
        }

        /// <inheritdoc/>
        public IEvent CreateEvent()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return new MqttEvent(_version, _defaultQoS, this);
        }

        /// <inheritdoc/>
        async ValueTask IMqttPublisher.PublishAsync(MqttPublishMessage message,
            IEventSchema? schema, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            await EnsureConnectedAsync(ct).ConfigureAwait(false);
            var topic = message.Topic;
            if (schema != null && _options.Value.ConfigureSchemaMessage != null)
            {
                // Rewrite topic when a schema is attached and a schema hook exists
                var sm = new MqttSchemaMessage { Topic = topic };
                _options.Value.ConfigureSchemaMessage(sm);
                topic = sm.Topic;
            }
            MqttPublishProperties? props = null;
            if (_version != MqttVersion.v311)
            {
                props = new MqttPublishProperties
                {
                    ContentType = message.ContentType,
                    ResponseTopic = message.ResponseTopic,
                    CorrelationData = message.CorrelationData,
                    MessageExpiryInterval = message.MessageExpiryIntervalSeconds,
                    UserProperties = message.UserProperties?
                        .Select(p => new MqttUserProperty(p.Key, p.Value)).ToList()
                };
            }
            await _client.PublishAsync(topic, message.Payload, (MqttQoS)message.QoS,
                message.Retain, props, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<IAsyncDisposable> SubscribeAsync(string topic,
            IEventConsumer consumer, CancellationToken ct = default)
        {
            if (!TopicFilter.IsValid(topic))
            {
                throw new ArgumentException("Invalid topic filter", nameof(topic));
            }
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            await EnsureConnectedAsync(ct).ConfigureAwait(false);

            EventFilterEntry entry;
            var created = false;
            lock (_eventSubs)
            {
                if (!_eventSubs.TryGetValue(topic, out entry!))
                {
                    entry = new EventFilterEntry();
                    _eventSubs.Add(topic, entry);
                    created = true;
                }
                entry.Consumers.Add(consumer);
            }
            if (created)
            {
                try
                {
                    entry.Subscription = await SubscribeRawAsync(topic,
                        m => DispatchEventAsync(topic, m), ct).ConfigureAwait(false);
                }
                catch
                {
                    lock (_eventSubs)
                    {
                        _eventSubs.Remove(topic);
                    }
                    throw;
                }
            }
            return new Unsubscribe(() => RemoveEventConsumerAsync(topic, consumer));
        }

        /// <inheritdoc/>
        public async ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler server,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            await EnsureConnectedAsync(ct).ConfigureAwait(false);
            var id = Guid.NewGuid();
            var serverTopic = $"{server.MountPoint.TrimEnd('/')}/#";
            var subscription = await SubscribeRawAsync(serverTopic,
                HandleRpcAsync, ct).ConfigureAwait(false);
            if (!_handlers.TryAdd(id, (server, subscription)))
            {
                await subscription.DisposeAsync().ConfigureAwait(false);
                throw new ResourceExhaustionException("Failed to add handler");
            }
            return new Unsubscribe(async () =>
            {
                if (_handlers.TryRemove(id, out var handler))
                {
                    await handler.Item2.DisposeAsync().ConfigureAwait(false);
                }
            });
        }

        /// <inheritdoc/>
        public void Start()
        {
            // Nothing to do
        }

        /// <inheritdoc/>
        public async ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
            string method, ReadOnlySequence<byte> payload, string contentType,
            TimeSpan? timeout = null, CancellationToken ct = default)
        {
            var callTimeout = timeout ??
                _options.Value.DefaultMethodCallTimeout ?? TimeSpan.FromSeconds(30);
            var attempt = -1;
            for (; attempt < (_options.Value.MethodCallTimeoutRetries ?? 1); attempt++)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(callTimeout);
                try
                {
                    return await CallInternalAsync(target, method, payload, contentType,
                        cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.RetryCallAfterTimeout();
                }
            }
            throw new MethodCallException(
                $"Timed out calling method {method} after {attempt + 1} attempts. " +
                $"Broker {_options.Value.HostName}:{_options.Value.Port} possibly unreachable.");
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            try
            {
                await _cts.CancelAsync().ConfigureAwait(false);
            }
            catch { }
            foreach (var handler in _handlers.Values)
            {
                try { await handler.Item2.DisposeAsync().ConfigureAwait(false); }
                catch (Exception ex) { _logger.RpcServerStopFailed(ex); }
            }
            _handlers.Clear();
            EventFilterEntry[] entries;
            lock (_eventSubs)
            {
                entries = _eventSubs.Values.ToArray();
                _eventSubs.Clear();
            }
            foreach (var entry in entries)
            {
                if (entry.Subscription != null)
                {
                    try { await entry.Subscription.DisposeAsync().ConfigureAwait(false); }
                    catch { }
                }
            }
            try
            {
                await _client.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ClientDisposeFailed(ex);
            }
            _cts.Dispose();
        }

        /// <summary>
        /// Call method internally (v5 / v3.11).
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="buffer"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        private async ValueTask<ReadOnlySequence<byte>> CallInternalAsync(string target,
            string method, ReadOnlySequence<byte> buffer, string contentType,
            CancellationToken ct)
        {
            await EnsureConnectedAsync(ct).ConfigureAwait(false);
            var tcs = new TaskCompletionSource<(string, MqttInboundMessage)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = ct.Register(() => tcs.TrySetCanceled()).ConfigureAwait(false);

            var requestId = Guid.NewGuid();
            IAsyncDisposable? subscription = null;
            try
            {
                int status;
                MqttInboundMessage message;
                if (_version != MqttVersion.v311)
                {
                    var responseTopic = MqttRpcProtocol.V5ResponseTopic(Identity);
                    subscription = await SubscribeRawAsync(responseTopic + "/#",
                        HandleRpcAsync, ct).ConfigureAwait(false);
                    _pending.TryAdd(requestId, tcs);

                    await PublishRpcAsync(MqttRpcProtocol.V5RequestTopic(target, method),
                        responseTopic, buffer, contentType, requestId.ToByteArray(),
                        null, ct).ConfigureAwait(false);

                    ct.ThrowIfCancellationRequested();
                    (_, message) = await tcs.Task.ConfigureAwait(false);

                    var statusText = message.UserProperties?
                        .FirstOrDefault(p => p.Key == MqttRpcProtocol.StatusCodeKey).Value;
                    status = int.Parse(statusText ?? "500", CultureInfo.InvariantCulture);
                }
                else
                {
                    subscription = await SubscribeRawAsync(
                        MqttRpcProtocol.V311ResponseFilter(target),
                        HandleRpcAsync, ct).ConfigureAwait(false);
                    _pending.TryAdd(requestId, tcs);

                    await PublishRpcAsync(
                        MqttRpcProtocol.V311RequestTopic(target, method, requestId),
                        null, buffer, contentType, null, null, ct).ConfigureAwait(false);

                    ct.ThrowIfCancellationRequested();
                    string topic;
                    (topic, message) = await tcs.Task.ConfigureAwait(false);

                    if (!MqttRpcProtocol.TryParseV311Response(topic, target,
                        out status, out var responseRequestId) ||
                        responseRequestId != requestId)
                    {
                        throw new MethodCallException("Did not get correct request id back.");
                    }
                }
                if (status != 200)
                {
                    MethodCallStatusException.Throw(message.Payload.ToArray(), status);
                }
                return message.Payload;
            }
            catch (ExternalDependencyException ex)
            {
                throw new MethodCallStatusException("Method Call failed.", ex);
            }
            finally
            {
                _pending.TryRemove(requestId, out _);
                if (subscription != null)
                {
                    await subscription.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Handle rpc messages (requests and responses).
        /// </summary>
        /// <param name="message"></param>
        private async Task HandleRpcAsync(MqttInboundMessage message)
        {
            if ((_pending.IsEmpty && _handlers.IsEmpty) || _isDisposed)
            {
                return;
            }
            if (!MqttRpcProtocol.ParseMessage(message.Topic, message.CorrelationData,
                message.ResponseTopic, out var isRequest, out var requestId,
                out var method, out var topicRoot))
            {
                return;
            }
            if (!isRequest && _pending.TryRemove(requestId, out var pending))
            {
                pending.TrySetResult((message.Topic, message));
                return;
            }
            if (isRequest && !_handlers.IsEmpty && method != null)
            {
                await InvokeAndRespondAsync(message, requestId, method,
                    topicRoot).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Invoke the registered handlers and publish the response.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="requestId"></param>
        /// <param name="method"></param>
        /// <param name="topicRoot"></param>
        private async Task InvokeAndRespondAsync(MqttInboundMessage message,
            Guid requestId, string method, string? topicRoot)
        {
            var ct = _cts.Token;
            var (payload, statusCode) = await InvokeAsync(method, message.Payload,
                message.ContentType ?? ContentMimeType.Json, ct).ConfigureAwait(false);
            var statusText = statusCode.ToString(CultureInfo.InvariantCulture);
            try
            {
                if (message.ResponseTopic != null)
                {
                    if (payload.IsEmpty)
                    {
                        payload = MqttRpcProtocol.EmptyPayload;
                    }
                    await PublishRpcAsync(message.ResponseTopic, null, payload, null,
                        message.CorrelationData,
                        [new KeyValuePair<string, string>(
                            MqttRpcProtocol.StatusCodeKey, statusText)],
                        ct).ConfigureAwait(false);
                }
                else
                {
                    topicRoot ??= "replies";
                    await PublishRpcAsync(MqttRpcProtocol.V311ResponseTopic(
                        topicRoot, statusCode, requestId), null, payload, null, null,
                        null, ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.InvokerExecutionFailed(ex);
            }
        }

        /// <summary>
        /// Invoke a method over the connected handlers.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="ct"></param>
        private async Task<(ReadOnlySequence<byte>, int)> InvokeAsync(string method,
            ReadOnlySequence<byte> payload, string contentType, CancellationToken ct)
        {
            foreach (var (server, _) in _handlers.Values)
            {
                try
                {
                    var result = await server.InvokeAsync(method, payload, contentType,
                        ct).ConfigureAwait(false);
                    if (result.Length > MaxMethodPayloadSizeInBytes)
                    {
                        _logger.PayloadTooLarge(result.Length);
                        return (default, 413); // RequestEntityTooLarge
                    }
                    return (result, 200);
                }
                catch (MethodCallStatusException mex)
                {
                    var body = new ReadOnlySequence<byte>(mex.Serialize());
                    return (body.Length > MaxMethodPayloadSizeInBytes ? default : body,
                        mex.Details.Status ?? 500);
                }
                catch (NotSupportedException)
                {
                    // Continue with next handler
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    return (default, 408); // RequestTimeout
                }
                catch (Exception)
                {
                    return (default, 405); // MethodNotAllowed
                }
            }
            return (default, 501); // NotImplemented
        }

        /// <summary>
        /// Publish a request or response message with the rpc quality of service.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="responseTopic"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <param name="correlationData"></param>
        /// <param name="properties"></param>
        /// <param name="ct"></param>
        private async Task PublishRpcAsync(string topic, string? responseTopic,
            ReadOnlySequence<byte> payload, string? contentType, byte[]? correlationData,
            IReadOnlyList<KeyValuePair<string, string>>? properties, CancellationToken ct)
        {
            MqttPublishProperties? props = null;
            if (_version != MqttVersion.v311)
            {
                props = new MqttPublishProperties
                {
                    ContentType = contentType,
                    ResponseTopic = responseTopic,
                    CorrelationData = correlationData,
                    UserProperties = properties?
                        .Select(p => new MqttUserProperty(p.Key, p.Value)).ToList()
                };
            }
            await _client.PublishAsync(topic, payload, MqttQoS.AtLeastOnce, false,
                props, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Dispatch an event to the registered consumers for a filter.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="message"></param>
        private async Task DispatchEventAsync(string filter, MqttInboundMessage message)
        {
            IEventConsumer[] consumers;
            lock (_eventSubs)
            {
                if (!_eventSubs.TryGetValue(filter, out var entry))
                {
                    return;
                }
                consumers = entry.Consumers
                    .Where(c => c != IEventConsumer.Null).ToArray();
            }
            if (consumers.Length == 0)
            {
                return;
            }
            var properties = new Dictionary<string, string?>();
            if (message.UserProperties != null)
            {
                foreach (var property in message.UserProperties)
                {
                    properties[property.Key] = property.Value;
                }
            }
            foreach (var consumer in consumers)
            {
                await consumer.HandleAsync(message.Topic, message.Payload,
                    message.ContentType ?? "NoContentType_UseMqttv5", properties,
                    this, _cts.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Remove an event consumer and unsubscribe when it was the last one.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="consumer"></param>
        private async ValueTask RemoveEventConsumerAsync(string topic, IEventConsumer consumer)
        {
            IAsyncDisposable? toDispose = null;
            lock (_eventSubs)
            {
                if (_eventSubs.TryGetValue(topic, out var entry))
                {
                    entry.Consumers.Remove(consumer);
                    if (entry.Consumers.Count == 0)
                    {
                        toDispose = entry.Subscription;
                        _eventSubs.Remove(topic);
                    }
                }
            }
            if (toDispose != null)
            {
                await toDispose.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Subscribe to a broker topic filter and pump inbound messages to a handler.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="handler"></param>
        /// <param name="ct"></param>
        private async Task<IAsyncDisposable> SubscribeRawAsync(string filter,
            Func<MqttInboundMessage, Task> handler, CancellationToken ct)
        {
            var options = new MqttSubscriptionOptions
            {
                QoS = (MqttQoS)_defaultQoS
            };
            var subscription = await _client.SubscribeAsync(filter, options, ct)
                .ConfigureAwait(false);
            return new RawSubscription(subscription, handler, _logger, _cts.Token);
        }

        /// <summary>
        /// Ensure the client has connected before publishing or subscribing.
        /// </summary>
        /// <param name="ct"></param>
        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_connected.IsCompletedSuccessfully)
            {
                return;
            }
            await _connected.WaitAsync(ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Connect with retry until the token is cancelled.
        /// </summary>
        /// <param name="ct"></param>
        private async Task ConnectWithRetryAsync(CancellationToken ct)
        {
            var delay = _options.Value.ReconnectDelay ?? TimeSpan.FromSeconds(5);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _client.ConnectAsync(ct).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ConnectFailed(_options.Value.HostName ?? "localhost",
                        _options.Value.Port ?? 1883, ex.Message);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Build the underlying mqtt client from the options.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="loggerFactory"></param>
        private MqttClient BuildClient(MqttOptions o, ILoggerFactory? loggerFactory)
        {
            var useTls = o.UseTls ?? (o.Port != null && o.Port != 1883);
            var host = o.HostName ?? "localhost";
            var port = o.Port ?? (useTls ? 8883 : 1883);
            var scheme = o.WebSocketPath != null
                ? (useTls ? "wss" : "ws")
                : (useTls ? "mqtts" : "mqtt");
            var builder = MqttClient.CreateBuilder()
                .ConnectTo($"{scheme}://{host}:{port}")
                .WithClientId(Identity)
                .WithProtocol(_version == MqttVersion.v311
                    ? MqttProtocolVersion.V311 : MqttProtocolVersion.V500)
                .WithCleanStart(o.CleanStart ?? true);

            if (o.KeepAlivePeriod != null)
            {
                builder = builder.WithKeepAlive(
                    (ushort)Math.Clamp(o.KeepAlivePeriod.Value.TotalSeconds, 0, ushort.MaxValue));
            }
            if (o.UserName != null)
            {
                var password = o.Password != null
                    ? Encoding.UTF8.GetBytes(o.Password)
                    : (o.PasswordFile != null ? File.ReadAllBytes(o.PasswordFile) : []);
                builder = builder.WithCredentials(o.UserName, password);
            }
            if (useTls && o.AllowUntrustedCertificates == true)
            {
                builder = builder.WithTls(tls =>
                    tls.RemoteCertificateValidationCallback =
                        (_, _, _, _) => true);
            }
            if (loggerFactory != null)
            {
                builder = builder.WithLogging(loggerFactory);
            }
            builder = builder.Configure(co =>
            {
                if (o.WebSocketPath != null)
                {
                    co.WebSocketPath = o.WebSocketPath;
                }
                if (o.ReceiveMaximum != null)
                {
                    co.ReceiveMaximum = o.ReceiveMaximum.Value;
                }
                if (o.SessionExpiry != null)
                {
                    co.OperationTimeout = co.OperationTimeout;
                }
                if (_version != MqttVersion.v311)
                {
                    co.MaxIncomingPacketSize = 268435455;
                }
            });
            return builder.Build();
        }

        /// <summary>
        /// Convert a received message to the internal inbound representation,
        /// copying the (pooled) payload so it can outlive the receive scope.
        /// </summary>
        /// <param name="msg"></param>
        private static MqttInboundMessage ToInbound(MqttMessage msg)
        {
            var payload = msg.Payload.ToArray();
            byte[]? correlation = null;
            var cd = msg.Properties?.CorrelationData;
            if (cd != null)
            {
                correlation = cd.Value.ToArray();
            }
            List<KeyValuePair<string, string>>? props = null;
            var up = msg.Properties?.UserProperties;
            if (up != null)
            {
                props = up.Select(p =>
                    new KeyValuePair<string, string>(p.Name, p.Value)).ToList();
            }
            return new MqttInboundMessage
            {
                Topic = msg.Topic,
                Payload = new ReadOnlySequence<byte>(payload),
                ContentType = msg.Properties?.ContentType,
                ResponseTopic = msg.Properties?.ResponseTopic,
                CorrelationData = correlation,
                UserProperties = props
            };
        }

        /// <summary>
        /// A raw broker subscription with a pump loop dispatching inbound messages.
        /// </summary>
        private sealed class RawSubscription : IAsyncDisposable
        {
            public RawSubscription(MqttSubscription subscription,
                Func<MqttInboundMessage, Task> handler, ILogger logger,
                CancellationToken ct)
            {
                _subscription = subscription;
                _pump = Task.Run(() => PumpAsync(handler, logger, ct), ct);
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    await _subscription.DisposeAsync().ConfigureAwait(false);
                }
                catch { }
                try
                {
                    await _pump.ConfigureAwait(false);
                }
                catch { }
            }

            private async Task PumpAsync(Func<MqttInboundMessage, Task> handler,
                ILogger logger, CancellationToken ct)
            {
                try
                {
                    await foreach (var msg in _subscription.Reader
                        .ReadAllAsync(ct).ConfigureAwait(false))
                    {
                        using (msg)
                        {
                            var inbound = ToInbound(msg);
                            try
                            {
                                await handler(inbound).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger.MessageHandlingFailed(ex);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (ChannelClosedException) { }
            }

            private readonly MqttSubscription _subscription;
            private readonly Task _pump;
        }

        /// <summary>
        /// Registered event consumers for a topic filter.
        /// </summary>
        private sealed class EventFilterEntry
        {
            public List<IEventConsumer> Consumers { get; } = [];
            public IAsyncDisposable? Subscription { get; set; }
        }

        /// <summary>
        /// Disposable that runs an async cleanup action once.
        /// </summary>
        private sealed class Unsubscribe : IAsyncDisposable
        {
            public Unsubscribe(Func<ValueTask> dispose)
            {
                _dispose = dispose;
            }

            public async ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    await _dispose().ConfigureAwait(false);
                }
            }

            private readonly Func<ValueTask> _dispose;
            private int _disposed;
        }

        private readonly IOptions<MqttOptions> _options;
        private readonly ILogger<MqttClientTransport> _logger;
        private readonly MqttClient _client;
        private readonly MqttVersion _version;
        private readonly QoS _defaultQoS;
        private readonly CancellationTokenSource _cts;
        private readonly Task _connected;
        private readonly ConcurrentDictionary<Guid,
            (IRpcHandler, IAsyncDisposable)> _handlers = new();
        private readonly ConcurrentDictionary<Guid,
            TaskCompletionSource<(string, MqttInboundMessage)>> _pending = new();
        private readonly Dictionary<string, EventFilterEntry> _eventSubs = [];
        private bool _isDisposed;
    }
}
