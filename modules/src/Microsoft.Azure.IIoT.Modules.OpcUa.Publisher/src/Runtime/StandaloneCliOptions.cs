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

            _logger = ConsoleLogger.Create(LogEventLevel.Warning);
        }

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        public StandaloneCliOptions(string[] args) {

            _logger = ConsoleLogger.Create(LogEventLevel.Warning);

            bool showHelp = false;
            List<string> unsupportedOptions = new List<string>();
            List<string> legacyOptions = new List<string>();

            // command line options
            var options = new Mono.Options.OptionSet {

                "",
                "General",
                "-------",
                "",

                // show help
                { "h|help",
                    "Show help and exit.\n",
                    b => showHelp = true },

                // Publisher configuration options
                { $"f|pf|publishfile=|{StandaloneCliConfigKeys.PublishedNodesConfigurationFilename}=",
                    "The name of the file containing the configuration of the nodes to be published as well as the information to connect to the OPC UA server sources.\nThis file is also used to persist changes made through the control plane, e.g., through IoT Hub device method calls.\nWhen this file is specified, or the default file is accessible by the module, OPC Publisher will start in standalone mode.\nDefault: `publishednodes.json`\n",
                    s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationFilename] = s },
                { $"pfs|publishfileschema=|{StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename}=",
                    "The validation schema filename for publish file. Schema validation is disabled by default.\nDefault: `not set` (disabled)\n",
                    s => this[StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename] = s },
                { $"rs|runtimestatereporting:|{StandaloneCliConfigKeys.RuntimeStateReporting}:",
                    "Enable that when publisher starts or restarts it reports its runtime state using a restart message.\nDefault: `False` (disabled)\n",
                    (bool? b) => this[StandaloneCliConfigKeys.RuntimeStateReporting] = b?.ToString() ?? "True"},

                "",
                "Messaging configuration",
                "-----------------------",
                "",

                { $"c|strict:|{StandaloneCliConfigKeys.UseStandardsCompliantEncoding}:",
                    "Use strict UA compliant encodings. Default is 'false' for backwards (2.5.x - 2.8.x) compatibility. It is recommended to run the publisher in compliant mode for best interoperability.\nDefault: `False`\n",
                    (bool? b) => this[StandaloneCliConfigKeys.UseStandardsCompliantEncoding] = b?.ToString() ?? "True" },
                { $"mm|messagingmode=|{StandaloneCliConfigKeys.MessagingMode}=",
                    $"The messaging mode for messages Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessagingMode)))}`\nDefault: `{nameof(MessagingMode.PubSub)}` if `-c` is specified, otherwise `{nameof(MessagingMode.Samples)}` for backwards compatibility.\n",
                    (MessagingMode m) => this[StandaloneCliConfigKeys.MessagingMode] = m.ToString() },

                // TODO: Add ability to specify networkmessage mask
                // TODO: Add ability to specify dataset message mask
                // TODO: Add ability to specify dataset field message mask
                // TODO: Allow override of content type
                // TODO: Allow overriding schema

                { $"me|messageencoding=|{StandaloneCliConfigKeys.MessageEncoding}=",
                    $"The message encoding for messages Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessageEncoding)))}`\nDefault: `{nameof(MessageEncoding.Json)}`.\n",
                    (MessageEncoding m) => this[StandaloneCliConfigKeys.MessageEncoding] = m.ToString() },
                    { $"fm|fullfeaturedmessage=|{StandaloneCliConfigKeys.FullFeaturedMessage}=",
                        "The full featured mode for messages (all fields filled in) for backwards compatibilty. \nDefault: `False` for legacy compatibility.\n",
                        (string b) => this[StandaloneCliConfigKeys.FullFeaturedMessage] = b, true },
                { $"bi|batchtriggerinterval=|{StandaloneCliConfigKeys.BatchTriggerInterval}=",
                    "The network message publishing interval in milliseconds. Determines the publishing period at which point messages are emitted. When `--bs` is 1 and `--bi` is set to 0 batching is disabled.\nDefault: `10000` (10 seconds).\nAlternatively can be set using `BatchTriggerInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int k) => this[StandaloneCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromMilliseconds(k).ToString() },
                    { $"si|iothubsendinterval=",
                        "The network message publishing interval in seconds for backwards compatibilty. \nDefault: `10` seconds.\n",
                        (string k) => this[StandaloneCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromSeconds(int.Parse(k)).ToString(), true },
                { $"bs|batchsize=|{StandaloneCliConfigKeys.BatchSize}=",
                    "The number of incoming OPC UA subscription notifications to collect until sending a network messages. When `--bs` is set to 1 and `--bi` is 0 batching is disabled and messages are sent as soon as notifications arrive.\nDefault: `50`.\n",
                    (int i) => this[StandaloneCliConfigKeys.BatchSize] = i.ToString() },
                { $"ms|maxmessagesize=|iothubmessagesize=|{StandaloneCliConfigKeys.IoTHubMaxMessageSize}=",
                    "The maximum size of the messages to emit. In case the encoder cannot encode a message because the size would be exceeded, the message is dropped. Otherwise the encoder will aim to chunk messages if possible. \nDefault: `256k` in case of IoT Hub messages, `0` otherwise.\n",
                    (int i) => this[StandaloneCliConfigKeys.IoTHubMaxMessageSize] = i.ToString() },

                // TODO: Add ConfiguredMessageSize

                { $"npd|maxnodesperdataset=|{StandaloneCliConfigKeys.MaxNodesPerDataSet}=",
                    "Maximum number of nodes within a Subscription. When there are more nodes configured for a data set writer, they will be added to new subscriptions. This also affects metadata message size. \nDefault: `1000`.\n",
                    (int i) => this[StandaloneCliConfigKeys.MaxNodesPerDataSet] = i.ToString() },
                { $"kfc|keyframecount=|{StandaloneCliConfigKeys.DefaultKeyFrameCount}=",
                    "The default number of delta messages to send until a key frame message is sent. If 0, no key frame messages are sent, if 1, every message will be a key frame. \nDefault: `0`.\n",
                    (int i) => this[StandaloneCliConfigKeys.DefaultKeyFrameCount] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"msi|metadatasendinterval=|{StandaloneCliConfigKeys.DefaultMetaDataSendInterval}=",
                    "Default value in milliseconds for the metadata send interval which determines in which interval metadata is sent.\nEven when disabled, metadata is still sent when the metadata version changes unless `--mm=*Samples` is set in which case this setting is ignored. Only valid for network message encodings. \nDefault: `0` which means periodic sending of metadata is disabled.\n",
                    (int i) => this[StandaloneCliConfigKeys.DefaultMetaDataSendInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"dm|disablemetadata:|{StandaloneCliConfigKeys.DisableDataSetMetaData}:",
                    "Disables sending any metadata when metadata version changes. This setting can be used to also override the messaging profile's default support for metadata sending. \nDefault: `False` if the messaging profile selected supports sending metadata, `True` otherwise.\n",
                    (bool? b) => this[StandaloneCliConfigKeys.DisableDataSetMetaData] = b?.ToString() ?? "True" },

                // TODO: Default metadata output name

                { $"ri|enableroutinginfo:|{StandaloneCliConfigKeys.EnableRoutingInfo}:",
                    "Add the routing info to telemetry messages. The name of the property is `$$RoutingInfo` and the value is the `DataSetWriterGroup` for that particular message.\nWhen the `DataSetWriterGroup` is not configured, the `$$RoutingInfo` property will not be added to the message even if this argument is set.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[StandaloneCliConfigKeys.EnableRoutingInfo] = b?.ToString() ?? "True" },
                    { $"lc|legacycompatibility=|{StandaloneCliConfigKeys.LegacyCompatibility}=",
                        "Run the publisher in legacy (2.5.x) compatibility mode.\nDefault: `False` (disabled).\n",
                        (string b) => this[StandaloneCliConfigKeys.LegacyCompatibility] = b, true },

                "",
                "Transport settings",
                "------------------",
                "",

                { $"b|mqc=|mqttclientconnectionstring=|{StandaloneCliConfigKeys.MqttClientConnectionString}=",
                    "An mqtt client connection string to use. Use this option to connect OPC Publisher to a MQTT Broker or to an EdgeHub or IoT Hub MQTT endpoint.\nTo connect to an MQTT broker use the format 'HostName=<IPorDnsName>;Port=<Port>[;DeviceId=<IoTDeviceId>]'.\nTo connect to IoT Hub or EdgeHub MQTT endpoint use a regular IoT Hub connection string.\nIgnored if `-c` option is used to set a connection string.\nDefault: `not set` (disabled).\nFor more information consult https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device) and https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#for-azure-iot-tools) on how to retrieve the device connection string or generate a SharedAccessSignature for one.\n",
                    mqc => this[StandaloneCliConfigKeys.MqttClientConnectionString] = mqc },
                { $"ttt|telemetrytopictemplate=|{StandaloneCliConfigKeys.TelemetryTopicTemplateKey}=",
                    "A template that shall be used to build the topic for outgoing telemetry messages. If not specified IoT Hub and EdgeHub compatible topics will be used. The placeholder '{device_id}' can be used to inject the device id and '{output_name}' to inject routing info into the topic template.\nDefault: `not set`.\n",
                    ttt => this[StandaloneCliConfigKeys.TelemetryTopicTemplateKey] = ttt },
                { $"ht|ih=|iothubprotocol=|{StandaloneCliConfigKeys.HubTransport}=",
                    $"Protocol to use for communication with EdgeHub. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(TransportOption)))}`\nDefault: `{nameof(TransportOption.Mqtt)}` if device or edge hub connection string is provided, ignored otherwise.\n",
                    (TransportOption p) => this[StandaloneCliConfigKeys.HubTransport] = p.ToString() },
                { $"ec|edgehubconnectionstring=|dc=|deviceconnectionstring=|{StandaloneCliConfigKeys.EdgeHubConnectionString}=",
                    "A edge hub or iot hub connection string to use if you run OPC Publisher outside of IoT Edge. The connection string can be obtained from the IoT Hub portal. Use this setting for testing only.\nDefault: `not set`.\n",
                    dc => this[StandaloneCliConfigKeys.EdgeHubConnectionString] = dc },
                { $"{StandaloneCliConfigKeys.BypassCertVerificationKey}=",
                    "Enables bypass of certificate verification for upstream communication to edgeHub. This setting is for debugging purposes only and should not be used in production.\nDefault: `False`\n",
                    (bool b) => this[StandaloneCliConfigKeys.BypassCertVerificationKey] = b.ToString() },
                { $"om|maxoutgressmessages=|{StandaloneCliConfigKeys.MaxOutgressMessages}=",
                    "The maximum number of messages to buffer on the send path before messages are dropped.\nDefault: `4096`\n",
                    (int i) => this[StandaloneCliConfigKeys.MaxOutgressMessages] = i.ToString() },

                "",
                "Subscription settings",
                "---------------------",
                "",

                { $"oi|opcsamplinginterval=|{StandaloneCliConfigKeys.OpcSamplingInterval}=",
                    "Default value in milliseconds to request the servers to sample values. This value is used if an explicit sampling interval for a node was not configured. \nDefault: `1000`.\nAlternatively can be set using `DefaultSamplingInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[StandaloneCliConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"op|opcpublishinginterval=|{StandaloneCliConfigKeys.OpcPublishingInterval}=",
                    "Default value in milliseconds for the publishing interval setting of a subscription created with an OPC UA server. This value is used if an explicit publishing interval was not configured.\nDefault: `1000`.\nAlternatively can be set using `DefaultPublishingInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[StandaloneCliConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"ki|keepaliveinterval=|{StandaloneCliConfigKeys.OpcKeepAliveIntervalInSec}=",
                    "The interval in seconds the publisher is sending keep alive messages to the OPC servers on the endpoints it is connected to.\nDefault: `10000` (10 seconds).\n",
                    (int i) => this[StandaloneCliConfigKeys.OpcKeepAliveIntervalInSec] = i.ToString() },
                { $"kt|keepalivethreshold=|{StandaloneCliConfigKeys.OpcKeepAliveDisconnectThreshold}=",
                    "Specify the number of keep alive packets a server can miss, before the session is disconneced.\nDefault: `50`.\n",
                    (uint u) => this[StandaloneCliConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString() },
                { $"fd|fetchdisplayname:|{StandaloneCliConfigKeys.FetchOpcNodeDisplayName}:",
                    "Fetches the displayname for the monitored items subscribed if a display name was not specified in the configuration.\nNote: This has high impact on OPC Publisher startup performance.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[StandaloneCliConfigKeys.FetchOpcNodeDisplayName] = b?.ToString() ?? "True" },
                { $"qs|queuesize=|{StandaloneCliConfigKeys.DefaultQueueSize}=",
                    "Default queue size for all monitored items if queue size was not specified in the configuration.\nDefault: `1` (for backwards compatibility).\n",
                    (uint u) => this[StandaloneCliConfigKeys.DefaultQueueSize] = u.ToString() },
                { $"ndo|nodiscardold:|{StandaloneCliConfigKeys.DiscardNewDefault}:",
                    "The publisher is using this as default value for the discard old setting of monitored item queue configuration. Setting to true will ensure that new values are dropped before older ones are drained. \nDefault: `False` (which is the OPC UA default).\n",
                    (bool? b) => this[StandaloneCliConfigKeys.DiscardNewDefault] = b?.ToString() ?? "True" },
                { $"mc|monitoreditemdatachangetrigger=|{StandaloneCliConfigKeys.DefaultDataChangeTrigger}=",
                    $"Default data change trigger for all monitored items configured in the published nodes configuration unless explicitly overridden. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(DataChangeTriggerType)))}`\nDefault: `{nameof(DataChangeTriggerType.StatusValue)}` (which is the OPC UA default).\n",
                    (DataChangeTriggerType t) => this[StandaloneCliConfigKeys.DefaultDataChangeTrigger] = t.ToString() },
                { $"sf|skipfirst:|{StandaloneCliConfigKeys.SkipFirstDefault}:",
                    "The publisher is using this as default value for the skip first setting of nodes configured without a skip first setting. A value of True will skip sending the first notification received when the monitored item is added to the subscription.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[StandaloneCliConfigKeys.SkipFirstDefault] = b?.ToString() ?? "True" },
                    { "skipfirstevent:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[StandaloneCliConfigKeys.SkipFirstDefault] = b ?? "True", /* hidden = */ true },
                { $"hb|heartbeatinterval=|{StandaloneCliConfigKeys.HeartbeatIntervalDefault}=",
                    "The publisher is using this as default value in seconds for the heartbeat interval setting of nodes that were configured without a heartbeat interval setting. A heartbeat is sent at this interval if no value has been received.\nDefault: `0` (disabled)\nAlternatively can be set using `DefaultHeartbeatInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[StandaloneCliConfigKeys.HeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },

                "",
                "OPC UA Client configuration",
                "---------------------------",
                "",

                { $"aa|autoaccept:|{StandaloneCliConfigKeys.AutoAcceptCerts}:",
                    "The publisher trusts all servers it is establishing a connection to. WARNING: This setting should never be used in production environments!\n",
                    (bool? b) => this[StandaloneCliConfigKeys.AutoAcceptCerts] = b?.ToString() ?? "True" },
                { $"ot|operationtimeout=|{StandaloneCliConfigKeys.OpcOperationTimeout}=",
                    "The operation service call timeout of the publisher OPC UA client in milliseconds. \nDefault: `120000` (2 minutes).\n",
                    (uint u) => this[StandaloneCliConfigKeys.OpcOperationTimeout] = u.ToString() },
                { $"ct|createsessiontimeout=|{StandaloneCliConfigKeys.OpcSessionCreationTimeout}=",
                    "Maximum amount of time in seconds that a session should remain open by the OPC server without any activity (session timeout) to request from the OPC server at session creation.\nDefault: `not set`.\n",
                    (uint u) => this[StandaloneCliConfigKeys.OpcSessionCreationTimeout] = u.ToString() },
                { $"slt|{StandaloneCliConfigKeys.MinSubscriptionLifetimeKey}=",
                    "Minimum subscription lifetime in seconds as per OPC UA definition.\nDefault: `not set`.\n",
                    (int i) => this[StandaloneCliConfigKeys.MinSubscriptionLifetimeKey] = i.ToString() },

                { $"otl|opctokenlifetime=|{StandaloneCliConfigKeys.SecurityTokenLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds.\nDefault: `3600000` (1h).\n",
                    (uint u) => this[StandaloneCliConfigKeys.SecurityTokenLifetimeKey] = u.ToString() },
                { $"ocl|opcchannellifetime=|{StandaloneCliConfigKeys.ChannelLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds.\nDefault: `300000` (5 min).\n",
                    (uint u) => this[StandaloneCliConfigKeys.ChannelLifetimeKey] = u.ToString() },
                { $"omb|opcmaxbufferlen=|{StandaloneCliConfigKeys.MaxBufferSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max buffer size.\nDefault: `65535` (64KB -1).\n",
                    (uint u) => this[StandaloneCliConfigKeys.MaxBufferSizeKey] = u.ToString() },
                { $"oml|opcmaxmessagelen=|{StandaloneCliConfigKeys.MaxMessageSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max message size.\nDefault: `4194304` (4 MB).\n",
                    (uint u) => this[StandaloneCliConfigKeys.MaxMessageSizeKey] = u.ToString() },
                { $"oal|opcmaxarraylen=|{StandaloneCliConfigKeys.MaxArrayLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max array length.\nDefault: `65535` (64KB - 1).\n",
                    (uint u) => this[StandaloneCliConfigKeys.MaxArrayLengthKey] = u.ToString() },
                { $"ol|opcmaxstringlen=|{StandaloneCliConfigKeys.OpcMaxStringLength}=",
                    "The max length of a string opc can transmit/receive over the OPC UA secure channel.\nDefault: `130816` (128KB - 256).\n",
                    (uint u) => this[StandaloneCliConfigKeys.OpcMaxStringLength] = u.ToString() },
                { $"obl|opcmaxbytestringlen=|{StandaloneCliConfigKeys.MaxByteStringLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max byte string length.\nDefault: `1048576` (1MB).\n",
                    (uint u) => this[StandaloneCliConfigKeys.MaxByteStringLengthKey] = u.ToString() },
                { $"au|appuri=|{StandaloneCliConfigKeys.ApplicationUriKey}=",
                    "Application URI as per OPC UA definition inside the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[StandaloneCliConfigKeys.ApplicationUriKey] = s },
                { $"pu|producturi=|{StandaloneCliConfigKeys.ProductUriKey}=",
                    "The Product URI as per OPC UA definition insde the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[StandaloneCliConfigKeys.ProductUriKey] = s },

                { $"rejectsha1=|{StandaloneCliConfigKeys.RejectSha1SignedCertificatesKey}=",
                    "The publisher rejects deprecated SHA1 certificates.\nNote: It is recommended to always set this value to `True` if the connected OPC UA servers does not use Sha1 signed certificates.\nDefault: `False` (to support older equipment).\n",
                    (bool b) => this[StandaloneCliConfigKeys.RejectSha1SignedCertificatesKey] = b.ToString() },
                { $"mks|minkeysize=|{StandaloneCliConfigKeys.MinimumCertificateKeySizeKey}=",
                    "Minimum accepted certificate size.\nNote: It is recommended to this value to the highest certificate key size possible based on the connected OPC UA servers.\nDefault: 1024.\n",
                    s => this[StandaloneCliConfigKeys.MinimumCertificateKeySizeKey] = s },
                { $"tm|trustmyself=|{StandaloneCliConfigKeys.TrustMyself}=",
                    "Set to `False` to disable adding the publisher's own certificate to the trusted store automatically.\nDefault: `True`.\n",
                    (bool b) => this[StandaloneCliConfigKeys.TrustMyself] = b.ToString() },
                { $"sn|appcertsubjectname=|{StandaloneCliConfigKeys.OpcApplicationCertificateSubjectName}=",
                    "The subject name for the app cert.\nDefault: `CN=Microsoft.Azure.IIoT, C=DE, S=Bav, O=Microsoft, DC=localhost`.\n",
                    s => this[StandaloneCliConfigKeys.OpcApplicationCertificateSubjectName] = s },
                { $"an|appname=|{StandaloneCliConfigKeys.OpcApplicationName}=",
                    "The name for the app (used during OPC UA authentication).\nDefault: `Microsoft.Azure.IIoT`\n",
                    s => this[StandaloneCliConfigKeys.OpcApplicationName] = s },
                { $"pki|pkirootpath=|{StandaloneCliConfigKeys.PkiRootPathKey}=",
                    "PKI certificate store root path.\nDefault: `pki`.\n",
                    s => this[StandaloneCliConfigKeys.PkiRootPathKey] = s },
                { $"ap|appcertstorepath=|{StandaloneCliConfigKeys.OpcOwnCertStorePath}=",
                    "The path where the own application cert should be stored.\nDefault: $\"{PkiRootPath}/own\".\n",
                    s => this[StandaloneCliConfigKeys.OpcOwnCertStorePath] = s },
                { $"apt|at=|appcertstoretype=|{StandaloneCliConfigKeys.OpcOwnCertStoreType}=",
                    $"The own application cert store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, StandaloneCliConfigKeys.OpcOwnCertStoreType, "apt") },
                { $"tp|trustedcertstorepath=|{StandaloneCliConfigKeys.OpcTrustedCertStorePath}=",
                    "The path of the trusted cert store.\nDefault: $\"{PkiRootPath}/trusted\".\n",
                    s => this[StandaloneCliConfigKeys.OpcTrustedCertStorePath] = s },
                { $"tpt|{StandaloneCliConfigKeys.TrustedPeerCertificatesTypeKey}=",
                    $"Trusted peer certificate store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, StandaloneCliConfigKeys.TrustedPeerCertificatesTypeKey, "tpt") },
                { $"rp|rejectedcertstorepath=|{StandaloneCliConfigKeys.OpcRejectedCertStorePath}=",
                    "The path of the rejected cert store.\nDefault: $\"{PkiRootPath}/rejected\".\n",
                    s => this[StandaloneCliConfigKeys.OpcRejectedCertStorePath] = s },
                { $"rpt|{StandaloneCliConfigKeys.RejectedCertificateStoreTypeKey}=",
                    $"Rejected certificate store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, StandaloneCliConfigKeys.RejectedCertificateStoreTypeKey, "rpt") },
                { $"ip|issuercertstorepath=|{StandaloneCliConfigKeys.OpcIssuerCertStorePath}=",
                    "The path of the trusted issuer cert store.\nDefault: $\"{PkiRootPath}/issuers\".\n",
                    s => this[StandaloneCliConfigKeys.OpcIssuerCertStorePath] = s },
                { $"tit|{StandaloneCliConfigKeys.TrustedIssuerCertificatesTypeKey}=",
                    $"Trusted issuer certificate store types. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, StandaloneCliConfigKeys.TrustedIssuerCertificatesTypeKey, "tit") },

                "",
                "Diagnostic options",
                "------------------",
                "",

                { $"di|diagnosticsinterval=|{StandaloneCliConfigKeys.DiagnosticsInterval}=",
                    "Shows publisher diagnostic information at this specified interval in seconds in the OPC Publisher log (need log level info). `-1` disables remote diagnostic log and diagnostic output.\nDefault:60000 (60 seconds).\nAlternatively can be set using `DiagnosticsInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`\".\n",
                    (int i) => this[StandaloneCliConfigKeys.DiagnosticsInterval] = TimeSpan.FromSeconds(i).ToString() },
                { $"l|lf|logfile=|{StandaloneCliConfigKeys.LogFileName}=",
                    "The filename of the logfile to write log output to.\nDefault: `not set` (publisher logs to the console only).\n",
                    s => this[StandaloneCliConfigKeys.LogFileName] = s },
                { $"lt|logflushtimespan=|{StandaloneCliConfigKeys.LogFileFlushTimeSpanSec}=",
                    "The timespan in seconds when the logfile should be flushed to disk.\nDefault: `not set`.\n",
                    (int i) => this[StandaloneCliConfigKeys.LogFileFlushTimeSpanSec] = TimeSpan.FromSeconds(i).ToString() },
                { "ll|loglevel=",
                    $"The loglevel to use. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(LogEventLevel)))}`\nDefault: `{LogEventLevel.Information}`.\n",
                    (LogEventLevel l) => LogControl.Level.MinimumLevel = l },
                { $"em|{StandaloneCliConfigKeys.EnableMetricsKey}=",
                    "Enables exporting prometheus metrics on the default prometheus endpoint.\nDefault: `True` (set to `False` to disable metrics exporting).\n",
                    (bool b) => this[StandaloneCliConfigKeys.EnableMetricsKey] = b.ToString() },

                // testing purposes

                { "sc|scaletestcount=",
                    "The number of monitored item clones in scale tests.\n",
                    (string i) => this[StandaloneCliConfigKeys.ScaleTestCount] = i, true },

                // Legacy: unsupported and hidden
                { "s|site=", "Legacy - do not use.", b => {legacyOptions.Add("s|site"); }, true },
                { "mq|monitoreditemqueuecapacity=", "Legacy - do not use.", b => {legacyOptions.Add("mq|monitoreditemqueuecapacity"); }, true },
                { "tc|telemetryconfigfile=", "Legacy - do not use.", b => {legacyOptions.Add("tc|telemetryconfigfile"); }, true },
                { "ic|iotcentral=", "Legacy - do not use.", b => {legacyOptions.Add("ic|iotcentral"); }, true },
                { "ns|noshutdown=", "Legacy - do not use.", b => {legacyOptions.Add("ns|noshutdown"); }, true },
                { "rf|runforever", "Legacy - do not use.", b => {legacyOptions.Add("rf|runforever"); }, true },
                { "pn|portnum=", "Legacy - do not use.", b => {legacyOptions.Add("pn|portnum"); }, true },
                { "pa|path=", "Legacy - do not use.", b => {legacyOptions.Add("pa|path"); }, true },
                { "lr|ldsreginterval=", "Legacy - do not use.", b => {legacyOptions.Add("lr|ldsreginterval"); }, true },
                { "ss|suppressedopcstatuscodes=", "Legacy - do not use.", b => {legacyOptions.Add("ss|suppressedopcstatuscodes"); }, true },
                { "csr", "Legacy - do not use.", b => {legacyOptions.Add("csr"); }, true },
                { "ab|applicationcertbase64=", "Legacy - do not use.",b => {legacyOptions.Add("ab|applicationcertbase64"); }, true },
                { "af|applicationcertfile=", "Legacy - do not use.", b => {legacyOptions.Add("af|applicationcertfile"); }, true },
                { "pk|privatekeyfile=", "Legacy - do not use.", b => {legacyOptions.Add("pk|privatekeyfile"); }, true },
                { "pb|privatekeybase64=", "Legacy - do not use.", b => {legacyOptions.Add("pb|privatekeybase64"); }, true },
                { "cp|certpassword=", "Legacy - do not use.", b => {legacyOptions.Add("cp|certpassword"); }, true },
                { "tb|addtrustedcertbase64=", "Legacy - do not use.", b => {legacyOptions.Add("tb|addtrustedcertbase64"); }, true },
                { "tf|addtrustedcertfile=", "Legacy - do not use.", b => {legacyOptions.Add("tf|addtrustedcertfile"); }, true },
                { "tt|trustedcertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("tt|trustedcertstoretype"); }, true },
                { "rt|rejectedcertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("rt|rejectedcertstoretype"); }, true },
                { "it|issuercertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("it|issuercertstoretype"); }, true },
                { "ib|addissuercertbase64=", "Legacy - do not use.", b => {legacyOptions.Add("ib|addissuercertbase64"); }, true },
                { "if|addissuercertfile=", "Legacy - do not use.", b => {legacyOptions.Add("if|addissuercertfile"); }, true },
                { "rb|updatecrlbase64=", "Legacy - do not use.", b => {legacyOptions.Add("rb|updatecrlbase64"); }, true },
                { "uc|updatecrlfile=", "Legacy - do not use.", b => {legacyOptions.Add("uc|updatecrlfile"); }, true },
                { "rc|removecert=", "Legacy - do not use.", b => {legacyOptions.Add("rc|removecert"); }, true },
                { "dt|devicecertstoretype=", "Legacy - do not use.", b => {legacyOptions.Add("dt|devicecertstoretype"); }, true },
                { "dp|devicecertstorepath=", "Legacy - do not use.", b => {legacyOptions.Add("dp|devicecertstorepath"); }, true },
                { "i|install", "Legacy - do not use.", b => {legacyOptions.Add("i|install"); }, true },
                { "st|opcstacktracemask=", "Legacy - do not use.", b => {legacyOptions.Add("st|opcstacktracemask"); }, true },
                { "sd|shopfloordomain=", "Legacy - do not use.", b => {legacyOptions.Add("sd|shopfloordomain"); }, true },
                { "vc|verboseconsole=", "Legacy - do not use.", b => {legacyOptions.Add("vc|verboseconsole"); }, true },
                { "as|autotrustservercerts=", "Legacy - do not use.", b => {legacyOptions.Add("as|autotrustservercerts"); }, true },
            };

            try {
                unsupportedOptions = options.Parse(args);
            }
            catch (Exception e) {
                Warning("Parse args exception: " + e.Message);
                Exit(160);
            }

            if (_logger.IsEnabled(LogEventLevel.Debug)) {
                foreach (var key in Keys) {
                    Debug("Parsed command line option: '{key}'='{value}'", key, this[key]);
                }
            }

            if (unsupportedOptions.Count > 0) {
                foreach (var option in unsupportedOptions) {
                    Warning("Option {option} wrong or not supported, " +
                        "please use -h option to get all the supported options.", option);
                }
            }

            if (legacyOptions.Count > 0) {
                foreach (var option in legacyOptions) {
                    Warning("Legacy option {option} not supported, please use -h option to get all the supported options.", option);
                }
            }

            if (!MessagingProfile.IsSupported(StandaloneCliModel.MessagingMode, StandaloneCliModel.MessageEncoding)) {
                Warning("The specified combination of --mm, and --me is not (yet) supported. Currently supported combinations are: {MessageProfiles}).",
                    MessagingProfile.Supported.Select(p => $"\n(--mm {p.MessagingMode} and --me {p.MessageEncoding})").Aggregate((a, b) => $"{a}, {b}"));
                Exit(170);
            }

            if (showHelp) {
                options.WriteOptionDescriptions(Console.Out);
#if WRITETABLE
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("The following messaging profiles are supported (selected with --mm and --me):");
                Console.WriteLine();
                Console.WriteLine(MessagingProfile.GetAllAsMarkdownTable());
#endif
                Exit(0);
            }
            Config = ToAgentConfigModel();


            void SetStoreType(string s, string storeTypeKey, string optionName) {
                if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                            s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase)) {
                    this[storeTypeKey] = s;
                    return;
                }
                throw new OptionException("Bad store type", optionName);
            }
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
        /// Flag to use strict UA compliant encoding for messages
        /// </summary>
        public bool UseStandardsCompliantEncoding => StandaloneCliModel.UseStandardsCompliantEncoding;

        /// <inheritdoc/>
        public bool EnableRoutingInfo => StandaloneCliModel.EnableRoutingInfo;

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

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        public virtual void Warning(string messageTemplate) {
            _logger.Warning(messageTemplate);
        }

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValue">Object positionally formatted into the message template.</param>
        public virtual void Warning<T>(string messageTemplate, T propertyValue) {
            _logger.Warning(messageTemplate, propertyValue);
        }

        /// <summary>
        /// Write a log event with the Debug level.
        /// </summary>
        public virtual void Debug<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) {
            _logger.Debug(messageTemplate, propertyValue0, propertyValue1);
        }

        private StandaloneCliModel ToStandaloneCliModel() {
            var model = new StandaloneCliModel();
            model.PublishedNodesFile = GetValueOrDefault(StandaloneCliConfigKeys.PublishedNodesConfigurationFilename, StandaloneCliConfigKeys.DefaultPublishedNodesFilename);
            model.PublishedNodesSchemaFile = GetValueOrDefault(StandaloneCliConfigKeys.PublishedNodesConfigurationSchemaFilename, StandaloneCliConfigKeys.DefaultPublishedNodesSchemaFilename);
            model.UseStandardsCompliantEncoding = GetValueOrDefault(StandaloneCliConfigKeys.UseStandardsCompliantEncoding, model.UseStandardsCompliantEncoding);
            model.DefaultHeartbeatInterval = GetValueOrDefault(StandaloneCliConfigKeys.HeartbeatIntervalDefault, model.DefaultHeartbeatInterval);
            model.DefaultSkipFirst = GetValueOrDefault(StandaloneCliConfigKeys.SkipFirstDefault, model.DefaultSkipFirst);
            model.DefaultDiscardNew = GetValueOrDefault(StandaloneCliConfigKeys.DiscardNewDefault, model.DefaultDiscardNew);
            model.DefaultSamplingInterval = GetValueOrDefault(StandaloneCliConfigKeys.OpcSamplingInterval, model.DefaultSamplingInterval);
            model.DefaultPublishingInterval = GetValueOrDefault(StandaloneCliConfigKeys.OpcPublishingInterval, model.DefaultPublishingInterval);
            model.DefaultMetaDataSendInterval = GetValueOrDefault(StandaloneCliConfigKeys.DefaultMetaDataSendInterval, model.DefaultMetaDataSendInterval);
            model.DisableDataSetMetaData = GetValueOrDefault(StandaloneCliConfigKeys.DisableDataSetMetaData, model.DisableDataSetMetaData);
            model.DefaultKeyFrameCount = GetValueOrDefault(StandaloneCliConfigKeys.DefaultKeyFrameCount, model.DefaultKeyFrameCount);
            model.FetchOpcNodeDisplayName = GetValueOrDefault(StandaloneCliConfigKeys.FetchOpcNodeDisplayName, model.FetchOpcNodeDisplayName);
            model.DefaultQueueSize = GetValueOrDefault(StandaloneCliConfigKeys.DefaultQueueSize, model.DefaultQueueSize);
            model.DiagnosticsInterval = GetValueOrDefault(StandaloneCliConfigKeys.DiagnosticsInterval, model.DiagnosticsInterval);
            model.LogFileFlushTimeSpan = GetValueOrDefault(StandaloneCliConfigKeys.LogFileFlushTimeSpanSec, model.LogFileFlushTimeSpan);
            model.LogFilename = GetValueOrDefault(StandaloneCliConfigKeys.LogFileName, model.LogFilename);
            model.SetFullFeaturedMessage(GetValueOrDefault(StandaloneCliConfigKeys.FullFeaturedMessage, false));
            model.MessagingMode = GetValueOrDefault(StandaloneCliConfigKeys.MessagingMode, model.MessagingMode);
            model.MessageEncoding = GetValueOrDefault(StandaloneCliConfigKeys.MessageEncoding, model.MessageEncoding);
            model.BatchSize = GetValueOrDefault(StandaloneCliConfigKeys.BatchSize, model.BatchSize);
            model.BatchTriggerInterval = GetValueOrDefault(StandaloneCliConfigKeys.BatchTriggerInterval, model.BatchTriggerInterval);
            model.MaxMessageSize = GetValueOrDefault(StandaloneCliConfigKeys.IoTHubMaxMessageSize, model.MaxMessageSize);
            model.ScaleTestCount = GetValueOrDefault(StandaloneCliConfigKeys.ScaleTestCount, model.ScaleTestCount);
            model.MaxOutgressMessages = GetValueOrDefault(StandaloneCliConfigKeys.MaxOutgressMessages, model.MaxOutgressMessages);
            model.MaxNodesPerDataSet = GetValueOrDefault(StandaloneCliConfigKeys.MaxNodesPerDataSet, model.MaxNodesPerDataSet);
            model.LegacyCompatibility = GetValueOrDefault(StandaloneCliConfigKeys.LegacyCompatibility, model.LegacyCompatibility);
            model.EnableRuntimeStateReporting = GetValueOrDefault(StandaloneCliConfigKeys.RuntimeStateReporting, model.EnableRuntimeStateReporting);
            model.EnableRoutingInfo = GetValueOrDefault(StandaloneCliConfigKeys.EnableRoutingInfo, model.EnableRoutingInfo);
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

        private readonly ILogger _logger;
        private StandaloneCliModel _standaloneCliModel;
    }
}

