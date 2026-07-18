// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests.Fixtures
{
    using Azure.IIoT.OpcUa.Core;
    using Azure.IIoT.OpcUa.Core.AzureSdk;
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using Azure.IIoT.OpcUa.Core.Hosting;
    using Azure.IIoT.OpcUa.Core.Messaging;
    using Azure.IIoT.OpcUa.Core.Messaging.Clients.Mqtt;
    using Azure.IIoT.OpcUa.Core.Rpc;
    using Azure.IIoT.OpcUa.Core.Storage;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using MqttNetServer = MQTTnet.Server.MqttServer;
    using MqttNetServerFactory = MQTTnet.Server.MqttServerFactory;
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Test device twin.
    /// </summary>
    public sealed record class DeviceTwinModel
    {
        /// <summary>Device id.</summary>
        public string Id { get; set; } = null!;

        /// <summary>Module id.</summary>
        public string? ModuleId { get; set; }

        /// <summary>Primary key.</summary>
        public string? PrimaryKey { get; set; }

        /// <summary>Secondary key.</summary>
        public string? SecondaryKey { get; set; }
    }

    /// <summary>
    /// Twin service abstraction used by the fixture.
    /// </summary>
    public interface IIoTHubTwinServices
    {
        /// <summary>Host name.</summary>
        string HostName { get; }

        /// <summary>Create or update twin.</summary>
        ValueTask<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel device,
            bool force = false, CancellationToken ct = default);

        /// <summary>Get registration.</summary>
        ValueTask<DeviceTwinModel> GetRegistrationAsync(string deviceId,
            string? moduleId = null, CancellationToken ct = default);
    }

    /// <summary>
    /// Telemetry handler.
    /// </summary>
    public interface IIoTHubTelemetryHandler
    {
        /// <summary>Handle telemetry.</summary>
        ValueTask HandleAsync(string deviceId, string? moduleId, string topic,
            ReadOnlySequence<byte> data, string contentType, string contentEncoding,
            IReadOnlyDictionary<string, string> properties,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Event processor.
    /// </summary>
    public interface IIoTHubEventProcessor :
        IEventRegistration<IIoTHubTelemetryHandler>;

    /// <summary>
    /// Mock hub.
    /// </summary>
    public interface IIoTHub
    {
        /// <summary>Connect module.</summary>
        IIoTHubConnection Connect(string deviceId, string moduleId);
    }

    /// <summary>
    /// Mock hub connection.
    /// </summary>
    public interface IIoTHubConnection
    {
        /// <summary>RPC server.</summary>
        IRpcServer RpcServer { get; }

        /// <summary>Event client.</summary>
        IEventClient EventClient { get; }

        /// <summary>Twin store.</summary>
        IKeyValueStore Twin { get; }

        /// <summary>Close connection.</summary>
        void Close();
    }

    /// <summary>
    /// In-memory IoT Hub mock for module tests.
    /// </summary>
    public sealed class IoTHubMock : IIoTHubTwinServices, IIoTHubEventProcessor,
        IIoTHub, IRpcClient
    {
        /// <inheritdoc/>
        public string HostName { get; }

        /// <inheritdoc/>
        public string Name => "IoTHub-Mock";

        /// <inheritdoc/>
        public int MaxMethodPayloadSizeInBytes => 120 * 1024;

        /// <summary>
        /// Create mock.
        /// </summary>
        public IoTHubMock()
            : this(null)
        {
        }

        private IoTHubMock(IEnumerable<DeviceTwinModel>? devices)
        {
            HostName = "test.test.org";
            if (devices != null)
            {
                foreach (var device in devices)
                {
                    Upsert(device);
                }
            }
        }

        /// <summary>
        /// Create mock.
        /// </summary>
        /// <param name="devices"></param>
        /// <returns></returns>
        public static IoTHubMock Create(IEnumerable<DeviceTwinModel> devices)
        {
            return new IoTHubMock(devices);
        }

        /// <inheritdoc/>
        public ValueTask<DeviceTwinModel> CreateOrUpdateAsync(DeviceTwinModel device,
            bool force = false, CancellationToken ct = default)
        {
            return ValueTask.FromResult(Upsert(device));
        }

        /// <inheritdoc/>
        public ValueTask<DeviceTwinModel> GetRegistrationAsync(string deviceId,
            string? moduleId = null, CancellationToken ct = default)
        {
            if (_devices.TryGetValue(Key(deviceId, moduleId), out var device))
            {
                return ValueTask.FromResult(device);
            }
            throw new KeyNotFoundException($"{deviceId}/{moduleId}");
        }

        /// <inheritdoc/>
        public IDisposable Register(IIoTHubTelemetryHandler listener)
        {
            _listeners[listener] = listener;
            return new Registration(() => _listeners.TryRemove(listener, out _));
        }

        /// <inheritdoc/>
        public IIoTHubConnection Connect(string deviceId, string moduleId)
        {
            var key = Key(deviceId, moduleId);
            lock (_connectionsLock)
            {
                if (!_devices.TryGetValue(key, out var twin))
                {
                    throw new KeyNotFoundException($"{deviceId}/{moduleId}");
                }
                if (_connections.TryGetValue(key, out var connection) &&
                    connection.IsConnected)
                {
                    throw new InvalidOperationException(
                        $"Device {deviceId}/{moduleId} is already connected.");
                }
                connection = new IoTHubConnection(this, twin);
                _connections[key] = connection;
                return connection;
            }
        }

        /// <inheritdoc/>
        public ValueTask<ReadOnlySequence<byte>> CallAsync(string target,
            string method, ReadOnlySequence<byte> payload, string contentType,
            TimeSpan? timeout = null, CancellationToken ct = default)
        {
            if (!HubResource.Parse(target, out _, out var deviceId, out var moduleId,
                out var error))
            {
                throw new ArgumentException($"Target is malformed: {error}.", nameof(target));
            }

            IoTHubConnection connection;
            lock (_connectionsLock)
            {
                var key = Key(deviceId, moduleId);
                if (!_devices.ContainsKey(key))
                {
                    throw new ResourceNotFoundException("No such device");
                }
                if (!_connections.TryGetValue(key, out connection) ||
                    !connection.IsConnected)
                {
                    throw new TimeoutException("Timed out waiting for device to connect");
                }
            }
            return WaitForMethodAsync(connection, method, payload, contentType, timeout, ct);
        }

        private static async ValueTask<ReadOnlySequence<byte>> WaitForMethodAsync(
            IoTHubConnection connection, string method, ReadOnlySequence<byte> payload,
            string contentType, TimeSpan? timeout, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            using var timeoutCts = new CancellationTokenSource();
            if (timeout.HasValue)
            {
                timeoutCts.CancelAfter(timeout.Value);
            }
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                ct, timeoutCts.Token);
            try
            {
                return await connection.InvokeMethodAsync(method, payload, contentType,
                    linkedCts.Token).AsTask().WaitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for device method call.");
            }
        }

        private DeviceTwinModel Upsert(DeviceTwinModel device)
        {
            device.PrimaryKey ??= Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            device.SecondaryKey ??= Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            _devices[Key(device.Id, device.ModuleId)] = device;
            return device;
        }

        private static string Key(string deviceId, string? moduleId)
        {
            return $"{deviceId}/{moduleId}";
        }

        private sealed class Registration : IDisposable
        {
            public Registration(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                _dispose();
            }

            private readonly Action _dispose;
        }

        private sealed class IoTHubConnection : IIoTHubConnection
        {
            public IRpcServer RpcServer { get; }

            public IEventClient EventClient { get; }

            public IKeyValueStore Twin { get; }

            internal bool IsConnected => Volatile.Read(ref _isConnected) != 0;

            public IoTHubConnection(IoTHubMock outer, DeviceTwinModel device)
            {
                RpcServer = new InMemoryRpcServer(this);
                EventClient = new InMemoryEventClient(outer, device);
                Twin = new InMemoryTwin();
                Twin.State[Constants.TwinPropertyApiKeyKey] =
                    JsonValue.Create(Guid.NewGuid().ToString());
                _isConnected = 1;
            }

            public void Close()
            {
                Interlocked.Exchange(ref _isConnected, 0);
            }

            internal async ValueTask<ReadOnlySequence<byte>> InvokeMethodAsync(
                string method, ReadOnlySequence<byte> payload, string contentType,
                CancellationToken ct)
            {
                foreach (var handler in RpcServer.Connected)
                {
                    try
                    {
                        return await handler.InvokeAsync(method, payload,
                            contentType, ct).ConfigureAwait(false);
                    }
                    catch (MethodCallStatusException)
                    {
                        throw;
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (NotSupportedException)
                    {
                    }
                    catch (Exception ex)
                    {
                        throw new MethodCallStatusException(500, ex.Message);
                    }
                }
                throw new MethodCallStatusException(500, "Not supported");
            }

            private int _isConnected;
        }

        private sealed class InMemoryTwin : IKeyValueStore
        {
            public string Name => "IoTHubTwin";

            public IDictionary<string, JsonNode?> State { get; } =
                new ConcurrentDictionary<string, JsonNode?>();

            public ValueTask<JsonNode?> TryPageInAsync(string key,
                CancellationToken ct = default)
            {
                State.TryGetValue(key, out var value);
                return ValueTask.FromResult(value);
            }
        }

        private sealed class InMemoryEventClient : IEventClient, IProcessIdentity
        {
            public string Name => "IoTHub";

            public int MaxEventPayloadSizeInBytes => 256 * 1024;

            public string Identity { get; }

            public InMemoryEventClient(IoTHubMock outer, DeviceTwinModel device)
            {
                _outer = outer;
                _device = device;
                Identity = HubResource.Format(outer.HostName, device.Id, device.ModuleId);
            }

            public IEvent CreateEvent()
            {
                return new InMemoryEvent(this);
            }

            private sealed class InMemoryEvent : IEvent
            {
                public InMemoryEvent(InMemoryEventClient outer)
                {
                    _outer = outer;
                }

                public IEvent SetTopic(string? value)
                {
                    _topic = value ?? string.Empty;
                    return this;
                }

                public IEvent SetTimestamp(DateTimeOffset value) => this;

                public IEvent SetContentType(string? value)
                {
                    _contentType = value ?? string.Empty;
                    return this;
                }

                public IEvent SetContentEncoding(string? value)
                {
                    _contentEncoding = value ?? string.Empty;
                    return this;
                }

                public IEvent AsCloudEvent(CloudEventHeader header) => this;

                public IEvent SetSchema(IEventSchema schema) => this;

                public IEvent AddProperty(string name, string? value)
                {
                    if (value != null)
                    {
                        _properties[name] = value;
                    }
                    return this;
                }

                public IEvent SetRetain(bool value) => this;

                public IEvent SetQoS(QoS value) => this;

                public IEvent SetTtl(TimeSpan value) => this;

                public IEvent AddBuffers(IEnumerable<ReadOnlySequence<byte>> value)
                {
                    _buffers.AddRange(value);
                    return this;
                }

                public async ValueTask SendAsync(CancellationToken ct = default)
                {
                    foreach (var buffer in _buffers)
                    {
                        foreach (var listener in _outer._outer._listeners.Values)
                        {
                            await listener.HandleAsync(_outer._device.Id,
                                _outer._device.ModuleId, _topic, buffer, _contentType,
                                _contentEncoding, _properties, ct).ConfigureAwait(false);
                        }
                    }
                }

                public void Dispose()
                {
                    _buffers.Clear();
                }

                private readonly InMemoryEventClient _outer;
                private readonly List<ReadOnlySequence<byte>> _buffers = [];
                private readonly Dictionary<string, string> _properties = [];
                private string _topic = string.Empty;
                private string _contentType = string.Empty;
                private string _contentEncoding = string.Empty;
            }

            private readonly IoTHubMock _outer;
            private readonly DeviceTwinModel _device;
        }

        private sealed class InMemoryRpcServer : IRpcServer
        {
            public string Name => "IoTHub";

            public IEnumerable<IRpcHandler> Connected
            {
                get
                {
                    lock (_lock)
                    {
                        return _handlers.ToArray();
                    }
                }
            }

            public InMemoryRpcServer(IoTHubConnection connection)
            {
                _connection = connection;
            }

            public ValueTask<IAsyncDisposable> ConnectAsync(IRpcHandler server,
                CancellationToken ct = default)
            {
                if (!_connection.IsConnected)
                {
                    throw new InvalidOperationException(
                        "Cannot connect server on disconnected connection.");
                }
                ct.ThrowIfCancellationRequested();
                lock (_lock)
                {
                    _handlers.Add(server);
                }
#pragma warning disable CA2000 // Dispose objects before losing scope
                // Ownership is transferred to the caller through the returned registration.
                return ValueTask.FromResult<IAsyncDisposable>(
                    new HandlerRegistration(this, server));
#pragma warning restore CA2000 // Dispose objects before losing scope
            }

            public void Start()
            {
            }

            private sealed class HandlerRegistration : IAsyncDisposable
            {
                public HandlerRegistration(InMemoryRpcServer outer, IRpcHandler handler)
                {
                    _outer = outer;
                    _handler = handler;
                }

                public ValueTask DisposeAsync()
                {
                    lock (_outer._lock)
                    {
                        _outer._handlers.Remove(_handler);
                    }
                    return ValueTask.CompletedTask;
                }

                private readonly InMemoryRpcServer _outer;
                private readonly IRpcHandler _handler;
            }

            private readonly IoTHubConnection _connection;
            private readonly object _lock = new();
            private readonly List<IRpcHandler> _handlers = [];
        }

        private readonly ConcurrentDictionary<string, DeviceTwinModel> _devices = new();
        private readonly Dictionary<string, IoTHubConnection> _connections = [];
        private readonly object _connectionsLock = new();
        private readonly ConcurrentDictionary<IIoTHubTelemetryHandler,
            IIoTHubTelemetryHandler> _listeners = new();
    }

    /// <summary>
    /// Test MQTT server.
    /// </summary>
    public sealed class MqttServer : IAwaitable<MqttServer>, IDisposable
    {
        /// <summary>
        /// Create and start the test MQTT server.
        /// </summary>
        /// <param name="options"></param>
        public MqttServer(IOptions<MqttOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);
            var port = options.Value.Port ??
                throw new InvalidOperationException("MQTT test server port is not configured.");
            var factory = new MqttNetServerFactory();
            var serverOptions = factory.CreateServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
                .WithDefaultEndpointBoundIPV6Address(IPAddress.None)
                .WithDefaultEndpointPort(port)
                .Build();
            _server = factory.CreateMqttServer(serverOptions);
            _server.StartAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public IAwaiter<MqttServer> GetAwaiter()
        {
            return Task.FromResult(this).AsAwaiter();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _server.Dispose();
        }

        private readonly MqttNetServer _server;
    }

    /// <summary>
    /// MQTT server registration for integration tests.
    /// </summary>
    public static class MqttServerServiceCollectionEx
    {
        /// <summary>
        /// Add test MQTT server.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddMqttServer(this IServiceCollection services)
        {
            services.AddSingleton<MqttServer>();
            services.AddSingleton<MqttClientTransport>();
            services.AddSingleton<IEventClient>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IEventSubscriber>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IRpcClient>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IRpcServer>(
                static provider => provider.GetRequiredService<MqttClientTransport>());
            services.AddSingleton<IPostConfigureOptions<MqttOptions>, MqttConfig>();
            return services;
        }
    }
}
