// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services {
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Exceptions;
    using Azure.IIoT.OpcUa.Shared.Models;
    using Furly.Extensions.Utils;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Client.ComplexTypes;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly.Extensions.Serializers;

    /// <summary>
    /// Client manager
    /// </summary>
    public class OpcUaClientManager : IClientHost, ISessionProvider<ConnectionModel>,
        ISubscriptionManager, IEndpointDiscovery, IDisposable,
        ICertificateServices<EndpointModel>, IConnectionServices<ConnectionModel> {

        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="serializer"></param>
        /// <param name="identity"></param>
        /// <param name="logger"></param>
        /// <param name="metrics"></param>
        public OpcUaClientManager(ILogger logger, IClientServicesConfig clientConfig,
            IJsonSerializer serializer, IProcessIdentity identity = null,
            IMetricsContext metrics = null)
            : this(metrics ?? new EmptyMetricsContext()) {
            _clientConfig = clientConfig ??
                throw new ArgumentNullException(nameof(clientConfig));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _configuration = _clientConfig.BuildApplicationConfigurationAsync(
                 identity == null ? "OpcUaClient" : identity.ToIdentityString(),
                 OnValidate, _logger);
            _lock = new SemaphoreSlim(1, 1);
            _cts = new CancellationTokenSource();
            _processor = Task.Factory.StartNew(() => RunClientManagerAsync(
                TimeSpan.FromSeconds(5), _cts.Token), _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public async ValueTask StartAsync() {
            await _configuration;
        }

        /// <inheritdoc/>
        public ValueTask<ISubscription> CreateSubscriptionAsync(SubscriptionModel subscription,
            CancellationToken ct) {
            var client = FindClient(subscription.Id.Connection);
            return OpcUaSubscription.CreateAsync(this, _clientConfig, subscription, _logger,
                client ?? _metrics, ct);
        }

        /// <inheritdoc/>
        public async ValueTask<ISessionHandle> GetOrCreateSessionAsync(ConnectionModel connection,
            IMetricsContext metrics, CancellationToken ct) {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            await _lock.WaitAsync(ct);
            try {
                // try to get an existing session
                if (!_clients.TryGetValue(id, out var client)) {
                    client = CreateClient(id, metrics ?? _metrics);
                    _clients.AddOrUpdate(id, client);
                    _logger.LogInformation(
                        "New session {Name} added, current number of sessions is {Count}.",
                        id, _clients.Count);
                }
                // Try and connect the session
                try {
                    await client.ConnectAsync(false, ct);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to connect session {Name}. " +
                        "Continue with unconnected session.", id);
                }
                return client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task ConnectAsync(ConnectionModel connection,
            CredentialModel credential, CancellationToken ct) {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(ConnectionModel connection,
            CredentialModel credential, CancellationToken ct) {
            await _lock.WaitAsync(ct);
            try {
                var id = new ConnectionIdentifier(connection);
                if (!_clients.TryGetValue(id, out var client)) {
                    throw new ResourceNotFoundException(
                        "Cannot disconnect. Connection not found.");
                }
                if (client.HasSubscriptions) {
                    throw new ResourceInvalidStateException(
                        "Cannot disconnect. Connection has subscriptions.");
                }
                if (!_clients.TryRemove(id, out client)) {
                    return;
                }
                await client.DisposeAsync();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public ISessionHandle GetSessionHandle(ConnectionModel connection) {
            return FindClient(connection);
        }

        /// <inheritdoc/>
        public ISessionHandle GetSessionHandle(ISession session) {
            if (session?.Handle is not OpcUaClient client) {
                throw new ResourceInvalidStateException("Session does not belong to this object.");
            }
            return client;
        }

        /// <inheritdoc/>
        public async Task AddTrustedPeerAsync(byte[] certificates) {
            var configuration = await _configuration;
            var chain = Utils.ParseCertificateChainBlob(certificates)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0) {
                throw new ArgumentNullException(nameof(certificates));
            }
            var certificate = chain.First();
            try {
                _logger.LogInformation("Adding Certificate {Thumbprint}, " +
                    "{Subject} to trust list...", certificate.Thumbprint,
                    certificate.Subject);
                configuration.SecurityConfiguration.TrustedPeerCertificates
                    .Add(certificate.YieldReturn());
                chain.RemoveAt(0);
                if (chain.Count > 0) {
                    configuration.SecurityConfiguration.TrustedIssuerCertificates
                        .Add(chain);
                }
                return;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to add Certificate {Thumbprint}, " +
                    "{Subject} to trust list.", certificate.Thumbprint,
                    certificate.Subject);
                throw;
            }
            finally {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public async Task RemoveTrustedPeerAsync(byte[] certificates) {
            var configuration = await _configuration;
            var chain = Utils.ParseCertificateChainBlob(certificates)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0) {
                throw new ArgumentNullException(nameof(certificates));
            }
            var certificate = chain.First();
            try {
                _logger.LogInformation("Removing Certificate {Thumbprint}, " +
                    "{Subject} from trust list...", certificate.Thumbprint,
                    certificate.Subject);
                configuration.SecurityConfiguration.TrustedPeerCertificates
                    .Remove(certificate.YieldReturn());

                // Remove only from trusted peers
                return;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to remove Certificate {Thumbprint}, " +
                    "{Subject} from trust list.", certificate.Thumbprint,
                    certificate.Subject);
                throw;
            }
            finally {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (!_disposed) {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync() {
            if (_disposed) {
                return;
            }
            _disposed = true;
            try {
                _logger.LogInformation("Stopping client manager process ...");
                _cts.Cancel();
                await _processor;
            }
            finally {
                _logger.LogDebug("Client manager process stopped.");
                _cts.Dispose();
            }

            _logger.LogInformation("Stopping all client sessions...");
            await _lock.WaitAsync();
            try {
                foreach (var client in _clients) {
                    try {
                        await client.Value.DisposeAsync();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Unexpected exception disposing session {Name}",
                            client.Key);
                    }
                }
                _clients.Clear();
            }
            finally {
                _lock.Release();
                _lock.Dispose();
                _logger.LogInformation(
                    "Stopped all sessions, current number of sessions is 0");
            }
        }

        /// <summary>
        /// Find the client using the connection information
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        private OpcUaClient FindClient(ConnectionModel connection) {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // try to get an existing session
                if (!_clients.TryGetValue(id, out var client)) {
                    return null;
                }
                return client;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Manage the clients in the client manager.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunClientManagerAsync(TimeSpan period, CancellationToken ct) {
            var timer = new PeriodicTimer(period);
            _logger.LogDebug("Client manager starting...");
            while (ct.IsCancellationRequested) {
                if (!await timer.WaitForNextTickAsync(ct)) {
                    break;
                }

                _logger.LogDebug("Running client manager connection and garbage collection cycle...");
                var inactive = new Dictionary<ConnectionIdentifier, OpcUaClient>();
                await _lock.WaitAsync(ct);
                try {
                    foreach (var client in _clients) {
                        //
                        // If active (lifetime and whether we have subscriptions
                        // keep the client connected
                        //
                        if (client.Value.IsActive) {
                            var connect = client.Value.ConnectAsync(true, ct);
                            if (!connect.IsCompletedSuccessfully) {
                                try {
                                    await connect;
                                }
                                catch (Exception ex) {
                                    _logger.LogDebug(ex,
                                        "Client manager failed to re-connect session {Name}.",
                                        client.Key);
                                }
                            }
                        }
                        else {
                            // Collect inactive clients
                            inactive.Add(client.Key, client.Value);
                        }
                    }

                    // Remove inactive clients from client list
                    foreach (var key in inactive.Keys) {
                        _clients.TryRemove(key, out _);
                    }
                }
                catch (OperationCanceledException) {
                    break;
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Client manager encountered unexpected error.");
                }
                finally {
                    _lock.Release();
                }

                // Garbage collect inactives
                if (inactive.Count > 0) {
                    foreach (var client in inactive.Values) {
                        await client.DisposeAsync();
                    }
                    _logger.LogInformation(
                        "Garbage collected {Sessions} sessions" +
                        ", current number of sessions is {Count}.",
                        inactive.Count, _clients.Count);
                    inactive.Clear();
                }
            }
            _logger.LogDebug("Client manager exiting...");
        }

        // Validate certificates
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e) {
            if (e.Accept) {
                return;
            }
            var configuration = _configuration.Result;
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                if (configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                    _logger.LogWarning("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    e.Accept = true;
                }

                // Validate thumbprint
                if (e.Certificate.RawData != null && !string.IsNullOrWhiteSpace(e.Certificate.Thumbprint)) {

                    if (_clients.Keys.Any(id => id?.Connection?.Endpoint?.Certificate != null &&
                        e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate)) {
                        e.Accept = true;

                        _logger.LogInformation("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                            "since it was specified in the endpoint!",
                            e.Certificate.Thumbprint, e.Certificate.Subject);

                        // add the certificate to trusted store
                        configuration.SecurityConfiguration.AddTrustedPeer(e.Certificate.RawData);
                        try {
                            var store = configuration.
                                SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                            try {
                                store.Delete(e.Certificate.Thumbprint);
                                store.Add(e.Certificate);
                            }
                            finally {
                                store.Close();
                            }
                        }
                        catch (Exception ex) {
                            _logger.LogWarning(ex, "Failed to add peer certificate {Thumbprint}, '{Subject}' " +
                                "to trusted store", e.Certificate.Thumbprint, e.Certificate.Subject);
                        }
                    }
                }
            }
            if (!e.Accept) {
                _logger.LogInformation("Rejecting peer certificate {Thumbprint}, '{Subject}' " +
                    "because of {Status}.", e.Certificate.Thumbprint, e.Certificate.Subject,
                    e.Error.StatusCode);
            }
        }

        /// <summary>
        /// Create new client
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        private OpcUaClient CreateClient(ConnectionIdentifier id, IMetricsContext metrics) {
            return new OpcUaClient(_configuration.Result, id, _serializer, _logger, metrics) {
                KeepAliveInterval = _clientConfig.KeepAliveInterval,
                SessionLifeTime = _clientConfig.DefaultSessionTimeout
            };
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, IReadOnlyList<string> locales, CancellationToken ct) {
            var results = new HashSet<DiscoveredEndpointModel>();
            var visitedUris = new HashSet<string> {
                CreateDiscoveryUri(discoveryUrl.ToString(), 4840)
            };
            var queue = new Queue<Tuple<Uri, List<string>>>();
            var localeIds = locales != null ? new StringCollection(locales) : null;
            queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
            ct.ThrowIfCancellationRequested();
            while (queue.Count > 0) {
                var nextServer = queue.Dequeue();
                discoveryUrl = nextServer.Item1;
                var sw = Stopwatch.StartNew();
                _logger.LogDebug("Try finding endpoints at {discoveryUrl}...", discoveryUrl);
                try {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 20000, visitedUris,
                            queue, results),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogDebug(ex, "Exception occurred duringing FindEndpoints at {discoveryUrl}.",
                        discoveryUrl);
                    _logger.LogError("Could not find endpoints at {discoveryUrl} " +
                        "due to {error} (after {elapsed}).",
                        discoveryUrl, ex.Message, sw.Elapsed);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.LogDebug("Finding endpoints at {discoveryUrl} completed in {elapsed}.",
                    discoveryUrl, sw.Elapsed);
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<X509CertificateChainModel> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpoint?.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var configuration = await _configuration;
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            endpointConfiguration.OperationTimeout = 20000;
            var discoveryUrl = new Uri(endpoint.Url);
            using (var client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration)) {
                // Get endpoint descriptions from endpoint url
                var endpoints = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, null, null).ConfigureAwait(false);

                // Match to provided endpoint info
                var ep = endpoints.Endpoints?.FirstOrDefault(e => e.IsSameAs(endpoint));
                if (ep == null) {
                    _logger.LogDebug("No endpoints at {discoveryUrl}...", discoveryUrl);
                    throw new ResourceNotFoundException("Endpoint not found");
                }
                _logger.LogDebug("Found endpoint at {discoveryUrl}...", discoveryUrl);
                return ep.ServerCertificate.ToCertificateChain();
            }
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteServiceAsync<T>(ConnectionModel connection,
            Func<ISessionHandle, Task<T>> service, CancellationToken ct) {
            if (connection.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            _cts.Token.ThrowIfCancellationRequested();
            if (await GetOrCreateSessionAsync(connection, null, ct) is not OpcUaClient client) {
                throw new ConnectionException("Failed to execute call, " +
                    $"no connection for {connection?.Endpoint?.Url}");
            }
            return await client.RunAsync(service, ct);
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
        private async Task DiscoverAsync(Uri discoveryUrl, StringCollection localeIds,
            IEnumerable<string> caps, int timeout, HashSet<string> visitedUris,
            Queue<Tuple<Uri, List<string>>> queue, HashSet<DiscoveredEndpointModel> result) {

            var configuration = await _configuration;
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
            endpointConfiguration.OperationTimeout = timeout;
            using (var client = DiscoveryClient.Create(discoveryUrl, endpointConfiguration)) {
                //
                // Get endpoints from current discovery server
                //
                var endpoints = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, localeIds, null).ConfigureAwait(false);
                if (!(endpoints?.Endpoints?.Any() ?? false)) {
                    _logger.LogDebug("No endpoints at {discoveryUrl}...", discoveryUrl);
                    return;
                }
                _logger.LogDebug("Found endpoints at {discoveryUrl}...", discoveryUrl);

                foreach (var ep in endpoints.Endpoints.Where(ep =>
                    ep.Server.ApplicationType != Opc.Ua.ApplicationType.DiscoveryServer)) {
                    result.Add(new DiscoveredEndpointModel {
                        Description = ep, // Reported
                        AccessibleEndpointUrl = new UriBuilder(ep.EndpointUrl) {
                            Host = discoveryUrl.DnsSafeHost
                        }.ToString(),
                        Capabilities = new HashSet<string>(caps)
                    });
                }

                //
                // Now Find servers on network.  This might fail for old lds
                // as well as reference servers, then we call FindServers...
                //
                try {
                    var response = await client.FindServersOnNetworkAsync(null, 0, 1000,
                        new StringCollection()).ConfigureAwait(false);
                    var servers = response?.Servers ?? new ServerOnNetworkCollection();
                    foreach (var server in servers) {
                        var url = CreateDiscoveryUri(server.DiscoveryUrl, discoveryUrl.Port);
                        if (!visitedUris.Contains(url)) {
                            queue.Enqueue(Tuple.Create(discoveryUrl,
                                server.ServerCapabilities.ToList()));
                            visitedUris.Add(url);
                        }
                    }
                }
                catch {
                    // Old lds, just continue...
                    _logger.LogDebug("{discoveryUrl} does not support ME extension...",
                        discoveryUrl);
                }

                //
                // Call FindServers first to push more unique discovery urls
                // into the discovery queue
                //
                var found = await client.FindServersAsync(null,
                    client.Endpoint.EndpointUrl, localeIds, null).ConfigureAwait(false);
                if (found?.Servers != null) {
                    var servers = found.Servers.SelectMany(s => s.DiscoveryUrls);
                    foreach (var server in servers) {
                        var url = CreateDiscoveryUri(server, discoveryUrl.Port);
                        if (!visitedUris.Contains(url)) {
                            queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
                            visitedUris.Add(url);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create discovery url from string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="defaultPort"></param>
        private static string CreateDiscoveryUri(string uri, int defaultPort) {
            var url = new UriBuilder(uri);
            if (url.Port == 0 || url.Port == -1) {
                url.Port = defaultPort;
            }
            url.Host = url.Host.Trim('.');
            url.Path = url.Path.Trim('/');
            return url.Uri.ToString();
        }

        /// <summary>
        /// Create metrics
        /// </summary>
        /// <param name="metrics"></param>
        private OpcUaClientManager(IMetricsContext metrics) {
            Diagnostics.Meter_CreateObservableUpDownCounter("iiot_edge_publisher_client_count",
                () => new Measurement<int>(_clients.Count, metrics.TagList), "Clients",
                "Monitored item count.");
            _metrics = metrics;
        }

        private const int kMaxDiscoveryAttempts = 3;
        private bool _disposed;
        private readonly ILogger _logger;
        private readonly IClientServicesConfig _clientConfig;
        private readonly IJsonSerializer _serializer;
        private readonly ConcurrentDictionary<ConnectionIdentifier, OpcUaClient> _clients = new();
        private readonly SemaphoreSlim _lock;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processor;
        private readonly Task<ApplicationConfiguration> _configuration;
        private readonly IMetricsContext _metrics;
    }
}
