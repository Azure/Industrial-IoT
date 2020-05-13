// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Sample {
    using Microsoft.Azure.IIoT.Utils;
    using Serilog;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.IdentityModel.Selectors;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Sample server factory
    /// </summary>
    public class ServerFactory : IServerFactory {

        /// <summary>
        /// Whether to log status
        /// </summary>
        public bool LogStatus { get; set; }

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="nodes"></param>
        public ServerFactory(ILogger logger, IEnumerable<INodeManagerFactory> nodes) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Create sample servers
        /// </summary>
        /// <param name="logger"></param>
        public ServerFactory(ILogger logger) :
            this(logger, new List<INodeManagerFactory> {
                new TestData.TestDataServer(),
                new MemoryBuffer.MemoryBufferServer(),
                new Boiler.BoilerServer(),
                new Vehicles.VehiclesServer(),
                new Reference.ReferenceServer(),
                new HistoricalEvents.HistoricalEventsServer(),
                new HistoricalAccess.HistoricalAccessServer(),
                new Views.ViewsServer(),
                new DataAccess.DataAccessServer(),
                new Alarms.AlarmConditionServer(),
                // new PerfTest.PerfTestServer(),
                new SimpleEvents.SimpleEventsServer(),
                new Plc.PlcServer()
            }) {
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports, string applicationName,
            out ServerBase server) {
            server = new Server(LogStatus, _nodes, _logger);
            return Server.CreateServerConfiguration(ports, applicationName);
        }

        /// <inheritdoc/>
        private class Server : StandardServer {

            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="logStatus"></param>
            /// <param name="nodes"></param>
            /// <param name="logger"></param>
            public Server(bool logStatus, IEnumerable<INodeManagerFactory> nodes,
                ILogger logger) {
                _logger = logger;
                _logStatus = logStatus;
                _nodes = nodes;
            }

            /// <summary>
            /// Create server configuration
            /// </summary>
            /// <param name="ports"></param>
            /// <returns></returns>
            public static ApplicationConfiguration CreateServerConfiguration(
                IEnumerable<int> ports, string pkiRootPath) {
                var extensions = new List<object> {
                    new MemoryBuffer.MemoryBufferConfiguration {
                        Buffers = new MemoryBuffer.MemoryBufferInstanceCollection {
                            new MemoryBuffer.MemoryBufferInstance {
                                Name = "UInt32",
                                TagCount = 10000,
                                DataType = "UInt32"
                            },
                            new MemoryBuffer.MemoryBufferInstance {
                                Name = "Double",
                                TagCount = 100,
                                DataType = "Double"
                            },
                        }
                    },

                    /// ...
                };
                if (string.IsNullOrEmpty(pkiRootPath)) {
                    pkiRootPath = "pki";
                }
                return new ApplicationConfiguration {
                    ApplicationName = "UA Core Sample Server",
                    ApplicationType = ApplicationType.Server,
                    ApplicationUri = $"urn:{Dns.GetHostName()}:OPCFoundation:CoreSampleServer",
                    Extensions = new XmlElementCollection(
                        extensions.Select(XmlElementEx.SerializeObject)),

                    ProductUri = "http://opcfoundation.org/UA/SampleServer",
                    SecurityConfiguration = new SecurityConfiguration {
                        ApplicationCertificate = new CertificateIdentifier {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/own",
                            SubjectName = "UA Core Sample Server",
                        },
                        TrustedPeerCertificates = new CertificateTrustList {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/trusted",
                        },
                        TrustedIssuerCertificates = new CertificateTrustList {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/issuer",
                        },
                        RejectedCertificateStore = new CertificateTrustList {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/rejected",
                        },
                        MinimumCertificateKeySize = 1024,
                        RejectSHA1SignedCertificates = false,
                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = TransportQuotaConfigEx.DefaultTransportQuotas(),
                    ServerConfiguration = new ServerConfiguration {

                        // Sample server specific
                        ServerProfileArray = new StringCollection {
                             "Standard UA Server Profile",
                             "Data Access Server Facet",
                             "Method Server Facet"
                        },
                        ServerCapabilities = new StringCollection {
                            "DA"
                        },
                        SupportedPrivateKeyFormats = new StringCollection {
                            "PFX", "PEM"
                        },

                        NodeManagerSaveFile = "nodes.xml",
                        DiagnosticsEnabled = false,
                        ShutdownDelay = 5,

                        // No op
                        MinRequestThreadCount = 3,
                        MaxRequestThreadCount = 100,
                        MaxQueuedRequestCount = 2000,

                        // Runtime configuration
                        BaseAddresses = new StringCollection(ports
                            .Distinct()
                            .Select(p => $"opc.tcp://localhost:{p}/UA/SampleServer")),

                        SecurityPolicies = new ServerSecurityPolicyCollection {
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.Sign,
                                SecurityPolicyUri = SecurityPolicies.Basic256Sha256,
                            },
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                                SecurityPolicyUri =SecurityPolicies.Basic256Sha256,
                            },
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection {
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Anonymous,
                                SecurityPolicyUri = SecurityPolicies.None,
                            },
                            new UserTokenPolicy {
                                TokenType = UserTokenType.UserName
                            },
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Certificate
                            }
                        },

                        MaxSessionCount = 100,
                        MinSessionTimeout = 10000,
                        MaxSessionTimeout = 3600000,
                        MaxBrowseContinuationPoints = 10,
                        MaxQueryContinuationPoints = 10,
                        MaxHistoryContinuationPoints = 100,
                        MaxRequestAge = 600000,
                        MinPublishingInterval = 100,
                        MaxPublishingInterval = 3600000,
                        PublishingResolution = 50,
                        MaxSubscriptionLifetime = 3600000,
                        MaxMessageQueueSize = 100,
                        MaxNotificationQueueSize = 100,
                        MaxNotificationsPerPublish = 1000,
                        MinMetadataSamplingInterval = 1000,
                        MaxPublishRequestCount = 20,
                        MaxSubscriptionCount = 100,
                        MaxEventQueueSize = 10000,
                        MinSubscriptionLifetime = 10000,

                        // Do not register with LDS
                        MaxRegistrationInterval = 0, // TODO
                        RegistrationEndpoint = null
                    },
                    TraceConfiguration = new TraceConfiguration {
                        TraceMasks = 1
                    }
                };
            }

            /// <inheritdoc/>
            protected override ServerProperties LoadServerProperties() {
                var properties = new ServerProperties {
                    ManufacturerName = "OPC Foundation",
                    ProductName = "OPC UA Sample Servers",
                    ProductUri = "http://opcfoundation.org/UA/Samples/v1.0",
                    SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                    BuildNumber = Utils.GetAssemblyBuildNumber(),
                    BuildDate = Utils.GetAssemblyTimestamp()
                };
                return properties;
            }

            /// <inheritdoc/>
            protected override MasterNodeManager CreateMasterNodeManager(
                IServerInternal server, ApplicationConfiguration configuration) {
                _logger.Information("Creating the Node Managers.");
                var nodeManagers = _nodes
                    .Select(n => n.CreateNodeManager(server, configuration));
                return new MasterNodeManager(server, configuration, null,
                    nodeManagers.ToArray());
            }

            /// <inheritdoc/>
            protected override void OnServerStopping() {
                _logger.Debug("The server is stopping.");
                base.OnServerStopping();
                _cts.Cancel();
                _statusLogger?.Wait();
            }

            /// <inheritdoc/>
            protected override void OnServerStarted(IServerInternal server) {
                // start the status thread
                _cts = new CancellationTokenSource();
                if (_logStatus) {
                    _statusLogger = Task.Run(() => LogStatusAsync(_cts.Token));

                    // print notification on session events
                    CurrentInstance.SessionManager.SessionActivated += OnEvent;
                    CurrentInstance.SessionManager.SessionClosing += OnEvent;
                    CurrentInstance.SessionManager.SessionCreated += OnEvent;
                }
                base.OnServerStarted(server);
                // request notifications when the user identity is changed. all valid users are accepted by default.
                server.SessionManager.ImpersonateUser += SessionManager_ImpersonateUser;
            }

            /// <inheritdoc/>
            protected override void OnServerStarting(ApplicationConfiguration configuration) {
                _logger.Debug("The server is starting.");
                CreateUserIdentityValidators(configuration);
                base.OnServerStarting(configuration);
            }

            /// <inheritdoc/>
            protected override void OnNodeManagerStarted(IServerInternal server) {
                _logger.Information("The NodeManagers have started.");
                base.OnNodeManagerStarted(server);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing) {
                base.Dispose(disposing);
                _cts?.Dispose();
            }

            /// <summary>
            /// Handle session event by logging status
            /// </summary>
            /// <param name="session"></param>
            /// <param name="reason"></param>
            private void OnEvent(Session session, SessionEventReason reason) {
                _lastEventTime = DateTime.UtcNow;
                LogSessionStatus(session, reason.ToString());
            }

            /// <summary>
            /// Continously log session status if not logged during events
            /// </summary>
            /// <returns></returns>
            private async Task LogStatusAsync(CancellationToken ct) {
                while (!ct.IsCancellationRequested) {
                    if (DateTime.UtcNow - _lastEventTime > TimeSpan.FromMilliseconds(6000)) {
                        foreach (var session in CurrentInstance.SessionManager.GetSessions()) {
                            LogSessionStatus(session, "-Status-", true);
                        }
                        _lastEventTime = DateTime.UtcNow;
                    }
                    await Try.Async(() => Task.Delay(1000, ct));
                }
            }

            /// <summary>
            /// Helper to log session status
            /// </summary>
            /// <param name="session"></param>
            /// <param name="reason"></param>
            /// <param name="lastContact"></param>
            private void LogSessionStatus(Session session, string reason, bool lastContact = false) {
                lock (session.DiagnosticsLock) {
                    var item = string.Format("{0,9}:{1,20}:", reason,
                        session.SessionDiagnostics.SessionName);
                    if (lastContact) {
                        item += string.Format("Last Event:{0:HH:mm:ss}",
                            session.SessionDiagnostics.ClientLastContactTime.ToLocalTime());
                    }
                    else {
                        if (session.Identity != null) {
                            item += string.Format(":{0,20}", session.Identity.DisplayName);
                        }
                        item += string.Format(":{0}", session.Id);
                    }
                    _logger.Information(item);
                }
            }
            /// <summary>
            /// Creates the objects used to validate the user identity tokens supported by the server.
            /// </summary>
            private void CreateUserIdentityValidators(ApplicationConfiguration configuration) {
                for (var ii = 0; ii < configuration.ServerConfiguration.UserTokenPolicies.Count; ii++) {
                    var policy = configuration.ServerConfiguration.UserTokenPolicies[ii];

                    // ignore policies without an explicit id.
                    if (string.IsNullOrEmpty(policy.PolicyId)) {
                        continue;
                    }

                    // create a validator for an issued token policy.
                    if (policy.TokenType == UserTokenType.IssuedToken) {
                        // the name of the element in the configuration file.
                        var qname = new XmlQualifiedName(policy.PolicyId, Namespaces.OpcUa);

                        // find the id for the issuer certificate.
                        var id = configuration.ParseExtension<CertificateIdentifier>(qname);

                        if (id == null) {
                            Utils.Trace(
                                Utils.TraceMasks.Error,
                                "Could not load CertificateIdentifier for UserTokenPolicy {0}",
                                policy.PolicyId);

                            continue;
                        }
                    }

                    // create a validator for a certificate token policy.
                    if (policy.TokenType == UserTokenType.Certificate) {
                        // the name of the element in the configuration file.
                        var qname = new XmlQualifiedName(policy.PolicyId, Namespaces.OpcUa);

                        // find the location of the trusted issuers.
                        var trustedIssuers = configuration.ParseExtension<CertificateTrustList>(qname);

                        if (trustedIssuers == null) {
                            Utils.Trace(
                                Utils.TraceMasks.Error,
                                "Could not load CertificateTrustList for UserTokenPolicy {0}",
                                policy.PolicyId);

                            continue;
                        }

                        // trusts any certificate in the trusted people store.
                        _certificateValidator = CertificateValidator.GetChannelValidator();
                    }
                }
            }

            /// <summary>
            /// Called when a client tries to change its user identity.
            /// </summary>
            private void SessionManager_ImpersonateUser(Session session,
                ImpersonateEventArgs args) {
                if (session == null) {
                    throw new ArgumentNullException(nameof(session));
                }

                if (args.NewIdentity is AnonymousIdentityToken guest) {
                    args.Identity = new UserIdentity(guest);
                    Utils.Trace("Guest access accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for a user name token.
                if (args.NewIdentity is UserNameIdentityToken userNameToken) {
                    var admin = VerifyPassword(userNameToken.UserName, userNameToken.DecryptedPassword);
                    args.Identity = new UserIdentity(userNameToken);
                    if (admin) {
                        args.Identity = new SystemConfigurationIdentity(args.Identity);
                    }
                    Utils.Trace("UserName Token accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for x509 user token.
                if (args.NewIdentity is X509IdentityToken x509Token) {
                    var admin = VerifyCertificate(x509Token.Certificate);
                    args.Identity = new UserIdentity(x509Token);
                    if (admin) {
                        args.Identity = new SystemConfigurationIdentity(args.Identity);
                    }
                    Utils.Trace("X509 Token accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for x509 user token.
                if (args.NewIdentity is IssuedIdentityToken wssToken) {
                    var admin = VerifyToken(wssToken);
                    args.Identity = new UserIdentity(wssToken);
                    if (admin) {
                        args.Identity = new SystemConfigurationIdentity(args.Identity);
                    }
                    Utils.Trace("Issued Token accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // construct translation object with default text.
                var info = new TranslationInfo("InvalidToken", "en-US",
                    "Specified token is not valid.");
                // create an exception with a vendor defined sub-code.
                throw new ServiceResultException(new ServiceResult(
                    StatusCodes.BadIdentityTokenRejected, "Bad token",
                    kServerNamespaceUri, new LocalizedText(info)));
            }

            /// <summary>
            /// Validates the token
            /// </summary>
            /// <param name="wssToken"></param>
            private bool VerifyToken(IssuedIdentityToken wssToken) {
                if ((wssToken.TokenData?.Length ?? 0) == 0) {
                    var info = new TranslationInfo("InvalidToken", "en-US",
                        "Specified token is empty.");
                    // create an exception with a vendor defined sub-code.
                    throw new ServiceResultException(new ServiceResult(
                        StatusCodes.BadIdentityTokenRejected, "Bad token",
                        kServerNamespaceUri, new LocalizedText(info)));
                }
                return false;
            }

            /// <summary>
            /// Validates the password for a username token.
            /// </summary>
            private bool VerifyPassword(string userName, string password) {
                if (string.IsNullOrEmpty(password)) {
                    // construct translation object with default text.
                    var info = new TranslationInfo(
                        "InvalidPassword", "en-US",
                        "Specified password is not valid for user '{0}'.",
                        userName);
                    // create an exception with a vendor defined sub-code.
                    throw new ServiceResultException(new ServiceResult(
                        StatusCodes.BadIdentityTokenRejected, "InvalidPassword",
                        kServerNamespaceUri, new LocalizedText(info)));
                }

                if (userName.EqualsIgnoreCase("test") && password == "test") {
                    // Testing purposes only
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Verifies that a certificate user token is trusted.
            /// </summary>
            private bool VerifyCertificate(X509Certificate2 certificate) {
                try {
                    if (_certificateValidator != null) {
                        _certificateValidator.Validate(certificate);
                    }
                    else {
                        CertificateValidator.Validate(certificate);
                    }

                    // determine if self-signed.
                    var isSelfSigned = Utils.CompareDistinguishedName(
                        certificate.Subject, certificate.Issuer);

                    // do not allow self signed application certs as user token
                    if (isSelfSigned && Utils.HasApplicationURN(certificate)) {
                        throw new ServiceResultException(StatusCodes.BadCertificateUseNotAllowed);
                    }
                    return false;
                }
                catch (Exception e) {
                    TranslationInfo info;
                    StatusCode result = StatusCodes.BadIdentityTokenRejected;
                    if (e is ServiceResultException se &&
                        se.StatusCode == StatusCodes.BadCertificateUseNotAllowed) {
                        info = new TranslationInfo(
                            "InvalidCertificate",
                            "en-US",
                            "'{0}' is an invalid user certificate.",
                            certificate.Subject);

                        result = StatusCodes.BadIdentityTokenInvalid;
                    }
                    else {
                        // construct translation object with default text.
                        info = new TranslationInfo(
                            "UntrustedCertificate",
                            "en-US",
                            "'{0}' is not a trusted user certificate.",
                            certificate.Subject);
                    }

                    // create an exception with a vendor defined sub-code.
                    throw new ServiceResultException(new ServiceResult(
                        result, info.Key, kServerNamespaceUri, new LocalizedText(info)));
                }
            }

            private readonly ILogger _logger;
            private readonly bool _logStatus;
            private readonly IEnumerable<INodeManagerFactory> _nodes;
            private Task _statusLogger;
            private DateTime _lastEventTime;
            private CancellationTokenSource _cts;
            private X509CertificateValidator _certificateValidator;

            private const string kServerNamespaceUri = "http://opcfoundation.org/UA/Sample/";
        }

        private readonly ILogger _logger;
        private readonly IEnumerable<INodeManagerFactory> _nodes;
    }
}
