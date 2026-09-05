// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.IoTEdge
{
    using Azure.IIoT.OpcUa.Core.IoTEdge;
    using global::IoTHubby;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Leases an IoT Hub event client for a writer-group connection string.
    /// </summary>
    /// <remarks>
    /// The default edge transport is a singleton created from the IoT Edge
    /// workload environment. A writer group can instead carry a device
    /// connection string in its transport configuration. Compatible leases
    /// share one client per hub/device/module identity. Credential or client
    /// configuration changes require releasing every old lease first, rather
    /// than connecting two MQTT clients with the same client identifier.
    /// </remarks>
    public sealed class IoTHubEventClientFactory : IEventClientFactory
    {
        /// <inheritdoc/>
        public string Name => "IoTHub";

        /// <summary>
        /// Create the factory.
        /// </summary>
        public IoTHubEventClientFactory(
            IOptions<IoTEdgeClientOptions> options,
            IEnumerable<IIoTEdgeClientState> stateHandlers,
            ILoggerFactory loggerFactory,
            IoTEdgeTransport? defaultClient = null,
            IIoTEdgeDeviceIdentity? defaultIdentity = null)
            : this(options, stateHandlers, loggerFactory,
                IoTHubModuleClientFactory.Instance, defaultClient, defaultIdentity)
        {
        }

        internal IoTHubEventClientFactory(
            IOptions<IoTEdgeClientOptions> options,
            IEnumerable<IIoTEdgeClientState> stateHandlers,
            ILoggerFactory loggerFactory,
            IIoTHubModuleClientFactory clientFactory,
            IoTEdgeTransport? defaultClient = null,
            IIoTEdgeDeviceIdentity? defaultIdentity = null)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(stateHandlers);
            _options = options.Value;
            _stateHandlers = [.. stateHandlers];
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _clientFactory = clientFactory ??
                throw new ArgumentNullException(nameof(clientFactory));
            if ((defaultClient is null) != (defaultIdentity is null))
            {
                throw new ArgumentException(
                    "The default IoT Hub transport and identity must be supplied together.");
            }
            _defaultClient = defaultClient;
            if (defaultClient is not null && !string.IsNullOrEmpty(_options.EdgeHubConnectionString))
            {
                var connection = IoTHubConnectionSettings.Parse(_options.EdgeHubConnectionString);
                _defaultIdentity = new ClientIdentity(CanonicalHost(connection.HostName),
                    connection.DeviceId, connection.ModuleId);
                _defaultConfiguration = new ClientConfiguration(connection, _options);
            }
            else if (defaultIdentity is not null)
            {
                if (string.IsNullOrWhiteSpace(defaultIdentity.Hub))
                {
                    throw new ArgumentException("The default IoT Hub identity requires a hub.",
                        nameof(defaultIdentity));
                }
                _defaultIdentity = new ClientIdentity(CanonicalHost(defaultIdentity.Hub),
                    defaultIdentity.DeviceId, defaultIdentity.ModuleId);
            }
        }

        /// <inheritdoc/>
        public IDisposable CreateEventClient(string connectionString,
            out IEventClient client)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            var connection = IoTHubConnectionSettings.Parse(connectionString);
            var options = new IoTEdgeClientOptions
            {
                EdgeHubConnectionString = connectionString,
                Product = _options.Product,
                KeepAlivePeriodSeconds = _options.KeepAlivePeriodSeconds,
                DefaultMethodCallTimeout = _options.DefaultMethodCallTimeout
            };
            var key = new ClientIdentity(CanonicalHost(connection.HostName),
                connection.DeviceId, connection.ModuleId);
            var configuration = new ClientConfiguration(connection, options);
            lock (_gate)
            {
                if (key == _defaultIdentity)
                {
                    if (_defaultConfiguration is null
                        || !_defaultConfiguration.IsCompatible(configuration))
                    {
                        throw new InvalidOperationException(
                            "The IoT Hub identity is owned by the default transport with " +
                            "different credentials or settings. Use the default transport " +
                            "without a writer-group connection string.");
                    }
                    client = _defaultClient!;
                    return new EventClientScope(null, null);
                }
                if (_clients.TryGetValue(key, out var existing))
                {
                    if (existing.References == 0)
                    {
                        throw new InvalidOperationException(
                            "The previous client for this IoT Hub identity is still closing " +
                            "or failed to close. A competing client cannot be created.");
                    }
                    if (!existing.Configuration.IsCompatible(configuration))
                    {
                        throw new InvalidOperationException(
                            "This IoT Hub identity already has active leases with different " +
                            "credentials or settings. Remove its writer groups and wait for " +
                            "their connections and metadata cleanup before reconfiguring it.");
                    }
                    existing.References++;
                    client = existing.Transport;
                    return new EventClientScope(this, existing);
                }

                var wrapped = Options.Create(options);
                var identity = new IoTEdgeIdentity(connection.HostName, connection.DeviceId,
                    connection.ModuleId, connection.GatewayHostName);
#pragma warning disable CA2000 // Ownership transfers to EventClientScope below.
                var moduleClient = new IoTEdgeModuleClient(wrapped, identity,
                    _stateHandlers, _loggerFactory, _clientFactory);
#pragma warning restore CA2000
                try
                {
                    var transport = new IoTEdgeTransport(moduleClient,
                        _loggerFactory.CreateLogger<IoTEdgeTransport>());
                    var entry = new SharedClient(key, configuration, transport, moduleClient);
                    _clients.Add(key, entry);
                    client = transport;
                    return new EventClientScope(this, entry);
                }
                catch
                {
                    moduleClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    throw;
                }
            }
        }

        private Task ReleaseAsync(SharedClient entry)
        {
            lock (_gate)
            {
                if (--entry.References != 0)
                {
                    return Task.CompletedTask;
                }
            }
            return DisposeClientAsync(entry);
        }

        private async Task DisposeClientAsync(SharedClient entry)
        {
            try
            {
                await entry.Transport.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                await entry.Client.DisposeAsync().ConfigureAwait(false);
            }
            // Keep the identity reserved until shutdown succeeds. A failed
            // shutdown must not allow a second, competing MQTT connection.
            lock (_gate)
            {
                _clients.Remove(entry.Identity);
            }
        }

        private static string CanonicalHost(string host)
        {
            return host.TrimEnd('.').ToUpperInvariant();
        }

        private sealed record ClientIdentity(string Hub, string Device, string? Module);

        private sealed class ClientConfiguration
        {
            public ClientConfiguration(IoTHubConnectionSettings connection,
                IoTEdgeClientOptions options)
            {
                _connection = connection;
                _gateway = string.IsNullOrEmpty(connection.GatewayHostName) ? null :
                    CanonicalHost(connection.GatewayHostName);
                _product = string.IsNullOrEmpty(options.Product) ? null : options.Product;
                var defaults = new IoTHubClientOptions();
                _keepAlive = options.KeepAlivePeriodSeconds is > 0
                    ? TimeSpan.FromSeconds(options.KeepAlivePeriodSeconds.Value)
                    : defaults.KeepAlive;
                _timeout = options.DefaultMethodCallTimeout ?? defaults.OperationTimeout;
                if (connection.SharedAccessKey is not null)
                {
                    byte[] key;
                    try
                    {
                        key = Convert.FromBase64String(connection.SharedAccessKey);
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException("The IoT Hub shared access key is invalid.");
                    }
                    try
                    {
                        _keyFingerprint = Convert.ToHexString(SHA256.HashData(key));
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(key);
                    }
                }
            }

            public bool IsCompatible(ClientConfiguration other)
            {
                return _connection.UsesX509 == other._connection.UsesX509
                    && _keyFingerprint == other._keyFingerprint
                    && _connection.SharedAccessKeyName == other._connection.SharedAccessKeyName
                    && _connection.SharedAccessSignature == other._connection.SharedAccessSignature
                    && _gateway == other._gateway
                    && _product == other._product
                    && _keepAlive == other._keepAlive
                    && _timeout == other._timeout;
            }

            private readonly IoTHubConnectionSettings _connection;
            private readonly string? _keyFingerprint;
            private readonly string? _gateway;
            private readonly string? _product;
            private readonly TimeSpan _keepAlive;
            private readonly TimeSpan _timeout;
        }

        private sealed class SharedClient(ClientIdentity identity,
            ClientConfiguration configuration, IoTEdgeTransport transport,
            IoTEdgeModuleClient client)
        {
            public ClientIdentity Identity { get; } = identity;
            public ClientConfiguration Configuration { get; } = configuration;
            public IoTEdgeTransport Transport { get; } = transport;
            public IoTEdgeModuleClient Client { get; } = client;
            public int References { get; set; } = 1;
        }

        private sealed class EventClientScope(IoTHubEventClientFactory? factory,
            SharedClient? entry) : IDisposable, IAsyncDisposable
        {
            public void Dispose()
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            public ValueTask DisposeAsync()
            {
                lock (_gate)
                {
                    return new ValueTask(_disposeTask ??=
                        factory is null ? Task.CompletedTask : factory.ReleaseAsync(entry!));
                }
            }

            private readonly Lock _gate = new();
            private Task? _disposeTask;
        }

        private readonly Lock _gate = new();
        private readonly Dictionary<ClientIdentity, SharedClient> _clients = [];
        private readonly IoTEdgeTransport? _defaultClient;
        private readonly ClientIdentity? _defaultIdentity;
        private readonly ClientConfiguration? _defaultConfiguration;
        private readonly IoTEdgeClientOptions _options;
        private readonly IIoTEdgeClientState[] _stateHandlers;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IIoTHubModuleClientFactory _clientFactory;
    }
}
