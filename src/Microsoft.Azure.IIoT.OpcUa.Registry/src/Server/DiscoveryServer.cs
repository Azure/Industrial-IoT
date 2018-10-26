// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Server {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc/>
    public class DiscoveryServer : StandardServer {

        /// <summary>
        /// Create server
        /// </summary>
        /// <param name="logStatus"></param>
        /// <param name="logger"></param>
        public DiscoveryServer(bool logStatus, ILogger logger) {
            _logger = logger;
            _logStatus = logStatus;
        }

        /// <inheritdoc/>
        public override ResponseHeader FindServersOnNetwork(RequestHeader requestHeader,
            uint startingRecordId, uint maxRecordsToReturn, StringCollection serverCapabilityFilter,
            out DateTime lastCounterResetTime, out ServerOnNetworkCollection servers) {

            lastCounterResetTime = DateTime.MinValue;
            servers = null;

            ValidateRequest(requestHeader);

            lock (Lock) {
                // if necessary fill and iterate through the registry cache

                // Each time the Discovery Server creates or updates a record in its cache it
                // shall assign a monotonically increasing identifier to the record. This
                // allows Clients to request records in batches by specifying the identifier
                // for the last record received in the last call to FindServersOnNetwork.
                // To support this the Discovery Server shall return records in numerical
                // order starting from the lowest record identifier. The Discovery Server
                // shall also return the last time the counter was reset for example due
                // to a restart of the Discovery Server.If a Client detects that this time
                // is more recent than the last time the Client called the Service it shall
                // call the Service again with a startingRecordId of 0.

                //   servers.Add(application);
            }

            return CreateResponse(requestHeader, StatusCodes.BadServiceUnsupported);
        }

        /// <inheritdoc/>
        public override ResponseHeader FindServers(RequestHeader requestHeader,
            string endpointUrl, StringCollection localeIds, StringCollection serverUris,
            out ApplicationDescriptionCollection servers) {
            var response = base.FindServers(requestHeader, endpointUrl, localeIds,
                serverUris, out servers);
            if (StatusCode.IsGood(response.ServiceResult)) {

                // Gateway mode.
                // Add all servers currently accessible (with activated endpoints)

                //   servers.Add(application);
            }
            return response;
        }

        /// <summary>
        /// Create server configuration
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        public static ApplicationConfiguration CreateServerConfiguration(
            IEnumerable<int> ports) {
            return new ApplicationConfiguration {
                ApplicationName = "Opc Twin Registry Server",
                ApplicationType = ApplicationType.Server,
                ApplicationUri =
            $"urn:{Utils.GetHostName()}:Microsoft:OpcTwinRegistry",

                ProductUri = "http://opcfoundation.org/UA/SampleServer",
                SecurityConfiguration = new SecurityConfiguration {
                    ApplicationCertificate = new CertificateIdentifier {
                        StoreType = "Directory",
                        StorePath =
            "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "Opc Twin Registry Server"
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
                        .Select(p => $"opc.tcp://localhost:{p}/ua/registry")),

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

        /// <inheritdoc/>
        protected override void OnServerStopping() {
            _cts.Cancel();
            if (_statusLogger != null) {
                _statusLogger.Wait();
            }
        }

        /// <inheritdoc/>
        protected override void OnServerStarting(ApplicationConfiguration configuration) {
            base.OnServerStarting(configuration);
            // start the status thread
            _cts = new CancellationTokenSource();
            if (_logStatus) {
                _statusLogger = Task.Run(() => LogStatusAsync(_cts.Token));

                // print notification on session events
                CurrentInstance.SessionManager.SessionActivated += OnEvent;
                CurrentInstance.SessionManager.SessionClosing += OnEvent;
                CurrentInstance.SessionManager.SessionCreated += OnEvent;
            }
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
}
