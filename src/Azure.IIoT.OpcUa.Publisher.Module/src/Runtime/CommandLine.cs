// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Runtime
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack.Runtime;
    using Furly.Azure.IoT.Edge;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Mono.Options;
    using Opc.Ua;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Class that represents a dictionary with all command line arguments from
    /// the current and legacy versions of the OPC Publisher. They are represented
    /// via configuration interfaces that is injected into the publisher container.
    /// </summary>
    public class CommandLine : Dictionary<string, string?>
    {
        /// <summary>
        /// Creates a new instance of the cli options based on existing configuration values.
        /// </summary>
        public CommandLine()
        {
        }

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        public CommandLine(string[] args)
        {
            var showHelp = false;
            var unsupportedOptions = new List<string>();
            var legacyOptions = new List<string>();

            // TODO: Remove. Key for the Legacy (2.5.x) compatibility mode. Controlled the content type value
            const string LegacyCompatibility = "LegacyCompatibility";

            // command line options
            var options = new Mono.Options.OptionSet
            {
                "",
                "General",
                "-------",
                "",

                // show help
                { "h|help",
                    "Show help and exit.\n",
                    _ => showHelp = true },

                // Publisher configuration options
                { $"f|pf|publishfile=|{PublisherConfig.PublishedNodesFileKey}=",
                    "The name of the file containing the configuration of the nodes to be published as well as the information to connect to the OPC UA server sources.\nThis file is also used to persist changes made through the control plane, e.g., through IoT Hub device method calls.\nWhen no file is specified a default `publishednodes.json` file is created in the working directory.\nDefault: `publishednodes.json`\n",
                    s => this[PublisherConfig.PublishedNodesFileKey] = s },
                { $"cf|createifnotexist:|{PublisherConfig.CreatePublishFileIfNotExistKey}:",
                    "Permit publisher to create the the specified publish file if it does not exist. The new file will be created under the access rights of the publisher module.\nThe default file 'publishednodes.json' is always created when no file name was provided on the command line and this option is ignored.\nIf a file was specified but does not exist and should not be created the module exits.\nDefault: `false`\n",
                    (bool? b) => this[PublisherConfig.CreatePublishFileIfNotExistKey] = b?.ToString() ?? "True" },
                { $"fe|forceencryptedcredentials:|{PublisherConfig.ForceCredentialEncryptionKey}:",
                    "If set to true the publisher will never write plain text credentials into the published nodes configuration file.\nIf a credential cannot be written to the file using the IoT Edge workload API crypto provider the publisher will exit with an error.\nDefault: `false` (write secrets as plain text into the configuration file which should be properly ACL'ed)\n",
                    (bool? b) => this[PublisherConfig.ForceCredentialEncryptionKey] = b?.ToString() ?? "True" },
                { $"id|publisherid=|{PublisherConfig.PublisherIdKey}=",
                    "Sets the publisher id of the publisher.\nDefault: `not set` which results in the IoT edge identity being used \n",
                    s => this[PublisherConfig.PublisherIdKey] = s},
                { $"s|site=|{PublisherConfig.SiteIdKey}=",
                    "Sets the site name of the publisher module.\nDefault: `not set` \n",
                    s => this[PublisherConfig.SiteIdKey] = s},
                { $"rs|runtimestatereporting:|{PublisherConfig.EnableRuntimeStateReportingKey}:",
                    "Enable that when publisher starts or restarts it reports its runtime state using a restart message.\nDefault: `False` (disabled)\n",
                    (bool? b) => this[PublisherConfig.EnableRuntimeStateReportingKey] = b?.ToString() ?? "True"},
                { $"doa|disableopenapi:|{PublisherConfig.DisableOpenApiEndpointKey}:",
                    "Disable the OPC Publisher Open API endpoint exposed by the built-in HTTP server.\nDefault: `enabled`.\n",
                    (bool? b) => this[PublisherConfig.DisableOpenApiEndpointKey] = b?.ToString() ?? "True" },

                "",
                "Messaging configuration",
                "-----------------------",
                "",

                { $"c|strict:|{PublisherConfig.UseStandardsCompliantEncodingKey}:",
                    "Use strict UA compliant encodings. Default is 'false' for backwards (2.5.x - 2.8.x) compatibility. It is recommended to run the publisher in compliant mode for best interoperability.\nDefault: `False`\n",
                    (bool? b) => this[PublisherConfig.UseStandardsCompliantEncodingKey] = b?.ToString() ?? "True" },
                { $"nf|namespaceformat=|{PublisherConfig.DefaultNamespaceFormatKey}=",
                    $"The format to use when serializing node ids and qualified names containing a namespace uri into a string.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(NamespaceFormat)))}`\nDefault: `{nameof(NamespaceFormat.Expanded)}` if `-c` is specified, otherwise `{nameof(NamespaceFormat.Uri)}` for backwards compatibility.\n",
                    (NamespaceFormat m) => this[PublisherConfig.DefaultNamespaceFormatKey] = m.ToString() },
                { $"mm|messagingmode=|{PublisherConfig.MessagingModeKey}=",
                    $"The messaging mode for messages\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessagingMode)))}`\nDefault: `{nameof(MessagingMode.PubSub)}` if `-c` is specified, otherwise `{nameof(MessagingMode.Samples)}` for backwards compatibility.\n",
                    (MessagingMode m) => this[PublisherConfig.MessagingModeKey] = m.ToString() },

                // TODO: Add ability to specify networkmessage mask
                // TODO: Add ability to specify dataset message mask
                // TODO: Add ability to specify dataset field message mask
                // TODO: Allow override of content type
                // TODO: Allow overriding schema

                { $"me|messageencoding=|{PublisherConfig.MessageEncodingKey}=",
                    $"The message encoding for messages\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessageEncoding)))}`\nDefault: `{nameof(MessageEncoding.Json)}`.\n",
                    (MessageEncoding m) => this[PublisherConfig.MessageEncodingKey] = m.ToString() },
                    { $"fm|fullfeaturedmessage=|{PublisherConfig.FullFeaturedMessage}=",
                        "The full featured mode for messages (all fields filled in) for backwards compatibilty. \nDefault: `False` for legacy compatibility.\n",
                        (string b) => this[PublisherConfig.FullFeaturedMessage] = b, true },
                { $"bi|batchtriggerinterval=|{PublisherConfig.BatchTriggerIntervalKey}=",
                    "The network message publishing interval in milliseconds. Determines the publishing period at which point messages are emitted.\nWhen `--bs` is 1 and `--bi` is set to 0 batching is disabled.\nDefault: `10000` (10 seconds).\nAlso can be set using `BatchTriggerInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int k) => this[PublisherConfig.BatchTriggerIntervalKey] = TimeSpan.FromMilliseconds(k).ToString() },
                    { "si|iothubsendinterval=",
                        "The network message publishing interval in seconds for backwards compatibilty. \nDefault: `10` seconds.\n",
                        (string k) => this[PublisherConfig.BatchTriggerIntervalKey] = TimeSpan.FromSeconds(int.Parse(k, CultureInfo.CurrentCulture)).ToString(), true },
                { $"bs|batchsize=|{PublisherConfig.BatchSizeKey}=",
                    "The number of incoming OPC UA subscription notifications to collect until sending a network messages. When `--bs` is set to 1 and `--bi` is 0 batching is disabled and messages are sent as soon as notifications arrive.\nDefault: `50`.\n",
                    (int i) => this[PublisherConfig.BatchSizeKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"ms|maxmessagesize=|iothubmessagesize=|{PublisherConfig.IoTHubMaxMessageSizeKey}=",
                    "The maximum size of the messages to emit. In case the encoder cannot encode a message because the size would be exceeded, the message is dropped. Otherwise the encoder will aim to chunk messages if possible. \nDefault: `256k` in case of IoT Hub messages, `0` otherwise.\n",
                    (int i) => this[PublisherConfig.IoTHubMaxMessageSizeKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"qos|{PublisherConfig.DefaultQualityOfServiceKey}=",
                    $"The default quality of service to use for data set messages.\nThis does not apply to metadata messages which are always sent with `AtLeastOnce` semantics.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(QoS)))}`\nDefault: `{nameof(QoS.AtLeastOnce)}`.\n",
                    (QoS q) => this[PublisherConfig.DefaultQualityOfServiceKey] = q.ToString() },

                // TODO: Add ConfiguredMessageSize

                { $"mts|messagetimestamp=|{PublisherConfig.MessageTimestampKey}=",
                    $"The value to set as as the timestamp property of messages during encoding (if the encoding supports writing message timestamps).\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(MessageTimestamp)))}`\nDefault: `{nameof(MessageTimestamp.CurrentTimeUtc)}` to use the time when the message was created in OPC Publisher.\n",
                    (MessageTimestamp m) => this[PublisherConfig.MessageTimestampKey] = m.ToString() },
                { $"npd|maxnodesperdataset=|{PublisherConfig.MaxNodesPerDataSetKey}=",
                    "Maximum number of nodes within a Subscription. When there are more nodes configured for a data set writer, they will be added to new subscriptions. This also affects metadata message size. \nDefault: `1000`.\n",
                    (int i) => this[PublisherConfig.MaxNodesPerDataSetKey] = i.ToString(CultureInfo.CurrentCulture) },

                { $"kfc|keyframecount=|{OpcUaSubscriptionConfig.DefaultKeyFrameCountKey}=",
                    "The default number of delta messages to send until a key frame message is sent. If 0, no key frame messages are sent, if 1, every message will be a key frame. \nDefault: `0`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.DefaultKeyFrameCountKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"ka|sendkeepalives:|{OpcUaSubscriptionConfig.EnableDataSetKeepAlivesKey}:",
                    "Enables sending keep alive messages triggered by writer subscription's keep alive notifications. This setting can be used to enable the messaging profile's support for keep alive messages.\nIf the chosen messaging profile does not support keep alive messages this setting is ignored.\nDefault: `False` (to save bandwidth).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.EnableDataSetKeepAlivesKey] = b?.ToString() ?? "True" },
                { $"eip|immediatepublishing:|{OpcUaSubscriptionConfig.EnableImmediatePublishingKey}:",
                    "By default OPC Publisher will create a subscription with publishing disabled and only enable it after it has filled it with all configured monitored items. Use this setting to create the subscription with publishing already enabled.\nDefault: `False`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DisableDataSetMetaDataKey] = b?.ToString() ?? "True" },
                { $"msi|metadatasendinterval=|{OpcUaSubscriptionConfig.DefaultMetaDataUpdateTimeKey}=",
                    "Default value in milliseconds for the metadata send interval which determines in which interval metadata is sent.\nEven when disabled, metadata is still sent when the metadata version changes unless `--mm=*Samples` is set in which case this setting is ignored. Only valid for network message encodings. \nDefault: `0` which means periodic sending of metadata is disabled.\n",
                    (int i) => this[OpcUaSubscriptionConfig.DefaultMetaDataUpdateTimeKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"dm|disablemetadata:|{OpcUaSubscriptionConfig.DisableDataSetMetaDataKey}:",
                    "Disables sending any metadata when metadata version changes. This setting can be used to also override the messaging profile's default support for metadata sending. \nDefault: `False` if the messaging profile selected supports sending metadata, `True` otherwise.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DisableDataSetMetaDataKey] = b?.ToString() ?? "True" },
                    { $"lc|legacycompatibility=|{LegacyCompatibility}=",
                        "Run the publisher in legacy (2.5.x) compatibility mode.\nDefault: `False` (disabled).\n",
                        b => this[LegacyCompatibility] = b, true },
                { $"amt|asyncmetadatathreshold=|{OpcUaSubscriptionConfig.AsyncMetaDataLoadThresholdKey}=",
                    $"The default threshold of monitored items in a subscription under which meta data is loaded synchronously during subscription creation.\nLoaded metadata guarantees a metadata message is sent before the first message is sent but loading of metadata takes time during subscription setup. Set to `0` to always load metadata asynchronously.\nOnly used if meta data is supported and enabled.\nDefault: `{OpcUaSubscriptionConfig.AsyncMetaDataLoadThresholdDefault}`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.AsyncMetaDataLoadThresholdKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"dsg|disablesessionpergroup:|{OpcUaSubscriptionConfig.DisableSessionPerWriterGroupKey}:",
                    $"Disable creating a separate session per writer group. Instead sessions are re-used across writer groups.\nDefault: `{OpcUaSubscriptionConfig.DisableSessionPerWriterGroupDefault}`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DisableSessionPerWriterGroupKey] = b?.ToString() ?? "True" },

                { $"om|maxsendqueuesize=|{PublisherConfig.MaxNetworkMessageSendQueueSizeKey}=",
                    $"The maximum number of messages to buffer on the send path before messages are dropped.\nDefault: `{PublisherConfig.MaxNetworkMessageSendQueueSizeDefault}`\n",
                    (int i) => this[PublisherConfig.MaxNetworkMessageSendQueueSizeKey] = i.ToString(CultureInfo.InvariantCulture) },
                    { "maxoutgressmessages|MaxOutgressMessages=", "Deprecated - do not use",
                        (string i) => this[PublisherConfig.MaxNetworkMessageSendQueueSizeKey] = i, true },
                { $"t|dmt=|defaultmessagetransport=|{PublisherConfig.DefaultTransportKey}=",
                    $"The desired transport to use to publish network messages with.\nRequires the transport to be properly configured (see transport settings).\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(WriterGroupTransport)))}`\nDefault: `{nameof(WriterGroupTransport.IoTHub)}` or the first configured transport of the allowed value list.\n",
                    (WriterGroupTransport p) => this[PublisherConfig.DefaultTransportKey] = p.ToString() },

                "",
                "Transport settings",
                "------------------",
                "",
                { $"b|mqc=|mqttclientconnectionstring=|{Configuration.MqttBroker.MqttClientConnectionStringKey}=",
                    $"An mqtt connection string to use. Use this option to connect OPC Publisher to a MQTT Broker endpoint.\nTo connect to an MQTT broker use the format 'HostName=<IPorDnsName>;Port=<Port>[;Username=<Username>;Password=<Password>;Protocol=<'v5'|'v311'>]'. To publish via MQTT by default specify `-t={nameof(WriterGroupTransport.Mqtt)}`.\nDefault: `not set`.\n",
                    mqc => this[Configuration.MqttBroker.MqttClientConnectionStringKey] = mqc },
                { $"e|ec=|edgehubconnectionstring=|dc=|deviceconnectionstring=|{Configuration.IoTEdge.EdgeHubConnectionString}=",
                    $"A edge hub or iot hub connection string to use if you run OPC Publisher outside of IoT Edge. The connection string can be obtained from the IoT Hub portal. It is not required to use this option if running inside IoT Edge. To publish through IoT Edge by default specify `-t={nameof(WriterGroupTransport.IoTHub)}`.\nDefault: `not set`.\n",
                    dc => this[Configuration.IoTEdge.EdgeHubConnectionString] = dc },
                    { $"ht|ih=|iothubprotocol=|{Configuration.IoTEdge.HubTransport}=",
                        $"Protocol to use for communication with EdgeHub.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(TransportOption)))}`\nDefault: `{nameof(TransportOption.Mqtt)}` if device or edge hub connection string is provided, ignored otherwise.\n",
                        (TransportOption p) => this[Configuration.IoTEdge.HubTransport] = p.ToString() },
                { $"d|dcs=|daprconnectionstring=|{Configuration.Dapr.DaprConnectionStringKey}=",
                    $"Connect the OPC Publisher to a dapr pub sub component using a connection string.\nThe connection string specifies the PubSub component to use and allows you to configure the side car connection if needed.\nUse the format 'PubSubComponent=<PubSubComponent>[;GrpcPort=<GrpcPort>;HttpPort=<HttpPort>][;ApiKey=<ApiKey>]'. To publish through dapr by default specify `-t={nameof(WriterGroupTransport.Dapr)}`.\nDefault: `not set`.\n",
                    dcs => this[Configuration.Dapr.DaprConnectionStringKey] = dcs },
                { $"w|hcs=|httpconnectionstring=|{Configuration.Http.HttpConnectionStringKey}=",
                    $"Allows OPC Publisher to publish multipart messages to a topic path using the http protocol (web hook). Specify the target host and configure the optional connection settings using a connection string of the format 'HostName=<IPorDnsName>[;Port=<Port>][;Scheme=<'https'|'http'>][;Put=true][;ApiKey=<ApiKey>]'. To publish via HTTP by default specify `-t={nameof(WriterGroupTransport.Http)}`.\nDefault: `not set`.\n",
                    hcs => this[Configuration.Http.HttpConnectionStringKey] = hcs },
                { $"o|outdir=|{Configuration.FileSystem.OutputRootKey}=",
                    $"A folder to write messages into.\nUse this option to have OPC Publisher write messages to a folder structure under this folder. The structure reflects the topic tree. To publish into the file system folder by default specify `-t={nameof(WriterGroupTransport.FileSystem)}`.\nDefault: `not set`.\n",
                    or => this[Configuration.FileSystem.OutputRootKey] = or },
                { $"p|httpserverport=|{Configuration.Kestrel.HttpServerPortKey}=",
                    $"The port on which the http server of OPC Publisher is listening.\nDefault: `{Configuration.Kestrel.HttpsPortDefault}` if no value is provided.\n",
                    p => this[Configuration.Kestrel.HttpServerPortKey] = p },
                { $"unsecurehttp:|{Configuration.Kestrel.UnsecureHttpServerPortKey}:",
                    $"Allow unsecure access to the REST api of OPC Publisher. A port can be specified if the default port {Configuration.Kestrel.HttpPortDefault} is not desired.\nDo not enable this in production as it exposes the Api Key on the network.\nDefault: `disabled`, if specified without a port `{Configuration.Kestrel.HttpPortDefault}` port is used.\n",
                    (int? p) => this[Configuration.Kestrel.UnsecureHttpServerPortKey] = p?.ToString(CultureInfo.CurrentCulture) ?? Configuration.Kestrel.HttpPortDefault.ToString(CultureInfo.CurrentCulture) },
                { $"useopenapiv3:|{Configuration.OpenApi.UseOpenApiV3Key}:",
                    "If enabled exposes the open api schema of OPC Publisher using v3 schema (yaml).\nOnly valid if Open API endpoint is not disabled.\nDefault: `v2` (json).\n",
                    (bool? b) => this[Configuration.OpenApi.UseOpenApiV3Key] = b?.ToString() ?? "True" },

                "",
                "Routing configuration",
                "---------------------",
                "",

                { $"rtt|roottopictemplate:|{PublisherConfig.RootTopicTemplateKey}:",
                    "The default root topic of OPC Publisher.\nIf not specified, the `{{PublisherId}}` template is the root topic.\nCurrently only the template variables\n    `{{SiteId}}` and\n    `{{PublisherId}}`\ncan be used as dynamic substituations in the template. If the template variable does not exist it is replaced with the `$default` string.\nDefault: `{{PublisherId}}`.\n",
                    t => this[PublisherConfig.RootTopicTemplateKey] = t },
                { $"mtt|methodtopictemplate=|{PublisherConfig.MethodTopicTemplateKey}=",
                    "The topic at which OPC Publisher's method handler is mounted.\nIf not specified, the `{{RootTopic}}/methods` template will be used as root topic with the method names as sub topic.\nOnly\n    `{{RootTopic}}`\n    `{{SiteId}}` and\n    `{{PublisherId}}`\ncan currently be used as replacement variables in the template.\nDefault: `{{RootTopic}}/methods`.\n",
                    t => this[PublisherConfig.MethodTopicTemplateKey] = t },
                { $"ttt|telemetrytopictemplate:|{PublisherConfig.TelemetryTopicTemplateKey}:",
                    "The default topic that all messages are sent to.\nIf not specified, the `{{RootTopic}}/messages/{{DataSetWriterGroup}}` template will be used as root topic for all events sent by OPC Publisher.\nThe template variables\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{PublisherId}}`\n    `{{DataSetClassId}}`\n    `{{DataSetWriterName}}` and\n    `{{DataSetWriterGroup}}`\n can be used as dynamic parts in the template. If a template variable does not exist the name of the variable is emitted.\nDefault: `{{RootTopic}}/messages/{{DataSetWriterGroup}}`.\n",
                    t => this[PublisherConfig.TelemetryTopicTemplateKey] = t },
                { $"ett|eventstopictemplate=|{PublisherConfig.EventsTopicTemplateKey}=",
                    "The topic into which OPC Publisher publishes any events that are not telemetry messages such as discovery or runtime events.\nIf not specified, the `{{RootTopic}}/events` template will be used.\nOnly\n    `{{RootTopic}}`\n    `{{SiteId}}` and\n    `{{PublisherId}}`\ncan currently be used as replacement variables in the template.\nDefault: `{{RootTopic}}/events`.\n",
                    t => this[PublisherConfig.EventsTopicTemplateKey] = t },
                { $"mdt|metadatatopictemplate:|{PublisherConfig.DataSetMetaDataTopicTemplateKey}:",
                    "The topic that metadata should be sent to.\nIn case of MQTT the message will be sent as RETAIN message with a TTL of either metadata send interval or infinite if metadata send interval is not configured.\nOnly valid if metadata is supported and/or explicitely enabled.\nThe template variables\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{TelemetryTopic}}`\n    `{{PublisherId}}`\n    `{{DataSetClassId}}`\n    `{{DataSetWriterName}}` and\n    `{{DatasetWriterGroup}}`\ncan be used as dynamic parts in the template. \nDefault: `{{TelemetryTopic}}` which means metadata is sent to the same output as regular messages. If specified without value, the default output is `{{TelemetryTopic}}/$metadata`.\n",
                    s => this[PublisherConfig.DataSetMetaDataTopicTemplateKey] = !string.IsNullOrEmpty(s) ? s : "{TelemetryTopic}/$metadata" },
                { $"ri|enableroutinginfo:|{PublisherConfig.EnableDataSetRoutingInfoKey}:",
                    $"Add routing information to messages. The name of the property is `{Constants.MessagePropertyRoutingKey}` and the value is the `DataSetWriterGroup` for that particular message.\nWhen the `DataSetWriterGroup` is not configured, the `{Constants.MessagePropertyRoutingKey}` property will not be added to the message even if this argument is set.\nDefault: `{PublisherConfig.EnableDataSetRoutingInfoDefault}`.\n",
                    (bool? b) => this[PublisherConfig.EnableDataSetRoutingInfoKey] = b?.ToString() ?? "True" },

                "",
                "Subscription settings",
                "---------------------",
                "",

                { $"oi|opcsamplinginterval=|{OpcUaSubscriptionConfig.DefaultSamplingIntervalKey}=",
                    "Default value in milliseconds to request the servers to sample values. This value is used if an explicit sampling interval for a node was not configured. \nDefault: `1000`.\nAlso can be set using `DefaultSamplingInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.DefaultSamplingIntervalKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"op|opcpublishinginterval=|{OpcUaSubscriptionConfig.DefaultPublishingIntervalKey}=",
                    "Default value in milliseconds for the publishing interval setting of a subscription created with an OPC UA server. This value is used if an explicit publishing interval was not configured.\nDefault: `1000`.\nAlso can be set using `DefaultPublishingInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.DefaultPublishingIntervalKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"kt|keepalivethreshold=|{OpcUaSubscriptionConfig.MaxKeepAliveCountKey}=",
                    "Specify the default number of keep alive packets a server can miss, before the session is disconneced.\nDefault: `50`.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.MaxKeepAliveCountKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"fd|fetchdisplayname:|{OpcUaSubscriptionConfig.FetchOpcNodeDisplayNameKey}:",
                    "Fetches the displayname for the monitored items subscribed if a display name was not specified in the configuration.\nNote: This has high impact on OPC Publisher startup performance.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.FetchOpcNodeDisplayNameKey] = b?.ToString() ?? "True" },
                { $"qs|queuesize=|{OpcUaSubscriptionConfig.DefaultQueueSize}=",
                    "Default queue size for all monitored items if queue size was not specified in the configuration.\nDefault: `1` (for backwards compatibility).\n",
                    (uint u) => this[OpcUaSubscriptionConfig.DefaultQueueSize] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ndo|nodiscardold:|{OpcUaSubscriptionConfig.DefaultDiscardNewKey}:",
                    "The publisher is using this as default value for the discard old setting of monitored item queue configuration. Setting to true will ensure that new values are dropped before older ones are drained. \nDefault: `False` (which is the OPC UA default).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultDiscardNewKey] = b?.ToString() ?? "True" },
                { $"mc|monitoreditemdatachangetrigger=|{OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey}=",
                    $"Default data change trigger for all monitored items configured in the published nodes configuration unless explicitly overridden.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(DataChangeTriggerType)))}`\nDefault: `{nameof(DataChangeTriggerType.StatusValue)}` (which is the OPC UA default).\n",
                    (DataChangeTriggerType t) => this[OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey] = t.ToString() },
                { $"sf|skipfirst:|{OpcUaSubscriptionConfig.DefaultSkipFirstKey}:",
                    "The publisher is using this as default value for the skip first setting of nodes configured without a skip first setting. A value of True will skip sending the first notification received when the monitored item is added to the subscription.\nDefault: `False` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultSkipFirstKey] = b?.ToString() ?? "True" },
                    { "skipfirstevent:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[OpcUaSubscriptionConfig.DefaultSkipFirstKey] = b ?? "True", /* hidden = */ true },
                { $"hbb|heartbeatbehavior=|{OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey}=",
                    $"Default behavior of the heartbeat mechanism unless overridden in the published nodes configuration explicitly.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(HeartbeatBehavior)))}`\nDefault: `{nameof(HeartbeatBehavior.WatchdogLKV)}` (Sending LKV in a watchdog fashion).\n",
                    (HeartbeatBehavior b) => this[OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey] = b.ToString() },
                { $"hb|heartbeatinterval=|{OpcUaSubscriptionConfig.DefaultHeartbeatIntervalKey}=",
                    "The publisher is using this as default value in seconds for the heartbeat interval setting of nodes that were configured without a heartbeat interval setting. A heartbeat is sent at this interval if no value has been received.\nDefault: `0` (disabled)\nAlso can be set using `DefaultHeartbeatInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.DefaultHeartbeatIntervalKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"slt|{OpcUaSubscriptionConfig.MinSubscriptionLifetimeKey}=",
                    "Minimum subscription lifetime in seconds as per OPC UA definition.\nDefault: `not set`.\n",
                    (int i) => this[OpcUaSubscriptionConfig.MinSubscriptionLifetimeKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"da|deferredacks:|{OpcUaSubscriptionConfig.UseDeferredAcknoledgementsKey}:",
                    "(Experimental) Acknoledge subscription notifications only when the data has been successfully published.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.UseDeferredAcknoledgementsKey] = b?.ToString() ?? "True" },
                { $"ucr|usecyclicreads:|{OpcUaSubscriptionConfig.DefaultSamplingUsingCyclicReadKey}:",
                    "(Experimental) All nodes should be sampled using periodical client reads instead of subscriptions services, unless otherwise configured.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultSamplingUsingCyclicReadKey] = b?.ToString() ?? "True" },
                { $"urc|usereverseconnect:|{OpcUaSubscriptionConfig.DefaultUseReverseConnectKey}:",
                    "(Experimental) Use reverse connect for all endpoints that are part of the subscription configuration unless otherwise configured.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultUseReverseConnectKey] = b?.ToString() ?? "True" },

                "",
                "OPC UA Client configuration",
                "---------------------------",
                "",

                { $"aa|acceptuntrusted:|{OpcUaClientConfig.AutoAcceptUntrustedCertificatesKey}:",
                    "The publisher accepts untrusted certificates presented by a server it connects to.\nThis does not include servers presenting bad certificates or certificates that fail chain validation. These errors cannot be suppressed and connection will always be rejected.\nWARNING: This setting should never be used in production environments!\n",
                    (bool? b) => this[OpcUaClientConfig.AutoAcceptUntrustedCertificatesKey] = b?.ToString() ?? "True" },
                     { "autoaccept:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[OpcUaClientConfig.AutoAcceptUntrustedCertificatesKey] = b ?? "True", /* hidden = */ true },
                { $"rur|rejectunknownrevocationstatus:|{OpcUaClientConfig.RejectUnknownRevocationStatusKey}:",
                    $"Set this to `False` to accept certificates presented by a server that have an unknown revocation status.\nWARNING: This setting should never be used in production environments!\nDefault: `{OpcUaClientConfig.RejectUnknownRevocationStatusDefault}`.\n",
                    (bool? b) => this[OpcUaClientConfig.RejectUnknownRevocationStatusKey] = b?.ToString() ?? "True" },
                { $"ct|createsessiontimeout=|{OpcUaClientConfig.CreateSessionTimeoutKey}=",
                    $"Amount of time in seconds to wait until a session is connected.\nDefault: `{OpcUaClientConfig.CreateSessionTimeoutDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaClientConfig.CreateSessionTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"mr|reconnectperiod=|{OpcUaClientConfig.MinReconnectDelayKey}=",
                    $"The minimum amount of time in milliseconds to wait reconnection of session is attempted again.\nDefault: `{OpcUaClientConfig.MinReconnectDelayDefault}` milliseconds.\n",
                    (int i) => this[OpcUaClientConfig.MinReconnectDelayKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"xr|maxreconnectperiod=|{OpcUaClientConfig.MaxReconnectDelayKey}=",
                    $"The maximum amount of time in millseconds to wait between reconnection attempts of the session.\nDefault: `{OpcUaClientConfig.MaxReconnectDelayDefault}` milliseconds.\n",
                    (int i) => this[OpcUaClientConfig.MaxReconnectDelayKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"sto|sessiontimeout=|{OpcUaClientConfig.DefaultSessionTimeoutKey}=",
                    $"Maximum amount of time in seconds that a session should remain open by the OPC server without any activity (session timeout). Requested from the OPC server at session creation.\nDefault: `{OpcUaClientConfig.DefaultSessionTimeoutDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaClientConfig.DefaultSessionTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ki|keepaliveinterval=|{OpcUaClientConfig.KeepAliveIntervalKey}=",
                    "The interval in seconds the publisher is sending keep alive messages to the OPC servers on the endpoints it is connected to.\nDefault: `10000` (10 seconds).\n",
                    (int i) => this[OpcUaClientConfig.KeepAliveIntervalKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"ot|operationtimeout=|{OpcUaClientConfig.OperationTimeoutKey}=",
                    $"The operation service call timeout of the publisher OPC UA client in milliseconds. \nDefault: `{OpcUaClientConfig.OperationTimeoutDefault}` milliseconds.\n",
                    (uint u) => this[OpcUaClientConfig.OperationTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"cl|clientlinger=|{OpcUaClientConfig.LingerTimeoutKey}=",
                    "Amount of time in seconds to delay closing a client and underlying session after the a last service call.\nUse this setting to speed up multiple subsequent calls to a server.\nDefault: `0` sec (no linger).\n",
                    (uint u) => this[OpcUaClientConfig.LingerTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"rcp|reverseconnectport=|{OpcUaClientConfig.ReverseConnectPortKey}=",
                    $"The port to use when accepting inbound reverse connect requests from servers.\nDefault: `{OpcUaClientConfig.ReverseConnectPortDefault}`.\n",
                    (ushort u) => this[OpcUaClientConfig.ReverseConnectPortKey] = u.ToString(CultureInfo.CurrentCulture) },

                { $"smi|subscriptionmanagementinterval=|{OpcUaClientConfig.SubscriptionManagementIntervalKey}=",
                    "The interval in seconds after which the publisher re-applies the desired state of the subscription to a session.\nDefault: `never` (only on configuration change).\n",
                    (int i) => this[OpcUaClientConfig.SubscriptionManagementIntervalKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"bnr|badnoderetrydelay=|{OpcUaClientConfig.BadMonitoredItemRetryDelayKey}=",
                    $"The delay in seconds after which nodes that were rejected by the server while added or updating a subscription or while publishing, are re-applied to a subscription.\nDefault: `{OpcUaClientConfig.BadMonitoredItemRetryDelayDefaultSec}` seconds.\n",
                    (int i) => this[OpcUaClientConfig.BadMonitoredItemRetryDelayKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"inr|invalidnoderetrydelay=|{OpcUaClientConfig.InvalidMonitoredItemRetryDelayKey}=",
                    $"The delay in seconds after which the publisher attempts to re-apply nodes that were incorrectly configured to a subscription.\nDefault: `{OpcUaClientConfig.InvalidMonitoredItemRetryDelayDefaultSec}` seconds.\n",
                    (int i) => this[OpcUaClientConfig.InvalidMonitoredItemRetryDelayKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"ser|subscriptionerrorretrydelay=|{OpcUaClientConfig.SubscriptionErrorRetryDelayKey}=",
                    $"The delay in seconds between attempts to create a subscription in a session.\nDefault: `{OpcUaClientConfig.SubscriptionErrorRetryDelayDefaultSec}` seconds.\n",
                    (int i) => this[OpcUaClientConfig.SubscriptionErrorRetryDelayKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"dcp|disablecomplextypepreloading:|{OpcUaClientConfig.DisableComplexTypePreloadingKey}:",
                    "Complex types (structures, enumerations) a server exposes are preloaded from the server after the session is connected. In some cases this can cause problems either on the client or server itself. Use this setting to disable pre-loading support.\nNote that since the complex type system is used for meta data messages it will still be loaded at the time the subscription is created, therefore also disable meta data support if you want to ensure the complex types are never loaded for an endpoint.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaClientConfig.DisableComplexTypePreloadingKey] = b?.ToString() ?? "True" },

                { $"otl|opctokenlifetime=|{OpcUaClientConfig.SecurityTokenLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Security token lifetime in milliseconds.\nDefault: `3600000` (1h).\n",
                    (uint u) => this[OpcUaClientConfig.SecurityTokenLifetimeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ocl|opcchannellifetime=|{OpcUaClientConfig.ChannelLifetimeKey}=",
                    "OPC UA Stack Transport Secure Channel - Channel lifetime in milliseconds.\nDefault: `300000` (5 min).\n",
                    (uint u) => this[OpcUaClientConfig.ChannelLifetimeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"omb|opcmaxbufferlen=|{OpcUaClientConfig.MaxBufferSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max buffer size.\nDefault: `65535` (64KB -1).\n",
                    (uint u) => this[OpcUaClientConfig.MaxBufferSizeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"oml|opcmaxmessagelen=|{OpcUaClientConfig.MaxMessageSizeKey}=",
                    "OPC UA Stack Transport Secure Channel - Max message size.\nDefault: `4194304` (4 MB).\n",
                    (uint u) => this[OpcUaClientConfig.MaxMessageSizeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"oal|opcmaxarraylen=|{OpcUaClientConfig.MaxArrayLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max array length.\nDefault: `65535` (64KB - 1).\n",
                    (uint u) => this[OpcUaClientConfig.MaxArrayLengthKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ol|opcmaxstringlen=|{OpcUaClientConfig.MaxStringLengthKey}=",
                    "The max length of a string opc can transmit/receive over the OPC UA secure channel.\nDefault: `130816` (128KB - 256).\n",
                    (uint u) => this[OpcUaClientConfig.MaxStringLengthKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"obl|opcmaxbytestringlen=|{OpcUaClientConfig.MaxByteStringLengthKey}=",
                    "OPC UA Stack Transport Secure Channel - Max byte string length.\nDefault: `1048576` (1MB).\n",
                    (uint u) => this[OpcUaClientConfig.MaxByteStringLengthKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"au|appuri=|{OpcUaClientConfig.ApplicationUriKey}=",
                    "Application URI as per OPC UA definition inside the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[OpcUaClientConfig.ApplicationUriKey] = s },
                { $"pu|producturi=|{OpcUaClientConfig.ProductUriKey}=",
                    "The Product URI as per OPC UA definition insde the OPC UA client application configuration presented to the server.\nDefault: `not set`.\n",
                    s => this[OpcUaClientConfig.ProductUriKey] = s },

                { $"rejectsha1=|{OpcUaClientConfig.RejectSha1SignedCertificatesKey}=",
                    "If set to `False` OPC Publisher will accept SHA1 certificates which have been officially deprecated and are unsafe to use.\nNote: Set this to `False` to support older equipment that uses Sha1 signed certificates rather than using no security.\nDefault: `True`.\n",
                    (bool b) => this[OpcUaClientConfig.RejectSha1SignedCertificatesKey] = b.ToString() },
                { $"mks|minkeysize=|{OpcUaClientConfig.MinimumCertificateKeySizeKey}=",
                    "Minimum accepted certificate size.\nNote: It is recommended to this value to the highest certificate key size possible based on the connected OPC UA servers.\nDefault: 1024.\n",
                    s => this[OpcUaClientConfig.MinimumCertificateKeySizeKey] = s },
                { $"tm|trustmyself=|{OpcUaClientConfig.AddAppCertToTrustedStoreKey}=",
                    "Set to `False` to disable adding the publisher's own certificate to the trusted store automatically.\nDefault: `True`.\n",
                    (bool b) => this[OpcUaClientConfig.AddAppCertToTrustedStoreKey] = b.ToString() },
                { $"sn|appcertsubjectname=|{OpcUaClientConfig.ApplicationCertificateSubjectNameKey}=",
                    "The subject name for the app cert.\nDefault: `CN=<the value of --an|--appname>, C=DE, S=Bav, O=Microsoft, DC=localhost`.\n",
                    s => this[OpcUaClientConfig.ApplicationCertificateSubjectNameKey] = s },
                { $"an|appname=|{OpcUaClientConfig.ApplicationNameKey}=",
                    $"The name for the app (used during OPC UA authentication).\nDefault: `{OpcUaClientConfig.ApplicationNameDefault}`\n",
                    s => this[OpcUaClientConfig.ApplicationNameKey] = s },
                { $"pki|pkirootpath=|{OpcUaClientConfig.PkiRootPathKey}=",
                    "PKI certificate store root path.\nDefault: `pki`.\n",
                    s => this[OpcUaClientConfig.PkiRootPathKey] = s },
                { $"ap|appcertstorepath=|{OpcUaClientConfig.ApplicationCertificateStorePathKey}=",
                    "The path where the own application cert should be stored.\nDefault: $\"{{PkiRootPath}}/own\".\n",
                    s => this[OpcUaClientConfig.ApplicationCertificateStorePathKey] = s },
                { $"apt|at=|appcertstoretype=|{OpcUaClientConfig.ApplicationCertificateStoreTypeKey}=",
                    $"The own application cert store type.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.ApplicationCertificateStoreTypeKey, "apt") },
                { $"apw|appcertstorepwd=|{OpcUaClientConfig.ApplicationCertificatePasswordKey}=",
                    "Password to use when storing the application certificate in the store folder if the store is of type `Directory`.\nDefault: empty, which means application certificate is not protected by default.\n",
                    s => this[OpcUaClientConfig.ApplicationCertificatePasswordKey] = s },
                { $"tp|trustedcertstorepath=|{OpcUaClientConfig.TrustedPeerCertificatesPathKey}=",
                    "The path of the trusted cert store.\nDefault: $\"{{PkiRootPath}}/trusted\".\n",
                    s => this[OpcUaClientConfig.TrustedPeerCertificatesPathKey] = s },
                { $"tpt|{OpcUaClientConfig.TrustedPeerCertificatesTypeKey}=",
                    $"Trusted peer certificate store type.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.TrustedPeerCertificatesTypeKey, "tpt") },
                { $"rp|rejectedcertstorepath=|{OpcUaClientConfig.RejectedCertificateStorePathKey}=",
                    "The path of the rejected cert store.\nDefault: $\"{{PkiRootPath}}/rejected\".\n",
                    s => this[OpcUaClientConfig.RejectedCertificateStorePathKey] = s },
                { $"rpt|{OpcUaClientConfig.RejectedCertificateStoreTypeKey}=",
                    $"Rejected certificate store type.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.RejectedCertificateStoreTypeKey, "rpt") },
                { $"ip|issuercertstorepath=|{OpcUaClientConfig.TrustedIssuerCertificatesPathKey}=",
                    "The path of the trusted issuer cert store.\nDefault: $\"{{PkiRootPath}}/issuer\".\n",
                    s => this[OpcUaClientConfig.TrustedIssuerCertificatesPathKey] = s },
                { $"ipt|{OpcUaClientConfig.TrustedIssuerCertificatesTypeKey}=",
                    $"Trusted issuer certificate store type.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.TrustedIssuerCertificatesTypeKey, "ipt") },
                { $"up|usercertstorepath=|{OpcUaClientConfig.TrustedUserCertificatesPathKey}=",
                    "The path of the certificate store for user certificates.\nDefault: $\"{{PkiRootPath}}/users\".\n",
                    s => this[OpcUaClientConfig.TrustedUserCertificatesPathKey] = s },
                { $"upt|{OpcUaClientConfig.TrustedUserCertificatesTypeKey}=",
                    $"Type of certificate store for all User certificates.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.TrustedUserCertificatesTypeKey, "upt") },
                { $"uip|userissuercertstorepath=|{OpcUaClientConfig.UserIssuerCertificatesPathKey}=",
                    "The path of the user issuer cert store.\nDefault: $\"{{PkiRootPath}}/users/issuer\".\n",
                    s => this[OpcUaClientConfig.UserIssuerCertificatesPathKey] = s },
                { $"uit|{OpcUaClientConfig.UserIssuerCertificatesTypeKey}=",
                    $"Type of the issuer certificate store for User certificates.\nAllowed values:\n    `{CertificateStoreType.Directory}`\n    `{CertificateStoreType.X509Store}`\nDefault: `{CertificateStoreType.Directory}`.\n",
                    s => SetStoreType(s, OpcUaClientConfig.UserIssuerCertificatesTypeKey, "uip") },

                "",
                "Diagnostic options",
                "------------------",
                "",

                { $"di|diagnosticsinterval=|{PublisherConfig.DiagnosticsIntervalKey}=",
                    "Shows publisher diagnostic information at this specified interval in seconds in the OPC Publisher log (need log level info). `-1` disables remote diagnostic log and diagnostic output.\nDefault:60000 (60 seconds).\nAlso can be set using `DiagnosticsInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`\".\n",
                    (int i) => this[PublisherConfig.DiagnosticsIntervalKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"ll|loglevel=|{Configuration.Logging.LogLevelKey}=",
                    $"The loglevel to use.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames(typeof(LogLevel)))}`\nDefault: `{LogLevel.Information}`.\n",
                    (LogLevel l) => this[Configuration.Logging.LogLevelKey] = l.ToString() },
                { $"sl|opcstacklogging:|{OpcUaClientConfig.EnableOpcUaStackLoggingKey}:",
                    "Enable opc ua stack logging beyond logging at error level.\nDefault: `disabled`.\n",
                    (bool? b) => this[OpcUaClientConfig.EnableOpcUaStackLoggingKey] = b?.ToString() ?? "True" },
                { "ln|lognotifications:",
                    "Log ingress subscription notifications at Informational level to aid debugging.\nDefault: `disabled`.\n",
                    (bool? b) => this[PublisherConfig.DebugLogNotificationsKey] = b?.ToString() ?? "True" },
                { $"oc|otlpcollector=|{Configuration.Otlp.OtlpCollectorEndpointKey}=",
                    "Specifiy the OpenTelemetry collector grpc endpoint url to export diagnostics to.\nDefault: `disabled`.\n",
                    s => this[Configuration.Otlp.OtlpCollectorEndpointKey] = s },
                { $"oxi|otlpexportinterval=|{Configuration.Otlp.OtlpExportIntervalMillisecondsKey}=",
                    $"The interval in milliseconds when OpenTelemetry is exported to the collector endpoint.\nDefault: `{Configuration.Otlp.OtlpExportIntervalMillisecondsDefault}` ({(int)(Configuration.Otlp.OtlpExportIntervalMillisecondsDefault / 1000)} seconds).\n",
                    (int i) => this[Configuration.Otlp.OtlpExportIntervalMillisecondsKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"em|enableprometheusendpoint=|{Configuration.Otlp.EnableMetricsKey}=",
                    "Explicitly enable or disable exporting prometheus metrics directly on the standard path.\nDefault: `disabled` if Otlp collector is configured, otherwise `enabled`.\n",
                    (bool? b) => this[Configuration.Otlp.EnableMetricsKey] = b?.ToString() ?? "True" },

                // testing purposes

                { "sc|scaletestcount=",
                    "The number of monitored item clones in scale tests.\n",
                    (string i) => this[PublisherConfig.ScaleTestCountKey] = i, true },

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
                Warning("Parse args exception {0}.", e.Message);
                ExitProcess(160);
                return;
            }

            if (unsupportedOptions.Count > 0)
            {
                foreach (var option in unsupportedOptions)
                {
                    Warning("Option {0} wrong or not supported, " +
                        "please use -h option to get all the supported options.", option);
                }
            }

            if (legacyOptions.Count > 0)
            {
                foreach (var option in legacyOptions)
                {
                    Warning("Legacy option {0} not supported, please use -h option to get all the supported options.", option);
                }
            }

            if (!showHelp)
            {
                // Test the publisher configuration for having all necessary content
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true)
                    .AddEnvironmentVariables()
                    .AddFromDotEnvFile()
                    .AddInMemoryCollection(this).Build();
                try
                {
                    // Throws if the messaging profile configuration is invalid
                    new PublisherConfig(configuration).ToOptions();
                }
                catch
                {
                    Warning("The specified combination of --mm, and --me is not (yet) supported. " +
                        "Currently supported combinations are: {0}), " +
                            "please use -h option to get all the supported options.",
                        MessagingProfile.Supported
                            .Select(p => $"\n(--mm {p.MessagingMode} and --me {p.MessageEncoding})")
                            .Aggregate((a, b) => $"{a}, {b}"));
                    ExitProcess(170);
                    return;
                }

                // Validate edge configuration
                var iotEdgeOptions = new IoTEdgeClientOptions();
                new Configuration.IoTEdge(configuration).Configure(iotEdgeOptions);

                // Check that the important values are provided
                if (iotEdgeOptions.EdgeHubConnectionString == null)
                {
                    Warning(
                        "To connect to Azure IoT Hub you must run as module inside IoT Edge or " +
                        "specify a device connection string using EdgeHubConnectionString " +
                        "environment variable or command line.");
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

        /// <summary>
        /// Call exit with exit code
        /// </summary>
        /// <param name="exitCode"></param>
        public virtual void ExitProcess(int exitCode)
        {
            Publisher.Runtime.Exit(exitCode);
        }

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        public virtual void Warning(string messageTemplate)
        {
            Console.WriteLine(messageTemplate);
        }

        /// <summary>
        /// Write a log event with the Warning level.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValue0">Object positionally formatted into the message template.</param>
        public virtual void Warning<T0>(string messageTemplate,
            T0 propertyValue0)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture,
                messageTemplate, propertyValue0));
        }

        /// <summary>
        /// Write a log event with the Debug level.
        /// </summary>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValue0">Object positionally formatted into the message template.</param>
        /// <param name="propertyValue1">Object positionally formatted into the message template.</param>
        public virtual void Debug<T0, T1>(string messageTemplate,
            T0 propertyValue0, T1 propertyValue1)
        {
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture,
                messageTemplate, propertyValue0, propertyValue1));
        }
    }
}
