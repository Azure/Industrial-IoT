// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Client {
    using Microsoft.Azure.Devices.Proxy;
    using Microsoft.Azure.Devices.Proxy.Provider;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Diagnostics;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Exceptions;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Models;
    using Microsoft.Azure.IoTSolutions.OpcUaExplorer.Services.Runtime;
    using Opc.Ua;
    using Opc.Ua.Bindings.Proxy;
    using Opc.Ua.Client;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Contracts;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Opc ua stack based client
    /// </summary>
    public class OpcUaServerClient : IOpcUaClient {

        public bool UsesProxy { get; }

        /// <summary>
        /// Create stack
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="servicesConfig"></param>
        public OpcUaServerClient(ILogger logger, IOpcUaServicesConfig servicesConfig) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new ConcurrentDictionary<SessionKey, ImmutableQueue<ServerSession>>();

            _config = CreateApplicationConfiguration();

            if (servicesConfig != null && 
                !string.IsNullOrEmpty(servicesConfig.IoTHubConnString) && 
                !servicesConfig.BypassProxy) {

                // initialize our custom transport via the proxy
                Socket.Provider = new DefaultProvider(servicesConfig.IoTHubConnString);
                WcfChannelBase.g_CustomTransportChannel = new ProxyTransportChannelFactory();
                UsesProxy = true;
            }

            if (UsesProxy) {
                _logger.Info("OPC stack with reverse proxy connection to shop floor", () => { });
            }
            else {
                _logger.Info("OPC stack with direct connection to shop floor servers", () => { });
            }
        }

        /// <summary>
        /// Try connecting to endpoint
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public async Task TryConnectAsync(ServerEndpointModel endpoint, 
            Action<ITransportChannel, IEnumerable<EndpointDescription>> callback) {
            await SelectEndpointAsync(endpoint, (endpoints, channel, _) => {
                callback(channel, endpoints);
                return null;
            });
        }

        /// <summary>
        /// Execute opc ua service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<T> ExecuteServiceAsync<T>(ServerEndpointModel endpoint,
            Func<Session, Task<T>> service) {
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
                        session = await CreateSessionAsync(key);
                    }
                    catch(ServiceResultException sre) {
                        _logger.Debug("Failed create session", () => new { sre, endpoint });
                        // Throw immediately - this cannot be retried...
                        throw ServiceResultToTypedException(sre);
                    }
                }
                try {
                    // Run service on session and convert 
                    var result = await service(session.Session);
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
                            if (!retry) {
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
        /// Server session
        /// </summary>
        private sealed class ServerSession {
            /// <summary>
            /// The endpoint 
            /// </summary>
            public ServerEndpointModel Endpoint { get; set; }

            /// <summary>
            /// The session
            /// </summary>
            public Session Session { get; set; }

            /// <summary>
            /// Its name
            /// </summary>
            public string SessionName { get; } = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Lookup key
        /// </summary>
        private sealed class SessionKey {

            /// <summary>
            /// Create new key
            /// </summary>
            /// <param name="endpoint"></param>
            public SessionKey(ServerEndpointModel endpoint) {
                Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            }

            /// <summary>
            /// The endpoint wrapped as key
            /// </summary>
            public ServerEndpointModel Endpoint { get; }

            /// <summary>
            /// Key equality
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj) {
                var key = obj as SessionKey;
                return key != null &&
                    Endpoint.Url == key.Endpoint.Url &&
                    Endpoint.User == key.Endpoint.User &&
                    Endpoint.Type == key.Endpoint.Type &&
                    Endpoint.EdgeController == key.Endpoint.EdgeController &&
                    EqualityComparer<object>.Default.Equals(
                        Endpoint.Token, key.Endpoint.Token) &&
                    EqualityComparer<bool?>.Default.Equals(
                        Endpoint.IsTrusted, key.Endpoint.IsTrusted) &&
                    EqualityComparer<X509Certificate2>.Default.Equals(
                        Endpoint.ServerCertificate, key.Endpoint.ServerCertificate) &&
                    EqualityComparer<X509Certificate2>.Default.Equals(
                        Endpoint.ClientCertificate, key.Endpoint.ClientCertificate);
            }

            /// <summary>
            /// Hash code
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode() {
                var hashCode = -1971667340;
                hashCode = hashCode * -1521134295 + 
                    Endpoint.Type.GetHashCode();
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.Url);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.User);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<object>.Default.GetHashCode(Endpoint.Token);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<bool?>.Default.GetHashCode(Endpoint.IsTrusted);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<X509Certificate2>.Default.GetHashCode(Endpoint.ServerCertificate);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<X509Certificate2>.Default.GetHashCode(Endpoint.ClientCertificate);
                hashCode = hashCode * -1521134295 + 
                    EqualityComparer<string>.Default.GetHashCode(Endpoint.EdgeController);
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
                    if (entry.Session.Connected) {
                        // Found one
                        return entry;
                    }
                    var cleanup = Task.Run(() => entry.Session.Close());
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a active session from the pool or creates a new one
        /// </summary>
        private async Task<ServerSession> CreateSessionAsync(SessionKey key) {

            var endpoint = key.Endpoint;
            var entry = new ServerSession {
                Endpoint = endpoint
            };

            var configuredEndpoint = await SelectEndpointAsync(endpoint, SelectHighSecurityEndpoint);
            if (configuredEndpoint == null) {
                throw new ConnectionException("Unable to select secure endpoint");
            }
            entry.Session = await Session.Create(_config, configuredEndpoint, true, false,
                entry.SessionName, 60000, CreateUserIdentity(endpoint), null);
            if (entry.Session == null) {
                throw new ExternalDependencyException("Cannot establish session");
            }
            entry.Session.KeepAlive += (s, e) => {
                if (e == null || !ServiceResult.IsBad(e.Status)) {
                    return;
                }
                e.CancelKeepAlive = true;

                // Make sure we remove the entry from the pool if it was returned already
                var serverSession = GetSession(key);
                if (serverSession != null && serverSession.Session != null) {
                    var cleanup = Task.Run(() => serverSession.Session.Close());
                }
            };
            return entry;
        }

        /// <summary>
        /// Makes a user identity
        /// </summary>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        private static IUserIdentity CreateUserIdentity(ServerEndpointModel endpoint) {
            IUserIdentity userId = null;
            switch (endpoint.Type) {
                case TokenType.UserNamePassword:
                    userId = new UserIdentity(endpoint.User, endpoint.Token.ToString());
                    break;
                case TokenType.X509Certificate:
                    userId = new UserIdentity((X509Certificate2)endpoint.Token);
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
        /// Create application configuration for importer
        /// </summary>
        /// <returns></returns>
        private static ApplicationConfiguration CreateApplicationConfiguration() {
            return new ApplicationConfiguration {
                ApplicationName = "UA Core Sample Client",
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:" + Utils.GetHostName() + ":OPCFoundation:CoreSampleClient",
                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "UA Core Sample Client"
                    },
                    TrustedPeerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Applications",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities",
                    },
                    RejectedCertificateStore = new CertificateTrustList {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/RejectedCertificates",
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
        /// Select endpoint
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        private async Task<ConfiguredEndpoint> SelectEndpointAsync(ServerEndpointModel server,
            Func<IEnumerable<EndpointDescription>, ITransportChannel, bool, EndpointDescription> selector) {
            await _config.Validate(ApplicationType.Client);
            var haveAppCertificate = 
                _config.SecurityConfiguration.ApplicationCertificate.Certificate != null;
            if (!haveAppCertificate) {
                _logger.Info($"Creating new application certificate: {_config.ApplicationName}",
                    () => { });
                var certificate = CertificateFactory.CreateCertificate(
                    _config.SecurityConfiguration.ApplicationCertificate.StoreType,
                    _config.SecurityConfiguration.ApplicationCertificate.StorePath, null,
                    _config.ApplicationUri,
                    _config.ApplicationName,
                    _config.SecurityConfiguration.ApplicationCertificate.SubjectName, null,
                    CertificateFactory.defaultKeySize, DateTime.UtcNow - TimeSpan.FromDays(1),
                    CertificateFactory.defaultLifeTime, CertificateFactory.defaultHashSize,
                    false, null, null);
                _config.SecurityConfiguration.ApplicationCertificate.Certificate = certificate;
            }

            // Add server certificate to trust list for this session ???
            // Retrieve above information from client cert

            // TODO: This store business is bogus.  Can we work around it by using the passed certs
            // or some other abstraction...????

            server.ClientCertificate = _config.SecurityConfiguration.ApplicationCertificate.Certificate;
            haveAppCertificate = server.ClientCertificate != null;
            if (haveAppCertificate) {
                _config.ApplicationUri = Utils.GetApplicationUriFromCertificate(
                    _config.SecurityConfiguration.ApplicationCertificate.Certificate);
                _config.CertificateValidator.CertificateValidation += (v, e) => {
                    if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) {
                        e.Accept = (server.IsTrusted ?? false);
                    }
                };
            }
            else {
                Console.WriteLine("WARNING: missing application certificate, using unsecure connection.");
            }

            var endpointURI = new Uri(server.Url);
            var selectedEndpoint = DiscoverAndSelectEndpoint(_config, new Uri(server.Url), 60000, 
                haveAppCertificate, selector);
            if (selectedEndpoint == null) {
                return null;
            }
            var endpointConfiguration = EndpointConfiguration.Create(_config);
            var endpoint = new ConfiguredEndpoint(selectedEndpoint.Server, endpointConfiguration);
            endpoint.Update(selectedEndpoint);
            return endpoint;
        }

        /// <summary>
        /// Select endpoint
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="haveCert"></param>
        /// <returns></returns>
        private static EndpointDescription SelectHighSecurityEndpoint(
            IEnumerable<EndpointDescription> endpoints, ITransportChannel channel, bool haveCert) {
            Contract.Requires(channel != null);
            EndpointDescription bestEndpoint = null;
            foreach (var endpoint in endpoints) {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport) {
                    if (bestEndpoint == null ||
                        haveCert && (endpoint.SecurityLevel > bestEndpoint.SecurityLevel) ||
                        !haveCert && (endpoint.SecurityLevel < bestEndpoint.SecurityLevel)) {
                        bestEndpoint = endpoint;
                    }
                }
            }
            return bestEndpoint;
        }

        /// <summary>
        /// Discover and select endpoint
        /// </summary>
        /// <param name="config"></param>
        /// <param name="discoveryUrl"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        private static EndpointDescription DiscoverAndSelectEndpoint(
            ApplicationConfiguration config, Uri discoveryUrl, int timeout, bool haveCert,
            Func<IEnumerable<EndpointDescription>, ITransportChannel, bool, EndpointDescription> selector) {
            // use a short timeout.
            var configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (var client = DiscoveryClient.Create(discoveryUrl, configuration)) {
                try {
                    var endpoints = client.GetEndpoints(null);
                    ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);

                    // Select best endpoint
                    return selector(endpoints, client.TransportChannel, haveCert);
                }
                catch (Exception e) {
                    Console.WriteLine("Could not fetch endpoints from url: {0}", discoveryUrl);
                    Console.WriteLine("Reason = {0}", e.Message);
                    throw e;
                }
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

        private readonly ILogger _logger;
        private readonly ApplicationConfiguration _config;
        private readonly ConcurrentDictionary<SessionKey, ImmutableQueue<ServerSession>> _cache;
    }
}
