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
    public sealed class CommandLine : Dictionary<string, string?>
    {
        /// <summary>
        /// Creates a new instance of the cli options based on existing configuration values.
        /// </summary>
        public CommandLine()
        {
            _logger = new CommandLineLogger();
        }

        /// <summary>
        /// Parse arguments and set values in the environment the way the new configuration expects it.
        /// </summary>
        /// <param name="args">The specified command line arguments.</param>
        /// <param name="logger"></param>
        public CommandLine(string[] args, CommandLineLogger? logger = null)
        {
            _logger = logger ?? new CommandLineLogger();
            var showHelp = false;
            var unsupportedOptions = new List<string>();
            var legacyOptions = new List<string>();

            // Key for the Legacy (before 2.9) compatibility mode. Controlled the content type value
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
                { $"f|pf=|publishfile=|{PublisherConfig.PublishedNodesFileKey}=",
                    "The name of the file containing the configuration of the nodes to be published as well as the information to connect to the OPC UA server sources.\nThis file is also used to persist changes made through the control plane, e.g., through IoT Hub device method calls.\nWhen no file is specified a default `publishednodes.json` file is created in the working directory.\nDefault: `publishednodes.json`\n",
                    s => this[PublisherConfig.PublishedNodesFileKey] = s },
                { $"cf|createifnotexist:|{PublisherConfig.CreatePublishFileIfNotExistKey}:",
                    "Permit publisher to create the the specified publish file if it does not exist. The new file will be created under the access rights of the publisher module.\nThe default file 'publishednodes.json' is always created when no file name was provided on the command line and this option is ignored.\nIf a file was specified but does not exist and should not be created the module exits.\nDefault: `false`\n",
                    (bool? b) => this[PublisherConfig.CreatePublishFileIfNotExistKey] = b?.ToString() ?? "True" },
                { $"pol|usepolling:|{PublisherConfig.UseFileChangePollingKey}:",
                    "Poll for file changes instead of using a file system watcher.\nUse this setting when the underlying file system does not support file system notifications such as in some docker container setups.\nDefault: `false`\n",
                    (bool? b) => this[PublisherConfig.UseFileChangePollingKey] = b?.ToString() ?? "True" },
                { $"fe|forceencryptedcredentials:|{PublisherConfig.ForceCredentialEncryptionKey}:",
                    "If set to true the publisher will never write plain text credentials into the published nodes configuration file.\nIf a credential cannot be written to the file using the IoT Edge workload API crypto provider the publisher will exit with an error.\nDefault: `false` (write secrets as plain text into the configuration file which should be properly ACL'ed)\n",
                    (bool? b) => this[PublisherConfig.ForceCredentialEncryptionKey] = b?.ToString() ?? "True" },
                { $"id|publisherid=|{PublisherConfig.PublisherIdKey}=",
                    "Sets the publisher id of the publisher.\nDefault: `not set` which results in the IoT edge identity being used \n",
                    s => this[PublisherConfig.PublisherIdKey] = s},
                { $"s|site=|{PublisherConfig.SiteIdKey}=",
                    "Sets the site name of the publisher module.\nDefault: `not set` \n",
                    s => this[PublisherConfig.SiteIdKey] = s},
                { $"pi|initfile:|{Configuration.FileSystem.InitFilePathKey}:",
                    "A file from which to read initialization instructions.\nUse this option to have OPC Publisher run a set of method calls found in this file.\nThe file must be formatted using a subset of the .http/.rest file format without support for indentation, scripting or environment variables.\nDefault: `not set` (disabled). If only a file name is specified, it is loaded from the path specifed using `--pn`. If just the argument is provided without a value the default is `publishednodes.init`.\n",
                    pi => this[Configuration.FileSystem.InitFilePathKey] = pi ?? " " },
                { $"il|initlog=|{Configuration.FileSystem.InitLogFileKey}=",
                    "A file into which the results of the initialization instructions are written.\nOnly valid if `--pi` option is specified.\nDefault: If a init file is set using `--pi`, it is appended with the `.log` extension. If just a file name is used, the file is created in the same folder as the init file configured using the `--pi` command line option.\n",
                    il => this[Configuration.FileSystem.InitLogFileKey] = il },
                { $"rs|runtimestatereporting:|{PublisherConfig.EnableRuntimeStateReportingKey}:",
                    "Enable that when publisher starts or restarts it reports its runtime state using a restart message.\nDefault: `false` (disabled)\n",
                    (bool? b) => this[PublisherConfig.EnableRuntimeStateReportingKey] = b?.ToString() ?? "True"},
                { $"api-key=|{PublisherConfig.ApiKeyOverrideKey}=",
                    "Sets the api key that must be used to authenticate calls on the publisher REST endpoint.\nDefault: `not set` (Key will be generated if not available) \n",
                    s => this[PublisherConfig.ApiKeyOverrideKey] = s},
                { $"doa|disableopenapi:|{PublisherConfig.DisableOpenApiEndpointKey}:",
                    "Disable the OPC Publisher Open API endpoint exposed by the built-in HTTP server.\nDefault: `false` (enabled).\n",
                    (bool? b) => this[PublisherConfig.DisableOpenApiEndpointKey] = b?.ToString() ?? "True" },

                "",
                "Messaging configuration",
                "-----------------------",
                "",

                { $"c|strict:|{PublisherConfig.UseStandardsCompliantEncodingKey}:",
                    "Use strict OPC UA standard compliance. It is recommended to run the publisher in compliant mode for best interoperability.\nBe aware that explicitly specifying other command line options can result in non-comnpliance despite this option being set.\nDefault: `false` for backwards compatibility (2.5.x - 2.8.x)\n",
                    (bool? b) => this[PublisherConfig.UseStandardsCompliantEncodingKey] = b?.ToString() ?? "True" },
                { $"nf|namespaceformat=|{PublisherConfig.DefaultNamespaceFormatKey}=",
                    $"The format to use when serializing node ids and qualified names containing a namespace uri into a string.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<NamespaceFormat>())}`\nDefault: `{nameof(NamespaceFormat.Expanded)}` if `-c` is specified, otherwise `{nameof(NamespaceFormat.Uri)}` for backwards compatibility.\n",
                    (NamespaceFormat m) => this[PublisherConfig.DefaultNamespaceFormatKey] = m.ToString() },
                { $"mm|messagingmode=|{PublisherConfig.MessagingModeKey}=",
                    $"The messaging mode for messages\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<MessagingMode>())}`\nDefault: `{nameof(MessagingMode.PubSub)}` if `-c` is specified, otherwise `{nameof(MessagingMode.Samples)}` for backwards compatibility.\n",
                    (MessagingMode m) => this[PublisherConfig.MessagingModeKey] = m.ToString() },
                { $"ode|optimizeddatasetencoding:|{PublisherConfig.WriteValueWhenDataSetHasSingleEntryKey}:",
                    "When a data set has a single entry the encoder will write only the value of a data set entry and omit the key.\nThis is not compliant with OPC UA Part 14.\nDefault: `false`.\n",
                    (bool? b) => this[PublisherConfig.WriteValueWhenDataSetHasSingleEntryKey] = b?.ToString() ?? "True" },

                // TODO: Add ability to specify networkmessage mask
                // TODO: Add ability to specify dataset message mask
                // TODO: Add ability to specify dataset field message mask
                // TODO: Allow override of content type
                // TODO: Allow overriding schema

                { $"me|messageencoding=|{PublisherConfig.MessageEncodingKey}=",
                    $"The message encoding for messages\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<MessageEncoding>())}`\nDefault: `{nameof(MessageEncoding.Json)}`.\n",
                    (MessageEncoding m) => this[PublisherConfig.MessageEncodingKey] = m.ToString() },
                    { $"fm|fullfeaturedmessage=|{PublisherConfig.FullFeaturedMessageKey}=",
                        "The full featured mode for messages (all fields filled in) for backwards compatibilty. \nDefault: `false` for legacy compatibility.\n",
                        (string b) => this[PublisherConfig.FullFeaturedMessageKey] = b, true },
                { $"bi|batchtriggerinterval=|{PublisherConfig.BatchTriggerIntervalKey}=",
                    "The network message publishing interval in milliseconds. Determines the publishing period at which point messages are emitted.\nWhen `--bs` is 1 and `--bi` is set to 0 batching is disabled.\nDefault: `10000` (10 seconds).\nAlso can be set using `BatchTriggerInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (uint k) => this[PublisherConfig.BatchTriggerIntervalKey] = TimeSpan.FromMilliseconds(k).ToString() },
                    { "si|iothubsendinterval=",
                        "The network message publishing interval in seconds for backwards compatibilty. \nDefault: `10` seconds.\n",
                        (string k) => this[PublisherConfig.BatchTriggerIntervalKey] = TimeSpan.FromSeconds(int.Parse(k, CultureInfo.CurrentCulture)).ToString(), true },
                { $"bs|batchsize=|{PublisherConfig.BatchSizeKey}=",
                    "The number of incoming OPC UA subscription notifications to collect until sending a network messages. When `--bs` is set to 1 and `--bi` is 0 batching is disabled and messages are sent as soon as notifications arrive.\nDefault: `50`.\n",
                    (uint i) => this[PublisherConfig.BatchSizeKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"rdb|removedupsinbatch:|{PublisherConfig.RemoveDuplicatesFromBatchKey}:",
                    "Use this option to remove values with the same node id from batch messages in legacy `Samples` mode. Sends only the latest value as per the value's source timestamp.\nOnly applies to `Samples` mode, otherwise this setting is ignored.\nDefault: `false` (keep all duplicate values).\n",
                    (bool? b) => this[PublisherConfig.RemoveDuplicatesFromBatchKey] = b?.ToString() ?? "True" },
                { $"ms|maxmessagesize=|iothubmessagesize=|{PublisherConfig.IoTHubMaxMessageSizeKey}=",
                    "The maximum size of the messages to emit. In case the encoder cannot encode a message because the size would be exceeded, the message is dropped. Otherwise the encoder will aim to chunk messages if possible. \nDefault: `256k` in case of IoT Hub messages, `0` otherwise.\n",
                    (uint i) => this[PublisherConfig.IoTHubMaxMessageSizeKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"qos|{PublisherConfig.DefaultQualityOfServiceKey}=",
                    $"The default quality of service to use for data set messages.\nThis does not apply to metadata messages which are always sent with `AtLeastOnce` semantics.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<QoS>())}`\nDefault: `{nameof(QoS.AtLeastOnce)}`.\n",
                    (QoS q) => this[PublisherConfig.DefaultQualityOfServiceKey] = q.ToString() },
                { $"ttl|{PublisherConfig.DefaultMessageTimeToLiveKey}=",
                    "The default time to live for all network message published in milliseconds if the transport supports it.\nThis does not apply to metadata messages which are always sent with a ttl of the metadata update interval or infinite ttl.\nDefault: `not set` (infinite).\n",
                    (uint k) => this[PublisherConfig.DefaultMessageTimeToLiveKey] = TimeSpan.FromMilliseconds(k).ToString() },
                { $"retain:|{PublisherConfig.DefaultMessageRetentionKey}:",
                    "Whether by default to send messages with retain flag to a broker if the transport supports it.\nThis does not apply to metadata messages which are always sent as retained messages.\nDefault: `false'.\n",
                    (bool? b) => this[PublisherConfig.DefaultMessageRetentionKey] = b?.ToString() ?? "True" },

                // TODO: Add ConfiguredMessageSize

                { $"mts|messagetimestamp=|{PublisherConfig.MessageTimestampKey}=",
                    $"The value to set as as the timestamp property of messages during encoding (if the encoding supports writing message timestamps).\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<MessageTimestamp>())}`\nDefault: `{nameof(MessageTimestamp.CurrentTimeUtc)}` to use the time when the message was created in OPC Publisher.\n",
                    (MessageTimestamp m) => this[PublisherConfig.MessageTimestampKey] = m.ToString() },
                { $"npd|maxnodesperdataset=|{PublisherConfig.MaxNodesPerDataSetKey}=",
                    "Maximum number of nodes within a Subscription. When there are more nodes configured for a data set writer, they will be added to new subscriptions. This also affects metadata message size. \nDefault: `1000`.\n",
                    (uint i) => this[PublisherConfig.MaxNodesPerDataSetKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"kfc|keyframecount=|{PublisherConfig.DefaultKeyFrameCountKey}=",
                    "The default number of delta messages to send until a key frame message is sent. If 0, no key frame messages are sent, if 1, every message will be a key frame. \nDefault: `0`.\n",
                    (uint i) => this[PublisherConfig.DefaultKeyFrameCountKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"ka|sendkeepalives:|{PublisherConfig.EnableDataSetKeepAlivesKey}:",
                    "Enables sending keep alive messages triggered by writer subscription's keep alive notifications. This setting can be used to enable the messaging profile's support for keep alive messages.\nIf the chosen messaging profile does not support keep alive messages this setting is ignored.\nDefault: `false` (to save bandwidth).\n",
                    (bool? b) => this[PublisherConfig.EnableDataSetKeepAlivesKey] = b?.ToString() ?? "True" },
                { $"msi|metadatasendinterval=|{PublisherConfig.DefaultMetaDataUpdateTimeKey}=",
                    "Default value in milliseconds for the metadata send interval which determines in which interval metadata is sent.\nEven when disabled, metadata is still sent when the metadata version changes unless `--mm=*Samples` is set in which case this setting is ignored. Only valid for network message encodings. \nDefault: `0` which means periodic sending of metadata is disabled.\n",
                    (uint i) => this[PublisherConfig.DefaultMetaDataUpdateTimeKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"dm|disablemetadata:|{PublisherConfig.DisableDataSetMetaDataKey}:",
                    "Disables sending any metadata when metadata version changes. This setting can be used to also override the messaging profile's default support for metadata sending.\nIt is recommended to disable sending metadata when too many nodes are part of a data set as this can slow down start up time.\nDefault: `false` if the messaging profile selected supports sending metadata and `--strict` is set but not '--dct', `True` otherwise.\n",
                    (bool? b) => this[PublisherConfig.DisableDataSetMetaDataKey] = b?.ToString() ?? "True" },
                    { $"lc|legacycompatibility=|{LegacyCompatibility}=",
                        "Run the publisher in legacy (2.5.x) compatibility mode.\nDefault: `false` (disabled).\n",
                        b => this[LegacyCompatibility] = b, true },
                { $"amt|asyncmetadataloadtimeout=|{PublisherConfig.AsyncMetaDataLoadTimeoutKey}=",
                    $"The default duration in seconds a publish request should wait until the meta data is loaded.\nLoaded metadata guarantees a metadata message is sent before the first message is sent but loading of metadata takes time during subscription setup. Set to `0` to block until metadata is loaded.\nOnly used if meta data is supported and enabled.\nDefault: `{PublisherConfig.AsyncMetaDataLoadTimeoutDefaultMillis}` milliseconds.\n",
                    (uint i) => this[PublisherConfig.AsyncMetaDataLoadTimeoutKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"ps|publishschemas:|{PublisherConfig.PublishMessageSchemaKey}:",
                    "Publish the Avro or Json message schemas to schema registry or subtopics.\nAutomatically enables complex type system and metadata support.\nOnly has effect if the messaging profile supports publishing schemas.\nDefault: `True` if the message encoding requires schemas (for example Avro) otherwise `False`.\n",
                    (bool? b) => this[PublisherConfig.PublishMessageSchemaKey] = b?.ToString() ?? "True" },
                    { $"asj|preferavro:|{PublisherConfig.PreferAvroOverJsonSchemaKey}:",
                        "Publish Avro schema even for Json encoded messages. Automatically enables publishing schemas as if `--ps` was set.\nDefault: `false`.\n",
                        (bool? b) => this[PublisherConfig.PreferAvroOverJsonSchemaKey] = b?.ToString() ?? "True" },
                    { $"daf|disableavrofiles:|{Configuration.AvroWriter.DisableKey}:",
                        "Disable writing avro files and instead dump messages and schema as zip files using the filesystem transport.\nDefault: `false`.\n",
                        (bool? b) => this[Configuration.AvroWriter.DisableKey] = b?.ToString() ?? "True" },
                { $"om|maxsendqueuesize=|{PublisherConfig.MaxNetworkMessageSendQueueSizeKey}=",
                    $"The maximum number of messages to buffer on the send path before messages are dropped.\nDefault: `{PublisherConfig.MaxNetworkMessageSendQueueSizeDefault}`\n",
                    (uint i) => this[PublisherConfig.MaxNetworkMessageSendQueueSizeKey] = i.ToString(CultureInfo.InvariantCulture) },
                    { "maxoutgressmessages|MaxOutgressMessages=", "Deprecated - do not use",
                        (string i) => this[PublisherConfig.MaxNetworkMessageSendQueueSizeKey] = i, true },
                { $"wgp|writergrouppartitions=|{PublisherConfig.DefaultWriterGroupPartitionCountKey}=",
                    "The number of partitions to split the writer group into. Each partition represents a data flow to the transport sink. The partition is selected by topic hash.\nDefault: `0` (partitioning is disabled)\n",
                    (ushort i) => this[PublisherConfig.DefaultWriterGroupPartitionCountKey] = i.ToString(CultureInfo.InvariantCulture) },
                { $"t|dmt=|defaultmessagetransport=|{PublisherConfig.DefaultTransportKey}=",
                    $"The desired transport to use to publish network messages with.\nRequires the transport to be properly configured (see transport settings).\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<WriterGroupTransport>())}`\nDefault: `{nameof(WriterGroupTransport.IoTHub)}` or the first configured transport of the allowed value list.\n",
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
                        $"Protocol to use for communication with EdgeHub.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<TransportOption>())}`\nDefault: `{nameof(TransportOption.Mqtt)}` if device or edge hub connection string is provided, ignored otherwise.\n",
                        (TransportOption p) => this[Configuration.IoTEdge.HubTransport] = p.ToString() },
                { $"eh=|eventhubnamespaceconnectionstring=|{Configuration.EventHubs.EventHubNamespaceConnectionString}=",
                    "The connection string of an existing event hub namespace to use for the Azure EventHub transport.\nDefault: `not set`.\n",
                    eh => this[Configuration.EventHubs.EventHubNamespaceConnectionString] = eh },
                { $"sg=|schemagroup=|{Configuration.EventHubs.SchemaGroupNameKey}=",
                    "The schema group in an event hub namespace to publish message schemas to.\nDefault: `not set`.\n",
                    sg => this[Configuration.EventHubs.SchemaGroupNameKey] = sg },
                { $"d|dcs=|daprconnectionstring=|{Configuration.Dapr.DaprConnectionStringKey}=",
                    $"Connect the OPC Publisher to a dapr pub sub component using a connection string.\nThe connection string specifies the PubSub component to use and allows you to configure the side car connection if needed.\nUse the format 'PubSubComponent=<PubSubComponent>[;GrpcPort=<GrpcPort>;HttpPort=<HttpPort>[;Scheme=<'https'|'http'>][;Host=<IPorDnsName>]][;CheckSideCarHealth=<'true'|'false'>]'.\nTo publish through dapr by default specify `-t={nameof(WriterGroupTransport.Dapr)}`.\nDefault: `not set`.\n",
                    dcs => this[Configuration.Dapr.DaprConnectionStringKey] = dcs },
                { $"w|hcs=|httpconnectionstring=|{Configuration.Http.HttpConnectionStringKey}=",
                    $"Allows OPC Publisher to publish multipart messages to a topic path using the http protocol (web hook). Specify the target host and configure the optional connection settings using a connection string of the format 'HostName=<IPorDnsName>[;Port=<Port>][;Scheme=<'https'|'http'>][;Put=true][;ApiKey=<ApiKey>]'. To publish via HTTP by default specify `-t={nameof(WriterGroupTransport.Http)}`.\nDefault: `not set`.\n",
                    hcs => this[Configuration.Http.HttpConnectionStringKey] = hcs },
                { $"o|outdir=|{Configuration.FileSystem.OutputRootKey}=",
                    $"A folder to write messages into.\nUse this option to have OPC Publisher write messages to a folder structure under this folder. The structure reflects the topic tree. To publish into the file system folder by default specify `-t={nameof(WriterGroupTransport.FileSystem)}`.\nDefault: `not set`.\n",
                    or => this[Configuration.FileSystem.OutputRootKey] = or },
                { $"p|httpserverport=|{PublisherConfig.HttpServerPortKey}=",
                    $"The port on which the http server of OPC Publisher is listening.\nDefault: `{PublisherConfig.HttpServerPortDefault}` if no value is provided.\n",
                    p => this[PublisherConfig.HttpServerPortKey] = p },
                { $"unsecurehttp:|{PublisherConfig.UnsecureHttpServerPortKey}:",
                    $"Allow unsecure access to the REST api of OPC Publisher. A port can be specified if the default port {PublisherConfig.UnsecureHttpServerPortDefault} is not desired.\nDo not enable this in production as it exposes the Api Key on the network.\nDefault: `disabled`, if specified without a port `{PublisherConfig.UnsecureHttpServerPortDefault}` port is used.\n",
                    (ushort? p) => this[PublisherConfig.UnsecureHttpServerPortKey] = p?.ToString(CultureInfo.CurrentCulture) ?? PublisherConfig.UnsecureHttpServerPortDefault.ToString(CultureInfo.CurrentCulture) },
                { $"rtc|renewtlscert:|{PublisherConfig.RenewTlsCertificateOnStartupKey}:",
                    "If set a new tls certificate is created during startup updating any previously created ones.\nDefault: `false`.\n",
                    (bool? b) => this[PublisherConfig.RenewTlsCertificateOnStartupKey] = b?.ToString() ?? "True" },
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
                    "The default topic that all messages are sent to.\nIf not specified, the `{{RootTopic}}/messages/{{WriterGroup}}` template will be used as root topic for all events sent by OPC Publisher.\nThe template variables\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{Encoding}}`\n    `{{PublisherId}}`\n    `{{DataSetClassId}}`\n    `{{DataSetWriter}}` and\n    `{{WriterGroup}}`\n can be used as dynamic parts in the template. If a template variable does not exist the name of the variable is emitted.\nDefault: `{{RootTopic}}/messages/{{WriterGroup}}`.\n",
                    t => this[PublisherConfig.TelemetryTopicTemplateKey] = t },
                { $"ett|eventstopictemplate=|{PublisherConfig.EventsTopicTemplateKey}=",
                    "The topic into which OPC Publisher publishes any events that are not telemetry messages such as discovery or runtime events.\nIf not specified, the `{{RootTopic}}/events` template will be used.\nOnly\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{Encoding}}` and\n    `{{PublisherId}}`\ncan currently be used as replacement variables in the template.\nDefault: `{{RootTopic}}/events`.\n",
                    t => this[PublisherConfig.EventsTopicTemplateKey] = t },
                { $"dtt|diagnosticstopictemplate=|{PublisherConfig.DiagnosticsTopicTemplateKey}=",
                    "The topic into which OPC Publisher publishes writer group diagnostics events.\nIf not specified, the `{{RootTopic}}/diagnostics/{{WriterGroup}}` template will be used.\nOnly\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{Encoding}}`\n    `{{PublisherId}}` and\n    `{{WriterGroup}}`\ncan currently be used as replacement variables in the template.\nDefault: `{{RootTopic}}/diagnostics/{{WriterGroup}}`\n",
                    t => this[PublisherConfig.DiagnosticsTopicTemplateKey] = t },
                { $"mdt|metadatatopictemplate:|{PublisherConfig.DataSetMetaDataTopicTemplateKey}:",
                    "The topic that metadata should be sent to.\nIn case of MQTT the message will be sent as RETAIN message with a TTL of either metadata send interval or infinite if metadata send interval is not configured.\nOnly valid if metadata is supported and/or explicitely enabled.\nThe template variables\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{TelemetryTopic}}`\n    `{{Encoding}}`\n    `{{PublisherId}}`\n    `{{DataSetClassId}}`\n    `{{DataSetWriter}}` and\n    `{{WriterGroup}}`\ncan be used as dynamic parts in the template. \nDefault: `{{TelemetryTopic}}` which means metadata is sent to the same output as regular messages. If specified without value, the default output is `{{TelemetryTopic}}/metadata`.\n",
                    s => this[PublisherConfig.DataSetMetaDataTopicTemplateKey] = !string.IsNullOrEmpty(s) ? s : PublisherConfig.MetadataTopicTemplateDefault },
                { $"stt|schematopictemplate:|{PublisherConfig.SchemaTopicTemplateKey}:",
                    "The topic that schemas should be sent to if schema publishing is configured.\nIn case of MQTT schemas will not be sent with .\nOnly valid if schema publishing is enabled (`--ps`).\nThe template variables\n    `{{RootTopic}}`\n    `{{SiteId}}`\n    `{{PublisherId}}`\n    `{{TelemetryTopic}}`\ncan be used as variables inside the template. \nDefault: `{{TelemetryTopic}}/schema` which means the schema is sent to a sub topic where the telemetry message is sent to.\n",
                    s => this[PublisherConfig.SchemaTopicTemplateKey] = !string.IsNullOrEmpty(s) ? s : PublisherConfig.SchemaTopicTemplateDefault },
                { $"uns|datasetrouting=|{PublisherConfig.DefaultDataSetRoutingKey}=",
                    $"Configures whether messages should automatically be routed using the browse path of the monitored item inside the address space starting from the RootFolder.\nThe browse path is appended as topic structure to the telemetry topic root which can be configured using `--ttt`. Reserved characters in browse names are escaped with their hex ASCII code.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<DataSetRoutingMode>())}`\nDefault: `{nameof(DataSetRoutingMode.None)}` (Topics must be configured).\n",
                    (DataSetRoutingMode m) => this[PublisherConfig.DefaultDataSetRoutingKey] = m.ToString() },
                { $"ri|enableroutinginfo:|{PublisherConfig.EnableDataSetRoutingInfoKey}:",
                    $"Add routing information to messages. The name of the property is `{Constants.MessagePropertyRoutingKey}` and the value is the `DataSetWriterGroup` from which the particular message is emitted.\nDefault: `{PublisherConfig.EnableDataSetRoutingInfoDefault}`.\n",
                    (bool? b) => this[PublisherConfig.EnableDataSetRoutingInfoKey] = b?.ToString() ?? "True" },

                "",
                "Subscription settings",
                "---------------------",
                "",

                { $"oi|opcsamplinginterval=|{OpcUaSubscriptionConfig.DefaultSamplingIntervalKey}=",
                    "Default value in milliseconds to request the servers to sample values. This value is used if an explicit sampling interval for a node was not configured. \nDefault: `1000`.\nAlso can be set using `DefaultSamplingInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (uint i) => this[OpcUaSubscriptionConfig.DefaultSamplingIntervalKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"op|opcpublishinginterval=|{OpcUaSubscriptionConfig.DefaultPublishingIntervalKey}=",
                    "Default value in milliseconds for the publishing interval setting of a subscription created with an OPC UA server. This value is used if an explicit publishing interval was not configured.\nWhen setting `--op=0` the server decides the lowest publishing interval it can support.\nDefault: `1000`.\nAlso can be set using `DefaultPublishingInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (uint i) => this[OpcUaSubscriptionConfig.DefaultPublishingIntervalKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"eip|immediatepublishing:|{OpcUaSubscriptionConfig.EnableImmediatePublishingKey}:",
                    "By default OPC Publisher will create a subscription with publishing disabled and only enable it after it has filled it with all configured monitored items. Use this setting to create the subscription with publishing already enabled.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.EnableImmediatePublishingKey] = b?.ToString() ?? "True" },
                { $"ska|keepalivecount=|{OpcUaSubscriptionConfig.DefaultKeepAliveCountKey}=",
                    "Specifies the default number of publishing intervals before a keep alive is returned with the next queued publishing response.\nDefault: `auto set based on publishing interval`.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.DefaultKeepAliveCountKey] = u.ToString(CultureInfo.CurrentCulture) },
                    { "kt|keepalivethreshold=|MaxKeepAliveCount=",
                    "Legacy way of specifying the keep alive counter.\n",
                        (string s) => this[OpcUaSubscriptionConfig.DefaultKeepAliveCountKey] = s, true },
                { $"slt|lifetimecount=|{OpcUaSubscriptionConfig.DefaultLifetimeCountKey}=",
                    "Default subscription lifetime count which is a multiple of the keep alive counter and when reached instructs the server to declare the subscription invalid.\nDefault: `auto set based on publishing interval`.\n",
                    (uint i) => this[OpcUaSubscriptionConfig.DefaultLifetimeCountKey] = i.ToString(CultureInfo.CurrentCulture) },
                    { "MinSubscriptionLifetime=", "Legacy way of specifying the subscription lifetime.",
                        (string s) => this[OpcUaSubscriptionConfig.DefaultLifetimeCountKey] = s, true },
                { $"fd|fetchdisplayname:|{OpcUaSubscriptionConfig.FetchOpcNodeDisplayNameKey}:",
                    "Fetches the displayname for the monitored items subscribed if a display name was not specified in the configuration.\nNote: This has high impact on OPC Publisher startup performance.\nDefault: `false` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.FetchOpcNodeDisplayNameKey] = b?.ToString() ?? "True" },
                { $"fp|fetchpathfromroot:|{OpcUaSubscriptionConfig.FetchOpcBrowsePathFromRootKey}:",
                    "(Experimental) Explicitly disable or enable retrieving relative paths from root for monitored items.\nDefault: `false` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.FetchOpcBrowsePathFromRootKey] = b?.ToString() ?? "True" },
                { $"qs|queuesize=|{OpcUaSubscriptionConfig.DefaultQueueSizeKey}=",
                    "Default queue size for all monitored items if queue size was not specified in the configuration.\nDefault: `1` (for backwards compatibility).\n",
                    (uint u) => this[OpcUaSubscriptionConfig.DefaultQueueSizeKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"aq|autosetqueuesize:|{OpcUaSubscriptionConfig.AutoSetQueueSizesKey}:",
                    "(Experimental) Automatically calculate queue sizes for monitored items using the subscription publishing interval and the item's sampling rate as max(configured queue size, roundup(publishinginterval / samplinginterval)).\nNote that the server might revise the queue size down if it cannot handle the calculated size.\nDefault: `false` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.AutoSetQueueSizesKey] = b?.ToString() ?? "True" },
                { $"ndo|nodiscardold:|{OpcUaSubscriptionConfig.DefaultDiscardNewKey}:",
                    "The publisher is using this as default value for the discard old setting of monitored item queue configuration. Setting to true will ensure that new values are dropped before older ones are drained. \nDefault: `false` (which is the OPC UA default).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultDiscardNewKey] = b?.ToString() ?? "True" },
                { $"mc|monitoreditemdatachangetrigger=|{OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey}=",
                    $"Default data change trigger for all monitored items configured in the published nodes configuration unless explicitly overridden.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<DataChangeTriggerType>())}`\nDefault: `{nameof(DataChangeTriggerType.StatusValue)}` (which is the OPC UA default).\n",
                    (DataChangeTriggerType t) => this[OpcUaSubscriptionConfig.DefaultDataChangeTriggerKey] = t.ToString() },
                { $"mwt|monitoreditemwatchdog=|{OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogSecondsKey}=",
                    "The subscription and monitored item watchdog timeout in seconds the subscription uses to check on late reporting monitored items unless overridden in the published nodes configuration explicitly.\nDefault: `not set` (watchdog disabled).\n",
                    (uint u) => this[OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogSecondsKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"mwc|monitoreditemwatchdogcondition=|{OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogConditionKey}=",
                    $"The default condition when to run the action configured as the watchdog behavior. The condition can be overridden in the published nodes configuration.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<MonitoredItemWatchdogCondition>())}`\nDefault: `{nameof(MonitoredItemWatchdogCondition.WhenAllAreLate)}` (if enabled).\n",
                    (MonitoredItemWatchdogCondition b) => this[OpcUaSubscriptionConfig.DefaultMonitoredItemWatchdogConditionKey] = b.ToString() },
                { $"dwb|watchdogbehavior=|{OpcUaSubscriptionConfig.DefaultWatchdogBehaviorKey}=",
                    $"Default behavior of the subscription and monitored item watchdog mechanism unless overridden in the published nodes configuration explicitly.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<SubscriptionWatchdogBehavior>())}`\nDefault: `{nameof(SubscriptionWatchdogBehavior.Diagnostic)}` (if enabled).\n",
                    (SubscriptionWatchdogBehavior b) => this[OpcUaSubscriptionConfig.DefaultWatchdogBehaviorKey] = b.ToString() },
                { $"sf|skipfirst:|{OpcUaSubscriptionConfig.DefaultSkipFirstKey}:",
                    $"The publisher is using this as default value for the skip first setting of nodes configured without a skip first setting. A value of True will skip sending the first notification received when the monitored item is added to the subscription.\nDefault: `{OpcUaSubscriptionConfig.DefaultSkipFirstDefault}` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultSkipFirstKey] = b?.ToString() ?? "True" },
                    { "skipfirstevent:", "Maintained for backwards compatibility, do not use.",
                        (string b) => this[OpcUaSubscriptionConfig.DefaultSkipFirstKey] = b ?? "True", /* hidden = */ true },
                { $"rat|republishaftertransfer:|{OpcUaSubscriptionConfig.DefaultRepublishAfterTransferKey}:",
                    $"Configure whether publisher republishes missed subscription notifications still in the server queue after transferring a subscription during reconnect handling.\nThis can result in out of order notifications after a reconnect but minimizes data loss.\nDefault: `{OpcUaSubscriptionConfig.DefaultRepublishAfterTransferDefault}` (disabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultRepublishAfterTransferKey] = b?.ToString() ?? "True" },
                { $"hbb|heartbeatbehavior=|{OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey}=",
                    $"Default behavior of the heartbeat mechanism unless overridden in the published nodes configuration explicitly.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<HeartbeatBehavior>().Where(n => !n.StartsWith(nameof(HeartbeatBehavior.Reserved), StringComparison.InvariantCulture)))}`\nDefault: `{nameof(HeartbeatBehavior.WatchdogLKV)}` (Sending LKV in a watchdog fashion).\n",
                    (HeartbeatBehavior b) => this[OpcUaSubscriptionConfig.DefaultHeartbeatBehaviorKey] = b.ToString() },
                { $"hb|heartbeatinterval=|{OpcUaSubscriptionConfig.DefaultHeartbeatIntervalKey}=",
                    "The publisher is using this as default value in seconds for the heartbeat interval setting of nodes that were configured without a heartbeat interval setting. A heartbeat is sent at this interval if no value has been received.\nDefault: `0` (disabled)\nAlso can be set using `DefaultHeartbeatInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`.\n",
                    (uint i) => this[OpcUaSubscriptionConfig.DefaultHeartbeatIntervalKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"ucr|usecyclicreads:|{OpcUaSubscriptionConfig.DefaultSamplingUsingCyclicReadKey}:",
                    "All nodes should be sampled using periodical client reads instead of subscriptions services, unless otherwise configured.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.DefaultSamplingUsingCyclicReadKey] = b?.ToString() ?? "True" },
                { $"xmi|maxmonitoreditems=|{OpcUaSubscriptionConfig.MaxMonitoredItemPerSubscriptionKey}=",
                    "Max monitored items per subscription until the subscription is split.\nThis is used if the server does not provide limits in its server capabilities.\nDefault: `not set`.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.MaxMonitoredItemPerSubscriptionKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"da|deferredacks:|{OpcUaSubscriptionConfig.UseDeferredAcknoledgementsKey}:",
                    "(Experimental) Acknoledge subscription notifications only when the data has been successfully published.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.UseDeferredAcknoledgementsKey] = b?.ToString() ?? "True" },
                { $"rbp|rebrowseperiod=|{OpcUaSubscriptionConfig.DefaultRebrowsePeriodKey}=",
                    $"(Experimental) The default time to wait until the address space model is browsed again when generating model change notifications.\nDefault: `{OpcUaSubscriptionConfig.DefaultRebrowsePeriodDefault}`.\n",
                    (TimeSpan t) => this[OpcUaSubscriptionConfig.DefaultRebrowsePeriodKey] = t.ToString() },
                { $"sqp|sequentialpublishing:|{OpcUaSubscriptionConfig.EnableSequentialPublishingKey}:",
                    $"Set to false to disable sequential publishing in the protocol stack.\nDefault: `{OpcUaSubscriptionConfig.EnableSequentialPublishingDefault}` (enabled).\n",
                    (bool? b) => this[OpcUaSubscriptionConfig.EnableSequentialPublishingKey] = b?.ToString() ?? "True" },
                { $"smi|subscriptionmanagementinterval=|{OpcUaSubscriptionConfig.SubscriptionManagementIntervalSecondsKey}=",
                    "The interval in seconds after which the publisher re-applies the desired state of the subscription to a session.\nDefault: `0` (only on configuration change).\n",
                    (uint u) => this[OpcUaSubscriptionConfig.SubscriptionManagementIntervalSecondsKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"bnr|badnoderetrydelay=|{OpcUaSubscriptionConfig.BadMonitoredItemRetryDelaySecondsKey}=",
                    $"The delay in seconds after which nodes that were rejected by the server while added or updating a subscription or while publishing, are re-applied to a subscription.\nSet to 0 to disable retrying.\nDefault: `{OpcUaSubscriptionConfig.BadMonitoredItemRetryDelayDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.BadMonitoredItemRetryDelaySecondsKey] = u.ToString(CultureInfo.CurrentCulture)  },
                { $"inr|invalidnoderetrydelay=|{OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelaySecondsKey}=",
                    $"The delay in seconds after which the publisher attempts to re-apply nodes that were incorrectly configured to a subscription.\nSet to 0 to disable retrying.\nDefault: `{OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelayDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.InvalidMonitoredItemRetryDelaySecondsKey] = u.ToString(CultureInfo.CurrentCulture)  },
                { $"ser|subscriptionerrorretrydelay=|{OpcUaSubscriptionConfig.SubscriptionErrorRetryDelaySecondsKey}=",
                    $"The delay in seconds between attempts to create a subscription in a session.\nSet to 0 to disable retrying.\nDefault: `{OpcUaSubscriptionConfig.SubscriptionErrorRetryDelayDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaSubscriptionConfig.SubscriptionErrorRetryDelaySecondsKey] = u.ToString(CultureInfo.CurrentCulture) },

                { $"urc|usereverseconnect:|{PublisherConfig.DefaultUseReverseConnectKey}:",
                    "(Experimental) Use reverse connect for all endpoints in the published nodes configuration unless otherwise configured.\nDefault: `false`.\n",
                    (bool? b) => this[PublisherConfig.DefaultUseReverseConnectKey] = b?.ToString() ?? "True" },
                { $"dtr|disabletransferonreconnect:|{PublisherConfig.DisableSubscriptionTransferKey}:",
                    "Do not attempt to transfer subscriptions when reconnecting but re-establish the subscription.\nDefault: `false`.\n",
                    (bool? b) => this[PublisherConfig.DisableSubscriptionTransferKey] = b?.ToString() ?? "True" },
                { $"dct|disablecomplextypesystem:|{PublisherConfig.DisableComplexTypeSystemKey}:",
                    "Never load the complex type system for any connections that are required for subscriptions.\nThis setting not just disables meta data messages but also prevents transcoding of unknown complex types in outgoing messages.\nDefault: `false`.\n",
                    (bool? b) => this[PublisherConfig.DisableComplexTypeSystemKey] = b?.ToString() ?? "True" },
                { $"dsg|disablesessionpergroup:|{PublisherConfig.DisableSessionPerWriterGroupKey}:",
                    $"Disable creating a separate session per writer group. Instead sessions are re-used across writer groups.\nDefault: `{PublisherConfig.DisableSessionPerWriterGroupDefault}`.\n",
                    (bool? b) => this[PublisherConfig.DisableSessionPerWriterGroupKey] = b?.ToString() ?? "True" },
                { $"ipi|ignorepublishingintervals:|{PublisherConfig.IgnoreConfiguredPublishingIntervalsKey}:",
                    $"Always use the publishing interval provided via command line argument `--op` and ignore all publishing interval settings in the configuration.\nCombine with `--op=0` to let the server use the lowest publishing interval it can support.\nDefault: `{PublisherConfig.IgnoreConfiguredPublishingIntervalsDefault}` (disabled).\n",
                    (bool? b) => this[PublisherConfig.IgnoreConfiguredPublishingIntervalsKey] = b?.ToString() ?? "True" },

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
                    (uint i) => this[OpcUaClientConfig.MinReconnectDelayKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"xr|maxreconnectperiod=|{OpcUaClientConfig.MaxReconnectDelayKey}=",
                    $"The maximum amount of time in millseconds to wait between reconnection attempts of the session.\nDefault: `{OpcUaClientConfig.MaxReconnectDelayDefault}` milliseconds.\n",
                    (uint i) => this[OpcUaClientConfig.MaxReconnectDelayKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"sto|sessiontimeout=|{OpcUaClientConfig.DefaultSessionTimeoutKey}=",
                    $"Maximum amount of time in seconds that a session should remain open by the OPC server without any activity (session timeout). Requested from the OPC server at session creation.\nDefault: `{OpcUaClientConfig.DefaultSessionTimeoutDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaClientConfig.DefaultSessionTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ki|keepaliveinterval=|{OpcUaClientConfig.KeepAliveIntervalKey}=",
                    $"The interval in seconds the publisher is sending keep alive messages to the OPC servers on the endpoints it is connected to.\nDefault: `{OpcUaClientConfig.KeepAliveIntervalDefaultSec}` seconds.\n",
                    (uint i) => this[OpcUaClientConfig.KeepAliveIntervalKey] = i.ToString(CultureInfo.CurrentCulture) },
                { $"sct|servicecalltimeout=|{OpcUaClientConfig.DefaultServiceCallTimeoutKey}=",
                    $"Maximum amount of time in seconds that a service call should take before it is being cancelled.\nThis value can be overridden in the request header.\nDefault: `{OpcUaClientConfig.DefaultServiceCallTimeoutDefaultSec}` seconds.\n",
                    (uint u) => this[OpcUaClientConfig.DefaultServiceCallTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"cto|connecttimeout=|{OpcUaClientConfig.DefaultConnectTimeoutKey}=",
                    "Maximum amount of time in seconds that a service call should wait for a connected session to be used.\nThis value can be overridden in the request header.\nDefault: `not set` (in this case the default service call timeout value is used).\n",
                    (uint u) => this[OpcUaClientConfig.DefaultConnectTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ot|operationtimeout=|{OpcUaClientConfig.OperationTimeoutKey}=",
                    $"The operation service call timeout of individual service requests to the server in milliseconds. As opposed to the `--sco` timeout, this is the timeout hint provided to the server in every request.\nThis value can be overridden in the request header.\nDefault: `{OpcUaClientConfig.OperationTimeoutDefault}` milliseconds.\n",
                    (uint u) => this[OpcUaClientConfig.OperationTimeoutKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"cl|clientlinger=|{OpcUaClientConfig.LingerTimeoutSecondsKey}=",
                    "Amount of time in seconds to delay closing a client and underlying session after the a last service call.\nUse this setting to speed up multiple subsequent calls.\nDefault: `0` sec (no linger).\n",
                    (uint u) => this[OpcUaClientConfig.LingerTimeoutSecondsKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"rcp|reverseconnectport=|{OpcUaClientConfig.ReverseConnectPortKey}=",
                    $"The port to use when accepting inbound reverse connect requests from servers.\nDefault: `{OpcUaClientConfig.ReverseConnectPortDefault}`.\n",
                    (ushort u) => this[OpcUaClientConfig.ReverseConnectPortKey] = u.ToString(CultureInfo.CurrentCulture) },

                { $"mnr|maxnodesperread=|{OpcUaClientConfig.MaxNodesPerReadOverrideKey}=",
                    "Limit max number of nodes to read in a single read request when batching reads or the server limit if less.\nDefault: `0` (using server limit).\n",
                    (uint u) => this[OpcUaClientConfig.MaxNodesPerReadOverrideKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"mnb|maxnodesperbrowse=|{OpcUaClientConfig.MaxNodesPerBrowseOverrideKey}=",
                    "Limit max number of nodes per browse request when batching browse operations or the server limit if less.\nDefault: `0` (using server limit).\n",
                    (uint u) => this[OpcUaClientConfig.MaxNodesPerBrowseOverrideKey] = u.ToString(CultureInfo.CurrentCulture) },

                { $"mpr|minpublishrequests=|{OpcUaClientConfig.MinPublishRequestsKey}=",
                    $"Minimum number of publish requests to queue once subscriptions are created in the session.\nDefault: `{OpcUaClientConfig.MinPublishRequestsDefault}`.\n",
                    (uint u) => this[OpcUaClientConfig.MinPublishRequestsKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"ppr|percentpublishrequests=|{OpcUaClientConfig.PublishRequestsPerSubscriptionPercentKey}=",
                    $"Percentage ratio of publish requests per subscriptions in the session in percent up to the number configured using `--xpr`.\nDefault: `{OpcUaClientConfig.PublishRequestsPerSubscriptionPercentDefault}`% (1 request per subscription).\n",
                    (ushort u) => this[OpcUaClientConfig.PublishRequestsPerSubscriptionPercentKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"xpr|maxpublishrequests=|{OpcUaClientConfig.MaxPublishRequestsKey}=",
                    $"Maximum number of publish requests to every queue once subscriptions are created in the session.\nDefault: `{OpcUaClientConfig.MaxPublishRequestsDefault}`.\n",
                    (uint u) => this[OpcUaClientConfig.MaxPublishRequestsKey] = u.ToString(CultureInfo.CurrentCulture) },
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
                { $"cfa|configurefromappcert:|{OpcUaClientConfig.TryConfigureFromExistingAppCertKey}:",
                    "Automatically set the application subject name, host name and application uri from the first valid application certificate found in the application certificate store path.\nIf the chosen certificate is valid, it will be used, otherwise a new, self-signed certificate with the information will be created.\nDefault: `false`.\n",
                    (bool? b) => this[OpcUaClientConfig.TryConfigureFromExistingAppCertKey] = b?.ToString() ?? "True" },
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

                { $"ll|loglevel=|{Configuration.LoggingLevel.LogLevelKey}=",
                    $"The loglevel to use.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<LogLevel>())}`\nDefault: `{LogLevel.Information}`.\n",
                    (LogLevel l) => this[Configuration.LoggingLevel.LogLevelKey] = l.ToString() },
                { $"lfm|logformat=|{Configuration.LoggingFormat.LogFormatKey}=",
                    $"The log format to use when writing to the console.\nAllowed values:\n    `{string.Join("`\n    `", Configuration.LoggingFormat.LogFormatsSupported)}`\nDefault: `{Configuration.LoggingFormat.LogFormatDefault}`.\n",
                    (string s) => this[Configuration.LoggingFormat.LogFormatKey] = s },
                { $"di|diagnosticsinterval=|{PublisherConfig.DiagnosticsIntervalKey}=",
                    "Produce publisher diagnostic information at this specified interval in seconds.\nBy default diagnostics are written to the OPC Publisher logger (which requires at least --loglevel `information`) unless configured differently using `--pd`.\n`0` disables diagnostic output.\nDefault:60000 (60 seconds).\nAlso can be set using `DiagnosticsInterval` environment variable in the form of a duration string in the form `[d.]hh:mm:ss[.fffffff]`\".\n",
                    (uint i) => this[PublisherConfig.DiagnosticsIntervalKey] = TimeSpan.FromSeconds(i).ToString() },
                { $"pd|diagnosticstarget=|{PublisherConfig.DiagnosticsTargetKey}=",
                    $"Configures how to emit diagnostics information at the `--di` configured interval.\nUse this to for example emit diagnostics as events to the event topic template instead of the console.\nAllowed values:\n    `{string.Join("`\n    `", Enum.GetNames<PublisherDiagnosticTargetType>())}`\nDefault: `{PublisherDiagnosticTargetType.Logger}`.\n",
                    (PublisherDiagnosticTargetType d) => this[PublisherConfig.DiagnosticsTargetKey] = d.ToString() },
                { $"dr|disableresourcemonitoring:|{PublisherConfig.DisableResourceMonitoringKey}:",
                    "Disable resource monitoring as part of the diagnostics output and metrics.\nDefault: `false` (enabled).\n",
                    (bool? b) => this[PublisherConfig.DisableResourceMonitoringKey] = b?.ToString() ?? "True" },
                { "ln|lognotifications:",
                    "Log ingress subscription notifications at Informational level to aid debugging.\nDefault: `disabled`.\n",
                    (bool? b) => this[PublisherConfig.DebugLogNotificationsKey] = b?.ToString() ?? "True" },
                { "lnh|lognotificationsandheartbeats:",
                    "Include heartbeats in notifications log.\nIf set also implicitly enables debug logging via `--ln`.\nDefault: `disabled`.\n",
                    (bool? b) => this[PublisherConfig.DebugLogNotificationsWithHeartbeatKey] = b?.ToString() ?? "True" },
                { "lnf|lognotificationfilter:",
                    "Only log notifications where the data set field name, subscription name, or data set name match the provided regular expression pattern.\nIf set implicitly enables debug logging via `--ln`.\nDefault: `null` (matches all).\n",
                    (string? r) => this[PublisherConfig.DebugLogNotificationsFilterKey] = r },
                { "len|logencodednotifications:",
                    "Log encoded subscription and monitored item notifications at Informational level to aid debugging.\nDefault: `disabled`.\n",
                    (bool? b) => this[PublisherConfig.DebugLogEncodedNotificationsKey] = b?.ToString() ?? "True" },
                { $"sl|opcstacklogging:|{OpcUaClientConfig.EnableOpcUaStackLoggingKey}:",
                    "Enable opc ua stack logging beyond logging at error level.\nDefault: `disabled`.\n",
                    (bool? b) => this[OpcUaClientConfig.EnableOpcUaStackLoggingKey] = b?.ToString() ?? "True" },
                { $"ksf|keysetlogfolder:|{OpcUaClientConfig.OpcUaKeySetLogFolderNameKey}:",
                    "Writes negotiated symmetric keys for all running client connection to this file.\nThe file can be loaded by Wireshark 4.3 and used to decrypt encrypted channels when analyzing network traffic captures.\nNote that enabling this feature presents a security risk!\nDefault: `disabled`.\n",
                    (string? f) => this[OpcUaClientConfig.OpcUaKeySetLogFolderNameKey] = f ?? Directory.GetCurrentDirectory() },
                { $"ecw|enableconsolewriter:|{Configuration.ConsoleWriter.EnableKey}:",
                    "Enable writing encoded messages to standard error log through the filesystem transport (must enable via `-t FileSystem` and `-o` must be set to either `stderr` or `stdout`).\nDefault: `false`.\n",
                    (bool? b) => this[Configuration.ConsoleWriter.EnableKey] = b?.ToString() ?? "True" },
                { $"oc|otlpcollector=|{Configuration.Otlp.OtlpCollectorEndpointKey}=",
                    "Specifiy the OpenTelemetry collector grpc endpoint url to export diagnostics to.\nDefault: `disabled`.\n",
                    s => this[Configuration.Otlp.OtlpCollectorEndpointKey] = s },
                { $"oxi|otlpexportinterval=|{Configuration.Otlp.OtlpExportIntervalMillisecondsKey}=",
                    $"The interval in milliseconds when OpenTelemetry is exported to the collector endpoint.\nDefault: `{Configuration.Otlp.OtlpExportIntervalMillisecondsDefault}` ({Configuration.Otlp.OtlpExportIntervalMillisecondsDefault / 1000} seconds).\n",
                    (uint i) => this[Configuration.Otlp.OtlpExportIntervalMillisecondsKey] = TimeSpan.FromMilliseconds(i).ToString() },
                { $"mms|maxmetricstreams=|{Configuration.Otlp.OtlpMaxMetricStreamsKey}=",
                    $"Specifiy the max number of streams to collect in the default view.\nDefault: `{Configuration.Otlp.OtlpMaxMetricDefault}`.\n",
                    (uint u) => this[Configuration.Otlp.OtlpMaxMetricStreamsKey] = u.ToString(CultureInfo.CurrentCulture) },
                { $"em|enableprometheusendpoint:|{Configuration.Otlp.EnableMetricsKey}:",
                    "Explicitly enable or disable exporting prometheus metrics directly on the standard path.\nDefault: `disabled` if Otlp collector is configured, otherwise `enabled`.\n",
                    (bool? b) => this[Configuration.Otlp.EnableMetricsKey] = b?.ToString() ?? "True" },
                { $"ari|addruntimeinstrumentation:|{Configuration.Otlp.OtlpRuntimeInstrumentationKey}:",
                    $"Include metrics captured for the underlying runtime and web server.\nDefault: `{Configuration.Otlp.OtlpRuntimeInstrumentationDefault}`.\n",
                    (bool? b) => this[Configuration.Otlp.OtlpRuntimeInstrumentationKey] = b?.ToString() ?? "True" },
                { $"ats|addtotalsuffix:|{Configuration.Otlp.OtlpTotalNameSuffixForCountersKey}:",
                    $"Add total suffix to all counter instrument names when exporting metrics via prometheus exporter.\nDefault: `{Configuration.Otlp.OtlpTotalNameSuffixForCountersDefault}`.\n",
                    (bool? b) => this[Configuration.Otlp.OtlpTotalNameSuffixForCountersKey] = b?.ToString() ?? "True" },

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
                _logger.Warning("Parse args exception {0}.", e.Message);
                _logger.ExitProcess(160);
                return;
            }

            if (unsupportedOptions.Count > 0)
            {
                foreach (var option in unsupportedOptions)
                {
                    _logger.Warning("Option {0} wrong or not supported, " +
                        "please use -h option to get all the supported options.", option);
                }
            }

            if (legacyOptions.Count > 0)
            {
                foreach (var option in legacyOptions)
                {
                    _logger.Warning("Legacy option {0} not supported, please use -h option to get all the supported options.", option);
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
                    _ = new PublisherConfig(configuration).ToOptions();
                }
                catch (Exception ex)
                {
                    _logger.Warning("{0}\nPlease use -h option to get all the supported options.",
                        ex.Message);
                    _logger.ExitProcess(170);
                    return;
                }

                // Validate edge configuration
                var iotEdgeOptions = new IoTEdgeClientOptions();
                new Configuration.IoTEdge(configuration).Configure(iotEdgeOptions);

                // Check that the important values are provided
                if (iotEdgeOptions.EdgeHubConnectionString == null)
                {
                    _logger.Warning(
                        "To connect to Azure IoT Hub you must run as module inside IoT Edge or " +
                        "specify a device connection string using EdgeHubConnectionString " +
                        "environment variable or command line.");
                }
            }
            else
            {
                options.WriteOptionDescriptions(Console.Out);
                _logger.ExitProcess(0);
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

        private readonly CommandLineLogger _logger;
    }

    /// <summary>
    /// Log command line errors
    /// </summary>
    public class CommandLineLogger
    {
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
    }
}
