// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
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
    /// Parse the command line options and enter them into the dictionary using the
    /// new property names, which are then picked up by the configuration implementation
    /// </summary>
    public class LegacyCliOptions : Dictionary<string, string> {
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

        // TODO: Figure out which are actually supported in the new publisher implementation

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
                        s => this[LegacyCommandLineConfigKeys.PublisherNodeConfigurationFilename] = s },
                    { "s|site=", "the site OPC Publisher is working in.",
                        s => this[LegacyCommandLineConfigKeys.PublisherSite] = s },

                    { "di|diagnosticsinterval=", "Shows publisher diagnostic info at the specified interval " +
                        "in seconds (need log level info).\n-1 disables remote diagnostic log and diagnostic output",
                        (TimeSpan s) => this[LegacyCommandLineConfigKeys.DiagnosticsInterval] = s.ToString() },
                    { "lf|logfile=", "the filename of the logfile to use.",
                        s => this[LegacyCommandLineConfigKeys.LogFileName] = s },
                    { "lt|logflushtimespan=", "the timespan in seconds when the logfile should be flushed.",
                        (TimeSpan s) => this[LegacyCommandLineConfigKeys.LogFileFlushTimeSpanSec] = s.ToString() },
                    { "ll|loglevel=", "the loglevel to use (allowed: fatal, error, warn, info, debug, verbose).",
                        (LogEventLevel l) => LogControl.Level.MinimumLevel = l },

                    { "ih|iothubprotocol=", "Protocol to use for communication with the hub. " +
                            $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(TransportOption)))}).",
                        (TransportOption p) => this[LegacyCommandLineConfigKeys.HubTransport] = p.ToString() },
                    { "dc|deviceconnectionstring=", "A device or edge module connection string to use.",
                        dc => this[LegacyCommandLineConfigKeys.EdgeHubConnectionString] = dc },
                    { "ec|edgehubconnectionstring=", "An edge module connection string to use",
                        dc => this[LegacyCommandLineConfigKeys.EdgeHubConnectionString] = dc },

                    { "hb|heartbeatinterval=", "the publisher is using this as default value in seconds " +
                        "for the heartbeat interval setting of nodes without a heartbeat interval setting.",
                        (int i) => this[LegacyCommandLineConfigKeys.HeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },
                    { "sf|skipfirstevent=", "the publisher is using this as default value for the skip first " +
                        "event setting of nodes without a skip first event setting.",
                        (bool b) => this[LegacyCommandLineConfigKeys.SkipFirstDefault] = b.ToString() },

                    // Client settings
                    { "ot|operationtimeout=", "the operation timeout of the publisher OPC UA client in ms.",
                        (uint i) => this[LegacyCommandLineConfigKeys.OpcOperationTimeout] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ol|opcmaxstringlen=", "the max length of a string opc can transmit/receive.",
                        (uint i) => this[LegacyCommandLineConfigKeys.OpcMaxStringLength] = i.ToString() },
                    { "oi|opcsamplinginterval=", "Default value in milliseconds to request the servers to " +
                        "sample values.",
                        (int i) => this[LegacyCommandLineConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "op|opcpublishinginterval=", "Default value in milliseconds for the publishing interval " +
                            "setting of the subscriptions against the OPC UA server.",
                        (int i) => this[LegacyCommandLineConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ct|createsessiontimeout=", "The timeout in seconds used when creating a session to an endpoint.",
                        (uint u) => this[LegacyCommandLineConfigKeys.OpcSessionCreationTimeout] = TimeSpan.FromSeconds(u).ToString() },
                    { "ki|keepaliveinterval=", "The interval in seconds the publisher is sending keep alive messages " +
                            "to the OPC servers on the endpoints it is connected to.",
                        (int i) => this[LegacyCommandLineConfigKeys.OpcKeepAliveIntervalInSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "kt|keepalivethreshold=", "specify the number of keep alive packets a server can miss, " +
                        "before the session is disconneced.",
                        (uint u) => this[LegacyCommandLineConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString() },
                    { "fd|fetchdisplayname=", "same as fetchname.",
                        (bool b) => this[LegacyCommandLineConfigKeys.FetchOpcNodeDisplayName] = b.ToString() },
                    { "sw|sessionconnectwait=", "Wait time in seconds publisher is trying to connect " +
                        "to disconnected endpoints and starts monitoring unmonitored items.",
                        (int s) => this[LegacyCommandLineConfigKeys.SessionConnectWaitSec] = TimeSpan.FromSeconds(s).ToString() },

                    // cert store options
                    { "aa|autoaccept", "the publisher trusts all servers it is establishing a connection to.",
                          b => this[LegacyCommandLineConfigKeys.AutoAcceptCerts] = (b != null).ToString() },
                    { "to|trustowncert", "the publisher certificate is put into the trusted store automatically.",
                        t => this[LegacyCommandLineConfigKeys.TrustMyself] = (t != null).ToString() },
                    { "at|appcertstoretype=", "the own application cert store type (allowed: Directory, X509Store).",
                        s => {
                            if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                                s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                                this[LegacyCommandLineConfigKeys.OpcOwnCertStoreType] = s;
                                return;
                            }
                            throw new OptionException("Bad store type", "at");
                        }
                    },
                    { "ap|appcertstorepath=", "the path where the own application cert should be stored.",
                        s => this[LegacyCommandLineConfigKeys.OpcOwnCertStorePath] = s },
                    { "tp|trustedcertstorepath=", "the path of the trusted cert store.",
                        s => this[LegacyCommandLineConfigKeys.OpcTrustedCertStorePath] = s },
                    { "tt|trustedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "rp|rejectedcertstorepath=", "the path of the rejected cert store.",
                        s => this[LegacyCommandLineConfigKeys.OpcRejectedCertStorePath] = s },
                    { "rt|rejectedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "ip|issuercertstorepath=", "the path of the trusted issuer cert store.",
                        s => this[LegacyCommandLineConfigKeys.OpcIssuerCertStorePath] = s },
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
                    { "as|autotrustservercerts=", "Legacy - do not use.", _ => {} }
                };
            options.Parse(args);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RunInLegacyMode => System.IO.File.Exists(GetValueOrDefault(LegacyCommandLineConfigKeys.PublisherNodeConfigurationFilename, LegacyCommandLineConfigKeys.DefaultPublishedNodesFilename));

        public LegacyCommandLineModel ToLegacyCommandLineModel() {
            return new LegacyCommandLineModel {
                Site = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.PublisherSite),
                PublishedNodesFile = GetValueOrDefault(LegacyCommandLineConfigKeys.PublisherNodeConfigurationFilename, LegacyCommandLineConfigKeys.DefaultPublishedNodesFilename),
                SessionConnectWait = GetValueOrDefault<TimeSpan>(LegacyCommandLineConfigKeys.SessionConnectWaitSec),
                DefaultHeartbeatInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.HeartbeatIntervalDefault),
                DefaultSkipFirst = GetValueOrDefault(LegacyCommandLineConfigKeys.SkipFirstDefault, false),
                DefaultSamplingInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.OpcSamplingInterval),
                DefaultPublishingInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.OpcPublishingInterval),
                FetchOpcNodeDisplayName = GetValueOrDefault(LegacyCommandLineConfigKeys.FetchOpcNodeDisplayName, false),
                DiagnosticsInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.DiagnosticsInterval),
                LogFileFlushTimeSpan = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.LogFileFlushTimeSpanSec),
                LogFilename = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.LogFileName),
                Transport = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.HubTransport),
                EdgeHubConnectionString = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.EdgeHubConnectionString),
                OperationTimeout = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.OpcOperationTimeout),
                MaxStringLength = GetValueOrDefault<long?>(LegacyCommandLineConfigKeys.OpcMaxStringLength),
                SessionCreationTimeout = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.OpcSessionCreationTimeout),
                KeepAliveInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.OpcKeepAliveIntervalInSec),
                MaxKeepAliveCount = GetValueOrDefault<uint?>(LegacyCommandLineConfigKeys.OpcKeepAliveDisconnectThreshold),
                TrustSelf = GetValueOrDefault(LegacyCommandLineConfigKeys.TrustMyself, false),
                AutoAcceptUntrustedCertificates = GetValueOrDefault(LegacyCommandLineConfigKeys.AutoAcceptCerts, false),
                ApplicationCertificateStoreType = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.OpcOwnCertStoreType),
                ApplicationCertificateStorePath = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.OpcOwnCertStorePath),
                TrustedPeerCertificatesPath = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.OpcTrustedCertStorePath),
                RejectedCertificateStorePath = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.OpcRejectedCertStorePath),
                TrustedIssuerCertificatesPath = GetValueOrDefault<string>(LegacyCommandLineConfigKeys.OpcIssuerCertStorePath)
            };
        }

        public AgentConfigModel ToAgentConfigModel() {
            return new AgentConfigModel {
                AgentId = "StandalonePublisher",
                Capabilities = new Dictionary<string, string>(),
                HeartbeatInterval = GetValueOrDefault<TimeSpan?>(LegacyCommandLineConfigKeys.HeartbeatIntervalDefault),
                JobCheckInterval = null,
                JobOrchestratorUrl = null,
                MaxWorkers = 1
            };
        }

        private T GetValueOrDefault<T>(string key, T defaultValue = default) {
            if (!ContainsKey(key)) {
                return defaultValue;
            }

            return (T)Convert.ChangeType(this[key], typeof(T));
        }
    }
}
