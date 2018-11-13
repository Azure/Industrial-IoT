// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Newtonsoft.Json.Linq;
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
        IDisposable {

        /// <inheritdoc/>
        public X509Certificate2 Certificate { get; private set; }

        /// <inheritdoc/>
        public Task UpdateClientCertificate(X509Certificate2 certificate) {
            Certificate = certificate ??
                throw new ArgumentNullException(nameof(certificate));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create client host services
        /// </summary>
        /// <param name="logger"></param>
        public ClientServices(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Create discovery config and client certificate
            _config = CreateApplicationConfiguration(TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
            Certificate = CertificateFactory.CreateCertificate(
                _config.SecurityConfiguration.ApplicationCertificate.StoreType,
                _config.SecurityConfiguration.ApplicationCertificate.StorePath, null,
                _config.ApplicationUri, _config.ApplicationName,
                _config.SecurityConfiguration.ApplicationCertificate.SubjectName, null,
                CertificateFactory.defaultKeySize, DateTime.UtcNow - TimeSpan.FromDays(1),
                CertificateFactory.defaultLifeTime, CertificateFactory.defaultHashSize,
                false, null, null);
            _timer = new Timer(_ => OnTimer(), null, kEvictionCheck,
                Timeout.InfiniteTimeSpan);
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (!_cts.IsCancellationRequested) {
                _cts.Cancel();
                _timer.Dispose();
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
                _logger.Verbose($"Discover endpoints at {discoveryUrl}...");
                try {
                    await Retry.Do(_logger, ct, () => DiscoverAsync(discoveryUrl,
                            localeIds, nextServer.Item2, 60000, visitedUris,
                            queue, results),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.Error($"Error at {discoveryUrl} (after {sw.Elapsed}).",
                        () => ex);
                    return new HashSet<DiscoveredEndpointModel>();
                }
                ct.ThrowIfCancellationRequested();
                _logger.Verbose($"Discovery at {discoveryUrl} completed in {sw.Elapsed}.");
            }
            return results;
        }

        /// <inheritdoc/>
        public Task<T> ExecuteServiceAsync<T>(EndpointModel endpoint,
            CredentialModel elevation, Func<Session, Task<T>> service,
            Func<Exception, bool> handler) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }

            var key = new EndpointKey(endpoint);
            while (!_cts.IsCancellationRequested) {
                var client = _clients.GetOrAdd(key, k => new ClientSession(
                    CreateApplicationConfiguration(
                        TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5)),
                    k.Endpoint, () => Certificate, _logger));

                var scheduled = client.TryScheduleServiceCall(service, handler,
                    elevation, out var result);
                if (scheduled) {
                    // Session is owning the task to completion now.
                    return result;
                }
                // Create new session next go around
                _clients.TryRemove(key, out client);
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

            var configuration = EndpointConfiguration.Create(_config);
            configuration.OperationTimeout = timeout;
            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                //
                // Get endpoints from current discovery server
                //
                var endpoints = await client.GetEndpointsAsync(null,
                    client.Endpoint.EndpointUrl, localeIds, null);
                // ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                if (!(endpoints?.Endpoints?.Any() ?? false)) {
                    _logger.Debug($"No endpoints at {discoveryUrl}...");
                    return;
                }
                _logger.Debug($"Found endpoints at {discoveryUrl}...");

                foreach (var ep in endpoints.Endpoints.Where(ep =>
                    ep.Server.ApplicationType != Opc.Ua.ApplicationType.DiscoveryServer)) {
                    result.Add(new DiscoveredEndpointModel {
                        Description = ep,
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
                        var url = CreateDiscoveryUri(server.DiscoveryUrl,
                            discoveryUrl.Port);
                        if (!visitedUris.Contains(url)) {
                            queue.Enqueue(Tuple.Create(discoveryUrl,
                                server.ServerCapabilities.ToList()));
                            visitedUris.Add(url);
                        }
                    }
                }
                catch {
                    // Old lds, just continue...
                    _logger.Debug($"{discoveryUrl} does not support ME extension...");
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
        /// Create application configuration for client
        /// </summary>
        /// <returns></returns>
        internal static ApplicationConfiguration CreateApplicationConfiguration(
            TimeSpan operationTimeout, TimeSpan sessionTimeout) {
            return new ApplicationConfiguration {
                ApplicationName = "Azure IIoT OPC Twin Client Services",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ApplicationUri =
            "urn:" + Utils.GetHostName() + ":OPCFoundation:CoreSampleClient",
                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = "Directory",
                        StorePath =
            "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "UA Core Sample Client"
                    },
                    TrustedPeerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
            "OPC Foundation/CertificateStores/UA Applications",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
            "OPC Foundation/CertificateStores/UA Certificate Authorities",
                    },
                    RejectedCertificateStore = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath =
            "OPC Foundation/CertificateStores/RejectedCertificates",
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas {
                    OperationTimeout = (int)operationTimeout.TotalMilliseconds,
                    MaxStringLength = ushort.MaxValue,
                    MaxByteStringLength = ushort.MaxValue * 16,
                    MaxArrayLength = ushort.MaxValue,
                    MaxMessageSize = ushort.MaxValue * 32
                },
                ClientConfiguration = new ClientConfiguration {
                    DefaultSessionTimeout = (int)sessionTimeout.TotalMilliseconds
                },
                TraceConfiguration = new TraceConfiguration {
                    TraceMasks = 1
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
                _logger.Error("Error managing session clients...", ex);
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
        /// Lookup key for client
        /// </summary>
        private sealed class EndpointKey {

            /// <summary>
            /// Create new key
            /// </summary>
            /// <param name="endpoint"></param>
            public EndpointKey(EndpointModel endpoint) {
                Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            }

            /// <summary>
            /// The endpoint wrapped as key
            /// </summary>
            public EndpointModel Endpoint { get; }

            /// <inheritdoc/>
            public override bool Equals(object obj) {
                return obj is EndpointKey key &&
                    key != null &&
                    Endpoint.Url == key.Endpoint.Url &&
                    (Endpoint.User?.Type ?? CredentialType.None) ==
                        (key.Endpoint.User?.Type ?? CredentialType.None) &&
                    (Endpoint.SecurityMode ?? SecurityMode.Best) ==
                        (key.Endpoint.SecurityMode ?? SecurityMode.Best) &&
                    Endpoint.SecurityPolicy == key.Endpoint.SecurityPolicy &&
                    JToken.DeepEquals(Endpoint.User?.Value,
                        key.Endpoint.User?.Value);
            }

            /// <inheritdoc/>
            public override int GetHashCode() {
                var hashCode = -1971667340;
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.SecurityPolicy);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.Url);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<CredentialType?>.Default.GetHashCode(
                        Endpoint.User?.Type ?? CredentialType.None);
                hashCode = hashCode * -1521134295 +
                   EqualityComparer<SecurityMode?>.Default.GetHashCode(
                       Endpoint.SecurityMode ?? SecurityMode.Best);
                hashCode = hashCode * -1521134295 +
                    JToken.EqualityComparer.GetHashCode(Endpoint.User?.Value);
                return hashCode;
            }
        }

        private static readonly TimeSpan kEvictionCheck = TimeSpan.FromSeconds(10);
        private const int kMaxDiscoveryAttempts = 3;
        private readonly ILogger _logger;
        private readonly ApplicationConfiguration _config;
        private readonly ConcurrentDictionary<EndpointKey, IClientSession> _clients =
            new ConcurrentDictionary<EndpointKey, IClientSession>();
        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly Timer _timer;
    }
}
