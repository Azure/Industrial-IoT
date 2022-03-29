// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Runtime {

    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Publisher;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Extensions.Configuration;
    using Mono.Options;
    using Opc.Ua;
    using Serilog;
    using Serilog.Events;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Class that represents a dictionary with all command line arguments from the legacy version of the OPC Publisher
    /// </summary>
    public class StandaloneCliOptions : Dictionary<string, string>, IAgentConfigProvider,
        ISettingsController, IEngineConfiguration, IStandaloneCliModelProvider, IRuntimeStateReporterConfiguration {
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
            List<string> unsupportedOptions = new List<string>();
            List<string> legacyOptions = new List<string>();
            var logger = ConsoleLogger.Create(LogEventLevel.Warning);

            // command line options
            var options = new Mono.Options.OptionSet {
                    // Publisher configuration options
                    { $"pf|publishfile=|{StandaloneCliConfigKeys.PublishedNodesConfigurationFilename}=", "The filename to configure the nodes to publish.",
                        s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationFilename] = s },
                    { $"pfs|publishfileschema=|{StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename}=", "The validation schema filename for publish file. Disabled by default.",
                        s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename] = s },
                    { "s|site=", "The site OPC Publisher is working in.",
                        s => this[StandaloneCliConfigKeys.PublisherSite] = s },

                    { "di|diagnosticsinterval=", "Shows publisher diagnostic info at the specified interval " +
                        "in seconds (need log level info).\n-1 disables remote diagnostic log and diagnostic output",
                        (int i) => this[StandaloneCliConfigKeys.DiagnosticsInterval] = TimeSpan.FromSeconds(i).ToString() },
                    { $"lf|logfile=|{StandaloneCliConfigKeys.LogFileName}=", "The filename of the logfile to use.",
                        s => this[StandaloneCliConfigKeys.LogFileName] = s },
                    { $"lt|logflushtimespan=|{StandaloneCliConfigKeys.LogFileFlushTimeSpanSec}=", "The timespan in seconds when the logfile should be flushed.",
                        (int i) => this[StandaloneCliConfigKeys.LogFileFlushTimeSpanSec] = TimeSpan.FromSeconds(i).ToString() },
                    { "ll|loglevel=", "The loglevel to use (allowed: fatal, error, warn, info, debug, verbose).",
                        (LogEventLevel l) => LogControl.Level.MinimumLevel = l },
                    { $"ih|iothubprotocol=|{StandaloneCliConfigKeys.HubTransport}=", "Protocol to use for communication with the hub. " +
                            $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(TransportOption)))}).",
                        (TransportOption p) => this[StandaloneCliConfigKeys.HubTransport] = p.ToString() },
                    { "dc|deviceconnectionstring=", "A device or edge module connection string to use.",
                        dc => this[StandaloneCliConfigKeys.EdgeHubConnectionString] = dc },
                    { "ec|edgehubconnectionstring=", "An edge module connection string to use",
                        dc => this[StandaloneCliConfigKeys.EdgeHubConnectionString] = dc },
                    { $"{StandaloneCliConfigKeys.BypassCertVerificationKey}=", "Enables bypass of certificate verification for upstream communication to edgeHub.",
                        (bool b) => this[StandaloneCliConfigKeys.BypassCertVerificationKey] = b.ToString() },
                    { $"{StandaloneCliConfigKeys.EnableMetricsKey}=", "Enables upstream metrics propagation.",
                        (bool b) => this[StandaloneCliConfigKeys.EnableMetricsKey] = b.ToString() },

                    { $"hb|heartbeatinterval=|{StandaloneCliConfigKeys.HeartbeatIntervalDefault}=", "The publisher is using this as default value in seconds " +
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
                    { $"ot|operationtimeout=|{StandaloneCliConfigKeys.OpcOperationTimeout}=", "The operation timeout of the publisher OPC UA client in milliseconds.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcOperationTimeout] = u.ToString() },
                    { $"ol|opcmaxstringlen=|{StandaloneCliConfigKeys.OpcMaxStringLength}=", "The max length of a string opc can transmit/receive.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcMaxStringLength] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.SecurityTokenLifetimeKey}=", "OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds.",
                        (uint u) => this[StandaloneCliConfigKeys.SecurityTokenLifetimeKey] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.ChannelLifetimeKey}=", "OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds.",
                        (uint u) => this[StandaloneCliConfigKeys.ChannelLifetimeKey] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.MaxBufferSizeKey}=", "OPC UA Stack Transport Secure Channel - Max buffer size.",
                        (uint u) => this[StandaloneCliConfigKeys.MaxBufferSizeKey] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.MaxMessageSizeKey}=", "OPC UA Stack Transport Secure Channel - Max message size.",
                        (uint u) => this[StandaloneCliConfigKeys.MaxMessageSizeKey] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.MaxArrayLengthKey}=", "OPC UA Stack Transport Secure Channel - Max array length.",
                        (uint u) => this[StandaloneCliConfigKeys.MaxArrayLengthKey] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.MaxByteStringLengthKey}=", "OPC UA Stack Transport Secure Channel - Max byte string length.",
                        (uint u) => this[StandaloneCliConfigKeys.MaxByteStringLengthKey] = u.ToString() },

                    { $"oi|opcsamplinginterval=|{StandaloneCliConfigKeys.OpcSamplingInterval}=", "Default value in milliseconds to request the servers to " +
                        "sample values.",
                        (int i) => this[StandaloneCliConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                    { $"op|opcpublishinginterval=|{StandaloneCliConfigKeys.OpcPublishingInterval}=", "Default value in milliseconds for the publishing interval " +
                        "setting of the subscriptions against the OPC UA server.",
                        (int i) => this[StandaloneCliConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },

                    { $"{StandaloneCliConfigKeys.ApplicationUriKey}=", "OPC UA Client Application Config - Application URI as per OPC UA definition.",
                        s => this[StandaloneCliConfigKeys.ApplicationUriKey] = s },
                    { $"{StandaloneCliConfigKeys.ProductUriKey}=", "OPC UA Client Application Config - Product URI as per OPC UA definition.",
                        s => this[StandaloneCliConfigKeys.ProductUriKey] = s },
                    { $"ct|createsessiontimeout=|{StandaloneCliConfigKeys.OpcSessionCreationTimeout}=", "Maximum amount of time in seconds that a session should " +
                        "remain open by the OPC server without any activity (session timeout) " +
                        "- to request from the OPC server at session creation.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcSessionCreationTimeout] = u.ToString() },
                    { $"{StandaloneCliConfigKeys.MinSubscriptionLifetimeKey}=", "OPC UA Client Application Config - " +
                        "Minimum subscription lifetime in seconds as per OPC UA definition.",
                        (int i) => this[StandaloneCliConfigKeys.MinSubscriptionLifetimeKey] = i.ToString() },
                    { "ki|keepaliveinterval=", "The interval in seconds the publisher is sending keep alive messages " +
                        "to the OPC servers on the endpoints it is connected to.",
                        (int i) => this[StandaloneCliConfigKeys.OpcKeepAliveIntervalInSec] = i.ToString() },
                    { $"kt|keepalivethreshold=|{StandaloneCliConfigKeys.OpcKeepAliveDisconnectThreshold}=",
                        "Specify the number of keep alive packets a server can miss, before the session is disconneced.",
                        (uint u) => this[StandaloneCliConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString() },

                    { $"fd|fetchdisplayname=|{StandaloneCliConfigKeys.FetchOpcNodeDisplayName}=", "Fetches the displayname for the monitored items subscribed.",
                        (bool b) => this[StandaloneCliConfigKeys.FetchOpcNodeDisplayName] = b.ToString() },
                    { $"mq|monitoreditemqueuecapacity=|{StandaloneCliConfigKeys.DefaultQueueSize}=", "Default queue size for monitored items.",
                        (uint u) => this[StandaloneCliConfigKeys.DefaultQueueSize] = u.ToString() },

                    // cert store option
                    { $"aa|autoaccept", "The publisher trusts all servers it is establishing a connection to.",
                        b => this[StandaloneCliConfigKeys.AutoAcceptCerts] = (b != null).ToString() },
                    { $"{StandaloneCliConfigKeys.AutoAcceptCerts}=", "The publisher trusts all servers it is establishing a connection to.",
                        (bool b) => this[StandaloneCliConfigKeys.AutoAcceptCerts] = b.ToString() },
                    { $"tm|trustmyself", "The publisher certificate is put into the trusted store automatically.",
                        b => this[StandaloneCliConfigKeys.TrustMyself] = (b != null).ToString() },
                    { $"{StandaloneCliConfigKeys.TrustMyself}=", "The publisher certificate is put into the trusted store automatically.",
                        (bool b) => this[StandaloneCliConfigKeys.TrustMyself] = b.ToString() },
                    { $"at|appcertstoretype=|{StandaloneCliConfigKeys.OpcOwnCertStoreType}=", "The own application cert store type (allowed: Directory, X509Store).",
                        s => {
                            if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                                s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                                this[StandaloneCliConfigKeys.OpcOwnCertStoreType] = s;
                                return;
                            }
                            throw new OptionException("Bad store type", "at");
                        }
                    },
                    { $"ap|appcertstorepath=|{StandaloneCliConfigKeys.OpcOwnCertStorePath}=", "The path where the own application cert should be stored.",
                        s => this[StandaloneCliConfigKeys.OpcOwnCertStorePath] = s },
                    { $"tp|trustedcertstorepath=|{StandaloneCliConfigKeys.OpcTrustedCertStorePath}=", "The path of the trusted cert store.",
                        s => this[StandaloneCliConfigKeys.OpcTrustedCertStorePath] = s },
                    { $"sn|appcertsubjectname=|{StandaloneCliConfigKeys.OpcApplicationCertificateSubjectName}=", "The subject name for the app cert.",
                        s => this[StandaloneCliConfigKeys.OpcApplicationCertificateSubjectName] = s },
                    { $"an|appname=|{StandaloneCliConfigKeys.OpcApplicationName}=", "The name for the app (used during OPC UA authentication).",
                        s => this[StandaloneCliConfigKeys.OpcApplicationName] = s },
                    { "tt|trustedcertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("tt|trustedcertstoretype"); } },
                    { $"rp|rejectedcertstorepath=|{StandaloneCliConfigKeys.OpcRejectedCertStorePath}=", "The path of the rejected cert store.",
                        s => this[StandaloneCliConfigKeys.OpcRejectedCertStorePath] = s },
                    { "rt|rejectedcertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("rt|rejectedcertstoretype"); } },
                    { $"ip|issuercertstorepath=|{StandaloneCliConfigKeys.OpcIssuerCertStorePath}=", "The path of the trusted issuer cert store.",
                        s => this[StandaloneCliConfigKeys.OpcIssuerCertStorePath] = s },
                    { $"{StandaloneCliConfigKeys.PkiRootPathKey}=", "PKI certificate store root path.",
                        s => this[StandaloneCliConfigKeys.PkiRootPathKey] = s },
                    { $"{StandaloneCliConfigKeys.TrustedIssuerCertificatesTypeKey}=", "Trusted issuer certificate types.",
                        s => this[StandaloneCliConfigKeys.TrustedIssuerCertificatesTypeKey] = s },
                    { $"{StandaloneCliConfigKeys.TrustedPeerCertificatesTypeKey}=", "Trusted peer certificate types.",
                        s => this[StandaloneCliConfigKeys.TrustedPeerCertificatesTypeKey] = s },
                    { $"{StandaloneCliConfigKeys.RejectedCertificateStoreTypeKey}=", "Rejected certificate types.",
                        s => this[StandaloneCliConfigKeys.RejectedCertificateStoreTypeKey] = s },
                    { $"{StandaloneCliConfigKeys.RejectSha1SignedCertificatesKey}=", "The publisher rejects deprecated SHA1 certificates.",
                        (bool b) => this[StandaloneCliConfigKeys.RejectSha1SignedCertificatesKey] = b.ToString() },
                    { $"{StandaloneCliConfigKeys.MinimumCertificateKeySizeKey}=", "Minimum accepted certificate size.",
                        s => this[StandaloneCliConfigKeys.MinimumCertificateKeySizeKey] = s },
                    { "it|issuercertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("it|issuercertstoretype"); } },
                    { "bs|batchsize=", "The size of message batching buffer.",
                        (int i) => this[StandaloneCliConfigKeys.BatchSize] = i.ToString() },
                    { $"si|iothubsendinterval=|{StandaloneCliConfigKeys.BatchTriggerInterval}=", "The trigger batching interval in seconds.",
                        (int k) => this[StandaloneCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromSeconds(k).ToString() },
                    { $"ms|iothubmessagesize=|{StandaloneCliConfigKeys.IoTHubMaxMessageSize}=", "The maximum size of the (IoT D2C) message.",
                        (int i) => this[StandaloneCliConfigKeys.IoTHubMaxMessageSize] = i.ToString() },
                    { "om|maxoutgressmessages=", "The maximum size of the (IoT D2C) message outgress buffer",
                        (int i) => this[StandaloneCliConfigKeys.MaxOutgressMessages] = i.ToString() },
                    { $"{StandaloneCliConfigKeys.MaxNodesPerDataSet}=", "Maximum number of nodes within a DataSet/Subscription.",
                        (int i) => this[StandaloneCliConfigKeys.MaxNodesPerDataSet] = i.ToString() },
                    { "mm|messagingmode=", "The messaging mode for messages " +
                        $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(MessagingMode)))}).",
                        (MessagingMode m) => this[StandaloneCliConfigKeys.MessagingMode] = m.ToString() },
                    { "me|messageencoding=", "The message encoding for messages " +
                        $"(allowed values: {string.Join(", ", Enum.GetNames(typeof(MessageEncoding)))}).",
                        (MessageEncoding m) => this[StandaloneCliConfigKeys.MessageEncoding] = m.ToString() },
                    { "lc|legacycompatibility=", "Run the publisher in legacy (2.5.x) compatibility mode. " +
                        "Default is 'false'.",
                        (bool b) => this[StandaloneCliConfigKeys.LegacyCompatibility] = b.ToString() },
                    { "rs|runtimestatereporting", "The publisher reports its restarts. By default this is disabled.",
                        b => this[StandaloneCliConfigKeys.RuntimeStateReporting] = (b != null).ToString()},

                    // testing purposes
                    { "sc|scaletestcount=", "The number of monitored item clones in scale tests.",
                        (int i) => this[StandaloneCliConfigKeys.ScaleTestCount] = i.ToString() },

                    // show help
                    { "h|help", "show this message and exit.",
                        b => showHelp = true },

                    // Legacy: unsupported
                    { "tc|telemetryconfigfile=", "Legacy - do not use.", b => {legacyOptions.Add("tc|telemetryconfigfile"); } },
                    { "ic|iotcentral=", "Legacy - do not use.", b => {legacyOptions.Add("ic|iotcentral"); } },
                    { "ns|noshutdown=", "Legacy - do not use.", b => {legacyOptions.Add("ns|noshutdown"); } },
                    { "rf|runforever", "Legacy - do not use.", b => {legacyOptions.Add("rf|runforever"); } },
                    { "pn|portnum=", "Legacy - do not use.", b => {legacyOptions.Add("pn|portnum"); } },
                    { "pa|path=", "Legacy - do not use.", b => {legacyOptions.Add("pa|path"); } },
                    { "lr|ldsreginterval=", "Legacy - do not use.", b => {legacyOptions.Add("lr|ldsreginterval"); } },
                    { "ss|suppressedopcstatuscodes=", "Legacy - do not use.", b => {legacyOptions.Add("ss|suppressedopcstatuscodes"); } },
                    { "csr", "Legacy - do not use.", b => {legacyOptions.Add("csr"); } },
                    { "ab|applicationcertbase64=", "Legacy - do not use.",b => {legacyOptions.Add("ab|applicationcertbase64"); } },
                    { "af|applicationcertfile=", "Legacy - do not use.", b => {legacyOptions.Add("af|applicationcertfile"); } },
                    { "pk|privatekeyfile=", "Legacy - do not use.", b => {legacyOptions.Add("pk|privatekeyfile"); } },
                    { "pb|privatekeybase64=", "Legacy - do not use.", b => {legacyOptions.Add("pb|privatekeybase64"); } },
                    { "cp|certpassword=", "Legacy - do not use.", b => {legacyOptions.Add("cp|certpassword"); } },
                    { "tb|addtrustedcertbase64=", "Legacy - do not use.", b => {legacyOptions.Add("tb|addtrustedcertbase64"); } },
                    { "tf|addtrustedcertfile=", "Legacy - do not use.", b => {legacyOptions.Add("tf|addtrustedcertfile"); } },
                    { "ib|addissuercertbase64=", "Legacy - do not use.", b => {legacyOptions.Add("ib|addissuercertbase64"); } },
                    { "if|addissuercertfile=", "Legacy - do not use.", b => {legacyOptions.Add("if|addissuercertfile"); } },
                    { "rb|updatecrlbase64=", "Legacy - do not use.", b => {legacyOptions.Add("rb|updatecrlbase64"); } },
                    { "uc|updatecrlfile=", "Legacy - do not use.", b => {legacyOptions.Add("uc|updatecrlfile"); } },
                    { "rc|removecert=", "Legacy - do not use.", b => {legacyOptions.Add("rc|removecert"); } },
                    { "dt|devicecertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("dt|devicecertstoretype"); } },
                    { "dp|devicecertstorepath=", "Legacy - do not use.", b => {legacyOptions.Add("dp|devicecertstorepath"); } },
                    { "i|install", "Legacy - do not use.", b => {legacyOptions.Add("i|install"); } },
                    { "st|opcstacktracemask=", "Legacy - do not use.", b => {legacyOptions.Add("st|opcstacktracemask"); } },
                    { "sd|shopfloordomain=", "Legacy - do not use.", b => {legacyOptions.Add("sd|shopfloordomain"); } },
                    { "vc|verboseconsole=", "Legacy - do not use.", b => {legacyOptions.Add("vc|verboseconsole"); } },
                    { "as|autotrustservercerts=", "Legacy - do not use.", b => {legacyOptions.Add("as|autotrustservercerts"); } },
                };

            try {
                unsupportedOptions = options.Parse(args);
            }
            catch (Exception e) {
                logger.Warning("Parse args exception: " + e.Message);
                Exit(160);
            }

            if (unsupportedOptions.Count > 0) {
                foreach (var option in unsupportedOptions) {
                    // Report warning only if the key is not part of security
                    // configuration keys which are processed separatelly.
                    var processedOption = option.TrimStart('-').Split('=');
                    if (!SecurityConfig.SecurityConfigKeys.Contains(processedOption[0])) {
                        logger.Warning("Option {option} wrong or not supported, " +
                            "please use -h option to get all the supported options.", option);
                    }
                }
            }

            if (legacyOptions.Any()) {
                foreach (var option in legacyOptions) {
                    logger.Warning("Legacy option {option} not supported, please use -h option to get all the supported options.", option);
                }
            }

            if (showHelp) {
                options.WriteOptionDescriptions(Console.Out);
                Exit(0);
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

        /// <inheritdoc/>
        public bool EnableRuntimeStateReporting { get => StandaloneCliModel.EnableRuntimeStateReporting; }

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

        /// <summary>
        /// Call exit with exit code
        /// </summary>
        public virtual void Exit(int exitCode) {
            Environment.Exit(exitCode);
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
            model.BatchSize = GetValueOrDefault(StandaloneCliConfigKeys.BatchSize, model.BatchSize);
            model.BatchTriggerInterval = GetValueOrDefault(StandaloneCliConfigKeys.BatchTriggerInterval, model.BatchTriggerInterval);
            model.MaxMessageSize = GetValueOrDefault(StandaloneCliConfigKeys.IoTHubMaxMessageSize, model.MaxMessageSize);
            model.ScaleTestCount = GetValueOrDefault(StandaloneCliConfigKeys.ScaleTestCount, model.ScaleTestCount);
            model.MaxOutgressMessages = GetValueOrDefault(StandaloneCliConfigKeys.MaxOutgressMessages, model.MaxOutgressMessages);
            model.MaxNodesPerDataSet = GetValueOrDefault(StandaloneCliConfigKeys.MaxNodesPerDataSet, model.MaxNodesPerDataSet);
            model.LegacyCompatibility = GetValueOrDefault(StandaloneCliConfigKeys.LegacyCompatibility, model.LegacyCompatibility);
            model.EnableRuntimeStateReporting = GetValueOrDefault(StandaloneCliConfigKeys.RuntimeStateReporting, model.EnableRuntimeStateReporting);
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

