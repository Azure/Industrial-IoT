// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.IIoT.Agent.Framework;
using Microsoft.Azure.IIoT.Agent.Framework.Models;
using Microsoft.Azure.IIoT.Diagnostics;
using Microsoft.Azure.IIoT.Module.Framework;
using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
using Microsoft.Azure.IIoT.OpcUa.Publisher;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Opc.Ua;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Class that represents a dictionary with all command line arguments from the legacy version of the OPC Publisher
    /// </summary>
    public class LegacyCliOptions : Dictionary<string, string>, IAgentConfigProvider,
        IEngineConfiguration, ILegacyCliModelProvider {
        /// <summary>
        /// Empty constructor.
        /// </summary>
        public LegacyCliOptions() {

        }

        /// <summary>
        /// Creates a new instance of the the legacy CLI options based on existing configuration values.
        /// </summary>
        /// <param name="config"></param>
        public LegacyCliOptions(IConfiguration config) {
            foreach (var item in config.GetChildren()) {
                this[item.Key] = item.Value;
            }

            Config = ToAgentConfigModel();
            LegacyCliModel = ToLegacyCliModel();
        }

        // TODO: Figure out which are actually supported in the new publisher implementation

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        public LegacyCliOptions(string[] args) {

            // command line options
            var options = new Mono.Options.OptionSet {
                    // Publisher configuration options
                    { "pf|publishfile=", "the filename to configure the nodes to publish.",
                        s => this[LegacyCliConfigKeys.PublisherNodeConfigurationFilename] = s },
                    { "s|site=", "the site OPC Publisher is working in.",
                        s => this[LegacyCliConfigKeys.PublisherSite] = s },

                    { "di|diagnosticsinterval=", "Shows publisher diagnostic info at the specified interval " +
                        "in seconds (need log level info).\n-1 disables remote diagnostic log and diagnostic output",
                        (int i) => this[LegacyCliConfigKeys.DiagnosticsInterval] = TimeSpan.FromSeconds(i).ToString() },
                    { "lf|logfile=", "the filename of the logfile to use.",
                        s => this[LegacyCliConfigKeys.LogFileName] = s },
                    { "lt|logflushtimespan=", "the timespan in seconds when the logfile should be flushed.",
                        (int i) => this[LegacyCliConfigKeys.LogFileFlushTimeSpanSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "ll|loglevel=", "the loglevel to use (allowed: fatal, error, warn, info, debug, verbose).",
                        (LogEventLevel l) => LogControl.Level.MinimumLevel = l },

                    { "ih|iothubprotocol=", "Protocol to use for communication with the hub. " +
                            $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(TransportOption)))}).",
                        (TransportOption p) => this[LegacyCliConfigKeys.HubTransport] = p.ToString() },
                    { "dc|deviceconnectionstring=", "A device or edge module connection string to use.",
                        dc => this[LegacyCliConfigKeys.EdgeHubConnectionString] = dc },
                    { "ec|edgehubconnectionstring=", "An edge module connection string to use",
                        dc => this[LegacyCliConfigKeys.EdgeHubConnectionString] = dc },

                    { "hb|heartbeatinterval=", "the publisher is using this as default value in seconds " +
                        "for the heartbeat interval setting of nodes without a heartbeat interval setting.",
                        (int i) => this[LegacyCliConfigKeys.HeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },
                    { "sf|skipfirstevent=", "the publisher is using this as default value for the skip first " +
                        "event setting of nodes without a skip first event setting.",
                        (bool b) => this[LegacyCliConfigKeys.SkipFirstDefault] = b.ToString() },
                    { "mm|messagingmode=", "The messaging mode for messages " +
                        $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(MessagingMode)))}).",
                        (MessagingMode m) => this[LegacyCliConfigKeys.MessagingMode] = m.ToString() },
                    { "fm|fullfeaturedmessage=", "The full featured mode for messages (all fields filled in)." + 
                        "Default is 'true', for legacy compatibility use 'false'",
                        (bool b) => this[LegacyCliConfigKeys.FullFeaturedMessage] = b.ToString() },

                    // Client settings
                    { "ot|operationtimeout=", "the operation timeout of the publisher OPC UA client in ms.",
                        (uint i) => this[LegacyCliConfigKeys.OpcOperationTimeout] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ol|opcmaxstringlen=", "the max length of a string opc can transmit/receive.",
                        (uint i) => this[LegacyCliConfigKeys.OpcMaxStringLength] = i.ToString() },
                    { "oi|opcsamplinginterval=", "Default value in milliseconds to request the servers to " +
                        "sample values.",
                        (int i) => this[LegacyCliConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "op|opcpublishinginterval=", "Default value in milliseconds for the publishing interval " +
                            "setting of the subscriptions against the OPC UA server.",
                        (int i) => this[LegacyCliConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ct|createsessiontimeout=", "The timeout in seconds used when creating a session to an endpoint.",
                        (uint u) => this[LegacyCliConfigKeys.OpcSessionCreationTimeout] = TimeSpan.FromSeconds(u).ToString() },
                    { "ki|keepaliveinterval=", "The interval in seconds the publisher is sending keep alive messages " +
                            "to the OPC servers on the endpoints it is connected to.",
                        (int i) => this[LegacyCliConfigKeys.OpcKeepAliveIntervalInSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "kt|keepalivethreshold=", "specify the number of keep alive packets a server can miss, " +
                        "before the session is disconneced.",
                        (uint u) => this[LegacyCliConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString() },
                    { "fd|fetchdisplayname=", "same as fetchname.",
                        (bool b) => this[LegacyCliConfigKeys.FetchOpcNodeDisplayName] = b.ToString() },
                    { "sw|sessionconnectwait=", "Wait time in seconds publisher is trying to connect " +
                        "to disconnected endpoints and starts monitoring unmonitored items.",
                        (int s) => this[LegacyCliConfigKeys.SessionConnectWaitSec] = TimeSpan.FromSeconds(s).ToString() },

                    // cert store options
                    { "aa|autoaccept", "the publisher trusts all servers it is establishing a connection to.",
                          b => this[LegacyCliConfigKeys.AutoAcceptCerts] = (b != null).ToString() },
                    { "tm|trustmyself", "the publisher certificate is put into the trusted store automatically.",
                        t => this[LegacyCliConfigKeys.TrustMyself] = (t != null).ToString() },
                    { "at|appcertstoretype=", "the own application cert store type (allowed: Directory, X509Store).",
                        s => {
                            if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                                s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                                this[LegacyCliConfigKeys.OpcOwnCertStoreType] = s;
                                return;
                            }
                            throw new OptionException("Bad store type", "at");
                        }
                    },
                    { "ap|appcertstorepath=", "the path where the own application cert should be stored.",
                        s => this[LegacyCliConfigKeys.OpcOwnCertStorePath] = s },
                    { "tp|trustedcertstorepath=", "the path of the trusted cert store.",
                        s => this[LegacyCliConfigKeys.OpcTrustedCertStorePath] = s },
                    { "tt|trustedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "rp|rejectedcertstorepath=", "the path of the rejected cert store.",
                        s => this[LegacyCliConfigKeys.OpcRejectedCertStorePath] = s },
                    { "rt|rejectedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "ip|issuercertstorepath=", "the path of the trusted issuer cert store.",
                        s => this[LegacyCliConfigKeys.OpcIssuerCertStorePath] = s },
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

            Config = ToAgentConfigModel();
            LegacyCliModel = ToLegacyCliModel();
        }

        /// <summary>
        ///
        /// </summary>
        public bool RunInLegacyMode => System.IO.File.Exists(GetValueOrDefault(LegacyCliConfigKeys.PublisherNodeConfigurationFilename, LegacyCliConfigKeys.DefaultPublishedNodesFilename));

        /// <summary>
        /// The AgentConfigModel instance that is based on specified legacy command line arguments.
        /// </summary>
        public AgentConfigModel Config { get; }

        /// <summary>
        /// OnConfigUpdated-Event - never called as command line arguments don't change while runtime.
        /// </summary>
#pragma warning disable 67
        public event ConfigUpdatedEventHandler OnConfigUpdated;
#pragma warning restore 67

        /// <summary>
        /// The batch size, hardcoded to 1.
        /// </summary>
        public int? BatchSize => 1;

        /// <summary>
        /// The interval to show diagnostic information in the log.
        /// </summary>
        public TimeSpan? DiagnosticsInterval => LegacyCliModel.DiagnosticsInterval;

        /// <summary>
        /// The model of the CLI arguments.
        /// </summary>
        public LegacyCliModel LegacyCliModel { get; }

        /// <summary>
        /// Gets the additiona loggerConfiguration that represents the command line arguments.
        /// </summary>
        /// <returns></returns>
        public LoggerConfiguration ToLoggerConfiguration() {
            LoggerConfiguration loggerConfiguration = null;

            if (!string.IsNullOrWhiteSpace(LegacyCliModel.LogFilename)) {
                loggerConfiguration ??= new LoggerConfiguration();
                loggerConfiguration = loggerConfiguration.WriteTo.File(LegacyCliModel.LogFilename, flushToDiskInterval: LegacyCliModel.LogFileFlushTimeSpan ?? TimeSpan.FromSeconds(30));
            }

            return loggerConfiguration;
        }

        private LegacyCliModel ToLegacyCliModel() {
            return new LegacyCliModel {
                Site = GetValueOrDefault<string>(LegacyCliConfigKeys.PublisherSite),
                PublishedNodesFile = GetValueOrDefault(LegacyCliConfigKeys.PublisherNodeConfigurationFilename, LegacyCliConfigKeys.DefaultPublishedNodesFilename),
                SessionConnectWait = GetValueOrDefault<TimeSpan>(LegacyCliConfigKeys.SessionConnectWaitSec),
                DefaultHeartbeatInterval = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.HeartbeatIntervalDefault),
                DefaultSkipFirst = GetValueOrDefault(LegacyCliConfigKeys.SkipFirstDefault, false),
                DefaultSamplingInterval = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.OpcSamplingInterval, TimeSpan.FromMilliseconds(1000)),
                DefaultPublishingInterval = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.OpcPublishingInterval, TimeSpan.FromMilliseconds(1000)),
                FetchOpcNodeDisplayName = GetValueOrDefault(LegacyCliConfigKeys.FetchOpcNodeDisplayName, false),
                DiagnosticsInterval = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.DiagnosticsInterval),
                LogFileFlushTimeSpan = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.LogFileFlushTimeSpanSec),
                LogFilename = GetValueOrDefault<string>(LegacyCliConfigKeys.LogFileName),
                Transport = GetValueOrDefault<string>(LegacyCliConfigKeys.HubTransport),
                MessagingMode = GetValueOrDefault(LegacyCliConfigKeys.MessagingMode, MessagingMode.Samples),
                FullFeaturedMessage = GetValueOrDefault(LegacyCliConfigKeys.FullFeaturedMessage, false),
                EdgeHubConnectionString = GetValueOrDefault<string>(LegacyCliConfigKeys.EdgeHubConnectionString),
                OperationTimeout = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.OpcOperationTimeout),
                MaxStringLength = GetValueOrDefault<long?>(LegacyCliConfigKeys.OpcMaxStringLength),
                SessionCreationTimeout = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.OpcSessionCreationTimeout),
                KeepAliveInterval = GetValueOrDefault<TimeSpan?>(LegacyCliConfigKeys.OpcKeepAliveIntervalInSec),
                MaxKeepAliveCount = GetValueOrDefault<uint?>(LegacyCliConfigKeys.OpcKeepAliveDisconnectThreshold),
                TrustSelf = GetValueOrDefault(LegacyCliConfigKeys.TrustMyself, false),
                AutoAcceptUntrustedCertificates = GetValueOrDefault(LegacyCliConfigKeys.AutoAcceptCerts, false),
                ApplicationCertificateStoreType = GetValueOrDefault<string>(LegacyCliConfigKeys.OpcOwnCertStoreType),
                ApplicationCertificateStorePath = GetValueOrDefault<string>(LegacyCliConfigKeys.OpcOwnCertStorePath),
                TrustedPeerCertificatesPath = GetValueOrDefault<string>(LegacyCliConfigKeys.OpcTrustedCertStorePath),
                RejectedCertificateStorePath = GetValueOrDefault<string>(LegacyCliConfigKeys.OpcRejectedCertStorePath),
                TrustedIssuerCertificatesPath = GetValueOrDefault<string>(LegacyCliConfigKeys.OpcIssuerCertStorePath)
            };
        }

        private AgentConfigModel ToAgentConfigModel() {
            return new AgentConfigModel {
                AgentId = "StandalonePublisher",
                Capabilities = new Dictionary<string, string>(),
                HeartbeatInterval = TimeSpan.FromSeconds(30), // heartbeat is needed even though in standalone mode to be notified about config file changes
                JobCheckInterval = TimeSpan.FromSeconds(30),
                JobOrchestratorUrl = "standalone", //we have to set a value so that the (legacy) job orchestrator is called
                MaxWorkers = 1
            };
        }

        private T GetValueOrDefault<T>(string key, T defaultValue = default) {
            if (!ContainsKey(key)) {
                return defaultValue;
            }

            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFrom(this[key]);
        }
    }
}
