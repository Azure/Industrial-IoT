# OPC Publisher configuration via command line options and environment variables

[Home](./readme.md)

> This documentation applies to version 2.9

The following OPC Publisher configuration can be applied by Command Line Interface (CLI) options or as environment variable settings. Any CamelCase options can also be provided using environment variables (without the preceding `--`). When both environment variable and CLI argument are provided, the command line option will override the environment variable.

> IMPORTANT: The command line of OPC Publisher only understands below command line options. You cannot specify environment variables on the command line (e.g., like `env1=value env2=value`). All option names are **case-sensitive**!

Secrets such as `EdgeHubConnectionString`, other connection strings, or the `ApiKey` should never be provided on the command line or as environment variables. It should be avoided at all cost. A file using the `.env` format can be specified using the `ADDITIONAL_CONFIGURATION` environment variable. The contents will be loaded before the command line arguments are evaluated. If a file name is not provided via said environment variable, OPC Publisher tries to load the `/run/secrets/.env` file. This approach integrates well with [docker secrets](https://github.com/compose-spec/compose-spec/blob/master/05-services.md#secrets). An example of this can be found [here](https://raw.githubusercontent.com/Azure/Industrial-IoT/main/deploy/docker/docker-compose.yaml).

> Please note that rolling of secrets is not supported and that any errors loading secrets is silently discarded.

```text
 ██████╗ ██████╗  ██████╗    ██████╗ ██╗   ██╗██████╗ ██╗     ██╗███████╗██╗  ██╗███████╗██████╗
██╔═══██╗██╔══██╗██╔════╝    ██╔══██╗██║   ██║██╔══██╗██║     ██║██╔════╝██║  ██║██╔════╝██╔══██╗
██║   ██║██████╔╝██║         ██████╔╝██║   ██║██████╔╝██║     ██║███████╗███████║█████╗  ██████╔╝
██║   ██║██╔═══╝ ██║         ██╔═══╝ ██║   ██║██╔══██╗██║     ██║╚════██║██╔══██║██╔══╝  ██╔══██╗
╚██████╔╝██║     ╚██████╗    ██║     ╚██████╔╝██████╔╝███████╗██║███████║██║  ██║███████╗██║  ██║
 ╚═════╝ ╚═╝      ╚═════╝    ╚═╝      ╚═════╝ ╚═════╝ ╚══════╝╚═╝╚══════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
                                                 2.9.12 (.NET 8.0.8/win-x64/OPC Stack 1.5.374.78)
General
-------

  -h, --help                 Show help and exit.
  -f, --pf, --publishfile, --PublishedNodesFile=VALUE
                             The name of the file containing the configuration
                               of the nodes to be published as well as the
                               information to connect to the OPC UA server
                               sources.
                               This file is also used to persist changes made
                               through the control plane, e.g., through IoT Hub
                               device method calls.
                               When no file is specified a default `
                               publishednodes.json` file is created in the
                               working directory.
                               Default: `publishednodes.json`
      --cf, --createifnotexist, --CreatePublishFileIfNotExistKey[=VALUE]
                             Permit publisher to create the the specified
                               publish file if it does not exist. The new file
                               will be created under the access rights of the
                               publisher module.
                               The default file 'publishednodes.json' is always
                               created when no file name was provided on the
                               command line and this option is ignored.
                               If a file was specified but does not exist and
                               should not be created the module exits.
                               Default: `false`
      --pol, --usepolling, --UseFileChangePolling[=VALUE]
                             Poll for file changes instead of using a file
                               system watcher.
                               Use this setting when the underlying file system
                               does not support file system notifications such
                               as in some docker container setups.
                               Default: `false`
      --fe, --forceencryptedcredentials, --ForceCredentialEncryption[=VALUE]
                             If set to true the publisher will never write
                               plain text credentials into the published nodes
                               configuration file.
                               If a credential cannot be written to the file
                               using the IoT Edge workload API crypto provider
                               the publisher will exit with an error.
                               Default: `false` (write secrets as plain text
                               into the configuration file which should be
                               properly ACL'ed)
      --id, --publisherid, --PublisherId=VALUE
                             Sets the publisher id of the publisher.
                               Default: `not set` which results in the IoT edge
                               identity being used
  -s, --site, --SiteId=VALUE Sets the site name of the publisher module.
                               Default: `not set`
      --pi, --initfile, --InitFilePath[=VALUE]
                             A file from which to read initialization
                               instructions.
                               Use this option to have OPC Publisher run a set
                               of method calls found in this file.
                               The file must be formatted using a subset of the
                               .http/.rest file format without support for
                               indentation, scripting or environment variables.
                               Default: `not set` (disabled). If only a file
                               name is specified, it is loaded from the path
                               specifed using `--pn`. If just the argument is
                               provided without a value the default is `
                               publishednodes.init`.
      --il, --initlog, --InitLogFile=VALUE
                             A file into which the results of the
                               initialization instructions are written.
                               Only valid if `--pi` option is specified.
                               Default: If a init file is set using `--pi`, it
                               is appended with the `.log` extension. If just a
                               file name is used, the file is created in the
                               same folder as the init file configured using
                               the `--pi` command line option.
      --rs, --runtimestatereporting, --RuntimeStateReporting[=VALUE]
                             Enable that when publisher starts or restarts it
                               reports its runtime state using a restart
                               message.
                               Default: `false` (disabled)
      --api-key, --ApiKey=VALUE
                             Sets the api key that must be used to authenticate
                               calls on the publisher REST endpoint.
                               Default: `not set` (Key will be generated if not
                               available)
      --doa, --disableopenapi, --DisableOpenApiEndpoint[=VALUE]
                             Disable the OPC Publisher Open API endpoint
                               exposed by the built-in HTTP server.
                               Default: `false` (enabled).

Messaging configuration
-----------------------

  -c, --strict, --UseStandardsCompliantEncoding[=VALUE]
                             Use strict OPC UA standard compliance. It is
                               recommended to run the publisher in compliant
                               mode for best interoperability.
                               Be aware that explicitly specifying other
                               command line options can result in non-
                               comnpliance despite this option being set.
                               Default: `false` for backwards compatibility (2.
                               5.x - 2.8.x)
      --nf, --namespaceformat, --DefaultNamespaceFormat=VALUE
                             The format to use when serializing node ids and
                               qualified names containing a namespace uri into
                               a string.
                               Allowed values:
                                   `Uri`
                                   `Index`
                                   `Expanded`
                               Default: `Expanded` if `-c` is specified,
                               otherwise `Uri` for backwards compatibility.
      --mm, --messagingmode, --MessagingMode=VALUE
                             The messaging mode for messages
                               Allowed values:
                                   `PubSub`
                                   `Samples`
                                   `FullNetworkMessages`
                                   `FullSamples`
                                   `DataSetMessages`
                                   `SingleDataSetMessage`
                                   `DataSets`
                                   `SingleDataSet`
                                   `RawDataSets`
                                   `SingleRawDataSet`
                               Default: `PubSub` if `-c` is specified,
                               otherwise `Samples` for backwards compatibility.
      --ode, --optimizeddatasetencoding, --WriteValueWhenDataSetHasSingleEntry[=VALUE]
                             When a data set has a single entry the encoder
                               will write only the value of a data set entry
                               and omit the key.
                               This is not compliant with OPC UA Part 14.
                               Default: `false`.
      --me, --messageencoding, --MessageEncoding=VALUE
                             The message encoding for messages
                               Allowed values:
                                   `Uadp`
                                   `Json`
                                   `Xml`
                                   `Avro`
                                   `IsReversible`
                                   `JsonReversible`
                                   `IsGzipCompressed`
                                   `JsonGzip`
                                   `AvroGzip`
                                   `JsonReversibleGzip`
                               Default: `Json`.
      --bi, --batchtriggerinterval, --BatchTriggerInterval=VALUE
                             The network message publishing interval in
                               milliseconds. Determines the publishing period
                               at which point messages are emitted.
                               When `--bs` is 1 and `--bi` is set to 0 batching
                               is disabled.
                               Default: `10000` (10 seconds).
                               Also can be set using `BatchTriggerInterval`
                               environment variable in the form of a duration
                               string in the form `[d.]hh:mm:ss[.fffffff]`.
      --bs, --batchsize, --BatchSize=VALUE
                             The number of incoming OPC UA subscription
                               notifications to collect until sending a network
                               messages. When `--bs` is set to 1 and `--bi` is
                               0 batching is disabled and messages are sent as
                               soon as notifications arrive.
                               Default: `50`.
      --rdb, --removedupsinbatch, --RemoveDuplicatesFromBatch[=VALUE]
                             Use this option to remove values with the same
                               node id from batch messages in legacy `Samples`
                               mode. Sends only the latest value as per the
                               value's source timestamp.
                               Only applies to `Samples` mode, otherwise this
                               setting is ignored.
                               Default: `false` (keep all duplicate values).
      --ms, --maxmessagesize, --iothubmessagesize, --IoTHubMaxMessageSize=VALUE
                             The maximum size of the messages to emit. In case
                               the encoder cannot encode a message because the
                               size would be exceeded, the message is dropped.
                               Otherwise the encoder will aim to chunk messages
                               if possible.
                               Default: `256k` in case of IoT Hub messages, `0`
                               otherwise.
      --qos, --DefaultQualityOfService=VALUE
                             The default quality of service to use for data set
                               messages.
                               This does not apply to metadata messages which
                               are always sent with `AtLeastOnce` semantics.
                               Allowed values:
                                   `AtMostOnce`
                                   `AtLeastOnce`
                                   `ExactlyOnce`
                               Default: `AtLeastOnce`.
      --ttl, --DefaultMessageTimeToLive=VALUE
                             The default time to live for all network message
                               published in milliseconds if the transport
                               supports it.
                               This does not apply to metadata messages which
                               are always sent with a ttl of the metadata
                               update interval or infinite ttl.
                               Default: `not set` (infinite).
      --retain, --DefaultMessageRetention[=VALUE]
                             Whether by default to send messages with retain
                               flag to a broker if the transport supports it.
                               This does not apply to metadata messages which
                               are always sent as retained messages.
                               Default: `false'.
      --mts, --messagetimestamp, --MessageTimestamp=VALUE
                             The value to set as as the timestamp property of
                               messages during encoding (if the encoding
                               supports writing message timestamps).
                               Allowed values:
                                   `CurrentTimeUtc`
                                   `PublishTime`
                                   `EncodingTimeUtc`
                               Default: `CurrentTimeUtc` to use the time when
                               the message was created in OPC Publisher.
      --npd, --maxnodesperdataset, --MaxNodesPerDataSet=VALUE
                             Maximum number of nodes within a Subscription.
                               When there are more nodes configured for a data
                               set writer, they will be added to new
                               subscriptions. This also affects metadata
                               message size.
                               Default: `1000`.
      --kfc, --keyframecount, --DefaultKeyFrameCount=VALUE
                             The default number of delta messages to send until
                               a key frame message is sent. If 0, no key frame
                               messages are sent, if 1, every message will be a
                               key frame.
                               Default: `0`.
      --ka, --sendkeepalives, --EnableDataSetKeepAlives[=VALUE]
                             Enables sending keep alive messages triggered by
                               writer subscription's keep alive notifications.
                               This setting can be used to enable the messaging
                               profile's support for keep alive messages.
                               If the chosen messaging profile does not support
                               keep alive messages this setting is ignored.
                               Default: `false` (to save bandwidth).
      --msi, --metadatasendinterval, --DefaultMetaDataUpdateTime=VALUE
                             Default value in milliseconds for the metadata
                               send interval which determines in which interval
                               metadata is sent.
                               Even when disabled, metadata is still sent when
                               the metadata version changes unless `--mm=*
                               Samples` is set in which case this setting is
                               ignored. Only valid for network message
                               encodings.
                               Default: `0` which means periodic sending of
                               metadata is disabled.
      --dm, --disablemetadata, --DisableDataSetMetaData[=VALUE]
                             Disables sending any metadata when metadata
                               version changes. This setting can be used to
                               also override the messaging profile's default
                               support for metadata sending.
                               It is recommended to disable sending metadata
                               when too many nodes are part of a data set as
                               this can slow down start up time.
                               Default: `false` if the messaging profile
                               selected supports sending metadata and `--strict`
                                is set but not '--dct', `True` otherwise.
      --amt, --asyncmetadataloadtimeout, --AsyncMetaDataLoadTimeout=VALUE
                             The default duration in seconds a publish request
                               should wait until the meta data is loaded.
                               Loaded metadata guarantees a metadata message is
                               sent before the first message is sent but
                               loading of metadata takes time during
                               subscription setup. Set to `0` to block until
                               metadata is loaded.
                               Only used if meta data is supported and enabled.
                               Default: `5000` milliseconds.
      --ps, --publishschemas, --PublishMessageSchema[=VALUE]
                             Publish the Avro or Json message schemas to schema
                               registry or subtopics.
                               Automatically enables complex type system and
                               metadata support.
                               Only has effect if the messaging profile
                               supports publishing schemas.
                               Default: `True` if the message encoding requires
                               schemas (for example Avro) otherwise `False`.
      --asj, --preferavro, --PreferAvroOverJsonSchema[=VALUE]
                             Publish Avro schema even for Json encoded messages.
                                Automatically enables publishing schemas as if `
                               --ps` was set.
                               Default: `false`.
      --daf, --disableavrofiles, --DisableAvroFileWriter[=VALUE]
                             Disable writing avro files and instead dump
                               messages and schema as zip files using the
                               filesystem transport.
                               Default: `false`.
      --om, --maxsendqueuesize, --MaxNetworkMessageSendQueueSize=VALUE
                             The maximum number of messages to buffer on the
                               send path before messages are dropped.
                               Default: `4096`
      --wgp, --writergrouppartitions, --DefaultWriterGroupPartitionCount=VALUE
                             The number of partitions to split the writer group
                               into. Each partition represents a data flow to
                               the transport sink. The partition is selected by
                               topic hash.
                               Default: `0` (partitioning is disabled)
  -t, --dmt, --defaultmessagetransport, --DefaultTransport=VALUE
                             The desired transport to use to publish network
                               messages with.
                               Requires the transport to be properly configured
                               (see transport settings).
                               Allowed values:
                                   `IoTHub`
                                   `Mqtt`
                                   `EventHub`
                                   `Dapr`
                                   `Http`
                                   `FileSystem`
                                   `Null`
                               Default: `IoTHub` or the first configured
                               transport of the allowed value list.

Transport settings
------------------

  -b, --mqc, --mqttclientconnectionstring, --MqttClientConnectionString=VALUE
                             An mqtt connection string to use. Use this option
                               to connect OPC Publisher to a MQTT Broker
                               endpoint.
                               To connect to an MQTT broker use the format '
                               HostName=<IPorDnsName>;Port=<Port>[;Username=<
                               Username>;Password=<Password>;Protocol=<'v5'|'
                               v311'>]'. To publish via MQTT by default specify
                               `-t=Mqtt`.
                               Default: `not set`.
  -e, --ec, --edgehubconnectionstring, --dc, --deviceconnectionstring, --EdgeHubConnectionString=VALUE
                             A edge hub or iot hub connection string to use if
                               you run OPC Publisher outside of IoT Edge. The
                               connection string can be obtained from the IoT
                               Hub portal. It is not required to use this
                               option if running inside IoT Edge. To publish
                               through IoT Edge by default specify `-t=IoTHub`.
                               Default: `not set`.
      --ht, --ih, --iothubprotocol, --Transport=VALUE
                             Protocol to use for communication with EdgeHub.
                               Allowed values:
                                   `None`
                                   `AmqpOverTcp`
                                   `AmqpOverWebsocket`
                                   `Amqp`
                                   `MqttOverTcp`
                                   `Tcp`
                                   `MqttOverWebsocket`
                                   `Websocket`
                                   `Mqtt`
                                   `Any`
                               Default: `Mqtt` if device or edge hub connection
                               string is provided, ignored otherwise.
      --eh, --eventhubnamespaceconnectionstring, --EventHubNamespaceConnectionString=VALUE
                             The connection string of an existing event hub
                               namespace to use for the Azure EventHub
                               transport.
                               Default: `not set`.
      --sg, --schemagroup, --SchemaGroupName=VALUE
                             The schema group in an event hub namespace to
                               publish message schemas to.
                               Default: `not set`.
  -d, --dcs, --daprconnectionstring, --DaprConnectionString=VALUE
                             Connect the OPC Publisher to a dapr pub sub
                               component using a connection string.
                               The connection string specifies the PubSub
                               component to use and allows you to configure the
                               side car connection if needed.
                               Use the format 'PubSubComponent=<PubSubComponent>
                               [;GrpcPort=<GrpcPort>;HttpPort=<HttpPort>[;
                               Scheme=<'https'|'http'>][;Host=<IPorDnsName>]][;
                               CheckSideCarHealth=<'true'|'false'>]'.
                               To publish through dapr by default specify `-t=
                               Dapr`.
                               Default: `not set`.
  -w, --hcs, --httpconnectionstring, --HttpConnectionString=VALUE
                             Allows OPC Publisher to publish multipart messages
                               to a topic path using the http protocol (web
                               hook). Specify the target host and configure the
                               optional connection settings using a connection
                               string of the format 'HostName=<IPorDnsName>[;
                               Port=<Port>][;Scheme=<'https'|'http'>][;Put=true]
                               [;ApiKey=<ApiKey>]'. To publish via HTTP by
                               default specify `-t=Http`.
                               Default: `not set`.
  -o, --outdir, --OutputRoot=VALUE
                             A folder to write messages into.
                               Use this option to have OPC Publisher write
                               messages to a folder structure under this folder.
                                The structure reflects the topic tree. To
                               publish into the file system folder by default
                               specify `-t=FileSystem`.
                               Default: `not set`.
  -p, --httpserverport, --HttpServerPort=VALUE
                             The port on which the http server of OPC Publisher
                               is listening.
                               Default: `9072` if no value is provided.
      --unsecurehttp, --UnsecureHttpServerPort[=VALUE]
                             Allow unsecure access to the REST api of OPC
                               Publisher. A port can be specified if the
                               default port 9071 is not desired.
                               Do not enable this in production as it exposes
                               the Api Key on the network.
                               Default: `disabled`, if specified without a port
                               `9071` port is used.
      --rtc, --renewtlscert, --RenewTlsCertificateOnStartup[=VALUE]
                             If set a new tls certificate is created during
                               startup updating any previously created ones.
                               Default: `false`.
      --useopenapiv3, --UseOpenApiV3[=VALUE]
                             If enabled exposes the open api schema of OPC
                               Publisher using v3 schema (yaml).
                               Only valid if Open API endpoint is not disabled.
                               Default: `v2` (json).

Routing configuration
---------------------

      --rtt, --roottopictemplate, --RootTopicTemplate[=VALUE]
                             The default root topic of OPC Publisher.
                               If not specified, the `{PublisherId}` template
                               is the root topic.
                               Currently only the template variables
                                   `{SiteId}` and
                                   `{PublisherId}`
                               can be used as dynamic substituations in the
                               template. If the template variable does not
                               exist it is replaced with the `$default` string.
                               Default: `{PublisherId}`.
      --mtt, --methodtopictemplate, --MethodTopicTemplate=VALUE
                             The topic at which OPC Publisher's method handler
                               is mounted.
                               If not specified, the `{RootTopic}/methods`
                               template will be used as root topic with the
                               method names as sub topic.
                               Only
                                   `{RootTopic}`
                                   `{SiteId}` and
                                   `{PublisherId}`
                               can currently be used as replacement variables
                               in the template.
                               Default: `{RootTopic}/methods`.
      --ttt, --telemetrytopictemplate, --TelemetryTopicTemplate[=VALUE]
                             The default topic that all messages are sent to.
                               If not specified, the `{RootTopic}/messages/{
                               WriterGroup}` template will be used as root
                               topic for all events sent by OPC Publisher.
                               The template variables
                                   `{RootTopic}`
                                   `{SiteId}`
                                   `{Encoding}`
                                   `{PublisherId}`
                                   `{DataSetClassId}`
                                   `{DataSetWriter}` and
                                   `{WriterGroup}`
                                can be used as dynamic parts in the template.
                               If a template variable does not exist the name
                               of the variable is emitted.
                               Default: `{RootTopic}/messages/{WriterGroup}`.
      --ett, --eventstopictemplate, --EventsTopicTemplate=VALUE
                             The topic into which OPC Publisher publishes any
                               events that are not telemetry messages such as
                               discovery or runtime events.
                               If not specified, the `{RootTopic}/events`
                               template will be used.
                               Only
                                   `{RootTopic}`
                                   `{SiteId}`
                                   `{Encoding}` and
                                   `{PublisherId}`
                               can currently be used as replacement variables
                               in the template.
                               Default: `{RootTopic}/events`.
      --dtt, --diagnosticstopictemplate, --DiagnosticsTopicTemplate=VALUE
                             The topic into which OPC Publisher publishes
                               writer group diagnostics events.
                               If not specified, the `{RootTopic}/diagnostics/{
                               WriterGroup}` template will be used.
                               Only
                                   `{RootTopic}`
                                   `{SiteId}`
                                   `{Encoding}`
                                   `{PublisherId}` and
                                   `{WriterGroup}`
                               can currently be used as replacement variables
                               in the template.
                               Default: `{RootTopic}/diagnostics/{WriterGroup}`
      --mdt, --metadatatopictemplate, --DataSetMetaDataTopicTemplate[=VALUE]
                             The topic that metadata should be sent to.
                               In case of MQTT the message will be sent as
                               RETAIN message with a TTL of either metadata
                               send interval or infinite if metadata send
                               interval is not configured.
                               Only valid if metadata is supported and/or
                               explicitely enabled.
                               The template variables
                                   `{RootTopic}`
                                   `{SiteId}`
                                   `{TelemetryTopic}`
                                   `{Encoding}`
                                   `{PublisherId}`
                                   `{DataSetClassId}`
                                   `{DataSetWriter}` and
                                   `{WriterGroup}`
                               can be used as dynamic parts in the template.
                               Default: `{TelemetryTopic}` which means metadata
                               is sent to the same output as regular messages.
                               If specified without value, the default output
                               is `{TelemetryTopic}/metadata`.
      --stt, --schematopictemplate, --SchemaTopicTemplate[=VALUE]
                             The topic that schemas should be sent to if schema
                               publishing is configured.
                               In case of MQTT schemas will not be sent with .
                               Only valid if schema publishing is enabled (`--
                               ps`).
                               The template variables
                                   `{RootTopic}`
                                   `{SiteId}`
                                   `{PublisherId}`
                                   `{TelemetryTopic}`
                               can be used as variables inside the template.
                               Default: `{TelemetryTopic}/schema` which means
                               the schema is sent to a sub topic where the
                               telemetry message is sent to.
      --uns, --datasetrouting, --DefaultDataSetRouting=VALUE
                             Configures whether messages should automatically
                               be routed using the browse path of the monitored
                               item inside the address space starting from the
                               RootFolder.
                               The browse path is appended as topic structure
                               to the telemetry topic root which can be
                               configured using `--ttt`. Reserved characters in
                               browse names are escaped with their hex ASCII
                               code.
                               Allowed values:
                                   `None`
                                   `UseBrowseNames`
                                   `UseBrowseNamesWithNamespaceIndex`
                               Default: `None` (Topics must be configured).
      --ri, --enableroutinginfo, --EnableRoutingInfo[=VALUE]
                             Add routing information to messages. The name of
                               the property is `$$RoutingInfo` and the value is
                               the `DataSetWriterGroup` from which the
                               particular message is emitted.
                               Default: `False`.

Subscription settings
---------------------

      --oi, --opcsamplinginterval, --DefaultSamplingInterval=VALUE
                             Default value in milliseconds to request the
                               servers to sample values. This value is used if
                               an explicit sampling interval for a node was not
                               configured.
                               Default: `1000`.
                               Also can be set using `DefaultSamplingInterval`
                               environment variable in the form of a duration
                               string in the form `[d.]hh:mm:ss[.fffffff]`.
      --op, --opcpublishinginterval, --DefaultPublishingInterval=VALUE
                             Default value in milliseconds for the publishing
                               interval setting of a subscription created with
                               an OPC UA server. This value is used if an
                               explicit publishing interval was not configured.
                               When setting `--op=0` the server decides the
                               lowest publishing interval it can support.
                               Default: `1000`.
                               Also can be set using `DefaultPublishingInterval`
                                environment variable in the form of a duration
                               string in the form `[d.]hh:mm:ss[.fffffff]`.
      --eip, --immediatepublishing, --EnableImmediatePublishing[=VALUE]
                             By default OPC Publisher will create a
                               subscription with publishing disabled and only
                               enable it after it has filled it with all
                               configured monitored items. Use this setting to
                               create the subscription with publishing already
                               enabled.
                               Default: `false`.
      --ska, --keepalivecount, --DefaultKeepAliveCount=VALUE
                             Specifies the default number of publishing
                               intervals before a keep alive is returned with
                               the next queued publishing response.
                               Default: `0`.
      --slt, --lifetimecount, --DefaultLifetimeCount=VALUE
                             Default subscription lifetime count which is a
                               multiple of the keep alive counter and when
                               reached instructs the server to declare the
                               subscription invalid.
                               Default: `0`.
      --fd, --fetchdisplayname, --FetchOpcNodeDisplayName[=VALUE]
                             Fetches the displayname for the monitored items
                               subscribed if a display name was not specified
                               in the configuration.
                               Note: This has high impact on OPC Publisher
                               startup performance.
                               Default: `false` (disabled).
      --fp, --fetchpathfromroot, --FetchOpcBrowsePathFromRoot[=VALUE]
                             (Experimental) Explicitly disable or enable
                               retrieving relative paths from root for
                               monitored items.
                               Default: `false` (disabled).
      --qs, --queuesize, --DefaultQueueSize=VALUE
                             Default queue size for all monitored items if
                               queue size was not specified in the
                               configuration.
                               Default: `1` (for backwards compatibility).
      --aq, --autosetqueuesize, --AutoSetQueueSizes[=VALUE]
                             (Experimental) Automatically calculate queue sizes
                               for monitored items using the subscription
                               publishing interval and the item's sampling rate
                               as max(configured queue size, roundup(
                               publishinginterval / samplinginterval)).
                               Note that the server might revise the queue size
                               down if it cannot handle the calculated size.
                               Default: `false` (disabled).
      --ndo, --nodiscardold, --DiscardNew[=VALUE]
                             The publisher is using this as default value for
                               the discard old setting of monitored item queue
                               configuration. Setting to true will ensure that
                               new values are dropped before older ones are
                               drained.
                               Default: `false` (which is the OPC UA default).
      --mc, --monitoreditemdatachangetrigger, --DefaulDataChangeTrigger=VALUE
                             Default data change trigger for all monitored
                               items configured in the published nodes
                               configuration unless explicitly overridden.
                               Allowed values:
                                   `Status`
                                   `StatusValue`
                                   `StatusValueTimestamp`
                               Default: `StatusValue` (which is the OPC UA
                               default).
      --mwt, --monitoreditemwatchdog, --DefaultMonitoredItemWatchdogSeconds=VALUE
                             The subscription and monitored item watchdog
                               timeout in seconds the subscription uses to
                               check on late reporting monitored items unless
                               overridden in the published nodes configuration
                               explicitly.
                               Default: `not set` (watchdog disabled).
      --mwc, --monitoreditemwatchdogcondition, --DefaultMonitoredItemWatchdogCondition=VALUE
                             The default condition when to run the action
                               configured as the watchdog behavior. The
                               condition can be overridden in the published
                               nodes configuration.
                               Allowed values:
                                   `WhenAllAreLate`
                                   `WhenAnyIsLate`
                               Default: `WhenAllAreLate` (if enabled).
      --dwb, --watchdogbehavior, --DefaultWatchdogBehavior=VALUE
                             Default behavior of the subscription and monitored
                               item watchdog mechanism unless overridden in the
                               published nodes configuration explicitly.
                               Allowed values:
                                   `Diagnostic`
                                   `Reset`
                                   `FailFast`
                                   `ExitProcess`
                               Default: `Diagnostic` (if enabled).
      --sf, --skipfirst, --DefaultSkipFirst[=VALUE]
                             The publisher is using this as default value for
                               the skip first setting of nodes configured
                               without a skip first setting. A value of True
                               will skip sending the first notification
                               received when the monitored item is added to the
                               subscription.
                               Default: `False` (disabled).
      --rat, --republishaftertransfer, --RepublishAfterTransfer[=VALUE]
                             Configure whether publisher republishes missed
                               subscription notifications still in the server
                               queue after transferring a subscription during
                               reconnect handling.
                               This can result in out of order notifications
                               after a reconnect but minimizes data loss.
                               Default: `False` (disabled).
      --hbb, --heartbeatbehavior, --DefaultHeartbeatBehavior=VALUE
                             Default behavior of the heartbeat mechanism unless
                               overridden in the published nodes configuration
                               explicitly.
                               Allowed values:
                                   `WatchdogLKV`
                                   `WatchdogLKG`
                                   `PeriodicLKV`
                                   `PeriodicLKG`
                                   `WatchdogLKVWithUpdatedTimestamps`
                                   `WatchdogLKVDiagnosticsOnly`
                                   `PeriodicLKVDropValue`
                                   `PeriodicLKGDropValue`
                               Default: `WatchdogLKV` (Sending LKV in a
                               watchdog fashion).
      --hb, --heartbeatinterval, --DefaultHeartbeatInterval=VALUE
                             The publisher is using this as default value in
                               seconds for the heartbeat interval setting of
                               nodes that were configured without a heartbeat
                               interval setting. A heartbeat is sent at this
                               interval if no value has been received.
                               Default: `0` (disabled)
                               Also can be set using `DefaultHeartbeatInterval`
                               environment variable in the form of a duration
                               string in the form `[d.]hh:mm:ss[.fffffff]`.
      --ucr, --usecyclicreads, --DefaultSamplingUsingCyclicRead[=VALUE]
                             All nodes should be sampled using periodical
                               client reads instead of subscriptions services,
                               unless otherwise configured.
                               Default: `false`.
      --xmi, --maxmonitoreditems, --MaxMonitoredItemPerSubscription=VALUE
                             Max monitored items per subscription until the
                               subscription is split.
                               This is used if the server does not provide
                               limits in its server capabilities.
                               Default: `not set`.
      --da, --deferredacks, --UseDeferredAcknoledgements[=VALUE]
                             (Experimental) Acknoledge subscription
                               notifications only when the data has been
                               successfully published.
                               Default: `false`.
      --rbp, --rebrowseperiod, --DefaultRebrowsePeriod=VALUE
                             (Experimental) The default time to wait until the
                               address space model is browsed again when
                               generating model change notifications.
                               Default: `12:00:00`.
      --sqp, --sequentialpublishing, --EnableSequentialPublishing[=VALUE]
                             Set to false to disable sequential publishing in
                               the protocol stack.
                               Default: `True` (enabled).
      --smi, --subscriptionmanagementinterval, --SubscriptionManagementIntervalSeconds=VALUE
                             The interval in seconds after which the publisher
                               re-applies the desired state of the subscription
                               to a session.
                               Default: `0` (only on configuration change).
      --bnr, --badnoderetrydelay, --BadMonitoredItemRetryDelaySeconds=VALUE
                             The delay in seconds after which nodes that were
                               rejected by the server while added or updating a
                               subscription or while publishing, are re-applied
                               to a subscription.
                               Set to 0 to disable retrying.
                               Default: `1800` seconds.
      --inr, --invalidnoderetrydelay, --InvalidMonitoredItemRetryDelaySeconds=VALUE
                             The delay in seconds after which the publisher
                               attempts to re-apply nodes that were incorrectly
                               configured to a subscription.
                               Set to 0 to disable retrying.
                               Default: `300` seconds.
      --ser, --subscriptionerrorretrydelay, --SubscriptionErrorRetryDelaySeconds=VALUE
                             The delay in seconds between attempts to create a
                               subscription in a session.
                               Set to 0 to disable retrying.
                               Default: `2` seconds.
      --urc, --usereverseconnect, --DefaultUseReverseConnect[=VALUE]
                             (Experimental) Use reverse connect for all
                               endpoints in the published nodes configuration
                               unless otherwise configured.
                               Default: `false`.
      --dtr, --disabletransferonreconnect, --DisableSubscriptionTransfer[=VALUE]
                             Do not attempt to transfer subscriptions when
                               reconnecting but re-establish the subscription.
                               Default: `false`.
      --dct, --disablecomplextypesystem, --DisableComplexTypeSystem[=VALUE]
                             Never load the complex type system for any
                               connections that are required for subscriptions.
                               This setting not just disables meta data
                               messages but also prevents transcoding of
                               unknown complex types in outgoing messages.
                               Default: `false`.
      --dsg, --disablesessionpergroup, --DisableSessionPerWriterGroup[=VALUE]
                             Disable creating a separate session per writer
                               group. Instead sessions are re-used across
                               writer groups.
                               Default: `False`.
      --ipi, --ignorepublishingintervals, --IgnoreConfiguredPublishingIntervals[=VALUE]
                             Always use the publishing interval provided via
                               command line argument `--op` and ignore all
                               publishing interval settings in the
                               configuration.
                               Combine with `--op=0` to let the server use the
                               lowest publishing interval it can support.
                               Default: `False` (disabled).

OPC UA Client configuration
---------------------------

      --aa, --acceptuntrusted, --AutoAcceptUntrustedCertificates[=VALUE]
                             The publisher accepts untrusted certificates
                               presented by a server it connects to.
                               This does not include servers presenting bad
                               certificates or certificates that fail chain
                               validation. These errors cannot be suppressed
                               and connection will always be rejected.
                               WARNING: This setting should never be used in
                               production environments!
      --rur, --rejectunknownrevocationstatus, --RejectUnknownRevocationStatus[=VALUE]
                             Set this to `False` to accept certificates
                               presented by a server that have an unknown
                               revocation status.
                               WARNING: This setting should never be used in
                               production environments!
                               Default: `True`.
      --ct, --createsessiontimeout, --CreateSessionTimeout=VALUE
                             Amount of time in seconds to wait until a session
                               is connected.
                               Default: `5` seconds.
      --mr, --reconnectperiod, --MinReconnectDelay=VALUE
                             The minimum amount of time in milliseconds to wait
                               reconnection of session is attempted again.
                               Default: `1000` milliseconds.
      --xr, --maxreconnectperiod, --MaxReconnectDelay=VALUE
                             The maximum amount of time in millseconds to wait
                               between reconnection attempts of the session.
                               Default: `60000` milliseconds.
      --sto, --sessiontimeout, --DefaultSessionTimeout=VALUE
                             Maximum amount of time in seconds that a session
                               should remain open by the OPC server without any
                               activity (session timeout). Requested from the
                               OPC server at session creation.
                               Default: `60` seconds.
      --ki, --keepaliveinterval, --KeepAliveInterval=VALUE
                             The interval in seconds the publisher is sending
                               keep alive messages to the OPC servers on the
                               endpoints it is connected to.
                               Default: `10` seconds.
      --sct, --servicecalltimeout, --DefaultServiceCallTimeout=VALUE
                             Maximum amount of time in seconds that a service
                               call should take before it is being cancelled.
                               This value can be overridden in the request
                               header.
                               Default: `180` seconds.
      --cto, --connecttimeout, --DefaultConnectTimeout=VALUE
                             Maximum amount of time in seconds that a service
                               call should wait for a connected session to be
                               used.
                               This value can be overridden in the request
                               header.
                               Default: `not set` (in this case the default
                               service call timeout value is used).
      --ot, --operationtimeout, --OperationTimeout=VALUE
                             The operation service call timeout of individual
                               service requests to the server in milliseconds.
                               As opposed to the `--sco` timeout, this is the
                               timeout hint provided to the server in every
                               request.
                               This value can be overridden in the request
                               header.
                               Default: `120000` milliseconds.
      --cl, --clientlinger, --LingerTimeoutSeconds=VALUE
                             Amount of time in seconds to delay closing a
                               client and underlying session after the a last
                               service call.
                               Use this setting to speed up multiple subsequent
                               calls.
                               Default: `0` sec (no linger).
      --rcp, --reverseconnectport, --ReverseConnectPort=VALUE
                             The port to use when accepting inbound reverse
                               connect requests from servers.
                               Default: `4840`.
      --mnr, --maxnodesperread, --MaxNodesPerReadOverride=VALUE
                             Limit max number of nodes to read in a single read
                               request when batching reads or the server limit
                               if less.
                               Default: `0` (using server limit).
      --mnb, --maxnodesperbrowse, --MaxNodesPerBrowseOverride=VALUE
                             Limit max number of nodes per browse request when
                               batching browse operations or the server limit
                               if less.
                               Default: `0` (using server limit).
      --mpr, --minpublishrequests, --MinPublishRequests=VALUE
                             Minimum number of publish requests to queue once
                               subscriptions are created in the session.
                               Default: `2`.
      --ppr, --percentpublishrequests, --PublishRequestsPerSubscriptionPercent=VALUE
                             Percentage ratio of publish requests per
                               subscriptions in the session in percent up to
                               the number configured using `--xpr`.
                               Default: `100`% (1 request per subscription).
      --xpr, --maxpublishrequests, --MaxPublishRequests=VALUE
                             Maximum number of publish requests to every queue
                               once subscriptions are created in the session.
                               Default: `10`.
      --dcp, --disablecomplextypepreloading, --DisableComplexTypePreloading[=VALUE]
                             Complex types (structures, enumerations) a server
                               exposes are preloaded from the server after the
                               session is connected. In some cases this can
                               cause problems either on the client or server
                               itself. Use this setting to disable pre-loading
                               support.
                               Note that since the complex type system is used
                               for meta data messages it will still be loaded
                               at the time the subscription is created,
                               therefore also disable meta data support if you
                               want to ensure the complex types are never
                               loaded for an endpoint.
                               Default: `false`.
      --otl, --opctokenlifetime, --SecurityTokenLifetime=VALUE
                             OPC UA Stack Transport Secure Channel - Security
                               token lifetime in milliseconds.
                               Default: `3600000` (1h).
      --ocl, --opcchannellifetime, --ChannelLifetime=VALUE
                             OPC UA Stack Transport Secure Channel - Channel
                               lifetime in milliseconds.
                               Default: `300000` (5 min).
      --omb, --opcmaxbufferlen, --MaxBufferSize=VALUE
                             OPC UA Stack Transport Secure Channel - Max buffer
                               size.
                               Default: `65535` (64KB -1).
      --oml, --opcmaxmessagelen, --MaxMessageSize=VALUE
                             OPC UA Stack Transport Secure Channel - Max
                               message size.
                               Default: `4194304` (4 MB).
      --oal, --opcmaxarraylen, --MaxArrayLength=VALUE
                             OPC UA Stack Transport Secure Channel - Max array
                               length.
                               Default: `65535` (64KB - 1).
      --ol, --opcmaxstringlen, --MaxStringLength=VALUE
                             The max length of a string opc can transmit/
                               receive over the OPC UA secure channel.
                               Default: `130816` (128KB - 256).
      --obl, --opcmaxbytestringlen, --MaxByteStringLength=VALUE
                             OPC UA Stack Transport Secure Channel - Max byte
                               string length.
                               Default: `1048576` (1MB).
      --au, --appuri, --ApplicationUri=VALUE
                             Application URI as per OPC UA definition inside
                               the OPC UA client application configuration
                               presented to the server.
                               Default: `not set`.
      --pu, --producturi, --ProductUri=VALUE
                             The Product URI as per OPC UA definition insde the
                               OPC UA client application configuration
                               presented to the server.
                               Default: `not set`.
      --rejectsha1, --RejectSha1SignedCertificates=VALUE
                             If set to `False` OPC Publisher will accept SHA1
                               certificates which have been officially
                               deprecated and are unsafe to use.
                               Note: Set this to `False` to support older
                               equipment that uses Sha1 signed certificates
                               rather than using no security.
                               Default: `True`.
      --mks, --minkeysize, --MinimumCertificateKeySize=VALUE
                             Minimum accepted certificate size.
                               Note: It is recommended to this value to the
                               highest certificate key size possible based on
                               the connected OPC UA servers.
                               Default: 1024.
      --tm, --trustmyself, --AddAppCertToTrustedStore=VALUE
                             Set to `False` to disable adding the publisher's
                               own certificate to the trusted store
                               automatically.
                               Default: `True`.
      --sn, --appcertsubjectname, --ApplicationCertificateSubjectName=VALUE
                             The subject name for the app cert.
                               Default: `CN=<the value of --an|--appname>, C=DE,
                                S=Bav, O=Microsoft, DC=localhost`.
      --an, --appname, --ApplicationName=VALUE
                             The name for the app (used during OPC UA
                               authentication).
                               Default: `Microsoft.Azure.IIoT`
      --pki, --pkirootpath, --PkiRootPath=VALUE
                             PKI certificate store root path.
                               Default: `pki`.
      --ap, --appcertstorepath, --ApplicationCertificateStorePath=VALUE
                             The path where the own application cert should be
                               stored.
                               Default: $"{PkiRootPath}/own".
      --apt, --at, --appcertstoretype, --ApplicationCertificateStoreType=VALUE
                             The own application cert store type.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --cfa, --configurefromappcert, --TryConfigureFromExistingAppCert[=VALUE]
                             Automatically set the application subject name,
                               host name and application uri from the first
                               valid application certificate found in the
                               application certificate store path.
                               If the chosen certificate is valid, it will be
                               used, otherwise a new, self-signed certificate
                               with the information will be created.
                               Default: `false`.
      --apw, --appcertstorepwd, --ApplicationCertificatePassword=VALUE
                             Password to use when storing the application
                               certificate in the store folder if the store is
                               of type `Directory`.
                               Default: empty, which means application
                               certificate is not protected by default.
      --tp, --trustedcertstorepath, --TrustedPeerCertificatesPath=VALUE
                             The path of the trusted cert store.
                               Default: $"{PkiRootPath}/trusted".
      --tpt, --TrustedPeerCertificatesType=VALUE
                             Trusted peer certificate store type.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --rp, --rejectedcertstorepath, --RejectedCertificateStorePath=VALUE
                             The path of the rejected cert store.
                               Default: $"{PkiRootPath}/rejected".
      --rpt, --RejectedCertificateStoreType=VALUE
                             Rejected certificate store type.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --ip, --issuercertstorepath, --TrustedIssuerCertificatesPath=VALUE
                             The path of the trusted issuer cert store.
                               Default: $"{PkiRootPath}/issuer".
      --ipt, --TrustedIssuerCertificatesType=VALUE
                             Trusted issuer certificate store type.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --up, --usercertstorepath, --TrustedUserCertificatesPath=VALUE
                             The path of the certificate store for user
                               certificates.
                               Default: $"{PkiRootPath}/users".
      --upt, --TrustedUserCertificatesType=VALUE
                             Type of certificate store for all User
                               certificates.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.
      --uip, --userissuercertstorepath, --UserIssuerCertificatesPath=VALUE
                             The path of the user issuer cert store.
                               Default: $"{PkiRootPath}/users/issuer".
      --uit, --UserIssuerCertificatesType=VALUE
                             Type of the issuer certificate store for User
                               certificates.
                               Allowed values:
                                   `Directory`
                                   `X509Store`
                               Default: `Directory`.

Diagnostic options
------------------

      --ll, --loglevel, --LogLevel=VALUE
                             The loglevel to use.
                               Allowed values:
                                   `Trace`
                                   `Debug`
                                   `Information`
                                   `Warning`
                                   `Error`
                                   `Critical`
                                   `None`
                               Default: `Information`.
      --lfm, --logformat, --LogFormat=VALUE
                             The log format to use when writing to the console.
                               Allowed values:
                                   `simple`
                                   `syslog`
                                   `systemd`
                               Default: `simple`.
      --di, --diagnosticsinterval, --DiagnosticsInterval=VALUE
                             Produce publisher diagnostic information at this
                               specified interval in seconds.
                               By default diagnostics are written to the OPC
                               Publisher logger (which requires at least --
                               loglevel `information`) unless configured
                               differently using `--pd`.
                               `0` disables diagnostic output.
                               Default:60000 (60 seconds).
                               Also can be set using `DiagnosticsInterval`
                               environment variable in the form of a duration
                               string in the form `[d.]hh:mm:ss[.fffffff]`".
      --pd, --diagnosticstarget, --DiagnosticsTarget=VALUE
                             Configures how to emit diagnostics information at
                               the `--di` configured interval.
                               Use this to for example emit diagnostics as
                               events to the event topic template instead of
                               the console.
                               Allowed values:
                                   `Logger`
                                   `Events`
                               Default: `Logger`.
      --dr, --disableresourcemonitoring, --DisableResourceMonitoring[=VALUE]
                             Disable resource monitoring as part of the
                               diagnostics output and metrics.
                               Default: `false` (enabled).
      --ln, --lognotifications[=VALUE]
                             Log ingress subscription notifications at
                               Informational level to aid debugging.
                               Default: `disabled`.
      --lnh, --lognotificationsandheartbeats[=VALUE]
                             Include heartbeats in notifications log.
                               If set also implicitly enables debug logging via
                               `--ln`.
                               Default: `disabled`.
      --lnf, --lognotificationfilter[=VALUE]
                             Only log notifications where the data set field
                               name, subscription name, or data set name match
                               the provided regular expression pattern.
                               If set implicitly enables debug logging via `--
                               ln`.
                               Default: `null` (matches all).
      --len, --logencodednotifications[=VALUE]
                             Log encoded subscription and monitored item
                               notifications at Informational level to aid
                               debugging.
                               Default: `disabled`.
      --sl, --opcstacklogging, --EnableOpcUaStackLogging[=VALUE]
                             Enable opc ua stack logging beyond logging at
                               error level.
                               Default: `disabled`.
      --ksf, --keysetlogfolder, --OpcUaKeySetLogFolderName[=VALUE]
                             Writes negotiated symmetric keys for all running
                               client connection to this file.
                               The file can be loaded by Wireshark 4.3 and used
                               to decrypt encrypted channels when analyzing
                               network traffic captures.
                               Note that enabling this feature presents a
                               security risk!
                               Default: `disabled`.
      --ecw, --enableconsolewriter, --EnableConsoleWriter[=VALUE]
                             Enable writing encoded messages to standard error
                               log through the filesystem transport (must
                               enable via `-t FileSystem` and `-o` must be set
                               to either `stderr` or `stdout`).
                               Default: `false`.
      --oc, --otlpcollector, --OtlpCollectorEndpoint=VALUE
                             Specifiy the OpenTelemetry collector grpc endpoint
                               url to export diagnostics to.
                               Default: `disabled`.
      --oxi, --otlpexportinterval, --OtlpExportIntervalMilliseconds=VALUE
                             The interval in milliseconds when OpenTelemetry is
                               exported to the collector endpoint.
                               Default: `15000` (15 seconds).
      --mms, --maxmetricstreams, --OtlpMaxMetricStreams=VALUE
                             Specifiy the max number of streams to collect in
                               the default view.
                               Default: `4000`.
      --em, --enableprometheusendpoint, --EnableMetrics[=VALUE]
                             Explicitly enable or disable exporting prometheus
                               metrics directly on the standard path.
                               Default: `disabled` if Otlp collector is
                               configured, otherwise `enabled`.
      --ari, --addruntimeinstrumentation, --OtlpRuntimeInstrumentation[=VALUE]
                             Include metrics captured for the underlying
                               runtime and web server.
                               Default: `False`.
      --ats, --addtotalsuffix, --OtlpTotalNameSuffixForCounters[=VALUE]
                             Add total suffix to all counter instrument names
                               when exporting metrics via prometheus exporter.
                               Default: `False`.
```

Currently supported combinations of `--mm` snd `--me` can be found [here](./messageformats.md).
