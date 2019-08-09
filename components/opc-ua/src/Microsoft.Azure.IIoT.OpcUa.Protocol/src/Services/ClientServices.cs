// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using Serilog;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Opc ua stack based service client
    /// </summary>
    public class ClientServices : IClientHost, IEndpointServices, IEndpointDiscovery,
        IDisposable {

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger, TimeSpan? maxOpTimeout = null) :
            this (logger, new ClientServicesConfig(), maxOpTimeout) {
        }

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger, IClientServicesConfig configuration,
            TimeSpan? maxOpTimeout = null) {

            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
            _maxOpTimeout = maxOpTimeout;

            // Create discovery config and client certificate
            _opcApplicationConfig = CreateApplicationConfiguration(
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            InitApplicationSecurityAsync().Wait();

            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck, Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public Task AddTrustedPeerAsync(byte[] certificates) {
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
                _opcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates
                    .Add(certificate.YieldReturn());
                chain.RemoveAt(0);
                if (chain.Count > 0) {
                    _opcApplicationConfig.SecurityConfiguration.TrustedIssuerCertificates
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
                _opcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates
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
        public async Task RegisterAsync(EndpointModel endpoint,
            Func<EndpointConnectivityState, Task> callback) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }
            var id = new EndpointIdentifier(endpoint);

            if (!_callbacks.TryAdd(id, callback)) {
                _callbacks.AddOrUpdate(id, callback, (k, v) => callback);
            }

            await _lock.WaitAsync();
            try {
                // Add a persistent session
                if (!_clients.TryGetValue(id, out var _)) {
                    _clients.Add(id, new ClientSession(
                        _opcApplicationConfig, id.Endpoint.Clone(), _logger,
                        NotifyStateChangeAsync, true, _maxOpTimeout));
                    _logger.Debug("Open session for endpoint {id} ({endpoint}).",
                        id, endpoint.Url);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UnregisterAsync(EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            var id = new EndpointIdentifier(endpoint);
            _callbacks.TryRemove(id, out _);

            await _lock.WaitAsync();
            try {
                // Remove any session
                if (_clients.TryGetValue(id, out var client)) {
                    await Try.Async(client.CloseAsync);
                    Try.Op(client.Dispose);

                    _clients.Remove(id);
                    _logger.Debug("Endpoint {id} ({endpoint}) closed.",
                        id, endpoint.Url);
                }
                else {
                    _logger.Debug(
                        "Session for endpoint {id} ({endpoint}) not found.",
                        endpoint.Url, id);
                }
            }
            finally {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (!_cts.IsCancellationRequested) {
                Try.Op(() => _cts.Cancel());
                Try.Op(() => _timer.Dispose());

                foreach (var client in _clients.Values) {
                    Try.Op(client.Dispose);
                }
                _clients.Clear();
            }
            Try.Op(() => _cts.Dispose());
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DiscoveredEndpointModel>> FindEndpointsAsync(
            Uri discoveryUrl, List<string> locales, CancellationToken ct) {

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
        public Task<T> ExecuteServiceAsync<T>(EndpointModel endpoint,
            CredentialModel elevation, int priority, Func<Session, Task<T>> service,
            TimeSpan? timeout, CancellationToken ct, Func<Exception, bool> handler) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var key = new EndpointIdentifier(endpoint);
            while (!_cts.IsCancellationRequested) {
                var client = GetOrCreateSession(key);
                if (!client.Inactive) {
                    var scheduled = client.TryScheduleServiceCall(elevation, priority,
                        service, handler, timeout, ct, out var result);
                    if (scheduled) {
                        // Session is owning the task to completion now.
                        return result;
                    }
                }
                // Create new session next go around
                EvictIfInactive(key);
            }
            return Task.FromCanceled<T>(_cts.Token);
        }

        /// <summary>
        /// Perform a single discovery using a discovery url
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="localeIds"></param>
        /// <param name="caps"></param>
        /// <param name="timeout"></param>
        /// <param name="result"></param>
        /// <param name="visitedUris"></param>
        /// <param name="queue"></param>
        private async Task DiscoverAsync(Uri discoveryUrl, StringCollection localeIds,
            IEnumerable<string> caps, int timeout, HashSet<string> visitedUris,
            Queue<Tuple<Uri, List<string>>> queue, HashSet<DiscoveredEndpointModel> result) {

            var configuration = EndpointConfiguration.Create(_opcApplicationConfig);
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
        private IClientSession GetOrCreateSession(EndpointIdentifier id) {
            _lock.Wait();
            try {
                if (!_clients.TryGetValue(id, out var session)) {
                    session = new ClientSession(
                        _opcApplicationConfig, id.Endpoint.Clone(), _logger,
                        NotifyStateChangeAsync, false, _maxOpTimeout);
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
        private void EvictIfInactive(EndpointIdentifier id) {
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
        /// Create application configuration for client
        /// </summary>
        /// <returns></returns>
        private ApplicationConfiguration CreateApplicationConfiguration(
            TimeSpan operationTimeout, TimeSpan sessionTimeout) {

            // mitigation for bug in .NET Core 2.1
            var effectiveAppCertStoreType = _configuration.AppCertStoreType;
            var effectiveOwnCertPath = _configuration.OwnCertPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                effectiveAppCertStoreType = CertificateStoreType.X509Store;
                effectiveOwnCertPath = _configuration.OwnCertX509StorePathDefault;
            }

            return new ApplicationConfiguration {
                ApplicationName = "Azure IIoT OPC Twin Client Services",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ApplicationUri = "urn:" + Utils.GetHostName() + ":Azure:IIoTOpcTwin",
                CertificateValidator = new CertificateValidator(),
                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = effectiveAppCertStoreType,
                        StorePath = effectiveOwnCertPath,
                        SubjectName = "Azure IIoT OPC Twin"
                    },
                    TrustedPeerCertificates = new CertificateTrustList {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = _configuration.TrustedCertPath
                    },
                    TrustedIssuerCertificates = new CertificateTrustList {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = _configuration.IssuerCertPath
                    },
                    RejectedCertificateStore = new CertificateTrustList {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = _configuration.RejectedCertPath
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = _configuration.AutoAccept,
                    RejectSHA1SignedCertificates = false,
                    AddAppCertToTrustedStore = false,
                    MinimumCertificateKeySize = 1024
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas {
                    OperationTimeout = (int)operationTimeout.TotalMilliseconds,
                    MaxStringLength = ushort.MaxValue * 32,
                    MaxByteStringLength = ushort.MaxValue * 32,
                    MaxArrayLength = ushort.MaxValue * 32,
                    MaxMessageSize = ushort.MaxValue * 64
                },
                ClientConfiguration = new ClientConfiguration {
                    DefaultSessionTimeout = (int)sessionTimeout.TotalMilliseconds
                }
            };
        }

        /// <summary>
        /// Initialize the OPC UA Application's security configuration
        /// </summary>
        /// <returns></returns>
        private async Task InitApplicationSecurityAsync() {

            // update certificates validator
            _opcApplicationConfig.CertificateValidator.CertificateValidation +=
                new CertificateValidationEventHandler(VerifyCertificate);
            await _opcApplicationConfig.CertificateValidator
                .Update(_opcApplicationConfig).ConfigureAwait(false);

            // lookup for an existing certificate in the configured store
            var ownCertificate = await _opcApplicationConfig.SecurityConfiguration
                .ApplicationCertificate.Find(true).ConfigureAwait(false);
            if (ownCertificate == null) {
                //
                // Work around windows issue and lookup application certificate also on
                // directory if configured.  This is needed for container persistence.
                //
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    _configuration.AppCertStoreType == CertificateStoreType.Directory) {

                    // Use x509 store instead of directory for private cert.
                    var ownCertificateIdentifier = new CertificateIdentifier {
                        StoreType = _configuration.AppCertStoreType,
                        StorePath = _configuration.OwnCertPath,
                        SubjectName = _opcApplicationConfig.SecurityConfiguration
                            .ApplicationCertificate.SubjectName
                    };
                    ownCertificate = await ownCertificateIdentifier.Find(true)
                        .ConfigureAwait(false);
                    if ((ownCertificate != null) && !ownCertificate.Verify()) {
                        try {
                            _logger.Warning("Found malformed own certificate {Thumbprint}, " +
                                "{Subject} in the store - deleting it...",
                                ownCertificate.Thumbprint, ownCertificate.Subject);
                            ownCertificateIdentifier.RemoveFromStore(ownCertificate);
                        }
                        catch (Exception ex) {
                            _logger.Information(ex,
                                "Failed to remove malformed own certificate");
                        }
                        ownCertificate = null;
                    }
                }
            }

            if (ownCertificate == null) {

                _logger.Information("Application own certificate not found. " +
                    "Creating a new self-signed certificate with default settings...");
                ownCertificate = CertificateFactory.CreateCertificate(
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StoreType,
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    _opcApplicationConfig.ApplicationUri, _opcApplicationConfig.ApplicationName,
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null, CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime, CertificateFactory.defaultHashSize,
                    false, null, null);

                _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate =
                    ownCertificate;
                _logger.Information(
                    "New application certificate with {Thumbprint}, {Subject} created",
                    ownCertificate.Thumbprint, ownCertificate.SubjectName.Name);
            }
            else {
                _logger.Information("Application certificate with {Thumbprint}, {Subject} " +
                    "found in the certificate store",
                    ownCertificate.Thumbprint, ownCertificate.SubjectName.Name);
            }
            // Set the Certificate as the newly created certificate
            await SetOwnCertificateAsync(ownCertificate);
            if (_opcApplicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                _logger.Warning(
                    "WARNING: Automatically accepting certificates. This is a security risk.");
            }
        }

        /// <summary>
        /// set a new application instance certificate
        /// </summary>
        /// <param name="newCertificate"></param>
        private async Task SetOwnCertificateAsync(X509Certificate2 newCertificate) {

            if (newCertificate == null || !newCertificate.HasPrivateKey) {
                throw new ArgumentException("Empty or invalid certificate");
            }

            //  attempt to replace the old certificate from the various trust lists
            var oldCertificate = _opcApplicationConfig.SecurityConfiguration
                .ApplicationCertificate.Certificate;
            if (oldCertificate?.Thumbprint != newCertificate.Thumbprint) {
                return;
            }

            _logger.Information(
                "Setting new application certificate {Thumbprint}, {Subject}...",
                newCertificate.Thumbprint, newCertificate.SubjectName.Name);

            // copy the certificate, public key only into the trusted certificates list
            using (var publicKey = new X509Certificate2(newCertificate.RawData)) {
                var trustList =
                    _opcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates;
                if (oldCertificate != null) {
                    trustList.Remove(oldCertificate.YieldReturn());
                }
                trustList.Add(newCertificate.YieldReturn());
            }

            // add the certificate to the own store
            try {
                var applicationCertificate = _opcApplicationConfig.SecurityConfiguration
                    .ApplicationCertificate;
                _logger.Information(
                    "Adding own certificate to configured certificate store");
                // Remove old and add new
                if (oldCertificate != null) {
                    applicationCertificate.RemoveFromStore(oldCertificate);
                }
                applicationCertificate.AddToStore(newCertificate, true);
            }
            catch (Exception ex) {
                _logger.Warning(ex,
                    "Failed adding own certificate into configured certificate store.");
            }

            //
            // Work around windows issue and persist application certificate also on
            // directory if configured.  This is needed for container persistence.
            //
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                _configuration.AppCertStoreType == CertificateStoreType.Directory) {
                var applicationCertificate = new CertificateIdentifier {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = _configuration.OwnCertPath,
                    SubjectName = newCertificate.SubjectName.Name
                };
                try {
                    _logger.Information(
                        "Persisting own certificate into directory certificate store...");
                    // Remove old and add new
                    if (oldCertificate != null) {
                        applicationCertificate.RemoveFromStore(oldCertificate);
                    }
                    applicationCertificate.AddToStore(newCertificate, true);
                }
                catch (Exception ex) {
                    _logger.Warning(ex,
                        "Failed adding own certificate to directory certificate store.");
                }
            }

            _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate
                .Certificate = newCertificate;
            await _opcApplicationConfig.CertificateValidator.UpdateCertificate(
                _opcApplicationConfig.SecurityConfiguration);
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
                e.Accept = _opcApplicationConfig.SecurityConfiguration
                    .AutoAcceptUntrustedCertificates;
                if (e.Accept) {
                    _logger.Warning("Trusting Peer Certificate {Thumbprint}, {Subject} " +
                        "due to AutoAccept(UntrustedCertificates) set!",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                }
                return;
            }
            _logger.Information("Rejecting peer Certificate {Thumbprint}, {Subject} " +
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
        /// <param name="ep"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private Task NotifyStateChangeAsync(EndpointModel ep, EndpointConnectivityState state) {
            var id = new EndpointIdentifier(ep);
            if (_callbacks.TryGetValue(id, out var cb)) {
                return cb(state);
            }
            return Task.CompletedTask;
        }


        private static readonly TimeSpan kEvictionCheck = TimeSpan.FromSeconds(10);
        private const int kMaxDiscoveryAttempts = 3;
        private readonly ILogger _logger;
        private readonly TimeSpan? _maxOpTimeout;
        private readonly IClientServicesConfig _configuration;
        private readonly ApplicationConfiguration _opcApplicationConfig;
        private readonly Dictionary<EndpointIdentifier, IClientSession> _clients =
            new Dictionary<EndpointIdentifier, IClientSession>();
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>> _callbacks =
            new ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>>();
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly Timer _timer;
    }
}
