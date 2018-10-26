// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Sample {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Sample;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Sample server factory
    /// </summary>
    public class SampleServerFactory : IServerFactory {

        /// <summary>
        /// Whether to log status
        /// </summary>
        public bool LogStatus { get; set; }

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="logger"></param>
        public SampleServerFactory(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            out ServerBase server) {
            server = new Server(LogStatus, _logger);
            return Server.CreateServerConfiguration(ports);
        }

        /// <inheritdoc/>
        class Server : SampleServer {

            /// <inheritdoc/>
            class MemoryBufferNodeManager : MemoryBuffer.MemoryBufferNodeManager {
                /// <inheritdoc/>
                public MemoryBufferNodeManager(IServerInternal server,
                    ApplicationConfiguration configuration) :
                    base(server, configuration) {
                }

                /// <inheritdoc/>
                protected override NodeStateCollection LoadPredefinedNodes(
                    ISystemContext context) {
                    var predefinedNodes = new NodeStateCollection();
                    predefinedNodes.LoadFromBinaryResource(context,
            typeof(TestData.TestDataNodeManager).Assembly.GetName().Name +
            ".Sample.External.MemoryBuffer.MemoryBuffer.PredefinedNodes.uanodes",
                        typeof(TestData.TestDataNodeManager).Assembly, true);
                    return predefinedNodes;
                }
            }

            /// <inheritdoc/>
            class TestDataNodeManager : TestData.TestDataNodeManager {
                /// <inheritdoc/>
                public TestDataNodeManager(IServerInternal server,
                    ApplicationConfiguration configuration) :
                    base(server, configuration) {
                }

                /// <inheritdoc/>
                protected override NodeStateCollection LoadPredefinedNodes(
                    ISystemContext context) {
                    var predefinedNodes = new NodeStateCollection();
                    predefinedNodes.LoadFromBinaryResource(context,
            typeof(TestData.TestDataNodeManager).Assembly.GetName().Name +
            ".Sample.External.TestData.TestData.PredefinedNodes.uanodes",
                        typeof(TestData.TestDataNodeManager).Assembly, true);
                    return predefinedNodes;
                }
            }

            /// <inheritdoc/>
            class BoilerNodeManager : Boiler.BoilerNodeManager {
                /// <inheritdoc/>
                public BoilerNodeManager(IServerInternal server,
                    ApplicationConfiguration configuration) :
                    base(server, configuration) {
                }

                /// <inheritdoc/>
                protected override NodeStateCollection LoadPredefinedNodes(
                    ISystemContext context) {
                    var predefinedNodes = new NodeStateCollection();
                    predefinedNodes.LoadFromBinaryResource(context,
            typeof(TestData.TestDataNodeManager).Assembly.GetName().Name +
            ".Sample.External.Boiler.Boiler.PredefinedNodes.uanodes",
                        typeof(TestData.TestDataNodeManager).Assembly, true);
                    return predefinedNodes;
                }
            }

            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="logStatus"></param>
            /// <param name="logger"></param>
            public Server(bool logStatus, ILogger logger) {
                _logger = logger;
                _logStatus = logStatus;
            }

            /// <summary>
            /// Create server configuration
            /// </summary>
            /// <param name="ports"></param>
            /// <returns></returns>
            public static ApplicationConfiguration CreateServerConfiguration(
                IEnumerable<int> ports) {
                var extensions = new List<object> {
                    new MemoryBuffer.MemoryBufferConfiguration {
                        Buffers = new MemoryBuffer.MemoryBufferInstanceCollection {
                            new MemoryBuffer.MemoryBufferInstance {
                                Name = "UInt32",
                                TagCount = 100,
                                DataType = "UInt32"
                            },
                            new MemoryBuffer.MemoryBufferInstance {
                                Name = "Double",
                                TagCount = 100,
                                DataType = "Double"
                            },
                        }
                    }
                };
                return new ApplicationConfiguration {
                    ApplicationName = "UA Core Sample Server",
                    ApplicationType = ApplicationType.Server,
                    ApplicationUri =
                $"urn:{Utils.GetHostName()}:OPCFoundation:CoreSampleServer",

                    Extensions = new XmlElementCollection(
                        extensions.Select(XmlElementEx.SerializeObject)),

                    ProductUri = "http://opcfoundation.org/UA/SampleServer",
                    SecurityConfiguration = new SecurityConfiguration {
                        ApplicationCertificate = new CertificateIdentifier {
                            StoreType = "Directory",
                            StorePath =
                "OPC Foundation/CertificateStores/MachineDefault",
                            SubjectName = "UA Core Sample Server"
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
                        AutoAcceptUntrustedCertificates = false
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas {
                        OperationTimeout = 120000,
                        MaxStringLength = ushort.MaxValue,
                        MaxByteStringLength = ushort.MaxValue * 16,
                        MaxArrayLength = ushort.MaxValue,
                        MaxBufferSize = ushort.MaxValue,
                        ChannelLifetime = 300000,
                        SecurityTokenLifetime = 3600000
                    },
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
                                SecurityPolicyUri =
                "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
                            },
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                                SecurityPolicyUri =
                "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
                            }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection {
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Anonymous,
                                SecurityPolicyUri =
                "http://opcfoundation.org/UA/SecurityPolicy#None"
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
            protected override MasterNodeManager CreateMasterNodeManager(
                IServerInternal server, ApplicationConfiguration configuration) {
                _logger.Info("Creating the Node Managers.");
                var nodeManagers = new List<INodeManager> {

                    new TestDataNodeManager(server, configuration),
                    new MemoryBufferNodeManager(server, configuration),
                    new BoilerNodeManager(server, configuration),

                    // ...
                };
                return new MasterNodeManager(server, configuration, null,
                    nodeManagers.ToArray());
            }

            /// <inheritdoc/>
            protected override void OnServerStopping() {
                _cts.Cancel();
                if (_statusLogger != null) {
                    _statusLogger.Wait();
                }
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
                        var sessions = CurrentInstance.SessionManager.GetSessions();
                        for (var ii = 0; ii < sessions.Count; ii++) {
                            var session = sessions[ii];
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
                    _logger.Info(item);
                }
            }

            private readonly ILogger _logger;
            private readonly bool _logStatus;
            private Task _statusLogger;
            private DateTime _lastEventTime;
            private CancellationTokenSource _cts;
        }

        private readonly ILogger _logger;
    }
}
