// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Azure.IIoT.OpcUa.Publisher.State;
    using Azure.IIoT.OpcUa.Models;
    using Furly.Extensions.Logging;
    using Microsoft.Azure.IIoT.Abstractions;
    using Microsoft.Azure.IIoT.Module.Framework;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Mono.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Class that represents a dictionary with all command line arguments from
    /// the current and legacy versions of the OPC Publisher. They are represented
    /// via configuration interfaces that is injected into the publisher container.
    /// </summary>
    public class PublisherCliOptions : Dictionary<string, string>,
        ISettingsController, IEngineConfiguration, IPublisherConfiguration,
        IRuntimeStateReporterConfiguration, ISubscriptionConfig
    {
        /// <summary>
        /// Creates a new instance of the cli options based on existing configuration values.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="logger"></param>
        public PublisherCliOptions(IConfiguration config, ILogger logger)
        {
            foreach (var item in config.GetChildren())
            {
                this[item.Key] = item.Value;
            }
            _logger = logger;
        }

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        public PublisherCliOptions(string[] args)
        {
            _logger = Log.Console<PublisherCliOptions>();

            var showHelp = false;
            var unsupportedOptions = new List<string>();
            var legacyOptions = new List<string>();

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
                { $"f|pf|publishfile=|{PublisherCliConfigKeys.PublishedNodesConfigurationFilename}=",
                    "The name of the file containing the configuration of the nodes to be published as well as the information to connect to the OPC UA server sources.\nThis file is also used to persist changes made through the control plane, e.g., through IoT Hub device method calls.\nWhen this file is specified, or the default file is accessible by the module, OPC Publisher will start in standalone mode.\nDefault: `publishednodes.json`\n",
                    s => this[PublisherCliConfigKeys.PublishedNodesConfigurationFilename] = s },
                { $"pfs|publishfileschema=|{PublisherCliConfigKeys.PublishedNodesConfigurationSchemaFilename}=",
                    "The validation schema filename for publish file. Schema validation is disabled by default.\nDefault: `not set` (disabled)\n",
                    s => this[PublisherCliConfigKeys.PublishedNodesConfigurationSchemaFilename] = s },
                { "s|site=",
                    "Sets the site name of the publisher module.\nDefault: `not set` \n",
                    s => this[PublisherCliConfigKeys.PublisherSite] = s},
                { $"rs|runtimestatereporting:|{PublisherCliConfigKeys.RuntimeStateReporting}:",
                    "Enable that when publisher starts or restarts it reports its runtime state using a restart message.\nDefault: `False` (disabled)\n",
                    (bool? b) => this[PublisherCliConfigKeys.RuntimeStateReporting] = b?.ToString() ?? "True"},

                "",
                "Messaging configuration",
                "-----------------------",
                "",

                { $"c|strict:|{PublisherCliConfigKeys.UseStandardsCompliantEncoding}:",
                    "Use strict UA compliant encodings. Default is 'false' for backwards (2.5.x - 2.8.x) compatibility. It is recommended to run the publisher in compliant mode for best interoperability.\nDefault: `False`\n",
                    (bool? b) => this[PublisherCliConfigKeys.UseStandardsCompliantEncoding] = b?.ToString() ?? "True" },
                { $"mm|messagingmode=|{PublisherCliConfigKeys.MessagingMode}=",
                    $"The messaging mode for messages Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessagingMode)))}`\nDefault: `{nameof(MessagingMode.PubSub)}` if `-c` is specified, otherwise `{nameof(MessagingMode.Samples)}` for backwards compatibility.\n",
                    (MessagingMode m) => this[PublisherCliConfigKeys.MessagingMode] = m.ToString() },

                // TODO: Add ability to specify networkmessage mask
                // TODO: Add ability to specify dataset message mask
                // TODO: Add ability to specify dataset field message mask
                // TODO: Allow override of content type
                // TODO: Allow overriding schema

                { $"me|messageencoding=|{PublisherCliConfigKeys.MessageEncoding}=",
                    $"The message encoding for messages Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessageEncoding)))}`\nDefault: `{nameof(MessageEncoding.Json)}`.\n",
                    (MessageEncoding m) => this[PublisherCliConfigKeys.MessageEncoding] = m.ToString() },
                    { $"fm|fullfeaturedmessage=|{PublisherCliConfigKeys.FullFeaturedMessage}=",
                        "The full featured mode for messages (all fields filled in) for backwards compatibilty. \nDefault: `False` for legacy compatibility.\n",
                        (string b) => this[PublisherCliConfigKeys.FullFeaturedMessage] = b, true },
                { $"bi|batchtriggerinterval=|{PublisherCliConfigKeys.BatchTriggerInterval}=",
                    "The network message publishing interval in milliseconds. Determines the publishing period at which point messages are emitted. When `--bs` is 1 and `--bi` is set to 0 batching is disabled.\nDefault: `10000` (10 seconds).\nAlternatively can be set using `BatchTriggerInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int k) => this[PublisherCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromMilliseconds(k).ToString() },
                    { "si|iothubsendinterval=",
                        "The network message publishing interval in seconds for backwards compatibilty. \nDefault: `10` seconds.\n",
                        (string k) => this[PublisherCliConfigKeys.BatchTriggerInterval] = TimeSpan.FromSeconds(int.Parse(k, CultureInfo.CurrentCulture)).ToString(), true },
                { $"bs|batchsize=|{PublisherCliConfigKeys.BatchSize}=",
                    "The number of incoming OPC UA subscription notifications to collect until sending a network messages. When `--bs` is set to 1 and `--bi` is 0 batching is disabled and messages are sent as soon as notifications arrive.\nDefault: `50`.\n",
                    (int i) => this[PublisherCliConfigKeys.BatchSize] = i.ToString(CultureInfo.CurrentCulture) },
                { $"ms|maxmessagesize=|iothubmessagesize=|{PublisherCliConfigKeys.IoTHubMaxMessageSize}=",
                    "The maximum size of the messages to emit. In case the encoder cannot encode a message because the size would be exceeded, the message is dropped. Otherwise the encoder will aim to chunk messages if possible. \nDefault: `256k` in case of IoT Hub messages, `0` otherwise.\n",
                    (int i) => this[PublisherCliConfigKeys.IoTHubMaxMessageSize] = i.ToString(CultureInfo.CurrentCulture) },

                // TODO: Add ConfiguredMessageSize

                { $"npd|maxnodesperdataset=|{PublisherCliConfigKeys.MaxNodesPerDataSet}=",
                    "Maximum number of nodes within a Subscription. When there are more nodes configured for a data set writer, they will be added to new subscriptions. This also affects metadata message size. \nDefault: `1000`.\n",
                    (int i) => this[PublisherCliConfigKeys.MaxNodesPerDataSet] = i.ToString(CultureInfo.CurrentCulture) },
                { $"kfc|keyframecount=|{PublisherCliConfigKeys.DefaultKeyFrameCount}=",
                    "The default number of delta messages to send until a key frame message is sent. If 0, no key frame messages are sent, if 1, every message will be a key frame. \nDefault: `0`.\n",
                    (int i) => this[PublisherCliConfigKeys.DefaultKeyFrameCount] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"msi|metadatasendinterval=|{PublisherCliConfigKeys.DefaultMetaDataUpdateTime}=",
                    "Default value in milliseconds for the metadata send interval which determines in which interval metadata is sent.\nEven when disabled, metadata is still sent when the metadata version changes unless `--mm=*Samples` is set in which case this setting is ignored. Only valid for network message encodings. \nDefault: `0` which means periodic sending of metadata is disabled.\n",
                    (int i) => this[PublisherCliConfigKeys.DefaultMetaDataUpdateTime] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"dm|disablemetadata:|{PublisherCliConfigKeys.DisableDataSetMetaData}:",
                    "Disables sending any metadata when metadata version changes. This setting can be used to also override the messaging profile's default support for metadata sending. \nDefault: `False` if the messaging profile selected supports sending metadata, `True` otherwise.\n",
                    (bool? b) => this[PublisherCliConfigKeys.DisableDataSetMetaData] = b?.ToString() ?? "True" },
                    { $"lc|legacycompatibility=|{PublisherCliConfigKeys.LegacyCompatibility}=",
                        "Run the publisher in legacy (2.5.x) compatibility mode.\nDefault: `False` (disabled).\n",
                        (string b) => this[PublisherCliConfigKeys.LegacyCompatibility] = b, true },

                "",
                "Transport settings",
                "------------------",
                "",

                { $"b|mqc=|mqttclientconnectionstring=|{PublisherCliConfigKeys.MqttClientConnectionString}=",
                    "An mqtt client connection string to use. Use this option to connect OPC Publisher to a MQTT Broker or to an EdgeHub or IoT Hub MQTT endpoint.\nTo connect to an MQTT broker use the format 'HostName=<IPorDnsName>;Port=<Port>[;Username=<Username>;Password=<Password>;DeviceId=<IoTDeviceId>;Protocol=<v500 or v311>]'.\nTo connect to IoT Hub or EdgeHub MQTT endpoint use a regular IoT Hub connection string.\nIgnored if `-c` option is used to set a connection string.\nDefault: `not set` (disabled).\nFor more information consult https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#using-the-mqtt-protocol-directly-as-a-device) and https://learn.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support#for-azure-iot-tools) on how to retrieve the device connection string or generate a SharedAccessSignature for one.\n",
                    mqc => this[PublisherCliConfigKeys.MqttClientConnectionString] = mqc },
                { $"ttt|telemetrytopictemplate=|{PublisherCliConfigKeys.TelemetryTopicTemplateKey}=",
                    "A template that shall be used to build the topic for outgoing telemetry messages. If not specified IoT Hub and EdgeHub compatible topics will be used. The placeholder '{device_id}' can be used to inject the device id and '{output_name}' to inject routing info into the topic template.\nDefault: `not set`.\n",
                    ttt => this[PublisherCliConfigKeys.TelemetryTopicTemplateKey] = ttt },
                { $"ri|enableroutinginfo:|{PublisherCliConfigKeys.EnableRoutingInfo}:",
                    "Add routing information to telemetry messages. The name of the property is `$$RoutingInfo` and the value is the `DataSetWriterGroup` for that particular message.\nWhen the `DataSetWriterGroup` is not configured, the `$$RoutingInfo` property will not be added to the message even if this argument is set.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[PublisherCliConfigKeys.EnableRoutingInfo] = b?.ToString() ?? "True" },
                { $"mqn|metadataqueuename:|{PublisherCliConfigKeys.DefaultDataSetMetaDataQueueName}:",
                    "The output that metadata should be sent to.\nThis will be a sub path of the configured telemetry topic or replacement of the output name token in the topic template, or in case of EdgeHub, the output name or appended sub path to existing configured output name.\nIn case of MQTT the message will be sent as RETAIN message with a TTL of either metadata send interval or infinite if metadata send interval is not configured.\nOnly valid if metadata is supported and/or explicitely enabled. \nDefault: `disabled` which means metadata is sent to the same output as regular messages. If specified without value, the default output is `$metadata`.\n",
                    (string s) => this[PublisherCliConfigKeys.DefaultDataSetMetaDataQueueName] = !string.IsNullOrEmpty(s) ? s : "$metadata" },
                { $"ht|ih=|iothubprotocol=|{PublisherCliConfigKeys.HubTransport}=",
                    $"Protocol to use for communication with EdgeHub. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(TransportOption)))}`\nDefault: `{nameof(TransportOption.Mqtt)}` if device or edge hub connection string is provided, ignored otherwise.\n",
                    (TransportOption p) => this[PublisherCliConfigKeys.HubTransport] = p.ToString() },
                { $"ec|edgehubconnectionstring=|dc=|deviceconnectionstring=|{PublisherCliConfigKeys.EdgeHubConnectionString}=",
                    "A edge hub or iot hub connection string to use if you run OPC Publisher outside of IoT Edge. The connection string can be obtained from the IoT Hub portal. Use this setting for testing only.\nDefault: `not set`.\n",
                    dc => this[PublisherCliConfigKeys.EdgeHubConnectionString] = dc },
                { $"{PublisherCliConfigKeys.BypassCertVerificationKey}=",
                    "Enables bypass of certificate verification for upstream communication to edgeHub. This setting is for debugging purposes only and should not be used in production.\nDefault: `False`\n",
                    (bool b) => this[PublisherCliConfigKeys.BypassCertVerificationKey] = b.ToString() },
                { $"om|maxoutgressmessages=|{PublisherCliConfigKeys.MaxOutgressMessages}=",
                    "The maximum number of messages to buffer on the send path before messages are dropped.\nDefault: `4096`\n",
                    (int i) => this[PublisherCliConfigKeys.MaxOutgressMessages] = i.ToString(CultureInfo.InvariantCulture) },

                "",
                "Subscription settings",
                "---------------------",
                "",

                { $"oi|opcsamplinginterval=|{PublisherCliConfigKeys.OpcSamplingInterval}=",
                    "Default value in milliseconds to request the servers to sample values. This value is used if an explicit sampling interval for a node was not configured. \nDefault: `1000`.\nAlternatively can be set using `DefaultSamplingInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[PublisherCliConfigKeys.OpcSamplingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"op|opcpublishinginterval=|{PublisherCliConfigKeys.OpcPublishingInterval}=",
                    "Default value in milliseconds for the publishing interval setting of a subscription created with an OPC UA server. This value is used if an explicit publishing interval was not configured.\nDefault: `1000`.\nAlternatively can be set using `DefaultPublishingInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[PublisherCliConfigKeys.OpcPublishingInterval] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"ki|keepaliveinterval=|{PublisherCliConfigKeys.OpcKeepAliveIntervalInSec}=",
                    "The interval in seconds the publisher is sending keep alive messages to the OPC servers on the endpoints it is connected to.\nDefault: `10000` (10 seconds).\n",
                    (int i) => this[PublisherCliConfigKeys.OpcKeepAliveIntervalInSec] = i.ToString(CultureInfo.CurrentCulture) },
                { $"kt|keepalivethreshold=|{PublisherCliConfigKeys.OpcKeepAliveDisconnectThreshold}=",
                    "Specify the number of keep alive packets a server can miss, before the session is disconneced.\nDefault: `50`.\n",
                    (uint u) => this[PublisherCliConfigKeys.OpcKeepAliveDisconnectThreshold] = u.ToString(CultureInfo.CurrentCulture) },
                { $"fd|fetchdisplayname:|{PublisherCliConfigKeys.FetchOpcNodeDisplayName}:",
                    "Fetches the displayname for the monitored items subscribed if a display name was not specified in the configuration.\nNote: This has high impact on OPC Publisher startup performance.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[PublisherCliConfigKeys.FetchOpcNodeDisplayName] = b?.ToString() ?? "True" },
                { $"qs|queuesize=|{PublisherCliConfigKeys.DefaultQueueSize}=",
                    "Default queue size for all monitored items if queue size was not specified in the configuration.\nDefault: `1` (for backwards compatibility).\n",
                    (uint u) => this[PublisherCliConfigKeys.DefaultQueueSize] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ndo|nodiscardold:|{PublisherCliConfigKeys.DiscardNewDefault}:",
                    "The publisher is using this as default value for the discard old setting of monitored item queue configuration. Setting to true will ensure that new values are dropped before older ones are drained. \nDefault: `False` (which is the OPC UA default).\n",
                    (bool? b) => this[PublisherCliConfigKeys.DiscardNewDefault] = b?.ToString() ?? "True" },
                { $"mc|monitoreditemdatachangetrigger=|{PublisherCliConfigKeys.DefaultDataChangeTrigger}=",
                    $"Default data change trigger for all monitored items configured in the published nodes configuration unless explicitly overridden. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(DataChangeTriggerType)))}`\nDefault: `{nameof(DataChangeTriggerType.StatusValue)}` (which is the OPC UA default).\n",
                    (DataChangeTriggerType t) => this[PublisherCliConfigKeys.DefaultDataChangeTrigger] = t.ToString() },
                { $"sf|skipfirst:|{PublisherCliConfigKeys.SkipFirstDefault}:",
                    "The publisher is using this as default value for the skip first setting of nodes configured without a skip first setting. A value of True will skip sending the first notification received when the monitored item is added to the subscription.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[PublisherCliConfigKeys.SkipFirstDefault] = b?.ToString() ?? "True" },
                    { "skipfirstevent:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[PublisherCliConfigKeys.SkipFirstDefault] = b ?? "True", /* hidden = */ true },
                { $"hb|heartbeatinterval=|{PublisherCliConfigKeys.HeartbeatIntervalDefault}=",
                    "The publisher is using this as default value in seconds for the heartbeat interval setting of nodes that were configured without a heartbeat interval setting. A heartbeat is sent at this interval if no value has been received.\nDefault: `0` (disabled)\nAlternatively can be set using `DefaultHeartbeatInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[PublisherCliConfigKeys.HeartbeatIntervalDefault] = TimeSpan.FromSeconds(i).ToString() },

                "",
                "OPC UA Client configuration",
                "---------------------------",
                "",

                { $"aa|acceptuntrusted:|{PublisherCliConfigKeys.AutoAcceptCerts}:",
                    "The publisher accepts untrusted certificates presented by a server it connects to.\nThis does not include servers presenting bad certificates or certificates that fail chain validation. These errors cannot be suppressed and connection will always be rejected.\nWARNING: This setting should never be used in production environments!\n",
                    (bool? b) => this[PublisherCliConfigKeys.AutoAcceptCerts] = b?.ToString() ?? "True" },
                     { "autoaccept:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[PublisherCliConfigKeys.AutoAcceptCerts] = b ?? "True", /* hidden = */ true },
                { $"ot|operationtimeout=|{PublisherCliConfigKeys.OpcOperationTimeout}=",
                    "The operation service call timeout of the publisher OPC UA client in milliseconds. \nDefault: `120000` (2 minutes).\n",
                    (uint u) => this[PublisherCliConfigKeys.OpcOperationTimeout] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ct|createsessiontimeout=|{PublisherCliConfigKeys.OpcSessionCreationTimeout}=",
                    "Maximum amount of time in seconds that a session should remain open by the OPC server without any activity (session timeout) to request from the OPC server at session creation.\nDefault: `not set`.\n",
                    (uint u) => this[PublisherCliConfigKeys.OpcSessionCreationTimeout] = u.ToString(CultureInfo.CurrentCulture) },
                { $"slt|{PublisherCliConfigKeys.MinSubscriptionLifetimeKey}=",
                    "Minimum subscription lifetime in seconds as per OPC UA definition.\nDefault: `not set`.\n",
                    (int i) => this[PublisherCliConfigKeys.MinSubscriptionLifetimeKey] = i.ToString(CultureInfo.CurrentCulture) },

                { $"otl|opctokenlifetime=|{PublisherCliConfigKeys.SecurityTokenLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds.\nDefault: `3600000` (1h).\n",
                    (uint u) => this[PublisherCliConfigKeys.SecurityTokenLifetimeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ocl|opcchannellifetime=|{PublisherCliConfigKeys.ChannelLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds.\nDefault: `300000` (5 min).\n",
                    (uint u) => this[PublisherCliConfigKeys.ChannelLifetimeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"omb|opcmaxbufferlen=|{PublisherCliConfigKeys.MaxBufferSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max buffer size.\nDefault: `65535` (64KB -1).\n",
                    (uint u) => this[PublisherCliConfigKeys.MaxBufferSizeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"oml|opcmaxmessagelen=|{PublisherCliConfigKeys.MaxMessageSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max message size.\nDefault: `4194304` (4 MB).\n",
                    (uint u) => this[PublisherCliConfigKeys.MaxMessageSizeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"oal|opcmaxarraylen=|{PublisherCliConfigKeys.MaxArrayLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max array length.\nDefault: `65535` (64KB - 1).\n",
                    (uint u) => this[PublisherCliConfigKeys.MaxArrayLengthKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ol|opcmaxstringlen=|{PublisherCliConfigKeys.OpcMaxStringLength}=",
                    "The max length of a string opc can transmit/receive over the OPC UA secure channel.\nDefault: `130816` (128KB - 256).\n",
                    (uint u) => this[PublisherCliConfigKeys.OpcMaxStringLength] = u.ToString(CultureInfo.CurrentCulture) },
                { $"obl|opcmaxbytestringlen=|{PublisherCliConfigKeys.MaxByteStringLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max byte string length.\nDefault: `1048576` (1MB).\n",
                    (uint u) => this[PublisherCliConfigKeys.MaxByteStringLengthKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"au|appuri=|{PublisherCliConfigKeys.ApplicationUriKey}=",
                    "Application URI as per OPC UA definition inside the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[PublisherCliConfigKeys.ApplicationUriKey] = s },
                { $"pu|producturi=|{PublisherCliConfigKeys.ProductUriKey}=",
                    "The Product URI as per OPC UA definition insde the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[PublisherCliConfigKeys.ProductUriKey] = s },

                { $"rejectsha1=|{PublisherCliConfigKeys.RejectSha1SignedCertificatesKey}=",
                    "The publisher rejects deprecated SHA1 certificates.\nNote: It is recommended to always set this value to `True` if the connected OPC UA servers does not use Sha1 signed certificates.\nDefault: `False` (to support older equipment).\n",
                    (bool b) => this[PublisherCliConfigKeys.RejectSha1SignedCertificatesKey] = b.ToString() },
                { $"mks|minkeysize=|{PublisherCliConfigKeys.MinimumCertificateKeySizeKey}=",
                    "Minimum accepted certificate size.\nNote: It is recommended to this value to the highest certificate key size possible based on the connected OPC UA servers.\nDefault: 1024.\n",
                    s => this[PublisherCliConfigKeys.MinimumCertificateKeySizeKey] = s },
                { $"tm|trustmyself=|{PublisherCliConfigKeys.TrustMyself}=",
                    "Set to `False` to disable adding the publisher's own certificate to the trusted store automatically.\nDefault: `True`.\n",
                    (bool b) => this[PublisherCliConfigKeys.TrustMyself] = b.ToString() },
                { $"sn|appcertsubjectname=|{PublisherCliConfigKeys.OpcApplicationCertificateSubjectName}=",
                    "The subject name for the app cert.\nDefault: `CN=Microsoft.Azure.IIoT, C=DE, S=Bav, O=Microsoft, DC=localhost`.\n",
                    s => this[PublisherCliConfigKeys.OpcApplicationCertificateSubjectName] = s },
                { $"an|appname=|{PublisherCliConfigKeys.OpcApplicationName}=",
                    "The name for the app (used during OPC UA authentication).\nDefault: `Microsoft.Azure.IIoT`\n",
                    s => this[PublisherCliConfigKeys.OpcApplicationName] = s },
                { $"pki|pkirootpath=|{PublisherCliConfigKeys.PkiRootPathKey}=",
                    "PKI certificate store root path.\nDefault: `pki`.\n",
                    s => this[PublisherCliConfigKeys.PkiRootPathKey] = s },
                { $"ap|appcertstorepath=|{PublisherCliConfigKeys.OpcOwnCertStorePath}=",
                    "The path where the own application cert should be stored.\nDefault: $\"{PkiRootPath}/own\".\n",
                    s => this[PublisherCliConfigKeys.OpcOwnCertStorePath] = s },
                { $"apt|at=|appcertstoretype=|{PublisherCliConfigKeys.OpcOwnCertStoreType}=",
                    $"The own application cert store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, PublisherCliConfigKeys.OpcOwnCertStoreType, "apt") },
                { $"tp|trustedcertstorepath=|{PublisherCliConfigKeys.OpcTrustedCertStorePath}=",
                    "The path of the trusted cert store.\nDefault: $\"{PkiRootPath}/trusted\".\n",
                    s => this[PublisherCliConfigKeys.OpcTrustedCertStorePath] = s },
                { $"tpt|{PublisherCliConfigKeys.TrustedPeerCertificatesTypeKey}=",
                    $"Trusted peer certificate store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, PublisherCliConfigKeys.TrustedPeerCertificatesTypeKey, "tpt") },
                { $"rp|rejectedcertstorepath=|{PublisherCliConfigKeys.OpcRejectedCertStorePath}=",
                    "The path of the rejected cert store.\nDefault: $\"{PkiRootPath}/rejected\".\n",
                    s => this[PublisherCliConfigKeys.OpcRejectedCertStorePath] = s },
                { $"rpt|{PublisherCliConfigKeys.RejectedCertificateStoreTypeKey}=",
                    $"Rejected certificate store type. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, PublisherCliConfigKeys.RejectedCertificateStoreTypeKey, "rpt") },
                { $"ip|issuercertstorepath=|{PublisherCliConfigKeys.OpcIssuerCertStorePath}=",
                    "The path of the trusted issuer cert store.\nDefault: $\"{PkiRootPath}/issuers\".\n",
                    s => this[PublisherCliConfigKeys.OpcIssuerCertStorePath] = s },
                { $"tit|{PublisherCliConfigKeys.TrustedIssuerCertificatesTypeKey}=",
                    $"Trusted issuer certificate store types. Allowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, PublisherCliConfigKeys.TrustedIssuerCertificatesTypeKey, "tit") },

                "",
                "Diagnostic options",
                "------------------",
                "",

                { $"di|diagnosticsinterval=|{PublisherCliConfigKeys.DiagnosticsInterval}=",
                    "Shows publisher diagnostic information at this specified interval in seconds in the OPC Publisher log (need log level info). `-1` disables remote diagnostic log and diagnostic output.\nDefault:60000 (60 seconds).\nAlternatively can be set using `DiagnosticsInterval` environment variable in the form of a time span string formatted string `[d.]hh:mm:ss[.fffffff]`\".\n",
                    (int i) => this[PublisherCliConfigKeys.DiagnosticsInterval] = TimeSpan.FromSeconds(i).ToString() },
                { "ll|loglevel=",
                    $"The loglevel to use. Allowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(LogLevel)))}`\nDefault: `{LogLevel.Information}`.\n",
                    (LogLevel l) => this[PublisherCliConfigKeys.LogLevelKey] = l.ToString() },
                { $"em|{PublisherCliConfigKeys.EnableMetricsKey}=",
                    "Enables exporting prometheus metrics on the default prometheus endpoint.\nDefault: `True` (set to `False` to disable metrics exporting).\n",
                    (bool b) => this[PublisherCliConfigKeys.EnableMetricsKey] = b.ToString() },

                // testing purposes

                { "sc|scaletestcount=",
                    "The number of monitored item clones in scale tests.\n",
                    (string i) => this[PublisherCliConfigKeys.ScaleTestCount] = i, true },

                // Legacy: unsupported and hidden
                { "mq|monitoreditemqueuecapacity=", "Legacy - do not use.", _ => legacyOptions.Add("mq|monitoreditemqueuecapacity"), true },
                { "tc|telemetryconfigfile=", "Legacy - do not use.", _ => legacyOptions.Add("tc|telemetryconfigfile"), true },
                { "ic|iotcentral=", "Legacy - do not use.", _ => legacyOptions.Add("ic|iotcentral"), true },
                { "ns|noshutdown=", "Legacy - do not use.", _ => legacyOptions.Add("ns|noshutdown"), true },
                { "rf|runforever", "Legacy - do not use.", _ => legacyOptions.Add("rf|runforever"), true },
                { "pn|portnum=", "Legacy - do not use.", _ => legacyOptions.Add("pn|portnum"), true },
                { "pa|path=", "Legacy - do not use.", _ => legacyOptions.Add("pa|path"), true },
                { "lr|ldsreginterval=", "Legacy - do not use.", _ => legacyOptions.Add("lr|ldsreginterval"), true },
                { "ss|suppressedopcstatuscodes=", "Legacy - do not use.", _ => legacyOptions.Add("ss|suppressedopcstatuscodes"), true },
                { "csr", "Legacy - do not use.", _ => legacyOptions.Add("csr"), true },
                { "ab|applicationcertbase64=", "Legacy - do not use.",_ => legacyOptions.Add("ab|applicationcertbase64"), true },
                { "af|applicationcertfile=", "Legacy - do not use.", _ => legacyOptions.Add("af|applicationcertfile"), true },
                { "pk|privatekeyfile=", "Legacy - do not use.", _ => legacyOptions.Add("pk|privatekeyfile"), true },
                { "pb|privatekeybase64=", "Legacy - do not use.", _ => legacyOptions.Add("pb|privatekeybase64"), true },
                { "cp|certpassword=", "Legacy - do not use.", _ => legacyOptions.Add("cp|certpassword"), true },
                { "tb|addtrustedcertbase64=", "Legacy - do not use.", _ => legacyOptions.Add("tb|addtrustedcertbase64"), true },
                { "tf|addtrustedcertfile=", "Legacy - do not use.", _ => legacyOptions.Add("tf|addtrustedcertfile"), true },
                { "tt|trustedcertstoretype=", "Legacy - do not use.", _ => legacyOptions.Add("tt|trustedcertstoretype"), true },
                { "rt|rejectedcertstoretype=", "Legacy - do not use.", _ => legacyOptions.Add("rt|rejectedcertstoretype"), true },
                { "it|issuercertstoretype=", "Legacy - do not use.", _ => legacyOptions.Add("it|issuercertstoretype"), true },
                { "ib|addissuercertbase64=", "Legacy - do not use.", _ => legacyOptions.Add("ib|addissuercertbase64"), true },
                { "if|addissuercertfile=", "Legacy - do not use.", _ => legacyOptions.Add("if|addissuercertfile"), true },
                { "rb|updatecrlbase64=", "Legacy - do not use.", _ => legacyOptions.Add("rb|updatecrlbase64"), true },
                { "uc|updatecrlfile=", "Legacy - do not use.", _ => legacyOptions.Add("uc|updatecrlfile"), true },
                { "rc|removecert=", "Legacy - do not use.", _ => legacyOptions.Add("rc|removecert"), true },
                { "dt|devicecertstoretype=", "Legacy - do not use.", _ => legacyOptions.Add("dt|devicecertstoretype"), true },
                { "dp|devicecertstorepath=", "Legacy - do not use.", _ => legacyOptions.Add("dp|devicecertstorepath"), true },
                { "i|install", "Legacy - do not use.", _ => legacyOptions.Add("i|install"), true },
                { "st|opcstacktracemask=", "Legacy - do not use.", _ => legacyOptions.Add("st|opcstacktracemask"), true },
                { "sd|shopfloordomain=", "Legacy - do not use.", _ => legacyOptions.Add("sd|shopfloordomain"), true },
                { "vc|verboseconsole=", "Legacy - do not use.", _ => legacyOptions.Add("vc|verboseconsole"), true },
                { "as|autotrustservercerts=", "Legacy - do not use.", _ => legacyOptions.Add("as|autotrustservercerts"), true },
                { "l|lf|logfile=", "Legacy - do not use.", _ => legacyOptions.Add("l|lf|logfile"), true },
                { "lt|logflushtimespan=", "Legacy - do not use.", _ => legacyOptions.Add("lt|logflushtimespan"), true }
            };

            try
            {
                unsupportedOptions = options.Parse(args);
            }
            catch (Exception e)
            {
                Warning("Parse args exception: " + e.Message);
                ExitProcess(160);
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var key in Keys)
                {
                    Debug("Parsed command line option: '{Key}'='{Value}'", key, this[key]);
                }
            }

            if (unsupportedOptions.Count > 0)
            {
                foreach (var option in unsupportedOptions)
                {
                    Warning("Option {Option} wrong or not supported, " +
                        "please use -h option to get all the supported options.", option);
                }
            }

            if (legacyOptions.Count > 0)
            {
                foreach (var option in legacyOptions)
                {
                    Warning("Legacy option {option} not supported, please use -h option to get all the supported options.", option);
                }
            }

            if (!showHelp)
            {
                if (!MessagingProfile.IsSupported(StandaloneCliModel.MessagingMode, StandaloneCliModel.MessageEncoding))
                {
                    Warning("The specified combination of --mm, and --me is not (yet) supported. Currently supported combinations are: {MessageProfiles}), " +
                            "please use -h option to get all the supported options.",
                        MessagingProfile.Supported.Select(p => $"\n(--mm {p.MessagingMode} and --me {p.MessageEncoding})").Aggregate((a, b) => $"{a}, {b}"));
                    ExitProcess(170);
                    return;
                }

                // Check that the important values are provided
                else if (!ContainsKey(PublisherCliConfigKeys.MqttClientConnectionString) &&
                    !ContainsKey(PublisherCliConfigKeys.EdgeHubConnectionString) &&
                    Environment.GetEnvironmentVariable(IoTEdgeVariables.IOTEDGE_DEVICEID) == null &&
                    Environment.GetEnvironmentVariable(PublisherCliConfigKeys.EdgeHubConnectionString) == null)
                {
                    Warning("You must specify a connection string or run inside IoT Edge context, " +
                            "please use -h option to get all the supported options.");
                    ExitProcess(180);
                    return;
                }
            }
            else
            {
                options.WriteOptionDescriptions(Console.Out);
                var markdown = MessagingProfile.GetAllAsMarkdownTable();
#if WRITETABLE
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("The following messaging profiles are supported (selected with --mm and --me):");
                Console.WriteLine();
                Console.WriteLine(markdown);
#endif
                ExitProcess(0);
            }

            void SetStoreType(string s, string storeTypeKey, string optionName)
            {
                if (s.Equals(CertificateStoreType.X509Store, StringComparison.OrdinalIgnoreCase) ||
                            s.Equals(CertificateStoreType.Directory, StringComparison.OrdinalIgnoreCase))
                {
                    this[storeTypeKey] = s;
                    return;
                }
                throw new OptionException("Bad store type", optionName);
            }
        }

        /// <inheritdoc/>
        public string Site => StandaloneCliModel.PublisherSite;

        /// <inheritdoc/>
        public int? BatchSize => StandaloneCliModel.BatchSize;

        /// <inheritdoc/>
        public TimeSpan? BatchTriggerInterval => StandaloneCliModel.BatchTriggerInterval;

        /// <inheritdoc/>
        public TimeSpan? DiagnosticsInterval => StandaloneCliModel.DiagnosticsInterval;

        /// <inheritdoc/>
        public int? MaxMessageSize => StandaloneCliModel.MaxMessageSize;

        /// <inheritdoc/>
        public int? MaxOutgressMessages => StandaloneCliModel.MaxOutgressMessages;

        /// <inheritdoc/>
        public bool UseStandardsCompliantEncoding => StandaloneCliModel.UseStandardsCompliantEncoding;

        /// <inheritdoc/>
        public bool EnableRoutingInfo => StandaloneCliModel.EnableRoutingInfo;

        /// <inheritdoc/>
        public string DefaultMetaDataQueueName => StandaloneCliModel.DefaultMetaDataQueueName;

        /// <inheritdoc/>
        public uint? DefaultMaxMessagesPerPublish => StandaloneCliModel.DefaultMaxMessagesPerPublish;

        /// <inheritdoc/>
        public bool EnableRuntimeStateReporting => StandaloneCliModel.EnableRuntimeStateReporting;

        /// <inheritdoc/>
        public uint? DefaultKeyFrameCount => StandaloneCliModel.DefaultKeyFrameCount;

        /// <inheritdoc/>
        public bool? DisableKeyFrames => !StandaloneCliModel.MessagingProfile.SupportsKeyFrames;

        /// <inheritdoc/>
        public TimeSpan? DefaultHeartbeatInterval => StandaloneCliModel.DefaultHeartbeatInterval;

        /// <inheritdoc/>
        public bool DefaultSkipFirst => StandaloneCliModel.DefaultSkipFirst;

        /// <inheritdoc/>
        public bool DefaultDiscardNew => StandaloneCliModel.DefaultDiscardNew ?? false;

        /// <inheritdoc/>
        public TimeSpan? DefaultSamplingInterval => StandaloneCliModel.DefaultSamplingInterval;

        /// <inheritdoc/>
        public TimeSpan? DefaultPublishingInterval => StandaloneCliModel.DefaultPublishingInterval;

        /// <inheritdoc/>
        public TimeSpan? DefaultMetaDataUpdateTime => StandaloneCliModel.DefaultMetaDataUpdateTime;

        /// <inheritdoc/>
        public uint? DefaultKeepAliveCount => StandaloneCliModel.DefaultKeepAliveCount;

        /// <inheritdoc/>
        public uint? DefaultLifeTimeCount => StandaloneCliModel.DefaultLifeTimeCount;

        /// <inheritdoc/>
        public bool? DisableDataSetMetaData => StandaloneCliModel.DisableDataSetMetaData
            ?? !StandaloneCliModel.MessagingProfile.SupportsMetadata;

        /// <inheritdoc/>
        public bool ResolveDisplayName => StandaloneCliModel.FetchOpcNodeDisplayName;

        /// <inheritdoc/>
        public uint? DefaultQueueSize => StandaloneCliModel.DefaultQueueSize;

        /// <inheritdoc/>
        public DataChangeTriggerType? DefaultDataChangeTrigger { get; }

        /// <inheritdoc/>
        public string PublishedNodesFile => StandaloneCliModel.PublishedNodesFile;

        /// <inheritdoc/>
        public string PublishedNodesSchemaFile => StandaloneCliModel.PublishedNodesSchemaFile;

        /// <inheritdoc/>
        public int MaxNodesPerPublishedEndpoint => StandaloneCliModel.MaxNodesPerPublishedEndpoint;

        /// <inheritdoc/>
        public MessagingProfile MessagingProfile => StandaloneCliModel.MessagingProfile;

        /// <inheritdoc/>
        public int? ScaleTestCount => StandaloneCliModel.ScaleTestCount;

        /// <summary>
        /// The model of the CLI arguments.
        /// </summary>
        public StandaloneCliModel StandaloneCliModel
        {
            get
            {
                _standaloneCliModel ??= ToStandaloneCliModel();

                return _standaloneCliModel;
            }
        }

        /// <summary>
        /// Call exit with exit code
        /// </summary>
        /// <param name="exitCode"></param>
        public virtual void ExitProcess(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        public virtual void Warning(string messageTemplate)
        {
            _logger.LogWarning(messageTemplate);
        }

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValue">Object positionally formatted into the message template.</param>
        public virtual void Warning<T>(string messageTemplate, T propertyValue)
        {
            _logger.LogWarning(messageTemplate, propertyValue);
        }

        /// <summary>
        /// Write a log event with the Debug level.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="messageTemplate"></param>
        /// <param name="propertyValue0"></param>
        /// <param name="propertyValue1"></param>
        public virtual void Debug<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            _logger.LogDebug(messageTemplate, propertyValue0, propertyValue1);
        }

        private StandaloneCliModel ToStandaloneCliModel()
        {
            var model = new StandaloneCliModel
            {
                PublishedNodesFile = GetValueOrDefault(PublisherCliConfigKeys.PublishedNodesConfigurationFilename, PublisherCliConfigKeys.DefaultPublishedNodesFilename),
                PublishedNodesSchemaFile = GetValueOrDefault(PublisherCliConfigKeys.PublishedNodesConfigurationSchemaFilename, PublisherCliConfigKeys.DefaultPublishedNodesSchemaFilename)
            };
            model.PublisherSite = GetValueOrDefault(PublisherCliConfigKeys.PublisherSite, model.PublisherSite);
            model.UseStandardsCompliantEncoding = GetValueOrDefault(PublisherCliConfigKeys.UseStandardsCompliantEncoding, model.UseStandardsCompliantEncoding);
            model.DefaultHeartbeatInterval = GetValueOrDefault(PublisherCliConfigKeys.HeartbeatIntervalDefault, model.DefaultHeartbeatInterval);
            model.DefaultSkipFirst = GetValueOrDefault(PublisherCliConfigKeys.SkipFirstDefault, model.DefaultSkipFirst);
            model.DefaultDiscardNew = GetValueOrDefault(PublisherCliConfigKeys.DiscardNewDefault, model.DefaultDiscardNew);
            model.DefaultSamplingInterval = GetValueOrDefault(PublisherCliConfigKeys.OpcSamplingInterval, model.DefaultSamplingInterval);
            model.DefaultPublishingInterval = GetValueOrDefault(PublisherCliConfigKeys.OpcPublishingInterval, model.DefaultPublishingInterval);
            model.DefaultMetaDataUpdateTime = GetValueOrDefault(PublisherCliConfigKeys.DefaultMetaDataUpdateTime, model.DefaultMetaDataUpdateTime);
            model.DefaultMetaDataQueueName = GetValueOrDefault(PublisherCliConfigKeys.DefaultDataSetMetaDataQueueName, model.DefaultMetaDataQueueName);
            model.DisableDataSetMetaData = GetValueOrDefault(PublisherCliConfigKeys.DisableDataSetMetaData, model.DisableDataSetMetaData);
            model.DefaultKeyFrameCount = GetValueOrDefault(PublisherCliConfigKeys.DefaultKeyFrameCount, model.DefaultKeyFrameCount);
            model.FetchOpcNodeDisplayName = GetValueOrDefault(PublisherCliConfigKeys.FetchOpcNodeDisplayName, model.FetchOpcNodeDisplayName);
            model.DefaultQueueSize = GetValueOrDefault(PublisherCliConfigKeys.DefaultQueueSize, model.DefaultQueueSize);
            model.DiagnosticsInterval = GetValueOrDefault(PublisherCliConfigKeys.DiagnosticsInterval, model.DiagnosticsInterval);
            model.LogFileFlushTimeSpan = GetValueOrDefault(PublisherCliConfigKeys.LogFileFlushTimeSpanSec, model.LogFileFlushTimeSpan);
            model.LogFilename = GetValueOrDefault(PublisherCliConfigKeys.LogFileName, model.LogFilename);
            model.SetFullFeaturedMessage(GetValueOrDefault(PublisherCliConfigKeys.FullFeaturedMessage, false));
            model.MessagingMode = GetValueOrDefault(PublisherCliConfigKeys.MessagingMode, model.MessagingMode);
            model.MessageEncoding = GetValueOrDefault(PublisherCliConfigKeys.MessageEncoding, model.MessageEncoding);
            model.BatchSize = GetValueOrDefault(PublisherCliConfigKeys.BatchSize, model.BatchSize);
            model.BatchTriggerInterval = GetValueOrDefault(PublisherCliConfigKeys.BatchTriggerInterval, model.BatchTriggerInterval);
            model.MaxMessageSize = GetValueOrDefault(PublisherCliConfigKeys.IoTHubMaxMessageSize, model.MaxMessageSize);
            model.ScaleTestCount = GetValueOrDefault(PublisherCliConfigKeys.ScaleTestCount, model.ScaleTestCount);
            model.MaxOutgressMessages = GetValueOrDefault(PublisherCliConfigKeys.MaxOutgressMessages, model.MaxOutgressMessages);
            model.MaxNodesPerPublishedEndpoint = GetValueOrDefault(PublisherCliConfigKeys.MaxNodesPerDataSet, model.MaxNodesPerPublishedEndpoint);
            model.LegacyCompatibility = GetValueOrDefault(PublisherCliConfigKeys.LegacyCompatibility, model.LegacyCompatibility);
            model.EnableRuntimeStateReporting = GetValueOrDefault(PublisherCliConfigKeys.RuntimeStateReporting, model.EnableRuntimeStateReporting);
            model.EnableRoutingInfo = GetValueOrDefault(PublisherCliConfigKeys.EnableRoutingInfo, model.EnableRoutingInfo);
            return model;
        }

        private T GetValueOrDefault<T>(string key, T defaultValue)
        {
            if (!ContainsKey(key))
            {
                return defaultValue;
            }
            var converter = TypeDescriptor.GetConverter(typeof(T));
            return (T)converter.ConvertFrom(this[key]);
        }

        private readonly ILogger _logger;
        private StandaloneCliModel _standaloneCliModel;
    }
}
