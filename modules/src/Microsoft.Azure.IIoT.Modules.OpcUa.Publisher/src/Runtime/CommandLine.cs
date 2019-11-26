// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Module.Client.Runtime;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Extensions.Configuration;
    using Mono.Options;
    using Opc.Ua;
    using Serilog.Events;
    using System.Linq;
    using System;
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using System.Runtime.CompilerServices;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Cli handling
    /// </summary>
    public static class CommandLine {
        /// <summary>
        /// Add options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="args"></param>
        public static IConfigurationBuilder AddLegacyPublisherCommandLine(this IConfigurationBuilder builder, string[] args) {
            return builder.AddInMemoryCollection(new LegacyCliOptions(args));
        }

        /// <summary>
        /// Parse the command line options and enter them into the dictionary using the
        /// new property names, which are then picked up by the configuration implementation
        /// </summary>
        public class LegacyCliOptions : Dictionary<string, string>, IAgentConfigProvider, IEngineConfiguration {
            
            /// <summary>
            /// 
            /// </summary>
            public LegacyCliOptions() {

            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="config"></param>
            public LegacyCliOptions(IConfiguration config) {
                foreach (var item in config.GetChildren()) {
                    this[item.Key] = item.Value;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            public bool RunInLegacyMode => System.IO.File.Exists(PublishedNodesFile);

            // TODO: Figure out which are actually supported in the new publisher implementation

            /// <summary>
            /// 
            /// </summary>
            public string Site => this[kPublisherSite];

            /// <summary>
            /// 
            /// </summary>
            public string PublishedNodesFile => this.ContainsKey(kPublisherNodeConfigurationFilename) ? this[kPublisherNodeConfigurationFilename] : DefaultPublishedNodesFilename;
            
            /// <summary>
            /// 
            /// </summary>
            public TimeSpan SessionConnectWait => TimeSpan.Parse(this[kSessionConnectWaitSec]);
            
            /// <summary>
            /// 
            /// </summary>
            public TimeSpan? DefaultHeartbeatInterval => this.ContainsKey(kHeartbeatIntervalDefault) ? (TimeSpan?)TimeSpan.Parse(this[kHeartbeatIntervalDefault]) : null;

            /// <summary>
            /// 
            /// </summary>
            public bool DefaultSkipFirst => Boolean.Parse(this[kSkipFirstDefault]);

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan DefaultSamplingInterval => TimeSpan.Parse(this[kOpcSamplingInterval]);

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan DefaultPublishingInterval => TimeSpan.Parse(this[kOpcPublishingInterval]);

            /// <summary>
            /// 
            /// </summary>
            public bool FetchOpcNodeDisplayName => Boolean.Parse(this[kFetchOpcNodeDisplayName]);

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan? DiagnosticsInterval => this.ContainsKey(kDiagnosticsInterval) ? (TimeSpan?)TimeSpan.Parse(this[kDiagnosticsInterval]) : null;
            
            /// <summary>
            /// 
            /// </summary>
            public TimeSpan LogFileFlushTimeSpan => TimeSpan.Parse(this[kLogFileFlushTimeSpanSec]);

            /// <summary>
            /// 
            /// </summary>
            private string LogFilename => this[kLogFileName];

            /// <summary>
            /// 
            /// </summary>
            private TransportOption Transport => Enum.Parse<TransportOption>(this[kHubTransport]);

            /// <summary>
            /// 
            /// </summary>
            public string EdgeHubConnectionString => this[kEdgeHubConnectionString];

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan OperationTimeout => TimeSpan.Parse(this[kOpcOperationTimeout]);

            /// <summary>
            /// 
            /// </summary>
            public long MaxStringLength => long.Parse(this[kOpcMaxStringLength]);

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan SessionCreationTimeout => TimeSpan.Parse(this[kOpcSessionCreationTimeout]);

            /// <summary>
            /// 
            /// </summary>
            public TimeSpan KeepAliveInterval => TimeSpan.Parse(this[kOpcKeepAliveIntervalInSec]);

            /// <summary>
            /// 
            /// </summary>
            public uint MaxKeepAliveCount => uint.Parse(this[kOpcKeepAliveDisconnectThreshold]);

            /// <summary>
            /// 
            /// </summary>
            public bool TrustSelf => Boolean.Parse(this[kTrustMyself]);

            /// <summary>
            /// 
            /// </summary>
            public bool AutoAcceptUntrustedCertificates => Boolean.Parse(kAutoAcceptCerts);

            /// <summary>
            /// 
            /// </summary>
            public string ApplicationCertificateStoreType => this[kOpcOwnCertStoreType];

            /// <summary>
            /// 
            /// </summary>
            public string ApplicationCertificateStorePath => this[kOpcOwnCertStorePath];

            /// <summary>
            /// 
            /// </summary>
            public string TrustedPeerCertificatesPath => this[kOpcTrustedCertStorePath];

            /// <summary>
            /// 
            /// </summary>
            public string RejectedCertificateStorePath => this[kOpcRejectedCertStorePath];

            /// <summary>
            /// 
            /// </summary>
            public string TrustedIssuerCertificatesPath => this[kOpcIssuerCertStorePath];

            /// <summary>
            /// 
            /// </summary>
            public AgentConfigModel Config => new AgentConfigModel() {
                AgentId = "Singleton",
                Capabilities = new Dictionary<string, string>(),
                HeartbeatInterval = this.DefaultHeartbeatInterval,
                JobCheckInterval = TimeSpan.Zero,
                JobOrchestratorUrl = null,
                MaxWorkers = 1
            };

            /// <summary>
            /// 
            /// </summary>
            public int? BatchSize => 1;

            private const string DefaultPublishedNodesFilename = "publishednodes.json";

            private const string kPublisherSite = "Site";
            private const string kPublisherNodeConfigurationFilename = "PublishedNodesFile";
            private const string kSessionConnectWaitSec = "SessionConnectWait";
            private const string kHeartbeatIntervalDefault = "DefaultHeartbeatInterval";
            private const string kSkipFirstDefault = "DefaultSkipFirst";
            private const string kOpcSamplingInterval = "DefaultSamplingInterval";
            private const string kOpcPublishingInterval = "DefaultPublishingInterval";
            private const string kFetchOpcNodeDisplayName = "FetchOpcNodeDisplayName";
            private const string kDiagnosticsInterval = "DiagnosticsInterval";
            private const string kLogFileFlushTimeSpanSec = "LogFileFlushTimeSpan";
            private const string kLogFileName = "LogFileName";

            private const string kHubTransport =
                ModuleConfig.TransportKey;
            private const string kEdgeHubConnectionString =
                ModuleConfig.EdgeHubConnectionStringKey;

            private const string kOpcOperationTimeout =
                TransportQuotaConfig.OperationTimeoutKey;
            private const string kOpcMaxStringLength =
                TransportQuotaConfig.MaxStringLengthKey;
            private const string kOpcSessionCreationTimeout =
                ClientServicesConfig.DefaultSessionTimeoutKey;
            private const string kOpcKeepAliveIntervalInSec =
                ClientServicesConfig.KeepAliveIntervalKey;
            private const string kOpcKeepAliveDisconnectThreshold =
                ClientServicesConfig.MaxKeepAliveCountKey;

            private const string kTrustMyself =
                "TrustSelf";
            private const string kAutoAcceptCerts =
                SecurityConfig.AutoAcceptUntrustedCertificatesKey;
            private const string kOpcOwnCertStoreType =
                SecurityConfig.ApplicationCertificateStoreTypeKey;
            private const string kOpcOwnCertStorePath =
                SecurityConfig.ApplicationCertificateStorePathKey;
            private const string kOpcTrustedCertStorePath =
                SecurityConfig.TrustedPeerCertificatesPathKey;
            private const string kOpcRejectedCertStorePath =
                SecurityConfig.RejectedCertificateStorePathKey;
            private const string kOpcIssuerCertStorePath =
                SecurityConfig.TrustedIssuerCertificatesPathKey;

            /// <summary>
            /// 
            /// </summary>
            public event ConfigUpdatedEventHandler OnConfigUpdated;

            /// <summary>
            /// Parse arguments and set values in the environment the way the new configuration expects it.
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            public LegacyCliOptions(string[] args) {

                // command line options
                var options = new Mono.Options.OptionSet {
                    // Publisher configuration options
                    { "pf|publishfile=", "the filename to configure the nodes to publish.",
                        s => this[kPublisherNodeConfigurationFilename] = s },
                    { "s|site=", "the site OPC Publisher is working in.",
                        s => this[kPublisherSite] = s },

                    { "di|diagnosticsinterval=", "Shows publisher diagnostic info at the specified interval " +
                        "in seconds (need log level info).\n-1 disables remote diagnostic log and diagnostic output",
                        (TimeSpan s) => this[kDiagnosticsInterval] = s.ToString() },
                    { "lf|logfile=", "the filename of the logfile to use.",
                        s => this[kLogFileName] = s },
                    { "lt|logflushtimespan=", "the timespan in seconds when the logfile should be flushed.",
                        (TimeSpan s) => this[kLogFileFlushTimeSpanSec] = s.ToString() },
                    { "ll|loglevel=", "the loglevel to use (allowed: fatal, error, warn, info, debug, verbose).",
                        (LogEventLevel l) => LogControl.Level.MinimumLevel = l },

                    { "ih|iothubprotocol=", "Protocol to use for communication with the hub. " +
                            $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(TransportOption)))}).",
                        (TransportOption p) => this[kHubTransport] = p.ToString() },
                    { "dc|deviceconnectionstring=", "A device or edge module connection string to use.",
                        dc => this[kEdgeHubConnectionString] = dc },
                    { "ec|edgehubconnectionstring=", "An edge module connection string to use",
                        dc => this[kEdgeHubConnectionString] = dc },

                    { "hb|heartbeatinterval=", "the publisher is using this as default value in seconds " +
                        "for the heartbeat interval setting of nodes without a heartbeat interval setting.",
                        (int i) => this[kHeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },
                    { "sf|skipfirstevent=", "the publisher is using this as default value for the skip first " +
                        "event setting of nodes without a skip first event setting.",
                        (bool b) => this[kSkipFirstDefault] = b.ToString() },

                    // Client settings
                    { "ot|operationtimeout=", "the operation timeout of the publisher OPC UA client in ms.",
                        (uint i) => this[kOpcOperationTimeout] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ol|opcmaxstringlen=", $"the max length of a string opc can transmit/receive.",
                        (uint i) => this[kOpcMaxStringLength] = i.ToString() },
                    { "oi|opcsamplinginterval=", "Default value in milliseconds to request the servers to " +
                        "sample values.",
                        (int i) => this[kOpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "op|opcpublishinginterval=", "Default value in milliseconds for the publishing interval " +
                            "setting of the subscriptions against the OPC UA server.",
                        (int i) => this[kOpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ct|createsessiontimeout=", "The timeout in seconds used when creating a session to an endpoint.",
                        (uint u) => this[kOpcSessionCreationTimeout] = TimeSpan.FromSeconds(u).ToString() },
                    { "ki|keepaliveinterval=", "The interval in seconds the publisher is sending keep alive messages " +
                            "to the OPC servers on the endpoints it is connected to.",
                        (int i) => this[kOpcKeepAliveIntervalInSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "kt|keepalivethreshold=", "specify the number of keep alive packets a server can miss, " +
                        "before the session is disconneced.",
                        (uint u) => this[kOpcKeepAliveDisconnectThreshold] = u.ToString() },
                    { "fd|fetchdisplayname=", "same as fetchname.",
                        (bool b) => this[kFetchOpcNodeDisplayName] = b.ToString() },
                    { "sw|sessionconnectwait=", "Wait time in seconds publisher is trying to connect " +
                        "to disconnected endpoints and starts monitoring unmonitored items.",
                        (int s) => this[kSessionConnectWaitSec] = TimeSpan.FromSeconds(s).ToString() },

                    // cert store options
                    { "aa|autoaccept", "the publisher trusts all servers it is establishing a connection to.",
                          b => this[kAutoAcceptCerts] = (b != null).ToString() },
                    { "to|trustowncert", "the publisher certificate is put into the trusted store automatically.",
                        t => this[kTrustMyself] = (t != null).ToString() },
                    { "at|appcertstoretype=", "the own application cert store type (allowed: Directory, X509Store).",
                        s => {
                            if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                                s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                                this[kOpcOwnCertStoreType] = s;
                                return;
                            }
                            throw new OptionException("Bad store type", "at");
                        }
                    },
                    { "ap|appcertstorepath=", "the path where the own application cert should be stored.",
                        s => this[kOpcOwnCertStorePath] = s },
                    { "tp|trustedcertstorepath=", "the path of the trusted cert store.",
                        s => this[kOpcTrustedCertStorePath] = s },
                    { "tt|trustedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "rp|rejectedcertstorepath=", "the path of the rejected cert store.",
                        s => this[kOpcRejectedCertStorePath] = s },
                    { "rt|rejectedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "ip|issuercertstorepath=", "the path of the trusted issuer cert store.",
                        s => this[kOpcIssuerCertStorePath] = s },
                    { "it|issuercertstoretype=", "Legacy - do not use.", _ => {} },

                    // Legacy unsupported
                    { "si|iothubsendinterval=", "Legacy - do not use.", _ => {} },
                    { "tc|telemetryconfigfile=", "Legacy - do not use.", _ => {} },
                    { "ic|iotcentral=", "Legacy - do not use.", _ => {} },
                    { "mq|monitoreditemqueuecapacity=", "Legacy - do not use.", _ => {} },
                    { "ns|noshutdown=", "Legacy - do not use.", _ => {} },
                    { "rf|runforever", "Legacy - do not use.", _ => {} },
                    { "ms|iothubmessagesize=", "Legacy - do not use.", _ => {} },
                    { "pn|portnum=", "Legacy - do not use.", _ => {} },
                    { "pa|path=", "Legacy - do not use.", _ => {} },
                    { "lr|ldsreginterval=", "Legacy - do not use.", _ => {} },
                    { "ss|suppressedopcstatuscodes=", "Legacy - do not use.", _ => {} },
                    { "csr", "Legacy - do not use.", _ => {} },
                    { "ab|applicationcertbase64=", "Legacy - do not use.", _ => {} },
                    { "af|applicationcertfile=", "Legacy - do not use.", _ => {} },
                    { "pk|privatekeyfile=", "Legacy - do not use.", _ => {} },
                    { "pb|privatekeybase64=", "Legacy - do not use.", _ => {} },
                    { "cp|certpassword=", "Legacy - do not use.", _ => {} },
                    { "tb|addtrustedcertbase64=", "Legacy - do not use.", _ => {} },
                    { "tf|addtrustedcertfile=", "Legacy - do not use.", _ => {} },
                    { "ib|addissuercertbase64=", "Legacy - do not use.", _ => {} },
                    { "if|addissuercertfile=", "Legacy - do not use.", _ => {} },
                    { "rb|updatecrlbase64=", "Legacy - do not use.", _ => {} },
                    { "uc|updatecrlfile=", "Legacy - do not use.", _ => {} },
                    { "rc|removecert=", "Legacy - do not use.", _ => {} },
                    { "dt|devicecertstoretype=", "Legacy - do not use.", _ => {} },
                    { "dp|devicecertstorepath=", "Legacy - do not use.", _ => {} },
                    { "i|install", "Legacy - do not use.", _ => {} },
                    { "st|opcstacktracemask=", "Legacy - do not use.", _ => {} },
                    { "sd|shopfloordomain=", "Legacy - do not use.", _ => {} },
                    { "vc|verboseconsole=", "Legacy - do not use.", _ => {} },
                    { "as|autotrustservercerts=", "Legacy - do not use.", _ => {} },
                };
                options.Parse(args);
            }
        }
    }
}
