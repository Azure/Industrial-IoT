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

    /// <summary>
    /// Opc ua stack based service client
    /// </summary>
    public class ClientServices : IClientHost, IEndpointServices, IEndpointDiscovery,
        IDisposable{

        /// <inheritdoc/>
        public X509Certificate2 Certificate {

            get {

                return _OpcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate;
            }
            private set {

                if (value == null) {
                    _logger.Warning("Certificate setter called with empty certificate");
                    return;
                }
                if (!value.HasPrivateKey) {

                    //TODO throw exception
                    //  invalid certificate provided
                    _logger.Warning("Certificate setter called with a certificate without private key");
                    return;
                }
               
                _OpcApplicationConfig.SecurityConfiguration.ApplicationCertificate.Certificate = value;
                _OpcApplicationConfig.CertificateValidator.UpdateCertificate(_OpcApplicationConfig.SecurityConfiguration);

                // copy the certificate into the trusted certificates list
                try {

                    // add new certificate.
                    X509Certificate2 publicKey = new X509Certificate2(value.RawData);
                                     
                    ICertificateStore trustedStore = _OpcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore();

                    try {
                        _logger.Information($"Adding own certificate in the certificate trusted peer store. " +
                            $"StorePath=" +
                            $"{_OpcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                        
                        trustedStore.Delete(publicKey.Thumbprint);
                        trustedStore.Add(publicKey);
                    }
                    catch (Exception ex) {
                        _logger.Warning(ex, $"Can not add own certificate to trusted peer store.");
                    }
                    finally {
                        trustedStore.Close();
                    }
                }
                catch (Exception ex) {
                    _logger.Warning(ex, $"Certificate Setter failed to open the trusted peer store.");
                }

                _OpcApplicationConfig.CertificateValidator.Update(_OpcApplicationConfig.SecurityConfiguration);
            }
        }

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="maxOpTimeout"></param>
        public ClientServices(ILogger logger, TimeSpan? maxOpTimeout = null){

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxOpTimeout = maxOpTimeout;
            _configuration = new ClientServicesConfig(null);

            // Create discovery config and client certificate
            _OpcApplicationConfig = CreateApplicationConfiguration(
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
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
            TimeSpan? maxOpTimeout = null){

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? 
                throw new ArgumentNullException(nameof(configuration)); ;
            _maxOpTimeout = maxOpTimeout;

            // Create discovery config and client certificate
            _OpcApplicationConfig = CreateApplicationConfiguration(
                TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));

            InitApplicationSecurityAsync().Wait();

            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck, Timeout.InfiniteTimeSpan);
        }


        /// <summary>
        /// Initialize the OPC UA Application's security configuration
        /// </summary>
        /// <returns></returns>
        private async Task InitApplicationSecurityAsync(){


            _OpcApplicationConfig.SecurityConfiguration.AddAppCertToTrustedStore = true;

            X509Certificate2 ownCertificate = await _OpcApplicationConfig.SecurityConfiguration.
                ApplicationCertificate.Find(true).ConfigureAwait(false);

            if (ownCertificate == null) {

                _logger.Information($"No existing Application own certificate found." +
                    $" Create a self-signed Application certificate valid from yesterday " +
                    $"for {CertificateFactory.defaultLifeTime} months,");
                _logger.Information($"with a {CertificateFactory.defaultKeySize} bit " +
                    $"key and {CertificateFactory.defaultHashSize} bit hash.");

                ownCertificate = CertificateFactory.CreateCertificate(
                    _OpcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StoreType,
                    _OpcApplicationConfig.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    _OpcApplicationConfig.ApplicationUri,
                    _OpcApplicationConfig.ApplicationName,
                    _OpcApplicationConfig.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null,
                    CertificateFactory.defaultKeySize,
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime,
                    CertificateFactory.defaultHashSize,
                    false,
                    null,
                    null);

                _logger.Information($"Application certificate with thumbprint '{ownCertificate.Thumbprint}' created.");
            }
            else {

                _logger.Information($"Application certificate with thumbprint " +
                    $"'{ownCertificate.Thumbprint}' found in the application certificate store.");
            }

            // Set the Certificate as the newly created certificate
            Certificate = ownCertificate;

            // updatecertificates validator
            _OpcApplicationConfig.CertificateValidator.CertificateValidation += 
                new Opc.Ua.CertificateValidationEventHandler(CertificateValidator_CertificateValidation);

            await _OpcApplicationConfig.CertificateValidator.Update(_OpcApplicationConfig).ConfigureAwait(false);

            if (_OpcApplicationConfig.SecurityConfiguration.AutoAcceptUntrustedCertificates) {
                _logger.Warning("WARNING: Automatically accepting certificates. This is a security risk.");
            }
        }

        /// <summary>
        /// Event handler to validate certificates.
        /// </summary>
        private void CertificateValidator_CertificateValidation(
            Opc.Ua.CertificateValidator validator, 
            Opc.Ua.CertificateValidationEventArgs e){

            if (e.Error.StatusCode == Opc.Ua.StatusCodes.BadCertificateUntrusted){

                e.Accept = _configuration.AutoAcceptUntrustedCertificates;

                if (_configuration.AutoAcceptUntrustedCertificates) {

                    _logger.Information($"Certificate '{e.Certificate.Subject}' will be trusted, " +
                        $"because AutoAcceptUntrustedCertificates is on.");

                    try {

                        ICertificateStore trustedStore = 
                            _OpcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.OpenStore();
                         try {
                            _logger.Information($"Adding server certificate to trusted peer store. " +
                                $"StorePath=" +
                                $"{_OpcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                            trustedStore.Delete(e.Certificate.Thumbprint);
                            
                            trustedStore.Add(e.Certificate);
                            validator.Update(_OpcApplicationConfig.SecurityConfiguration);
                        }
                        catch (Exception ex) {
                            _logger.Warning(ex, $"Can not add server certificate to " +
                                $"trusted peer store.");
                        }
                        finally {
                            trustedStore.Close();
                        }
                    }
                    catch(Exception ex) {
                        _logger.Warning(ex, $"Can not open certificate to trusted certificate peer store.");
                    }
                }                                
            }
            else{
                _logger.Information($"Not trusting OPC application with the certificate subject '{e.Certificate.Subject}'.");
                _logger.Information("If you want to trust this certificate, please copy it from the directory:");
                _logger.Information($"{_OpcApplicationConfig.SecurityConfiguration.RejectedCertificateStore.StorePath}");
                _logger.Information("to the directory:");
                _logger.Information($"{_OpcApplicationConfig.SecurityConfiguration.TrustedPeerCertificates.StorePath}");
                _logger.Information($"Rejecting certificate for now.");
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

            var configuration = EndpointConfiguration.Create(_OpcApplicationConfig);
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
                _OpcApplicationConfig, k.Endpoint, () => Certificate, _logger, NotifyStateChangeAsync, persistent,
                    _maxOpTimeout));
        }

        /// <summary>
        /// Create application configuration for client
        /// </summary>
        /// <returns></returns>
        internal ApplicationConfiguration CreateApplicationConfiguration(
            TimeSpan operationTimeout, TimeSpan sessionTimeout) {

            return new ApplicationConfiguration {
                ApplicationName = "Azure IIoT OPC Twin Client Services",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ApplicationUri ="urn:" + Utils.GetHostName() + ":Azure:IIoTOpcTwin",
                CertificateValidator = new Opc.Ua.CertificateValidator(),
                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = "Directory",
                        StorePath = _configuration.ApplicationCertificateFolder ??
                                    (_configuration.PkiRootFolder != null ? 
                                        _configuration.PkiRootFolder + "/pki/own" :
                                        "pki/own"),
                        SubjectName = "Azure IIoT OPC Twin"
                    },
                    TrustedPeerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath = _configuration.TrustedPeerCertificatesFolder ??
                                    (_configuration.PkiRootFolder != null ?
                                        _configuration.PkiRootFolder + "/pki/trusted" :
                                        "pki/trusted"),
                    },
                    TrustedIssuerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath = _configuration.TrustedIssuerCertificatesFolder ??
                                    (_configuration.PkiRootFolder != null ?
                                        _configuration.PkiRootFolder + "/pki/issuer" :
                                        "pki/issuer"),
                    },
                    RejectedCertificateStore = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =_configuration.RejectedCertificatesFolder ??
                                    (_configuration.PkiRootFolder != null ?
                                        _configuration.PkiRootFolder + "/pki/rejected" :
                                        "pki/rejected"),
                    
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = _configuration.AutoAcceptUntrustedCertificates,
                    RejectSHA1SignedCertificates = false
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
        private readonly ApplicationConfiguration _OpcApplicationConfig;
        private readonly ConcurrentDictionary<EndpointIdentifier, IClientSession> _clients =
            new ConcurrentDictionary<EndpointIdentifier, IClientSession>();
        private readonly ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>> _callbacks =
            new ConcurrentDictionary<EndpointIdentifier, Func<EndpointConnectivityState, Task>>();
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly Timer _timer;
    }
}
