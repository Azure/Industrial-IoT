// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Sample
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
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Server factory
    /// </summary>
    public sealed class ServerFactory : IServerFactory
    {
        /// <summary>
        /// Whether to log status
        /// </summary>
        public bool LogStatus { get; set; }

        /// <summary>
        /// Whether to enable diagnostics
        /// </summary>
        public bool EnableDiagnostics { get; set; }

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tempPath"></param>
        /// <param name="nodes"></param>
        public ServerFactory(ILogger<ServerFactory> logger, string tempPath,
            IEnumerable<INodeManagerFactory> nodes)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            _tempPath = tempPath;
        }

        /// <summary>
        /// Full set of servers
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tempPath"></param>
        /// <param name="scaleunits"></param>
        public ServerFactory(ILogger<ServerFactory> logger, string tempPath,
            uint scaleunits = 0) :
            this(logger, tempPath, new List<INodeManagerFactory>
            {
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
                new SimpleEvents.SimpleEventsServer(),
                new Plc.PlcServer(new TimeService(), logger, scaleunits),
                new Isa95Jobs.Isa95JobControlServer()
                // new FileSystem.FileSystemServer(),
                // new Asset.AssetServer(logger),
                // new PerfTest.PerfTestServer(),
            })
        {
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            string pkiRootPath, out ServerBase server, string listenHostName,
            IEnumerable<string> alternativeAddresses, string path,
            string certStoreType, Action<ServerConfiguration> configure)
        {
            server = new Server(LogStatus, _nodes, _logger);
            return Server.CreateServerConfiguration(_tempPath,
                ports, listenHostName, alternativeAddresses, path,
                pkiRootPath, certStoreType, EnableDiagnostics, configure);
        }

        /// <inheritdoc/>
        private sealed class Server : ReverseConnectServer, ITestServer
        {
            /// <inheritdoc/>
            public string PublishedNodesJson => _plc.GetPnJson();

            /// <inheritdoc/>
            public bool Chaos
            {
                get
                {
                    return _chaosMode != null;
                }
                set
                {
                    if (value)
                    {
                        if (_chaosMode == null)
                        {
                            _chaosCts = new CancellationTokenSource();
                            _chaosMode = ChaosAsync(_chaosCts.Token);
                        }
                    }
                    else if (_chaosMode != null)
                    {
                        _chaosCts.Cancel();
                        _chaosMode.GetAwaiter().GetResult();
                        _chaosCts.Dispose();
                        _chaosMode = null;
                        _chaosCts = null;
                    }
                }
            }

            /// <inheritdoc/>
            public int InjectErrorResponseRate { get; set; }

            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="logStatus"></param>
            /// <param name="nodes"></param>
            /// <param name="logger"></param>
            public Server(bool logStatus, IEnumerable<INodeManagerFactory> nodes,
                ILogger logger)
            {
                _logger = logger;
                _logStatus = logStatus;
                _nodes = nodes;
            }

            /// <summary>
            /// Create configuration
            /// </summary>
            public static ApplicationConfiguration CreateServerConfiguration(string curDir,
                IEnumerable<int> ports, string hostName, IEnumerable<string> alternativeAddresses,
                string path, string pkiRootPath, string certStoreType, bool enableDiagnostics = false,
                Action<ServerConfiguration> configure = null)
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
                    },

                    // ...

                    new FolderConfiguration
                    {
                        CurrentDirectory = curDir
                    }
                };
                certStoreType ??= CertificateStoreType.Directory;
                if (string.IsNullOrEmpty(pkiRootPath))
                {
                    pkiRootPath = "pki";
                }
                path ??= "/UA/SampleServer";
                if (path.Length > 0 && !path.StartsWith("/", StringComparison.Ordinal))
                {
                    path = "/" + path;
                }
                var configuration = new ApplicationConfiguration
                {
                    ApplicationName = "UA Core Sample Server",
                    ApplicationType = ApplicationType.Server,
                    ApplicationUri = $"urn:{hostName ?? Utils.GetHostName()}:OPCFoundation:CoreSampleServer",
                    Extensions = [.. extensions.Select(XmlElementEx.SerializeObject)],

                    ProductUri = "http://opcfoundation.org/UA/SampleServer",
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = certStoreType,
                            StorePath = $"{pkiRootPath}/own",
                            SubjectName = "UA Core Sample Server"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = certStoreType,
                            StorePath = $"{pkiRootPath}/trusted"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = certStoreType,
                            StorePath = $"{pkiRootPath}/issuer"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = certStoreType,
                            StorePath = $"{pkiRootPath}/rejected"
                        },
                        MinimumCertificateKeySize = 1024,
                        RejectSHA1SignedCertificates = false,
                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true,
                        RejectUnknownRevocationStatus = true
                    },
                    TransportConfigurations = [],
                    TransportQuotas = new TransportQuotas
                    {
                        SecurityTokenLifetime = 60 * 60 * 1000,
                        ChannelLifetime = 300 * 1000,
                        MaxBufferSize = (64 * 1024) - 1,
                        MaxMessageSize = 4 * 1024 * 1024,
                        MaxArrayLength = (64 * 1024) - 1,
                        MaxByteStringLength = 1024 * 1024,
                        MaxStringLength = (128 * 1024) - 256,
                        OperationTimeout = 120 * 1000
                    },
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

                        ReverseConnect = new ReverseConnectServerConfiguration
                        {
                            ConnectInterval = 1000,
                            ConnectTimeout = 120000,
                            RejectTimeout = 120000
                        },

                        NodeManagerSaveFile = "nodes.xml",
                        DiagnosticsEnabled = enableDiagnostics,
                        ShutdownDelay = 0,

                        // Runtime configuration
                        BaseAddresses = [.. ports
                            .Distinct()
                            .Select(p => $"opc.tcp://{hostName ?? "localhost"}:{p}{path}")],
                        AlternateBaseAddresses = alternativeAddresses == null ? null :
                            [.. alternativeAddresses.Distinct().SelectMany(e => ports
                                .Distinct()
                                .Select(p => $"opc.tcp://{e}:{p}{path}"))],
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

                        MaxSessionCount = 30,
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
                        MaxPublishRequestCount = 8,
                        MaxSubscriptionCount = 30,
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

            private NodeId[] Sessions => CurrentInstance.SessionManager
                .GetSessions()
                .Select(s => s.Id)
                .ToArray();

            /// <inheritdoc/>
            public void CloseSessions(bool deleteSubscriptions = false)
            {
                foreach (var session in Sessions)
                {
                    CurrentInstance.CloseSession(null, session, deleteSubscriptions);
                }
            }

            private uint[] Subscriptions => CurrentInstance.SubscriptionManager
                .GetSubscriptions()
                .Select(s => s.Id)
                .ToArray();

            /// <inheritdoc/>
            public void CloseSubscriptions(bool notifyExpiration = false)
            {
                foreach (var subscription in Subscriptions)
                {
                    CloseSubscription(subscription, notifyExpiration);
                }
            }

            /// <inheritdoc/>
            public void CloseSubscription(uint subscriptionId, bool notifyExpiration)
            {
                if (notifyExpiration)
                {
                    NotifySubscriptionExpiration(subscriptionId);
                }
                CurrentInstance.DeleteSubscription(subscriptionId);
            }

            /// <inheritdoc/>
            public void NotifySubscriptionExpiration(uint subscriptionId)
            {
                try
                {
                    var subscription = CurrentInstance.SubscriptionManager
                        .GetSubscriptions()
                        .FirstOrDefault(s => s.Id == subscriptionId);
                    if (subscription != null)
                    {
                        var expireMethod = typeof(SubscriptionManager).GetMethod("SubscriptionExpired",
                            BindingFlags.NonPublic | BindingFlags.Instance);
                        expireMethod?.Invoke(
                            CurrentInstance.SubscriptionManager, new object[] { subscription });
                    }
                }
                catch
                {
                    // Nothing to do
                }
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
                    .Select(n => n.Create(server, configuration))
                    .ToArray();

                _plc = nodeManagers.OfType<Plc.PlcNodeManager>().FirstOrDefault();
                return new MasterNodeManager(server, configuration, null, nodeManagers);
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
                _lastEventTime = DateTime.UtcNow;
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
                    if (DateTime.UtcNow - _lastEventTime > TimeSpan.FromMilliseconds(6000))
                    {
                        _lastEventTime = DateTime.UtcNow;
                        foreach (var session in CurrentInstance.SessionManager.GetSessions())
                        {
                            LogSessionStatus(session, "-Status-", _lastEventTime);
                        }
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
            private void LogSessionStatus(Session session, string reason, DateTime? lastContact = null)
            {
                lock (session.DiagnosticsLock)
                {
                    if (lastContact.HasValue)
                    {
                        _logger.SessionLastContact(reason, session.SessionDiagnostics.SessionName, lastContact.Value);
                    }
                    else
                    {
                        _logger.SessionStatus(reason, session.SessionDiagnostics.SessionName,
                            session.Identity.DisplayName ?? "session",
                            session.Id);
                    }
                }
            }
            /// <summary>
            /// Creates the objects used to validate the user identity tokens supported by the server.
            /// </summary>
            /// <param name="configuration"></param>
            private void CreateUserIdentityValidators(ApplicationConfiguration configuration)
            {
                for (var ii = 0; ii < configuration.ServerConfiguration.UserTokenPolicies.Count; ii++)
                {
                    var policy = configuration.ServerConfiguration.UserTokenPolicies[ii];

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

                if (StringComparer.OrdinalIgnoreCase.Equals(userName, "test") && password == "test")
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

            /// <summary>
            /// Chaos monkey mode
            /// </summary>
            /// <param name="ct"></param>
            /// <returns></returns>
#pragma warning disable CA5394 // Do not use insecure randomness
            private async Task ChaosAsync(CancellationToken ct)
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(10, 60)), ct).ConfigureAwait(false);
                        Console.WriteLine("===================\nCHAOS MONKEY TIME\n===================");
                        Console.WriteLine($"{Subscriptions.Length} subscriptions in {Sessions.Length} sessions!");
                        switch (Random.Shared.Next(0, 16))
                        {
                            case 0:
                                Console.WriteLine("!!!!! Closing all sessions and associated subscriptions. !!!!!!");
                                CloseSessions(true);
                                break;
                            case 1:
                                Console.WriteLine("!!!!! Closing all sessions. !!!!! ");
                                CloseSessions(false);
                                break;
                            case 2:
                                Console.WriteLine("!!!!! Notifying expiration and closing all subscriptions. !!!!! ");
                                CloseSubscriptions(true);
                                break;
                            case 3:
                                Console.WriteLine("!!!!! Closing all subscriptions. !!!!!");
                                CloseSubscriptions(false);
                                break;
                            case > 3 and < 8:
                                var sessions = Sessions;
                                if (sessions.Length == 0)
                                {
                                    break;
                                }

                                var session = sessions[Random.Shared.Next(0, sessions.Length)];
                                var delete = Random.Shared.Next() % 2 == 0;
                                Console.WriteLine($"!!!!! Closing session {session} (delete subscriptions:{delete}). !!!!!");
                                CurrentInstance.CloseSession(null, session, delete);
                                break;
                            case > 10 and < 13:
                                if (InjectErrorResponseRate != 0)
                                {
                                    break;
                                }
                                InjectErrorResponseRate = Random.Shared.Next(1, 20);
                                var duration = TimeSpan.FromSeconds(Random.Shared.Next(10, 150));
                                Console.WriteLine($"!!!!! Injecting random errors every {InjectErrorResponseRate} " +
                                    $"responses for {duration.TotalMicroseconds} ms. !!!!!");
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await Task.Delay(duration, ct).ConfigureAwait(false);
                                    }
                                    catch (OperationCanceledException) { }
                                    InjectErrorResponseRate = 0;
                                }, ct);
                                break;
                            default:
                                var subscriptions = Subscriptions;
                                if (subscriptions.Length == 0)
                                {
                                    break;
                                }

                                var subscription = subscriptions[Random.Shared.Next(0, subscriptions.Length)];
                                var notify = Random.Shared.Next() % 2 == 0;
                                Console.WriteLine($"!!!!! Closing subscription {subscription} (notify:{notify}). !!!!!");
                                CloseSubscription(subscription, notify);
                                break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Nothing to do
                }
            }

            private static readonly StatusCode[] kStatusCodes =
            {
                StatusCodes.BadCertificateInvalid,
                StatusCodes.BadAlreadyExists,
                StatusCodes.BadNoSubscription,
                StatusCodes.BadSecureChannelClosed,
                StatusCodes.BadSessionClosed,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadSessionIdInvalid,
                StatusCodes.BadConnectionClosed,
                StatusCodes.BadServerHalted,
                StatusCodes.BadNotConnected,
                StatusCodes.BadNoCommunication,
                StatusCodes.BadRequestInterrupted,
                StatusCodes.BadRequestInterrupted,
                StatusCodes.BadRequestInterrupted
            };
            protected override OperationContext ValidateRequest(RequestHeader requestHeader, RequestType requestType)
            {
                if (InjectErrorResponseRate != 0)
                {
                    var dice = Random.Shared.Next(0, kStatusCodes.Length * InjectErrorResponseRate);
                    if (dice < kStatusCodes.Length)
                    {
                        Console.WriteLine("--------> Injecting error: {0}", kStatusCodes[dice]);
                        throw new ServiceResultException(kStatusCodes[dice]);
                    }
                }
                return base.ValidateRequest(requestHeader, requestType);
            }

