// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Azure.IIoT.OpcUa.Publisher.Stack.Models;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Exceptions;
    using Furly;
    using Furly.Exceptions;
    using Furly.Extensions.Hosting;
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
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Client manager
    /// </summary>
    public sealed class OpcUaClientManager : ISessionProvider<ConnectionModel>,
        ISubscriptionManager, IEndpointDiscovery, IAwaitable<OpcUaClientManager>,
        ICertificateServices<EndpointModel>, IConnectionServices<ConnectionModel>,
        IDisposable
    {
        /// <summary>
        /// Create client manager
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        /// <param name="serializer"></param>
        /// <param name="identity"></param>
        /// <param name="metrics"></param>
        public OpcUaClientManager(ILoggerFactory loggerFactory, IOptions<ClientOptions> options,
            IJsonSerializer serializer, IProcessIdentity? identity = null,
            IMetricsContext? metrics = null)
        {
            _metrics = metrics ?? IMetricsContext.Empty;
            InitializeMetrics();
            _options = options ??
                throw new ArgumentNullException(nameof(options));
            _serializer = serializer ??
                throw new ArgumentNullException(nameof(serializer));
            _loggerFactory = loggerFactory ??
                throw new ArgumentNullException(nameof(loggerFactory));
            _logger = _loggerFactory.CreateLogger<OpcUaClientManager>();
            _configuration = _options.Value.BuildApplicationConfigurationAsync(
                 identity == null ? "OpcUaClient" : identity.Id,
                 OnValidate, _logger);
            _lock = new SemaphoreSlim(1, 1);
            _cts = new CancellationTokenSource();
            _processor = Task.Factory.StartNew(() => RunClientManagerAsync(
                TimeSpan.FromSeconds(5), _cts.Token), _cts.Token,
                    TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        /// <inheritdoc/>
        public IAwaiter<OpcUaClientManager> GetAwaiter()
        {
            return _configuration.AsAwaiter(this);
        }

        /// <inheritdoc/>
        public ValueTask<ISubscription> CreateSubscriptionAsync(SubscriptionModel subscription,
            IMetricsContext metrics, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return OpcUaSubscription.CreateAsync(this, _options, subscription, _loggerFactory,
                new OpcUaClientTagList(subscription.Id.Connection, metrics ?? _metrics), ct);
        }

        /// <inheritdoc/>
        public async ValueTask<ISessionHandle> GetOrCreateSessionAsync(ConnectionModel connection,
            IMetricsContext? metrics, CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // try to get an existing session
                if (_clients.TryGetValue(id, out var client) && !client.IsActive)
                {
                    _inactive.Enqueue(client);
                    client = null;
                }
                if (client == null)
                {
                    client = CreateClient(id, new OpcUaClientTagList(connection, metrics ?? _metrics));
                    _clients.AddOrUpdate(id, client);
                    _logger.LogInformation(
                        "New session {Name} added, current number of sessions is {Count}.",
                        id, _clients.Count);
                }
                // Try and connect the client
                try
                {
                    await client.ConnectAsync(false, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect session {Name}. " +
                        "Continue with unconnected session.", id);
                }
                return client;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public Task ConnectAsync(ConnectionModel endpoint, CredentialModel? credential,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync(ConnectionModel endpoint, CredentialModel? credential,
            CancellationToken ct)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var id = new ConnectionIdentifier(endpoint);
                if (!_clients.TryGetValue(id, out var client))
                {
                    throw new ResourceNotFoundException(
                        "Cannot disconnect. Connection not found.");
                }
                if (client.HasSubscriptions)
                {
                    throw new ResourceInvalidStateException(
                        "Cannot disconnect. Connection has subscriptions.");
                }
                if (!_clients.TryRemove(id, out client))
                {
                    return;
                }
                await client.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public ISessionHandle? GetSessionHandle(ConnectionModel connection)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return FindClient(connection);
        }

        /// <inheritdoc/>
        public ISessionHandle GetSessionHandle(ISession session)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (session?.Handle is not OpcUaClient client)
            {
                throw new ResourceInvalidStateException("Session does not belong to this object.");
            }
            return client;
        }

        /// <inheritdoc/>
        public async Task AddTrustedPeerAsync(byte[] certificate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var configuration = await _configuration.ConfigureAwait(false);
            var chain = Utils.ParseCertificateChainBlob(certificate)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            var x509Certificate = chain[0];
            try
            {
                _logger.LogInformation("Adding Certificate {Thumbprint}, " +
                    "{Subject} to trust list...", x509Certificate.Thumbprint,
                    x509Certificate.Subject);
                configuration.SecurityConfiguration.TrustedPeerCertificates
                    .Add(x509Certificate.YieldReturn());
                chain.RemoveAt(0);
                if (chain.Count > 0)
                {
                    configuration.SecurityConfiguration.TrustedIssuerCertificates
                        .Add(chain);
                }
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add Certificate {Thumbprint}, " +
                    "{Subject} to trust list.", x509Certificate.Thumbprint,
                    x509Certificate.Subject);
                throw;
            }
            finally
            {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public async Task RemoveTrustedPeerAsync(byte[] certificate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var configuration = await _configuration.ConfigureAwait(false);
            var chain = Utils.ParseCertificateChainBlob(certificate)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0)
            {
                throw new ArgumentNullException(nameof(certificate));
            }
            var x509Certificate = chain[0];
            try
            {
                _logger.LogInformation("Removing Certificate {Thumbprint}, " +
                    "{Subject} from trust list...", x509Certificate.Thumbprint,
                    x509Certificate.Subject);
                configuration.SecurityConfiguration.TrustedPeerCertificates
                    .Remove(x509Certificate.YieldReturn());

                // Remove only from trusted peers
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove Certificate {Thumbprint}, " +
                    "{Subject} from trust list.", x509Certificate.Thumbprint,
                    x509Certificate.Subject);
                throw;
            }
            finally
            {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_disposed)
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
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
            try
            {
                _logger.LogInformation("Stopping client manager process ...");
                _cts.Cancel();
                await _processor.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _logger.LogDebug("Client manager process stopped.");
                _cts.Dispose();
            }

            _logger.LogInformation("Stopping all client sessions...");
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
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
            }
            finally
            {
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
        private OpcUaClient? FindClient(ConnectionModel connection)
        {
            // Find session and if not exists create
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try
            {
                // try to get an existing session
                if (!_clients.TryGetValue(id, out var client))
                {
                    return null;
                }
                return client;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Manage the clients in the client manager.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task RunClientManagerAsync(TimeSpan period, CancellationToken ct)
        {
            var timer = new PeriodicTimer(period);
            _logger.LogDebug("Client manager starting...");
            while (!ct.IsCancellationRequested)
            {
                if (!await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    break;
                }

                _logger.LogDebug("Running client manager connection and garbage collection cycle...");
                var inactive = new List<ConnectionIdentifier>();
                await _lock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    foreach (var client in _clients)
                    {
                        if (client.Value.IsActive)
                        {
                            // If active keep the client connected
                            try
                            {
                                await client.Value.ConnectAsync(true, ct).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex,
                                    "Client manager failed to re-connect session {Name}.",
                                    client.Key);
                                 // ? inactive.Add(client.Key);
                            }
                        }
                        else
                        {
                            // Collect inactive clients
                            inactive.Add(client.Key);
                        }
                    }

                    // Remove inactive clients from client list
                    foreach (var key in inactive)
                    {
                        if (_clients.TryRemove(key, out var client))
                        {
                            _inactive.Enqueue(client);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Client manager encountered unexpected error.");
                }
                finally
                {
                    _lock.Release();
                }

                // Garbage collect inactives
                while (_inactive.TryDequeue(out var client))
                {
                    await client.DisposeAsync().ConfigureAwait(false);
                }

                if (inactive.Count > 0)
                {
                    _logger.LogInformation(
                        "Garbage collected {Sessions} sessions, current number of sessions is {Count}.",
                        inactive.Count, _clients.Count);
                }
            }
        }

        /// <summary>
        /// Validate certificates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnValidate(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            if (e.Accept || e.AcceptAll)
            {
                return;
            }
            var configuration = _configuration.Result;
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
            {
                if (configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
                {
                    _logger.LogWarning("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    e.AcceptAll = true;
                    e.Accept = true;
                }

                // Validate thumbprint
                else if (e.Certificate.RawData != null && !string.IsNullOrWhiteSpace(e.Certificate.Thumbprint) &&
                    _clients.Keys.Any(id => id?.Connection?.Endpoint?.Certificate != null &&
                    e.Certificate.Thumbprint == id.Connection.Endpoint.Certificate))
                {
                    e.Accept = true;

                    _logger.LogInformation("Accepting untrusted peer certificate {Thumbprint}, '{Subject}' " +
                        "since the same thumbprint was specified in the connection!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);

                    // add the certificate to trusted store
                    configuration.SecurityConfiguration.AddTrustedPeer(e.Certificate.RawData);
                    try
                    {
                        var store = configuration.
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
                        _logger.LogWarning(ex, "Failed to add peer certificate {Thumbprint}, '{Subject}' " +
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
        /// Create new client
        /// </summary>
        /// <param name="id"></param>
        /// <param name="metrics"></param>
        /// <returns></returns>
        private OpcUaClient CreateClient(ConnectionIdentifier id, IMetricsContext metrics)
        {
            var logger = _loggerFactory.CreateLogger<OpcUaClient>();
            return new OpcUaClient(_configuration.Result, id, _serializer, logger,
                _options.Value.ClientInactivityTimeout, metrics)
            {
                KeepAliveInterval = _options.Value.KeepAliveInterval,
                SessionTimeout = _options.Value.DefaultSessionTimeout,
                ReconnectPeriod = _options.Value.ReconnectRetryDelay
            };
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
                    _logger.LogDebug(ex, "Exception occurred duringing FindEndpoints at {DiscoveryUrl}.",
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
            var configuration = await _configuration.ConfigureAwait(false);
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
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
        public async Task<T> ExecuteServiceAsync<T>(ConnectionModel connection,
            Func<ISessionHandle, Task<T>> service, CancellationToken ct)
        {
            if (connection.Endpoint == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url))
            {
                throw new ArgumentException("Missing endpoint url", nameof(connection));
            }
            _cts.Token.ThrowIfCancellationRequested();
            var session = await GetOrCreateSessionAsync(connection, null, ct).ConfigureAwait(false);
            if (session is not OpcUaClient client)
            {
                throw new ConnectionException("Failed to execute call, " +
                    $"no connection for {connection?.Endpoint?.Url}");
            }
            return await client.RunAsync(service, ct).ConfigureAwait(false);
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
            var configuration = await _configuration.ConfigureAwait(false);
            var endpointConfiguration = EndpointConfiguration.Create(configuration);
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
        /// Create metrics
        /// </summary>
        private void InitializeMetrics()
        {
            Diagnostics.Meter.CreateObservableUpDownCounter("iiot_edge_publisher_client_count",
                () => new Measurement<int>(_clients.Count + _inactive.Count, _metrics.TagList),
                "Clients", "Number of clients.");
        }

        private const int kMaxDiscoveryAttempts = 3;
        private bool _disposed;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly IOptions<ClientOptions> _options;
        private readonly IJsonSerializer _serializer;
        private readonly ConcurrentQueue<OpcUaClient> _inactive = new();
        private readonly ConcurrentDictionary<ConnectionIdentifier, OpcUaClient> _clients = new();
        private readonly SemaphoreSlim _lock;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processor;
        private readonly Task<ApplicationConfiguration> _configuration;
        private readonly IMetricsContext _metrics;
    }
}
