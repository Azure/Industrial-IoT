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
    public class StandaloneCliOptions : Dictionary<string, string>, IAgentConfigProvider,
        ISettingsController, IEngineConfiguration, IStandaloneCliModelProvider {
        /// <summary>
        /// Creates a new instance of the the standalone cli options based on existing configuration values.
        /// </summary>
        /// <param name="config"></param>
        public StandaloneCliOptions(IConfiguration config) {
            foreach (var item in config.GetChildren()) {
                this[item.Key] = item.Value;
            }
            Config = ToAgentConfigModel();
        }

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        public StandaloneCliOptions(string[] args) {

            bool showHelp = false;
            bool isLegacyOption = false;
            var logger = ConsoleLogger.Create(LogEventLevel.Warning);

            // command line options
            var options = new Mono.Options.OptionSet {
                    // Publisher configuration options
                    { "pf|publishfile=", "The filename to configure the nodes to publish.",
                        s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationFilename] = s },
                    { "pfs|publishfileschema=", "The validation schema filename for publish file. Disabled by default.",
                        s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename] = s },
                    { "s|site=", "The site OPC Publisher is working in.",
                        s => this[StandaloneCliConfigKeys.PublisherSite] = s },

                    { "di|diagnosticsinterval=", "Shows publisher diagnostic info at the specified interval " +
                        "in seconds (need log level info).\n-1 disables remote diagnostic log and diagnostic output",
                        (int i) => this[StandaloneCliConfigKeys.DiagnosticsInterval] = TimeSpan.FromSeconds(i).ToString() },
                    { "lf|logfile=", "The filename of the logfile to use.",
                        s => this[StandaloneCliConfigKeys.LogFileName] = s },
                    { "lt|logflushtimespan=", "The timespan in seconds when the logfile should be flushed.",
                        (int i) => this[StandaloneCliConfigKeys.LogFileFlushTimeSpanSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "ll|loglevel=", "The loglevel to use (allowed: fatal, error, warn, info, debug, verbose).",
                        (LogEventLevel l) => LogControl.Level.MinimumLevel = l },
                    { "ih|iothubprotocol=", "Protocol to use for communication with the hub. " +
                            $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(TransportOption)))}).",
                        (TransportOption p) => this[StandaloneCliConfigKeys.HubTransport] = p.ToString() },
                    { "dc|deviceconnectionstring=", "A device or edge module connection string to use.",
                        dc => this[StandaloneCliConfigKeys.EdgeHubConnectionString] = dc },
                    { "ec|edgehubconnectionstring=", "An edge module connection string to use",
                        dc => this[StandaloneCliConfigKeys.EdgeHubConnectionString] = dc },

                    { "hb|heartbeatinterval=", "The publisher is using this as default value in seconds " +
                        "for the heartbeat interval setting of nodes without a heartbeat interval setting.",
                        (int i) => this[StandaloneCliConfigKeys.HeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },
                    // ToDo: Bring back once SkipFirst mechanism is implemented.
                    //{ "sf|skipfirstevent=", "The publisher is using this as default value for the skip first " +
                    //    "event setting of nodes without a skip first event setting.",
                    //    (bool b) => this[StandaloneCliConfigKeys.SkipFirstDefault] = b.ToString() },

                    { "fm|fullfeaturedmessage=", "The full featured mode for messages (all fields filled in)." +
                        "Default is 'false' for legacy compatibility.",
                        (bool b) => this[StandaloneCliConfigKeys.FullFeaturedMessage] = b.ToString() },

                    // Client settings
                    { "ot|operationtimeout=", "The operation timeout of the publisher OPC UA client in ms.",
                        (uint i) => this[StandaloneCliConfigKeys.OpcOperationTimeout] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ol|opcmaxstringlen=", "The max length of a string opc can transmit/receive.",
                        (uint i) => this[StandaloneCliConfigKeys.OpcMaxStringLength] = i.ToString() },
                    { "oi|opcsamplinginterval=", "Default value in milliseconds to request the servers to " +
                        "sample values.",
                        (int i) => this[StandaloneCliConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "op|opcpublishinginterval=", "Default value in milliseconds for the publishing interval " +
                        "setting of the subscriptions against the OPC UA server.",
                        (int i) => this[StandaloneCliConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { "ct|createsessiontimeout=", "Maximum amount of time in seconds that a session should " +
                        "remain open by the OPC server without any activity (session timeout) " +
                        "- to request from the OPC server at session creation.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcSessionCreationTimeout] = TimeSpan.FromSeconds(u).ToString() },
                    { "ki|keepaliveinterval=", "The interval in seconds the publisher is sending keep alive messages " +
                        "to the OPC servers on the endpoints it is connected to.",
                        (int i) => this[StandaloneCliConfigKeys.OpcKeepAliveIntervalInSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "kt|keepalivethreshold=", "Specify the number of keep alive packets a server can miss, " +
                        "before the session is disconneced.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString() },
                    { "fd|fetchdisplayname=", "Fetches the displayname for the monitored items subscribed.",
                        (bool b) => this[StandaloneCliConfigKeys.FetchOpcNodeDisplayName] = b.ToString() },
                    { "mq|monitoreditemqueuecapacity=", "Default queue size for monitored items.",
                        (uint u) => this[StandaloneCliConfigKeys.DefaultQueueSize] = u.ToString() },

                    // cert store option
                    { "aa|autoaccept", "The publisher trusts all servers it is establishing a connection to.",
                          b => this[StandaloneCliConfigKeys.AutoAcceptCerts] = (b != null).ToString() },
                    { "tm|trustmyself", "The publisher certificate is put into the trusted store automatically.",
                        t => this[StandaloneCliConfigKeys.TrustMyself] = (t != null).ToString() },
                    { "at|appcertstoretype=", "The own application cert store type (allowed: Directory, X509Store).",
                        s => {
                            if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                                s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                                this[StandaloneCliConfigKeys.OpcOwnCertStoreType] = s;
                                return;
                            }
                            throw new OptionException("Bad store type", "at");
                        }
                    },
                    { "ap|appcertstorepath=", "The path where the own application cert should be stored.",
                        s => this[StandaloneCliConfigKeys.OpcOwnCertStorePath] = s },
                    { "tp|trustedcertstorepath=", "The path of the trusted cert store.",
                        s => this[StandaloneCliConfigKeys.OpcTrustedCertStorePath] = s },
                    { "sn|appcertsubjectname=", "The subject name for the app cert.",
                        s => this[StandaloneCliConfigKeys.OpcApplicationCertificateSubjectName] = s },
                    { "an|appname=", "The name for the app (used during OPC UA authentication).",
                        s => this[StandaloneCliConfigKeys.OpcApplicationName] = s },
                    { "tt|trustedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "rp|rejectedcertstorepath=", "The path of the rejected cert store.",
                        s => this[StandaloneCliConfigKeys.OpcRejectedCertStorePath] = s },
                    { "rt|rejectedcertstoretype=", "Legacy - do not use.", _ => {} },
                    { "ip|issuercertstorepath=", "The path of the trusted issuer cert store.",
                        s => this[StandaloneCliConfigKeys.OpcIssuerCertStorePath] = s },
                    { "it|issuercertstoretype=", "Legacy - do not use.", _ => {} },
                    { "bs|batchsize=", "The size of message batching buffer.",
                        (int i) => this[StandaloneCliConfigKeys.BatchSize] = i.ToString() },
                    { "si|iothubsendinterval=", "The trigger batching interval in seconds.",
                        (int k) => this[StandaloneCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromSeconds(k).ToString() },
                    { "ms|iothubmessagesize=", "The maximum size of the (IoT D2C) message.",
                        (int i) => this[StandaloneCliConfigKeys.IoTHubMaxMessageSize] = i.ToString() },
                    { "om|maxoutgressmessages=", "The maximum size of the (IoT D2C) message outgress buffer",
                        (int i) => this[StandaloneCliConfigKeys.MaxOutgressMessages] = i.ToString() },
                    { "mm|messagingmode=", "The messaging mode for messages " +
                        $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(MessagingMode)))}).",
                        (MessagingMode m) => this[StandaloneCliConfigKeys.MessagingMode] = m.ToString() },
                    { "me|messageencoding=", "The message encoding for messages " +
                        $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(MessageEncoding)))}).",
                        (MessageEncoding m) => this[StandaloneCliConfigKeys.MessageEncoding] = m.ToString() },
                    { "lc|legacycompatibility=", "Run the publisher in legacy (2.5.x) compatibility mode. " +
                        "Default is 'false'.",
                        (bool b) => this[StandaloneCliConfigKeys.LegacyCompatibility] = b.ToString() },

                    // testing purposes
                    { "sc|scaletestcount=", "The number of monitored item clones in scale tests.",
                        (int i) => this[StandaloneCliConfigKeys.ScaleTestCount] = i.ToString() },

                    // show help
                    { "h|help", "show this message and exit.",
                        b => showHelp = true },

                    // Legacy: unsupported
                    { "tc|telemetryconfigfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "ic|iotcentral=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "ns|noshutdown=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "rf|runforever", "Legacy - do not use.", b => isLegacyOption = true },
                    { "pn|portnum=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "pa|path=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "lr|ldsreginterval=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "ss|suppressedopcstatuscodes=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "csr", "Legacy - do not use.", b => isLegacyOption = true },
                    { "ab|applicationcertbase64=", "Legacy - do not use.",b => isLegacyOption = true },
                    { "af|applicationcertfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "pk|privatekeyfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "pb|privatekeybase64=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "cp|certpassword=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "tb|addtrustedcertbase64=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "tf|addtrustedcertfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "ib|addissuercertbase64=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "if|addissuercertfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "rb|updatecrlbase64=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "uc|updatecrlfile=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "rc|removecert=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "dt|devicecertstoretype=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "dp|devicecertstorepath=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "i|install", "Legacy - do not use.", b => isLegacyOption = true },
                    { "st|opcstacktracemask=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "sd|shopfloordomain=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "vc|verboseconsole=", "Legacy - do not use.", b => isLegacyOption = true },
                    { "as|autotrustservercerts=", "Legacy - do not use.",b => isLegacyOption = true }
                };

            try {
                options.Parse(args);
            }
            catch (Exception e) {
                logger.Warning("Parse args exception: " + e.Message);
                Environment.Exit(0);
            }

            if (isLegacyOption) {
                logger.Warning("Legacy option not supported, please use -h option to get all the supported options.");
            }

            if (showHelp) {
                options.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }
            Config = ToAgentConfigModel();
        }

        /// <summary>
        /// check if we're running in standalone mode - default publishednodes.json file accessible
        /// </summary>
        public bool RunInStandaloneMode => TryGetValue(StandaloneCliConfigKeys.PublishedNodesConfigurationFilename, out _) ||
             System.IO.File.Exists(StandaloneCliConfigKeys.DefaultPublishedNodesFilename);

        /// <summary>
        /// The AgentConfigModel instance that is based on specified standalone command line arguments.
        /// </summary>
        public AgentConfigModel Config { get; }

        /// <inheritdoc/>
        public event ConfigUpdatedEventHandler OnConfigUpdated;

        /// <inheritdoc/>
        public void TriggerConfigUpdate(object sender, EventArgs eventArgs) {
            OnConfigUpdated?.Invoke(sender, eventArgs);
        }

        /// <summary>
        /// The batch size
        /// </summary>
        public int? BatchSize => StandaloneCliModel.BatchSize;

        /// <summary>
        /// The interval to show diagnostic information in the log.
        /// </summary>
        public TimeSpan? BatchTriggerInterval => StandaloneCliModel.BatchTriggerInterval;

        /// <summary>
        /// The interval to show diagnostic information in the log.
        /// </summary>
        public TimeSpan? DiagnosticsInterval => StandaloneCliModel.DiagnosticsInterval;

        /// <summary>
        /// the Maximum (IoT D2C) message size
        /// </summary>
        public int? MaxMessageSize => StandaloneCliModel.MaxMessageSize;

        /// <summary>
        /// The Maximum (IoT D2C) message buffer size
        /// </summary>
        public int? MaxOutgressMessages => StandaloneCliModel.MaxOutgressMessages;

        /// <summary>
        /// The model of the CLI arguments.
        /// </summary>
        public StandaloneCliModel StandaloneCliModel {
            get {
                if (_standaloneCliModel == null) {
                    _standaloneCliModel = ToStandaloneCliModel();
                }

                return _standaloneCliModel;
            }
        }

        /// <summary>
        /// Gets the additional loggerConfiguration that represents the command line arguments.
        /// </summary>
        /// <returns></returns>
        public LoggerConfiguration ToLoggerConfiguration() {
            LoggerConfiguration loggerConfiguration = null;

            if (!string.IsNullOrWhiteSpace(StandaloneCliModel.LogFilename)) {
                loggerConfiguration ??= new LoggerConfiguration();
                loggerConfiguration = loggerConfiguration.WriteTo.File(
                    StandaloneCliModel.LogFilename, flushToDiskInterval: StandaloneCliModel.LogFileFlushTimeSpan);
            }

            return loggerConfiguration;
        }

        private StandaloneCliModel ToStandaloneCliModel() {
            var model = new StandaloneCliModel();
            model.PublishedNodesFile = GetValueOrDefault(StandaloneCliConfigKeys.PublishedNodesConfigurationFilename, StandaloneCliConfigKeys.DefaultPublishedNodesFilename);
            model.PublishedNodesSchemaFile = GetValueOrDefault(StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename, StandaloneCliConfigKeys.DefaultPublishedNodesSchemaFilename);
            model.DefaultHeartbeatInterval = GetValueOrDefault(StandaloneCliConfigKeys.HeartbeatIntervalDefault, model.DefaultHeartbeatInterval);
            model.DefaultSkipFirst = GetValueOrDefault(StandaloneCliConfigKeys.SkipFirstDefault, model.DefaultSkipFirst);
            model.DefaultSamplingInterval = GetValueOrDefault(StandaloneCliConfigKeys.OpcSamplingInterval, model.DefaultSamplingInterval);
            model.DefaultPublishingInterval = GetValueOrDefault(StandaloneCliConfigKeys.OpcPublishingInterval, model.DefaultPublishingInterval);
            model.FetchOpcNodeDisplayName = GetValueOrDefault(StandaloneCliConfigKeys.FetchOpcNodeDisplayName, model.FetchOpcNodeDisplayName);
            model.DefaultQueueSize = GetValueOrDefault(StandaloneCliConfigKeys.DefaultQueueSize, model.DefaultQueueSize);
            model.DiagnosticsInterval = GetValueOrDefault(StandaloneCliConfigKeys.DiagnosticsInterval, model.DiagnosticsInterval);
            model.LogFileFlushTimeSpan = GetValueOrDefault(StandaloneCliConfigKeys.LogFileFlushTimeSpanSec, model.LogFileFlushTimeSpan);
            model.LogFilename = GetValueOrDefault(StandaloneCliConfigKeys.LogFileName, model.LogFilename);
            model.MessagingMode = GetValueOrDefault(StandaloneCliConfigKeys.MessagingMode, model.MessagingMode);
            model.MessageEncoding = GetValueOrDefault(StandaloneCliConfigKeys.MessageEncoding, model.MessageEncoding);
            model.FullFeaturedMessage = GetValueOrDefault(StandaloneCliConfigKeys.FullFeaturedMessage, model.FullFeaturedMessage);
            model.OperationTimeout = GetValueOrDefault(StandaloneCliConfigKeys.OpcOperationTimeout, model.OperationTimeout);
            model.BatchSize = GetValueOrDefault(StandaloneCliConfigKeys.BatchSize, model.BatchSize);
            model.BatchTriggerInterval = GetValueOrDefault(StandaloneCliConfigKeys.BatchTriggerInterval, model.BatchTriggerInterval);
            model.MaxMessageSize = GetValueOrDefault(StandaloneCliConfigKeys.IoTHubMaxMessageSize, model.MaxMessageSize);
            model.ScaleTestCount = GetValueOrDefault(StandaloneCliConfigKeys.ScaleTestCount, model.ScaleTestCount);
            model.MaxOutgressMessages = GetValueOrDefault(StandaloneCliConfigKeys.MaxOutgressMessages, model.MaxOutgressMessages);
            model.MaxNodesPerDataSet = GetValueOrDefault(StandaloneCliConfigKeys.MaxNodesPerDataSet, model.MaxNodesPerDataSet);
            model.LegacyCompatibility = GetValueOrDefault(StandaloneCliConfigKeys.LegacyCompatibility, model.LegacyCompatibility);
            return model;
        }

        private static AgentConfigModel ToAgentConfigModel() {
            return new AgentConfigModel {
                AgentId = "StandalonePublisher",
                Capabilities = new Dictionary<string, string>(),
                // heartbeat is needed even though in standalone mode to be notified about config file changes
                HeartbeatInterval = TimeSpan.FromSeconds(30),
                JobCheckInterval = TimeSpan.FromSeconds(30),
                //we have to set a value so that the standalone job orchestrator is called
                JobOrchestratorUrl = "standalone",
                MaxWorkers = 1,
            };
        }

        private T GetValueOrDefault<T>(string key, T defaultValue) {
            if (!ContainsKey(key)) {
                return defaultValue;
            }
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFrom(this[key]);
        }

        private StandaloneCliModel _standaloneCliModel;
    }
}