#pragma warning restore CA5394 // Do not use insecure randomness
            private readonly ILogger _logger;
            private readonly bool _logStatus;
            private readonly IEnumerable<INodeManagerFactory> _nodes;
            private Task _statusLogger;
            private DateTime _lastEventTime;
            private CancellationTokenSource _cts;
            private ICertificateValidator _certificateValidator;
            private CancellationTokenSource _chaosCts;
            private Task _chaosMode;
#pragma warning disable CA2213 // Disposable fields should be disposed
            private Plc.PlcNodeManager _plc;
#pragma warning restore CA2213 // Disposable fields should be disposed
            private const string kServerNamespaceUri = "http://opcfoundation.org/UA/Sample/";
        }

        private readonly ILogger _logger;
        private readonly IEnumerable<INodeManagerFactory> _nodes;
        private readonly string _tempPath;
    }

    /// <summary>
    /// Source-generated logging definitions for Server
    /// </summary>
    internal static partial class ServerLogging
    {
        private const int EventClass = 150;

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
            Message = "{Reason,9}:{SessionName,20}:Last Event:{LastEvent:HH:mm:ss}")]
        public static partial void SessionLastContact(this ILogger logger,
            string reason, string sessionName, DateTime lastEvent);

        [LoggerMessage(EventId = EventClass + 6, Level = LogLevel.Information,
            Message = "{Reason,9}:{SessionName,20}:{DisplayName,20}:{SessionId}")]
        public static partial void SessionStatus(this ILogger logger,
            string reason, string sessionName, string displayName, NodeId sessionId);
    }
}
