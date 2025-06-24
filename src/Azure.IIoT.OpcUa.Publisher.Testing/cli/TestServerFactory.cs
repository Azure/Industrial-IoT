// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Cli
{
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Extensions.Utils;
    using Microsoft.Extensions.Logging;
    using Opc.Ua;
    using Opc.Ua.Server;
    using Opc.Ua.Test;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Reference server factory
    /// </summary>
    public class TestServerFactory : IServerFactory
    {
        /// <summary>
        /// Whether to log status
        /// </summary>
        public bool LogStatus { get; set; }

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="nodes"></param>
        public TestServerFactory(ILogger<TestServerFactory> logger,
            IEnumerable<INodeManagerFactory> nodes)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        /// <summary>
        /// Full set of servers
        /// </summary>
        /// <param name="logger"></param>
        public TestServerFactory(ILogger<TestServerFactory> logger) :
            this(logger, new List<INodeManagerFactory> {
                new TestData.TestDataServer(),
                new MemoryBuffer.MemoryBufferServer(),
                new Boiler.BoilerServer(),
                new Vehicles.VehiclesServer(),
                new Reference.ReferenceServer(),
                new HistoricalEvents.HistoricalEventsServer(new TimeService()),
                new HistoricalAccess.HistoricalAccessServer(new TimeService()),
                new Views.ViewsServer(),
                new DataAccess.DataAccessServer(),
                new Alarms.AlarmConditionServer(new TimeService()),
                new PerfTest.PerfTestServer(),
                new SimpleEvents.SimpleEventsServer(),
                new Plc.PlcServer(new TimeService(), logger, 1),
                new FileSystem.FileSystemServer(),
                new Asset.AssetServer(logger),
                new Isa95Jobs.Isa95JobControlServer()
            })
        {
        }

        internal static IServerFactory Create(string serverType, ILogger<TestServerFactory> logger)
        {
            switch (serverType.ToLowerInvariant())
            {
                case "reference":
                    return new TestServerFactory(logger,
                    [
                        new Reference.ReferenceServer(),
                    ]);
                case "plc":
                    return new TestServerFactory(logger,
                    [
                        new Plc.PlcServer(new TimeService(), logger, 1),
                    ]);
                case "asset":
                    return new TestServerFactory(logger,
                    [
                        new Asset.AssetServer(logger),
                    ]);
                case "testdata":
                    return new TestServerFactory(logger,
                    [
                        new TestData.TestDataServer(),
                        new MemoryBuffer.MemoryBufferServer(),
                        new Boiler.BoilerServer(),
                        new Vehicles.VehiclesServer(),
                        new DataAccess.DataAccessServer(),
                    ]);
                default:
                    return new TestServerFactory(logger,
                    [
                        new TestData.TestDataServer(),
                        new MemoryBuffer.MemoryBufferServer(),
                        new Boiler.BoilerServer(),
                        new Vehicles.VehiclesServer(),
                        new Reference.ReferenceServer(),
                        new HistoricalEvents.HistoricalEventsServer(new TimeService()),
                        new HistoricalAccess.HistoricalAccessServer(new TimeService()),
                        new Views.ViewsServer(),
                        new DataAccess.DataAccessServer(),
                        new Alarms.AlarmConditionServer(new TimeService()),
                        new PerfTest.PerfTestServer(),
                        new SimpleEvents.SimpleEventsServer(),
                        new Plc.PlcServer(new TimeService(), logger, 1),
                        new FileSystem.FileSystemServer(),
                        new Asset.AssetServer(logger),
                        new Isa95Jobs.Isa95JobControlServer()
                    ]);
            }
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            string pkiRootPath, out ServerBase server,
            Action<ServerConfiguration> configure)
        {
            server = new Server(LogStatus, _nodes, _logger);
            return Server.CreateServerConfiguration(
                ports, pkiRootPath, configure);
        }

        /// <inheritdoc/>
        private sealed class Server : ReverseConnectServer
        {
            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="logStatus"></param>
            /// <param name="nodes"></param>
            /// <param name="logger"></param>
            internal Server(bool logStatus, IEnumerable<INodeManagerFactory> nodes,
                ILogger logger)
            {
                _logger = logger;
                _logStatus = logStatus;
                _nodes = nodes;
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            /// <param name="ports"></param>
            /// <param name="pkiRootPath"></param>
            /// <param name="configure"></param>
            /// <returns></returns>
            public static ApplicationConfiguration CreateServerConfiguration(
                IEnumerable<int> ports, string pkiRootPath,
                Action<ServerConfiguration> configure)
            {
                var extensions = new List<object>
                {
                    new MemoryBuffer.MemoryBufferConfiguration
                    {
                        Buffers =
                        [
                            new MemoryBuffer.MemoryBufferInstance
                            {
                                Name = "UInt32",
                                TagCount = 10000,
                                DataType = "UInt32"
                            },
                            new MemoryBuffer.MemoryBufferInstance
                            {
                                Name = "Double",
                                TagCount = 100,
                                DataType = "Double"
                            }
                        ]
                    }
                    /// ...
                };
                if (string.IsNullOrEmpty(pkiRootPath))
                {
                    pkiRootPath = "pki";
                }
                var configuration = new ApplicationConfiguration
                {
                    ApplicationName = "UA Core Sample Server",
                    ApplicationType = ApplicationType.Server,
                    ApplicationUri = $"urn:{Utils.GetHostName()}:OPCFoundation:CoreSampleServer",
                    Extensions = new XmlElementCollection(
                        extensions.Select(XmlElementEx.SerializeObject)),

                    ProductUri = "http://opcfoundation.org/UA/SampleServer",
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/own",
                            SubjectName = "UA Core Sample Server"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/trusted"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/issuer"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = CertificateStoreType.Directory,
                            StorePath = $"{pkiRootPath}/rejected"
                        },
                        MinimumCertificateKeySize = 1024,
                        RejectSHA1SignedCertificates = false,
                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true
                    },
                    TransportConfigurations = [],
                    TransportQuotas = new TransportQuotas(),
                    ServerConfiguration = new ServerConfiguration
                    {
                        // Sample server specific
                        ServerProfileArray = [
                             "Standard UA Server Profile",
                             "Data Access Server Facet",
                             "Method Server Facet"
                        ],
                        ServerCapabilities = [
                            "DA"
                        ],
                        SupportedPrivateKeyFormats = [
                            "PFX", "PEM"
                        ],

                        NodeManagerSaveFile = "nodes.xml",
                        DiagnosticsEnabled = false,
                        ShutdownDelay = 0,

                        // Runtime configuration
                        BaseAddresses = new StringCollection(ports
                            .Distinct()
                            .Select(p => $"opc.tcp://localhost:{p}/UA/SampleServer")),

                        SecurityPolicies = [
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.Sign,
                                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
                            },
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                                SecurityPolicyUri =SecurityPolicies.Basic256Sha256
                            },
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.None,
                                SecurityPolicyUri = SecurityPolicies.None
                            }
                        ],
                        UserTokenPolicies = [
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Anonymous,
                                SecurityPolicyUri = SecurityPolicies.None
                            },
                            new UserTokenPolicy {
                                TokenType = UserTokenType.UserName
                            },
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Certificate
                            }
                        ],

                        MinRequestThreadCount = 200,
                        MaxRequestThreadCount = 2000,
                        MaxQueuedRequestCount = 2000000,

                        MaxSessionCount = 10000,
                        MinSessionTimeout = 10000,
                        MaxSessionTimeout = 3600000,
                        MaxBrowseContinuationPoints = 1000,
                        MaxQueryContinuationPoints = 1000,
                        MaxHistoryContinuationPoints = 1000,
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
                    TraceConfiguration = new TraceConfiguration
                    {
                        TraceMasks = 1
                    }
                };
                configure?.Invoke(configuration.ServerConfiguration);
                return configuration;
            }

            /// <inheritdoc/>
            protected override ServerProperties LoadServerProperties()
            {
                return new ServerProperties
                {
                    ManufacturerName = "OPC Foundation",
                    ProductName = "OPC UA Sample Servers",
                    ProductUri = "http://opcfoundation.org/UA/Samples/v1.0",
                    SoftwareVersion = Utils.GetAssemblySoftwareVersion(),
                    BuildNumber = Utils.GetAssemblyBuildNumber(),
                    BuildDate = Utils.GetAssemblyTimestamp()
                };
            }

            /// <inheritdoc/>
            protected override MasterNodeManager CreateMasterNodeManager(
                IServerInternal server, ApplicationConfiguration configuration)
            {
                _logger.CreatingNodeManagers();
                var nodeManagers = _nodes
                    .Select(n => n.Create(server, configuration));
                return new MasterNodeManager(server, configuration, null,
                    nodeManagers.ToArray());
            }

            /// <inheritdoc/>
            protected override void OnServerStopping()
            {
                _logger.ServerStopping();
                base.OnServerStopping();
                _cts.Cancel();
                _statusLogger?.Wait();
            }

            /// <inheritdoc/>
            protected override void OnServerStarted(IServerInternal server)
            {
                // start the status thread
                _cts = new CancellationTokenSource();
                if (_logStatus)
                {
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
            protected override void OnServerStarting(ApplicationConfiguration configuration)
            {
                _logger.ServerStarting();
                CreateUserIdentityValidators(configuration);
                base.OnServerStarting(configuration);
            }

            /// <inheritdoc/>
            protected override void OnNodeManagerStarted(IServerInternal server)
            {
                _logger.NodeManagersStarted();
                base.OnNodeManagerStarted(server);
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                _cts?.Dispose();
            }

            /// <summary>
            /// Handle session event by logging status
            /// </summary>
            /// <param name="session"></param>
            /// <param name="reason"></param>
            private void OnEvent(Session session, SessionEventReason reason)
            {
                _lastEventTime = DateTimeOffset.UtcNow;
                LogSessionStatus(session, reason.ToString());
            }

            /// <summary>
            /// Continously log session status if not logged during events
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
            private async Task LogStatusAsync(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    if (DateTimeOffset.UtcNow - _lastEventTime > TimeSpan.FromMilliseconds(6000))
                    {
                        foreach (var session in CurrentInstance.SessionManager.GetSessions())
                        {
                            LogSessionStatus(session, "-Status-", true);
                        }
                        _lastEventTime = DateTimeOffset.UtcNow;
                    }
                    await Try.Async(() => Task.Delay(1000, ct)).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Helper to log session status
            /// </summary>
            /// <param name="session"></param>
            /// <param name="reason"></param>
            /// <param name="lastContact"></param>
            private void LogSessionStatus(Session session, string reason, bool lastContact = false)
            {
                lock (session.DiagnosticsLock)
                {
                    var item = $"{reason,9}:{session.SessionDiagnostics.SessionName,20}:";
                    if (lastContact)
                    {
                        item += $"Last Event:{session.SessionDiagnostics.ClientLastContactTime.ToLocalTime():HH:mm:ss}";
                    }
                    else
                    {
                        if (session.Identity != null)
                        {
                            item += $":{session.Identity.DisplayName,20}";
                        }
                        item += $":{session.Id}";
                    }
                    _logger.ItemStatus(item);
                }
            }

            /// <summary>
            /// Creates the objects used to validate the user identity tokens supported by the server.
            /// </summary>
            /// <param name="configuration"></param>
            private void CreateUserIdentityValidators(ApplicationConfiguration configuration)
            {
                for (var i = 0; i < configuration.ServerConfiguration.UserTokenPolicies.Count; i++)
                {
                    var policy = configuration.ServerConfiguration.UserTokenPolicies[i];

                    // ignore policies without an explicit id.
                    if (string.IsNullOrEmpty(policy.PolicyId))
                    {
                        continue;
                    }

                    // create a validator for an issued token policy.
                    if (policy.TokenType == UserTokenType.IssuedToken)
                    {
                        // the name of the element in the configuration file.
                        var qname = new XmlQualifiedName(policy.PolicyId, Namespaces.OpcUa);

                        // find the id for the issuer certificate.
                        var id = configuration.ParseExtension<CertificateIdentifier>(qname);

                        if (id == null)
                        {
                            Utils.Trace(
                                Utils.TraceMasks.Error,
                                "Could not load CertificateIdentifier for UserTokenPolicy {0}",
                                policy.PolicyId);

                            continue;
                        }
                    }

                    // create a validator for a certificate token policy.
                    if (policy.TokenType == UserTokenType.Certificate)
                    {
                        // the name of the element in the configuration file.
                        var qname = new XmlQualifiedName(policy.PolicyId, Namespaces.OpcUa);

                        // find the location of the trusted issuers.
                        var trustedIssuers = configuration.ParseExtension<CertificateTrustList>(qname);

                        if (trustedIssuers == null)
                        {
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
            /// <param name="session"></param>
            /// <param name="args"></param>
            /// <exception cref="ArgumentNullException"><paramref name="session"/> is <c>null</c>.</exception>
            /// <exception cref="ServiceResultException"></exception>
            private void SessionManager_ImpersonateUser(Session session,
                ImpersonateEventArgs args)
            {
                ArgumentNullException.ThrowIfNull(session);

                if (args.NewIdentity is AnonymousIdentityToken guest)
                {
                    args.Identity = new UserIdentity(guest);
                    Utils.Trace("Guest access accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for a user name token.
                if (args.NewIdentity is UserNameIdentityToken userNameToken)
                {
                    var admin = VerifyPassword(userNameToken.UserName, userNameToken.DecryptedPassword);
                    args.Identity = new UserIdentity(userNameToken);
                    if (admin)
                    {
                        args.Identity = new SystemConfigurationIdentity(args.Identity);
                    }
                    Utils.Trace("UserName Token accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for x509 user token.
                if (args.NewIdentity is X509IdentityToken x509Token)
                {
                    var admin = VerifyCertificate(x509Token.Certificate);
                    args.Identity = new UserIdentity(x509Token);
                    if (admin)
                    {
                        args.Identity = new SystemConfigurationIdentity(args.Identity);
                    }
                    Utils.Trace("X509 Token accepted: {0}", args.Identity.DisplayName);
                    return;
                }

                // check for x509 user token.
                if (args.NewIdentity is IssuedIdentityToken wssToken)
                {
                    var admin = VerifyToken(wssToken);
                    args.Identity = new UserIdentity(wssToken);
                    if (admin)
                    {
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
            /// <exception cref="ServiceResultException"></exception>
            private static bool VerifyToken(IssuedIdentityToken wssToken)
            {
                if ((wssToken.TokenData?.Length ?? 0) == 0)
                {
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
            /// <param name="userName"></param>
            /// <param name="password"></param>
            /// <exception cref="ServiceResultException"></exception>
            private static bool VerifyPassword(string userName, string password)
            {
                if (string.IsNullOrEmpty(password))
                {
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

                if (userName.Equals("test", StringComparison.OrdinalIgnoreCase) &&
                    password == "test")
                {
                    // Testing purposes only
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Verifies that a certificate user token is trusted.
            /// </summary>
            /// <param name="certificate"></param>
            /// <exception cref="ServiceResultException"></exception>
            private bool VerifyCertificate(X509Certificate2 certificate)
            {
                try
                {
                    if (_certificateValidator != null)
                    {
                        _certificateValidator.Validate(certificate);
                    }
                    else
                    {
                        CertificateValidator.Validate(certificate);
                    }

                    // determine if self-signed.
                    var isSelfSigned = X509Utils.CompareDistinguishedName(
                        certificate.Subject, certificate.Issuer);

                    // do not allow self signed application certs as user token
                    if (isSelfSigned && X509Utils.HasApplicationURN(certificate))
                    {
                        throw new ServiceResultException(StatusCodes.BadCertificateUseNotAllowed);
                    }
                    return false;
                }
                catch (Exception e)
                {
                    TranslationInfo info;
                    StatusCode result = StatusCodes.BadIdentityTokenRejected;
                    if (e is ServiceResultException se &&
                        se.StatusCode == StatusCodes.BadCertificateUseNotAllowed)
                    {
                        info = new TranslationInfo(
                            "InvalidCertificate",
                            "en-US",
                            "'{0}' is an invalid user certificate.",
                            certificate.Subject);

                        result = StatusCodes.BadIdentityTokenInvalid;
                    }
                    else
                    {
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
            private DateTimeOffset _lastEventTime;
            private CancellationTokenSource _cts;
            private ICertificateValidator _certificateValidator;

            private const string kServerNamespaceUri = "http://opcfoundation.org/UA/Sample/";
        }

        private readonly ILogger _logger;
        private readonly IEnumerable<INodeManagerFactory> _nodes;
    }

    /// <summary>
    /// Source-generated logging definitions for TestServer
    /// </summary>
    internal static partial class TestServerLogging
    {
        private const int EventClass = 0;

        [LoggerMessage(EventId = EventClass + 1, Level = LogLevel.Information,
            Message = "Creating the Node Managers.")]
        public static partial void CreatingNodeManagers(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 2, Level = LogLevel.Debug,
            Message = "The server is stopping.")]
        public static partial void ServerStopping(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 3, Level = LogLevel.Debug,
            Message = "The server is starting.")]
        public static partial void ServerStarting(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 4, Level = LogLevel.Information,
            Message = "The NodeManagers have started.")]
        public static partial void NodeManagersStarted(this ILogger logger);

        [LoggerMessage(EventId = EventClass + 5, Level = LogLevel.Information,
            Message = "{Log}")]
        public static partial void ItemStatus(this ILogger logger, string log);
    }
}
