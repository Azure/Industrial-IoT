// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Services {
    using Microsoft.Azure.IIoT.OpcUa.Exceptions;
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Client;
    using System;
    using System.Linq;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using System.Diagnostics.Contracts;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Diagnostics;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Opc ua stack based client services
    /// </summary>
    public class ClientServices : IClientHost, IEndpointServices,
        IEndpointDiscovery {

        /// <summary>
        /// Client public certificate
        /// </summary>
        public X509Certificate2 Certificate => _clientCert;

        /// <summary>
        /// Update client certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public Task UpdateClientCertificate(X509Certificate2 certificate) {
            _clientCert = certificate ??
                throw new ArgumentNullException(nameof(certificate));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create stack
        /// </summary>
        /// <param name="logger"></param>
        public ClientServices(ILogger logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new ConcurrentDictionary<SessionKey, ImmutableQueue<ServerSession>>();

            // Create default config and client certificate
            _config = CreateApplicationConfiguration();
            _clientCert = CertificateFactory.CreateCertificate(
                _config.SecurityConfiguration.ApplicationCertificate.StoreType,
                _config.SecurityConfiguration.ApplicationCertificate.StorePath, null,
                _config.ApplicationUri, _config.ApplicationName,
                _config.SecurityConfiguration.ApplicationCertificate.SubjectName, null,
                CertificateFactory.defaultKeySize, DateTime.UtcNow - TimeSpan.FromDays(1),
                CertificateFactory.defaultLifeTime, CertificateFactory.defaultHashSize,
                false, null, null);
        }

        /// <summary>
        /// Execute opc ua service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task<T> ExecuteServiceAsync<T>(EndpointModel endpoint,
            Func<Session, Task<T>> service, Func<Exception, bool> handler) {
            if (endpoint == null) {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (string.IsNullOrEmpty(endpoint.Url)) {
                throw new ArgumentNullException(nameof(endpoint.Url));
            }
            var retry = true;
            while (true) {
                var key = new SessionKey(endpoint);

                // Get session
                var session = GetSession(key);
                if (session != null) {
                    // If this session is bad - try again...
                    retry = true;
                }
                else {
                    // Create new session
                    try {
                        session = await CreateSessionAsync(key).ConfigureAwait(false);
                    }
                    catch(ServiceResultException sre) {
                        _logger.Debug("Failed create session", () => new { sre, endpoint });
                        // Throw immediately - this cannot be retried...
                        throw ServiceResultToTypedException(sre);
                    }
                }
                try {
                    // Run service on session and convert
                    var result = await service(session.Session).ConfigureAwait(false);
                    ReturnSession(session);
                    return result;
                }
                catch (Exception e) {
                    // Process exception - first convert sre into non opc exception
                    if (e is ServiceResultException sre) {
                        e = ServiceResultToTypedException(sre);
                    }
                    // See which ones we can retry, and which ones we cannot
                    switch (e) {
                        case ServerBusyException sb:
                            // TODO: Throttle?
                        case TimeoutException te:
                            // TODO: should we retry this one?
                        case ConnectionException cn:
                        case ProtocolException pe:
                        case CommunicationException ce:
                            if (!handler(e) || !retry) {
                                _logger.Error("Comm error during service call", () => e);
                                throw e;
                            }
                            retry = false;
                            _logger.Warn("Try with new session...", () => e);
                            // Session is bad - attempt to close and open a new one...
                            var cleanup = Task.Run(() => session.Session?.Close());
                            break;
                        default:
                            // App error - session should be good - test
                            if (session.Session?.Connected ?? false) {
                                // and return back to pool
                                ReturnSession(session);
                            }
                            _logger.Error("App error during service call", () => e);
                            throw e;
                    }
                }
            }
        }

        /// <summary>
        /// Connect timeout
        /// </summary>
        protected int Timeout { get; set; } = 60000;

        /// <summary>
        /// Try get unique set of endpoints from all servers found on discovery server
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Protocol.DiscoveredEndpointsModel>> FindEndpointsAsync(
            Uri discoveryUrl, CancellationToken ct) {

            var results = new HashSet<Protocol.DiscoveredEndpointsModel>();
            var visitedUris = new HashSet<string> {
                CreateDiscoveryUri(discoveryUrl.ToString(), 4840)
            };
            var queue = new Queue<Tuple<Uri, List<string>>>();
            queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
            ct.ThrowIfCancellationRequested();
            while (queue.Any()) {
                var nextServer = queue.Dequeue();
                discoveryUrl = nextServer.Item1;
                var sw = Stopwatch.StartNew();
#if TRACE
                _logger.Debug($"Discover endpoints at {discoveryUrl}...", () => { });
#endif
                try {
                    await Retry.Do(_logger, ct, () =>
                        Task.Run(() => Discover(discoveryUrl, nextServer.Item2,
                            Timeout, visitedUris, queue, results), ct),
                        _ => !ct.IsCancellationRequested, Retry.NoBackoff,
                        kMaxDiscoveryAttempts - 1).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.Error($"Error at {discoveryUrl} (after {sw.Elapsed}).",
                        () => ex);
                    return new HashSet<Protocol.DiscoveredEndpointsModel>();
                }
                ct.ThrowIfCancellationRequested();
#if TRACE
                _logger.Debug(
                    $"Discovery at {discoveryUrl} completed (took {sw.Elapsed}).",
                        () => { });
#endif
            }
            return results;
        }

        /// <summary>
        /// Perform a single discovery run
        /// </summary>
        /// <param name="discoveryUrl"></param>
        /// <param name="caps"></param>
        /// <param name="timeout"></param>
        /// <param name="result"></param>
        /// <param name="visitedUris"></param>
        /// <param name="queue"></param>
        private void Discover(Uri discoveryUrl, IEnumerable<string> caps, int timeout,
            HashSet<string> visitedUris, Queue<Tuple<Uri, List<string>>> queue,
            HashSet<Protocol.DiscoveredEndpointsModel> result) {

            var configuration = EndpointConfiguration.Create(_config);
            configuration.OperationTimeout = timeout;
            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                //
                // Get endpoints from current discovery server
                //
                var endpoints = client.GetEndpoints(null);
                // ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                if (!endpoints.Any()) {
                    _logger.Debug($"No endpoints at {discoveryUrl}...", () => { });
                    return;
                }

                _logger.Debug($"Found endpoints at {discoveryUrl}...",
                    () => { });

                foreach (var ep in endpoints.Where(ep =>
                    ep.Server.ApplicationType != Opc.Ua.ApplicationType.DiscoveryServer)) {
                    result.Add(new Protocol.DiscoveredEndpointsModel {
                        Description = ep,
                        Capabilities = new HashSet<string>(caps)
                    });
                }

                //
                // Now Find servers on network.  This might fail for old lds
                // as well as reference servers, then we call FindServers...
                //
                try {
                    var filter = new StringCollection();
                    var servers = client.FindServersOnNetwork(0, 1000, filter,
                        out var tmp);
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
                    _logger.Debug($"{discoveryUrl} does not support ME extension...",
                        () => { });
                }

                //
                // Call FindServers first to push more unique discovery urls
                // into the browse queue
                //
                var apps = client.FindServers(null);
                foreach (var server in apps.SelectMany(s => s.DiscoveryUrls)) {
                    var url = CreateDiscoveryUri(server, discoveryUrl.Port);
                    if (!visitedUris.Contains(url)) {
                        queue.Enqueue(Tuple.Create(discoveryUrl, new List<string>()));
                        visitedUris.Add(url);
                    }
                }
            }
        }

        /// <summary>
        /// Discover and select endpoint
        /// </summary>
        /// <param name="server"></param>
        /// <param name="discoveryUrl"></param>
        /// <param name="timeout"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        private Task<ConfiguredEndpoint> DiscoverEndpointsAsync(EndpointModel server,
            Uri discoveryUrl, int timeout, Func<EndpointModel, IEnumerable<EndpointDescription>,
                ITransportChannel, EndpointDescription> selector) {
            return Task.Run(() => {
                var selectedEndpoint = DiscoverEndpoints(server, discoveryUrl, timeout,
                    selector);
                if (selectedEndpoint == null) {
                    return null;
                }
                var endpoint = new ConfiguredEndpoint(selectedEndpoint.Server,
                    EndpointConfiguration.Create(_config));
                endpoint.Update(selectedEndpoint);
                return endpoint;
            });
        }

        /// <summary>
        /// Discover and select endpoint
        /// </summary>
        /// <param name="server"></param>
        /// <param name="discoveryUrl"></param>
        /// <param name="timeout"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        private EndpointDescription DiscoverEndpoints(EndpointModel server,
            Uri discoveryUrl, int timeout, Func<EndpointModel, IEnumerable<EndpointDescription>,
                ITransportChannel, EndpointDescription> selector) {

            // use a short timeout.
            var configuration = EndpointConfiguration.Create(_config);
            configuration.OperationTimeout = timeout;

            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                var endpoints = client.GetEndpoints(null);
                ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);

                // Select best endpoint
                return selector(server, endpoints, client.TransportChannel);
            }
        }

        /// <summary>
        /// Replace local host
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="discoveryUrl"></param>
        private static void ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints,
            Uri discoveryUrl) {
            foreach (var endpoint in endpoints) {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl,
                    discoveryUrl.DnsSafeHost);
                var updatedDiscoveryUrls = new StringCollection();
                foreach (var url in endpoint.Server.DiscoveryUrls) {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }
                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }
        }

        /// <summary>
        /// Server session
        /// </summary>
        private sealed class ServerSession : IDisposable {

            /// <summary>
            /// Its name
            /// </summary>
            public string SessionName { get; } =
                Guid.NewGuid().ToString();

            /// <summary>
            /// Configuration for the session
            /// </summary>
            public ApplicationConfiguration Config { get; } =
                CreateApplicationConfiguration();

            /// <summary>
            /// The endpoint
            /// </summary>
            public EndpointModel Endpoint { get; set; }

            /// <summary>
            /// The session
            /// </summary>
            public Session Session { get; set; }

            /// <summary>
            /// Dispose
            /// </summary>
            public void Dispose() {
                Session?.Close();
                Session = null;
            }
        }

        /// <summary>
        /// Lookup key
        /// </summary>
        private sealed class SessionKey {

            /// <summary>
            /// Create new key
            /// </summary>
            /// <param name="endpoint"></param>
            public SessionKey(EndpointModel endpoint) {
                Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            }

            /// <summary>
            /// The endpoint wrapped as key
            /// </summary>
            public EndpointModel Endpoint { get; }

            /// <summary>
            /// Key equality
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj) {
                return obj is SessionKey key &&
                    key != null &&
                    Endpoint.Url == key.Endpoint.Url &&
                    Endpoint.Authentication?.User == key.Endpoint.Authentication?.User &&
                    (Endpoint.Authentication?.TokenType ?? TokenType.None) ==
                        (key.Endpoint.Authentication?.TokenType ?? TokenType.None) &&
                    (Endpoint.SecurityMode ?? SecurityMode.Best) ==
                        (key.Endpoint.SecurityMode ?? SecurityMode.Best) &&
                    Endpoint.SecurityPolicy == key.Endpoint.SecurityPolicy &&
                    JToken.DeepEquals(Endpoint.Authentication?.Token,
                        key.Endpoint.Authentication?.Token);
            }

            /// <summary>
            /// Hash code
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode() {
                var hashCode = -1971667340;
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.SecurityPolicy);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.Url);
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.Authentication?.User ?? "");
                hashCode = hashCode * -1521134295 +
                    EqualityComparer<TokenType?>.Default.GetHashCode(
                        Endpoint.Authentication?.TokenType ?? TokenType.None);
                hashCode = hashCode * -1521134295 +
                   EqualityComparer<SecurityMode?>.Default.GetHashCode(
                       Endpoint.SecurityMode ?? SecurityMode.Best);
                hashCode = hashCode * -1521134295 +
                    JToken.EqualityComparer.GetHashCode(Endpoint.Authentication?.Token);
                return hashCode;
            }
        }

        /// <summary>
        /// Return the session back into the session pool
        /// </summary>
        /// <param name="session"></param>
        private void ReturnSession(ServerSession session) {
            _cache.AddOrUpdate(new SessionKey(session.Endpoint),
                k => ImmutableQueue<ServerSession>.Empty.Enqueue(session),
                (k, v) => v.Enqueue(session));
        }

        /// <summary>
        /// Get session
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private ServerSession GetSession(SessionKey key) {
            ServerSession entry = null;
            _cache.AddOrUpdate(key,
                k => ImmutableQueue<ServerSession>.Empty,
                (k, v) => {
                    if (v.IsEmpty) {
                        return v;
                    }
                    entry = v.Peek();
                    return v.Dequeue();
                });
            if (entry != null) {
                if (entry.Session != null) {
                    if (entry.Config.SecurityConfiguration?
                            .ApplicationCertificate?.Certificate == _clientCert &&
                        entry.Session.Connected) {
                        // Found one
                        return entry;
                    }
                    var cleanup = Task.Run(() => entry.Dispose());
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a new session
        /// </summary>
        private async Task<ServerSession> CreateSessionAsync(SessionKey key) {
            var entry = new ServerSession { Endpoint = key.Endpoint };
            await entry.Config.Validate(Opc.Ua.ApplicationType.Client);
            if (_clientCert != null) {
                entry.Config.SecurityConfiguration.ApplicationCertificate.Certificate =
                    _clientCert;
                entry.Config.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                    _clientCert);
                entry.Config.CertificateValidator.CertificateValidation += (v, e) => {
                    if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                        // TODO: Validate against endpoint model certificate
                        e.Accept = true;
                    }
                };
            }
            else if (entry.Endpoint.SecurityMode == SecurityMode.None) {
                _logger.Warn("Using unsecure connection.", () => { });
            }
            else {
                throw new CertificateInvalidException("Missing client certificate");
            }

            var selectedEndpoint = DiscoverEndpoints(entry.Endpoint,
                new Uri(entry.Endpoint.Url), 60000, (server, endpoints, channel) =>
                    SelectServerEndpoint(server, endpoints, channel, _clientCert != null));
            if (selectedEndpoint == null) {
                throw new ConnectionException("Unable to select secure endpoint");
            }

            var configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server,
                EndpointConfiguration.Create(_config));
            configuredEndpoint.Update(selectedEndpoint);

            entry.Session = await Session.Create(entry.Config, configuredEndpoint, true, false,
                entry.SessionName, (uint)Timeout * 2, CreateUserIdentity(entry.Endpoint), null);
            if (entry.Session == null) {
                throw new ExternalDependencyException("Cannot establish session.");
            }
            entry.Session.KeepAlive += (s, e) => {
                if (e == null || !ServiceResult.IsBad(e.Status)) {
                    return;
                }
                e.CancelKeepAlive = true;

                // Make sure we remove the entry from the pool if it was returned already
                var serverSession = GetSession(key);
                if (serverSession != null && serverSession.Session != null) {
                    var cleanup = Task.Run(() => serverSession.Dispose());
                }
            };
            return entry;
        }

        /// <summary>
        /// Select the endpoint based on the model
        /// </summary>
        /// <param name="server"></param>
        /// <param name="endpoints"></param>
        /// <param name="channel"></param>
        /// <param name="haveCert"></param>
        /// <returns></returns>
        private static EndpointDescription SelectServerEndpoint(EndpointModel server,
            IEnumerable<EndpointDescription> endpoints, ITransportChannel channel, bool haveCert) {

            Contract.Requires(channel != null);

            // Filter
            var filtered = endpoints
                .Where(e => e.TransportProfileUri == Profiles.UaTcpTransport)
                .Where(e => {
                    switch (server.SecurityMode) {
                        case SecurityMode.Best:
                            return true;
                        case SecurityMode.None:
                            return e.SecurityMode == MessageSecurityMode.None;
                        case SecurityMode.Sign:
                            return e.SecurityMode == MessageSecurityMode.Sign;
                        case SecurityMode.SignAndEncrypt:
                            return e.SecurityMode == MessageSecurityMode.SignAndEncrypt;
                    }
                    return true;
                })
                .Where(e => string.IsNullOrEmpty(server.SecurityPolicy) ||
                    server.SecurityPolicy == e.SecurityPolicyUri);

            var bestEndpoint = filtered.FirstOrDefault();
            foreach (var endpoint in filtered) {
                if (haveCert && (endpoint.SecurityLevel > bestEndpoint.SecurityLevel) ||
                    !haveCert && (endpoint.SecurityLevel < bestEndpoint.SecurityLevel)) {
                    bestEndpoint = endpoint;
                }
            }
            return bestEndpoint;
        }

        /// <summary>
        /// Makes a user identity
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static IUserIdentity CreateUserIdentity(EndpointModel endpoint) {
            IUserIdentity userId = null;
            switch (endpoint.Authentication?.TokenType ?? TokenType.None) {
                case TokenType.UserNamePassword:
                    userId = new UserIdentity(endpoint.Authentication.User ?? "unknown",
                        endpoint.Authentication.Token?.ToString() ?? "");
                    break;
                case TokenType.X509Certificate:
                    userId = new UserIdentity(new X509Certificate2(
                        endpoint.Authentication.Token?.ToObject<byte[]>()));
                    break;

                // TODO:
                // ...

                default:
                    userId = new UserIdentity(new AnonymousIdentityToken());
                    break;
            }

            return userId;
        }

        /// <summary>
        /// Create application configuration for client
        /// </summary>
        /// <returns></returns>
        private static ApplicationConfiguration CreateApplicationConfiguration() {
            return new ApplicationConfiguration {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ApplicationUri = "urn:" + Utils.GetHostName() +
                    ":OPCFoundation:CoreSampleClient",
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
                    OperationTimeout = 120000,
                    MaxStringLength = ushort.MaxValue,
                    MaxByteStringLength = ushort.MaxValue * 16,
                    MaxArrayLength = ushort.MaxValue,
                    MaxMessageSize = ushort.MaxValue * 32
                },
                ClientConfiguration = new ClientConfiguration {
                    DefaultSessionTimeout = 120000
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
        /// Convert service result exception to typed exception
        /// </summary>
        /// <param name="sre"></param>
        /// <returns></returns>
        private static Exception ServiceResultToTypedException(ServiceResultException sre) {
            switch (sre.StatusCode) {
                case StatusCodes.BadProtocolVersionUnsupported:
                case StatusCodes.BadConnectionClosed:
                case StatusCodes.BadNotConnected:
                case StatusCodes.BadTcpEndpointUrlInvalid:
                case StatusCodes.BadConnectionRejected:
                case StatusCodes.BadSecurityModeRejected:
                case StatusCodes.BadSecurityPolicyRejected:
                    return new ConnectionException(sre.SymbolicId, sre);
                case StatusCodes.BadLicenseLimitsExceeded:
                case StatusCodes.BadTcpServerTooBusy:
                case StatusCodes.BadTooManySessions:
                    return new ServerBusyException(sre.SymbolicId, sre);
                case StatusCodes.BadTcpMessageTypeInvalid:
                case StatusCodes.BadTcpMessageTooLarge:
                case StatusCodes.BadSequenceNumberUnknown:
                case StatusCodes.BadSequenceNumberInvalid:
                case StatusCodes.BadNonceInvalid:
                    return new ProtocolException(sre.SymbolicId, sre);
                case StatusCodes.BadSecureChannelClosed:
                case StatusCodes.BadSecureChannelTokenUnknown:
                case StatusCodes.BadSecureChannelIdInvalid:
                case StatusCodes.BadCommunicationError:
                case StatusCodes.BadTcpNotEnoughResources:
                case StatusCodes.BadTcpInternalError:
                case StatusCodes.BadSessionClosed:
                case StatusCodes.BadSessionIdInvalid:
                case StatusCodes.BadDisconnect:
                    return new CommunicationException(sre.SymbolicId, sre);
                case StatusCodes.BadTimeout:
                case StatusCodes.BadRequestTimeout:
                    return new TimeoutException(sre.SymbolicId, sre);
                case StatusCodes.BadWriteNotSupported:
                case StatusCodes.BadMethodInvalid:
                case StatusCodes.BadNotReadable:
                case StatusCodes.BadNotWritable:
                    return new InvalidOperationException(sre.SymbolicId, sre);
                case StatusCodes.BadTypeMismatch:
                case StatusCodes.BadArgumentsMissing:
                case StatusCodes.BadInvalidArgument:
                case StatusCodes.BadTooManyArguments:
                case StatusCodes.BadOutOfRange:
                    return new ArgumentException(sre.SymbolicId, sre);
                case StatusCodes.BadCertificateRevocationUnknown:
                case StatusCodes.BadCertificateIssuerRevocationUnknown:
                case StatusCodes.BadCertificateRevoked:
                case StatusCodes.BadCertificateIssuerRevoked:
                case StatusCodes.BadCertificateChainIncomplete:
                case StatusCodes.BadCertificateIssuerUseNotAllowed:
                case StatusCodes.BadCertificateUseNotAllowed:
                case StatusCodes.BadCertificateUriInvalid:
                case StatusCodes.BadCertificateTimeInvalid:
                case StatusCodes.BadCertificateIssuerTimeInvalid:
                case StatusCodes.BadCertificateInvalid:
                case StatusCodes.BadCertificateHostNameInvalid:
                case StatusCodes.BadNoValidCertificates:
                    return new CertificateInvalidException(sre.SymbolicId, sre);
                case StatusCodes.BadCertificateUntrusted:
                    return new CertificateUntrustedException(sre.SymbolicId, sre);
                case StatusCodes.BadUserAccessDenied:
                case StatusCodes.BadIdentityTokenInvalid:
                case StatusCodes.BadIdentityTokenRejected:
                case StatusCodes.BadRequestNotAllowed:
                case StatusCodes.BadLicenseExpired:
                case StatusCodes.BadLicenseNotAvailable:
                    return new UnauthorizedAccessException(sre.SymbolicId, sre);
                case StatusCodes.BadEncodingError:
                case StatusCodes.BadDecodingError:
                case StatusCodes.BadEncodingLimitsExceeded:
                case StatusCodes.BadRequestTooLarge:
                case StatusCodes.BadResponseTooLarge:
                case StatusCodes.BadDataEncodingInvalid:
                    return new FormatException(sre.SymbolicId, sre);
                case StatusCodes.BadDataEncodingUnsupported:
                case StatusCodes.BadServiceUnsupported:
                case StatusCodes.BadNotSupported:
                    return new NotSupportedException(sre.SymbolicId, sre);
                case StatusCodes.BadNotImplemented:
                    return new NotImplementedException(sre.SymbolicId, sre);
                default:
                    return new BadRequestException(sre.SymbolicId, sre);
            }
        }

        private const int kMaxDiscoveryAttempts = 3;

        private readonly ILogger _logger;
        private readonly ApplicationConfiguration _config;
        private readonly ConcurrentDictionary<SessionKey, ImmutableQueue<ServerSession>> _cache;
        private X509Certificate2 _clientCert;
    }
}
