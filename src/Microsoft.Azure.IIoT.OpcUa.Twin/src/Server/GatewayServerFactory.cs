// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Server {
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// Gateway server factory
    /// </summary>
    public class GatewayServerFactory : IServerFactory {

        /// <summary>
        /// Server factory
        /// </summary>
        /// <param name="encoder"></param>
        /// <param name="twin"></param>
        /// <param name="browser"></param>
        /// <param name="logger"></param>
        public GatewayServerFactory(IVariantEncoder encoder,
            INodeServices<string> twin, IBrowseServices<string> browser,
            ILogger logger) {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _twin = twin ?? throw new ArgumentNullException(nameof(twin));
        }

        /// <inheritdoc/>
        public ApplicationConfiguration CreateServer(IEnumerable<int> ports,
            out ServerBase server) {
            server = new Server(_encoder, _twin, _browser, _logger);
            return Server.CreateServerConfiguration(ports);
        }

        /// <inheritdoc/>
        public class Server : GatewayServer {

            /// <summary>
            /// Create server
            /// </summary>
            /// <param name="encoder"></param>
            /// <param name="twin"></param>
            /// <param name="browser"></param>
            /// <param name="logger"></param>
            public Server(IVariantEncoder encoder, INodeServices<string> twin,
                IBrowseServices<string> browser, ILogger logger) :
                base(null, null, twin, browser, encoder, logger) {
            }

            /// <summary>
            /// Create server configuration
            /// </summary>
            /// <param name="ports"></param>
            /// <returns></returns>
            public static ApplicationConfiguration CreateServerConfiguration(
                IEnumerable<int> ports) {
                return new ApplicationConfiguration {
                    ApplicationName = "Opc Twin Gateway Server",
                    ApplicationType = ApplicationType.ClientAndServer,
                    ApplicationUri =
                $"urn:{Utils.GetHostName()}:Microsoft:OpcTwinGatewayServer",

                    ProductUri = "http://opcfoundation.org/UA/SampleServer",
                    SecurityConfiguration = new SecurityConfiguration {
                        ApplicationCertificate = new CertificateIdentifier {
                            StoreType = "Directory",
                            StorePath =
                "OPC Foundation/CertificateStores/MachineDefault",
                            SubjectName = "Opc Twin Gateway Server"
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
                            "Local Discovery Server Profile"
                        },
                        ServerCapabilities = new StringCollection {
                            "LDS"
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
                            .Select(p => $"opc.tcp://localhost:{p}/ua/twin")),

                        SecurityPolicies = new ServerSecurityPolicyCollection {
                            new ServerSecurityPolicy {
                                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                                SecurityPolicyUri =
                "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256"
                            }
                        },
                        UserTokenPolicies = new UserTokenPolicyCollection {
#if !NO_ANON
                            new UserTokenPolicy {
                                TokenType = UserTokenType.Anonymous,
                                SecurityPolicyUri =
                "http://opcfoundation.org/UA/SecurityPolicy#None"
                            },
#endif
                            new UserTokenPolicy {
                                TokenType = UserTokenType.UserName
                            },
                            new UserTokenPolicy {
                                TokenType = UserTokenType.IssuedToken
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
                        MaxRegistrationInterval = 0,
                        RegistrationEndpoint = null
                    },
                    TraceConfiguration = new TraceConfiguration {
                        TraceMasks = 1
                    }
                };
            }
        }

        private readonly IVariantEncoder _encoder;
        private readonly ILogger _logger;
        private readonly IBrowseServices<string> _browser;
        private readonly INodeServices<string> _twin;
    }
}
