// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Exceptions;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client manager
    /// </summary>
    internal sealed class OpcUaClientManager : IOpcUaClientManager<ConnectionModel>,
        IOpcUaSubscriptionManager, IEndpointDiscovery, ICertificateServices<EndpointModel>,
        IClientAccessor<ConnectionModel>, IConnectionServices<ConnectionModel>, IDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<EndpointConnectivityState>? OnConnectionStateChange;

        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="serializer"></param>
        /// <param name="options"></param>
        /// <param name="configuration"></param>
        /// <param name="metrics"></param>
        /// <param name="sessionFactory"></param>
        public OpcUaClientManager(ILoggerFactory loggerFactory, IJsonSerializer serializer,
            IOptions<OpcUaClientOptions> options, IOpcUaConfiguration configuration,
            IMetricsContext? metrics = null, ISessionFactory? sessionFactory = null)
        {
            _metrics = metrics ??
                IMetricsContext.Empty;
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

            _logger = _loggerFactory.CreateLogger<OpcUaClientManager>();
            _sessionFactory = sessionFactory ?? DefaultSessionFactory.Instance;
            _reverseConnectManager = new ReverseConnectManager();
            _reverseConnectStartException = new Lazy<Exception?>(
                StartReverseConnectManager, isThreadSafe: true);
            _configuration.Validate += OnValidate;
            InitializeMetrics();
        }

        /// <inheritdoc/>
        public ValueTask<IOpcUaSubscription> CreateSubscriptionAsync(
            SubscriptionModel subscription, IMetricsContext metrics,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return OpcUaSubscription.CreateAsync(this, _options, subscription,
                _loggerFactory, new OpcUaClientTagList(
                    subscription.Id.Connection, metrics ?? _metrics), ct);
        }

        /// <inheritdoc/>
        public IOpcUaClient GetOrCreateClient(ConnectionModel connection)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            connection.ThrowIfInvalid(nameof(connection));
            return GetOrAddClient(connection);
        }

        /// <inheritdoc/>
        public IOpcUaClient? GetClient(ConnectionModel connection)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var client = FindClient(connection);
            client?.AddRef();
            return client;
        }

        /// <inheritdoc/>
        public IAsyncDisposable Sample(ConnectionModel connection,
            TimeSpan samplingRate, ReadValueId nodeToRead, Action<uint, DataValue> callback)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var client = GetOrAddClient(connection);
            return client.RegisterSampler(samplingRate, nodeToRead, callback);
        }

        /// <inheritdoc/>
        public Task<ConnectResponseModel> ConnectAsync(ConnectionModel connection,
            ConnectRequestModel request, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            using var client = GetOrAddClient(connection);

            var handle = Guid.NewGuid().ToString();
            client.AddRef(handle, request.ExpiresAfter);
            return Task.FromResult(new ConnectResponseModel
            {
                ConnectionHandle = handle
            });
        }

        /// <inheritdoc/>
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            ConnectionModel endpoint, TestConnectionRequestModel request,
            CancellationToken ct)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Endpoint?.Url))
            {
                throw new ArgumentException("Endpoint url is missing.", nameof(endpoint));
            }

            var endpointUrl = endpoint.Endpoint.Url;
            var endpointDescription = CoreClientUtils.SelectEndpoint(
                _configuration.Value, endpointUrl,
                    endpoint.Endpoint.SecurityMode != SecurityMode.None);
            var endpointConfiguration = EndpointConfiguration.Create(_configuration.Value);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription,
                endpointConfiguration);
            var userIdentity = await endpoint.User.ToUserIdentityAsync(
                _configuration.Value).ConfigureAwait(false);
            try
            {
                using var session = await _sessionFactory.CreateAsync(
                    _configuration.Value, reverseConnectManager: null, configuredEndpoint,
                    updateBeforeConnect: true, // Update endpoint through discovery
                    checkDomain: false, // Domain must match on connect
                    "Test" + Guid.NewGuid().ToString(),
                    10000, userIdentity, null, ct).ConfigureAwait(false);
                try
                {
                    Debug.Assert(session != null);
                    await session.CloseAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    // We close as a courtesy to the server
                }
                return new TestConnectionResponseModel();
            }
            catch (Exception ex)
            {
                return new TestConnectionResponseModel
                {
                    ErrorInfo = ex.ToServiceResultModel()
                };
            }
        }

        /// <inheritdoc/>
        public Task DisconnectAsync(ConnectionModel connection,
            DisconnectRequestModel request, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var client = FindClient(connection);
            client?.Release(request.ConnectionHandle);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, IReadOnlyList<string>? locales, CancellationToken ct)
        {
            var results = new HashSet<DiscoveredEndpointModel>();
            var visitedUris = new HashSet<string> {
                CreateDiscoveryUri(discoveryUrl.ToString(), 4840)
            };
            var queue = new Queue<Tuple<Uri, List<string>>>();
            var localeIds = locales != null ? new StringCollection(locales) : null;
            queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
            ct.ThrowIfCancellationRequested();
            while (queue.Count > 0)
            {
                var nextServer = queue.Dequeue();
                discoveryUrl = nextServer.Item1;
                var sw = Stopwatch.StartNew();
                _logger.LogDebug("Try finding endpoints at {DiscoveryUrl}...", discoveryUrl);
                try
                {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 20000, visitedUris,
                            queue, results),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Exception occurred during FindEndpoints at {DiscoveryUrl}.",
                        discoveryUrl);
                    _logger.LogError("Could not find endpoints at {DiscoveryUrl} " +
                        "due to {Error} (after {Elapsed}).",
                        discoveryUrl, ex.Message, sw.Elapsed);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.LogDebug("Finding endpoints at {DiscoveryUrl} completed in {Elapsed}.",
                    discoveryUrl, sw.Elapsed);
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url))
            {
                throw new ArgumentException("Endpoint url is missing.", nameof(endpoint));
            }
            var endpointConfiguration = EndpointConfiguration.Create(_configuration.Value);
            endpointConfiguration.OperationTimeout = 20000;
            var discoveryUrl = new Uri(endpoint.Url);
            using var client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration);
            // Get endpoint descriptions from endpoint url
            var endpoints = await client.GetEndpointsAsync(new RequestHeader(),
                client.Endpoint.EndpointUrl, null, null).ConfigureAwait(false);

            // Match to provided endpoint info
            var ep = endpoints.Endpoints?.FirstOrDefault(e => e.IsSameAs(endpoint));
            if (ep == null)
            {
                _logger.LogDebug("No endpoints at {DiscoveryUrl}...", discoveryUrl);
                throw new ResourceNotFoundException("Endpoint not found");
            }
            _logger.LogDebug("Found endpoint at {DiscoveryUrl}...", discoveryUrl);
            return ep.ServerCertificate.ToCertificateChain();
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteAsync<T>(ConnectionModel connection,
            Func<ServiceCallContext, Task<T>> func, CancellationToken ct)
        {
            if (connection.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            using var client = GetOrAddClient(connection);
            return await client.RunAsync(func, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> ExecuteAsync<T>(ConnectionModel connection,
            Stack<Func<ServiceCallContext, ValueTask<IEnumerable<T>>>> stack,
            CancellationToken ct)
        {
            if (connection.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            return ExecuteAsyncCore(ct);

            async IAsyncEnumerable<T> ExecuteAsyncCore(
                [EnumeratorCancellation] CancellationToken ct)
            {
                using var client = GetOrAddClient(connection);
                await foreach (var result in client.RunAsync(stack, ct).ConfigureAwait(false))
                {
                    yield return result;
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                _meter.Dispose();

                DisposeAsync().AsTask().GetAwaiter().GetResult();

                _reverseConnectManager.Dispose();

                _configuration.Validate -= OnValidate;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _logger.LogInformation("Stopping all clients...");
            foreach (var client in _clients)
            {
                try
                {
                    await client.Value.DisposeAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected exception disposing session {Name}",
                        client.Key);
                }
            }
            _clients.Clear();
            _logger.LogInformation(
                "Stopped all sessions, current number of sessions is 0");
        }

        /// <summary>
        /// Find the client using the connection information
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private OpcUaClient? FindClient(ConnectionModel connection)
        {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            // try to get an existing session
            if (!_clients.TryGetValue(id, out var client))
            {
                return null;
            }
            return client;
        }

        /// <summary>
        /// Perform a single discovery using a discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="localeIds"></param>
        /// <param name="caps"></param>
        /// <param name="timeout"></param>
        /// <param name="visitedUris"></param>
        /// <param name="queue"></param>
        /// <param name="result"></param>
        private async Task DiscoverAsync(Uri discoveryUrl, StringCollection? localeIds,
            IEnumerable<string> caps, int timeout, HashSet<string> visitedUris,
            Queue<Tuple<Uri, List<string>>> queue, HashSet<DiscoveredEndpointModel> result)
        {
            var endpointConfiguration = EndpointConfiguration.Create(_configuration.Value);
            endpointConfiguration.OperationTimeout = timeout;
            using var client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration);
            //
            // Get endpoints from current discovery server
            //
            var endpoints = await client.GetEndpointsAsync(new RequestHeader(),
                client.Endpoint.EndpointUrl, localeIds, null).ConfigureAwait(false);
            if (!(endpoints?.Endpoints?.Any() ?? false))
            {
                _logger.LogDebug("No endpoints at {DiscoveryUrl}...", discoveryUrl);
                return;
            }
            _logger.LogDebug("Found endpoints at {DiscoveryUrl}...", discoveryUrl);

            foreach (var ep in endpoints.Endpoints.Where(ep =>
                ep.Server.ApplicationType != Opc.Ua.ApplicationType.DiscoveryServer))
            {
                result.Add(new DiscoveredEndpointModel
                {
                    Description = ep, // Reported
                    AccessibleEndpointUrl = new UriBuilder(ep.EndpointUrl)
                    {
                        Host = discoveryUrl.DnsSafeHost
                    }.ToString(),
                    Capabilities = new HashSet<string>(caps)
                });
            }

            //
            // Now Find servers on network.  This might fail for old lds
            // as well as reference servers, then we call FindServers...
            //
            try
            {
                var response = await client.FindServersOnNetworkAsync(new RequestHeader(),
                    0, 1000, new StringCollection()).ConfigureAwait(false);
                foreach (var server in response?.Servers ?? new ServerOnNetworkCollection())
                {
                    var url = CreateDiscoveryUri(server.DiscoveryUrl, discoveryUrl.Port);
                    if (!visitedUris.Contains(url))
                    {
                        queue.Enqueue(Tuple.Create(discoveryUrl,
                            server.ServerCapabilities.ToList()));
                        visitedUris.Add(url);
                    }
                }
            }
            catch
            {
                // Old lds, just continue...
                _logger.LogDebug("{DiscoveryUrl} does not support ME extension...",
                    discoveryUrl);
            }

            //
            // Call FindServers first to push more unique discovery urls
            // into the discovery queue
            //
            var found = await client.FindServersAsync(new RequestHeader(),
                client.Endpoint.EndpointUrl, localeIds, null).ConfigureAwait(false);
            if (found?.Servers != null)
            {
                foreach (var server in found.Servers.SelectMany(s => s.DiscoveryUrls))
                {
                    var url = CreateDiscoveryUri(server, discoveryUrl.Port);
                    if (!visitedUris.Contains(url))
                    {
                        queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
                        visitedUris.Add(url);
                    }
                }
            }
        }

        /// <summary>
        /// Create discovery url from string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="defaultPort"></param>
        private static string CreateDiscoveryUri(string uri, int defaultPort)
        {
            var url = new UriBuilder(uri);
            if (url.Port is 0 or (-1))
            {
                url.Port = defaultPort;
            }
            url.Host = url.Host.Trim('.');
            url.Path = url.Path.Trim('/');
            return url.Uri.ToString();
        }

        /// <summary>
        /// Load client configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            if (e.Accept || e.AcceptAll)
            {
                return;
            }
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                if (_configuration.Value.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    _logger.LogWarning(
                        "Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    e.AcceptAll = true;
                    e.Accept = true;
                }

                // Validate thumbprint
                else if (e.Certificate.RawData != null &&
                    !string.IsNullOrWhiteSpace(e.Certificate.Thumbprint) &&
                    _clients.Keys.Any(id => id?.Connection?.Endpoint?.Certificate != null &&
                    e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate))
                {
                    e.Accept = true;

                    _logger.LogInformation(
                        "Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "since the same thumbprint was specified in the connection!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);

                    // add the certificate to trusted store
                    _configuration.Value.SecurityConfiguration
                        .AddTrustedPeer(e.Certificate.RawData);
                    try
                    {
                        var store = _configuration.Value.
                            SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                        try
                        {
                            store.Delete(e.Certificate.Thumbprint);
                            store.Add(e.Certificate);
                        }
                        finally
                        {
                            store.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to add peer certificate {Thumbprint}, '{Subject}' " +
                            "to trusted store", e.Certificate.Thumbprint, e.Certificate.Subject);
                    }
                }
            }
            if (!e.Accept)
            {
                _logger.LogInformation("Rejecting peer certificate {Thumbprint}, '{Subject}' " +
                    "because of {Status}.", e.Certificate.Thumbprint, e.Certificate.Subject,
                    e.Error.StatusCode);
            }
        }

        /// <summary>
        /// Get or add new client
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private OpcUaClient GetOrAddClient(ConnectionModel connection)
        {
            // Lazy start connect manager
            var reverseConnect = connection.IsReverseConnect();
            if (reverseConnect && _reverseConnectStartException.Value != null)
            {
                throw _reverseConnectStartException.Value;
            }

            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            // try to get an existing session
            var client = _clients.GetOrAdd(id, id =>
            {
                var client = new OpcUaClient(_configuration.Value, id, _serializer,
                    _loggerFactory, _meter, _metrics, OnConnectionStateChange, _sessionFactory,
                    reverseConnect ? _reverseConnectManager : null,
                    _options.Value.MaxReconnectDelay)
                {
                    OperationTimeout = _options.Value.Quotas.OperationTimeout == 0 ? null :
                        TimeSpan.FromMilliseconds(_options.Value.Quotas.OperationTimeout),

                    DisableComplexTypePreloading = _options.Value.DisableComplexTypePreloading,
                    MinReconnectDelay = _options.Value.MinReconnectDelay,
                    CreateSessionTimeout = _options.Value.CreateSessionTimeout,
                    KeepAliveInterval = _options.Value.KeepAliveInterval,
                    SessionTimeout = _options.Value.DefaultSessionTimeout,
                    LingerTimeout = _options.Value.LingerTimeout
                };
                _logger.LogInformation("New client {Client} created.", client);
                return client;
            });

            client.AddRef();
            return client;
        }

        /// <summary>
        /// Start reverse connect manager service
        /// </summary>
        /// <returns></returns>
        private Exception? StartReverseConnectManager()
        {
            var port = _options.Value.ReverseConnectPort ?? 4840;
            try
            {
                _reverseConnectManager.StartService(new ReverseConnectClientConfiguration
                {
                    HoldTime = 120000,
                    WaitTimeout = 120000,
                    ClientEndpoints = new ReverseConnectClientEndpointCollection
                    {
                        new ReverseConnectClientEndpoint
                        {
                            EndpointUrl = $"opc.tcp://localhost:{port}"
                        }
                    }
                });
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Create metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_count",
                () => new Measurement<int>(_clients.Count, _metrics.TagList),
                "Clients", "Number of clients.");
        }

        private const int kMaxDiscoveryAttempts = 3;
        private bool _disposed;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOpcUaConfiguration _configuration;
        private readonly IOptions<OpcUaClientOptions> _options;
        private readonly IJsonSerializer _serializer;
        private readonly ReverseConnectManager _reverseConnectManager;
        private readonly Lazy<Exception?> _reverseConnectStartException;
        private readonly ConcurrentDictionary<ConnectionIdentifier, OpcUaClient> _clients = new();
        private readonly ISessionFactory _sessionFactory;
        private readonly IMetricsContext _metrics;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }
}
