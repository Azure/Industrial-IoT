// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Serilog;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Opc ua stack based service client
    /// </summary>
    public class ClientServices : IClientHost, IEndpointServices, IEndpointDiscovery,
        IDisposable {

        /// <inheritdoc/>
        public X509Certificate2 Certificate {
            get {
                return _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate;
            }
            private set {
                SetOwnCertificate(value);
            }
        }

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger, TimeSpan? maxOpTimeout = null) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxOpTimeout = maxOpTimeout;
            _configuration = new ClientServicesConfig(null);

            // Create discovery config and client certificate
            _opcApplicationConfig = CreateApplicationConfiguration(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            InitApplicationSecurityAsync().Wait();

            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger,
            IClientServicesConfig configuration,
            TimeSpan? maxOpTimeout = null) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration)); ;
            _maxOpTimeout = maxOpTimeout;

            // Create discovery config and client certificate
            _opcApplicationConfig = CreateApplicationConfiguration(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            InitApplicationSecurityAsync().Wait();

            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Initialize the OPC UA Application's security configuration
        /// </summary>
        /// <returns></returns>
        private async Task InitApplicationSecurityAsync() {

            // update certificates validator
            _opcApplicationConfig.CertificateValidator.CertificateValidation +=
                new Opc.Ua.CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            await _opcApplicationConfig.CertificateValidator.Update(_opcApplicationConfig).ConfigureAwait(false);

            // lookup for an existing certificate in the configured store
            var ownCertificate = await _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Find(true).ConfigureAwait(false);

            if (ownCertificate == null) {

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    _configuration.AppCertStoreType == CertificateStoreType.Directory) {

                    var ownCertificateIdentifier = new CertificateIdentifier {
                        StoreType = _configuration.AppCertStoreType,
                        StorePath = _configuration.OwnCertPath,
                        SubjectName = _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.SubjectName
                    };
                    ownCertificate = await ownCertificateIdentifier.Find(true).ConfigureAwait(false);
                    if ((ownCertificate != null) && !ownCertificate.Verify()) {
                        try {
                            _logger.Warning("Found malformed own certificate {Thumbprint}, {Subject} in the store - deleting it", 
                                ownCertificate.Thumbprint, ownCertificate.Subject);
                            var ownCertificateStore = ownCertificateIdentifier.OpenStore();
                            await ownCertificateStore.Delete(ownCertificate.Thumbprint);
                            ownCertificateStore.Close();
                        }
                        catch (Exception ex) {
                            _logger.Information(ex, "Failed to remove malformed own certificate");
                        }
                        finally {
                            ownCertificate = null;
                        }
                    }
                }
            }

            if (ownCertificate == null) {

                _logger.Information("Application own certificate not found. About to create a new self-signed certificate with default settings");

                ownCertificate = CertificateFactory.CreateCertificate(
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StoreType,
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    _opcApplicationConfig.ApplicationUri,
                    _opcApplicationConfig.ApplicationName,
                    _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null);
                _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate = ownCertificate;
                _logger.Information("New application certificate with {Thumbprint}, {Subject} created",
                    ownCertificate.Thumbprint, ownCertificate.SubjectName.Name);
            }
            else {
                _logger.Information("Application certificate with {Thumbprint}, {Subject} found in the certificate store", 
                    ownCertificate.Thumbprint, ownCertificate.SubjectName.Name);
            }

            // Set the Certificate as the newly created certificate
            Certificate = ownCertificate;

            if (_opcApplicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                _logger.Warning("WARNING: Automatically accepting certificates. This is a security risk.");
            }
        }

        /// <summary>
        /// set a new application instance certificate
        /// </summary>
        /// <param name="newCertificate"></param>
        private void SetOwnCertificate(X509Certificate2 newCertificate) {

            if (newCertificate == null || !newCertificate.HasPrivateKey /*|| !newCertificate.Verify()*/) {
                _logger.Warning("Set certificate with empty or invalid argument");
                throw new ArgumentException("Empty or invalid certificate");
            }

            _logger.Information("About to set new application certificate {Thumbprint}, {Subject}",
                newCertificate.Thumbprint, newCertificate.SubjectName.Name);

            //  attempt to replace the old certificate from the various trust lists
            var oldCertificate = _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate;

            // copy the certificate, public key only, into the trusted certificates list
            try {
                var publicKey = new X509Certificate2(newCertificate.RawData);
                var trustedStore = _opcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                try {
                    _logger.Information("Adding own certificate in the certificate trusted peer store");
                    if (oldCertificate != null) {
                        trustedStore.Delete(oldCertificate.Thumbprint);
                    }
                    trustedStore.Add(publicKey);
                }
                catch (Exception ex) {
                    _logger.Warning(ex, "Can not add own certificate to trusted peer store");
                }
                finally {
                    trustedStore.Close();
                }
            }
            catch (Exception ex) {
                _logger.Warning(ex, "Can not add own certificate to trusted peer store, store open failure");
            }

            if (!_opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate.EqualsSafe(newCertificate)) {

                // add the certificate to the right store 
                try {
                    var appStore = _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.OpenStore();
                    try {
                        _logger.Information("Adding own certificate in the certificate app store");
                        if (oldCertificate != null) {
                            appStore.Delete(oldCertificate.Thumbprint);
                        }
                        appStore.Add(newCertificate);
                    }
                    catch (Exception ex) {
                        _logger.Warning(ex, "Can not add own certificate in the certificate app store");
                    }
                    finally {
                        appStore.Close();
                    }
                }
                catch (Exception ex) {
                    _logger.Warning(ex, "Can not add own certificate in the certificate app store, store open failure");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                _configuration.AppCertStoreType == CertificateStoreType.Directory) {

                var applicationCertificate = new CertificateIdentifier{
                        StoreType = _configuration.AppCertStoreType,
                        StorePath = _configuration.OwnCertPath,
                        SubjectName = newCertificate.SubjectName.Name
                    };

                try {
                    var applicationCertStore = applicationCertificate.OpenStore();
                    try {
                        _logger.Information("Adding own certificate in the certificate app store");
                        if (oldCertificate != null) {
                            applicationCertStore.Delete(oldCertificate.Thumbprint);
                        }
                        applicationCertStore.Add(newCertificate);
                    }
                    catch (Exception ex) {
                        _logger.Warning(ex, "Can not add own certificate to trusted app store");
                    }
                    finally {
                        applicationCertStore.Close();
                    }
                }
                catch (Exception ex) {
                    _logger.Warning(ex, "Can not add own certificate to trusted app store, store open failure");
                }
            }

            _opcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate = newCertificate;
            _opcApplicationConfig.CertificateValidator.UpdateCertificate(_opcApplicationConfig.SecurityConfiguration);
        }

        /// <summary>
        /// Event handler to validate certificates.
        /// </summary>
        private void CertificateValidator_CertificateValidation(
            Opc.Ua.CertificateValidator validator, 
            Opc.Ua.CertificateValidationEventArgs e) {

                if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted){
                e.Accept = _opcApplicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates;
                if (_opcApplicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                    _logger.Information("Trusting Peer Certificate {Thumbprint}, {Subject} due to AutoAccept(UntrustedCertificates) set to true", 
                        e.Certificate.Thumbprint, e.Certificate.Subject);
                    try {

                        var publicKey = new X509Certificate2(e.Certificate.RawData);
                        var trustedStore = _opcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore(); 

                        try {
                            trustedStore.Delete(publicKey.Thumbprint);
                            _logger.Information("Adding peer Certificate {Thumbprint}, {Subject} to trusted store", 
                                publicKey.Thumbprint, publicKey.Subject);
                            trustedStore.Add(publicKey);                          
                        }
                        catch (Exception ex) {
                            _logger.Warning(ex, "Failed to add peer Certificate {Thumbprint}, {Subject} to trusted store",
                                publicKey.Thumbprint, publicKey.Subject);
                        }
                        finally {
                            trustedStore.Close();
                            validator.Update(_opcApplicationConfig.SecurityConfiguration);
                        }
                    }
                    catch(Exception ex) {
                        _logger.Warning(ex, "Can not open certificate to trusted certificate peer store");
                    }
                }
            }
            else{
                _logger.Information("Rejecting Untrusted Peer Certificate {Thumbprint}, {Subject}",
                        e.Certificate.Thumbprint, e.Certificate.Subject);
            }
        }

        /// <inheritdoc/>
        public Task UpdateClientCertificate(X509Certificate2 certificate) {
            Certificate = certificate ??
                throw new ArgumentNullException(nameof(certificate));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Register(EndpointModel endpoint,
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
            // Create persistent session
            GetOrCreateSession(id, true);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Unregister(EndpointModel endpoint) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }

            var id = new EndpointIdentifier(endpoint);
            _callbacks.TryRemove(id, out _);
            // Remove persistent session
            if (_clients.TryRemove(id, out var client)) {
                return Try.Async(client.CloseAsync);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (!_cts.IsCancellationRequested) {
                _cts.Cancel();
                _timer.Dispose();

                foreach (var client in _clients.Values) {
                    Try.Op(client.Dispose);
                }
                _clients.Clear();
            }
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
                _logger.Verbose("Discover endpoints at {discoveryUrl}...", discoveryUrl);
                try {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 20000, visitedUris,
                            queue, results),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.Error(ex, "Error at {discoveryUrl} (after {elapsed}).",
                        discoveryUrl, sw.Elapsed);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.Verbose("Discovery at {discoveryUrl} completed in {elapsed}.",
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
                var client = GetOrCreateSession(key, false);
                if (!client.Inactive) {
                    var scheduled = client.TryScheduleServiceCall(elevation, priority,
                        service, handler, timeout, ct, out var result);
                    if (scheduled) {
                        // Session is owning the task to completion now.
                        return result;
                    }
                }
                // Create new session next go around
                _clients.TryRemove(key, out client);
                client.Dispose();
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
                    _logger.Debug("{discoveryUrl} does not support ME extension...", discoveryUrl);
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
        /// <param name="persistent"></param>
        /// <returns></returns>
        internal IClientSession GetOrCreateSession(EndpointIdentifier id, bool persistent) {
            return _clients.GetOrAdd(id, k => new ClientSession(
                _opcApplicationConfig, k.Endpoint, () => Certificate, _logger, NotifyStateChangeAsync, persistent,
                    _maxOpTimeout));
        }

        /// <summary>
        /// Create application configuration for client
        /// </summary>
        /// <returns></returns>
        internal ApplicationConfiguration CreateApplicationConfiguration(
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
                CertificateValidator = new Opc.Ua.CertificateValidator(),
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
                        StorePath =_configuration.RejectedCertPath
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

        /// <summary>
        /// Called when timer fired evicting inactive / timedout sessions
        /// </summary>
        /// <returns></returns>
        private void OnTimer() {
            try {
                // manage sessions
                foreach (var client in _clients.ToList()) {
                    if (client.Value.Inactive) {
                        if (_clients.TryRemove(client.Key, out var item)) {
                            item.Dispose();
                        }
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

        private static readonly TimeSpan kEvictionCheck = TimeSpan.FromSeconds(10);
        private const int kMaxDiscoveryAttempts = 3;
        private readonly ILogger _logger;
        private readonly TimeSpan? _maxOpTimeout;
        private readonly IClientServicesConfig _configuration;
        private readonly ApplicationConfiguration _opcApplicationConfig;
        private readonly ConcurrentDictionary<EndpointIdentifier, IClientSession> _clients =
            new ConcurrentDictionary<EndpointIdentifier, IClientSession>();
        private readonly ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>> _callbacks =
            new ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>>();
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly Timer _timer;
    }
}
