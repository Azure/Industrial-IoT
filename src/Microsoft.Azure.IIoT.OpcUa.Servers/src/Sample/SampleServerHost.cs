// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Servers.Sample {
    using MemoryBuffer;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Utils;
    using Opc.Ua;
    using Opc.Ua.Configuration;
    using Opc.Ua.Sample;
    using Opc.Ua.Server;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    public class SampleServerHost : IServerHost {

        /// <inheritdoc/>
        public bool AutoAccept { get; set; }

        /// <summary>
        /// Create server console host
        /// </summary>
        /// <param name="logger"></param>
        public SampleServerHost(ILogger logger) {
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task StopAsync() {
            if (_server != null) {
                try {
                    await _lock.WaitAsync();
                    if (_server != null) {
                        _logger.Info($"Stopping server.", () => { });
                        try {
                            _cts.Cancel();
                            await _statusLogger;
                            _server.Stop();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception se) {
                            _logger.Error("Server not cleanly stopped.",
                                () => se);
                        }
                        _server.Dispose();
                    }
                    _logger.Info($"Server stopped.", () => { });
                }
                catch (Exception ce) {
                    _logger.Error("Stopping server caused exception.",
                        () => ce);
                }
                finally {
                    _server = null;
                    _lock.Release();
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartAsync(IEnumerable<int> ports) {
            if (_server == null) {
                try {
                    await _lock.WaitAsync();
                    if (_server == null) {
                        await StartServerInternal(ports);
                        return;
                    }
                }
                catch (Exception ex) {
                    _server?.Dispose();
                        _server = null;
                    throw ex;
                }
                finally {
                    _lock.Release();
                }
            }
            throw new InvalidOperationException("Already started");
        }

        /// <inheritdoc/>
        public void Dispose() => StopAsync().Wait();

        /// <summary>
        /// Start server
        /// </summary>
        /// <param name="ports"></param>
        /// <returns></returns>
        private async Task StartServerInternal(IEnumerable<int> ports) {
            _logger.Info("Starting server...", () => { });
            ApplicationInstance.MessageDlg = new DummyDialog();

            var application = new ApplicationInstance(
                ApplicationInstance.FixupAppConfig(CreateServerConfiguration(ports)));
            var config = application.ApplicationConfiguration;

            // Create cert
            var cert = CertificateFactory.CreateCertificate(
                config.SecurityConfiguration.ApplicationCertificate.StoreType,
                config.SecurityConfiguration.ApplicationCertificate.StorePath,
                null, config.ApplicationUri, config.ApplicationName,
                config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                null, CertificateFactory.defaultKeySize,
                DateTime.UtcNow - TimeSpan.FromDays(1),
                CertificateFactory.defaultLifeTime,
                CertificateFactory.defaultHashSize,
                false, null, null);

            await config.Validate(ApplicationType.Server);
            config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;
            config.ApplicationUri = Utils.GetApplicationUriFromCertificate(cert);
            config.CertificateValidator.CertificateValidation += (v, e) => {
                if (e.Error.StatusCode ==
                    StatusCodes.BadCertificateUntrusted) {
                    e.Accept = AutoAccept;

                    _logger.Info((e.Accept ? "Accepted" : "Rejected") +
                        $" Certificate {e.Certificate.Subject}",
                        () => { });
                }
            };

            // check the application certificate.
            var haveAppCertificate =
                await application.CheckApplicationInstanceCertificate(false, 0);
            if (!haveAppCertificate) {
                throw new Exception(
                    "Application instance certificate invalid!");
            }

            // start the server.
            _server = new SampleServer();
            await application.Start(_server);

            // start the status thread
            _cts = new CancellationTokenSource();
            _statusLogger = Task.Run(() => LogStatusAsync(_cts.Token));

            // print notification on session events
            _server.CurrentInstance.SessionManager.SessionActivated += OnEvent;
            _server.CurrentInstance.SessionManager.SessionClosing += OnEvent;
            _server.CurrentInstance.SessionManager.SessionCreated += OnEvent;
            _logger.Info("Server started.", () => { });
        }

        /// <summary>
        /// Create server configuration
        /// </summary>
        /// <returns></returns>
        private static ApplicationConfiguration CreateServerConfiguration(
            IEnumerable<int> ports) {
            return new ApplicationConfiguration {
                ApplicationName = "UA Core Sample Server",
                ApplicationType = ApplicationType.Server,
                ApplicationUri =
            $"urn:{Utils.GetHostName()}:OPCFoundation:CoreSampleServer",

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
                    MaxRegistrationInterval = 30000,
                    MaxPublishRequestCount = 20,
                    MaxSubscriptionCount = 100,
                    MaxEventQueueSize = 10000,
                    MinSubscriptionLifetime = 10000,

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
                    MaxTrustListSize = 0

                    // Do not register with LDS
                },
                TraceConfiguration = new TraceConfiguration {
                    TraceMasks = 1
                },

                // Address space configuration
                Extensions = new XmlElementCollection {
                    XmlElementEx.SerializeObject(
                        new MemoryBufferConfiguration {
                            Buffers = new MemoryBufferInstanceCollection {
                                new MemoryBufferInstance {
                                    Name = "UInt32",
                                    TagCount = 100,
                                    DataType = "UInt32"
                                },
                                new MemoryBufferInstance {
                                    Name = "Double",
                                    TagCount = 100,
                                    DataType = "Double"
                                },
                            }
                        })
                }
            };
        }

        /// <summary>
        /// Handle session event by logging status
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        private void OnEvent(Session session, SessionEventReason reason) {
            lastEventTime = DateTime.UtcNow;
            LogSessionStatus(session, reason.ToString());
        }

        /// <summary>
        /// Continously log session status if not logged during events
        /// </summary>
        /// <returns></returns>
        private async Task LogStatusAsync(CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                if (DateTime.UtcNow - lastEventTime > TimeSpan.FromMilliseconds(6000)) {
                    var sessions = _server.CurrentInstance.SessionManager.GetSessions();
                    for (var ii = 0; ii < sessions.Count; ii++) {
                        var session = sessions[ii];
                        LogSessionStatus(session, "-Status-", true);
                    }
                    lastEventTime = DateTime.UtcNow;
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
                _logger.Info(item, () => { });
            }
        }

        private class DummyDialog : IApplicationMessageDlg {
            /// <inheritdoc/>
            public override void Message(string text, bool ask) { }
            /// <inheritdoc/>
            public override Task<bool> ShowAsync() => Task.FromResult(true);
        }

        private SampleServer _server;
        private Task _statusLogger;
        private DateTime lastEventTime;
        private CancellationTokenSource _cts;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1);
    }
}
