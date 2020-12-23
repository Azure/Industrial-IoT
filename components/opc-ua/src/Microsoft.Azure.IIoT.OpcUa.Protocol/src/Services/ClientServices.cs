// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Module;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Opc.Ua.Extensions;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Opc ua stack based service client
    /// </summary>
    public class ClientServices : IClientHost, IEndpointServices, IEndpointDiscovery,
        ICertificateServices<EndpointModel>, IDisposable {

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="clientConfig"></param>
        /// <param name="logger"></param>
        /// <param name="identity"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger, IClientServicesConfig clientConfig,
            IIdentity identity = null, TimeSpan? maxOpTimeout = null) {

            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _clientConfig = clientConfig ??
                throw new ArgumentNullException(nameof(clientConfig));
            _identity = identity;
            _maxOpTimeout = maxOpTimeout;
            // Create discovery config and client certificate
            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// initializes the OPC stack app config
        /// </summary>
        public async Task InitializeAsync() {
            if (_appConfig == null) {
                _appConfig = await _clientConfig.ToApplicationConfigurationAsync(
                    _identity, true, VerifyCertificate);
            }
        }

        /// <inheritdoc/>
        public Task AddTrustedPeerAsync(byte[] certificates) {
            InitializeAsync().ConfigureAwait(false);
            var chain = Utils.ParseCertificateChainBlob(certificates)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0) {
                return Task.FromException(
                    new ArgumentNullException(nameof(certificates)));
            }
            var certificate = chain.First();
            try {
                _logger.Information("Adding Certificate {Thumbprint}, " +
                    "{Subject} to trust list...", certificate.Thumbprint,
                    certificate.Subject);
                _appConfig.SecurityConfiguration.TrustedPeerCertificates
                    .Add(certificate.YieldReturn());
                chain.RemoveAt(0);
                if (chain.Count > 0) {
                    _appConfig.SecurityConfiguration.TrustedIssuerCertificates
                        .Add(chain);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to add Certificate {Thumbprint}, " +
                    "{Subject} to trust list.", certificate.Thumbprint,
                    certificate.Subject);
                return Task.FromException(ex);
            }
            finally {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public Task RemoveTrustedPeerAsync(byte[] certificates) {
            InitializeAsync().ConfigureAwait(false);
            var chain = Utils.ParseCertificateChainBlob(certificates)?
                .Cast<X509Certificate2>()
                .Reverse()
                .ToList();
            if (chain == null || chain.Count == 0) {
                return Task.FromException(
                    new ArgumentNullException(nameof(certificates)));
            }
            var certificate = chain.First();
            try {
                _logger.Information("Removing Certificate {Thumbprint}, " +
                    "{Subject} from trust list...", certificate.Thumbprint,
                    certificate.Subject);
                _appConfig.SecurityConfiguration.TrustedPeerCertificates
                    .Remove(certificate.YieldReturn());

                // Remove only from trusted peers
                return Task.CompletedTask;
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to remove Certificate {Thumbprint}, " +
                    "{Subject} from trust list.", certificate.Thumbprint,
                    certificate.Subject);
                return Task.FromException(ex);
            }
            finally {
                chain?.ForEach(c => c?.Dispose());
            }
        }

        /// <inheritdoc/>
        public ISessionHandle GetSessionHandle(ConnectionModel connection) {
            if (connection?.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            InitializeAsync().ConfigureAwait(false);
            var id = new ConnectionIdentifier(connection);
            _lock.Wait();
            try {
                // Add a persistent session
                if (!_clients.TryGetValue(id, out var client)) {
                    var tuple = ClientSession.CreateWithHandle(_appConfig,
                        id.Connection, _logger, NotifyStateChangeAsync, _maxOpTimeout);
                    _clients.Add(id, tuple.Item1);
                    _logger.Debug("Opened session for endpoint {id} ({endpoint}).",
                        id, connection.Endpoint.Url);
                    return tuple.Item2;
                }
                return client.GetSafeHandle();
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public IDisposable RegisterCallback(ConnectionModel connection,
            Func<EndpointConnectivityState, Task> callback) {
            if (connection?.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            return new CallbackHandle(this, connection, callback);
        }

        /// <inheritdoc/>
        public void Dispose() {
            Try.Op(() => _cts.Cancel());
            Try.Op(() => _timer.Dispose());
            foreach (var client in _clients.Values) {
                Try.Op(client.Dispose);
            }
            _clients.Clear();
            Try.Op(() => _cts.Dispose());
            _lock.Dispose();
            _appConfig = null;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, List<string> locales, CancellationToken ct) {
            await InitializeAsync();
            var results = new HashSet<DiscoveredEndpointModel>();
            var visitedUris = new HashSet<string> {
                CreateDiscoveryUri(discoveryUrl.ToString(), 4840)
            };
            var queue = new Queue<Tuple<Uri, List<string>>>();
            var localeIds = locales != null ? new StringCollection(locales) : null;
            queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
            ct.ThrowIfCancellationRequested();
            while (queue.Any()) {
                var nextServer = queue.Dequeue();
                discoveryUrl = nextServer.Item1;
                var sw = Stopwatch.StartNew();
                _logger.Debug("Try finding endpoints at {discoveryUrl}...", discoveryUrl);
                try {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 20000, visitedUris,
                            queue, results),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.Debug(ex, "Exception occurred duringing FindEndpoints at {discoveryUrl}.",
                        discoveryUrl);
                    _logger.Error("Could not find endpoints at {discoveryUrl} " +
                        "due to {error} (after {elapsed}).",
                        discoveryUrl, ex.Message, sw.Elapsed);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.Debug("Finding endpoints at {discoveryUrl} completed in {elapsed}.",
                    discoveryUrl, sw.Elapsed);
            }
            return results;
        }

        /// <inheritdoc/>
        public async Task<byte[]> GetEndpointCertificateAsync(
            EndpointModel endpoint, CancellationToken ct) {
            if (string.IsNullOrEmpty(endpoint?.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            await InitializeAsync();
            var configuration = EndpointConfiguration.Create(_appConfig);
            configuration.OperationTimeout = 20000;
            var discoveryUrl = new Uri(endpoint.Url);
            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                // Get endpoint descriptions from endpoint url
                var endpoints = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, null, null);

                // Match to provided endpoint info
                var ep = endpoints.Endpoints?.FirstOrDefault(e => e.IsSameAs(endpoint));
                if (ep == null) {
                    _logger.Debug("No endpoints at {discoveryUrl}...", discoveryUrl);
                    throw new ResourceNotFoundException("Endpoint not found");
                }
                _logger.Debug("Found endpoint at {discoveryUrl}...", discoveryUrl);
                return ep.ServerCertificate;
            }
        }

        /// <inheritdoc/>
        public async Task<T> ExecuteServiceAsync<T>(ConnectionModel connection,
            CredentialModel elevation, int priority, Func<Session, Task<T>> service,
            TimeSpan? timeout, CancellationToken ct, Func<Exception, bool> handler) {
            if (connection.Endpoint == null) {
                throw new ArgumentNullException(nameof(connection));
            }
            if (string.IsNullOrEmpty(connection.Endpoint?.Url)) {
                throw new ArgumentNullException(nameof(connection.Endpoint.Url));
            }
            await InitializeAsync();
            var key = new ConnectionIdentifier(connection);
            while (true) {
                _cts.Token.ThrowIfCancellationRequested();
                var client = GetOrCreateSession(key);
                if (!client.Inactive) {
                    var scheduled = client.TryScheduleServiceCall(elevation, priority,
                        service, handler, timeout, ct, out var result);
                    if (scheduled) {
                        // Session is owning the task to completion now.
                        return await result;
                    }
                }
                // Create new session next go around
                EvictIfInactive(key);
            }
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

            var configuration = EndpointConfiguration.Create(_appConfig);
            configuration.OperationTimeout = timeout;
            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                //
                // Get endpoints from current discovery server
                //
                var endpoints = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, localeIds, null);
                if (!(endpoints?.Endpoints?.Any() ?? false)) {
                    _logger.Debug("No endpoints at {discoveryUrl}...", discoveryUrl);
                    return;
                }
                _logger.Debug("Found endpoints at {discoveryUrl}...", discoveryUrl);

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
                        new StringCollection());
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
                    _logger.Debug("{discoveryUrl} does not support ME extension...",
                        discoveryUrl);
                }

                //
                // Call FindServers first to push more unique discovery urls
                // into the discovery queue
                //
                var found = await client.FindServersAsync(null,
                    client.Endpoint.EndpointUrl, localeIds, null);
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
        /// Create session
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private IClientSession GetOrCreateSession(ConnectionIdentifier id) {
            _lock.Wait();
            try {
                if (!_clients.TryGetValue(id, out var session)) {
                    session = ClientSession.Create(
                        _appConfig, id.Connection, _logger,
                        NotifyStateChangeAsync, _maxOpTimeout);
                    _clients.Add(id, session);
                    _logger.Debug("Add new session to session cache.");
                }
                return session;
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Called when timer fired evicting inactive / timedout sessions
        /// </summary>
        /// <returns></returns>
        private void OnTimer() {
            if (_cts.IsCancellationRequested) {
                return;
            }
            try {
                // manage sessions
                foreach (var client in _clients.ToList()) {
                    if (client.Value.Inactive) {
                        EvictIfInactive(client.Key);
                    }
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Error managing session clients...");
            }
            if (_cts.IsCancellationRequested) {
                return;
            }
            try {
                // Re-arm
                _timer.Change((int)kEvictionCheck.TotalMilliseconds, 0);
            }
            catch (ObjectDisposedException) {
                // object disposed
            }
        }

        /// <summary>
        /// Handle inactive
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void EvictIfInactive(ConnectionIdentifier id) {
            _lock.Wait();
            try {
                if (_clients.TryGetValue(id, out var item)) {
                    if (item.Inactive && _clients.Remove(id)) {
                        item.Dispose();
                        _logger.Debug("Evicted inactive session from session cache.");
                    }
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <summary>
        /// Default event handler to validate certificates and handle auto accept.
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="e"></param>
        private void VerifyCertificate(CertificateValidator validator,
            CertificateValidationEventArgs e) {
            if (e.Accept == true) {
                return;
            }
            if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                e.Accept = _appConfig.SecurityConfiguration
                    .AutoAcceptUntrustedCertificates;
                if (e.Accept) {
                    _logger.Warning("Trusting peer certificate {Thumbprint}, {Subject} " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    return;
                }
            }
            _logger.Information("Rejecting peer certificate {Thumbprint}, {Subject} " +
                "because of {Status}.", e.Certificate.Thumbprint,
                e.Certificate.Subject, e.Error.StatusCode);
        }

        /// <summary>
        /// Create discovery url from string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="defaultPort"></param>
        /// <returns></returns>
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
        /// Notify about session/endpoint state changes
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private Task NotifyStateChangeAsync(ConnectionModel connection,
            EndpointConnectivityState state) {
            var id = new ConnectionIdentifier(connection);
            if (_callbacks.TryGetValue(id, out var list)) {
                lock (list) {
                    if (list.Count > 0) {
                        return Task.WhenAll(list.Select(cb => cb.Callback.Invoke(state)))
                            .ContinueWith(_ => Task.CompletedTask);
                    }
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposable callback handle
        /// </summary>
        private class CallbackHandle : IDisposable {

            /// <summary>
            /// Callback
            /// </summary>
            public Func<EndpointConnectivityState, Task> Callback { get; }

            /// <summary>
            /// Create handle
            /// </summary>
            /// <param name="outer"></param>
            /// <param name="connection"></param>
            /// <param name="callback"></param>
            public CallbackHandle(ClientServices outer, ConnectionModel connection,
                Func<EndpointConnectivityState, Task> callback) {
                Callback = callback;
                _outer = outer;
                _connection = new ConnectionIdentifier(connection);
                _outer._callbacks.AddOrUpdate(_connection,
                    new HashSet<CallbackHandle> { this },
                    (id, list) => {
                        lock (list) {
                            list.Add(this);
                            return list;
                        }
                    });
            }

            /// <inheritdoc/>
            public void Dispose() {
                _outer._callbacks.AddOrUpdate(_connection,
                    new HashSet<CallbackHandle>(),
                    (id, list) => {
                        lock (list) {
                            list.Remove(this);
                            return list;
                        }
                    });
            }

            private readonly ClientServices _outer;
            private readonly ConnectionIdentifier _connection;
        }

        private static readonly TimeSpan kEvictionCheck = TimeSpan.FromSeconds(10);
        private const int kMaxDiscoveryAttempts = 3;
        private readonly ILogger _logger;
        private readonly IIdentity _identity;
        private readonly TimeSpan? _maxOpTimeout;
        private readonly IClientServicesConfig _clientConfig;
        private ApplicationConfiguration _appConfig;
        private readonly Dictionary<ConnectionIdentifier, IClientSession> _clients =
            new Dictionary<ConnectionIdentifier, IClientSession>();
        private readonly ConcurrentDictionary<ConnectionIdentifier, HashSet<CallbackHandle>> _callbacks =
            new ConcurrentDictionary<ConnectionIdentifier, HashSet<CallbackHandle>>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
#pragma warning disable IDE0069 // Disposable fields should be disposed
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly Timer _timer;
#pragma warning restore IDE0069 // Disposable fields should be disposed
    }
}
