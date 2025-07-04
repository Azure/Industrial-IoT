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
    using Nito.AsyncEx;
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
        IEndpointDiscovery, ICertificateServices<EndpointModel>, IClientDiagnostics,
        IConnectionServices<ConnectionModel>, IDisposable
    {
        /// <inheritdoc/>
        public event EventHandler<EndpointConnectivityStateEventArgs>? OnConnectionStateChange;

        /// <inheritdoc/>
        IReadOnlyList<ChannelDiagnosticModel> IClientDiagnostics.ChannelDiagnostics
            => _clients.Values.Select(c => c.LastDiagnostics).ToList();

        /// <inheritdoc/>
        public IReadOnlyList<ConnectionModel> ActiveConnections
            => _clients.Keys.Select(c => c.Connection).ToList();

        /// <summary>
        /// Create kv manager
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="serializer"></param>
        /// <param name="configuration"></param>
        /// <param name="clientOptions"></param>
        /// <param name="subscriptionOptions"></param>
        /// <param name="timeProvider"></param>
        /// <param name="metrics"></param>
        public OpcUaClientManager(ILoggerFactory loggerFactory, IJsonSerializer serializer,
            IOpcUaConfiguration configuration, IOptions<OpcUaClientOptions> clientOptions,
            IOptions<OpcUaSubscriptionOptions> subscriptionOptions,
            TimeProvider? timeProvider = null, IMetricsContext? metrics = null)
        {
            _metrics = metrics ??
                IMetricsContext.Empty;
            _timeProvider = timeProvider ??
                TimeProvider.System;
            _clientOptions = clientOptions ??
                throw new ArgumentNullException(nameof(clientOptions));
            _subscriptionOptions = subscriptionOptions ??
                throw new ArgumentNullException(nameof(subscriptionOptions));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));

            _logger = _loggerFactory.CreateLogger<OpcUaClientManager>();
            _reverseConnectManager = new ReverseConnectManager();
            _reverseConnectStartException = new Lazy<Exception?>(
                StartReverseConnectManager, isThreadSafe: true);
            _configuration.Validate += OnValidate;
            InitializeMetrics();
        }

        /// <inheritdoc/>
        public async ValueTask<ISubscription> CreateSubscriptionAsync(
            ConnectionModel connection, SubscriptionModel subscription,
            ISubscriber callback, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            using var client = GetOrAddClient(connection);
            return await client.RegisterAsync(
                subscription, callback, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TestConnectionResponseModel> TestConnectionAsync(
            ConnectionModel endpoint, TestConnectionRequestModel request, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint.Endpoint?.Url);

            var endpointUrl = new Uri(endpoint.Endpoint.Url);
            try
            {
                var endpointDescription = await OpcUaClient.SelectEndpointAsync(
                    _configuration.Value, endpointUrl, null,
                    endpoint.Endpoint.SecurityMode ?? SecurityMode.NotNone,
                    endpoint.Endpoint.SecurityPolicy, _logger, endpoint,
                    ct: ct).ConfigureAwait(false);

                var endpointConfiguration = EndpointConfiguration.Create(
                    _configuration.Value);
                var configuredEndpoint = new ConfiguredEndpoint(null,
                    endpointDescription, endpointConfiguration);
                var userIdentity = await endpoint.User.ToUserIdentityAsync(
                    _configuration.Value).ConfigureAwait(false);
                using var session = await DefaultSessionFactory.Instance.CreateAsync(
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
        public Task ResetAllConnectionsAsync(CancellationToken ct)
        {
            return Task.WhenAll(_clients.Values.Select(c => c.ResetAsync(ct)).ToArray());
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ConnectionDiagnosticsModel> GetConnectionDiagnosticsAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            foreach (var kv in _clients.ToList())
            {
                SessionDiagnosticsModel? server = null;
                try
                {
                    server = await kv.Value.GetSessionDiagnosticsAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.GetDiagnosticsFailed(ex, kv.Value);
                }
                yield return new ConnectionDiagnosticsModel
                {
                    Connection = kv.Key.Connection,
                    // Client = kv.Value.Diagnostics,
                    Server = server
                };
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChannelDiagnosticModel> WatchChannelDiagnosticsAsync(
            [EnumeratorCancellation] CancellationToken ct)
        {
            var queue = new AsyncProducerConsumerQueue<ChannelDiagnosticModel>();
            _listeners.TryAdd(queue, true);
            try
            {
                // Get all items from buffer
                var set = new HashSet<ChannelDiagnosticModel>(
                    _clients.Values.Select(c => c.LastDiagnostics));
                foreach (var item in set)
                {
                    yield return item;
                }

                // Dequeue items we have not yet sent from current state from queue
                // until cancelled
                while (!ct.IsCancellationRequested)
                {
                    // Get updates - handle fact that we have already sent the reference
                    var item = await queue.DequeueAsync(ct).ConfigureAwait(false);
                    if (!set.Contains(item))
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                _listeners.TryRemove(queue, out _);
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, IReadOnlyList<string>? locales, bool findServersOnNetwork,
            CancellationToken ct)
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
                _logger.FindingEndpoints(discoveryUrl);
                try
                {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 20000, visitedUris,
                            queue, results, findServersOnNetwork),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.FindEndpointsException(ex, discoveryUrl);
                    _logger.FindEndpointsFailed(discoveryUrl, ex.Message, sw.Elapsed);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.FindingEndpointsCompleted(discoveryUrl, sw.Elapsed);
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
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
                _logger.NoEndpoints(discoveryUrl);
                throw new ResourceNotFoundException("Endpoint not found");
            }
            _logger.FoundEndpoint(discoveryUrl);
            return ep.ServerCertificate.ToCertificateChain();
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteAsync<T>(ConnectionModel connection,
            Func<ServiceCallContext, Task<T>> func, RequestHeaderModel? header,
            CancellationToken ct)
        {
            connection = UpdateConnectionFromHeader(connection, header);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            using var client = GetOrAddClient(connection);
            return await client.RunAsync(func, header?.ConnectTimeout,
                header?.ServiceCallTimeout, ct).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<T> ExecuteAsync<T>(ConnectionModel connection,
            AsyncEnumerableBase<T> operation, RequestHeaderModel? header,
            CancellationToken ct)
        {
            connection = UpdateConnectionFromHeader(connection, header);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                operation.Dispose();
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            return ExecuteAsyncCore(ct);

            async IAsyncEnumerable<T> ExecuteAsyncCore(
                [EnumeratorCancellation] CancellationToken ct)
            {
                try
                {
                    using var client = GetOrAddClient(connection);
                    await foreach (var result in client.RunAsync(operation,
                        header?.ConnectTimeout, header?.ServiceCallTimeout,
                        ct).ConfigureAwait(false))
                    {
                        yield return result;
                    }
                }
                finally
                {
                    operation.Dispose();
                }
            }
        }

        /// <inheritdoc/>
        public async Task<ISessionHandle> AcquireSessionAsync(ConnectionModel connection,
            RequestHeaderModel? header, CancellationToken ct)
        {
            connection = UpdateConnectionFromHeader(connection, header);
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            using var client = GetOrAddClient(connection);
            return await client.AcquireAsync(header?.ConnectTimeout,
                header?.ServiceCallTimeout, ct).ConfigureAwait(false);
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

            _logger.StoppingAllClients(_clients.Count);
            foreach (var client in _clients)
            {
                try
                {
                    await client.Value.CloseAsync(true).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.DisposeClientFailed(ex, client.Key);
                }
            }
            _clients.Clear();
            _logger.StoppedAllClients();
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
        /// <param name="findServersOnNetwork"></param>
        private async Task DiscoverAsync(Uri discoveryUrl, StringCollection? localeIds,
            IEnumerable<string> caps, int timeout, HashSet<string> visitedUris,
            Queue<Tuple<Uri, List<string>>> queue, HashSet<DiscoveredEndpointModel> result,
            bool findServersOnNetwork)
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
                _logger.NoEndpoints(discoveryUrl);
                return;
            }
            _logger.FoundEndpoints(discoveryUrl);

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

            if (!findServersOnNetwork)
            {
                return;
            }

            //
            // Now Find servers on network.  This might fail for old lds
            // as well as reference servers, then we call FindServers...
            //
            try
            {
                var response = await client.FindServersOnNetworkAsync(new RequestHeader(),
                    0, 1000, []).ConfigureAwait(false);
                foreach (var server in response?.Servers ?? [])
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
                _logger.ExtensionNotSupported(discoveryUrl);
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
        /// Update connection from header
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        private static ConnectionModel UpdateConnectionFromHeader(ConnectionModel connection,
            RequestHeaderModel? header)
        {
            if (header == null)
            {
                return connection;
            }
            if (header.Elevation != null)
            {
                connection = connection with
                {
                    User = header.Elevation,
                };
            }
            if (header.Locales != null)
            {
                connection = connection with
                {
                    Locales = header.Locales
                };
            }
            return connection;
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
        /// Load kv configuration
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
                    _logger.AcceptingUntrustedCert(e.Certificate.Thumbprint, e.Certificate.Subject);
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

                    _logger.AcceptingUntrustedCertByThumbprint(e.Certificate.Thumbprint, e.Certificate.Subject);

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
                        _logger.AddPeerCertToTrustedStoreFailed(ex, e.Certificate.Thumbprint, e.Certificate.Subject);
                    }
                }
            }
            if (!e.Accept)
            {
                _logger.RejectingPeerCert(e.Certificate.Thumbprint, e.Certificate.Subject, e.Error.StatusCode);
            }
        }

        /// <summary>
        /// Get or add new kv
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

            // Find kv and if not exists create
            var id = new ConnectionIdentifier(connection);
            // try to get an existing kv
            var client = _clients.GetOrAdd(id, id =>
            {
                var client = new OpcUaClient(_configuration.Value, id, _serializer,
                    _loggerFactory, _timeProvider, _meter, _metrics, OnConnectionStateChange,
                    reverseConnect ? _reverseConnectManager : null,
                    OnClientConnectionDiagnosticChange, _clientOptions, _subscriptionOptions);
                _logger.CreatedNewClient(client);
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
            var port = _clientOptions.Value.ReverseConnectPort ?? 4840;
            try
            {
                _reverseConnectManager.StartService(new ReverseConnectClientConfiguration
                {
                    HoldTime = 120000,
                    WaitTimeout = 120000,
                    ClientEndpoints =
                    [
                        new ReverseConnectClientEndpoint
                        {
                            EndpointUrl = $"opc.tcp://localhost:{port}"
                        }
                    ]
                });
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        /// <summary>
        /// Called by clients when their connection information changed
        /// </summary>
        /// <param name="model"></param>
        private void OnClientConnectionDiagnosticChange(ChannelDiagnosticModel model)
        {
            foreach (var listener in _listeners.Keys)
            {
                listener.Enqueue(model);
            }
        }

        /// <summary>
        /// Create metrics
        /// </summary>
        private void InitializeMetrics()
        {
            _meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_count",
                () => new Measurement<int>(_clients.Count, _metrics.TagList),
                description: "Number of clients.");
        }

        private const int kMaxDiscoveryAttempts = 3;
        private bool _disposed;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly TimeProvider _timeProvider;
        private readonly IOpcUaConfiguration _configuration;
        private readonly IOptions<OpcUaClientOptions> _clientOptions;
        private readonly IOptions<OpcUaSubscriptionOptions> _subscriptionOptions;
        private readonly IJsonSerializer _serializer;
        private readonly ReverseConnectManager _reverseConnectManager;
        private readonly Lazy<Exception?> _reverseConnectStartException;
        private readonly ConcurrentDictionary<
            AsyncProducerConsumerQueue<ChannelDiagnosticModel>, bool> _listeners = new();
        private readonly ConcurrentDictionary<ConnectionIdentifier, OpcUaClient> _clients = new();
        private readonly IMetricsContext _metrics;
        private readonly Meter _meter = Diagnostics.NewMeter();
    }

    /// <summary>
    /// Source-generated logging definitions for OpcUaClientManager
    /// </summary>
    internal static partial class OpcUaClientManagerLogging
    {
        private const int EventClass = 600;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Error,
            Message = "Failed to get diagnostics for client {Client}.")]
        public static partial void GetDiagnosticsFailed(this ILogger logger, Exception ex,
            OpcUaClient client);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "Try finding endpoints at {DiscoveryUrl}...")]
        public static partial void FindingEndpoints(this ILogger logger, Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "Exception occurred during FindEndpoints at {DiscoveryUrl}.")]
        public static partial void FindEndpointsException(this ILogger logger, Exception ex,
            Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Error,
            Message = "Could not find endpoints at {DiscoveryUrl} due to {Error} (after {Elapsed}).")]
        public static partial void FindEndpointsFailed(this ILogger logger, Uri discoveryUrl,
            string error, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Debug,
            Message = "Finding endpoints at {DiscoveryUrl} completed in {Elapsed}.")]
        public static partial void FindingEndpointsCompleted(this ILogger logger,
            Uri discoveryUrl, TimeSpan elapsed);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Debug,
            Message = "No endpoints at {DiscoveryUrl}...")]
        public static partial void NoEndpoints(this ILogger logger, Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 7, Level = LogLevel.Debug,
            Message = "Found endpoint at {DiscoveryUrl}...")]
        public static partial void FoundEndpoint(this ILogger logger, Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 8, Level = LogLevel.Debug,
            Message = "Found endpoints at {DiscoveryUrl}...")]
        public static partial void FoundEndpoints(this ILogger logger, Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 9, Level = LogLevel.Debug,
            Message = "{DiscoveryUrl} does not support ME extension...")]
        public static partial void ExtensionNotSupported(this ILogger logger, Uri discoveryUrl);

        [LoggerMessage(EventId = EventClass + 10, Level = LogLevel.Information,
            Message = "Stopping all {Count} clients...")]
        public static partial void StoppingAllClients(this ILogger logger, int count);

        [LoggerMessage(EventId = EventClass + 11, Level = LogLevel.Error,
            Message = "Unexpected exception disposing client {Client}")]
        public static partial void DisposeClientFailed(this ILogger logger, Exception ex,
            ConnectionIdentifier client);

        [LoggerMessage(EventId = EventClass + 12, Level = LogLevel.Information,
            Message = "Stopped all clients, current number of clients is 0")]
        public static partial void StoppedAllClients(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 13, Level = LogLevel.Warning,
            Message = "Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
            "due to AutoAccept(UntrustedCertificates) set!")]
        public static partial void AcceptingUntrustedCert(this ILogger logger,
            string thumbprint, string subject);

        [LoggerMessage(EventId = EventClass + 14, Level = LogLevel.Information,
            Message = "Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
            "since the same thumbprint was specified in the connection!")]
        public static partial void AcceptingUntrustedCertByThumbprint(this ILogger logger,
            string thumbprint, string subject);

        [LoggerMessage(EventId = EventClass + 15, Level = LogLevel.Error,
            Message = "Failed to add peer certificate {Thumbprint}, '{Subject}' to trusted store")]
        public static partial void AddPeerCertToTrustedStoreFailed(this ILogger logger,
            Exception ex, string thumbprint, string subject);

        [LoggerMessage(EventId = EventClass + 16, Level = LogLevel.Information,
            Message = "Rejecting peer certificate {Thumbprint}, '{Subject}' because of {Status}.")]
        public static partial void RejectingPeerCert(this ILogger logger, string thumbprint,
            string subject, StatusCode status);

        [LoggerMessage(EventId = EventClass + 17, Level = LogLevel.Information,
            Message = "{Client}: Created new client.")]
        public static partial void CreatedNewClient(this ILogger logger, OpcUaClient client);
    }
}
